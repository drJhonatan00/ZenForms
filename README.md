# ZenForms Core

**A themed WinForms component library for .NET Framework 4.7.2**

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
- [Experimental animated controls](#experimental-animated-controls)
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

        ZenButtonBlue save = new ZenButtonBlue
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

```csharp
var card = new ZenCard { CornerRadius = 18, Padding = new Padding(16), Size = new Size(320, 150) };
card.Controls.Add(new ZenLabel { Text = "Account overview", AutoSize = true, Location = new Point(16, 16) });
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

A collapsible section with a header and a scrollable child panel. Set `Text`, `Expanded`, and `ContentHeight`. Add controls to `ContentPanel.Controls` (or `ContentControls`); `ExpandedChanged` reports expand/collapse changes.

```csharp
zenAccordion1.Text = "Advanced options";
zenAccordion1.ContentPanel.Controls.Add(new CheckBox { Text = "Enable diagnostics", AutoSize = true });
zenAccordion1.ExpandedChanged += (s, e) => UpdateLayout();
```

### `ZenHeatmapCalendar`

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

## Advanced data and developer tools

### `ZenLogConsole`

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

## Media and visualisation tools

### `ZenMediaPlayer`

A compact MP3/MP4 player using the Windows Media Player ActiveX control. It includes open, play, pause, stop, seek, volume, time display, and full-screen controls. Use `Open(path)`, `Play()`, `Pause()`, `Stop()`, or `OpenFileDialogAndPlay()`. `Volume` and `FullScreen` are configurable; `FileName` is read-only. Events: `MediaOpened`, `MediaEnded`, and `MediaError`.

```csharp
zenMediaPlayer1.MediaError += (s, e) => MessageBox.Show(e.Message);
zenMediaPlayer1.Open(@"C:\Media\clip.mp4");
// The control also has its own Open button and transport controls.
```

**Windows requirement:** `WMPlayer.OCX` / Windows Media Player Legacy must be installed and enabled. Supported formats and codecs depend on the target Windows installation. Validate the application's target architecture against the registered ActiveX component. MP4 playback is not guaranteed on every Windows image.

### `ZenModelViewer`

A GDI+-based mesh viewer for **OBJ**, **STL (ASCII and binary)**, **PLY ASCII**, and **OFF**. It triangulates polygon faces and normalises the mesh for viewing. The left mouse button orbits, the wheel zooms, and the right mouse button pans. Toolbar controls open a model, fit/reset the view, and toggle wireframe. Properties: `Wireframe`, `ShowAxes`, `ModelColor`, `FileName`, `VertexCount`, and `TriangleCount`; methods: `LoadModel(path)`, `OpenFileDialogAndLoad()`, `FitModel()`, and `ResetView()`; events: `ModelLoaded` and `ModelError`.

```csharp
zenModelViewer1.ModelLoaded += (s, e) =>
    statusLabel.Text = zenModelViewer1.TriangleCount + " triangles";
zenModelViewer1.LoadModel(@"C:\Models\part.obj");
```

Large meshes may draw slowly because the renderer uses GDI+. Binary PLY, OBJ/MTL materials and textures, and animation are not supported.

### `ZenWaveformAnalyzer`

Opens and visualises WAV audio as a waveform; it is an analyser, **not an audio player**. It accepts integer PCM at 8, 16, 24, or 32 bits and IEEE-float 32-bit data. It reports sample rate, channel count, duration, and cursor position. Clicking the waveform updates `PositionSeconds` and raises `SeekRequested`, allowing an application with its own audio engine to seek to that position.

```csharp
zenWaveformAnalyzer1.SeekRequested += (s, e) => audioEngine.Seek(e.PositionSeconds);
zenWaveformAnalyzer1.LoadWave(@"C:\Audio\voice.wav");
```

The waveform is reduced to at most 8,192 peak buckets to keep the display compact; audio samples are read from the file stream rather than retaining the entire file in memory. Compressed WAV codecs and other audio containers are not supported.

### `ZenGCodePreview`

A visual preview for 3D-printer G-code. `LoadGCode(path)` reads linear `G0`/`G1` moves with absolute `X`, `Y`, `Z`, and `E` coordinates. Extrusion and travel moves are drawn differently; the layer slider reveals the print progressively. Use `ShowTravel` to hide travel paths and `IsometricView` for an approximate isometric projection. Scroll to zoom and drag to pan; `LayerCount` and `CurrentLayer` are read-only.

```csharp
zenGCodePreview1.LoadGCode(@"C:\Prints\bracket.gcode");
zenGCodePreview1.ShowTravel = false;
zenGCodePreview1.IsometricView = true;
```

This is a visual path preview, not a slicer or firmware simulator. Relative positioning (`G91`), arcs (`G2`/`G3`), coordinate offsets, temperatures, machine-specific kinematics, and firmware behaviour are not simulated. Check the generated file in a proper slicer/host before printing.

## Animated controls

These controls are deliberately expressive UI decorations or visual indicators; they use timers and mouse interaction rather than external animation packages. Avoid placing many high-refresh animated controls on a single form, and set their animation properties appropriately for your application.

| Control | What it does | Useful properties / interaction |
|---|---|---|
| `ZenAnalogClock` | Neon analogue clock with optional digital read-out. | `DialColor`, `FaceColor`, `ShowDigital`, `DisplayedTime`; set `TimeZoneId` to a Windows time-zone ID. An invalid ID falls back to local time. |
| `ZenCodeRain` | Animated falling glyphs in a terminal-like rain effect. | `CellSize`, `Speed` (milliseconds), `RainColor`, `Running`. |
| `ZenHoloCard` | Pointer-reactive card with tilt, glow, and parallax. | `Text`, `Subtitle`, `GlowColor`, `CornerRadius`. |
| `ZenLiquidGauge` | Animated liquid fill in a circular gauge. | `Value` (0–100), `LiquidColor`, `GlassColor`, `Animate`. |
| `ZenLoadingSpinner` | Animated circular busy indicator. | `SpinnerColor`, `Speed` (milliseconds), `Spinning`. |
| `ZenNeuralConstellation` | Procedural nodes and links that respond to the pointer. | `NodeCount` (8–100), `NodeColor`, `LinkDistance`. |
| `ZenOrbitMenu` | Clickable commands arranged around a central hub. | `Items`, `AddItem`, `ClearItems`, `OrbitColor`, `Expanded`; `ItemSelected` returns the selected index and item. Clicking the centre toggles expansion. |
| `ZenParticleField` | Moving particles, proximity links, and pointer attraction. | `ParticleCount`, `LinkDistance`, `ParticleColor`. |

```csharp
zenLiquidGauge1.Value = 68;
zenCodeRain1.RainColor = Color.LimeGreen;
zenCodeRain1.Speed = 50;
zenNeuralConstellation1.NodeCount = 54;
zenOrbitMenu1.AddItem("Export");
zenOrbitMenu1.ItemSelected += (s, e) => RunAction(e.Item);
```

## Support types

These public enums and event-argument types are used by the controls above:

- `ZenChartType`: `Line`, `Spline`, `Column`, `Bar`, `Area`, `Pie`, `Doughnut`, `StackedColumn`.
- `ZenBadgeStyle`: `Primary`, `Success`, `Warning`, `Danger`, `Neutral`.
- `ZenNotificationType`: `Information`, `Success`, `Warning`, `Error`.
- `ZenLogLevel`: `Trace`, `Information`, `Success`, `Warning`, `Error`.
- `ZenPomodoroPhase`: `Work`, `ShortBreak`, `LongBreak`.
- `ZenColumnProfile`: the per-column summary returned by `ZenDataProfiler.Profiles`.
- Other support objects include `ZenLogEntry`, `ZenTimelineItem`, `ZenWorkflowNode`, and `ZenWorkflowConnection`.
- Event argument types include `ZenCommandEventArgs`, `ZenDiffSummaryEventArgs`, `ZenFilesDroppedEventArgs`, `ZenFormulaErrorEventArgs`, `ZenGCodeErrorEventArgs`, `ZenKanbanMoveEventArgs`, `ZenOrbitItemEventArgs`, `ZenPomodoroPhaseEventArgs`, `ZenTimelineEventArgs`, `ZenWaveSeekEventArgs`, `ZenWaveformErrorEventArgs`, and `ZenWorkflowNodeEventArgs`.

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
