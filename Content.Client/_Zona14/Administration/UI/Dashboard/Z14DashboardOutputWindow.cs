// SPDX-License-Identifier: MIT

using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14DashboardOutputWindow : DefaultWindow
{
    private readonly RichTextLabel _label;
    [Dependency] private readonly IClipboardManager _clipboard = default!;

    public string Command { get; }

    public event Action<string>? OnRefresh;

    public Z14DashboardOutputWindow(string title, string command)
    {
        IoCManager.InjectDependencies(this);
        Command = command;
        Title = title;
        SetSize = new Vector2(650, 500);
        MinSize = new Vector2(400, 300);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(8),
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _label = new RichTextLabel
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        scroll.AddChild(_label);
        root.AddChild(scroll);

        var buttonRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };

        var refreshButton = new Button { Text = "Refresh" };
        refreshButton.OnPressed += _ => OnRefresh?.Invoke(Command);
        buttonRow.AddChild(refreshButton);

        var copyButton = new Button { Text = "Copy" };
        copyButton.OnPressed += _ => _clipboard.SetText(_label.GetMessage() ?? string.Empty);
        buttonRow.AddChild(copyButton);

        root.AddChild(buttonRow);
        Contents.AddChild(root);
    }

    public void SetText(string text)
    {
        _label.SetMessage(text);
    }
}
