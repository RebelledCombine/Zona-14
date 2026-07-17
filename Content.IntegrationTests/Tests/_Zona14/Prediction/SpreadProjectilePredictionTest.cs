// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Zona14.Weapons.Ranged.Prediction;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Zona14.Prediction;

// Every pellet a spread shell fires must carry PredictedProjectileServerComponent: the shooter's
// client only hides server twins that are tagged, so an untagged pellet renders over its predicted
// twin (double tracers).
[TestFixture]
public sealed class SpreadProjectilePredictionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    // Arbitrary — spread handling is ammo-side, not weapon-side.
    private static readonly EntProtoId Gun = "WeaponSniperMosin";

    // 6 = PelletShotgunSpread's ProjectileSpread count in shotgun.yml.
    private static readonly EntProtoId Shell = "ShellShotgun";
    private const int PelletCount = 6;

    [Test]
    public async Task EverySpreadPelletIsTaggedForPredictionHiding()
    {
        await PlaceInHands(Gun);
        await Pair.RunSeconds(1f);

        EntityUid gunUid = default;
        GunComponent gunComp = default!;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SGun.TryGetGun(SPlayer, out gunUid, out gunComp!), Is.True, "Player not holding a gun");
        });

        await Server.WaitAssertion(() =>
        {
            var from = SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates;
            var to = from.Offset(new Vector2(0f, 5f));
            var shell = SEntMan.SpawnEntity(Shell, from);

            // Stands in for the client's per-pellet predicted ids; tagging needs one per pellet.
            var predictedIds = new List<int> { 1, 2, 3, 4, 5, 6 };

            // Inspect in the same tick — high-velocity pellets can collide and queue-delete on the
            // next physics step.
            var spawned = SGun.Shoot(gunUid, gunComp, shell, from, to, out _, SPlayer,
                predictedProjectiles: predictedIds, userSession: ServerSession);

            Assert.That(spawned, Has.Count.EqualTo(PelletCount),
                $"Shoot should return all {PelletCount} pellets so the client sends a predicted id " +
                $"per pellet; returned {spawned?.Count ?? 0}");

            var pellets = 0;
            var tagged = 0;
            var query = SEntMan.EntityQueryEnumerator<ProjectileComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                pellets++;
                if (SEntMan.HasComponent<PredictedProjectileServerComponent>(uid))
                    tagged++;
            }

            Assert.That(pellets, Is.EqualTo(PelletCount), "buckshot shell should spawn 6 pellets");
            Assert.That(tagged, Is.EqualTo(pellets),
                $"every server pellet must carry PredictedProjectileServerComponent so its client twin is hidden; " +
                $"only {tagged}/{pellets} were tagged — the untagged pellets render on top of the shooter's " +
                $"predicted pellets (double tracers)");
        });
    }
}
