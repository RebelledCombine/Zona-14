// SPDX-License-Identifier: MIT

using Content.Shared._Stalker.Anomaly.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.AnomalyMigration;

public readonly record struct Z14AnomalyMigrationTarget(
    MapId MapId,
    string? MapKey,
    string MapName,
    ProtoId<STAnomalyGenerationOptionsPrototype> OptionsId);
