// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Zona14.DoAfterAnimations;

/// <summary>
/// Zona14: draws a small themed busy-animation above a player while a DoAfter is running.
/// The overlay delegates here so each action (drawing a gun, equipping armor, eating, etc.)
/// can show its own icon instead of the generic progress bar.
/// </summary>
public sealed class DoAfterAnimationSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private const float FlashTime = 0.125f;

    private readonly Dictionary<DoAfterAnimationType, AnimationData> _data = new();
    private readonly Dictionary<DoAfterAnimationType, Texture[]?> _frames = new();

    public override void Initialize()
    {
        base.Initialize();

        _data[DoAfterAnimationType.GunChamber] = new AnimationData(
            new ResPath("/Textures/_Zona14/Interface/Misc/revolver_chamber.rsi"),
            StatePrefix: null,
            FrameCount: 7,
            Scale: 0.65f,
            SpinSpeed: 4f);

        var genericPath = new ResPath("/Textures/_Zona14/Interface/Misc/doafter_animations.rsi");
        _data[DoAfterAnimationType.Armor] = new AnimationData(genericPath, "armor_", 7, 0.5f, 0f);
        _data[DoAfterAnimationType.Clothing] = new AnimationData(genericPath, "clothing_", 7, 0.5f, 0f);
        _data[DoAfterAnimationType.Eat] = new AnimationData(genericPath, "eat_", 7, 0.5f, 0f);
        _data[DoAfterAnimationType.Drink] = new AnimationData(genericPath, "drink_", 7, 0.5f, 0f);
        _data[DoAfterAnimationType.Pill] = new AnimationData(genericPath, "pill_", 7, 0.5f, 0f);
        _data[DoAfterAnimationType.AmmoFill] = new AnimationData(genericPath, "ammo_", 7, 0.5f, 0f);
    }

    /// <summary>
    /// Tries to draw a themed animation for the given DoAfter. Returns true if it drew one and
    /// the generic progress bar should be skipped.
    /// </summary>
    public bool TryDrawAnimation(
        DrawingHandleWorld handle,
        global::Content.Shared.DoAfter.DoAfter doAfter,
        EntityUid user,
        TimeSpan time,
        Matrix3x2 matty,
        float yOffset,
        ref float offset,
        float alpha,
        float scale)
    {
        if (!TryResolveAnimation(doAfter, user, out var type, out var item, out var reverse))
            return false;

        var frames = GetFrames(type);
        if (frames == null || frames.Length == 0)
            return false;

        var elapsedRatio = GetElapsedRatio(doAfter, time);
        var displayRatio = reverse ? 1f - elapsedRatio : elapsedRatio;

        var lastFrame = frames.Length - 1;
        var frameIndex = Math.Clamp((int)MathF.Round(displayRatio * lastFrame), 0, lastFrame);
        var texture = frames[frameIndex];

        var animAlpha = GetAnimationAlpha(doAfter, time, alpha);
        var drawColor = Color.White.WithAlpha(animAlpha);

        var nativeSize = new Vector2(texture.Width, texture.Height) / EyeManager.PixelsPerMeter;
        var drawnSize = nativeSize * _data[type].Scale;
        var centre = new Vector2(0f,
            yOffset / scale + offset / EyeManager.PixelsPerMeter * scale + drawnSize.Y / 2f);

        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(_data[type].Scale));
        var spinDir = reverse ? -1f : 1f;
        var spin = Matrix3Helpers.CreateRotation(new Angle(time.TotalSeconds * _data[type].SpinSpeed * spinDir));
        var toCentre = Matrix3Helpers.CreateTranslation(centre);
        var local = Matrix3x2.Multiply(Matrix3x2.Multiply(scaleMatrix, spin), toCentre);
        var world = Matrix3x2.Multiply(local, matty);

        handle.SetTransform(world);
        handle.DrawTexture(texture, -nativeSize / 2f, drawColor);
        handle.SetTransform(matty);

        offset += texture.Height * _data[type].Scale / scale;
        return true;
    }

    private bool TryResolveAnimation(global::Content.Shared.DoAfter.DoAfter doAfter, EntityUid user, out DoAfterAnimationType type, out EntityUid item, out bool reverse)
    {
        var args = doAfter.Args;

        // Drawing, holstering, or pulling a gun into hands.
        if (args.Used is { } used && HasComp<GunComponent>(used))
        {
            type = DoAfterAnimationType.GunChamber;
            item = used;
            reverse = _hands.IsHolding(user, used);
            return true;
        }

        // Equipping/unequipping any clothing item (armor, clothes, toggleables, pulls/pickups).
        if (args.Used is { } usedClothing && TryComp<ClothingComponent>(usedClothing, out var clothing))
        {
            type = (clothing.Slots & SlotFlags.OUTERCLOTHING) != 0
                ? DoAfterAnimationType.Armor
                : DoAfterAnimationType.Clothing;
            item = usedClothing;
            reverse = !_hands.IsHolding(user, usedClothing);
            return true;
        }

        // Loading rounds into a magazine/ammo box.
        if (args.Event is AmmoFillDoAfterEvent && args.Target is { } target && HasComp<BallisticAmmoProviderComponent>(target))
        {
            type = DoAfterAnimationType.AmmoFill;
            item = target;
            reverse = false;
            return true;
        }

        // Eating, drinking, or taking a pill.
        if (args.Event is EatingDoAfterEvent && args.Target is { } food && TryComp<EdibleComponent>(food, out var edible))
        {
            type = GetEdibleAnimationType(edible.Edible);
            item = food;
            reverse = false;
            return true;
        }

        type = default;
        item = EntityUid.Invalid;
        reverse = false;
        return false;
    }

    private DoAfterAnimationType GetEdibleAnimationType(ProtoId<EdiblePrototype> edible)
    {
        if (edible == IngestionSystem.Food)
            return DoAfterAnimationType.Eat;
        if (edible == IngestionSystem.Drink)
            return DoAfterAnimationType.Drink;
        return DoAfterAnimationType.Pill;
    }

    private Texture[]? GetFrames(DoAfterAnimationType type)
    {
        if (_frames.TryGetValue(type, out var cached))
            return cached;

        var data = _data[type];
        var frames = TryLoadFrames(data);
        _frames[type] = frames;
        return frames;
    }

    private Texture[]? TryLoadFrames(AnimationData data)
    {
        try
        {
            var frames = new Texture[data.FrameCount];
            for (var i = 0; i < frames.Length; i++)
            {
                var state = data.StatePrefix != null ? $"{data.StatePrefix}{i}" : i.ToString();
                var sprite = new SpriteSpecifier.Rsi(data.RsiPath, state);
                frames[i] = _sprite.Frame0(sprite);
            }

            return frames;
        }
        catch
        {
            return null;
        }
    }

    private static float GetElapsedRatio(global::Content.Shared.DoAfter.DoAfter doAfter, TimeSpan time)
    {
        if (doAfter.Args.Delay <= TimeSpan.Zero)
            return 1f;

        TimeSpan elapsed;
        if (doAfter.CancelledTime != null)
            elapsed = doAfter.CancelledTime.Value - doAfter.StartTime;
        else
            elapsed = time - doAfter.StartTime;

        return (float)Math.Min(1, elapsed.TotalSeconds / doAfter.Args.Delay.TotalSeconds);
    }

    private static float GetAnimationAlpha(global::Content.Shared.DoAfter.DoAfter doAfter, TimeSpan time, float alpha)
    {
        if (doAfter.CancelledTime == null)
            return alpha;

        var cancelElapsed = (time - doAfter.CancelledTime.Value).TotalSeconds;
        var flash = Math.Floor(cancelElapsed / FlashTime) % 2 == 0;
        return flash ? alpha : 0f;
    }

    private readonly record struct AnimationData(ResPath RsiPath, string? StatePrefix, int FrameCount, float Scale, float SpinSpeed);
}
