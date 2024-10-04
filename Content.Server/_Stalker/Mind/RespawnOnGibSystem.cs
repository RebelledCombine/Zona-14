using Content.Server.Body.Components;
using Content.Shared.Mind;
using Robust.Server.Console;
using Robust.Shared.Player;

namespace Content.Server._Stalker.Mind;

/// <summary>
/// This handles respawning stalker when it gibbed
/// </summary>
public sealed class RespawnOnGibSystem : EntitySystem
{
    [Dependency] private readonly IServerConsoleHost _consoleHost = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RespawnOnGibComponent, BeingGibbedEvent>(OnBeforeGib);
    }

    private void OnBeforeGib(EntityUid uid, RespawnOnGibComponent component, BeingGibbedEvent args)
    {
        _consoleHost.ExecuteCommand(args.Session, "respawn");
    }
}
