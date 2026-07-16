// SPDX-License-Identifier: MIT

using System.Text.Json;
using Content.Server.Administration.Logs.Converters;
using Content.Shared._Zona14.Administration.Logs;

namespace Content.Server._Zona14.Administration.Logs.Converters;

// Zona14: JSON converter for offline player references in admin log messages.
[AdminLogConverter]
public sealed class AdminLogPlayerValueConverter : AdminLogConverter<AdminLogPlayerValue>
{
    public override void Write(Utf8JsonWriter writer, AdminLogPlayerValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("player", value.UserId.UserId);
        if (!string.IsNullOrEmpty(value.Name))
            writer.WriteString("name", value.Name);
        writer.WriteEndObject();
    }
}
