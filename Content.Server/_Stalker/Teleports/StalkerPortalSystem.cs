using System.Linq;
using System.Numerics;
using Content.Server._Stalker.Sponsors;
using Content.Server._Stalker.StalkerDB;
using Content.Server._Stalker.Storage;
using Content.Shared._Stalker.StalkerRepository;
using Content.Shared._Stalker.Teleport;
using Content.Shared.Access.Systems;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server._Stalker.Trash;
using Content.Shared.Buckle.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.EntitySerialization.Systems;
using SponsorSystem = Content.Server._Stalker.Sponsors.System.SponsorSystem;


namespace Content.Server._Stalker.Teleports;

public sealed class StalkerPortalSystem : SharedTeleportSystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly StalkerDbSystem _stalkerDbSystem = default!;
    [Dependency] private readonly StalkerStorageSystem _stalkerStorageSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly SponsorSystem _sponsorSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float StashPortalCooldownTime = 5f;
    private ISawmill _sawmill = default!;


    //Path to the stalker arena map // Zona14: translated comment
    public const string ArenaMapPath = "/Maps/_StalkerMaps/PersonalStalkerArena/StalkerMap.yml";
    public Dictionary<NetUserId, EntityUid> ArenaMap { get; } = new();
    public Dictionary<NetUserId, EntityUid?> ArenaGrid { get; } = new();

    //List of player stalker arenas (map and grid data where the stalker arena is located) // Zona14: translated comment
    public List<StalkerArenaData> StalkerArenaDataList = new(0);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StalkerPortalComponent, StartCollideEvent>(OnCollideStalkerPortal);
        SubscribeLocalEvent<StalkerPortalPersonalComponent, StartCollideEvent>(OnCollideStalkerPortalPersonal);


        SubscribeLocalEvent<StalkerPortalComponent, GetVerbsEvent<InteractionVerb>>(OnInteractStalkerPortal);
        SubscribeLocalEvent<StalkerPortalPersonalComponent, GetVerbsEvent<InteractionVerb>>(OnInteractStalkerPortalPersonal);

        SubscribeLocalEvent<RequestClearArenaGridsEvent>(OnClearArenaGrids);
        _sawmill = Logger.GetSawmill("repository");
    }

    private void OnInteractStalkerPortal(EntityUid uid, StalkerPortalComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => HandleStalkerPortals(uid, component, args.User, args.Target),
            Text = Loc.GetString("Enter")
        });
    }

    // When colliding with a portal outside the stalker arena, teleport to the stalker arena // Zona14: translated comment
    private void OnCollideStalkerPortal(EntityUid uid, StalkerPortalComponent component, ref StartCollideEvent args)
    {
        HandleStalkerPortals(uid, component, args.OtherEntity, args.OurEntity);
    }

    private void HandleStalkerPortals(EntityUid uid, StalkerPortalComponent component, EntityUid otherEntity, EntityUid ourEntity)
    {
        if (!TryComp(otherEntity, out ActorComponent? actor))
            return;

         // Check cooldown first
        if (TryComp<PortalTimeoutComponent>(otherEntity, out var existingTimeout))
        {
            if (existingTimeout.Cooldown != null && existingTimeout.Cooldown > _timing.CurTime)
                return; // Still in cooldown

            // Existing back-teleport prevention logic
            if (existingTimeout.EnteredPortal != ourEntity)
                RemCompDeferred<PortalTimeoutComponent>(otherEntity);
        }

        // Check for access
        if (!component.AllowAll)
        {
            if (!_accessReaderSystem.IsAllowed(otherEntity, ourEntity))
                return;
        }

        var player = actor.PlayerSession;
        var (mapUid, gridUid) = StalkerAssertArenaLoaded(player, component.PortalName, uid);

        // Set cooldown before teleport
        var timeout = EnsureComp<PortalTimeoutComponent>(otherEntity);
        timeout.EnteredPortal = ourEntity;
        timeout.Cooldown = _timing.CurTime + TimeSpan.FromSeconds(StashPortalCooldownTime);
        Dirty(otherEntity, timeout);

        TeleportEntity(otherEntity, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
    }
    private void OnInteractStalkerPortalPersonal(EntityUid uid, StalkerPortalPersonalComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => HandleStalkerPortalPersonal(uid, component, args.User, args.Target),
            Text = Loc.GetString("Enter")
        });
    }

    private void OnClearArenaGrids(RequestClearArenaGridsEvent args)
    {
        for (var i = StalkerArenaDataList.Count - 1; i >= 0; i--)
        {
            var data = StalkerArenaDataList[i];
            var gridIdNet = NetEntity.Parse(data.GridId.ToString());

            if (!_ent.TryGetEntity(gridIdNet, out var gridId) || !_ent.HasComponent<MapGridComponent>(gridId))
                continue;

            if (!TryComp<TransformComponent>(gridId, out var transform))
                continue;

            if (GridHasActiveMind(transform))
                continue;

            _ent.QueueDeleteEntity(gridId);
            StalkerArenaDataList.RemoveAt(i);
        }
    }

    private bool GridHasActiveMind(TransformComponent transform)
    {
        var enumerator = transform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            // checking if the entity has a mind
            if (TryComp<MindContainerComponent>(child, out var mind) && mind.HasMind)
                return true;

            // checking objects buckled to the entity on the grid
            if (TryComp<StrapComponent>(child, out var strap))
            {
                foreach (var buckledEntity in strap.BuckledEntities)
                {
                    if (TryComp<MindContainerComponent>(buckledEntity, out var mindBuckle) && mindBuckle.HasMind)
                        return true;
                }
            }
        }

        return false;
    }



    // When colliding with a teleport in the stalker arena, teleport back to the portal from which the player entered // Zona14: translated comment
    private void OnCollideStalkerPortalPersonal(EntityUid uid, StalkerPortalPersonalComponent component, ref StartCollideEvent args)
    {
        HandleStalkerPortalPersonal(uid, component, args.OtherEntity, args.OurEntity);
    }

    private void HandleStalkerPortalPersonal(EntityUid uid, StalkerPortalPersonalComponent component, EntityUid otherEntity, EntityUid ourEntity)
    {
        if (!TryComp<ActorComponent>(otherEntity, out var actor))
            return;

        // Check cooldown first
        if (TryComp<PortalTimeoutComponent>(otherEntity, out var existingTimeout))
        {
             if (existingTimeout.Cooldown != null && existingTimeout.Cooldown > _timing.CurTime)
                return; // Still in cooldown

            // Existing back-teleport prevention logic
            if (existingTimeout.EnteredPortal != ourEntity)
                RemCompDeferred<PortalTimeoutComponent>(otherEntity);          
        }

        if (component.ReturnPortalEntity.IsValid())
        {

            // Set cooldown before teleport
            var timeout = EnsureComp<PortalTimeoutComponent>(otherEntity);
            timeout.EnteredPortal = ourEntity;
            timeout.Cooldown = _timing.CurTime + TimeSpan.FromSeconds(StashPortalCooldownTime);
            Dirty(otherEntity, timeout);

            TeleportEntity(otherEntity, new EntityCoordinates(component.ReturnPortalEntity, new Vector2(0, -1f)));
        }
    }


    // Create the stalker arena and perform initial setup if not yet created; returns the player's individual arena coordinates // Zona14: translated comment
    public (EntityUid Map, EntityUid? Grid) StalkerAssertArenaLoaded(ICommonSession admin, string teleportName, EntityUid? returnTeleportEntityUid)
    {
        if (InStalkerTeleportDataList(admin.Name) == true)
        {
            var stalkerTeleportData = GetFromStalkerTeleportDataList(admin.Name);

            SetReturnPortal(stalkerTeleportData.GridId,teleportName,returnTeleportEntityUid);

            return (stalkerTeleportData.MapId, stalkerTeleportData.GridId);
        }

        ArenaMap[admin.UserId] = _mapManager.GetMapEntityId(_mapManager.CreateMap());
        _metaDataSystem.SetEntityName(ArenaMap[admin.UserId], $"STALKER_MAP-{admin.Name}");

        var map = Comp<MapComponent>(ArenaMap[admin.UserId]);

        var grids = _mapManager.GetAllMapGrids(map.MapId).Select(mc => mc.Owner).ToList();
        if (grids.Count != 0)
        {
            _metaDataSystem.SetEntityName(grids[0], $"STALKER_GRID-{admin.Name}");
            ArenaGrid[admin.UserId] = grids[0];
        }
        else
        {
            ArenaGrid[admin.UserId] = null;
        }

        if (TryComp(grids[0], out TransformComponent? xform))
        {
            var enumerator = xform.ChildEnumerator;
            while (enumerator.MoveNext(out var entity))
            {
                /*
                if (TryComp(entity, out StoreComponent? storeComponent))
                {
                    storeComponent.Balance["Roubles"] = _stalkerDbSystem.GetMoney(admin.Name);
                }
                */

                if (!TryComp(entity, out StalkerRepositoryComponent? stalkerRepositoryComponent))
                    continue;

                stalkerRepositoryComponent.StorageOwner = admin.Name;
                stalkerRepositoryComponent.LoadedDbJson = _stalkerDbSystem.GetInventoryJson(admin.Name);
                _stalkerStorageSystem.LoadStalkerItemsByEntityUid(entity);
                var ev = new RepositoryAdminSetEvent(GetNetEntity(entity), admin.Name);
                RaiseLocalEvent(entity, ev);

                // Sponsors
                stalkerRepositoryComponent.MaxWeight =
                    _sponsorSystem.GetRepositoryWeight(admin.UserId, stalkerRepositoryComponent.MaxWeight);
                break;
            }
        }

        StalkerArenaDataList.Add(new StalkerArenaData(admin.Name, ArenaMap[admin.UserId], ArenaGrid[admin.UserId]));

        SetReturnPortal(ArenaGrid[admin.UserId],teleportName,returnTeleportEntityUid);

        return (ArenaMap[admin.UserId], ArenaGrid[admin.UserId]);
    }



    //Assign to the personal teleport (in the stalker arena) the entity ID of the teleport to return to // Zona14: translated comment
    public void SetReturnPortal(EntityUid? teleport, string teleportName, EntityUid? returnTeleportEntityUid)
    {
        if (!TryComp(teleport, out TransformComponent? transformComponent))
            return;

        var enumerator = transformComponent.ChildEnumerator;
        while (enumerator.MoveNext(out var entity))
        {
            if (!entity.IsValid())
                continue;

            if (!TryComp(entity, out StalkerPortalPersonalComponent? portalPersonalComponent))
                continue;

            portalPersonalComponent.ReturnPortal = teleportName;
            if (returnTeleportEntityUid != null)
            {
                portalPersonalComponent.ReturnPortalEntity = (EntityUid) returnTeleportEntityUid;
            }
        }
    }

    //Check by player login whether a stalker arena exists in the list // Zona14: translated comment
    public bool InStalkerTeleportDataList(string inputLogin)
    {
        foreach (var data in StalkerArenaDataList)
        {
            if (data.Login == inputLogin)
            {
                return true;
            }
        }
        return false;
    }

    //Return stalker arena data // Zona14: translated comment
    public StalkerArenaData GetFromStalkerTeleportDataList(string inputLogin)
    {
        foreach (var data in StalkerArenaDataList)
        {
            if (data.Login == inputLogin)
            {
                return data;
            }
        }
        return null!;
    }


    //Stalker arena data // Zona14: translated comment
    public sealed class StalkerArenaData
    {
        //Player login // Zona14: translated comment
        public string Login;
        //Map ID where the stalker arena is located // Zona14: translated comment
        public EntityUid MapId;
        //Grid ID where the stalker arena is located // Zona14: translated comment
        public EntityUid? GridId;

        public StalkerArenaData(string login, EntityUid mapId, EntityUid? gridId)
        {
            Login = login;
            MapId = mapId;
            GridId = gridId;
        }
    }
}
[Serializable]
public sealed class RepositoryAdminSetEvent : EntityEventArgs
{
    public NetEntity Repository;
    public string Admin;

    public RepositoryAdminSetEvent(NetEntity repository, string admin)
    {
        Repository = repository;
        Admin = admin;
    }
}
