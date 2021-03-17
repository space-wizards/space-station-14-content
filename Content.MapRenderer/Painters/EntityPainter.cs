﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;
using SpriteComponent = Robust.Client.GameObjects.SpriteComponent;

namespace Content.MapRenderer.Painters
{
    public class EntityPainter
    {
        private readonly IResourceCache _cResourceCache;
        private readonly IEntityManager _cEntityManager;
        private readonly IEntityManager _sEntityManager;

        private readonly Dictionary<(string path, string state), Image> _images;
        private readonly Image _errorImage;

        private readonly ConcurrentDictionary<GridId, List<EntityData>> _entities;

        public EntityPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _cResourceCache = client.ResolveDependency<IResourceCache>();
            _cEntityManager = client.ResolveDependency<IEntityManager>();
            _sEntityManager = server.ResolveDependency<IEntityManager>();

            _errorImage = Image.Load<Rgba32>(_cResourceCache.ContentFileRead("/Textures/error.rsi/error.png"));
            _images = new Dictionary<(string path, string state), Image>();

            _entities = GetEntities();
        }

        public void Run(Image gridCanvas, IMapGrid grid)
        {
            if (!_entities.TryGetValue(grid.Index, out var entities))
            {
                Console.WriteLine($"No entities found on grid {grid.Index}");
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var bounds = grid.WorldBounds;
            var xOffset = (int) Math.Abs(bounds.Left);
            var yOffset = (int) Math.Abs(bounds.Bottom);


            // TODO cache this shit what are we insane
            entities.Sort(Comparer<EntityData>.Create((x, y) => x.Sprite.DrawDepth.CompareTo(y.Sprite.DrawDepth)));

            foreach (var entity in entities)
            {
                if (entity.Sprite.Owner.HasComponent<SubFloorHideComponent>())
                {
                    continue;
                }

                if (!entity.Sprite.Visible || entity.Sprite.ContainerOccluded)
                {
                    continue;
                }

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

                    if (rsi == null || rsi.Path == null || !rsi.TryGetState(layer.RsiState, out var state))
                    {
                        image = _errorImage;
                    }
                    else
                    {
                        var key = (rsi.Path!.ToString(), state.StateId.Name);

                        if (!_images.TryGetValue(key, out image))
                        {
                            var stream = _cResourceCache.ContentFileRead($"{rsi.Path}/{state.StateId}.png");
                            image = Image.Load<Rgba32>(stream);

                            _images[key] = image;
                        }
                    }

                    image = image.CloneAs<Rgba32>();

                    var directions = entity.Sprite.GetLayerDirectionCount(layer);

                    // TODO add support for 8 directions and animations (delays)
                    if (directions != 1 && directions != 8)
                    {
                        double xStart, xEnd, yStart, yEnd;

                        switch (directions)
                        {
                            case 4:
                            {
                                var dir = layer.EffectiveDirection(entity.Sprite.Owner.Transform.WorldRotation);

                                (xStart, xEnd, yStart, yEnd) = dir switch
                                {
                                    // Only need the first tuple as doubles for the compiler to recognize it
                                    RSI.State.Direction.South => (0d, 0.5d, 0d, 0.5d),
                                    RSI.State.Direction.East => (0, 0.5, 0.5, 1),
                                    RSI.State.Direction.North => (0.5, 1, 0, 0.5),
                                    RSI.State.Direction.West => (0.5, 1, 0.5, 1),
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        var x = (int) (image.Width * xStart);
                        var width = (int) (image.Width * xEnd) - x;

                        var y = (int) (image.Height * yStart);
                        var height = (int) (image.Height * yEnd) - y;

                        image.Mutate(o => o.Crop(new Rectangle(x, y, width, height)));
                    }

                    var colorMix = entity.Sprite.Color * layer.Color;
                    var imageColor = Color.FromRgba(colorMix.RByte, colorMix.GByte, colorMix.BByte, colorMix.AByte);
                    var coloredImage = new Image<Rgba32>(image.Width, image.Height);
                    coloredImage.Mutate(o => o.BackgroundColor(imageColor));

                    image.Mutate(o => o
                        .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1)
                        .Resize(32, 32).Flip(FlipMode.Vertical));

                    var pointX = (int) ((entity.X + xOffset) * 32) - 16;
                    var pointY = (int) ((entity.Y + yOffset) * 32) - 16;
                    gridCanvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), 1));
                }
            }

            Console.WriteLine($"{nameof(EntityPainter)} painted {entities.Count} entities on grid {grid.Index} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private ConcurrentDictionary<GridId, List<EntityData>> GetEntities()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var components = new ConcurrentDictionary<GridId, List<EntityData>>();

            foreach (var entity in _sEntityManager.GetEntities())
            {
                if (!entity.HasComponent<ISpriteRenderableComponent>())
                {
                    continue;
                }

                if (entity.Prototype == null)
                {
                    continue;
                }

                var clientEntity = _cEntityManager.GetEntity(entity.Uid);

                if (!clientEntity.TryGetComponent(out SpriteComponent sprite))
                {
                    throw new InvalidOperationException(
                        $"No sprite component found on an entity for which a server sprite component exists. Prototype id: {entity.Prototype?.ID}");
                }

                var position = entity.Transform.WorldPosition;
                var x = position.X;
                var y = position.Y;
                var data = new EntityData(sprite, x, y);

                components.GetOrAdd(entity.Transform.GridID, _ => new List<EntityData>()).Add(data);
            }

            Console.WriteLine($"Found {components.Values.Sum(l => l.Count)} entities on {components.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return components;
        }
    }
}
