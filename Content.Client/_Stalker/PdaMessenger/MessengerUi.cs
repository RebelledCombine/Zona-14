using Content.Client.UserInterface.Fragments;
using Content.Shared._Stalker.PdaMessenger;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._Stalker.PdaMessenger;

public sealed partial class MessengerUi : UIFragment
{
    private MessengerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessengerUiFragment();
        _fragment.OnSendMessage += message =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new MessengerUiMessageEvent(message)));
        };

        _fragment.OnLogin += owner =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new MessengerUiSetLoginEvent(owner)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerUiState messengerState)
            return;

        _fragment?.UpdateState(messengerState.Chats);
    }
}
