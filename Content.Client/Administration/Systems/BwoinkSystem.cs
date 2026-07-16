#nullable enable
using Content.Shared._Zona14.Administration.Bwoink; // Zona14: AHelp assignment
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public event EventHandler<BwoinkTextMessage>? OnBwoinkTextMessageRecieved;
        public event Action<NetUserId, string?>? OnAssignUpdated; // Zona14: AHelp assignment changed

        private readonly Dictionary<NetUserId, string?> _assignedAdmins = new(); // Zona14: channel -> admin name
        private (TimeSpan Timestamp, bool Typing) _lastTypingUpdateSent;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<BwoinkAssignUpdated>(HandleAssignUpdated); // Zona14
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            OnBwoinkTextMessageRecieved?.Invoke(this, message);
        }

        // Zona14: AHelp assignment update received from server
        private void HandleAssignUpdated(BwoinkAssignUpdated msg, EntitySessionEventArgs args)
        {
            if (msg.AdminName == null)
                _assignedAdmins.Remove(msg.Channel);
            else
                _assignedAdmins[msg.Channel] = msg.AdminName;

            OnAssignUpdated?.Invoke(msg.Channel, msg.AdminName);
        }

        public bool TryGetAssignedAdmin(NetUserId channel, out string? adminName)
        {
            return _assignedAdmins.TryGetValue(channel, out adminName);
        }

        public void Send(NetUserId channelId, string text, bool playSound, bool adminOnly)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text, playSound: playSound, adminOnly: adminOnly));
            SendInputTextUpdated(channelId, false);
        }

        // Zona14: request assignment change for an AHelp channel
        public void SendAssign(NetUserId channelId, bool unassign)
        {
            RaiseNetworkEvent(new BwoinkAssignMessage(channelId, unassign));
        }

        public void SendInputTextUpdated(NetUserId channel, bool typing)
        {
            if (_lastTypingUpdateSent.Typing == typing &&
                _lastTypingUpdateSent.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime)
            {
                return;
            }

            _lastTypingUpdateSent = (_timing.RealTime, typing);
            RaiseNetworkEvent(new BwoinkClientTypingUpdated(channel, typing));
        }
    }
}
