// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Content.Server._Zona14.PersonalCache;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14ListCachesCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override string Command => "z14listcaches";
    public override string Description => "List personal caches for a user or all users.";
    public override string Help => "z14listcaches [userId|all]";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        List<StalkerPersonalCache> caches;
        if (args.Length == 0 || args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            caches = _dbManager.GetAllStalkerPersonalCachesAsync().GetAwaiter().GetResult();
        }
        else if (Guid.TryParse(args[0], out var userId))
        {
            caches = _dbManager.GetStalkerPersonalCachesByUserAsync(userId).GetAwaiter().GetResult();
        }
        else
        {
            shell.WriteError("Invalid user ID.");
            return;
        }

        if (caches.Count == 0)
        {
            shell.WriteLine("No caches found.");
            return;
        }

        foreach (var c in caches)
        {
            shell.WriteLine($"{c.CacheId}: owner {c.UserId}, map {c.MapKey}, pos ({c.X:F1}, {c.Y:F1}), hidden {c.Hidden}, weight {c.CurrentWeight:F1} kg");
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14CacheInfoCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override string Command => "z14cacheinfo";
    public override string Description => "Show detailed information about a personal cache.";
    public override string Help => "z14cacheinfo <cacheId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !Guid.TryParse(args[0], out var cacheId))
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var db = _dbManager.GetStalkerPersonalCacheAsync(cacheId).GetAwaiter().GetResult();
        if (db == null)
        {
            shell.WriteError("Cache not found.");
            return;
        }

        shell.WriteLine($"Cache {db.CacheId}: owner {db.UserId}, map {db.MapKey}, pos ({db.X:F1}, {db.Y:F1}, {db.Z:F1}), hidden {db.Hidden}, weight {db.CurrentWeight:F1} kg");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14Tp2CacheCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "z14tp2cache";
    public override string Description => "Teleport to a personal cache.";
    public override string Help => "z14tp2cache <cacheId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !Guid.TryParse(args[0], out var cacheId))
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var admin = shell.Player;
        if (admin?.AttachedEntity is not { } entity)
        {
            shell.WriteError("You must have an attached entity to teleport.");
            return;
        }

        _entitySystemManager.GetEntitySystem<Z14PersonalCacheSystem>().TeleportToCache(cacheId, entity, admin);
        shell.WriteLine("Teleported to cache.");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14ClearCacheCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "z14clearcache";
    public override string Description => "Delete a personal cache and its contents.";
    public override string Help => "z14clearcache <cacheId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !Guid.TryParse(args[0], out var cacheId))
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        _entitySystemManager.GetEntitySystem<Z14PersonalCacheSystem>().DeleteCache(cacheId, shell.Player);
        shell.WriteLine("Cache cleared.");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14ClearCachesCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "z14clearcaches";
    public override string Description => "Delete all personal caches belonging to a user.";
    public override string Help => "z14clearcaches <userId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !Guid.TryParse(args[0], out var userId))
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        _entitySystemManager.GetEntitySystem<Z14PersonalCacheSystem>().DeleteCachesByUser(userId, shell.Player);
        shell.WriteLine("All caches for user cleared.");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14RecoverCacheCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "z14recovercache";
    public override string Description => "Drop a personal cache's contents at your feet.";
    public override string Help => "z14recovercache <cacheId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !Guid.TryParse(args[0], out var cacheId))
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var admin = shell.Player;
        if (admin?.AttachedEntity is not { } entity)
        {
            shell.WriteError("You must have an attached entity to recover items.");
            return;
        }

        var count = _entitySystemManager.GetEntitySystem<Z14PersonalCacheSystem>().RecoverCacheContents(cacheId, entity, admin);
        shell.WriteLine(count > 0 ? $"Recovered {count} items." : "No items to recover.");
    }
}
