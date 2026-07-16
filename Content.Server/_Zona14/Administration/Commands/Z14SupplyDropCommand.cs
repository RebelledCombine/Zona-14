// SPDX-License-Identifier: MIT

using Content.Server._Zona14.SupplyDrop;
using Content.Server.Administration;
using Content.Shared._Zona14.SupplyDrop;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class Z14SupplyDropCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "z14supplydrop";
    public override string Description => Loc.GetString("cmd-z14supplydrop-description");
    public override string Help => Loc.GetString("cmd-z14supplydrop-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _entityManager.System<Z14SupplyDropRuleSystem>();

        Z14SupplyDropVariant? variant = null;
        EntityUid? zone = null;

        var argIndex = 0;
        if (argIndex < args.Length && TryParseVariant(args[argIndex], out var parsedVariant))
        {
            variant = parsedVariant;
            argIndex++;
        }

        if (argIndex < args.Length)
        {
            var zoneArg = args[argIndex];
            if (zoneArg.Equals("here", StringComparison.OrdinalIgnoreCase))
            {
                zone = CreateTemporaryZoneAtPlayer(shell);
                if (!zone.HasValue)
                {
                    shell.WriteError(Loc.GetString("cmd-z14supplydrop-error-no-player"));
                    return;
                }
            }
            else if (NetEntity.TryParse(zoneArg, out var netEntity)
                     && _entityManager.TryGetEntity(netEntity, out var ent)
                     && _entityManager.HasComponent<Z14SupplyDropZoneComponent>(ent))
            {
                zone = ent;
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-z14supplydrop-error-invalid-zone", ("zone", zoneArg)));
                return;
            }
        }

        system.Trigger(variant, zone, shell.Player?.AttachedEntity);
    }

    private bool TryParseVariant(string input, out Z14SupplyDropVariant variant)
    {
        return Enum.TryParse<Z14SupplyDropVariant>(input, true, out variant);
    }

    private EntityUid? CreateTemporaryZoneAtPlayer(IConsoleShell shell)
    {
        if (shell.Player is not { } player
            || player.AttachedEntity is not { } attached
            || !_entityManager.TryGetComponent<TransformComponent>(attached, out var xform))
        {
            return null;
        }

        var mapCoords = _entityManager.System<SharedTransformSystem>().GetMapCoordinates(attached, xform);
        var zone = _entityManager.SpawnEntity("Z14SupplyDropZone", mapCoords);
        if (_entityManager.TryGetComponent<Z14SupplyDropZoneComponent>(zone, out var zoneComp))
            zoneComp.DeleteAfterSpawn = true;

        return zone;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = new[] { "helicopter", "truck", "any" };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-z14supplydrop-arg-variant"));
        }

        if (args.Length == 2)
        {
            var options = new[] { "here" };
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-z14supplydrop-arg-zone"));
        }

        return CompletionResult.Empty;
    }
}
