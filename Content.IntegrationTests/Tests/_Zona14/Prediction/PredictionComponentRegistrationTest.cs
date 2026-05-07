// SPDX-License-Identifier: MIT
using Content.Shared._Zona14.Weapons.Ranged.Prediction;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Zona14.Prediction;

[TestFixture]
public sealed class PredictionComponentRegistrationTest
{
    [Test]
    public async Task PredictionComponentsRegistered()
    {
        await using var pair = await PoolManager.GetServerClient();
        var compFactory = pair.Server.ResolveDependency<IComponentFactory>();

        Assert.Multiple(() =>
        {
            Assert.That(compFactory.GetRegistration<IgnorePredictionHideComponent>(), Is.Not.Null);
            Assert.That(compFactory.GetRegistration<IgnorePredictionHitComponent>(), Is.Not.Null);
            Assert.That(compFactory.GetRegistration<GunIgnorePredictionComponent>(), Is.Not.Null);
            Assert.That(compFactory.GetRegistration<PredictedProjectileServerComponent>(), Is.Not.Null);
            Assert.That(compFactory.GetRegistration<PredictedProjectileClientComponent>(), Is.Not.Null);
            Assert.That(compFactory.GetRegistration<PredictedProjectileHitComponent>(), Is.Not.Null);
        });

        await pair.CleanReturnAsync();
    }
}
