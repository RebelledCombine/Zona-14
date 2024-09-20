namespace Content.Server._Stalker.PdaMessenger;

[RegisterComponent]
public sealed partial class PdaMessengerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SendTimeCooldown = TimeSpan.FromSeconds(5);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSendTime;

}
