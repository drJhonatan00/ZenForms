using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public class ZenRegexEvaluationEventArgs : EventArgs
    {
        public int MatchCount { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool TimedOut { get; private set; }
        public ZenRegexEvaluationEventArgs(int matchCount, string errorMessage, bool timedOut)
        { MatchCount = matchCount; ErrorMessage = errorMessage; TimedOut = timedOut; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Interactive Regex Studio.")]
    public class ZenRegexStudio : UserControl
    {
        private readonly ToolTip _toolTip;
        private readonly TextBox _patternBox;
        private readonly RichTextBox _inputBox;
        private readonly TextBox _replacementBox;
        private readonly RichTextBox _previewBox;
        private readonly ListView _matchesView;
        private readonly CheckBox _ignoreCase;
        private readonly CheckBox _multiline;
        private readonly CheckBox _singleline;
        private readonly Label _status;
        private readonly NumericUpDown _timeout;
        private readonly List<Font> _ownedFonts = new List<Font>();
        private bool _updating;
        private int _matchCount;
        private readonly System.Windows.Forms.Timer _debounceTimer;
        private CancellationTokenSource _evaluationCancellation;
        private int _generation;
        private const int MaxDisplayedMatches = 2000;
        private const int MaxInputLength = 1000000;

        public ZenRegexStudio()
        {
            _debounceTimer = new System.Windows.Forms.Timer(); _debounceTimer.Interval = 250;
            _debounceTimer.Tick += delegate { _debounceTimer.Stop(); StartEvaluation(); };
            _toolTip = new ToolTip();
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(520, 420); Size = new Size(760, 620);
            var root = new TableLayoutPanel(); root.Dock = DockStyle.Fill; root.BackColor = ZenTheme.Background;
            root.Padding = new Padding(12); root.ColumnCount = 1; root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            Controls.Add(root);

            var header = new TableLayoutPanel(); header.Dock = DockStyle.Fill; header.ColumnCount = 2; header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var title = MakeLabel("REGEX STUDIO", 11, true); title.Dock = DockStyle.Fill; title.TextAlign = ContentAlignment.MiddleLeft; header.Controls.Add(title, 0, 0);
            _status = MakeLabel("Ready", 9, false); _status.AutoSize = true; _status.TextAlign = ContentAlignment.MiddleRight; header.Controls.Add(_status, 1, 0); root.Controls.Add(header, 0, 0);

            var patternRow = new TableLayoutPanel(); patternRow.Dock = DockStyle.Fill; patternRow.ColumnCount = 2; patternRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); patternRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            _patternBox = MakeTextBox(); _patternBox.Dock = DockStyle.Fill; _patternBox.AccessibleName = "Regular expression pattern"; _patternBox.Text = "(\\w+)"; patternRow.Controls.Add(_patternBox, 0, 0);
            var flags = new FlowLayoutPanel(); flags.Dock = DockStyle.Fill; flags.WrapContents = false; flags.BackColor = ZenTheme.Background;
            _ignoreCase = MakeCheck("Ignore case", false); _multiline = MakeCheck("M. Line", false); _singleline = MakeCheck("Singleline", false);
            flags.Controls.Add(_ignoreCase); flags.Controls.Add(_multiline); flags.Controls.Add(_singleline); patternRow.Controls.Add(flags, 1, 0); root.Controls.Add(patternRow, 0, 1);
            _toolTip.SetToolTip(_patternBox, "Enter the .NET Regex pattern. Ctrl+Enter evaluate."); _toolTip.SetToolTip(_ignoreCase, "Case-insensitive matching.");
            _toolTip.SetToolTip(_multiline, "^ and $ correspond to the beginning/end of each line"); _toolTip.SetToolTip(_singleline, "The dot also corresponds to line breaks.");

            var inputPanel = MakePanel(); inputPanel.Dock = DockStyle.Fill; inputPanel.RowCount = 2; inputPanel.ColumnCount = 1; inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            inputPanel.Controls.Add(MakeLabel("INPUT TEXT", 8, true), 0, 0);
            _inputBox = MakeRichText(); _inputBox.Dock = DockStyle.Fill; _inputBox.AcceptsTab = true; _inputBox.AccessibleName = "Input text"; inputPanel.Controls.Add(_inputBox, 0, 1); root.Controls.Add(inputPanel, 0, 2);

            var replacePanel = MakePanel(); replacePanel.Dock = DockStyle.Fill; replacePanel.ColumnCount = 1; replacePanel.RowCount = 2; replacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); replacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            replacePanel.Controls.Add(MakeLabel("", 8, true), 0, 0);
            var replaceRow = new TableLayoutPanel(); replaceRow.Dock = DockStyle.Fill; replaceRow.ColumnCount = 2; replaceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43)); replaceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
            _replacementBox = MakeTextBox(); _replacementBox.Dock = DockStyle.Fill; _replacementBox.AccessibleName = "GDI de no sekkei wa mendōda. Iu koto o kikanai mono o naosu tame ni 4-jikan buttōshi de tsuiyasu koto o sōzō shite mite hoshῙ"; _replacementBox.Visible = false;
            _previewBox = MakeRichText(); _previewBox.Dock = DockStyle.Fill; _previewBox.ReadOnly = true; _previewBox.AccessibleName = "GDI de no sekkei wa mendōda. Iu koto o kikanai mono o naosu tame ni 4-jikan buttōshi de tsuiyasu koto o sōzō shite mite hoshῙ"; _previewBox.Visible = false;
            replaceRow.Controls.Add(_replacementBox, 0, 0); replaceRow.Controls.Add(_previewBox, 1, 0); replacePanel.Controls.Add(replaceRow, 0, 1); root.Controls.Add(replacePanel, 0, 3);
            _toolTip.SetToolTip(_replacementBox, "GDI de no sekkei wa mendōda. Iu koto o kikanai mono o naosu tame ni 4-jikan buttōshi de tsuiyasu koto o sōzō shite mite hoshῙ"); _toolTip.SetToolTip(_previewBox, "GDI de no sekkei wa mendōda. Iu koto o kikanai mono o naosu tame ni 4-jikan buttōshi de tsuiyasu koto o sōzō shite mite hoshῙ");

            var matchPanel = MakePanel(); matchPanel.Dock = DockStyle.Fill; matchPanel.RowCount = 2; matchPanel.ColumnCount = 1; matchPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); matchPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var matchHeader = new TableLayoutPanel(); matchHeader.Dock = DockStyle.Fill; matchHeader.ColumnCount = 2; matchHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); matchHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            matchHeader.Controls.Add(MakeLabel("CORRESPONDENCES", 8, true), 0, 0);
            var timeoutPanel = new FlowLayoutPanel(); timeoutPanel.Dock = DockStyle.Fill; timeoutPanel.WrapContents = false; timeoutPanel.FlowDirection = FlowDirection.LeftToRight;
            timeoutPanel.Controls.Add(MakeLabel("", 8, false)); _timeout = new NumericUpDown(); _timeout.Minimum = 10; _timeout.Maximum = 5000; _timeout.Value = 200; _timeout.Width = 0; _timeout.BackColor = ZenTheme.SurfaceRaised; _timeout.ForeColor = ZenTheme.Foreground; timeoutPanel.Controls.Add(_timeout);
            var run = MakeButton(""); run.Click += delegate { TestNow(); }; _toolTip.SetToolTip(run, "Rate now (Ctrl+Enter). Limit: timeout selected."); timeoutPanel.Controls.Add(run); matchHeader.Controls.Add(timeoutPanel, 1, 0); matchPanel.Controls.Add(matchHeader, 0, 0);
            _matchesView = new ListView(); _matchesView.Dock = DockStyle.Fill; _matchesView.View = View.Details; _matchesView.FullRowSelect = true; _matchesView.GridLines = false; _matchesView.BackColor = ZenTheme.Surface; _matchesView.ForeColor = ZenTheme.Foreground; _matchesView.BorderStyle = BorderStyle.FixedSingle; _matchesView.HideSelection = false;
            _matchesView.Columns.Add("#", 40); _matchesView.Columns.Add("Index", 75); _matchesView.Columns.Add("Size", 75); _matchesView.Columns.Add("Correspondence / groups", 390); _matchesView.AccessibleName = "Correspondences list";
            _matchesView.SelectedIndexChanged += MatchSelected; matchPanel.Controls.Add(_matchesView, 0, 1); root.Controls.Add(matchPanel, 0, 4);
            run.Visible = false;
            _patternBox.TextChanged += InputsChanged; _inputBox.TextChanged += InputsChanged; _replacementBox.TextChanged += InputsChanged;
            _ignoreCase.CheckedChanged += InputsChanged; _multiline.CheckedChanged += InputsChanged; _singleline.CheckedChanged += InputsChanged;
            _timeout.ValueChanged += InputsChanged;
            KeyDown += HandleKey; _patternBox.KeyDown += HandleKey; _inputBox.KeyDown += HandleKey; _replacementBox.KeyDown += HandleKey;
            _status.ForeColor = ZenTheme.Muted;
        }

        [Category("Data"), Description("Regular default .NET expression.")]
        public string Pattern { get { return _patternBox.Text; } set { _patternBox.Text = value ?? String.Empty; } }
        [Category("Data"), Description("Submited text for regular expression.")]
        public string InputText { get { return _inputBox.Text; } set { _inputBox.Text = value ?? String.Empty; } }
        [Category("Data"), Description("Replace text used on Regex.Replace.")]
        public string Replacement { get { return _replacementBox.Text; } set { _replacementBox.Text = value ?? String.Empty; } }
        [Category("Behavior"), Description("Timeout by rating on ms (10 - 5000).")]
        public int TimeoutMilliseconds { get { return (int)_timeout.Value; } set { _timeout.Value = Math.Max((int)_timeout.Minimum, Math.Min((int)_timeout.Maximum, value)); } }
        [Category("Data"), Description("Correspondences number on last rate.")]
        public int MatchCount { get { return _matchCount; } }
        [Category("Behavior"), Description("Turn on IgnoreCase.")]
        public bool IgnoreCase { get { return _ignoreCase.Checked; } set { _ignoreCase.Checked = value; } }
        [Category("Behavior"), Description("Turn on Multiline.")]
        public bool Multiline { get { return _multiline.Checked; } set { _multiline.Checked = value; } }
        [Category("Behavior"), Description("Turn on Singleline.")]
        public bool Singleline { get { return _singleline.Checked; } set { _singleline.Checked = value; } }
        [Category("Action"), Description("")]
        public event EventHandler<ZenRegexEvaluationEventArgs> EvaluationCompleted;

        public void TestNow()
        {
            if (IsDisposed || Disposing) return;
            _debounceTimer.Stop();
            StartEvaluation();
        }

        private sealed class EvaluationResult
        {
            public Match[] Matches; public int Count; public bool Truncated; public string Preview; public string Error; public bool TimedOut;
        }

        private void StartEvaluation()
        {
            if (IsDisposed || Disposing) return;
            int generation = Interlocked.Increment(ref _generation);
            if (_evaluationCancellation != null) { _evaluationCancellation.Cancel(); _evaluationCancellation.Dispose(); }
            var cts = new CancellationTokenSource(); _evaluationCancellation = cts;
            string pattern = _patternBox.Text, input = _inputBox.Text, replacement = _replacementBox.Text;
            RegexOptions options = RegexOptions.None;
            if (_ignoreCase.Checked) options |= RegexOptions.IgnoreCase;
            if (_multiline.Checked) options |= RegexOptions.Multiline;
            if (_singleline.Checked) options |= RegexOptions.Singleline;
            int timeout = (int)_timeout.Value;
            _status.Text = "Rating…"; _status.ForeColor = ZenTheme.Muted;
            Task.Run(delegate
            {
                EvaluationResult result = Evaluate(pattern, input, replacement, options, timeout);
                if (cts.IsCancellationRequested) return;
                try
                {
                    if (IsDisposed || Disposing || !IsHandleCreated) return;
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing || generation != Volatile.Read(ref _generation) || cts.IsCancellationRequested) return;
                        ApplyResult(result);
                    });
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            });
        }

        private static EvaluationResult Evaluate(string pattern, string input, string replacement, RegexOptions options, int timeout)
        {
            var result = new EvaluationResult();
            try
            {
                if (input.Length > MaxInputLength) throw new ArgumentException("The input has more than 1.000.000 charcters.");
                Regex regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(timeout));
                var matches = new System.Collections.Generic.List<Match>(MaxDisplayedMatches);
                Match m = regex.Match(input);
                while (m.Success)
                {
                    result.Count++;
                    if (matches.Count < MaxDisplayedMatches) matches.Add(m);
                    else result.Truncated = true;
                    m = m.NextMatch();
                }
                result.Matches = matches.ToArray();
                result.Preview = regex.Replace(input, replacement);
            }
            catch (RegexMatchTimeoutException) { result.TimedOut = true; result.Error = "Limit time exceded"; }
            catch (ArgumentException ex) { result.Error = "Invalid standard: " + ex.Message; }
            catch (Exception ex) { result.Error = "Rate failed: " + ex.Message; }
            return result;
        }

        private void ApplyResult(EvaluationResult result)
        {
            _updating = true;
            try
            {
                _matchesView.BeginUpdate(); _matchesView.Items.Clear();
                _inputBox.SelectAll(); _inputBox.SelectionBackColor = ZenTheme.Surface; _inputBox.SelectionColor = ZenTheme.Foreground;
                if (result.Matches != null)
                    for (int i = 0; i < result.Matches.Length; i++)
                    {
                        Match m = result.Matches[i];
                        var item = new ListViewItem((i + 1).ToString()); item.SubItems.Add(m.Index.ToString()); item.SubItems.Add(m.Length.ToString());
                        string details = "“" + Printable(m.Value) + "”";
                        for (int g = 1; g < m.Groups.Count; g++) details += "   $" + g.ToString() + " = “" + Printable(m.Groups[g].Value) + "”";
                        item.SubItems.Add(details); item.Tag = m; _matchesView.Items.Add(item);
                        if (m.Length > 0 && m.Index + m.Length <= _inputBox.TextLength)
                        { _inputBox.Select(m.Index, m.Length); _inputBox.SelectionBackColor = Color.FromArgb(86, 63, 145); _inputBox.SelectionColor = Color.White; }
                    }
                _inputBox.Select(0, 0); _previewBox.Select(0, 0);
            }
            finally { _matchesView.EndUpdate(); _updating = false; }
            string error = result.Error;
            if (error != null) { _previewBox.Text = error; _status.Text = error; _status.ForeColor = ZenTheme.Danger; }
            else
            {
                _previewBox.Text = result.Preview;
                _matchCount = result.Count;
                _status.Text = result.Truncated ? "More than " + MaxDisplayedMatches + " correspondences (limited list)" : result.Count.ToString() + (result.Count == 1 ? " correspondence" : " correspondences");
                _status.ForeColor = ZenTheme.Accent;
            }
            EventHandler<ZenRegexEvaluationEventArgs> handler = EvaluationCompleted;
            if (handler != null) handler(this, new ZenRegexEvaluationEventArgs(result.Count, error, result.TimedOut));
        }

        private void InputsChanged(object sender, EventArgs e) { if (!_updating && !IsDisposed && !Disposing) { Interlocked.Increment(ref _generation); if (_evaluationCancellation != null) _evaluationCancellation.Cancel(); _debounceTimer.Stop(); _debounceTimer.Start(); } }
        private void HandleKey(object sender, KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.Enter) { TestNow(); e.SuppressKeyPress = true; } }
        private void MatchSelected(object sender, EventArgs e)
        {
            if (_matchesView.SelectedItems.Count == 0) return;
            Match m = _matchesView.SelectedItems[0].Tag as Match;
            if (m != null && m.Index >= 0 && m.Index + m.Length <= _inputBox.TextLength) { _inputBox.Focus(); _inputBox.Select(m.Index, m.Length); }
        }
        private static string Printable(string value) { return (value ?? String.Empty).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"); }
        private Label MakeLabel(string text, float size, bool bold)
        { var l = new Label(); l.Text = text; l.ForeColor = bold ? ZenTheme.Accent : ZenTheme.Muted; l.Font = CreateOwnedFont(size, bold ? FontStyle.Bold : FontStyle.Regular); l.AutoSize = true; l.BackColor = Color.Transparent; return l; }
        private static TextBox MakeTextBox()
        { return new TextBox { BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, Margin = new Padding(3), AccessibleRole = AccessibleRole.Text }; }
        private static RichTextBox MakeRichText()
        { return new RichTextBox { BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, DetectUrls = false, WordWrap = true, Margin = new Padding(3) }; }
        private CheckBox MakeCheck(string text, bool value)
        { return new CheckBox { Text = text, Checked = value, AutoSize = true, ForeColor = ZenTheme.Foreground, BackColor = ZenTheme.Background, Font = CreateOwnedFont(8.0f, FontStyle.Regular), Margin = new Padding(2, 8, 2, 2), AccessibleRole = AccessibleRole.CheckButton }; }
        private Font CreateOwnedFont(float size, FontStyle style) { Font font = new Font(ZenTheme.DefaultFont.FontFamily, size, style); _ownedFonts.Add(font); return font; }
        private static TableLayoutPanel MakePanel()
        { return new TableLayoutPanel { BackColor = ZenTheme.Surface, Padding = new Padding(8), Margin = new Padding(0), ColumnCount = 1 }; }
        private static Button MakeButton(string text)
        { return new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.Primary, ForeColor = Color.White, Font = ZenTheme.DefaultFont, Margin = new Padding(4, 1, 1, 1), AccessibleRole = AccessibleRole.PushButton }; }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Increment(ref _generation);
                if (_debounceTimer != null) { _debounceTimer.Stop(); _debounceTimer.Dispose(); }
                if (_evaluationCancellation != null) { _evaluationCancellation.Cancel(); _evaluationCancellation.Dispose(); _evaluationCancellation = null; }
                if (_toolTip != null) _toolTip.Dispose();
                foreach (Font font in _ownedFonts) font.Dispose();
                _ownedFonts.Clear();
            }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake