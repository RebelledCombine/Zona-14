# Zona-14 DoAfter Busy Animations

This client-only system replaces the generic `DoAfter` progress bar above a player's head with a short themed animation when the action can be identified.

## Supported actions

| Animation | Trigger | Sprite / RSI |
| --- | --- | --- |
| `GunChamber` | Drawing, holstering, or pulling a gun into hands. | `/Textures/_Zona14/Interface/Misc/revolver_chamber.rsi` (7 frames, spins in reverse when holstering). |
| `Armor` | Equipping or unequipping an outer clothing item (`SlotFlags.OUTERCLOTHING`). | `/Textures/_Zona14/Interface/Misc/doafter_animations.rsi` state `armor_*` (7 frames). |
| `Clothing` | Equipping or unequipping any other `ClothingComponent` item. | `doafter_animations.rsi` state `clothing_*`. |
| `Eat` | Eating food (`EdiblePrototype` == `Food`). | `doafter_animations.rsi` state `eat_*`. |
| `Drink` | Drinking (`EdiblePrototype` == `Drink`). | `doafter_animations.rsi` state `drink_*`. |
| `Pill` | Taking any other edible (pills, etc.). | `doafter_animations.rsi` state `pill_*`. |
| `AmmoFill` | Loading rounds into a magazine/ammo box (`AmmoFillDoAfterEvent` on a `BallisticAmmoProviderComponent`). | `doafter_animations.rsi` state `ammo_*`. |

## How it works

`DoAfterOverlay` (upstream file, marked with `// Zona14:` comments) calls `DoAfterAnimationSystem.TryDrawAnimation` for each active `DoAfter`. If a themed animation is found and its sprite frames load successfully, the overlay draws it and skips the generic progress bar for that `DoAfter`. Unknown actions fall back to the default bar.

## Adding a new animation

1. Add a value to `DoAfterAnimationType`.
2. Register the animation data in `DoAfterAnimationSystem.Initialize`:
   ```csharp
   _data[DoAfterAnimationType.MyAction] = new AnimationData(
       new ResPath("/Textures/_Zona14/Interface/Misc/doafter_animations.rsi"),
       StatePrefix: "myaction_",
       FrameCount: 7,
       Scale: 0.5f,
       SpinSpeed: 0f);
   ```
3. Add a `TryResolveAnimation` branch that returns your type, the relevant entity, and whether the animation should run in reverse (e.g. holstering).
4. Add the 7-frame sprite states `myaction_0` through `myaction_6` to the RSI with a `meta.json` containing `license` and `copyright`.

## Files

- `Content.Client/_Zona14/DoAfterAnimations/DoAfterAnimationSystem.cs`
- `Content.Client/_Zona14/DoAfterAnimations/DoAfterAnimationType.cs`
- `Content.Client/DoAfter/DoAfterOverlay.cs` (upstream edit with `// Zona14:` markers)
- `Resources/Textures/_Zona14/Interface/Misc/doafter_animations.rsi/`
