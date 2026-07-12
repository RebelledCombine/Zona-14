// SPDX-License-Identifier: MIT

using Content.Shared.Examine;
using Content.Shared.Lock;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Shared._Zona14.SupplyDrop;

public sealed class Z14SupplyDropCrateSystem : EntitySystem
{
    private void OnExamined(EntityUid uid, Z14SupplyDropCrateComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<LockComponent>(uid, out var lockComp) && lockComp.Locked)
        {
            var seconds = (int) (lockComp.UnlockTime != TimeSpan.Zero
                ? lockComp.UnlockTime.TotalSeconds
                : lockComp.LockTime.TotalSeconds);

            args.PushMarkup(Loc.GetString("z14-supplydrop-crate-examine", ("seconds", seconds)));
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Z14SupplyDropCrateComponent, ExaminedEvent>(OnExamined);
    }
}
