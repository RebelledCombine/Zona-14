// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._Stalker.Map;
using Content.Server._Stalker.Shovel;
using Content.Server._Stalker.StalkerRepository;
using Content.Server._Stalker.StationEvents.Components;
using Content.Server._Stalker.Storage;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared._Stalker.StalkerRepository;
using Content.Shared._Stalker.Storage;
using Content.Shared._Zona14.PersonalCache;
using Content.Shared.Burial.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.PersonalCache;

/// <summary>
/// Handles placement, hiding, ownership, persistence, and admin cleanup of Z14 personal caches.
/// </summary>
public sealed class Z14PersonalCacheSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StalkerStorageSystem _stalkerStorage = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private const int MaxCaches = 5;
    private const float CacheMaxWeight = 30f;
    private const float HideDelaySeconds = 5f;
    private const int RespawnSearchRadius = 3;

    private static readonly ProtoId<TagPrototype> HideContextMenuTag = new("HideContextMenu");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Z14PersonalCacheKitComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<Z14PersonalCacheComponent, InteractUsingEvent>(OnInteractUsing, before: new[] { typeof(StalkerRepositorySystem) });
        SubscribeLocalEvent<Z14PersonalCacheComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<Z14PersonalCacheComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
        SubscribeLocalEvent<Z14PersonalCacheComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<Z14PersonalCacheComponent, Z14PersonalCacheHideDoAfterEvent>(OnHideDoAfter);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
    }

    #region Placement

    private void OnUseInHand(EntityUid uid, Z14PersonalCacheKitComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetUserId(args.User, out var userId))
        {
            args.Handled = true;
            return;
        }

        var cache = TryPlaceCache(args.User, userId, uid);
        if (cache is { } cacheUid && TryComp<Z14PersonalCacheComponent>(cacheUid, out var cacheComp))
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-placed"), args.User, args.User);
            _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Low, $"Player {Name(args.User):user} placed personal cache {cacheComp.CacheId} at {cacheComp.MapKey}");
            QueueDel(uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-place-failed"), args.User, args.User);
        }

        args.Handled = true;
    }

    private EntityUid? TryPlaceCache(EntityUid user, Guid userId, EntityUid kitUid)
    {
        var caches = _dbManager.GetStalkerPersonalCachesByUserAsync(userId).GetAwaiter().GetResult();
        if (caches.Count >= MaxCaches)
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-too-many", ("count", MaxCaches)), user, user);
            return null;
        }

        var mapPos = Transform(user).MapPosition;
        if (mapPos.MapId == MapId.Nullspace)
            return null;

        var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);
        if (!_mapManager.TryFindGridAt(mapPos, out var gridUid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-invalid-tile"), user, user);
            return null;
        }

        if (IsSafeZone(mapUid) || IsSafeZone(gridUid))
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-safe-zone"), user, user);
            return null;
        }

        var tile = _map.TileIndicesFor(gridUid, gridComp, mapPos);
        if (!TryFindValidTile(gridUid, gridComp, tile, out var validTile))
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-invalid-tile"), user, user);
            return null;
        }

        return SpawnCache(gridUid, gridComp, validTile, mapUid, userId, db: null);
    }

    private EntityUid? SpawnCache(EntityUid gridUid, MapGridComponent gridComp, Vector2i tile, EntityUid mapUid, Guid userId, StalkerPersonalCache? db = null, string? mapKeyFallback = null)
    {
        var coordinates = _map.GridTileToLocal(gridUid, gridComp, tile);
        var worldPos = _map.GridTileToWorldPos(gridUid, gridComp, tile);
        var cache = Spawn("Z14PersonalCache", coordinates);

        if (!TryComp<Z14PersonalCacheComponent>(cache, out var cacheComp) ||
            !TryComp<StalkerRepositoryComponent>(cache, out var repo))
        {
            QueueDel(cache);
            return null;
        }

        cacheComp.CacheId = (db?.CacheId ?? Guid.NewGuid()).ToString();
        cacheComp.OwnerUserId = (db?.UserId ?? userId).ToString();
        cacheComp.MapKey = GetMapKey(mapUid, mapKeyFallback);
        cacheComp.X = worldPos.X;
        cacheComp.Y = worldPos.Y;
        cacheComp.Z = 0f;
        cacheComp.Hidden = db?.Hidden ?? false;

        repo.MaxWeight = CacheMaxWeight;
        repo.LoadedDbJson = db?.ContentsJson ?? string.Empty;

        _stalkerStorage.LoadStalkerItemsByEntityUid(cache);
        SetHidden(cache, cacheComp, cacheComp.Hidden);

        // Persist placement and (on respawn) the updated MapKey.
        _stalkerStorage.SaveStorage(repo);

        return cache;
    }

    #endregion

    #region Interactions

    private void OnInteractUsing(EntityUid uid, Z14PersonalCacheComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetUserId(args.User, out var userId))
            return;

        if (userId.ToString() != component.OwnerUserId)
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-not-owner"), args.User, args.User);
            args.Handled = true;
            return;
        }

        if (HasShovel(args.Used))
        {
            StartHideDoAfter(uid, args.User, args.Used);
            args.Handled = true;
            return;
        }

        if (component.Hidden)
        {
            _popup.PopupEntity(Loc.GetString("z14-personal-cache-buried"), args.User, args.User);
            args.Handled = true;
        }
    }

    private void StartHideDoAfter(EntityUid uid, EntityUid user, EntityUid used)
    {
        var ev = new Z14PersonalCacheHideDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(HideDelaySeconds), ev, uid, uid, used)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1.5f,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnHideDoAfter(EntityUid uid, Z14PersonalCacheComponent component, Z14PersonalCacheHideDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryGetUserId(args.User, out var userId) || userId.ToString() != component.OwnerUserId)
            return;

        SetHidden(uid, component, !component.Hidden);

        var key = component.Hidden ? "z14-personal-cache-hidden" : "z14-personal-cache-unhidden";
        _popup.PopupEntity(Loc.GetString(key), args.User, args.User);
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Low, $"Player {Name(args.User):user} {(component.Hidden ? "buried" : "unearthed")} personal cache {component.CacheId}");
    }

    private bool SetHidden(EntityUid uid, Z14PersonalCacheComponent component, bool hidden)
    {
        component.Hidden = hidden;

        if (!TryComp<StalkerRepositoryComponent>(uid, out var repo))
            return false;

        _stealth.SetVisibility(uid, hidden ? -1f : 1f);

        if (hidden)
            _tags.AddTag(uid, HideContextMenuTag);
        else
            _tags.RemoveTag(uid, HideContextMenuTag);

        _physics.SetCanCollide(uid, !hidden, force: true);

        _stalkerStorage.SaveStorage(repo);
        return true;
    }

    private void OnOpenAttempt(EntityUid uid, Z14PersonalCacheComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!TryGetUserId(args.User, out var userId) || userId.ToString() != component.OwnerUserId)
        {
            args.Cancel();
            if (!args.Silent)
                _popup.PopupEntity(Loc.GetString("z14-personal-cache-not-owner"), args.User, args.User);
            return;
        }

        if (component.Hidden)
        {
            args.Cancel();
            if (!args.Silent)
                _popup.PopupEntity(Loc.GetString("z14-personal-cache-buried"), args.User, args.User);
        }
    }

    private void OnBeforeOpen(EntityUid uid, Z14PersonalCacheComponent component, BeforeActivatableUIOpenEvent args)
    {
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Low, $"Player {Name(args.User):user} opened personal cache {component.CacheId}");
    }

    private void OnGetVerbs(EntityUid uid, Z14PersonalCacheComponent component, GetVerbsEvent<Verb> args)
    {
        if (!TryGetUserId(args.User, out var userId) || userId.ToString() != component.OwnerUserId)
            return;

        var verb = new Verb
        {
            Text = Loc.GetString("z14-personal-cache-remove-verb"),
            Act = () => TryRemoveCache(args.User, uid, component),
        };

        if (!CanRemove(uid, component, args.User, out var message))
        {
            verb.Disabled = true;
            verb.Message = message;
        }

        args.Verbs.Add(verb);
    }

    private bool CanRemove(EntityUid uid, Z14PersonalCacheComponent component, EntityUid user, out string? message)
    {
        message = null;

        if (!TryGetUserId(user, out var userId) || userId.ToString() != component.OwnerUserId)
        {
            message = "z14-personal-cache-not-owner";
            return false;
        }

        if (component.Hidden)
        {
            message = "z14-personal-cache-buried";
            return false;
        }

        if (TryComp<StalkerRepositoryComponent>(uid, out var repo) && repo.ContainedItems.Count > 0)
        {
            message = "z14-personal-cache-not-empty";
            return false;
        }

        return true;
    }

    private void TryRemoveCache(EntityUid user, EntityUid uid, Z14PersonalCacheComponent component)
    {
        if (!CanRemove(uid, component, user, out var message))
        {
            if (message != null)
                _popup.PopupEntity(Loc.GetString(message), user, user);
            return;
        }

        var coordinates = Transform(uid).Coordinates;
        Spawn("Z14PersonalCacheKit", coordinates);
        QueueDel(uid);

        _dbManager.DeleteStalkerPersonalCacheAsync(Guid.Parse(component.CacheId)).GetAwaiter().GetResult();
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Low, $"Player {Name(user):user} removed personal cache {component.CacheId}");
        _popup.PopupEntity(Loc.GetString("z14-personal-cache-removed"), user, user);
    }

    #endregion

    #region Persistence

    private async void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        try
        {
            var mapUid = _mapManager.GetMapEntityId(ev.Map);
            var mapKey = GetMapKey(mapUid, ev.GameMap.ID);
            var caches = await _dbManager.GetStalkerPersonalCachesByMapKeyAsync(mapKey);

            foreach (var db in caches)
            {
                var mapCoords = new MapCoordinates(new Vector2(db.X, db.Y), ev.Map);
                if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var gridComp))
                    continue;

                var tile = _map.TileIndicesFor(gridUid, gridComp, mapCoords);
                if (!TryFindValidTile(gridUid, gridComp, tile, out var validTile, RespawnSearchRadius))
                    continue;

                SpawnCache(gridUid, gridComp, validTile, mapUid, db.UserId, db, db.MapKey);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load Z14 personal caches: {ex}");
        }
    }

    #endregion

    #region Helpers

    private bool TryFindValidTile(EntityUid gridUid, MapGridComponent gridComp, Vector2i center, out Vector2i valid, int maxRadius = 0)
    {
        for (var r = 0; r <= maxRadius; r++)
        {
            for (var x = -r; x <= r; x++)
            {
                for (var y = -r; y <= r; y++)
                {
                    if (Math.Abs(x) != r && Math.Abs(y) != r)
                        continue;

                    var candidate = new Vector2i(center.X + x, center.Y + y);
                    if (_map.TryGetTileRef(gridUid, gridComp, candidate, out var tileRef) && !tileRef.Tile.IsEmpty)
                    {
                        valid = candidate;
                        return true;
                    }
                }
            }
        }

        valid = Vector2i.Zero;
        return false;
    }

    private string GetMapKey(EntityUid mapUid, string? fallback = null)
    {
        if (TryComp<STMapKeyComponent>(mapUid, out var mapKey) && !string.IsNullOrEmpty(mapKey.Value))
            return mapKey.Value;

        return fallback ?? mapUid.ToString();
    }

    private bool IsSafeZone(EntityUid uid)
    {
        return uid.IsValid() && HasComp<StalkerSafeZoneComponent>(uid);
    }

    private bool TryGetUserId(EntityUid user, out Guid userId)
    {
        userId = Guid.Empty;
        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        userId = actor.PlayerSession.UserId.UserId;
        return true;
    }

    private bool HasShovel(EntityUid used)
    {
        return HasComp<ShovelComponent>(used) || HasComp<StalkerShovelComponent>(used);
    }

    #endregion

    #region Admin API

    public void DeleteCache(Guid cacheId, ICommonSession? admin)
    {
        var db = _dbManager.GetStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        if (db == null)
            return;

        DeleteLiveCache(cacheId);
        _dbManager.DeleteStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Medium, $"Admin {admin?.Name ?? "Console"} cleared personal cache {cacheId}");
    }

    public void DeleteCachesByUser(Guid userId, ICommonSession? admin)
    {
        var caches = _dbManager.GetStalkerPersonalCachesByUserAsync(userId).GetAwaiter().GetResult();
        foreach (var db in caches)
        {
            DeleteLiveCache(db.CacheId);
        }

        _dbManager.DeleteStalkerPersonalCachesByUserAsync(userId).GetAwaiter().GetResult();
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Medium, $"Admin {admin?.Name ?? "Console"} cleared all personal caches for user {userId}");
    }

    public MapCoordinates? GetCacheMapCoordinates(Guid cacheId)
    {
        var db = _dbManager.GetStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        if (db == null)
            return null;

        var liveUid = FindLiveCache(cacheId);
        if (liveUid is { } uid && Exists(uid))
            return Transform(uid).MapPosition;

        foreach (var mapId in _mapManager.GetAllMapIds())
        {
            var mapUid = _mapManager.GetMapEntityId(mapId);
            if (TryComp<STMapKeyComponent>(mapUid, out var mapKey) && mapKey.Value == db.MapKey)
                return new MapCoordinates(new Vector2(db.X, db.Y), mapId);
        }

        return null;
    }

    public void TeleportToCache(Guid cacheId, EntityUid admin, ICommonSession? adminSession)
    {
        var coords = GetCacheMapCoordinates(cacheId);
        if (coords is not { } mapCoords)
            return;

        _xforms.SetMapCoordinates(admin, mapCoords);
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Medium, $"Admin {adminSession?.Name ?? "Console"} teleported to personal cache {cacheId}");
    }

    public int RecoverCacheContents(Guid cacheId, EntityUid recipient, ICommonSession? admin)
    {
        var db = _dbManager.GetStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        if (db == null)
            return 0;

        var liveUid = FindLiveCache(cacheId);
        StalkerRepositoryComponent? repo = null;
        var tempUid = EntityUid.Invalid;

        if (liveUid is { } uid && Exists(uid) && TryComp<StalkerRepositoryComponent>(uid, out var liveRepo))
        {
            repo = liveRepo;
        }
        else
        {
            repo = LoadRepositoryItemsFromDb(db.ContentsJson);
            tempUid = repo.Owner;
        }

        if (repo == null)
            return 0;

        var coords = Transform(recipient).Coordinates;
        var count = 0;

        foreach (var item in repo.ContainedItems.ToList())
        {
            if (string.IsNullOrEmpty(item.ProductEntity))
                continue;

            var amount = item.Count;
            while (amount > 0)
            {
                var spawned = Spawn(item.ProductEntity, coords);
                if (item.SStorageData is IItemStalkerStorage iss)
                    _stalkerStorage.SpawnedItem(spawned, iss);

                count++;
                amount--;
            }
        }

        if (tempUid.IsValid())
            QueueDel(tempUid);

        DeleteLiveCache(cacheId);
        _dbManager.DeleteStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        _adminLogger.Add(LogType.Z14PersonalCache, LogImpact.Medium, $"Admin {admin?.Name ?? "Console"} recovered {count} items from personal cache {cacheId}");

        return count;
    }

    private StalkerRepositoryComponent LoadRepositoryItemsFromDb(string contentsJson)
    {
        var temp = EntityManager.CreateEntityUninitialized(null);
        EntityManager.InitializeEntity(temp);

        var repo = EnsureComp<StalkerRepositoryComponent>(temp);
        repo.LoadedDbJson = contentsJson;
        _stalkerStorage.LoadStalkerItemsByEntityUid(temp);

        return repo;
    }

    private EntityUid? FindLiveCache(Guid cacheId)
    {
        var query = EntityQueryEnumerator<Z14PersonalCacheComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CacheId == cacheId.ToString())
                return uid;
        }

        return null;
    }

    private void DeleteLiveCache(Guid cacheId)
    {
        var uid = FindLiveCache(cacheId);
        if (uid is { } u && Exists(u))
            QueueDel(u);
    }

    #endregion
}
