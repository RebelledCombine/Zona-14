using System.Numerics;
using Content.Shared._Stalker.Anomaly;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Stalker.Anomaly;

public sealed class STAnomalyTipsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly EntityLookupSystem _lookup;

    public STAnomalyTipsOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var viewport = args.WorldAABB;

        var handle = args.WorldHandle;

        const float scale = 1f;

        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3x2.CreateRotation(-(float)rotation.Theta);

        var entities = _entity.EntityQueryEnumerator<STAnomalyTipsComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out var tips, out var xform))
        {
            if (xform.MapID != eye?.Position.MapId)
                continue;

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3x2.CreateTranslation(worldPosition);

            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);

            handle.SetTransform(matty);

            var texture = _sprite.GetFrame(tips.Icon, TimeSpan.Zero);
            handle.DrawTexture(texture, Vector2.Zero);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
