// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Client._Zona14.UserInterface.Systems.MentorHelp;
using Content.Shared._Zona14.Administration.MentorHelp;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;

namespace Content.Client._Zona14.Administration.UI.MentorHelp;

public sealed class MentorHelpWindow : DefaultWindow, IMentorHelpUIHandler
{
    private readonly NetUserId _owner;
    private readonly IMentorHelpController _controller;

    public MentorHelpControl Control { get; }

    public MentorHelpWindow(NetUserId owner, IMentorHelpController controller)
    {
        _owner = owner;
        _controller = controller;

        Title = Loc.GetString("mentorhelp-window-title");
        MinSize = new Vector2(640, 400);
        SetSize = new Vector2(800, 600);

        Control = new MentorHelpControl(controller)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        Contents.AddChild(Control);
    }

    public void Receive(MentorHelpTextMessage message)
    {
    }

    public void PeopleTypingUpdated(MentorHelpPlayerTypingUpdated message)
    {
    }

    public void ToggleWindow()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open(NetUserId? channelId = null)
    {
        OpenCentered();
        Control.PopulateList();

        if (channelId != null)
            Control.SelectChannel(channelId.Value);
    }

}
