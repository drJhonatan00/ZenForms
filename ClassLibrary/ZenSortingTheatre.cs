using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Interactive alghoritm viewer.")]
    public class ZenSortingTheatre : UserControl
    {
        public enum SortingAlgorithm { Bubble, Insertion, Selection, Merge, Quick }

        public sealed class SortingProgressEventArgs : EventArgs
        {
            public int StepIndex { get; private set; }
            public int TotalSteps { get; private set; }
            public long Comparisons { get; private set; }
            public long Swaps { get; private set; }
            public bool IsCompleted { get; private set; }
            internal SortingProgressEventArgs(int step, int total, long comparisons, long swaps, bool completed)
            { StepIndex = step; TotalSteps = total; Comparisons = comparisons; Swaps = swaps; IsCompleted = completed; }
        }

        private sealed class Operation
        {
            public int A, B, Value, Kind;
            public Operation(int kind, int a, int b, int value) { Kind = kind; A = a; B = b; Value = value; }
        }

        private const int MaximumArraySize = 256;
        private readonly Timer _timer;
        private readonly List<int> _values = new List<int>();
        private readonly List<Operation> _operations = new List<Operation>();
        private readonly HashSet<int> _sorted = new HashSet<int>();
        private readonly ComboBox _algorithmBox;
        private readonly NumericUpDown _sizeInput;
        private readonly TrackBar _speedInput;
        private readonly Button _playButton, _stepButton, _restartButton, _exportButton;
        private readonly Label _statsLabel, _hintLabel;
        private SortingAlgorithm _algorithm = SortingAlgorithm.Bubble;
        private int _seed = 1729, _step, _compareA = -1, _compareB = -1, _pivot = -1;
        private long _comparisons, _swaps;
        private bool _suppress, _disposed;
        private Font _titleFont, _monoFont;
        private string _lastExport = "";

        public event EventHandler<SortingProgressEventArgs> ProgressChanged;
        public event EventHandler<SortingProgressEventArgs> SortingCompleted;
        [Category("Behavior")]
        public SortingAlgorithm Algorithm
        {
            get { return _algorithm; }
            set
            {
                if (!Enum.IsDefined(typeof(SortingAlgorithm), value)) throw new ArgumentOutOfRangeException("value");
                if (_algorithm == value) return;
                _algorithm = value;
                if (_algorithmBox != null) { _suppress = true; _algorithmBox.SelectedIndex = (int)value; _suppress = false; }
                Restart();
            }
        }

        [Category("Behavior")]
        public int ArraySize
        {
            get { return (int)_sizeInput.Value; }
            set
            {
                if (value < 2 || value > MaximumArraySize) throw new ArgumentOutOfRangeException("value", "The size must be between 2 and 256.");
                if (_sizeInput.Value == value) return;
                _sizeInput.Value = value; Restart();
            }
        }

        [Category("Behavior")]
        public int PlaybackInterval
        {
            get { return _timer.Interval; }
            set { if (value < 10 || value > 1000) throw new ArgumentOutOfRangeException("value"); _timer.Interval = value; }
        }

        [Browsable(false)] public bool IsPlaying { get { return _timer.Enabled; } }
        [Browsable(false)] public long Comparisons { get { return _comparisons; } }
        [Browsable(false)] public long Swaps { get { return _swaps; } }
        [Browsable(false)] public int[] Values { get { return _values.ToArray(); } }
        [Browsable(false)] public string LastExport { get { return _lastExport; } }

        public ZenSortingTheatre()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true; BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; MinimumSize = new Size(430, 330); Size = new Size(760, 500);
            _titleFont = new Font("Segoe UI", 11f, FontStyle.Bold); _monoFont = new Font("Consolas", 9f);
            _algorithmBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 112 };
            _algorithmBox.Items.AddRange(new object[] { "Bubble", "Insertion", "Selection", "Merge", "Quick" }); _algorithmBox.SelectedIndex = 0;
            _algorithmBox.SelectedIndexChanged += delegate { if (!_suppress) Algorithm = (SortingAlgorithm)_algorithmBox.SelectedIndex; };
            _sizeInput = new NumericUpDown { Minimum = 2, Maximum = MaximumArraySize, Value = 32, Width = 60 };
            _sizeInput.ValueChanged += delegate { if (!_suppress) Restart(); };
            _speedInput = new TrackBar { Minimum = 10, Maximum = 100, Value = 50, TickFrequency = 15, Width = 110, Height = 28 };
            _speedInput.ValueChanged += delegate { _timer.Interval = 1010 - _speedInput.Value * 10; };
            _playButton = MakeButton("▶ Play"); _stepButton = MakeButton("Step"); _restartButton = MakeButton("Restart"); _exportButton = MakeButton("Export array");
            _playButton.Click += delegate { TogglePlayback(); }; _stepButton.Click += delegate { Step(); }; _restartButton.Click += delegate { Restart(); }; _exportButton.Click += delegate { CopyFinalArray(); };
            _statsLabel = MakeLabel(); _hintLabel = MakeLabel();
            Controls.AddRange(new Control[] { _algorithmBox, _sizeInput, _speedInput, _playButton, _stepButton, _restartButton, _exportButton, _statsLabel, _hintLabel });
            _timer = new Timer { Interval = 510 }; _timer.Tick += delegate { ExecuteStep(); };
            Restart();
        }

        private static Button MakeButton(string text) { return new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, FlatAppearance = { BorderColor = ZenTheme.Border } }; }
        private static Label MakeLabel() { return new Label { AutoSize = true, ForeColor = ZenTheme.Muted, BackColor = Color.Transparent }; }

        public void TogglePlayback()
        {
            if (_step >= _operations.Count) Restart();
            _timer.Enabled = _step < _operations.Count;
            _playButton.Text = _timer.Enabled ? "❚❚ Pause" : "▶ Play";
            Focus();
        }

        public void Step()
        {
            if (InvokeRequired) { BeginInvoke((MethodInvoker)Step); return; }
            _timer.Stop(); _playButton.Text = "▶ Play"; ExecuteStep(); Focus();
        }

        public void Restart()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke((MethodInvoker)Restart); return; }
            _timer.Stop(); _playButton.Text = "▶ Play"; _values.Clear(); _operations.Clear(); _sorted.Clear(); _step = 0; _comparisons = 0; _swaps = 0;
            _compareA = _compareB = _pivot = -1;
            Random random = new Random(_seed++);
            for (int i = 0; i < (int)_sizeInput.Value; i++) _values.Add(random.Next(5, 101));
            BuildOperations(); UpdateStats(); Invalidate();
        }

        public string CopyFinalArray()
        {
            if (InvokeRequired) { string answer = ""; Invoke((MethodInvoker)delegate { answer = CopyFinalArray(); }); return answer; }
            if (_step < _operations.Count) { _hintLabel.Text = "Conclude the ordenation before export."; return ""; }
            _lastExport = "new int[] { " + string.Join(", ", _values) + " };";
            try { Clipboard.SetText(_lastExport); _hintLabel.Text = "Final array copied."; }
            catch (Exception) { _hintLabel.Text = "Array ready on LastExport (indisponible clipboard)."; }
            return _lastExport;
        }

        private void BuildOperations()
        {
            switch (_algorithm)
            {
                case SortingAlgorithm.Bubble: BuildBubble(); break;
                case SortingAlgorithm.Insertion: BuildInsertion(); break;
                case SortingAlgorithm.Selection: BuildSelection(); break;
                case SortingAlgorithm.Merge: BuildMerge(); break;
                case SortingAlgorithm.Quick: BuildQuick(0, _values.Count - 1); break;
                default: throw new ArgumentOutOfRangeException("Algorithm");
            }
            _operations.Add(new Operation(4, -1, -1, 0));
        }
        private void AddCompare(int a, int b) { _operations.Add(new Operation(0, a, b, 0)); }
        private void AddSwap(int a, int b) { _operations.Add(new Operation(1, a, b, 0)); }
        private void AddWrite(int a, int value) { _operations.Add(new Operation(2, a, -1, value)); }
        private void BuildBubble()
        {
            for (int end = _values.Count - 1; end > 0; end--) { for (int j = 0; j < end; j++) { AddCompare(j, j + 1); AddSwap(j, j + 1); } _operations.Add(new Operation(3, end, -1, 0)); }
            _operations.Add(new Operation(3, 0, -1, 0));
        }
        private void BuildInsertion()
        {
            for (int i = 1; i < _values.Count; i++) { int j = i; while (j > 0) { AddCompare(j - 1, j); AddSwap(j - 1, j); j--; } _operations.Add(new Operation(3, i, -1, 0)); }
            _operations.Add(new Operation(3, 0, -1, 0));
        }
        private void BuildSelection()
        {
            for (int i = 0; i < _values.Count; i++) { for (int j = i + 1; j < _values.Count; j++) { AddCompare(i, j); AddSwap(i, j); } _operations.Add(new Operation(3, i, -1, 0)); }
        }
        private void BuildMerge()
        {
            int[] copy = _values.ToArray();
            for (int width = 1; width < copy.Length; width *= 2)
            {
                for (int left = 0; left < copy.Length; left += width * 2)
                {
                    int mid = Math.Min(left + width, copy.Length), right = Math.Min(left + width * 2, copy.Length);
                    int i = left, j = mid, k = left; int[] temp = new int[right - left];
                    while (i < mid && j < right) { AddCompare(i, j); temp[k++ - left] = copy[i] <= copy[j] ? copy[i++] : copy[j++]; }
                    while (i < mid) temp[k++ - left] = copy[i++]; while (j < right) temp[k++ - left] = copy[j++];
                    for (k = left; k < right; k++) { copy[k] = temp[k - left]; AddWrite(k, copy[k]); }
                }
                if (width > copy.Length / 2) break;
            }
            for (int i = 0; i < copy.Length; i++) _operations.Add(new Operation(3, i, -1, 0));
        }
        private void BuildQuick(int low, int high)
        {
            if (low >= high) { if (low == high) _operations.Add(new Operation(3, low, -1, 0)); return; }
            int pivot = _values[high], i = low;
            for (int j = low; j < high; j++) { AddCompare(j, high); if (_values[j] <= pivot) { AddSwap(i, j); i++; } }
            AddSwap(i, high); _operations.Add(new Operation(3, i, -1, 0));
            BuildQuick(low, i - 1); BuildQuick(i + 1, high);
        }

        private void ExecuteStep()
        {
            if (_step >= _operations.Count) { _timer.Stop(); return; }
            Operation op = _operations[_step++]; _compareA = _compareB = _pivot = -1;
            if (op.Kind == 0) { _compareA = op.A; _compareB = op.B; _comparisons++; }
            else if (op.Kind == 1)
            {
                _compareA = op.A; _compareB = op.B;
                if (_algorithm == SortingAlgorithm.Quick && op.B >= 0 && op.B < _values.Count) _pivot = op.B;
                int t = _values[op.A]; _values[op.A] = _values[op.B]; _values[op.B] = t; _swaps++;
            }
            else if (op.Kind == 2) { _values[op.A] = op.Value; _compareA = op.A; _swaps++; }
            else if (op.Kind == 3) _sorted.Add(op.A);
            bool completed = _step >= _operations.Count;
            if (completed) { _timer.Stop(); _playButton.Text = "▶ Play"; _sorted.Clear(); for (int i = 0; i < _values.Count; i++) _sorted.Add(i); }
            UpdateStats(); Invalidate();
            SortingProgressEventArgs args = new SortingProgressEventArgs(_step, _operations.Count, _comparisons, _swaps, completed);
            EventHandler<SortingProgressEventArgs> progress = ProgressChanged; if (progress != null) progress(this, args);
            if (completed) { EventHandler<SortingProgressEventArgs> done = SortingCompleted; if (done != null) done(this, args); }
        }

        private void UpdateStats()
        {
            _statsLabel.Text = "Comparisons  " + _comparisons + "     Swaps / writes  " + _swaps;
            _hintLabel.Text = "Purple: comparing   ·   Teal: pivot   ·   Green: sorted     |     Space: play/pause   →: step   R: restart";
            LayoutControls();
        }
        private void LayoutControls()
        {
            int x = 12, y = 12;
            _algorithmBox.Location = new Point(x, y + 2); x += _algorithmBox.Width + 8;
            _sizeInput.Location = new Point(x, y + 4); x += _sizeInput.Width + 7;
            _speedInput.Location = new Point(x, y); x += _speedInput.Width + 5;
            _playButton.Location = new Point(x, y + 1); x += _playButton.Width + 5;
            _stepButton.Location = new Point(x, y + 1); x += _stepButton.Width + 5;
            _restartButton.Location = new Point(x, y + 1); x += _restartButton.Width + 5;
            _exportButton.Location = new Point(x, y + 1);
            _statsLabel.Location = new Point(12, 51); _hintLabel.Location = new Point(12, 70);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space) { TogglePlayback(); return true; }
            if (keyData == Keys.Right) { Step(); return true; }
            if (keyData == Keys.R) { Restart(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

       
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int top = 104, bottom = Height - 52, left = 14, right = Width - 14;
            if (right <= left || bottom <= top) return;
            using (SolidBrush b = new SolidBrush(ZenTheme.Surface)) g.FillRectangle(b, left, top, right - left, bottom - top);
            using (Pen border = new Pen(ZenTheme.Border)) g.DrawRectangle(border, left, top, right - left, bottom - top);
            int n = _values.Count; if (n == 0) return;
            float slot = (right - left - 12f) / n, barWidth = Math.Max(1f, slot - 2f); int baseY = bottom - 16; float scale = Math.Max(1f, (bottom - top - 44f) / 100f);
            for (int i = 0; i < n; i++)
            {
                float h = _values[i] * scale; float bx = left + 6 + i * slot; RectangleF r = new RectangleF(bx, baseY - h, barWidth, h);
                Color color = _sorted.Contains(i) ? ZenTheme.Accent : (i == _pivot ? Color.FromArgb(255, 194, 92) : ((i == _compareA || i == _compareB) ? ZenTheme.Primary : Color.FromArgb(79, 89, 124)));
                using (SolidBrush brush = new SolidBrush(color)) g.FillRectangle(brush, r);
                if (n <= 36 && barWidth >= 13) { using (SolidBrush text = new SolidBrush(ZenTheme.Foreground)) { string s = _values[i].ToString(); SizeF size = g.MeasureString(s, Font); g.DrawString(s, Font, text, bx + (barWidth - size.Width) / 2, Math.Max(top + 3, baseY - h - size.Height - 1)); } }
            }
            DrawPseudoCode(g, left + 10, bottom - 6);
        }
        private void DrawPseudoCode(Graphics g, int x, int y)
        {
            string[] lines = PseudoLines(); int start = Math.Max(0, lines.Length - 3);
            using (SolidBrush panel = new SolidBrush(Color.FromArgb(240, ZenTheme.Surface)))
            using (SolidBrush regular = new SolidBrush(ZenTheme.Muted))
            using (SolidBrush active = new SolidBrush(ZenTheme.Accent))
            {
                g.FillRectangle(panel, x, y - 53, Math.Min(Width - x - 18, 420), 48);
                for (int i = start; i < lines.Length; i++) g.DrawString(lines[i], _monoFont, (i == (start + _step % Math.Max(1, lines.Length - start)) ? active : regular), x + 6, y - 50 + (i - start) * 14);
            }
        }
        private string[] PseudoLines()
        {
            switch (_algorithm)
            {
                case SortingAlgorithm.Bubble: return new[] { "for end = n-1 .. 1", "  compare neighbors; swap if out of order", "  mark end as sorted" };
                case SortingAlgorithm.Insertion: return new[] { "for i = 1 .. n-1", "  move item left until prefix is sorted", "  mark prefix position as sorted" };
                case SortingAlgorithm.Selection: return new[] { "for each position i", "  scan suffix for a smaller value", "  swap minimum into position i" };
                case SortingAlgorithm.Merge: return new[] { "split into runs of increasing width", "merge adjacent runs into sorted order", "write merged run back into array" };
                default: return new[] { "choose last item as pivot", "partition values around the pivot", "repeat recursively on each side" };
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed) { _disposed = true; _timer.Stop(); _timer.Dispose(); _titleFont.Dispose(); _monoFont.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake