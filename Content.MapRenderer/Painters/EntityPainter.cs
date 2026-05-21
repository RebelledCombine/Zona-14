using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters;

public sealed class EntityPainter
{
    private readonly IResourceManager _resManager;

    private readonly Dictionary<(string path, string state), Image> _images;
    private readonly Image _errorImage;

    private readonly IEntityManager _sEntityManager;
    private readonly SpriteSystem _sprite;

    public EntityPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
    {
        _resManager = client.ResolveDependency<IResourceManager>();

        _sEntityManager = server.ResolveDependency<IEntityManager>();
        _sprite = client.ResolveDependency<IEntityManager>().System<SpriteSystem>();

        _images = new Dictionary<(string path, string state), Image>();
        _errorImage = Image.Load<Rgba32>(_resManager.ContentFileRead("/Textures/error.rsi/error.png"));
    }

    public void Run(Image canvas, List<EntityData> entities, Vector2 customOffset = default)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO cache this shit what are we insane
        entities.Sort(Comparer<EntityData>.Create((x, y) => x.Sprite.DrawDepth.CompareTo(y.Sprite.DrawDepth)));
        var xformSystem = _sEntityManager.System<SharedTransformSystem>();

        foreach (var entity in entities)
        {
            Run(canvas, entity, xformSystem, customOffset);
        }

        Console.WriteLine($"{nameof(EntityPainter)} painted {entities.Count} entities in {(int)stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    public void Run(Image canvas, EntityData entity, SharedTransformSystem xformSystem, Vector2 customOffset = default)
    {
        if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
        {
            return;
        }

        var worldRotation = xformSystem.GetWorldRotation(entity.Owner);
        foreach (var layer in entity.Sprite.AllLayers)
        {
            if (!layer.Visible)
            {
                continue;
            }

            if (!layer.RsiState.IsValid)
            {
                continue;
            }

            var rsi = layer.ActualRsi;
            Image image;

            if (rsi == null || !rsi.TryGetState(layer.RsiState, out var state))
            {
                image = _errorImage;
            }
            else
            {
                var key = (rsi.Path!.ToString(), state.StateId.Name!);

                if (!_images.TryGetValue(key, out image!))
                {
                    var stream = _resManager.ContentFileRead($"{rsi.Path}/{state.StateId}.png");
                    image = Image.Load<Rgba32>(stream);

                    _images[key] = image;
                }
            }

            image = image.CloneAs<Rgba32>();

            (int, int, int, int) GetRsiFrame(RSI? rsi, Image image, EntityData entity, ISpriteLayer layer, int direction)
            {
                if (rsi is null)
                    return (0, 0, EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter);

                var statesX = image.Width / rsi.Size.X;
                var statesY = image.Height / rsi.Size.Y;
                var stateCount = statesX * statesY;
                var frames = stateCount / _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer);
                var target = direction * frames;
                var targetY = target / statesX;
                var targetX = target % statesX;
                return (targetX * rsi.Size.X, targetY * rsi.Size.Y, rsi.Size.X, rsi.Size.Y);
            }

            var dir = _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) switch
            {
                0 => 0,
                _ => (int)layer.EffectiveDirection(worldRotation)
            };

            var (x, y, width, height) = GetRsiFrame(rsi, image, entity, layer, dir);

            var rect = new Rectangle(x, y, width, height);
            if (!new Rectangle(Point.Empty, image.Size).Contains(rect))
            {
                Console.WriteLine($"Invalid layer {rsi!.Path}/{layer.RsiState.Name}.png for entity {_sEntityManager.ToPrettyString(entity.Owner)} at ({entity.X}, {entity.Y})");
                return;
            }

            image.Mutate(o => o.Crop(rect));

            var spriteRotation = 0f;
            if (!entity.Sprite.NoRotation && !entity.Sprite.SnapCardinals && _sprite.LayerGetDirectionCount((SpriteComponent.Layer)layer) == 1)
            {
                spriteRotation = (float)worldRotation.Degrees;
            }

            var colorMix = entity.Sprite.Color * layer.Color;
            var imageColor = Color.FromRgba(colorMix.RByte, colorMix.GByte, colorMix.BByte, colorMix.AByte);
            var coloredImage = new Image<Rgba32>(image.Width, image.Height);
            coloredImage.Mutate(o => o.BackgroundColor(imageColor));

            var (imgX, imgY) = rsi?.Size ?? (EyeManager.PixelsPerMeter, EyeManager.PixelsPerMeter);

            // Zona14: Apply sprite scale so entities with large RSI files but small
            // scale factors (e.g. graffiti 712×712 at scale 0.045) render at the
            // intended size instead of full resolution.
            var scale = entity.Sprite.Scale; // Zona14
            imgX = Math.Max(1, (int)(imgX * scale.X)); // Zona14
            imgY = Math.Max(1, (int)(imgY * scale.Y)); // Zona14

            var spriteOffsetX = (int)(entity.Sprite.Offset.X * EyeManager.PixelsPerMeter); // Zona14: sprite offset separate from canvas offset
            var spriteOffsetY = (int)(entity.Sprite.Offset.Y * EyeManager.PixelsPerMeter); // Zona14: sprite offset separate from canvas offset
            image.Mutate(o => o
                .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1)
                .Resize(imgX, imgY)
                .Flip(FlipMode.Vertical)
                .Rotate(spriteRotation));

            // Zona14: customOffset is now a pixel-space canvas offset computed at render time
            var pointX = (int)(entity.X + customOffset.X) + spriteOffsetX - imgX / 2; // Zona14
            var pointY = (int)(entity.Y + customOffset.Y) + spriteOffsetY - imgY / 2; // Zona14
            canvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), 1));
        }
    }
}
