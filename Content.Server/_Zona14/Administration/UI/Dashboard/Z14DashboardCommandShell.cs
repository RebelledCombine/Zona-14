// SPDX-License-Identifier: MIT

using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Zona14.Administration.UI.Dashboard;

/// <summary>
/// A console shell that captures all written output so it can be shown in a client UI window.
/// </summary>
public sealed class Z14DashboardCommandShell : IConsoleShell
{
    private readonly StringBuilder _output = new();

    public IConsoleHost ConsoleHost { get; }
    public bool IsLocal => false;
    public bool IsServer => true;
    public ICommonSession? Player { get; }
    public string OutputText => _output.ToString();

    public Z14DashboardCommandShell(IConsoleHost consoleHost, ICommonSession? player)
    {
        ConsoleHost = consoleHost;
        Player = player;
    }

    public void ExecuteCommand(string command)
    {
    }

    public void RemoteExecuteCommand(string command)
    {
    }

    public void WriteLine(string text)
    {
        _output.AppendLine(text);
    }

    public void WriteLine(FormattedMessage message)
    {
        _output.AppendLine(message.ToMarkup());
    }

    public void WriteError(string text)
    {
        _output.AppendLine($"[Error] {text}");
    }

    public void Clear()
    {
        _output.Clear();
    }
}
