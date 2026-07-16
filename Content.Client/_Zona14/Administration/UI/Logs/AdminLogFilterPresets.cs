// SPDX-License-Identifier: MIT

using System.IO;
using System.Text;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Content.Client._Zona14.Administration.UI.Logs;

/// <summary>
/// Persistable admin-log filter preset.
/// </summary>
[DataDefinition]
public sealed partial class AdminLogFilterPreset
{
    [DataField] public string Name { get; set; } = string.Empty;
    [DataField] public string Search { get; set; } = string.Empty;
    [DataField] public int RoundId { get; set; }
    [DataField] public List<string> Types { get; set; } = new();
    [DataField] public List<string> Impacts { get; set; } = new();
    [DataField] public List<string> Players { get; set; } = new();
    [DataField] public bool IncludeNonPlayerLogs { get; set; } = true;
}

/// <summary>
/// Loads/saves user-defined admin-log filter presets from the client's user data folder.
/// </summary>
public sealed class AdminLogFilterPresets
{
    private static readonly ResPath PresetsPath = new("/admin_log_presets.yml");

    private readonly IWritableDirProvider _userData;
    private readonly ISerializationManager _serializationManager;
    private readonly List<AdminLogFilterPreset> _presets = new();

    public IReadOnlyList<AdminLogFilterPreset> Presets => _presets;

    public AdminLogFilterPresets(IWritableDirProvider userData, ISerializationManager serializationManager)
    {
        _userData = userData;
        _serializationManager = serializationManager;
    }

    public void Load()
    {
        _presets.Clear();

        if (!_userData.TryReadAllText(PresetsPath, out var text))
            return;

        if (text.Length == 0)
            return;

        try
        {
            var byteCount = Encoding.UTF8.GetByteCount(text);
            var bytes = new byte[byteCount];
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);

            DataNode? root = null;
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8);
            foreach (var doc in DataNodeParser.ParseYamlStream(reader))
            {
                root = doc.Root;
                break;
            }

            if (root == null)
                return;

            var loaded = _serializationManager.Read<List<AdminLogFilterPreset>>(root, null, false, null, true);
            if (loaded != null)
                _presets.AddRange(loaded);
        }
        catch
        {
            // Corrupt file; start fresh.
        }
    }

    public void Save()
    {
        var node = _serializationManager.WriteValue(_presets, false, null, true);
        _userData.WriteAllText(PresetsPath, node.ToString());
    }

    public void Add(AdminLogFilterPreset preset)
    {
        _presets.RemoveAll(p => p.Name == preset.Name);
        _presets.Add(preset);
        Save();
    }

    public void Remove(string name)
    {
        if (_presets.RemoveAll(p => p.Name == name) > 0)
            Save();
    }
}
