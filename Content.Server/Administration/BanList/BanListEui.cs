using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Administration.BanList;
using Content.Shared._Zona14.Administration.BanList; // Zona14: PardonBanMessage
using Content.Shared.Eui;
using Robust.Shared.Network;

namespace Content.Server.Administration.BanList;

public sealed class BanListEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IBanManager _banManager = default!; // Zona14: pardon support

    public BanListEui()
    {
        IoCManager.InjectDependencies(this);
    }

    private Guid BanListPlayer { get; set; }
    private string BanListPlayerName { get; set; } = string.Empty;
    private List<SharedServerBan> Bans { get; } = new();
    private List<SharedServerRoleBan> RoleBans { get; } = new();

    public override void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
    }

    // Zona14: handle pardon requests from the ban list UI
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PardonBanMessage pardon)
            return;

        OnPardon(pardon);
    }

    private async void OnPardon(PardonBanMessage pardon)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Ban))
            return;

        if (pardon.IsRoleBan)
            await _banManager.PardonRoleBan(pardon.BanId, Player.UserId, DateTimeOffset.Now);
        else
            await _banManager.PardonBan(pardon.BanId, Player.UserId, DateTimeOffset.Now);

        await LoadFromDb();
    }
    // End Zona14

    public override EuiStateBase GetNewState()
    {
        return new BanListEuiState(BanListPlayerName, Bans, RoleBans);
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            Close();
        }
    }

    // Zona14: load all bans when no user is selected (global ban list)
    private async Task LoadBans(NetUserId? userId)
    {
        var bans = userId.HasValue
            ? await _db.GetServerBansAsync(null, userId, null, null)
            : await _db.GetAllServerBansAsync();

        foreach (var ban in bans)
        {
            SharedServerUnban? unban = null;
            if (ban.Unban is { } unbanDef)
            {
                var unbanningAdmin = unbanDef.UnbanningAdmin == null
                    ? null
                    : (await _playerLocator.LookupIdAsync(unbanDef.UnbanningAdmin.Value))?.Username;
                unban = new SharedServerUnban(unbanningAdmin, ban.Unban.UnbanTime.UtcDateTime);
            }

            (string, int cidrMask)? ip = ("*Hidden*", 0);
            var hwid = "*Hidden*";

            if (_admins.HasAdminFlag(Player, AdminFlags.Pii))
            {
                ip = ban.Address is { } address
                    ? (address.address.ToString(), address.cidrMask)
                    : null;

                hwid = ban.HWId?.ToString();
            }

            Bans.Add(new SharedServerBan(
                ban.Id,
                ban.UserId,
                ip,
                hwid,
                ban.BanTime.UtcDateTime,
                ban.ExpirationTime?.UtcDateTime,
                ban.Reason,
                ban.BanningAdmin == null
                    ? null
                    : (await _playerLocator.LookupIdAsync(ban.BanningAdmin.Value))?.Username,
                unban
            ));
        }
    }
    // End Zona14

    // Zona14: load all role bans when no user is selected (global ban list)
    private async Task LoadRoleBans(NetUserId? userId)
    {
        var bans = userId.HasValue
            ? await _db.GetServerRoleBansAsync(null, userId, null, null)
            : await _db.GetAllServerRoleBansAsync();

        foreach (var ban in bans)
        {
            SharedServerUnban? unban = null;
            if (ban.Unban is { } unbanDef)
            {
                var unbanningAdmin = unbanDef.UnbanningAdmin == null
                    ? null
                    : (await _playerLocator.LookupIdAsync(unbanDef.UnbanningAdmin.Value))?.Username;
                unban = new SharedServerUnban(unbanningAdmin, ban.Unban.UnbanTime.UtcDateTime);
            }

            (string, int cidrMask)? ip = ("*Hidden*", 0);
            var hwid = "*Hidden*";

            if (_admins.HasAdminFlag(Player, AdminFlags.Pii))
            {
                ip = ban.Address is { } address
                    ? (address.address.ToString(), address.cidrMask)
                    : null;

                hwid = ban.HWId?.ToString();
            }
            RoleBans.Add(new SharedServerRoleBan(
                ban.Id,
                ban.UserId,
                ip,
                hwid,
                ban.BanTime.UtcDateTime,
                ban.ExpirationTime?.UtcDateTime,
                ban.Reason,
                ban.BanningAdmin == null
                    ? null
                    : (await _playerLocator.LookupIdAsync(ban.BanningAdmin.Value))?.Username,
                unban,
                ban.Role
            ));
        }
    }
    // End Zona14

    // Zona14: load all bans when BanListPlayer is Guid.Empty
    private async Task LoadFromDb()
    {
        Bans.Clear();
        RoleBans.Clear();

        if (BanListPlayer == Guid.Empty)
        {
            BanListPlayerName = Loc.GetString("ban-list-all");
            await LoadBans(null);
            await LoadRoleBans(null);
        }
        else
        {
            var userId = new NetUserId(BanListPlayer);
            BanListPlayerName = (await _playerLocator.LookupIdAsync(userId))?.Username ??
                                string.Empty;

            await LoadBans(userId);
            await LoadRoleBans(userId);
        }

        StateDirty();
    }
    // End Zona14

    public async Task ChangeBanListPlayer(Guid banListPlayer)
    {
        BanListPlayer = banListPlayer;
        await LoadFromDb();
    }
}
