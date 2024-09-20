using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.PdaMessenger;

[Serializable, NetSerializable]
public sealed class PdaMessage
{
    public string Title;
    public string Content;
    public string Receiver;

    public PdaMessage(string title, string content, string receiver)
    {
        Title = title;
        Content = content;
        Receiver = receiver;
    }
}
