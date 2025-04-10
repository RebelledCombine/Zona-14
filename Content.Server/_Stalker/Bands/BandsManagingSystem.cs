using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Stalker.Bands.Components;
using Content.Server.Database;
using Content.Server.Mind;
using Content.Shared._Stalker.Bands;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Stalker.Bands
{
    public sealed class BandsManagingSystem : EntitySystem
    {
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BandsManagingComponent, BoundUIOpenedEvent>(OnUiOpen);
            SubscribeLocalEvent<BandsManagingComponent, BandsManagingAddMemberMessage>(OnAddMember);
            SubscribeLocalEvent<BandsManagingComponent, BandsManagingRemoveMemberMessage>(OnRemoveMember);
        }

        private async void OnUiOpen(EntityUid uid, BandsManagingComponent component, BoundUIOpenedEvent args)
        {
            if (!_mindSystem.TryGetMind(args.Actor, out _, out var mindComp) || !_mindSystem.TryGetSession(mindComp, out var session))
                return;
            var userId = session.UserId;

            var band = await GetPlayerBandAsync(userId);
            var members = await GetBandMembersAsync(userId);

            string? bandName = band?.Name;
            int maxMembers = band?.MaxMembers ?? 0;
            bool canManage = await CanPlayerManageBandAsync(userId);

            var memberInfos = members.Select(m => new BandMemberInfo(m.UserId, m.LastSeenUserName, m.PrimaryRoleId)).ToList();

            var state = new BandsManagingBoundUserInterfaceState(bandName, maxMembers, memberInfos, canManage);

            _uiSystem.TrySetUiState(uid, component.Key, state);
        }

        private async void OnAddMember(EntityUid uid, BandsManagingComponent component, BandsManagingAddMemberMessage msg)
        {
            if (!_uiSystem.TryGetSession(uid, out var session))
                return;

            var leaderUserId = session.UserId;

            if (!await _dbManager.CanPlayerManageBandAsync(leaderUserId))
                return;

            var targetPlayerRecord = await _dbManager.GetPlayerRecordByUserName(msg.PlayerName);
            if (targetPlayerRecord == null)
                return;

            var targetUserId = targetPlayerRecord.UserId;

            var leaderBand = await _dbManager.GetPlayerBandAsync(leaderUserId);
            if (leaderBand == null)
                return;

            var currentMembers = await _dbManager.GetBandMembersAsync(leaderUserId);
            if (currentMembers.Count >= leaderBand.MaxMembers)
                return;

            if (currentMembers.Any(m => m.UserId == targetUserId))
                return;

            var targetBand = await _dbManager.GetPlayerBandAsync(targetUserId);
            if (targetBand != null)
                return;

            var lowestRankRoleId = leaderBand.Hierarchy.OrderBy(kvp => kvp.Key).FirstOrDefault().Value;
            if (lowestRankRoleId == default)
                return;

            await _dbManager.AddRoleWhitelistAsync(targetUserId, lowestRankRoleId);

            OnUiOpen(uid, component, new BoundUIOpenedEvent(session));
        }

        private async void OnRemoveMember(EntityUid uid, BandsManagingComponent component, BandsManagingRemoveMemberMessage msg)
        {
            if (!_uiSystem.TryGetSession(uid, out var session))
                return;

            var leaderUserId = session.UserId;

            if (!await _dbManager.CanPlayerManageBandAsync(leaderUserId))
                return;

            if (leaderUserId.UserId == msg.PlayerUserId)
                return;

            var leaderBand = await _dbManager.GetPlayerBandAsync(leaderUserId);
            if (leaderBand == null)
                return;

            var bandRoleIds = leaderBand.Hierarchy.Values.Select(p => p.Id).ToHashSet();
            bandRoleIds.Add(leaderBand.ID);

            var memberRolesInBand = (await _dbManager.GetRoleWhitelistsAsync(msg.PlayerUserId))
                .Where(rw => bandRoleIds.Contains(rw.RoleId))
                .ToList();

            if (!memberRolesInBand.Any())
                return;

            foreach (var roleWhitelist in memberRolesInBand)
            {
                await _dbManager.RemoveRoleWhitelistAsync(msg.PlayerUserId, roleWhitelist.RoleId);
            }

            OnUiOpen(uid, component, new BoundUIOpenedEvent(session));
        }
    }
}
