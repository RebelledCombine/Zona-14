// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Content.Server.CartridgeLoader;
using Content.Server.Database;
using Content.Shared._Zona14.PersonalCache;
using Content.Shared.CartridgeLoader;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;


namespace Content.Server._Zona14.PersonalCache;

/// <summary>
/// Server-side PDA cartridge that sends the owner's personal cache list to the UI.
/// </summary>
public sealed class Z14PersonalCacheCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Z14PersonalCacheCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, Z14PersonalCacheCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var userId = ResolveViewerUserId(args.Loader);
        if (userId == Guid.Empty)
            return;

        var caches = _dbManager.GetStalkerPersonalCachesByUserAsync(userId).GetAwaiter().GetResult();
        var entries = new List<Z14PersonalCacheUiEntry>(caches.Count);

        foreach (var db in caches)
        {
            entries.Add(new Z14PersonalCacheUiEntry
            {
                MapKey = db.MapKey,
                X = db.X,
                Y = db.Y,
                Hidden = db.Hidden,
                Weight = db.CurrentWeight,
            });
        }

        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, new Z14PersonalCacheUiState { Caches = entries });
    }

    private Guid ResolveViewerUserId(EntityUid loaderUid)
    {
        if (!TryComp<TransformComponent>(loaderUid, out var xform))
            return Guid.Empty;

        var holder = xform.ParentUid;
        if (!holder.IsValid())
            return Guid.Empty;

        if (!TryComp<ActorComponent>(holder, out var actor))
            return Guid.Empty;

        return actor.PlayerSession.UserId.UserId;
    }
}
