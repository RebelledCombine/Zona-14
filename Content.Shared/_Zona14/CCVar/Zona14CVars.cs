// SPDX-License-Identifier: MIT
// Ported from RMC-14 Content.Shared/_RMC14/CCVar/RMCCVars.cs@33bca9e819 (lines 196-212, 517-521).
using Robust.Shared.Configuration;

namespace Content.Shared._Zona14.CCVar;

[CVarDefs]
public static class Zona14CVars
{
    public static readonly CVarDef<bool> GunPredictionPreventCollision =
        CVarDef.Create("zona14.gun_prediction_prevent_collision", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> GunPredictionLogHits =
        CVarDef.Create("zona14.gun_prediction_log_hits", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> GunPredictionCoordinateDeviation =
        CVarDef.Create("zona14.gun_prediction_coordinate_deviation", 3f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> GunPredictionLowestCoordinateDeviation =
        CVarDef.Create("zona14.gun_prediction_lowest_coordinate_deviation", 3f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> GunPredictionAabbEnlargement =
        CVarDef.Create("zona14.gun_prediction_aabb_enlargement", 1.5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> LagCompensationMilliseconds =
        CVarDef.Create("zona14.lag_compensation_milliseconds", 750, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> LagCompensationMarginTiles =
        CVarDef.Create("zona14.lag_compensation_margin_tiles", 0.25f, CVar.SERVER | CVar.REPLICATED);

    // Ported from funky-station Content.Shared/_Funkystation/CCVars/CCVars.Funky.cs@2e50750ab6 (content warning CVars)

    /// <summary>
    /// If the content warning should be displayed.
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningDisplay =
        CVarDef.Create("zona14.content_warning.display", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// If ignoring the content warning should kick (quit) the client.
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningKickOnIgnore =
        CVarDef.Create("zona14.content_warning.kick_on_ignore", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// If the content warning popup was acknowledged on this client.
    /// </summary>
    public static readonly CVarDef<bool> ContentWarningAcknowledged =
        CVarDef.Create("zona14.content_warning.acknowledged", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
