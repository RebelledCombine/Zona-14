// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Discord;
using Content.Server._Zona14.Administration.Logs;
using Content.Shared.Database;
using Content.Shared._Zona14.CCVar;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.Log;

namespace Content.Server._Zona14.Administration.Systems;

/// <summary>
/// Sends Discord webhook alerts for Extreme impact admin logs.
/// </summary>
public sealed class Z14AdminAlertSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discordWebhook = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private bool _extremeLogsEnabled;
    private string _webhookUrl = string.Empty;

    private WebhookData? _cachedWebhook;
    private string? _cachedWebhookUrl;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("z14.admin_alert");
        LoadCVars();
        _cfg.OnCVarValueChanged += OnCVarValueChanged;
        ((IAdminLogManager)_adminLog).OnAdminLogAdded += OnAdminLogAdded;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.OnCVarValueChanged -= OnCVarValueChanged;
        ((IAdminLogManager)_adminLog).OnAdminLogAdded -= OnAdminLogAdded;
    }

    private void LoadCVars()
    {
        _enabled = _cfg.GetCVar(Zona14CVars.AdminAlertWebhook) != string.Empty;
        _webhookUrl = _cfg.GetCVar(Zona14CVars.AdminAlertWebhook);
        _extremeLogsEnabled = _cfg.GetCVar(Zona14CVars.AdminAlertExtremeLogsEnabled);
    }

    private void OnCVarValueChanged(CVarChangeInfo info)
    {
        if (info.Name.StartsWith("zona14.admin_alert_"))
            LoadCVars();
    }

    private void OnAdminLogAdded(object? sender, AdminLogAddedEventArgs e)
    {
        if (!_enabled || !_extremeLogsEnabled || e.Impact != LogImpact.Extreme)
            return;

        SendWebhook(e);
    }

    private async void SendWebhook(AdminLogAddedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        try
        {
            if (_cachedWebhookUrl != _webhookUrl || _cachedWebhook is null)
            {
                var fetched = await _discordWebhook.GetWebhook(_webhookUrl);
                if (fetched is null)
                    return;

                _cachedWebhook = fetched;
                _cachedWebhookUrl = _webhookUrl;
            }

            var data = _cachedWebhook.Value;
            var payload = BuildPayload(e);
            await _discordWebhook.CreateMessage(data.ToIdentifier(), payload);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send Discord admin alert webhook: {ex}");
        }
    }

    private WebhookPayload BuildPayload(AdminLogAddedEventArgs e)
    {
        return new WebhookPayload
        {
            Username = "Z14 Admin Alert",
            Embeds = new List<WebhookEmbed> { BuildEmbed(e) },
            AllowedMentions = new WebhookMentions()
        };
    }

    private WebhookEmbed BuildEmbed(AdminLogAddedEventArgs e)
    {
        var description = e.Log.Message;
        if (description.Length > 4000)
            description = description[..4000] + "...";

        var players = e.Players.Count == 0
            ? "none"
            : string.Join(", ", e.Players.Take(10).Select(p => p.PlayerUserId.ToString()))
              + (e.Players.Count > 10 ? "..." : string.Empty);

        var footer = $"{_baseServer.ServerName} - Round {e.Log.RoundId}";

        return new WebhookEmbed
        {
            Title = $"Extreme admin log: {e.Log.Type}",
            Description = description,
            Color = 0xFF0000,
            Footer = new WebhookEmbedFooter { Text = footer },
            Fields = new List<WebhookEmbedField>
            {
                new() { Name = "Type", Value = e.Log.Type.ToString(), Inline = true },
                new() { Name = "Impact", Value = e.Log.Impact.ToString(), Inline = true },
                new() { Name = "Round", Value = e.Log.RoundId.ToString(), Inline = true },
                new() { Name = "Date", Value = e.Log.Date.ToString("yyyy-MM-dd HH:mm:ss UTC"), Inline = true },
                new() { Name = "Players", Value = players, Inline = false }
            }
        };
    }
}
