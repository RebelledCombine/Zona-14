// SPDX-License-Identifier: MIT

#nullable enable

using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Pair;
using Content.Shared._Stalker.Teleport;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Zona14.Maps;

/// <summary>
/// Loads every map referenced by a <see cref="MapLoaderPrototype"/> to ensure they remain valid.
/// </summary>
[TestFixture]
public sealed class MapLoaderPrototypeTest
{
    private static readonly string MapLoaderYamlPath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "..",
        "..",
        "Resources",
        "Prototypes",
        "_Stalker",
        "ScriptedEntities",
        "Portals",
        "mapLoader.yml");

    public static IEnumerable MapLoaderPrototypeMapPaths()
    {
        var path = Path.GetFullPath(MapLoaderYamlPath);
        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var root = (YamlSequenceNode) yaml.Documents[0].RootNode;
        foreach (var mapping in root.Cast<YamlMappingNode>())
        {
            if (mapping.GetNode("type").AsString() != "mapLoader")
                continue;

            if (!mapping.Children.TryGetValue("mapPaths", out var mapPathsNode))
                continue;

            var mapPaths = (YamlMappingNode) mapPathsNode;
            foreach (var entry in mapPaths.Children)
            {
                var name = ((YamlScalarNode) entry.Key).Value;
                var mapPath = entry.Value.AsString();
                yield return new TestCaseData(name, mapPath).SetName($"MapLoadable_{name}");
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(MapLoaderPrototypeMapPaths))]
    public async Task MapLoadable(string mapName, string mapPath)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
            FailureLogLevel = LogLevel.Fatal,
        });

        var server = pair.Server;
        var mapLoader = server.EntMan.System<MapLoaderSystem>();
        var mapSystem = server.EntMan.System<SharedMapSystem>();

        var path = new ResPath(mapPath);
        var success = false;
        Exception? exception = null;

        await server.WaitPost(() =>
        {
            try
            {
                success = mapLoader.TryLoadMap(
                    path,
                    out var map,
                    out _,
                    DeserializationOptions.Default with { InitializeMaps = true });

                if (map != null)
                {
                    mapSystem.DeleteMap(map.Value.Comp.MapId);
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
        });

        await server.WaitRunTicks(1);

        if (exception != null)
        {
            Assert.Fail($"MapLoader map {mapName} ({mapPath}) threw an exception while loading: {exception}");
        }

        Assert.That(success, Is.True, $"MapLoader map {mapName} ({mapPath}) failed to load");

        await pair.CleanReturnAsync();
    }
}
