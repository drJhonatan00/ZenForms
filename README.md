<div align="center">

# ZenForms Core

</div>

**A themed WinForms component library for .NET Framework 4.7.2**

<img width="983" height="697" alt="1000049830" src="https://github.com/user-attachments/assets/e533da73-eaef-436d-8eb7-77921ae0a6cf" />



ZenForms Core is a collection of individually implemented Windows Forms controls. It combines themed replacements for everyday WinForms widgets with experimental visual controls and practical specialist tools. The library targets Windows and the .NET Framework; it is not a cross-platform UI toolkit.

All public controls and helper types are in the **`ZenForms.Core`** namespace. Each control has its own `.cs` file in the `ClassLibrary` project. The checked-in solution is `ClassLibrary.sln`.

> **Build status:** the project and source files have been assembled for Visual Studio, but this environment did not have the Windows .NET Framework targeting pack/MSBuild needed to compile and run the WinForms project. Build and exercise the controls on Windows before shipping them.

## Contents

- [Requirements and installation](#requirements-and-installation)
- [First steps and shared theme](#first-steps-and-shared-theme)
- [Button](#button)
- [Everyday themed WinForms controls](#everyday-themed-winforms-controls)
- [Menus and tool strips](#menus-and-tool-strips)
- [Charts, indicators and presentation controls](#charts-indicators-and-presentation-controls)
- [Productivity and interaction controls](#productivity-and-interaction-controls)
- [Advanced data and developer tools](#advanced-data-and-developer-tools)
- [Media and visualisation tools](#media-and-visualisation-tools)
- [Creative and visual tools](#creative-and-visual-tools)
- [Animated controls](#animated-controls)
- [Events and threading](#events-and-threading)
- [Support types](#support-types)

## Requirements and installation

- Windows with Visual Studio 2022 (or a compatible Visual Studio installation) and the **.NET desktop development** workload.
- The **.NET Framework 4.7.2 Developer/Targeting Pack**.

### Build the DLL and add it to a form

1. Extract the supplied project archive.
2. Open `ClassLibrary.sln` in Visual Studio and allow NuGet package restore to finish.
3. Select **Release** and **Build Solution**. The class library assembly is named `ClassLibrary.dll`; the usual output is `ClassLibrary\bin\Release\ClassLibrary.dll`.
4. In a WinForms application, add a project reference to `ClassLibrary` or browse to the built DLL.
5. To use the Toolbox, build the library, right-click the WinForms Toolbox, choose **Choose Items…**, browse to `ClassLibrary.dll`, and select the controls you want. If the Toolbox was already open, close/reopen it or refresh it after rebuilding.
6. Add controls to the form in the designer, or create them in code. Set `Dock`, `Anchor`, and `Size` just as you would for ordinary WinForms controls.

Example project code:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using ZenForms.Core;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        ZenTheme.Apply(this); // Optional: apply the shared palette to the form tree.

        ZenButtonBase save = new ZenButtonBase
        {
            Text = "Save",
            Location = new Point(24, 24),
            Size = new Size(130, 42)
        };
        save.Click += (sender, args) => SaveDocument();
        Controls.Add(save);
    }

    private void SaveDocument()
    {
        // Your application logic.
    }
}
```

Use `using ZenForms.Core;` to import the controls. For types such as `Color`, `Point`, `DataTable`, or `Task`, also import the corresponding .NET namespaces (`System.Drawing`, `System.Data`, and so on).

## First steps and shared theme

`ZenTheme` exposes the shared dark palette: `Background`, `Surface`, `SurfaceRaised`, `Primary`, `Accent`, `Foreground`, `Muted`, `Border`, and `Danger`, plus `DefaultFont`. These are public static fields. You can adjust them before creating controls if you want a different palette. `ZenTheme.Apply(Control root)` recursively sets background, foreground, and font on a control tree; it is optional, and a control's own painting may still use particular theme colours.

```csharp
ZenTheme.Primary = Color.FromArgb(110, 80, 245);
ZenTheme.Accent = Color.FromArgb(35, 205, 170);
ZenTheme.Apply(this);
```

Most controls inherit the normal WinForms API of their base class. Two composite controls expose a small, explicit wrapper API: `ZenTextBox` and `ZenComboBox`; their public members are described below. Controls with richer behaviour have their own properties, methods, and events.

## Button

<img width="213" height="128" alt="1000049778" src="https://github.com/user-attachments/assets/6dceab8b-3bda-487f-a0c9-1d0d703d9104" />


The library includes a simple, stylish, and highly customizable button, called ZenButtonBase.


```csharp
var button = new ZenButtonBase
{
    Text = "Confirm",
    CornerRadius = 12,
    BorderWidth = 1,
    NormalBackColor = Color.FromArgb(22, 105, 81),
    HoverBackColor = Color.FromArgb(34, 145, 105),
    TextColor = Color.White,
    Location = new Point(20, 20),
    Size = new Size(140, 42)
};
button.Click += (s, e) => Confirm();
```

Shared `ZenButtonBase` customisation: `CornerRadius`, `NormalBackColor`, `HoverBackColor`, `TextColor`, `BorderColor`, and `BorderWidth`. Individual derived buttons supply their own starting colours.

### `ZenRippleButton`

A standalone button with a smooth radial ripple that starts at the click point, a hover glow, and keyboard support. It derives from `Control` (not from `ZenButtonBase`), so use the normal `Text` and `Click` members plus the properties below.

```csharp
var ripple = new ZenRippleButton
{
    Text = "Deploy",
    CornerRadius = 14,
    RippleColor = Color.FromArgb(120, 255, 255, 255),
    HoverGlowColor = ZenTheme.Accent,
    Size = new Size(150, 44)
};
ripple.Click += (s, e) => Deploy();
```

Properties: `CornerRadius`, `BorderWidth`, `BorderColor`, `NormalBackColor`, `HoverBackColor`, `TextColor`, `RippleColor`, `RippleDuration` (milliseconds), `HoverGlowColor`, and `HoverGlowIntensity`.

## Everyday themed WinForms controls

These are intended as familiar WinForms counterparts with ZenForms styling. Unless a wrapper is noted, use the inherited .NET control API (`Text`, `Items`, `SelectedIndex`, `DataSource`, `Nodes`, and similar members) and the standard WinForms events.

### Input and selection

| Control | Purpose and typical use |
|---|---|
| `ZenTextBox` | Captioned text input with a themed border and native placeholder support. Set `LabelText`, `Placeholder`, `Text`, and optionally `UsePasswordChar`. `InnerTextBox` exposes the contained native `TextBox` when an application needs a native event or property that the wrapper does not surface. |
| `ZenComboBox` | Compact themed drop-down. Its explicit wrapper members are `Items`, `DropDownStyle`, `SelectedIndex`, and `Text`; `Items` is the inner `ComboBox.ObjectCollection`. |
| `ZenCheckBox` | Themed check box; use `Checked`, `Text`, and `CheckedChanged` as with a normal `CheckBox`. |
| `ZenRadioButton` | Themed radio selection; use normal `RadioButton` properties and group related choices in the same parent container. |
| `ZenCheckedListBox` | Themed list of independently checked items; inherits `CheckedListBox`. |
| `ZenListBox` | Themed single/multiple-selection list; inherits `ListBox`. |
| `ZenMaskedTextBox` | Themed input constrained by a mask; set `Mask` and handle `TextChanged`/`MaskInputRejected` as appropriate. |
| `ZenNumericUpDown` | Themed numeric editor; configure `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, and `Value`. |
| `ZenDateTimePicker` | Themed date/time picker; configure `Value`, `Format`, `MinDate`, and `MaxDate`. |
| `ZenTrackBar` | Themed slider; configure `Minimum`, `Maximum`, `Value`, and handle `Scroll` or `ValueChanged`. |
| `ZenToggle` | Custom switch with `Checked` and `CheckedChanged`; it can be toggled by click or Space when focused. |
| `ZenSpringSlider` | Slider whose thumb moves with spring physics. See [Specialised input controls](#specialised-input-controls). |
| `ZenTagEditor` | Chip-style tag input with add/remove events. See [Specialised input controls](#specialised-input-controls). |
| `ZenShortcutRecorder` | Click-to-record keyboard shortcut field. See [Specialised input controls](#specialised-input-controls). |

`ZenTextBox` example:

```csharp
var customer = new ZenTextBox
{
    LabelText = "Customer name",
    Placeholder = "Type a name…",
    Width = 280
};
customer.TextChanged += (s, e) => ValidateCustomer(customer.Text);
```

`ZenComboBox` example:

```csharp
var choice = new ZenComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
choice.Items.AddRange(new object[] { "Small", "Medium", "Large" });
choice.SelectedIndex = 0;
```

### Specialised input controls

#### `ZenSpringSlider`

A numeric slider with a spring-animated thumb: when `Value` changes, the thumb eases toward it with a slight overshoot. Configure the range with `Minimum`/`Maximum`, snap values with `Step`, and tune the motion with `SpringStrength` (0.01–1.0) and `Damping` (0.1–0.99). `Value` is the logical value; `VisualValue` is the current animated position. Drag the thumb, click the track, or use the arrow keys, Page Up/Down, Home and End.

```csharp
zenSpringSlider1.Minimum = 0;
zenSpringSlider1.Maximum = 200;
zenSpringSlider1.Step = 5;
zenSpringSlider1.ValueFormat = "0";
zenSpringSlider1.ValueChanged += (s, e) => volumeLabel.Text = zenSpringSlider1.Value.ToString("0");
zenSpringSlider1.AnimationCompleted += (s, e) => Debug.WriteLine("Settled");
```

Appearance: `ShowValueLabel`, `ShowMinMaxLabels`, `ShowTrackGlow`, `CornerRadius`, `TrackHeight`, `ThumbRadius`, `SidePadding`, `TrackColor`, `AccentColor`, `ThumbColor`, and `TextColor`. `ValueFormat` is a standard numeric format string.

#### `ZenTagEditor`

A chip-style tag input. Type text and press **Enter** to add a tag; press **Backspace** in an empty input to remove the last tag, or click the remove mark on a chip. `PreventDuplicates` (default `true`) rejects repeated tags and `MaximumTags` limits the count (`-1` means no limit).

```csharp
zenTagEditor1.MaximumTags = 8;
zenTagEditor1.AddTag("winforms");
zenTagEditor1.AddTag("dark-theme");
zenTagEditor1.TagAdded += (s, e) => Debug.WriteLine("Added: " + e.Tag);
zenTagEditor1.TagRemoved += (s, e) => Debug.WriteLine("Removed: " + e.Tag);
```

`AddTag(tag)` and `RemoveTag(tag)` return `true` when the collection changed. `ClearTags()` removes everything. `Tags` exposes the underlying `List<string>`; use the methods above to change it so events and layout stay in sync. Appearance: `CornerRadius`, `ChipRadius`, `ChipColor`, and `ChipHoverColor`.

#### `ZenShortcutRecorder`

Click the control and press a key combination to record it; **Backspace** or **Esc** clears the shortcut. Read or set `ShortcutKeys` (a `System.Windows.Forms.Keys` value that includes modifiers), check `HasShortcut`, and handle `ShortcutChanged`. Set `AllowModifierOnly` to accept combinations such as Ctrl+Shift without a main key. `EmptyText` is the placeholder and `CornerRadius` styles the field.

```csharp
zenShortcutRecorder1.ShortcutChanged += (s, e) =>
    shortcutLabel.Text = zenShortcutRecorder1.HasShortcut
        ? zenShortcutRecorder1.ShortcutKeys.ToString()
        : "(none)";
```

The control only records and displays the shortcut; it does not register it with the system. Your application decides how to act on it.

### Containers, text, and data

| Control | Purpose and typical use |
|---|---|
| `ZenPanel` | Themed `Panel` for grouping child controls. |
| `ZenFlowLayoutPanel` | Themed `FlowLayoutPanel` for responsive flow of child controls. |
| `ZenTableLayoutPanel` | Themed `TableLayoutPanel` for row/column layouts. |
| `ZenSplitContainer` | Themed split view with two resizable panels. |
| `ZenTabControl` | Themed tabbed pages; add `TabPage` instances to `TabPages`. |
| `ZenGroupBox` | Themed titled group container; use `Text` as its caption. |
| `ZenCard` | Rounded panel with a subtle accent line. Add child controls through `Controls`; adjust `CornerRadius` and standard panel properties such as `Padding`. |
| `ZenLabel` | Themed text label; use normal `Label` properties. |
| `ZenLinkLabel` | Themed hyperlink label; handle `LinkClicked`. |
| `ZenRichTextBox` | Themed rich-text editor/viewer; inherits `RichTextBox`. |
| `ZenDataGridView` | Themed grid; bind with `DataSource` or populate `Rows`/`Columns` as for `DataGridView`. |
| `ZenTreeView` | Themed hierarchical tree; populate `Nodes` and handle `AfterSelect`. |
| `ZenSeparator` | Decorative divider for separating groups or sections. |
| `ZenBreadcrumb` | Neon breadcrumb navigation bar. See [`ZenBreadcrumb`](#zenbreadcrumb). |

```csharp
var card = new ZenCard { CornerRadius = 18, Padding = new Padding(16), Size = new Size(320, 150) };
card.Controls.Add(new ZenLabel { Text = "Account overview", AutoSize = true, Location = new Point(16, 16) });
```

#### `ZenBreadcrumb`

A navigation bar that shows a path as clickable segments. Fill `Segments` (a `ZenBreadcrumbSegmentCollection`, which behaves like a collection of strings) or call `AddSegment(text)`; `ClearSegments()` empties it. `SelectedIndex` is the active segment (`-1` when none) and `SelectedSegment` returns its text. `SegmentClicked` fires on mouse click or keyboard activation (Enter/Space) and supplies `Index` and `Text`. The left/right arrow keys, Home and End move between segments. `Separator` is the text drawn between segments and `CornerRadius` styles the bar.

```csharp
zenBreadcrumb1.AddSegment("Home");
zenBreadcrumb1.AddSegment("Projects");
zenBreadcrumb1.AddSegment("ZenForms");
zenBreadcrumb1.Separator = "›";
zenBreadcrumb1.SegmentClicked += (s, e) => NavigateTo(e.Index, e.Text);
```

## Menus and tool strips

- **`ZenMenuStrip`** is a themed `MenuStrip`; populate `Items` with `ToolStripMenuItem` objects.
- **`ZenContextMenuStrip`** is a themed context menu; assign it to a control's `ContextMenuStrip` property.
- **`ZenToolStrip`** provides a themed toolbar for buttons and other `ToolStripItem` objects.
- **`ZenStatusStrip`** is a themed status bar; add `ToolStripStatusLabel` items.
- **`ZenToolTip`** provides themed tooltips; use `SetToolTip(control, text)`.
- **`ZenMenuRenderer`** is the shared professional renderer used by the menu/strip controls. It is normally used internally; assign it to another `ToolStrip` only if you want to style a custom strip consistently.

```csharp
var menu = new ZenMenuStrip();
var file = new ToolStripMenuItem("File");
file.DropDownItems.Add("Exit", null, (s, e) => Close());
menu.Items.Add(file);
MainMenuStrip = menu;
Controls.Add(menu);
```

## Charts, indicators and presentation controls

### `ZenChart` and `ZenChartType`

<img width="1177" height="820" alt="1000049780" src="https://github.com/user-attachments/assets/334cd491-fffb-43ec-806f-2bdb2d5cc062" />


`ZenChart` wraps the .NET Framework `System.Windows.Forms.DataVisualization.Charting.Chart`. It supports `Line`, `Spline`, `Column`, `Bar`, `Area`, `Pie`, `Doughnut`, and `StackedColumn`, with named series, category/value points, optional legend, grid and line markers. Use `AddPoint(seriesName, category, value)` for labelled categories or `AddPoint(seriesName, value)` for a value-only point. A series is created automatically the first time `AddPoint` uses its name.

```csharp
zenChart1.ChartTitle = "Monthly sales";
zenChart1.ChartType = ZenChartType.Column;
zenChart1.ShowLegend = true;
zenChart1.ShowGrid = true;
zenChart1.AddPoint("2026", "Jan", 120);
zenChart1.AddPoint("2026", "Feb", 180);
zenChart1.AddPoint("2026", "Mar", 155);

// A second named series makes comparisons straightforward.
zenChart1.AddPoint("2025", "Jan", 95);
zenChart1.AddPoint("2025", "Feb", 140);
zenChart1.AddPoint("2025", "Mar", 130);
```

For a pie or doughnut chart, use one series and one point per slice:

```csharp
zenChart1.ClearSeries();
zenChart1.ChartType = ZenChartType.Doughnut;
zenChart1.AddPoint("Share", "Books", 30);
zenChart1.AddPoint("Share", "Music", 45);
zenChart1.AddPoint("Share", "Other", 25);
```

`SeriesColor` and `SecondSeriesColor` set the alternating series colours. `ClearData()` removes points but keeps the series; `ClearSeries()` removes the series as well. `InnerChart` exposes the underlying native chart for advanced configuration. The control uses the framework charting assembly already referenced by the project; it does not add a NuGet package.

### Progress, ratings, labels, and notices

- **`ZenProgressBar`** displays 0–100 progress. Set `Value`; set `ShowPercentage` to show the number inside the bar.
- **`ZenCircularProgress`** presents the same 0–100 range as a ring. Set `Value`, `ProgressColor`, `TrackColor`, and `Thickness`; `ShowPercentage` controls the centre label.
- **`ZenLiquidGauge`** displays 0–100 as a moving liquid level. Set `Value`, `LiquidColor`, `GlassColor`, and `Animate`.
- **`ZenLoadingSpinner`** is an animated loading indicator. Set `SpinnerColor`, `Speed` (timer interval in milliseconds), or `Spinning`.
- **`ZenMorphingGauge`** is a neon circular gauge with a configurable `Minimum`/`Maximum` range. Setting `Value` jumps to a value immediately; call `StartAnimation(target)` (or set `TargetValue` and call `StartAnimation()`) to morph smoothly from the current value, over `AnimationDuration` milliseconds. `StopAnimation()` halts it, `IsAnimating` reports its state, and `AnimationCompleted` fires at the end. Other options: `StrokeWidth`, `CenterText`, `ShowCenterText`, `ShowTarget` (marks the target on the ring), `Label`, `PulseGlow`, `TrackColor`, `ProgressColor`, `GlowColor`, `CenterTextColor`, and `LabelColor`.
- **`ZenOrbitLoader`** is a neon loader with satellites orbiting a core. It does not run until you call `Start()`; `Stop()` halts it and `IsRunning` reports the state. Configure `SatelliteCount`, `OrbitSpeed` (revolutions per second), `Clockwise`, `Indeterminate`, `OrbitRadius`, `SatelliteSize`, `CoreSize`, `AnimationInterval` (milliseconds), `CornerRadius`, and the colours `SurfaceColor`, `CoreColor`, `SatelliteColor`, `TextColor`, and `MutedColor`.
- **`ZenServiceMonitor`** is a status board for a list of services; see [`ZenServiceMonitor`](#zenservicemonitor).
- **`ZenRating`** is a mouse- and keyboard-selectable star rating. Set/read `Value` and `Maximum`; `ValueChanged` is raised when the value changes. `ActiveColor` and `InactiveColor` customise the stars.
- **`ZenBadge`** displays compact status text. Set its inherited `Text` and `Style` (`ZenBadgeStyle.Primary`, `Success`, `Warning`, `Danger`, or `Neutral`).
- **`ZenNotification`** is an inline message with `Text`, a `Type` (`Information`, `Success`, `Warning`, or `Error`), and `ShowCloseButton`. `Dismissed` fires when its close button is used.
- **`ZenAvatar`** draws a circular image or initials. Set `AvatarImage` to an `Image`, or set `Text` to a person's name for generated initials. `FillColor`, `OutlineColor`, and `OutlineWidth` tune its appearance.
- **`ZenHoloCard`** is a hover-reactive card with a light/parallax treatment. Set `Text`, `Subtitle`, `GlowColor`, and `CornerRadius`; it is decorative and does not host child controls.

```csharp
zenCircularProgress1.Value = 72;
zenRating1.Maximum = 5;
zenRating1.ValueChanged += (s, e) => ratingLabel.Text = zenRating1.Value + " / 5";
zenNotification1.Type = ZenNotificationType.Success;
zenNotification1.Text = "Changes saved";
```

## Productivity and interaction controls

### `ZenAccordion`

<img width="526" height="357" alt="1000049774" src="https://github.com/user-attachments/assets/22cc45aa-a16c-46f8-a538-bd537c8d9d41" />


A collapsible section with a header and a scrollable child panel. Set `Text`, `Expanded`, and `ContentHeight`. Add controls to `ContentPanel.Controls` (or `ContentControls`); `ExpandedChanged` reports expand/collapse changes.

```csharp
zenAccordion1.Text = "Advanced options";
zenAccordion1.ContentPanel.Controls.Add(new CheckBox { Text = "Enable diagnostics", AutoSize = true });
zenAccordion1.ExpandedChanged += (s, e) => UpdateLayout();
```

### `ZenHeatmapCalendar`

<img width="918" height="285" alt="1000049797" src="https://github.com/user-attachments/assets/040064d3-1c08-42a9-9b1e-7fbb55dc0a68" />


A year-at-a-glance activity calendar. Set `Year` to choose the displayed year, then call `SetValue(date, intensity)` for each day, where intensity is clamped to 0–4. `GetValue(date)` reads a day's level and `Clear()` removes all values. `LowColor`, `HighColor`, and `CellSize` customise the presentation. Hover over a day to see its date and level.

```csharp
zenHeatmapCalendar1.Year = new DateTime(2026, 1, 1);
zenHeatmapCalendar1.SetValue(new DateTime(2026, 10, 30), 4);
zenHeatmapCalendar1.SetValue(DateTime.Today, 2);
```

### `ZenCommandPalette`

A searchable local action list inspired by IDE command palettes. Add a label, optional shortcut text, and an `Action`; the control filters as the user types and runs the selected action with Enter/double-click.

```csharp
zenCommandPalette1.AddCommand("Save document", "Ctrl+S", () => SaveDocument());
zenCommandPalette1.AddCommand("Clear form", () => ClearForm());
zenCommandPalette1.CommandExecuted += (s, e) => statusLabel.Text = "Ran: " + e.Name;

// Add a shortcut in the host form, then open the palette.
private void Form1_KeyDown(object sender, KeyEventArgs e)
{
    if (e.Control && e.KeyCode == Keys.P)
    {
        zenCommandPalette1.Open();
        e.SuppressKeyPress = true;
    }
}
```

Other API: `RemoveCommand(name)` returns whether an item was removed; `ClearCommands()` removes all actions; `Open()` makes the control visible, clears the search and focuses the input. The shortcut string is descriptive text; the palette does not register that shortcut globally.

### `ZenKanbanBoard`

<img width="820" height="506" alt="1000049799" src="https://github.com/user-attachments/assets/7fa562ac-3d30-4956-b94a-0882d70159a4" />


A horizontal board with named columns and draggable cards. Create columns before adding cards. `CardMoved` supplies the card title and source/destination column names.

```csharp
zenKanbanBoard1.AddColumn("To do");
zenKanbanBoard1.AddColumn("In progress");
zenKanbanBoard1.AddColumn("Done");
zenKanbanBoard1.AddCard("To do", "Design the screen");
zenKanbanBoard1.AddCard("In progress", "Implement sign-in");
zenKanbanBoard1.CardMoved += (s, e) =>
    auditLog.Write(e.Card + ": " + e.FromColumn + " → " + e.ToColumn);
```

Additional methods: `RemoveColumn(name)`, `ClearCards(columnName)`, and `GetCardTitles(columnName)`. `CardColor` changes the card background.

### `ZenWorkflowCanvas`

<img width="1173" height="554" alt="1000049829" src="https://github.com/user-attachments/assets/aed8e5ff-2c69-4afc-9548-096ad3376aaf" />


A small visual workflow canvas. `AddNode` returns a node object; `Connect` links two nodes belonging to that canvas. Drag nodes to reposition them. `NodeSelected` returns the selected node. `RemoveNode` also removes its links; `ClearWorkflow()` clears all nodes and links.

```csharp
var start = zenWorkflowCanvas1.AddNode("Start", 40, 70);
var validate = zenWorkflowCanvas1.AddNode("Validate", "Check required fields", new Point(280, 70), Color.MediumPurple);
var finish = zenWorkflowCanvas1.AddNode("Complete", 520, 70);
zenWorkflowCanvas1.Connect(start, validate);
zenWorkflowCanvas1.Connect(validate, finish);
zenWorkflowCanvas1.NodeSelected += (s, e) => selectedNodeLabel.Text = e.Node.Title;
```

### `ZenTimeline`

<img width="480" height="234" alt="1000049824" src="https://github.com/user-attachments/assets/d9b28dae-5a33-40ae-a454-51d4101be337" />

A vertical event/milestone timeline. Add entries with a title, description, time label, and optionally a point colour. Click an entry to select it; `ItemSelected` supplies both its index and `ZenTimelineItem`. The mouse wheel scrolls a long timeline.

```csharp
zenTimeline1.AddItem("Order received", "The order entered the system.", "09:15");
zenTimeline1.AddItem("Payment approved", "Payment confirmed.", "09:18", ZenTheme.Accent);
zenTimeline1.ItemSelected += (s, e) => ShowDetails(e.Item.Title);
// To rebuild it:
zenTimeline1.ClearItems();
```

### `ZenPomodoroClock`

A visual focus/break timer. Defaults are 25 minutes of work, a 5-minute short break and a 15-minute long break. After every fourth work session it selects a long break. The next phase is announced but does not automatically start.

```csharp
zenPomodoroClock1.WorkMinutes = 25;
zenPomodoroClock1.ShortBreakMinutes = 5;
zenPomodoroClock1.LongBreakMinutes = 15;
zenPomodoroClock1.PhaseChanged += (s, e) => Notify(e.Phase.ToString());
zenPomodoroClock1.Start();
```

Methods: `Start()`, `Pause()`, `ResetClock()`, and `SkipPhase()`. Read `Phase` and `CompletedSessions` to show the current state. `ZenPomodoroPhase` values are `Work`, `ShortBreak`, and `LongBreak`.

### `ZenFileDropZone`

A drag-and-drop file intake area with a file picker, accepted-file list, and `FilesAdded` event. `ExtensionFilter` accepts patterns such as `"*.jpg;*.png"` or `"jpg;png"`. `Files` is a read-only list of accepted paths; `ClearFiles()` clears it.

```csharp
zenFileDropZone1.ExtensionFilter = "*.csv;*.tsv";
zenFileDropZone1.FilesAdded += (s, e) =>
{
    foreach (string path in e.Files) ImportFile(path);
};
// Files can also be supplied from application code:
zenFileDropZone1.AddFiles(new[] { @"C:\Data\customers.csv" });
```

### `ZenAgendaCalendar`

A monthly calendar with selectable days and coloured event markers. Add events with `AddEvent(date, title)`, `AddEvent(date, title, color)`, or `AddEvent(ZenAgendaEvent)`; `Events` is a read-only list and `ClearEvents()` removes them all. `DisplayMonth` chooses the visible month, `GoToPreviousMonth()`/`GoToNextMonth()` move it, and `SelectedDate` (a nullable `DateTime`) holds the chosen day. `DateSelected` supplies the date through `ZenDateSelectedEventArgs.Date`. `DateToolTipFormat` is a standard date format string for the hover tip and `CornerRadius` styles the surface.

Keyboard: the left/right arrows move one day, up/down move one week, and Page Up/Page Down change month.

```csharp
zenAgendaCalendar1.AddEvent(new DateTime(2026, 11, 12), "Sprint review", ZenTheme.Primary);
zenAgendaCalendar1.AddEvent(new DateTime(2026, 11, 20), "Release");
zenAgendaCalendar1.DateSelected += (s, e) => ShowAgendaFor(e.Date);
```

`ZenAgendaEvent` has `Date`, `Title`, and `Color` properties and can be constructed directly.

### `ZenTextEditor`

A compact rich-text editor with a built-in toolbar. It offers New, Open, Save, Save as and Print, Undo/Redo, Cut/Copy/Paste, Find and Replace, bold/italic/underline, a bulleted list, left/centre/right alignment, text colour and highlight colour, zoom, and an Insert Date/Time button.

```csharp
zenTextEditor1.OpenDocument(@"C:\Docs\notes.rtf");
zenTextEditor1.DocumentSaved += (s, e) => statusLabel.Text = "Saved: " + zenTextEditor1.DocumentPath;
zenTextEditor1.DocumentChanged += (s, e) => MarkDirty();
int replaced = zenTextEditor1.ReplaceAll("colour", "color");
```

Files: **`.rtf`**, **`.txt`** (UTF-8) and a **basic `.docx`** (formatted text, sizes, colours and paragraph alignment; it is not a full Word implementation). Other extensions are refused.

API: `NewDocument()`, `OpenDocument()` (shows a dialog) or `OpenDocument(path)`, `SaveDocument()` or `SaveDocument(path)`, `Undo()`, `Redo()`, `FindNext(text)` / `FindNext()`, `ReplaceAll(find, replacement)` (returns the number of replacements), and `InsertDateTime()`. Properties: `DocumentName`, `DocumentPath` (read-only), `ZoomFactor` (0.5–3.0), `AccentColor`, `IsReadOnly`, `ShowStatusBar`, and `InnerEditor`, which exposes the contained `RichTextBox` for anything the wrapper does not surface. Events: `DocumentChanged`, `DocumentSaved`, and `DocumentOpened`.

## Advanced data and developer tools

### `ZenLogConsole`

<img width="836" height="478" alt="1000049805" src="https://github.com/user-attachments/assets/b1ac11aa-87e8-42ac-87ef-7b2a5888e579" />


A colour-coded in-form log viewer with timestamps, a text filter, a Clear button, and a bounded history. Use `Write(message)` for Information or pass an explicit `ZenLogLevel` (`Trace`, `Information`, `Success`, `Warning`, `Error`). `MaxEntries`, `AutoScroll`, and `ShowTimestamp` control behaviour; `Entries` is a read-only view; `EntryAdded` fires when a record is written.

```csharp
zenLogConsole1.Write(ZenLogLevel.Information, "Application started");
zenLogConsole1.Write(ZenLogLevel.Success, "Database connected");
zenLogConsole1.Write(ZenLogLevel.Warning, "Cache is nearly full");
zenLogConsole1.Write(ZenLogLevel.Error, "Export failed");
zenLogConsole1.EntryAdded += (s, entry) => Debug.WriteLine(entry.Timestamp + " " + entry.Message);
```

`Write` marshals a call to the UI thread when invoked from a worker thread. It does not make arbitrary application state thread-safe; keep your own data access synchronised.

### `ZenDataProfiler`

Profiles a `DataTable` and shows the data type, missing values, distinct count, numeric minimum/maximum, and completeness for each column. Assign `SourceTable` to analyse a table, call `Analyze()` again after changing it, or use **Load CSV…** in the control. `Profiles` gives programmatic access to `ZenColumnProfile` values; `ProfilingCompleted` fires at the end.

```csharp
zenDataProfiler1.SourceTable = customerTable;
foreach (ZenColumnProfile column in zenDataProfiler1.Profiles)
{
    Debug.WriteLine(column.ColumnName + ": " + column.Completeness.ToString("0.0") + "% complete");
}
```

CSV import detects comma, semicolon, or tab delimiters and supports quoted fields. Imported values remain strings; profiling does not infer and convert CSV column types. Numeric min/max are calculated only for numeric `DataColumn` types.

### `ZenDiffViewer`

A side-by-side text comparison tool. Set `LeftText`/`RightText`, call `SetTexts(before, after)`, or use the two file buttons. Added and removed lines are highlighted; the header reports added, removed, and unchanged aligned lines. `ComparisonCompleted` supplies those counts.

```csharp
zenDiffViewer1.SetTexts(oldVersion, newVersion);
zenDiffViewer1.ComparisonCompleted += (s, e) =>
    resultLabel.Text = e.Added + " added, " + e.Removed + " removed";
```

The line-alignment algorithm uses an LCS table up to 1.5 million line-pair cells. Larger inputs use a lower-memory, position-by-position fallback, which is faster and more bounded but may not align moved blocks as helpfully.

### `ZenMarkdownView`

A lightweight, read-only Markdown preview rendered with the framework's `RichTextBox`. Set `Markdown` or call `SetMarkdown(text)`. The built-in formatter recognises line headings (`#`, `##`, `###`), bullet lines, numbered lines, `>` quotations, and fenced code blocks (triple backticks). It is intentionally not a full CommonMark engine: tables, images, nested lists, and Markdown link syntax are not fully rendered.

~~~csharp
zenMarkdownView1.Markdown = @"# Release notes

## Improvements
- Faster search
- Better keyboard navigation

> Restart the application to apply the update.

```csharp
var result = Calculate();
```";
~~~

### `ZenColorStudio`

An HSV colour picker with a HEX field and five generated harmony swatches. Read `SelectedColor` or `HexValue`, set `SelectedColor` in code, or listen to `SelectedColorChanged`. Clicking a harmony swatch selects it.

```csharp
zenColorStudio1.SelectedColorChanged += (s, e) =>
{
    previewPanel.BackColor = zenColorStudio1.SelectedColor;
    hexLabel.Text = zenColorStudio1.HexValue;
};
```

### `ZenApiWorkbench`

A visual workbench for sending and inspecting REST requests, similar in spirit to a small API client. It has a method selector (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`), a URL box, a header editor (one `Name: value` per line), a request-body editor, Send/Cancel/Copy buttons, and a response viewer with status, timing and size.

```csharp
zenApiWorkbench1.Method = "POST";
zenApiWorkbench1.Url = "https://api.example.com/items";
zenApiWorkbench1.SetHeader("Authorization", "Bearer " + token);
zenApiWorkbench1.RequestBody = "{ \"name\": \"Widget\" }";
zenApiWorkbench1.ResultReceived += (s, e) =>
    statusLabel.Text = (int)e.StatusCode + " in " + e.Duration.TotalMilliseconds.ToString("0") + " ms";
zenApiWorkbench1.RequestError += (s, e) => MessageBox.Show(e.Message);

await zenApiWorkbench1.SendAsync();
```

Members: `Method`, `Url`, `HeadersText`, `RequestBody`, `Timeout` (a `TimeSpan`; use `System.Threading.Timeout.InfiniteTimeSpan` for no limit), `SetHeader(name, value)`, `SendAsync()` / `SendAsync(CancellationToken)`, `Cancel()`, and `CopyResponse()`. After a request, read `LastStatusCode`, `LastDuration`, `ResponseText`, and `LastResponseTruncated`. `ResultReceived` supplies `StatusCode`, `ResponseText`, `Duration`, `ResponseSize`, and `ResponseTruncated`; `RequestError` supplies the `Exception` and its `Message`.

Only absolute `http://` and `https://` URLs are accepted, one request can run at a time, and response bodies are capped at 8 MiB (`ResponseTruncated` tells you when the cap was hit). Validation and network problems are reported through `RequestError`.

### `ZenDataFlowStudio`

A visual pipeline editor for a `DataTable`. Assign `Source`, add stages, and run `Apply()`; the source table is never modified. Stages run in order and each can be enabled, disabled, reordered or removed. Selecting a stage previews its output (up to 200 rows), and the result can be exported to CSV from the toolbar.

Stage kinds (`PipelineStepKind`):

| Kind | Expression format |
|---|---|
| `Filter` | A `DataView.RowFilter` expression, e.g. `Country = 'BR' AND Total > 100`. |
| `Sort` | A `DataView.Sort` expression, e.g. `Total DESC, Name`. |
| `SelectColumns` | Column names separated by commas; every name must exist. |
| `ComputedColumn` | A new column defined by a name, a CLR type and a `DataColumn.Expression`. |

```csharp
zenDataFlowStudio1.Source = ordersTable;
zenDataFlowStudio1.AddFilter("Total > 100");
zenDataFlowStudio1.AddComputedColumn("WithTax", "Total * 1.2", typeof(decimal));
zenDataFlowStudio1.AddSort("WithTax DESC");
zenDataFlowStudio1.SelectColumns("Id", "Customer", "WithTax");

DataTable result = zenDataFlowStudio1.Apply();
zenDataFlowStudio1.StepError += (s, e) => Debug.WriteLine(e.Step.Name + ": " + e.Step.Error);
```

Other API: `MoveStep(from, to)`, `SetStepEnabled(index, enabled)`, `Steps` (read-only list of `PipelineStep`, each with `Kind`, `Name`, `Expression`, `Enabled`, `Error`, `RowCount`, and `Preview`), `Result`, `LastRunSucceeded`, and `LastError`. Events: `SourceChanged`, `ResultChanged`, `StepChanged`, `StepError`, `StepCompleted`, `PipelineApplied`, and `PipelineFailed`. In the step list, Ctrl+Up/Down reorders, Space toggles and Delete removes the selected stage.

### `ZenRegexStudio`

An interactive regular-expression tester. Enter a `Pattern`, some `InputText` and an optional `Replacement`; matches are listed as you work and the replacement preview is shown. Toggle `IgnoreCase`, `Multiline`, and `Singleline`. Evaluation runs under a time limit (`TimeoutMilliseconds`, 10–5000, default 200) so catastrophic patterns cannot freeze the form.

```csharp
zenRegexStudio1.Pattern = @"(?<user>\w+)@(?<host>[\w.]+)";
zenRegexStudio1.InputText = "ana@example.com, bob@example.org";
zenRegexStudio1.IgnoreCase = true;
zenRegexStudio1.EvaluationCompleted += (s, e) =>
    status.Text = e.TimedOut ? "Timed out" : e.ErrorMessage ?? (e.MatchCount + " matches");
zenRegexStudio1.TestNow();
```

`TestNow()` runs the evaluation immediately (the toolbar button and Ctrl+Enter do the same), and `MatchCount` holds the last count. `EvaluationCompleted` supplies `MatchCount`, `ErrorMessage` (for an invalid pattern), and `TimedOut`.

### `ZenJsonExplorer`

A dark neon tree viewer for JSON. Set `JsonText`; objects and arrays become expandable nodes with value and type information. Invalid JSON does not throw: `ErrorText` describes the problem. Call `RefreshJson()` to re-parse. `NodeSelected` supplies the node's `Name`, `Value`, `TypeName`, and the underlying `TreeNode`. Appearance: `HeaderText`, `ShowHeader`, `GlowColor`, and `CornerRadius`.

```csharp
zenJsonExplorer1.HeaderText = "Response";
zenJsonExplorer1.JsonText = "{ \"name\": \"Ana\", \"roles\": [\"admin\", \"dev\"], \"active\": true }";
zenJsonExplorer1.NodeSelected += (s, e) => detailLabel.Text = e.Name + " (" + e.TypeName + ")";
```

The parser is `System.Web.Script.Serialization.JavaScriptSerializer`, so the project needs the `System.Web.Extensions` reference (already present in `ClassLibrary.csproj`).

### `ZenHexExplorer`

A hexadecimal viewer and editor. The grid shows an offset column, 16 editable hex byte columns and an ASCII column. Open a file with `OpenFile(path)`, edit bytes in place, and write back with `SaveFile(path)`. Saving writes a temporary file and then replaces the target, so a failed save does not leave a half-written file.

```csharp
zenHexExplorer1.OpenFile(@"C:\Data\firmware.bin");
zenHexExplorer1.NavigateToOffset(0x40);
long hit = zenHexExplorer1.FindHex("DE AD BE EF"); // -1 when not found
zenHexExplorer1.DirtyStateChanged += (s, e) => saveButton.Enabled = zenHexExplorer1.IsDirty;
```

Members: `OpenFile`, `SaveFile`, `FindHex(sequence)` (searches forward from the selection and wraps around; returns the offset or `-1`), `NavigateToOffset(offset)`, `FilePath`, `FileLength`, `SelectedOffset`, `IsDirty`, and `ReadOnly`. Events: `FileOpened`, `FileChanged` (after a save), `DirtyStateChanged`, and `SelectedOffsetChanged`. The toolbar has Open, Save, Go-to-offset and Search-hex controls, with Ctrl+O, Ctrl+S, Ctrl+G and Ctrl+F shortcuts.

**Limit:** the whole file is loaded into memory, and files larger than 64 MiB are refused.

### `ZenFormulaPlotter`

<img width="772" height="446" alt="1000049794" src="https://github.com/user-attachments/assets/50407e91-eabb-44c7-add2-67638e85bcc1" />


Plots a function of `x`. Type an expression and press Enter or click the plot button; the mouse wheel zooms around the cursor and dragging pans. Set `Formula` (default `sin(x)`) and `CurveColor`, call `Plot()` to redraw, and handle `FormulaError` (it supplies `Message`) for invalid expressions.

```csharp
zenFormulaPlotter1.Formula = "sin(x) + x^2 / 5";
zenFormulaPlotter1.CurveColor = ZenTheme.Accent;
zenFormulaPlotter1.FormulaError += (s, e) => statusLabel.Text = e.Message;
```

Supported syntax: numbers, the variable `x`, the constants `pi` and `e`, the operators `+ - * / ^` (including unary minus), parentheses, and the functions `sin`, `cos`, `tan`, `sqrt`, `abs`, `log` (base 10), `ln`, `exp`, `floor`, and `ceil`. The parser is built in; no scripting engine is involved, so the text is never executed as code.

### `ZenServiceMonitor`

A scrollable status board for a list of services. Each `ZenServiceItem` has a `Name`, a `State` (`ZenServiceState`: `Unknown`, `Healthy`, `Warning`, `Error`, `Offline`), a `LatencyMilliseconds` value and a `Message`. The control only displays what you give it; it does not probe anything. Your code runs the health checks and calls `UpdateService`.

```csharp
zenServiceMonitor1.AddService("API", ZenServiceState.Healthy, 42, "OK");
zenServiceMonitor1.AddService("Database", ZenServiceState.Warning, 310, "Slow queries");
zenServiceMonitor1.AddService("Mail relay", ZenServiceState.Offline, 0, "Connection refused");
zenServiceMonitor1.ServiceSelected += (s, e) => ShowDetails(e.Service);

// Later, after a health check:
zenServiceMonitor1.UpdateService("Database", ZenServiceState.Healthy, 55, "Recovered");
```

API: `AddService(...)` (individual values or a `ZenServiceItem`), `UpdateService(name, ...)` (returns `false` when the name is not found), `GetService(name)`, `ClearServices()`, `Services`, and `SelectedService`. Appearance: `RowHeight`, `CornerRadius`, and `EmptyText`. Select rows with the mouse or Up/Down/Home/End; the mouse wheel scrolls. Remember to call `UpdateService` from the UI thread (see [Events and threading](#events-and-threading)).

### `ZenCircuitLab`

A visual simulator for digital logic. Build a circuit from inputs, gates and outputs, wire them together, and toggle inputs to watch the outputs change. Gates are `AND`, `OR`, `XOR`, `NAND` (two inputs) and `NOT` (one input); an output has one input.

```csharp
var a = zenCircuitLab1.AddInput("A");
var b = zenCircuitLab1.AddInput("B");
var xor = zenCircuitLab1.AddGate(ZenCircuitLab.GateType.XOR, "XOR");
var sum = zenCircuitLab1.AddOutput("Sum");
zenCircuitLab1.Connect(a, xor);
zenCircuitLab1.Connect(b, xor);
zenCircuitLab1.Connect(xor, sum);

zenCircuitLab1.OutputChanged += (s, e) => Debug.WriteLine(e.Output.Name + " = " + e.Value);
zenCircuitLab1.SetInput("A", true);

ZenCircuitLab.TruthTable table = zenCircuitLab1.CreateTruthTable();
```

API: `AddInput(name[, initialValue])`, `AddGate(type[, name])`, `AddOutput(name)`, `Connect(source, destination[, inputPin])`, `SetInput(node)` / `SetInput(name, value)`, `Evaluate()`, `CreateTruthTable()`, `ClearCircuit()`, and `Nodes` (read-only). `Connect` refuses loops, wires into inputs, wires out of outputs, and connections that skip a pin; pins must be filled in order. `CreateTruthTable()` supports at most 8 inputs and returns `InputNames`, `OutputNames`, and `Rows` (each row holds the input values followed by the output values). Events: `OutputChanged` (`Output`, `Value`) and `CircuitError` (`Message`, `Node`).

The helper types `GateType`, `NodeKind`, `CircuitNode`, `TruthTable`, `OutputChangedEventArgs`, and `CircuitErrorEventArgs` are nested inside `ZenCircuitLab`. Click an input node on the canvas to toggle it, or use Tab and Space/Enter from the keyboard.

## Media and visualisation tools

### `ZenMediaPlayer`

<img width="1033" height="546" alt="1000049808" src="https://github.com/user-attachments/assets/0d637731-f0f1-4fba-a156-cf7568080c56" />

A compact MP3/MP4 player using the Windows Media Player ActiveX control. It includes open, play, pause, stop, seek, volume, time display, and full-screen controls. Use `Open(path)`, `Play()`, `Pause()`, `Stop()`, or `OpenFileDialogAndPlay()`. `Volume` and `FullScreen` are configurable; `FileName` is read-only. Events: `MediaOpened`, `MediaEnded`, and `MediaError`.

```csharp
zenMediaPlayer1.MediaError += (s, e) => MessageBox.Show(e.Message);
zenMediaPlayer1.Open(@"C:\Media\clip.mp4");
// The control also has its own Open button and transport controls.
```

**Windows requirement:** `WMPlayer.OCX` / Windows Media Player Legacy must be installed and enabled. Supported formats and codecs depend on the target Windows installation. Validate the application's target architecture against the registered ActiveX component. MP4 playback is not guaranteed on every Windows image.


### `ZenModelViewer`

<img width="1286" height="979" alt="1000049809" src="https://github.com/user-attachments/assets/5f029500-453f-4132-945c-b4d36369d381" />

A GDI+-based mesh viewer for **OBJ**, **STL (ASCII and binary)**, **PLY ASCII**, and **OFF**. It triangulates polygon faces and normalises the mesh for viewing. The left mouse button orbits, the wheel zooms, and the right mouse button pans. Toolbar controls open a model, fit/reset the view, and toggle wireframe. Properties: `Wireframe`, `ShowAxes`, `ModelColor`, `FileName`, `VertexCount`, and `TriangleCount`; methods: `LoadModel(path)`, `OpenFileDialogAndLoad()`, `FitModel()`, and `ResetView()`; events: `ModelLoaded` and `ModelError`.

```csharp
zenModelViewer1.ModelLoaded += (s, e) =>
    statusLabel.Text = zenModelViewer1.TriangleCount + " triangles";
zenModelViewer1.LoadModel(@"C:\Models\part.obj");
```

Large meshes may draw slowly because the renderer uses GDI+. Binary PLY, OBJ/MTL materials and textures, and animation are not supported.


### `ZenWaveformAnalyzer`

<img width="1168" height="550" alt="1000049828" src="https://github.com/user-attachments/assets/ec83c3e8-5876-4877-a4fb-bd8a4ef647e3" />


Opens and visualises WAV audio as a waveform; it is an analyser, **not an audio player**. It accepts integer PCM at 8, 16, 24, or 32 bits and IEEE-float 32-bit data. It reports sample rate, channel count, duration, and cursor position. Clicking the waveform updates `PositionSeconds` and raises `SeekRequested`, allowing an application with its own audio engine to seek to that position.

```csharp
zenWaveformAnalyzer1.SeekRequested += (s, e) => audioEngine.Seek(e.PositionSeconds);
zenWaveformAnalyzer1.LoadWave(@"C:\Audio\voice.wav");
```

After loading, `DurationSeconds`, `SampleRate`, and `Channels` describe the file, and `WaveColor` changes the waveform colour. If a file cannot be read, `LoadError` (which supplies `Message`) is raised; when nothing handles it, the control shows a warning message box instead.

The waveform is reduced to at most 8,192 peak buckets to keep the display compact; audio samples are read from the file stream rather than retaining the entire file in memory. Compressed WAV codecs and other audio containers are not supported.

### `ZenWaveformScroller`

A scrolling waveform strip with a simulated playhead, for UI mock-ups, editors and audio tools where **your own code** handles the audio. It does not decode or play sound. Supply normalised amplitudes with `SetAmplitudes(values)` (or fill `Amplitudes`), then drive the playhead with `Play()`, `Pause()`, `Stop()`, or `TogglePlayPause()`; Space toggles playback when the control has focus. Click to move the playhead and drag to select a range.

```csharp
zenWaveformScroller1.SetAmplitudes(peaks);          // IEnumerable<float>
zenWaveformScroller1.PlaybackSpeed = 1.5f;          // 0.1 – 8
zenWaveformScroller1.Loop = true;
zenWaveformScroller1.PositionChanged += (s, e) =>
    audioEngine.Seek(zenWaveformScroller1.PlaybackPosition);   // 0.0 – 1.0
zenWaveformScroller1.Play();
```

Properties: `PlaybackPosition` (0–1), `PlaybackSpeed`, `Loop`, `IsPlaying` (read-only), `SelectionStart` and `SelectionEnd` (0–1), `CornerRadius`, `WaveformPadding`, and the colours `WaveColor`, `PlayedWaveColor`, `PlayheadColor`, and `SelectionColor`. `PositionChanged` is raised when the playhead moves, whether from the timer or from the user.


### `ZenGCodePreview`

<img width="787" height="570" alt="1000049795" src="https://github.com/user-attachments/assets/cf0cecab-5b3c-4757-b889-a1ca044af889" />


A visual preview for 3D-printer G-code. `LoadGCode(path)` reads linear `G0`/`G1` moves with absolute `X`, `Y`, `Z`, and `E` coordinates. Extrusion and travel moves are drawn differently; the layer slider reveals the print progressively. Use `ShowTravel` to hide travel paths and `IsometricView` for an approximate isometric projection. Scroll to zoom and drag to pan; `LayerCount` and `CurrentLayer` are read-only.

```csharp
zenGCodePreview1.LoadGCode(@"C:\Prints\bracket.gcode");
zenGCodePreview1.ShowTravel = false;
zenGCodePreview1.IsometricView = true;
```

This is a visual path preview, not a slicer or firmware simulator. Relative positioning (`G91`), arcs (`G2`/`G3`), coordinate offsets, temperatures, machine-specific kinematics, and firmware behaviour are not simulated. Check the generated file in a proper slicer/host before printing.

## Creative and visual tools

### `ZenFractalExplorer`

An interactive Mandelbrot and Julia set explorer. The toolbar button switches between the two sets, the mouse wheel zooms around the cursor, and dragging with the left button pans. The keyboard also works: `+`/`-` zoom and `R` resets the view. The status line shows the mode, zoom and the complex-plane coordinates under the cursor.

```csharp
zenFractalExplorer1.Mode = ZenFractalMode.Julia;
zenFractalExplorer1.MaxIterations = 250;
zenFractalExplorer1.RenderCompleted += (s, e) => statusLabel.Text = "Rendered";
zenFractalExplorer1.RenderFailed += (s, e) => Debug.WriteLine(e.Exception.Message);
zenFractalExplorer1.ResetView();
```

Properties: `Mode` (`ZenFractalMode.Mandelbrot` or `Julia`), `MaxIterations` (default 120; higher values show more detail but render more slowly), and `Zoom` (0.25 to 1,000,000). Methods: `Render()` and `ResetView()`. Events: `RenderCompleted` and `RenderFailed` (`ZenFractalRenderErrorEventArgs.Exception`). Rendering is CPU-based, so deep zooms with high iteration counts take longer.

### `ZenPixelForge`

A small pixel-art editor. It has Pen, Eraser and Fill tools, a colour palette with an Add-colour button, a grid toggle, Undo/Redo history, canvas size inputs, and PNG open/save.

```csharp
zenPixelForge1.NewCanvas(32, 32);                       // 1–512 pixels per side
zenPixelForge1.PixelSize = 16;                          // on-screen zoom, 2–64
zenPixelForge1.CurrentColor = Color.OrangeRed;
zenPixelForge1.ActiveTool = ZenPixelForge.EditorTool.Pencil;
zenPixelForge1.SavePng(@"C:\Art\sprite.png");           // actual pixel size
zenPixelForge1.SavePng(@"C:\Art\sprite_x16.png", true); // enlarged by PixelSize
```

API: `NewCanvas(width, height[, background])`, `LoadPng(path)` (images up to 512×512), `SavePng(path[, exportEnlarged])`, `Undo()` and `Redo()` (both return `false` when there is nothing to do), `PixelSize` (values outside 2–64 throw), `CurrentColor`, `ActiveTool` (`EditorTool`: `Pencil`, `Eraser`, `Fill`), `ShowGrid`, `WidthPixels`, and `HeightPixels`. Events: `CanvasChanged`, `CurrentColorChanged`, and `ToolChanged`. Shortcuts: `P` pencil, `E` eraser, `F` fill, Ctrl+Z undo, Ctrl+Y redo; the right mouse button pans the canvas. Invalid sizes and unreadable files raise exceptions, so wrap the calls in `try`/`catch` when the input comes from a user.

### `ZenScreenCaptureStudio`

A screenshot tool with simple editing. **Capture** grabs the primary monitor; **Crop** and **Redact** let you drag a region (crop to keep it, redact to black it out); **Restore** returns to the original capture; **Copy** puts the image on the clipboard; **Save…** writes a PNG or JPEG.

```csharp
zenScreenCaptureStudio1.ImageCaptured += (s, e) => statusLabel.Text = "Captured";
Bitmap shot = zenScreenCaptureStudio1.CaptureScreen();   // null if the capture failed
if (zenScreenCaptureStudio1.SaveImage(@"C:\Shots\today.png"))
    statusLabel.Text = "Saved";
```

API: `CaptureScreen()` (returns a copy of the captured bitmap), `SaveImage(path)` (`.png`, `.jpg`, `.jpeg`; asks before overwriting and returns `false` if declined or if saving fails), `CopyToClipboard()`, `Image` (get returns an independent clone; set loads an image into the editor), and the `ImageCaptured` event. The capture happens immediately, so your own window is part of the screenshot unless you minimise or hide it first. Only the primary monitor is captured.

### `ZenVisionAccessibility`

A visual lab for checking how a design reads under colour-vision deficiencies, plus a contrast checker. Load an image and pick a simulation mode (`VisionSimulationMode`: `None`, `Protanopia`, `Deuteranopia`, `Tritanopia`, `Grayscale`). To check contrast, click **Choose colour 1** or **Choose colour 2** and then click a pixel in the image; the control shows both swatches and their contrast ratio.

```csharp
zenVisionAccessibility1.LoadImage(@"C:\Design\dashboard.png");
zenVisionAccessibility1.SimulationMode = VisionSimulationMode.Deuteranopia;
using (Bitmap simulated = zenVisionAccessibility1.Simulate()) { /* use the bitmap */ }

double ratio = zenVisionAccessibility1.GetContrastRatio(Color.White, Color.FromArgb(90, 90, 120));
zenVisionAccessibility1.ExportImage(@"C:\Design\dashboard_deuteranopia.png");
```

API: `LoadImage(Image)` or `LoadImage(path)`, `SimulationMode`, `Simulate()` (returns a new `Bitmap`), `ExportImage(path)` (returns `true` on success), `GetContrastRatio(first, second)`, `ContrastBackground` (colours with transparency are composited over this before the ratio is computed), `FirstContrastColor`, `SecondContrastColor`, and `ContrastRatio`. The `SimulationChanged` event fires when the mode changes.

The simulations are approximations for design review, not a medical or certification tool.

## Animated controls

<img width="510" height="432" alt="1000049784" src="https://github.com/user-attachments/assets/12c6db87-50d6-427a-be80-cce4c10b0ab2" />


These controls are deliberately expressive UI decorations or visual indicators; they use timers and mouse interaction rather than external animation packages. Avoid placing many high-refresh animated controls on a single form, and set their animation properties appropriately for your application.

| Control | What it does | Useful properties / interaction |
|---|---|---|
| `ZenAnalogClock` | Neon analogue clock with optional digital read-out. | `DialColor`, `FaceColor`, `ShowDigital`, `DisplayedTime`; set `TimeZoneId` to a Windows time-zone ID. An invalid ID falls back to local time. |
| `ZenAuroraPanel` | Rounded panel with an animated aurora of gradients and softly moving light blobs, useful as a backdrop. | `PrimaryColor`, `AccentColor`, `Intensity` (0–100, default 72), `BlobCount`, `AnimationEnabled`, `AnimationSpeed`, `AnimationInterval` (milliseconds, default 33), `CornerRadius`; `AnimationTick` fires on every frame. |
| `ZenCodeRain` | Animated falling glyphs in a terminal-like rain effect. | `CellSize`, `Speed` (milliseconds), `RainColor`, `Running`. |
| `ZenConfettiCelebration` | Transparent overlay that fires a confetti burst. | `Quantity` (default 90), `Gravity`, `Duration` (milliseconds), `Palette` (`Color[]`), `CelebrationText`, `TimerInterval` (15–100 ms); `Play()`, `Stop()`, `IsPlaying`, `Completed`. |
| `ZenGlitchLabel` | Neon label with a controlled RGB-split glitch and scanlines. | `Text`, `TextAlign`, `Intensity` (maximum RGB displacement in pixels, default 5), `Frequency` (glitch updates per second, 1–60, default 18), `ChannelOffset`, `Scanlines`, `ScanlineSpacing`, `AnimationEnabled`, `SurfaceColor`, `GlowColor`, `CornerRadius`. |
| `ZenHoloCard` | Pointer-reactive card with tilt, glow, and parallax. | `Text`, `Subtitle`, `GlowColor`, `CornerRadius`. |
| `ZenLiquidGauge` | Animated liquid fill in a circular gauge. | `Value` (0–100), `LiquidColor`, `GlassColor`, `Animate`. |
| `ZenLoadingSpinner` | Animated circular busy indicator. | `SpinnerColor`, `Speed` (milliseconds), `Spinning`. |
| `ZenMatrixConsole` | Matrix-style falling characters with a short message console on top. | `Start()`, `Stop()`, `IsRunning`, `WriteLine(message)`, `ClearLines()`, `Messages`, `MessageLimit` (default 5), `RainMode`, `Density` (percentage of active columns, default 72), `Speed` (milliseconds, default 55), `TrailLength`, `CharacterSet`, `PauseWhenInvisible`, `RainColor`, `MessageColor`, `GlowColor`, `CornerRadius`. |
| `ZenMorphingGauge` | Neon circular gauge that morphs smoothly to a target value. | `Minimum`, `Maximum`, `Value`, `TargetValue`, `StartAnimation()`, `StartAnimation(target)`, `StopAnimation()`, `AnimationDuration`, `AnimationCompleted`. See [Progress, ratings, labels, and notices](#progress-ratings-labels-and-notices). |
| `ZenNeuralConstellation` | Procedural nodes and links that respond to the pointer. | `NodeCount` (8–100), `NodeColor`, `LinkDistance`. |
| `ZenOrbitLoader` | Neon loader with satellites orbiting a core. | `Start()`, `Stop()`, `IsRunning`, `SatelliteCount`, `OrbitSpeed`, `Clockwise`, `Indeterminate`, `OrbitRadius`, `SatelliteSize`, `CoreSize`, `AnimationInterval`. See [Progress, ratings, labels, and notices](#progress-ratings-labels-and-notices). |
| `ZenOrbitMenu` | Clickable commands arranged around a central hub. | `Items`, `AddItem`, `ClearItems`, `OrbitColor`, `Expanded`; `ItemSelected` returns the selected index and item. Clicking the centre toggles expansion. |
| `ZenParticleEmitter` | Particle canvas with continuous emission and click-triggered explosions. | `AutoStart` (default `true`), `Paused`, `EmissionRate`, `ParticleSpeed`, `Gravity`, `ParticleLife`, `MaxParticles`, `ParticleSize`, `BurstCount`, `StartColor`, `EndColor`, `SurfaceColor`, `ConnectParticles`, `ConnectionDistance`, `CornerRadius`; `EmitBurst()` or `EmitBurst(Point)`; `BurstEmitted`. |
| `ZenParticleField` | Moving particles, proximity links, and pointer attraction. | `ParticleCount`, `LinkDistance`, `ParticleColor`. |

```csharp
zenLiquidGauge1.Value = 68;
zenCodeRain1.RainColor = Color.LimeGreen;
zenCodeRain1.Speed = 50;
zenNeuralConstellation1.NodeCount = 54;
zenOrbitMenu1.AddItem("Export");
zenOrbitMenu1.ItemSelected += (s, e) => RunAction(e.Item);

// Newer animated controls
zenAuroraPanel1.PrimaryColor = Color.FromArgb(124, 92, 255);
zenAuroraPanel1.AnimationSpeed = 0.8f;

zenMatrixConsole1.Start();
zenMatrixConsole1.WriteLine("System online");

zenMorphingGauge1.Label = "CPU";
zenMorphingGauge1.StartAnimation(72);      // morphs from the current value to 72

zenOrbitLoader1.Start();

zenParticleEmitter1.BurstEmitted += (s, e) => Debug.WriteLine("Boom");
zenParticleEmitter1.EmitBurst();
```

### `ZenConfettiCelebration`

A transparent overlay meant to be placed over other content. Size it to cover the area you want to celebrate, bring it to the front, and call `Play()` when something worth celebrating happens. The pieces fall for `Duration` milliseconds, and `Completed` fires when the animation ends or `Stop()` is called while it is playing. If the form is not visible when `Play()` is called, the animation starts once the control becomes visible.

```csharp
zenConfettiCelebration1.BringToFront();
zenConfettiCelebration1.CelebrationText = "Order complete!";
zenConfettiCelebration1.Palette = new[] { Color.Gold, Color.HotPink, Color.DeepSkyBlue };
zenConfettiCelebration1.Completed += (s, e) => zenConfettiCelebration1.Visible = false;
zenConfettiCelebration1.Visible = true;
zenConfettiCelebration1.Play();
```

### Using the animated controls responsibly

Each of these controls runs its own timer. Start them only when they are visible, use `AnimationEnabled`, `Paused`, or `Stop()` while a form is in the background, and keep the number of simultaneous animated controls small on older machines.

## Support types

These public enums and event-argument types are used by the controls above:

- `ZenChartType`: `Line`, `Spline`, `Column`, `Bar`, `Area`, `Pie`, `Doughnut`, `StackedColumn`.
- `ZenBadgeStyle`: `Primary`, `Success`, `Warning`, `Danger`, `Neutral`.
- `ZenNotificationType`: `Information`, `Success`, `Warning`, `Error`.
- `ZenLogLevel`: `Trace`, `Information`, `Success`, `Warning`, `Error`.
- `ZenPomodoroPhase`: `Work`, `ShortBreak`, `LongBreak`.
- `ZenFractalMode`: `Mandelbrot`, `Julia`.
- `ZenServiceState`: `Unknown`, `Healthy`, `Warning`, `Error`, `Offline`.
- `VisionSimulationMode`: `None`, `Protanopia`, `Deuteranopia`, `Tritanopia`, `Grayscale`.
- Nested types: `ZenCircuitLab.GateType` (`AND`, `OR`, `XOR`, `NOT`, `NAND`), `ZenCircuitLab.NodeKind` (`Input`, `Gate`, `Output`), `ZenDataFlowStudio.PipelineStepKind` (`Filter`, `Sort`, `SelectColumns`, `ComputedColumn`), and `ZenPixelForge.EditorTool` (`Pencil`, `Eraser`, `Fill`).
- `ZenColumnProfile`: the per-column summary returned by `ZenDataProfiler.Profiles`.
- Other support objects include `ZenLogEntry`, `ZenTimelineItem`, `ZenWorkflowNode`, `ZenWorkflowConnection`, `ZenAgendaEvent`, `ZenServiceItem`, `ZenBreadcrumbSegmentCollection`, `ZenCircuitLab.CircuitNode`, `ZenCircuitLab.TruthTable`, and `ZenDataFlowStudio.PipelineStep`.
- Event argument types include `ApiErrorEventArgs`, `ApiResultEventArgs`, `ServiceSelectedEventArgs`, `TagEventArgs`, `ZenBreadcrumbSegmentClickedEventArgs`, `ZenCommandEventArgs`, `ZenDateSelectedEventArgs`, `ZenDiffSummaryEventArgs`, `ZenFilesDroppedEventArgs`, `ZenFormulaErrorEventArgs`, `ZenFractalRenderErrorEventArgs`, `ZenGCodeErrorEventArgs`, `ZenJsonNodeSelectedEventArgs`, `ZenKanbanMoveEventArgs`, `MediaPlayerErrorEventArgs`, `ModelViewerErrorEventArgs`, `ZenOrbitItemEventArgs`, `ZenPomodoroPhaseEventArgs`, `ZenRegexEvaluationEventArgs`, `ZenTimelineEventArgs`, `ZenWaveSeekEventArgs`, `ZenWaveformErrorEventArgs`, and `ZenWorkflowNodeEventArgs`; plus the nested `ZenCircuitLab.OutputChangedEventArgs`, `ZenCircuitLab.CircuitErrorEventArgs`, and `ZenDataFlowStudio.PipelineStepEventArgs`.

## Events and threading

WinForms controls should normally be created, changed, and disposed on the UI thread. `ZenLogConsole.Write` is the exception: it detects worker-thread calls and marshals the write back to its UI control. For other controls, marshal background results with `Control.Invoke`/`BeginInvoke` before setting properties or adding items. Event handlers execute on the thread used by the control unless the API documentation for that control says otherwise.

Dispose forms and controls normally through WinForms ownership (`using` for standalone forms/dialogues, or the parent form's component lifecycle). Animated controls own timers and stop/dispose them with the control.

## Project structure

```text
ClassLibrary.sln
ClassLibrary/
  ClassLibrary.csproj
  Zen*.cs                 # one source file per control/support type
  README.md
```

The build artefact is a class library; it does not contain a standalone demo application. Reference the DLL from your own WinForms executable and add the controls you need.

---

Developed by drJhonatan00


<!--私は私、それだけ-->
