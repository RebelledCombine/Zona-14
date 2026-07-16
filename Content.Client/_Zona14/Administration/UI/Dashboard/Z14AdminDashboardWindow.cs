// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Stylesheets;
using Content.Shared._Stalker.Characteristics;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Client._Zona14.Administration.UI.MentorHelp;
using Content.Client._Zona14.UserInterface.Systems.MentorHelp;
using Content.Shared._Stalker.Anomaly.Prototypes;
using Content.Shared._Stalker.Bands;
using Content.Shared._Stalker.WarZone;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Weather;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14AdminDashboardWindow : DefaultWindow
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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
    private Label _statusLabel = null!;

    private Button _pauseEventsButton = null!;
    private Button _clearEventsButton = null!;
    private Button _teleportMapButton = null!;

    private WrapContainer _actionButtons = null!;
    private readonly Dictionary<Button, AdminFlags> _buttonFlags = new();

    private AdminFlags _flags;
    private List<Z14AdminDashboardPlayer> _allPlayers = new();
    private List<Z14AdminDashboardMap> _allMaps = new();
    private List<SharedAdminLog> _allEvents = new();
    private List<Z14AdminDashboardCommandInfo> _allowedCommands = new();
    private HashSet<string> _allowedCommandNames = new();
    private bool _eventsPaused;
    private TabContainer _tabContainer = null!;

    private readonly List<OptionButton> _playerDropdowns = new();
    private readonly List<OptionButton> _mapDropdowns = new();

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
        _tabContainer.AddChild(MakeLiveEventsTab());
        _tabContainer.AddChild(MakeAlertsTab());
        _tabContainer.AddChild(MakeAHelpTab());
        _tabContainer.AddChild(MakeMentorhelpTab());

        _statusLabel = new Label
        {
            Text = "Ready",
            StyleClasses = { StyleClass.LabelSubText },
            HorizontalExpand = true,
            Margin = new Thickness(4, 2),
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 0,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        root.AddChild(_tabContainer);
        root.AddChild(_statusLabel);
        Contents.AddChild(root);
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
        AddActionButton(_actionButtons, "Refresh", null, AdminFlags.Admin, custom: _ =>
        {
            SetStatus("Refreshing...");
            OnRefresh?.Invoke();
        });

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
            SetStatus(_eventsPaused ? "Events paused" : "Events resumed");
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
            SetStatus("Event feed cleared");
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
        openAHelp.OnPressed += _ =>
        {
            SetStatus("Opening AHelp...");
            _consoleHost.ExecuteCommand("openahelp");
        };
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

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
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
            button.OnPressed += _ =>
            {
                SetStatus($"Running {command}...");
                _consoleHost.ExecuteCommand(command!);
            };
        }
        else if (!string.IsNullOrEmpty(command))
        {
            button.OnPressed += _ =>
            {
                SetStatus($"Running {command}...");
                OnFeatureCommand?.Invoke(command!);
            };
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
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        inputRow.AddChild(button);

        row.AddChild(inputRow);
        container.AddChild(row);
    }

    private void AddCharacteristicCommand(BoxContainer container, string labelText, bool setLevel, string command, AdminFlags flag)
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

        var uidInput = new LineEdit
        {
            PlaceHolder = "uid",
            HorizontalExpand = true,
            MinSize = new Vector2(80, 0),
        };
        inputRow.AddChild(uidInput);

        var typeNames = Enum.GetNames<CharacteristicType>();
        var typeDropdown = new OptionButton { MinWidth = 120 };
        foreach (var name in typeNames)
        {
            typeDropdown.AddItem(name);
        }

        if (typeNames.Length > 0)
            typeDropdown.SelectId(0);

        inputRow.AddChild(typeDropdown);

        LineEdit? levelInput = null;
        if (setLevel)
        {
            levelInput = new LineEdit
            {
                PlaceHolder = "level",
                MinSize = new Vector2(60, 0),
            };
            inputRow.AddChild(levelInput);
        }

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            var uid = uidInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(uid))
            {
                SetStatus($"{labelText}: enter a target UID");
                return;
            }

            var type = typeNames[typeDropdown.SelectedId];
            var full = setLevel
                ? $"{command} {uid} {type} {levelInput?.Text.Trim()}"
                : $"{command} {uid} {type}";

            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        inputRow.AddChild(button);

        row.AddChild(inputRow);
        container.AddChild(row);
    }

    private void AddToggleCommand(Control container, string labelText, string command, AdminFlags flag,
        string trueValue = "true", string falseValue = "false")
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = labelText, HorizontalExpand = true };
        row.AddChild(label);

        var dropdown = new OptionButton { MinWidth = 80 };
        dropdown.AddItem(trueValue, 0);
        dropdown.AddItem(falseValue, 1);
        dropdown.SelectId(0);
        row.AddChild(dropdown);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            var value = dropdown.SelectedId == 0 ? trueValue : falseValue;
            var full = $"{command} {value}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddPlayerDropdownCommand(Control container, string labelText, string command, AdminFlags flag,
        string? extraPlaceholder = null, string? defaultExtra = null)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = labelText, HorizontalExpand = true };
        row.AddChild(label);

        var playerDropdown = new OptionButton { MinWidth = 150 };
        _playerDropdowns.Add(playerDropdown);
        PopulatePlayerDropdown(playerDropdown);
        row.AddChild(playerDropdown);

        LineEdit? extraInput = null;
        if (extraPlaceholder != null)
        {
            extraInput = new LineEdit
            {
                PlaceHolder = extraPlaceholder,
                Text = defaultExtra ?? string.Empty,
                MinSize = new Vector2(80, 0),
            };
            row.AddChild(extraInput);
        }

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (playerDropdown.SelectedMetadata is not Z14AdminDashboardPlayer selected)
            {
                SetStatus($"{labelText}: select a player");
                return;
            }

            var extra = extraInput?.Text.Trim();
            var full = string.IsNullOrWhiteSpace(extra) ? $"{command} {selected.Name}" : $"{command} {selected.Name} {extra}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddMapDropdownCommand(Control container, string labelText, string command, AdminFlags flag,
        string? extraPlaceholder = null, string? defaultExtra = null)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = labelText, HorizontalExpand = true };
        row.AddChild(label);

        var mapDropdown = new OptionButton { MinWidth = 180 };
        _mapDropdowns.Add(mapDropdown);
        PopulateMapDropdown(mapDropdown);
        row.AddChild(mapDropdown);

        LineEdit? extraInput = null;
        if (extraPlaceholder != null)
        {
            extraInput = new LineEdit
            {
                PlaceHolder = extraPlaceholder,
                Text = defaultExtra ?? string.Empty,
                MinSize = new Vector2(80, 0),
            };
            row.AddChild(extraInput);
        }

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (mapDropdown.SelectedMetadata is not Z14AdminDashboardMap selected)
            {
                SetStatus($"{labelText}: select a map");
                return;
            }

            var extra = extraInput?.Text.Trim();
            var full = string.IsNullOrWhiteSpace(extra) ? $"{command} {selected.MapId}" : $"{command} {selected.MapId} {extra}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddWeatherCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Set Weather", HorizontalExpand = true };
        row.AddChild(label);

        var mapDropdown = new OptionButton { MinWidth = 140 };
        _mapDropdowns.Add(mapDropdown);
        PopulateMapDropdown(mapDropdown);
        row.AddChild(mapDropdown);

        var protoDropdown = new OptionButton { MinWidth = 120 };
        PopulatePrototypeDropdown<WeatherPrototype>(protoDropdown, "null");
        row.AddChild(protoDropdown);

        var secondsInput = new LineEdit
        {
            PlaceHolder = "seconds",
            Text = "300",
            MinSize = new Vector2(60, 0),
        };
        row.AddChild(secondsInput);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (mapDropdown.SelectedMetadata is not Z14AdminDashboardMap map)
            {
                SetStatus("Set Weather: select a map");
                return;
            }

            var protoId = protoDropdown.SelectedMetadata as string ?? "null";
            var full = $"weather {map.MapId} {protoId} {secondsInput.Text.Trim()}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddAnomalyStartCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Anomaly Start", HorizontalExpand = true };
        row.AddChild(label);

        var mapDropdown = new OptionButton { MinWidth = 140 };
        _mapDropdowns.Add(mapDropdown);
        PopulateMapDropdown(mapDropdown);
        row.AddChild(mapDropdown);

        var protoDropdown = new OptionButton { MinWidth = 180 };
        PopulatePrototypeDropdown<STAnomalyGenerationOptionsPrototype>(protoDropdown);
        row.AddChild(protoDropdown);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (mapDropdown.SelectedMetadata is not Z14AdminDashboardMap map)
            {
                SetStatus("Anomaly Start: select a map");
                return;
            }

            var protoId = protoDropdown.SelectedMetadata as string;
            if (string.IsNullOrWhiteSpace(protoId))
            {
                SetStatus("Anomaly Start: select a prototype");
                return;
            }

            var full = $"st_anomaly_generation_start {map.MapId} {protoId}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddSetCharacterChangeableCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Set Character Changeable", HorizontalExpand = true };
        row.AddChild(label);

        var playerDropdown = new OptionButton { MinWidth = 140 };
        _playerDropdowns.Add(playerDropdown);
        PopulatePlayerDropdown(playerDropdown);
        row.AddChild(playerDropdown);

        var changeableDropdown = new OptionButton { MinWidth = 80 };
        changeableDropdown.AddItem("true", 0);
        changeableDropdown.AddItem("false", 1);
        changeableDropdown.SelectId(0);
        row.AddChild(changeableDropdown);

        var slotInput = new LineEdit
        {
            PlaceHolder = "slot",
            Text = "0",
            MinSize = new Vector2(50, 0),
        };
        row.AddChild(slotInput);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (playerDropdown.SelectedMetadata is not Z14AdminDashboardPlayer player)
            {
                SetStatus("Set Character Changeable: select a player");
                return;
            }

            var changeable = changeableDropdown.SelectedId == 0 ? "true" : "false";
            var full = $"st_set_character_changeable {player.Name} {changeable} {slotInput.Text.Trim()}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddAddMarkingCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Add Character Marking", HorizontalExpand = true };
        row.AddChild(label);

        var playerDropdown = new OptionButton { MinWidth = 120 };
        _playerDropdowns.Add(playerDropdown);
        PopulatePlayerDropdown(playerDropdown);
        row.AddChild(playerDropdown);

        var slotInput = new LineEdit
        {
            PlaceHolder = "slot",
            Text = "0",
            MinSize = new Vector2(40, 0),
        };
        row.AddChild(slotInput);

        var markingInput = new LineEdit
        {
            PlaceHolder = "markingId",
            MinSize = new Vector2(80, 0),
        };
        row.AddChild(markingInput);

        var colorsInput = new LineEdit
        {
            PlaceHolder = "colors",
            ToolTip = "Optional colors, e.g. #FF0000 #00FF00",
            MinSize = new Vector2(80, 0),
        };
        row.AddChild(colorsInput);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            if (playerDropdown.SelectedMetadata is not Z14AdminDashboardPlayer player)
            {
                SetStatus("Add Character Marking: select a player");
                return;
            }

            var marking = markingInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(marking))
            {
                SetStatus("Add Character Marking: enter a markingId");
                return;
            }

            var colors = colorsInput.Text.Trim();
            var full = string.IsNullOrWhiteSpace(colors)
                ? $"st_character_add_marking {player.Name} {slotInput.Text.Trim()} {marking}"
                : $"st_character_add_marking {player.Name} {slotInput.Text.Trim()} {marking} {colors}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddMapRadiationCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Map Radiation", HorizontalExpand = true };
        row.AddChild(label);

        var actionDropdown = new OptionButton { MinWidth = 90 };
        var actions = new[] { "list", "enable", "disable", "interval", "damage" };
        for (var i = 0; i < actions.Length; i++)
        {
            actionDropdown.AddItem(actions[i], i);
            actionDropdown.SetItemMetadata(actionDropdown.ItemCount - 1, actions[i]);
        }

        actionDropdown.SelectId(0);
        row.AddChild(actionDropdown);

        var mapDropdown = new OptionButton { MinWidth = 140 };
        _mapDropdowns.Add(mapDropdown);
        PopulateMapDropdown(mapDropdown);
        row.AddChild(mapDropdown);

        var valueInput = new LineEdit
        {
            PlaceHolder = "value",
            ToolTip = "For interval: seconds. For damage: type amount (e.g. Radiation 5).",
            MinSize = new Vector2(100, 0),
        };
        row.AddChild(valueInput);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            var action = actionDropdown.SelectedMetadata as string ?? "list";
            if (action == "list")
            {
                SetStatus("Running z14mapradiation list...");
                OnFeatureCommand?.Invoke("z14mapradiation list");
                return;
            }

            if (mapDropdown.SelectedMetadata is not Z14AdminDashboardMap map)
            {
                SetStatus("Map Radiation: select a map");
                return;
            }

            var value = valueInput.Text.Trim();
            string full;
            if (action == "enable")
                full = $"z14mapradiation {map.MapId} enabled true";
            else if (action == "disable")
                full = $"z14mapradiation {map.MapId} enabled false";
            else if (string.IsNullOrWhiteSpace(value))
            {
                SetStatus($"Map Radiation: enter a value for {action}");
                return;
            }
            else
                full = $"z14mapradiation {map.MapId} {action} {value}";

            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddWarzoneSetPointsCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Warzone Set Points", HorizontalExpand = true };
        row.AddChild(label);

        var typeDropdown = new OptionButton { MinWidth = 80 };
        typeDropdown.AddItem("band", 0);
        typeDropdown.SetItemMetadata(typeDropdown.ItemCount - 1, "band");
        typeDropdown.AddItem("faction", 1);
        typeDropdown.SetItemMetadata(typeDropdown.ItemCount - 1, "faction");
        typeDropdown.SelectId(0);
        row.AddChild(typeDropdown);

        var protoDropdown = new OptionButton { MinWidth = 140 };
        row.AddChild(protoDropdown);

        var pointsInput = new LineEdit
        {
            PlaceHolder = "points",
            Text = "0",
            MinSize = new Vector2(60, 0),
        };
        row.AddChild(pointsInput);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;

        void RefreshProtoDropdown()
        {
            var selectedType = typeDropdown.SelectedMetadata as string ?? "band";
            protoDropdown.Clear();
            if (selectedType == "band")
                PopulatePrototypeDropdown<STBandPrototype>(protoDropdown);
            else
                PopulatePrototypeDropdown<NpcFactionPrototype>(protoDropdown);
        }

        typeDropdown.OnItemSelected += _ => RefreshProtoDropdown();
        RefreshProtoDropdown();

        button.OnPressed += _ =>
        {
            var type = typeDropdown.SelectedMetadata as string ?? "band";
            var protoId = protoDropdown.SelectedMetadata as string;
            if (string.IsNullOrWhiteSpace(protoId))
            {
                SetStatus("Warzone Set Points: select a prototype");
                return;
            }

            var full = $"st_warzoneadmin setpoints {type} {protoId} {pointsInput.Text.Trim()}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddWarzoneSetOwnerCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Warzone Set Owner", HorizontalExpand = true };
        row.AddChild(label);

        var zoneDropdown = new OptionButton { MinWidth = 120 };
        PopulatePrototypeDropdown<STWarZonePrototype>(zoneDropdown);
        row.AddChild(zoneDropdown);

        var ownerTypeDropdown = new OptionButton { MinWidth = 80 };
        ownerTypeDropdown.AddItem("band", 0);
        ownerTypeDropdown.SetItemMetadata(ownerTypeDropdown.ItemCount - 1, "band");
        ownerTypeDropdown.AddItem("faction", 1);
        ownerTypeDropdown.SetItemMetadata(ownerTypeDropdown.ItemCount - 1, "faction");
        ownerTypeDropdown.SelectId(0);
        row.AddChild(ownerTypeDropdown);

        var ownerProtoDropdown = new OptionButton { MinWidth = 120 };
        row.AddChild(ownerProtoDropdown);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;

        void RefreshOwnerProtoDropdown()
        {
            var selectedType = ownerTypeDropdown.SelectedMetadata as string ?? "band";
            ownerProtoDropdown.Clear();
            if (selectedType == "band")
                PopulatePrototypeDropdown<STBandPrototype>(ownerProtoDropdown);
            else
                PopulatePrototypeDropdown<NpcFactionPrototype>(ownerProtoDropdown);
        }

        ownerTypeDropdown.OnItemSelected += _ => RefreshOwnerProtoDropdown();
        RefreshOwnerProtoDropdown();

        button.OnPressed += _ =>
        {
            var zoneId = zoneDropdown.SelectedMetadata as string;
            var ownerType = ownerTypeDropdown.SelectedMetadata as string ?? "band";
            var ownerProtoId = ownerProtoDropdown.SelectedMetadata as string;
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                SetStatus("Warzone Set Owner: select a zone");
                return;
            }

            if (string.IsNullOrWhiteSpace(ownerProtoId))
            {
                SetStatus("Warzone Set Owner: select an owner prototype");
                return;
            }

            var full = $"st_warzoneadmin setowner {zoneId} {ownerType} {ownerProtoId}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void AddWarzoneClearOwnerCommand(Control container, AdminFlags flag)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };

        var label = new Label { Text = "Warzone Clear Owner", HorizontalExpand = true };
        row.AddChild(label);

        var zoneDropdown = new OptionButton { MinWidth = 180 };
        PopulatePrototypeDropdown<STWarZonePrototype>(zoneDropdown);
        row.AddChild(zoneDropdown);

        var button = new Button
        {
            Text = "Run",
            Disabled = !_adminManager.HasFlag(flag),
        };
        _buttonFlags[button] = flag;
        button.OnPressed += _ =>
        {
            var zoneId = zoneDropdown.SelectedMetadata as string;
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                SetStatus("Warzone Clear Owner: select a zone");
                return;
            }

            var full = $"st_warzoneadmin clearowner {zoneId}";
            SetStatus($"Running {full}...");
            OnFeatureCommand?.Invoke(full);
        };
        row.AddChild(button);

        container.AddChild(row);
    }

    private void PopulatePlayerDropdown(OptionButton dropdown)
    {
        dropdown.Clear();
        var id = 0;
        foreach (var player in _allPlayers.OrderBy(p => p.Name))
        {
            dropdown.AddItem(player.Name, id);
            dropdown.SetItemMetadata(dropdown.ItemCount - 1, player);
            id++;
        }
    }

    private void PopulateMapDropdown(OptionButton dropdown)
    {
        dropdown.Clear();
        var id = 0;
        foreach (var map in _allMaps.OrderBy(m => m.Name))
        {
            var label = $"{map.Name} ({map.MapId})";
            dropdown.AddItem(label, id);
            dropdown.SetItemMetadata(dropdown.ItemCount - 1, map);
            id++;
        }
    }

    private void PopulatePrototypeDropdown<T>(OptionButton dropdown, string? noneItem = null) where T : class, IPrototype
    {
        dropdown.Clear();
        var id = 0;
        if (noneItem != null)
        {
            dropdown.AddItem($"(none)", id);
            dropdown.SetItemMetadata(dropdown.ItemCount - 1, "null");
            id++;
        }

        foreach (var proto in _prototypeManager.EnumeratePrototypes<T>().OrderBy(p => p.ID))
        {
            dropdown.AddItem(proto.ID, id);
            dropdown.SetItemMetadata(dropdown.ItemCount - 1, proto.ID);
            id++;
        }
    }

    private void UpdatePlayerDropdowns()
    {
        foreach (var dropdown in _playerDropdowns)
        {
            var selectedName = dropdown.ItemCount > 0 && dropdown.SelectedMetadata is Z14AdminDashboardPlayer prev ? prev.Name : null;
            PopulatePlayerDropdown(dropdown);
            if (selectedName != null)
            {
                for (var i = 0; i < dropdown.ItemCount; i++)
                {
                    if (dropdown.GetItemMetadata(i) is Z14AdminDashboardPlayer p && p.Name == selectedName)
                    {
                        dropdown.Select(i);
                        break;
                    }
                }
            }
        }
    }

    private void UpdateMapDropdowns()
    {
        foreach (var dropdown in _mapDropdowns)
        {
            var selectedId = dropdown.ItemCount > 0 && dropdown.SelectedMetadata is Z14AdminDashboardMap prev ? (int?)prev.MapId : null;
            PopulateMapDropdown(dropdown);
            if (selectedId != null)
            {
                for (var i = 0; i < dropdown.ItemCount; i++)
                {
                    if (dropdown.GetItemMetadata(i) is Z14AdminDashboardMap m && m.MapId == selectedId)
                    {
                        dropdown.Select(i);
                        break;
                    }
                }
            }
        }
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

        var cmd = $"tp 0 0 {map.MapId}";
        SetStatus($"Running {cmd}...");
        OnFeatureCommand?.Invoke(cmd);
    }

    private void RunCommandLine()
    {
        var text = _commandLine.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        UserInterfaceManager.ClickSound();
        SetStatus($"Running {text}...");
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
        UpdatePlayerDropdowns();
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
        _allMaps = maps;
        _mapList.Clear();

        foreach (var map in maps)
        {
            var item = _mapList.AddItem($"{map.Name}: {map.GridCount} grids, {map.EntityCount} entities");
            item.Metadata = map;
            item.TooltipText = $"MapId: {map.MapId}";
        }

        UpdateMapDropdowns();
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
