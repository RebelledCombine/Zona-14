using System.Collections.Generic;
using Content.Client._Starfall.Particles;
using Content.Shared._Zona14.Airborne;

namespace Content.Client._Zona14.Airborne;

/// <summary>
///     Zona14: renders the constant particle haze for anomalies carrying <see cref="Z14AnomalyHazeComponent"/>.
///     Each anomaly gets a continuous emitter attached to it. A throttled poll keeps exactly one live emitter
///     per in-view anomaly and drops emitters for anomalies that left view — this fixes the emitter vanishing
///     (but the damage continuing) after you walk away and come back: on re-entry the component start doesn't
///     reliably re-fire, so we re-spawn from the poll instead. Visual only; damage is server-side AirborneHazard.
/// </summary>
public sealed class Z14AnomalyHazeSystem : EntitySystem
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float PollInterval = 0.5f;
    private float _accumulator;

    private readonly Dictionary<EntityUid, ActiveEmitter> _active = new();
    private readonly HashSet<EntityUid> _seen = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Z14AnomalyHazeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<Z14AnomalyHazeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<Z14AnomalyHazeComponent> ent, ref ComponentStartup args)
    {
        Ensure(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<Z14AnomalyHazeComponent> ent, ref ComponentShutdown args)
    {
        Stop(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < PollInterval)
            return;
        _accumulator = 0f;

        _seen.Clear();

        // Ensure every in-view haze anomaly (the client only has PVS-visible ones) has a live emitter.
        var query = EntityQueryEnumerator<Z14AnomalyHazeComponent>();
        while (query.MoveNext(out var uid, out var haze))
        {
            _seen.Add(uid);
            Ensure(uid, haze);
        }

        // Stop emitters whose anomaly is no longer present (left view / deleted).
        _toRemove.Clear();
        foreach (var (uid, emitter) in _active)
        {
            if (!_seen.Contains(uid))
            {
                emitter.Exhausted = true;
                _toRemove.Add(uid);
            }
        }

        foreach (var uid in _toRemove)
            _active.Remove(uid);
    }

    private void Ensure(EntityUid uid, Z14AnomalyHazeComponent haze)
    {
        if (_active.TryGetValue(uid, out var existing) && !existing.Exhausted)
            return;

        var coords = _transform.GetMapCoordinates(uid);
        var emitter = _particles.SpawnEffect(haze.Effect, coords, uid);
        if (emitter != null)
            _active[uid] = emitter;
    }

    private void Stop(EntityUid uid)
    {
        if (_active.Remove(uid, out var emitter))
            emitter.Exhausted = true;
    }
}
