// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Stylesheets;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Client._Zona14.Administration.UI.MentorHelp;
using Content.Client._Zona14.UserInterface.Systems.MentorHelp;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14AdminDashboardWindow : DefaultWindow
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private Label _roundLabel = null!;
    private Label _durationLabel = null!;
    private Label _runLevelLabel = null!;
    private Label _playerCountLabel = null!;
    private Label _adminCountLabel = null!;
    private WrapContainer _flagContainer = null!;
    private RichTextLabel _eventCountLabel = null!;
    private Label _alertCountLabel = null!;

    private ItemList _playerList = null!;
    private ItemList _mapList = null!;
    private OutputPanel _eventOutput = null!;
    private OutputPanel _alertOutput = null!;
    private ItemList _commandList = null!;

    private HistoryLineEdit _commandLine = null!;
    private LineEdit _commandSearch = null!;
    private LineEdit _playerSearch = null!;
    private LineEdit _eventFilter = null!;

    private RichTextLabel _commandHint = null!;

    private Button _pauseEventsButton = null!;
    private Button _clearEventsButton = null!;
    private Button _teleportMapButton = null!;

    private WrapContainer _actionButtons = null!;
    private readonly Dictionary<Button, AdminFlags> _buttonFlags = new();

    private AdminFlags _flags;
    private List<Z14AdminDashboardPlayer> _allPlayers = new();
    private List<SharedAdminLog> _allEvents = new();
    private List<Z14AdminDashboardCommandInfo> _allowedCommands = new();
    private HashSet<string> _allowedCommandNames = new();
    private bool _eventsPaused;
    private TabContainer _tabContainer = null!;

    private List<string> _commandLineMatches = new();
    private int _commandLineMatchIndex = -1;
    private string _commandLineMatchPrefix = string.Empty;

    public event Action<PlayerAction>? OnPlayerAction;
    public event Action<string>? OnFeatureCommand;
    public event Action? OnRefresh;

    public Z14AdminDashboardWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = "Z14 Admin Dashboard";
        SetSize = new Vector2(1200, 800);
        MinSize = new Vector2(900, 650);

        _tabContainer = new TabContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _tabContainer.AddChild(MakeOverviewTab());
        _tabContainer.AddChild(MakePlayersTab());
        _tabContainer.AddChild(MakeMapsTab());
        _tabContainer.AddChild(MakeZ14ControlsTab());
        _tabContainer.AddChild(MakeStalkerTab());
        _tabContainer.AddChild(MakeLiveEventsTab());
        _tabContainer.AddChild(MakeAlertsTab());
        _tabContainer.AddChild(MakeAHelpTab());
        _tabContainer.AddChild(MakeMentorhelpTab());
        _tabContainer.AddChild(MakeAdminToolsTab());

        Contents.AddChild(_tabContainer);
    }

    #region Tab construction

    private Control MakeOverviewTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Overview");

        var main = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        main.AddChild(MakeServerInfoSection());
        main.AddChild(MakeQuickActionsSection());
        main.AddChild(MakeCommandPaletteSection());

        scroll.AddChild(main);
        return scroll;
    }

    private Control MakeServerInfoSection()
    {
        var info = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var grid = new GridContainer
        {
            Columns = 6,
            HSeparationOverride = 10,
            VSeparationOverride = 5,
            HorizontalExpand = true,
        };

        grid.AddChild(MakeLabel("Round:"));
        _roundLabel = MakeValue();
        grid.AddChild(_roundLabel);

        grid.AddChild(MakeLabel("Duration:"));
        _durationLabel = MakeValue();
        grid.AddChild(_durationLabel);

        grid.AddChild(MakeLabel("Run Level:"));
        _runLevelLabel = MakeValue();
        grid.AddChild(_runLevelLabel);

        grid.AddChild(MakeLabel("Players:"));
        _playerCountLabel = MakeValue();
        grid.AddChild(_playerCountLabel);

        grid.AddChild(MakeLabel("Admins:"));
        _adminCountLabel = MakeValue();
        grid.AddChild(_adminCountLabel);

        info.AddChild(grid);

        var flagsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };
        flagsBox.AddChild(MakeLabel("Admin flags:"));
        _flagContainer = new WrapContainer
        {
            LayoutAxis = Axis.Horizontal,
            SeparationOverride = 5,
            CrossSeparationOverride = 3,
            HorizontalExpand = true,
        };
        flagsBox.AddChild(_flagContainer);
        info.AddChild(flagsBox);

        return MakeSection("Server / Round", info);
    }

    private Control MakeQuickActionsSection()
    {
        _actionButtons = new WrapContainer
        {
            LayoutAxis = Axis.Horizontal,
            SeparationOverride = 5,
            CrossSeparationOverride = 5,
        };

        AddActionButton(_actionButtons, "Admin Logs", "adminlogs", AdminFlags.Logs);
        AddActionButton(_actionButtons, "Ban List", "banlistall", AdminFlags.Ban);
        AddActionButton(_actionButtons, "AHelp", "openahelp", AdminFlags.Adminhelp, true);
        AddActionButton(_actionButtons, "Admin Activity", "adminactivity", AdminFlags.Logs);
        AddActionButton(_actionButtons, "Refresh", null, AdminFlags.Admin, custom: _ => OnRefresh?.Invoke());

        return MakeSection("Quick Actions", _actionButtons);
    }

    private Control MakeCommandPaletteSection()
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        _commandSearch = new LineEdit
        {
            PlaceHolder = "Search commands...",
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        _commandSearch.OnTextChanged += _ => ApplyCommandFilter();
        _commandSearch.OnTabComplete += OnCommandSearchTabComplete;
        box.AddChild(_commandSearch);

        _commandList = new ItemList
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 150),
        };
        _commandList.OnItemSelected += OnCommandSelected;
        box.AddChild(_commandList);

        var commandBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };
        _commandLine = new HistoryLineEdit
        {
            PlaceHolder = "Enter a console command...",
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        _commandLine.OnTextEntered += _ => RunCommandLine();
        _commandLine.OnTextChanged += OnCommandLineTextChanged;
        _commandLine.OnTabComplete += OnCommandLineTabComplete;
        commandBox.AddChild(_commandLine);

        var runButton = new Button { Text = "Run" };
        runButton.OnPressed += _ => RunCommandLine();
        commandBox.AddChild(runButton);

        box.AddChild(commandBox);

        _commandHint = new RichTextLabel
        {
            HorizontalExpand = true,
            Text = "Enter a console command and press Enter. Use Tab to autocomplete, Up/Down for history.",
        };
        box.AddChild(_commandHint);

        return MakeSection("Command Palette", box);
    }

    private Control MakePlayersTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Players");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        _playerSearch = new LineEdit
        {
            PlaceHolder = "Search players (ckey, character, job)...",
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        _playerSearch.OnTextChanged += _ => ApplyPlayerFilter();
        box.AddChild(_playerSearch);

        _playerList = new ItemList
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 200),
        };
        _playerList.OnItemSelected += OnPlayerSelected;
        box.AddChild(_playerList);

        var hint = new Label
        {
            Text = "Select a player to open the action panel (panel, logs, ban, whitelist, stash).",
            StyleClasses = { StyleClass.LabelSubText },
        };
        box.AddChild(hint);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeMapsTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Maps");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        _mapList = new ItemList
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 200),
        };
        box.AddChild(_mapList);

        var actionBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
        };
        _teleportMapButton = new Button { Text = "Teleport to map" };
        _teleportMapButton.OnPressed += OnTeleportToMap;
        actionBox.AddChild(_teleportMapButton);
        box.AddChild(actionBox);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeZ14ControlsTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Z14 Controls");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        var featureBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };

        AddArgCommand(featureBox, "Map Radiation (list/enable/disable)", "z14mapradiation list", "z14mapradiation", "list");
        AddArgCommand(featureBox, "Trigger Anomaly Migration", "z14anomigrate", "z14anomigrate", "");
        AddArgCommand(featureBox, "Trigger Supply Drop", "z14supplydrop", "z14supplydrop", "");
        AddArgCommand(featureBox, "List Personal Caches", "z14listcaches", "z14listcaches", "");
        AddArgCommand(featureBox, "Clear Personal Cache", "z14clearcache <id>", "z14clearcache", "");
        AddArgCommand(featureBox, "Spawn Mutant Lair", "z14spawnlair", "z14spawnlair", "");
        AddArgCommand(featureBox, "Z14 Info", "z14admininfo", "z14admininfo", "");

        box.AddChild(MakeSection("Feature Controls", featureBox));

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeStalkerTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Stalker");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        var warBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(warBox, "War Zone Info (zones)", "zones", "st_warzoneinfo", "zones", AdminFlags.Admin);
        AddArgCommand(warBox, "War Zone Info (bands)", "bands", "st_warzoneinfo", "bands", AdminFlags.Admin);
        AddArgCommand(warBox, "War Zone Info (factions)", "factions", "st_warzoneinfo", "factions", AdminFlags.Admin);
        AddArgCommand(warBox, "War Zone Admin", "setpoints band <id> <points>", "st_warzoneadmin", "", AdminFlags.Admin);
        box.AddChild(MakeSection("War Zone", warBox));

        var anomalyBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(anomalyBox, "Anomaly Get Active", "", "st_anomaly_generation_get_active", "", AdminFlags.Host);
        AddArgCommand(anomalyBox, "Anomaly Get Data UID", "", "st_anomaly_generation_get_data_uid", "", AdminFlags.Host);
        AddArgCommand(anomalyBox, "Anomaly Clear", "mapId", "st_anomaly_generation_clear", "", AdminFlags.Host);
        AddArgCommand(anomalyBox, "Anomaly Start", "mapId protoId", "st_anomaly_generation_start", "", AdminFlags.Host);
        box.AddChild(MakeSection("Anomaly Generation", anomalyBox));

        var stashBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(stashBox, "Clear Stash", "username", "clear_stash", "", AdminFlags.Ban);
        AddArgCommand(stashBox, "Persistent Craft Reset", "username", "st_pcraft_reset", "", AdminFlags.Host);
        AddArgCommand(stashBox, "Persistent Craft Reset Offline", "userId characterName", "st_pcraft_reset_offline", "", AdminFlags.Host);
        AddArgCommand(stashBox, "Persistent Craft Reset All", "confirm", "st_pcraft_reset_all", "", AdminFlags.Host);
        box.AddChild(MakeSection("Stash / Crafting", stashBox));

        var toolsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(toolsBox, "Trash Cleanup", "seconds", "request_trash_cleanup", "60", AdminFlags.Spawn);
        AddArgCommand(toolsBox, "Set Character Changeable", "username changeable slot", "st_set_character_changeable", "", AdminFlags.Admin);
        AddArgCommand(toolsBox, "Regenerate Sniper Map", "", "st_sniper_regenerate_map", "", AdminFlags.Host);
        AddArgCommand(toolsBox, "All Entities Info", "name", "allentsinfo", "", AdminFlags.Spawn);
        AddArgCommand(toolsBox, "All Prototypes", "id", "all_prototype", "", AdminFlags.Host);
        AddArgCommand(toolsBox, "Reload Grouped Portals", "", "reload_grouped", "", AdminFlags.Debug);
        AddArgCommand(toolsBox, "Delayed Restart", "seconds", "delayed_restart", "60", AdminFlags.Round);
        AddArgCommand(toolsBox, "Set Characteristic Level", "uid type level", "characteristic_set_levels", "", AdminFlags.Host);
        AddArgCommand(toolsBox, "Get Characteristic Level", "uid type", "characteristic_get_levels", "", AdminFlags.Host);
        AddArgCommand(toolsBox, "Add Character Marking", "username slot markingId [colors...]", "st_character_add_marking", "", AdminFlags.Host);
        box.AddChild(MakeSection("Stalker Tools", toolsBox));

        var balanceBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(balanceBox, "Balance Shop Armor", "", "st_balance:shoparmor", "", AdminFlags.Round);
        AddArgCommand(balanceBox, "Balance Shop Guns", "", "st_balance:shopguns", "", AdminFlags.Round);
        AddArgCommand(balanceBox, "Balance Guns", "", "st_balance:guns", "", AdminFlags.Round);
        AddArgCommand(balanceBox, "Balance Armor", "", "st_balance:armor", "", AdminFlags.Round);
        box.AddChild(MakeSection("Balance", balanceBox));

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeLiveEventsTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Live Events");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
        };
        _eventFilter = new LineEdit
        {
            PlaceHolder = "Filter events...",
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        _eventFilter.OnTextChanged += _ => ApplyEventFilter();
        controls.AddChild(_eventFilter);

        _pauseEventsButton = new Button { Text = "Pause" };
        _pauseEventsButton.OnPressed += _ =>
        {
            _eventsPaused = !_eventsPaused;
            _pauseEventsButton.Text = _eventsPaused ? "Resume" : "Pause";
            if (!_eventsPaused)
                ApplyEventFilter();
        };
        controls.AddChild(_pauseEventsButton);

        _clearEventsButton = new Button { Text = "Clear" };
        _clearEventsButton.OnPressed += _ =>
        {
            _allEvents.Clear();
            ApplyEventFilter();
            ApplyAlertFilter();
        };
        controls.AddChild(_clearEventsButton);

        box.AddChild(controls);

        _eventCountLabel = new RichTextLabel
        {
            HorizontalExpand = true,
            Text = "Counts: none",
        };
        box.AddChild(_eventCountLabel);

        _eventOutput = new OutputPanel
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 250),
        };
        box.AddChild(_eventOutput);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeAlertsTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Alerts");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        _alertCountLabel = new Label();
        box.AddChild(_alertCountLabel);

        _alertOutput = new OutputPanel
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            MinSize = new Vector2(0, 250),
        };
        box.AddChild(_alertOutput);

        var hint = new Label
        {
            Text = "High/Extreme impact events and anti-cheat alerts.",
            StyleClasses = { StyleClass.LabelSubText },
        };
        box.AddChild(hint);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeAHelpTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "AHelp");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        var openAHelp = new Button { Text = "Open AHelp" };
        openAHelp.OnPressed += _ => _consoleHost.ExecuteCommand("openahelp");
        box.AddChild(openAHelp);

        AddArgCommand(box, "AHelp Transcript", "username", "ahelptranscript", "", AdminFlags.Adminhelp);
        AddArgCommand(box, "Player Logs", "username or userId", "playerlogs", "", AdminFlags.Logs);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeMentorhelpTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Mentorhelp");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var controller = IoCManager.Resolve<IUserInterfaceManager>().GetUIController<MentorHelpUIController>();
        var control = new MentorHelpControl(controller)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        box.AddChild(control);

        scroll.AddChild(box);
        return scroll;
    }

    private Control MakeAdminToolsTab()
    {
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false,
        };
        TabContainer.SetTabTitle(scroll, "Admin Tools");

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(6),
            HorizontalExpand = true,
        };

        var roundGrid = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(roundGrid, "Restart Round", "", "restartround", "", AdminFlags.Round);
        AddArgCommand(roundGrid, "Restart Round Now", "", "restartroundnow", "", AdminFlags.Round);
        AddArgCommand(roundGrid, "End Round", "", "endround", "", AdminFlags.Round);
        AddArgCommand(roundGrid, "Toggle Late Join", "true/false", "toggledisallowlatejoin", "true", AdminFlags.Round);
        AddArgCommand(roundGrid, "Force Map", "map prototype", "forcemap", "", AdminFlags.Round);
        AddArgCommand(roundGrid, "Set Game Preset", "preset [rounds] [decoy]", "setgamepreset", "", AdminFlags.Round);
        AddArgCommand(roundGrid, "List Game Maps", "", "listgamemaps", "", AdminFlags.Round);
        box.AddChild(MakeSection("Round", roundGrid));

        var serverGrid = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(serverGrid, "Panic Bunker", "", "panicbunker", "", AdminFlags.Server);
        AddArgCommand(serverGrid, "Panic Bunker Disable With Admins", "", "panicbunker_disable_with_admins", "", AdminFlags.Server);
        AddArgCommand(serverGrid, "Panic Bunker Enable Without Admins", "", "panicbunker_enable_without_admins", "", AdminFlags.Server);
        AddArgCommand(serverGrid, "Set MOTD", "message", "set-motd", "", AdminFlags.Moderator);
        AddArgCommand(serverGrid, "Set Weather", "mapId proto [seconds]", "weather", "0 Storm 300", AdminFlags.Fun);
        box.AddChild(MakeSection("Server", serverGrid));

        var chatGrid = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(chatGrid, "Set OOC", "", "setooc", "", AdminFlags.Admin);
        AddArgCommand(chatGrid, "Set LOOC", "", "setlooc", "", AdminFlags.Admin);
        AddArgCommand(chatGrid, "Admin Chat", "message", "asay", "", AdminFlags.Adminchat);
        AddArgCommand(chatGrid, "Show Rules", "", "showrules", "", AdminFlags.Admin);
        box.AddChild(MakeSection("Chat", chatGrid));

        var logsGrid = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
        };
        AddArgCommand(logsGrid, "Admin Activity", "", "adminactivity", "", AdminFlags.Logs);
        AddArgCommand(logsGrid, "Door Logs", "", "doorlogs", "", AdminFlags.Logs);
        AddArgCommand(logsGrid, "Z14 Info", "", "z14admininfo", "", AdminFlags.Admin);
        AddArgCommand(logsGrid, "War Zone Info", "", "warzoneinfo", "", AdminFlags.Admin);
        box.AddChild(MakeSection("Logs / Info", logsGrid));

        scroll.AddChild(box);
        return scroll;
    }

    #endregion

    #region Helpers

    private static Control MakeSection(string title, Control content)
    {
        var panel = new PanelContainer
        {
            StyleClasses = { StyleClass.BackgroundPanel },
        };

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
            Margin = new Thickness(8),
            HorizontalExpand = true,
        };

        box.AddChild(new Label
        {
            Text = title,
            StyleClasses = { StyleClass.LabelHeading },
        });
        box.AddChild(content);

        panel.AddChild(box);
        return panel;
    }

    private static Label MakeLabel(string text)
    {
        return new Label { Text = text, StyleClasses = { StyleClass.LabelSubText } };
    }

    private static Label MakeValue()
    {
        return new Label { Text = "-" };
    }

    private void AddActionButton(Control container, string text, string? command, AdminFlags flag, bool client = false, Action<BaseButton.ButtonEventArgs>? custom = null)
    {
        var button = new Button
        {
            Text = text,
            Disabled = false,
        };
        _buttonFlags[button] = flag;

        if (custom != null)
        {
            button.OnPressed += custom;
        }
        else if (client && !string.IsNullOrEmpty(command))
        {
            button.OnPressed += _ => _consoleHost.ExecuteCommand(command!);
        }
        else if (!string.IsNullOrEmpty(command))
        {
            button.OnPressed += _ => OnFeatureCommand?.Invoke(command!);
        }

        container.AddChild(button);
    }

    private void AddArgCommand(BoxContainer container, string labelText, string placeholder, string command, string defaultValue, AdminFlags flag = AdminFlags.Admin)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            HorizontalExpand = true,
        };

        var label = new RichTextLabel
        {
            Text = labelText,
            HorizontalExpand = true,
        };
        row.AddChild(label);

        var inputRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var input = new LineEdit
        {
            PlaceHolder = placeholder,
            Text = defaultValue,
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        inputRow.AddChild(input);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            var arg = input.Text.Trim();
            var full = string.IsNullOrEmpty(arg) ? command : $"{command} {arg}";
            OnFeatureCommand?.Invoke(full);
        };
        inputRow.AddChild(button);

        row.AddChild(inputRow);
        container.AddChild(row);
    }

    #endregion

    #region Event handlers

    private void OnPlayerSelected(ItemList.ItemListSelectedEventArgs args)
    {
        if (args.ItemIndex < 0 || args.ItemIndex >= _playerList.Count)
            return;

        var item = _playerList[args.ItemIndex];
        if (item.Metadata is not Z14AdminDashboardPlayer player)
            return;

        var window = new Z14PlayerActionWindow(player, _flags, (action, extra) => OnPlayerAction?.Invoke(new PlayerAction(action, player.UserId, player.Name, extra)));
        window.OpenCentered();
    }

    private void OnCommandSelected(ItemList.ItemListSelectedEventArgs args)
    {
        if (args.ItemIndex < 0 || args.ItemIndex >= _commandList.Count)
            return;

        var item = _commandList[args.ItemIndex];
        if (item.Metadata is not Z14AdminDashboardCommandInfo command)
            return;

        _commandLine.Text = command.Name + " ";
        _commandLine.GrabKeyboardFocus();
        ApplyCommandHint(_commandLine.Text);
    }

    private void OnTeleportToMap(BaseButton.ButtonEventArgs args)
    {
        var selected = _mapList.GetSelected().FirstOrDefault();
        if (selected?.Metadata is not Z14AdminDashboardMap map)
            return;

        OnFeatureCommand?.Invoke($"tp 0 0 {map.MapId}");
    }

    private void RunCommandLine()
    {
        var text = _commandLine.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        OnFeatureCommand?.Invoke(text);
        _commandLine.Text = string.Empty;
    }

    private void OnCommandLineTextChanged(LineEdit.LineEditEventArgs args)
    {
        _commandLineMatchIndex = -1;
        _commandLineMatchPrefix = string.Empty;
        ApplyCommandHint(args.Text);
    }

    private void OnCommandLineTabComplete(LineEdit.LineEditEventArgs args)
    {
        var text = _commandLine.Text;
        var firstSpace = text.IndexOf(' ');
        var prefix = firstSpace == -1 ? text : text.Substring(0, firstSpace);
        var rest = firstSpace == -1 ? string.Empty : text.Substring(firstSpace);

        if (string.IsNullOrEmpty(prefix))
            return;

        if (_commandLineMatchIndex == -1 || _commandLineMatchPrefix != prefix)
        {
            _commandLineMatchPrefix = prefix;
            _commandLineMatchIndex = 0;
            _commandLineMatches = _allowedCommands
                .Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Name)
                .OrderBy(x => x.ToLowerInvariant())
                .ToList();
        }
        else if (_commandLineMatches.Count > 0)
        {
            _commandLineMatchIndex = (_commandLineMatchIndex + 1) % _commandLineMatches.Count;
        }

        if (_commandLineMatches.Count == 0)
            return;

        var selected = _commandLineMatches[_commandLineMatchIndex];
        var newText = string.IsNullOrEmpty(rest) ? selected : selected + rest;
        _commandLine.Text = newText;
        _commandLine.CursorPosition = selected.Length;
        ApplyCommandHint(newText);
    }

    private void OnCommandSearchTabComplete(LineEdit.LineEditEventArgs args)
    {
        var filter = _commandSearch.Text.Trim();
        if (string.IsNullOrEmpty(filter))
            return;

        var match = _allowedCommands
            .FirstOrDefault(c => c.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return;

        _commandLine.Text = match.Name + " ";
        _commandLine.GrabKeyboardFocus();
        ApplyCommandHint(_commandLine.Text);
    }

    #endregion

    #region Update logic

    public void UpdateState(Z14AdminDashboardState state)
    {
        _roundLabel.Text = state.RoundId.ToString();
        _durationLabel.Text = state.RoundDuration.ToString("hh\\:mm\\:ss");
        _runLevelLabel.Text = state.RunLevel;
        _playerCountLabel.Text = state.PlayerCount.ToString();
        _adminCountLabel.Text = state.AdminCount.ToString();
        _flags = (AdminFlags)state.Flags;
        UpdateFlagContainer();

        _allowedCommands = state.AllowedCommands;
        _allowedCommandNames = new HashSet<string>(_allowedCommands.Select(c => c.Name.ToLowerInvariant()));

        UpdateActionButtonFlags();

        _allPlayers = state.Players;
        ApplyPlayerFilter();
        UpdateMapList(state.Maps);
        UpdateEventCounts(state.EventCounts);

        _allEvents = state.RecentEvents;
        ApplyEventFilter();
        ApplyAlertFilter();

        ApplyCommandFilter();
        ApplyCommandHint(_commandLine.Text);
    }

    public void AddEvents(List<SharedAdminLog> events)
    {
        UpdateEvents(events, false);
    }

    private void UpdateActionButtonFlags()
    {
        foreach (var (button, flag) in _buttonFlags)
        {
            button.Disabled = !_adminManager.HasFlag(flag);
        }
    }

    private void UpdateFlagContainer()
    {
        _flagContainer.RemoveAllChildren();
        if (_flags == AdminFlags.None)
        {
            _flagContainer.AddChild(new Label { Text = "-" });
            return;
        }

        var names = _flags.ToString().Split(", ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
            _flagContainer.AddChild(new Label { Text = name });
        }
    }

    private void UpdatePlayerList(List<Z14AdminDashboardPlayer> players)
    {
        _allPlayers = players;
        ApplyPlayerFilter();
    }

    private void ApplyPlayerFilter()
    {
        _playerList.Clear();

        var filter = _playerSearch?.Text.Trim() ?? string.Empty;
        var filterLower = filter.ToLowerInvariant();

        foreach (var player in _allPlayers)
        {
            var charPart = player.CharacterName != null ? $" ({player.CharacterName})" : string.Empty;
            var adminPart = player.IsAdmin ? " [Admin]" : string.Empty;
            var label = $"{player.Name}{charPart}{adminPart}";

            if (!string.IsNullOrEmpty(filter))
            {
                var job = player.Job?.ToLowerInvariant() ?? string.Empty;
                if (!label.ToLowerInvariant().Contains(filterLower)
                    && !player.Name.ToLowerInvariant().Contains(filterLower)
                    && !player.UserId.ToString().ToLowerInvariant().Contains(filterLower)
                    && !job.Contains(filterLower))
                {
                    continue;
                }
            }

            var item = _playerList.AddItem(label);
            item.Metadata = player;
            item.TooltipText = $"Ckey: {player.Name}\nUserId: {player.UserId}\nCharacter: {player.CharacterName ?? "none"}\nJob: {player.Job ?? "none"}\nAdmin: {player.IsAdmin}";
        }
    }

    private void UpdateMapList(List<Z14AdminDashboardMap> maps)
    {
        _mapList.Clear();

        foreach (var map in maps)
        {
            var item = _mapList.AddItem($"{map.Name}: {map.GridCount} grids, {map.EntityCount} entities");
            item.Metadata = map;
            item.TooltipText = $"MapId: {map.MapId}";
        }
    }

    private void UpdateEventCounts(Dictionary<string, int> counts)
    {
        var text = "Counts: ";
        if (counts.Count == 0)
        {
            text += "none";
        }
        else
        {
            text += string.Join(", ", counts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        _eventCountLabel.Text = text;
    }

    private void UpdateEvents(List<SharedAdminLog> events, bool replace)
    {
        if (replace)
        {
            _allEvents.Clear();
            _allEvents.AddRange(events);
        }
        else
        {
            if (_eventsPaused)
                return;

            _allEvents.AddRange(events);

            if (_allEvents.Count > 500)
            {
                _allEvents.RemoveRange(0, _allEvents.Count - 500);
            }
        }

        ApplyEventFilter();
        ApplyAlertFilter();
    }

    private void ApplyEventFilter()
    {
        _eventOutput.Clear();

        var filter = _eventFilter?.Text.Trim() ?? string.Empty;
        var filterLower = filter.ToLowerInvariant();

        foreach (var ev in _allEvents)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                var message = ev.Message.ToLowerInvariant();
                var type = ev.Type.ToString().ToLowerInvariant();
                if (!message.Contains(filterLower) && !type.Contains(filterLower))
                    continue;
            }

            AddEventEntry(_eventOutput, ev);
        }
    }

    private void ApplyAlertFilter()
    {
        _alertOutput.Clear();

        foreach (var ev in _allEvents)
        {
            if (!IsAlertEvent(ev))
                continue;

            AddEventEntry(_alertOutput, ev);
        }

        _alertCountLabel.Text = $"Alerts: {_allEvents.Count(IsAlertEvent)}";
    }

    private static bool IsAlertEvent(SharedAdminLog ev)
    {
        return ev.Impact >= LogImpact.High || ev.Type == LogType.AdminAlert;
    }

    private void AddEventEntry(OutputPanel output, SharedAdminLog ev)
    {
        var text = $"[{ev.Date:HH:mm:ss}] [{ev.Impact}] {ev.Type}: {ev.Message}";
        var msg = new FormattedMessage();
        msg.PushColor(GetColorForImpact(ev.Impact));
        msg.AddText(text);
        msg.Pop();
        output.AddMessage(msg);
    }

    private static Color GetColorForImpact(LogImpact impact)
    {
        return impact switch
        {
            LogImpact.Extreme => Color.Red,
            LogImpact.High => Color.Orange,
            LogImpact.Medium => Color.Yellow,
            LogImpact.Low => Color.LightGray,
            _ => Color.White,
        };
    }

    private void ApplyCommandFilter()
    {
        _commandList.Clear();

        var filter = _commandSearch?.Text.Trim() ?? string.Empty;
        var filterLower = filter.ToLowerInvariant();

        foreach (var command in _allowedCommands)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                if (!command.Name.ToLowerInvariant().Contains(filterLower)
                    && !command.Description.ToLowerInvariant().Contains(filterLower))
                {
                    continue;
                }
            }

            var item = _commandList.AddItem(command.Name);
            item.TooltipText = $"{command.Description}\n\n{command.Help}\n\nRequired flags: {FormatFlags(command.Flags)}";
            item.Metadata = command;
        }
    }

    private void ApplyCommandHint(string text)
    {
        var firstWord = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        if (string.IsNullOrEmpty(firstWord))
        {
            _commandHint.Text = "Enter a console command and press Enter. Use Tab to autocomplete, Up/Down for history.";
            return;
        }

        var match = _allowedCommands.FirstOrDefault(c => c.Name.Equals(firstWord, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            _commandHint.Text = "Enter a console command and press Enter. Use Tab to autocomplete, Up/Down for history.";
            return;
        }

        var msg = new FormattedMessage();
        msg.PushColor(Color.LightGray);
        msg.AddText(match.Help);
        msg.Pop();
        msg.PushNewline();
        msg.AddText($"Required flags: {FormatFlags(match.Flags)}");
        _commandHint.SetMessage(msg);
    }

    private static string FormatFlags(uint[] flags)
    {
        if (flags.Length == 0)
            return "any";

        return string.Join(", ", flags.Select(f => ((AdminFlags)f).ToString()));
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _inputManager.UIKeyBindStateChanged += OnUIKeyBindStateChanged;
    }

    protected override void ExitedTree()
    {
        _inputManager.UIKeyBindStateChanged -= OnUIKeyBindStateChanged;
        base.ExitedTree();
    }

    private bool OnUIKeyBindStateChanged(BoundKeyEventArgs args)
    {
        if (!IsOpen || args.State != BoundKeyState.Down)
            return false;

        if (args.Function == EngineKeyFunctions.GuiTabNavigateNext)
        {
            _tabContainer.CurrentTab = Math.Min(_tabContainer.ChildCount - 1, _tabContainer.CurrentTab + 1);
            return true;
        }

        if (args.Function == EngineKeyFunctions.GuiTabNavigatePrev)
        {
            _tabContainer.CurrentTab = Math.Max(0, _tabContainer.CurrentTab - 1);
            return true;
        }

        return false;
    }

    #endregion
}
