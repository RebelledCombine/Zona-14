using Content.Server._Stalker.Chat;
using Content.Server.Radio;
using Content.Shared._Stalker.Deafness;
using Content.Shared.Chat;

namespace Content.Server._Stalker.Deafness;

public sealed class STDeafSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STDeafComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<STDeafComponent, STChatMessageOverrideInVoiceRangeEvent>(OnOverrideInVoiceRange);
    }

    private void OnRadioReceiveAttempt(Entity<STDeafComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (args.RadioReceiver != ent.Owner)
            return;

        args.Cancelled = true;
    }

    private void OnOverrideInVoiceRange(Entity<STDeafComponent> ent, ref STChatMessageOverrideInVoiceRangeEvent args)
    {
        if (args.Channel is ChatChannel.Emotes
            or ChatChannel.Damage
            or ChatChannel.Visual
            or ChatChannel.Notifications
            or ChatChannel.OOC
            or ChatChannel.LOOC)
            return;

        var message = Loc.GetString(ent.Owner != args.Source ? "st-deaf-someone-talking" : "st-deaf-cant-hear-self"); // Zona14: localized

        args.WrappedMessage = message;
        args.Message = message;
    }
}
