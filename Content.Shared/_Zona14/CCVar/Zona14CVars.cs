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

    // Zona14: admin alerting / anti-cheat CVars
    /// <summary>
    /// Discord webhook URL for Extreme admin log alerts.
    /// </summary>
    public static readonly CVarDef<string> AdminAlertWebhook =
        CVarDef.Create("zona14.admin_alert_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    /// Whether to send Discord webhook alerts for Extreme impact admin logs.
    /// </summary>
    public static readonly CVarDef<bool> AdminAlertExtremeLogsEnabled =
        CVarDef.Create("zona14.admin_alert_extreme_logs_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Master switch for anti-cheat detection.
    /// </summary>
    public static readonly CVarDef<bool> CheatDetectionEnabled =
        CVarDef.Create("zona14.cheat_detection_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Whether impossible-movement detection is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CheatDetectionMovementEnabled =
        CVarDef.Create("zona14.cheat_detection_movement_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Minimum time (seconds) between movement samples.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionMovementMinTime =
        CVarDef.Create("zona14.cheat_detection_movement_min_time", 0.1f, CVar.SERVERONLY);

    /// <summary>
    /// Time (seconds) after which a movement sample is reset due to inactivity.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionMovementSampleWindow =
        CVarDef.Create("zona14.cheat_detection_movement_sample_window", 1.0f, CVar.SERVERONLY);

    /// <summary>
    /// Maximum distance (tiles) in a movement sample; larger distances are treated as teleports.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionMovementMaxDistance =
        CVarDef.Create("zona14.cheat_detection_movement_max_distance", 5.0f, CVar.SERVERONLY);

    /// <summary>
    /// Maximum speed (tiles/sec) before a movement is flagged as impossible.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionMovementMaxSpeed =
        CVarDef.Create("zona14.cheat_detection_movement_max_speed", 25.0f, CVar.SERVERONLY);

    /// <summary>
    /// Whether rapid-kill detection is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CheatDetectionKillsEnabled =
        CVarDef.Create("zona14.cheat_detection_kills_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Sliding window (seconds) for rapid-kill detection.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionKillsWindow =
        CVarDef.Create("zona14.cheat_detection_kills_window", 10.0f, CVar.SERVERONLY);

    /// <summary>
    /// Number of kills in the window before a rapid-kill alert is raised.
    /// </summary>
    public static readonly CVarDef<int> CheatDetectionKillsThreshold =
        CVarDef.Create("zona14.cheat_detection_kills_threshold", 5, CVar.SERVERONLY);

    /// <summary>
    /// Whether mass door-destruction detection is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CheatDetectionDoorsEnabled =
        CVarDef.Create("zona14.cheat_detection_doors_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Sliding window (seconds) for mass door-destruction detection.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionDoorsWindow =
        CVarDef.Create("zona14.cheat_detection_doors_window", 20.0f, CVar.SERVERONLY);

    /// <summary>
    /// Number of door destructions in the window before an alert is raised.
    /// </summary>
    public static readonly CVarDef<int> CheatDetectionDoorsThreshold =
        CVarDef.Create("zona14.cheat_detection_doors_threshold", 5, CVar.SERVERONLY);

    /// <summary>
    /// Whether mass item-spawn detection is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CheatDetectionSpawnsEnabled =
        CVarDef.Create("zona14.cheat_detection_spawns_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Sliding window (seconds) for mass item-spawn detection.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionSpawnsWindow =
        CVarDef.Create("zona14.cheat_detection_spawns_window", 10.0f, CVar.SERVERONLY);

    /// <summary>
    /// Number of item spawns in the window before an alert is raised.
    /// </summary>
    public static readonly CVarDef<int> CheatDetectionSpawnsThreshold =
        CVarDef.Create("zona14.cheat_detection_spawns_threshold", 10, CVar.SERVERONLY);

    /// <summary>
    /// Cooldown (seconds) per player before another cheat alert is raised.
    /// </summary>
    public static readonly CVarDef<float> CheatDetectionAlertCooldown =
        CVarDef.Create("zona14.cheat_detection_alert_cooldown", 30.0f, CVar.SERVERONLY);

    /// <summary>
    /// Whether to preload all new-map teleport target maps on PostGameMapLoad.
    /// </summary>
    public static readonly CVarDef<bool> NewMapTeleportPreload =
        CVarDef.Create("zona14.newmap_teleport_preload", false, CVar.SERVERONLY);

    // Zona14: mentor help CVars
    /// <summary>
    /// Path to the sound played for incoming mentor help messages.
    /// </summary>
    public static readonly CVarDef<string> MentorHelpSound =
        CVarDef.Create("audio.mhelp_sound", "/Audio/_Stalker/announce.ogg", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Whether the mentor help sound is enabled.
    /// </summary>
    public static readonly CVarDef<bool> MentorHelpSoundEnabled =
        CVarDef.Create("audio.mhelp_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Mentor help rate limit period in seconds.
    /// </summary>
    public static readonly CVarDef<float> MentorHelpRateLimitPeriod =
        CVarDef.Create("mhelp.rate_limit_period", 2.0f, CVar.SERVERONLY);

    /// <summary>
    /// Number of mentor help messages allowed per rate limit period.
    /// </summary>
    public static readonly CVarDef<int> MentorHelpRateLimitCount =
        CVarDef.Create("mhelp.rate_limit_count", 10, CVar.SERVERONLY);
    // End Zona14
}
