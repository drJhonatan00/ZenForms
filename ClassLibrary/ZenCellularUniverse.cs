using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenCellularRulePreset { ConwayLife, HighLife, Seeds, BriansBrain, Custom }

    public sealed class ZenCellularUniverseChangedEventArgs : EventArgs
    {
        public long Generation { get; private set; }
        public int Population { get; private set; }
        internal ZenCellularUniverseChangedEventArgs(long generation, int population) { Generation = generation; Population = population; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Cellular automata 2D interactive lab, with editable rules, patterns and zoom.")]
    public class ZenCellularUniverse : UserControl
    {
        private const int MaxCells = 250000;
        private readonly Timer _timer;
        private readonly Canvas _canvas;
        private readonly Panel _toolbar;
        private readonly Label _status;
        private byte[,] _cells;
        private byte[,] _next;
        private int _columns, _rows, _speed = 10;
        private int _birthMask = 1 << 3, _survivalMask = (1 << 2) | (1 << 3);
        private ZenCellularRulePreset _preset = ZenCellularRulePreset.ConwayLife;
        private bool _brain, _running, _updatingControls;
        private string _customRule = "B3/S23";
        private long _generation;
        private int _population;
        private int _seed = 1;
        private Random _random = new Random(1);
        private readonly NumericUpDown _speedInput;
        private readonly ComboBox _rulesInput;
        private const int HeaderHeight = 40, FooterHeight = 27;
        public event EventHandler<ZenCellularUniverseChangedEventArgs> UniverseChanged;
        public event EventHandler RunningChanged;
        public ZenCellularUniverse() : this(80, 50) { }

        public ZenCellularUniverse(int columns, int rows)
        {
            int w, h; ValidateDimensions(columns, rows, out w, out h);
            _columns = w; _rows = h; _cells = new byte[w, h]; _next = new byte[w, h];
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(250, 180);
            _toolbar = new Panel { Dock = DockStyle.Top, Height = HeaderHeight, BackColor = ZenTheme.Surface };
            _canvas = new Canvas(this) { Dock = DockStyle.Fill, BackColor = ZenTheme.Background, TabStop = true };
            _status = new Label { Dock = DockStyle.Bottom, Height = FooterHeight, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 4, 0), ForeColor = ZenTheme.Muted, BackColor = ZenTheme.Surface };
            Controls.Add(_canvas); Controls.Add(_status); Controls.Add(_toolbar);
            AddButton("▶ / ❚❚", 8, 68, delegate { ToggleRunning(); });
            AddButton("Step", 82, 54, delegate { Step(); });
            AddButton("Clear", 142, 58, delegate { Clear(); });
            AddButton("Random", 206, 68, delegate { Randomize(_seed); });
            _rulesInput = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 125, Location = new Point(282, 8), BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, FlatStyle = FlatStyle.Flat };
            _rulesInput.Items.AddRange(new object[] { "Conway Life", "HighLife", "Seeds", "Brian's Brain", "Custom B/S" }); _rulesInput.SelectedIndex = 0;
            _rulesInput.SelectedIndexChanged += delegate { if (!_updatingControls) SetPreset((ZenCellularRulePreset)_rulesInput.SelectedIndex); };
            _toolbar.Controls.Add(_rulesInput);
            AddLabel("Speed:", 420, 9, 49);
            _speedInput = new NumericUpDown { Minimum = 1, Maximum = 60, Value = 10, Width = 54, Location = new Point(470, 8), BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle };
            _speedInput.ValueChanged += delegate { Speed = (int)_speedInput.Value; };
            _toolbar.Controls.Add(_speedInput);
            _timer = new Timer(); _timer.Interval = 100; _timer.Tick += delegate { Step(); };
            UpdateStatus();
        }
        [Category("Behavior"), Description("Grid width on cells; the total grid is limited: 250.000 cells.")]
        public int Columns { get { return _columns; } }
        [Category("Behavior"), Description("Grid height on cells.")]
        public int Rows { get { return _rows; } }
        [Browsable(false)] public long Generation { get { return _generation; } }
        [Browsable(false)] public int Population { get { return _population; } }
        [Browsable(false)] public bool IsRunning { get { return _running; } }
        [Category("Behavior"), Description("Simulation speed and updates per second.")]
        public int Speed { get { return _speed; } set { _speed = Math.Max(1, Math.Min(60, value)); _timer.Interval = Math.Max(16, 1000 / _speed); if (_speedInput != null && _speedInput.Value != _speed) _speedInput.Value = _speed; } }
        [Category("Behavior"), Description("Selected cell rule.")]
        public ZenCellularRulePreset RulePreset { get { return _preset; } set { SetPreset(value); } }
        [Category("Behavior"), Description("Customized B/S rule, how B3/S23.")]
        public string CustomRule { get { return _customRule; } set { int b, s; ParseRule(value, out b, out s); _customRule = FormatRule(b, s); _birthMask = b; _survivalMask = s; if (_preset == ZenCellularRulePreset.Custom) RaiseChanged(); } }
        [Category("Behavior"), Description("Seed reproducible to Randomize().")]
        public int RandomSeed { get { return _seed; } set { _seed = value; } }
        [Browsable(false)] public float Zoom { get { return _canvas.Zoom; } }
        public void ToggleRunning() { SetRunning(!_running); }
        public void SetRunning(bool running) { if (_running == running) return; _running = running; _timer.Enabled = running; if (RunningChanged != null) RunningChanged(this, EventArgs.Empty); UpdateStatus(); _canvas.Invalidate(); }
        public void Step()
        {
            int count = 0;
            for (int y = 0; y < _rows; y++) for (int x = 0; x < _columns; x++)
                {
                    int neighbors = 0;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            byte neighbor = _cells[Wrap(x + dx, _columns), Wrap(y + dy, _rows)];
                            if (_brain ? neighbor == 1 : neighbor > 0) neighbors++;
                        }
                    byte value;
                    if (_brain) value = _cells[x, y] == 1 ? (byte)2 : (_cells[x, y] == 2 ? (byte)0 : (neighbors == 2 ? (byte)1 : (byte)0));
                    else { bool alive = _cells[x, y] > 0; bool nextAlive = ((alive ? _survivalMask : _birthMask) & (1 << neighbors)) != 0; value = nextAlive ? (byte)(alive ? Math.Min(255, _cells[x, y] + 1) : 1) : (byte)0; }
                    _next[x, y] = value; if (_brain ? value == 1 : value > 0) count++;
                }
            byte[,] swap = _cells; _cells = _next; _next = swap; _generation++; _population = count; _canvas.Invalidate(); RaiseChanged();
        }
        public void Clear() { Array.Clear(_cells, 0, _cells.Length); Array.Clear(_next, 0, _next.Length); _generation = 0; _population = 0; _canvas.Invalidate(); RaiseChanged(); }
        public void Randomize(int seed, double density = 0.28)
        {
            if (density < 0 || density > 1 || Double.IsNaN(density)) throw new ArgumentOutOfRangeException("density");
            _seed = seed; _random = ResetRandom(seed); _generation = 0; _population = 0;
            for (int y = 0; y < _rows; y++) for (int x = 0; x < _columns; x++) { _cells[x, y] = _random.NextDouble() < density ? (byte)1 : (byte)0; if (_cells[x, y] == 1) _population++; }
            Array.Clear(_next, 0, _next.Length); _canvas.Invalidate(); RaiseChanged();
        }
        public void SetCell(int x, int y, bool alive) { CheckCell(x, y); _cells[x, y] = alive ? (byte)1 : (byte)0; Recount(); _canvas.Invalidate(); RaiseChanged(); }
        public bool GetCell(int x, int y) { CheckCell(x, y); return _brain ? _cells[x, y] == 1 : _cells[x, y] > 0; }
        public void LoadPattern(string pattern, int originX = 0, int originY = 0, bool clearFirst = false)
        {
            if (pattern == null) throw new ArgumentNullException("pattern");
            if (clearFirst) Array.Clear(_cells, 0, _cells.Length);
            string[] lines = pattern.Replace("\r", "").Split('\n'); int y = originY;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i]; if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.IndexOf("x =", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf("rule", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                int x = originX;
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j]; if (c == 'b' || c == '.') x++; else if (c == 'o' || c == 'O' || c == 'X' || c == '*') { if (x >= 0 && x < _columns && y >= 0 && y < _rows) _cells[x, y] = 1; x++; } else if (c == '$') { y++; x = originX; } else if (c == '!') break;
                }
                y++;
            }
            _generation = 0; Recount(); _canvas.Invalidate(); RaiseChanged();
        }
        public string ExportText() { StringBuilder sb = new StringBuilder(); for (int y = 0; y < _rows; y++) { for (int x = 0; x < _columns; x++) sb.Append(_cells[x, y] == 0 ? '.' : 'O'); if (y + 1 < _rows) sb.AppendLine(); } return sb.ToString(); }
        public string ExportRle()
        {
            StringBuilder sb = new StringBuilder(); sb.Append("x = ").Append(_columns).Append(", y = ").Append(_rows).Append(", rule = ").Append(_brain ? "B2/S" : _customRule).AppendLine();
            StringBuilder body = new StringBuilder(); char last = '\0'; int run = 0;
            Action flush = delegate { if (run > 0) { if (run > 1) body.Append(run.ToString(CultureInfo.InvariantCulture)); body.Append(last); run = 0; } };
            for (int y = 0; y < _rows; y++) { for (int x = 0; x < _columns; x++) { char c = _cells[x, y] == 0 ? 'b' : 'o'; if (c != last) { flush(); last = c; } run++; } if (y < _rows - 1) { flush(); if (body.Length > 0 && body[body.Length - 1] == 'b') body.Length--; body.Append('$'); last = '\0'; } }
            flush(); while (body.Length > 0 && body[body.Length - 1] == 'b') body.Length--; body.Append('!'); sb.Append(body); return sb.ToString();
        }
        public void ResetView() { _canvas.ResetView(); }

        private void SetPreset(ZenCellularRulePreset p)
        {
            if (!Enum.IsDefined(typeof(ZenCellularRulePreset), p)) throw new ArgumentOutOfRangeException("value"); _preset = p; _brain = p == ZenCellularRulePreset.BriansBrain;
            switch (p) { case ZenCellularRulePreset.ConwayLife: _birthMask = 1 << 3; _survivalMask = (1 << 2) | (1 << 3); _customRule = "B3/S23"; break; case ZenCellularRulePreset.HighLife: _birthMask = (1 << 3) | (1 << 6); _survivalMask = (1 << 2) | (1 << 3); _customRule = "B36/S23"; break; case ZenCellularRulePreset.Seeds: _birthMask = 1 << 2; _survivalMask = 0; _customRule = "B2/S"; break; case ZenCellularRulePreset.BriansBrain: _customRule = "B2/S"; break; case ZenCellularRulePreset.Custom: ParseRule(_customRule, out _birthMask, out _survivalMask); break; }
            _updatingControls = true; if (_rulesInput.SelectedIndex != (int)p) _rulesInput.SelectedIndex = (int)p; _updatingControls = false; _canvas.Invalidate(); RaiseChanged();
        }
        private static void ParseRule(string rule, out int birth, out int survival)
        {
            if (rule == null) throw new ArgumentNullException("rule"); string s = rule.ToUpperInvariant().Replace(" ", ""); int bi = s.IndexOf('B'), si = s.IndexOf('S');
            if (bi < 0 || si < 0 || bi > si) throw new ArgumentException("Use B/S notation, example: B3/S23.", "rule");
            birth = ParseMask(s.Substring(bi + 1, si - bi - 1)); survival = ParseMask(s.Substring(si + 1));
        }
        private static int ParseMask(string digits) { int mask = 0; for (int i = 0; i < digits.Length; i++) { mask |= 1 << (digits[i] - '0'); } return mask; }
        private static string FormatRule(int b, int s) { StringBuilder r = new StringBuilder("B"); for (int i = 0; i <= 8; i++) if ((b & (1 << i)) != 0) r.Append(i); r.Append("/S"); for (int i = 0; i <= 8; i++) if ((s & (1 << i)) != 0) r.Append(i); return r.ToString(); }
        private static void ValidateDimensions(int columns, int rows, out int w, out int h) { if (columns < 1 || rows < 1 || columns > 500 || rows > 500 || (long)columns * rows > MaxCells) throw new ArgumentOutOfRangeException("columns", "The grid must contain beetwen 1 and 500 cells per axis and in max 250.000 cells."); w = columns; h = rows; }
        private void CheckCell(int x, int y) { if (x < 0 || x >= _columns) throw new ArgumentOutOfRangeException("x"); if (y < 0 || y >= _rows) throw new ArgumentOutOfRangeException("y"); }
        private static int Wrap(int n, int max) { if (n < 0) return n + max; if (n >= max) return n - max; return n; }
        private Random ResetRandom(int seed) { return new Random(seed); }
        private void Recount() { int n = 0; for (int y = 0; y < _rows; y++) for (int x = 0; x < _columns; x++) if (_brain ? _cells[x, y] == 1 : _cells[x, y] > 0) n++; _population = n; }
        private void RaiseChanged() { UpdateStatus(); EventHandler<ZenCellularUniverseChangedEventArgs> h = UniverseChanged; if (h != null) h(this, new ZenCellularUniverseChangedEventArgs(_generation, _population)); }
        private void UpdateStatus() { if (_status != null && !_status.IsDisposed) _status.Text = String.Format(CultureInfo.InvariantCulture, "{0}   •   Generation {1}   •   Population {2}   •   {3}   •   Click/drag to edit · scrool: zoom · right button: pan", _running ? "IN PROGRESS" : "PAUSED", _generation, _population, _preset == ZenCellularRulePreset.BriansBrain ? "Brian's Brain" : _customRule); }
        private void AddButton(string text, int x, int width, Action action) { Button b = new Button { Text = text, Location = new Point(x, 7), Size = new Size(width, 26), FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, TabStop = false }; b.FlatAppearance.BorderColor = ZenTheme.Border; b.Click += delegate { action(); }; _toolbar.Controls.Add(b); }
        private void AddLabel(string text, int x, int y, int width) { _toolbar.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 23), TextAlign = ContentAlignment.MiddleRight, ForeColor = ZenTheme.Muted }); }
        protected override void Dispose(bool disposing) { if (disposing) { if (_timer != null) { _timer.Stop(); _timer.Dispose(); } } base.Dispose(disposing); }

        private sealed class Canvas : Control
        {
            private readonly ZenCellularUniverse _owner;
            private float _zoom = 10f, _offsetX, _offsetY;
            private bool _painting, _panning;
            private Point _last;
            private bool _paintValue;
            internal float Zoom { get { return _zoom; } }
            internal Canvas(ZenCellularUniverse owner) { _owner = owner; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
            internal void ResetView() { _zoom = 10f; _offsetX = 0; _offsetY = 0; Invalidate(); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); Graphics g = e.Graphics; g.Clear(ZenTheme.Background); g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                int x0 = Math.Max(0, (int)Math.Floor(-_offsetX / _zoom)), y0 = Math.Max(0, (int)Math.Floor(-_offsetY / _zoom));
                int x1 = Math.Min(_owner._columns, (int)Math.Ceiling((Width - _offsetX) / _zoom)), y1 = Math.Min(_owner._rows, (int)Math.Ceiling((Height - _offsetY) / _zoom));
                using (SolidBrush dead = new SolidBrush(ZenTheme.Surface)) using (Pen grid = new Pen(Color.FromArgb(29, 55, 65, 91))) using (Pen alive = new Pen(Color.FromArgb(70, 124, 92, 255))) using (SolidBrush active = new SolidBrush(ZenTheme.Primary)) using (SolidBrush brain = new SolidBrush(ZenTheme.Accent))
                {
                    for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                        {
                            float px = _offsetX + x * _zoom, py = _offsetY + y * _zoom; byte v = _owner._cells[x, y];
                            if (v == 0) { if (_zoom >= 6) g.FillRectangle(dead, px + 1, py + 1, Math.Max(1, _zoom - 2), Math.Max(1, _zoom - 2)); }
                            else if (_owner._brain && v == 2) g.FillRectangle(brain, px + 1, py + 1, Math.Max(1, _zoom - 2), Math.Max(1, _zoom - 2));
                            else { int age = Math.Min((byte)255, (byte)v); int glow = 100 + (age * 155 / 255); using (SolidBrush aged = new SolidBrush(Color.FromArgb(glow, 75 + age / 2, 80, 255))) g.FillRectangle(aged, px + 1, py + 1, Math.Max(1, _zoom - 2), Math.Max(1, _zoom - 2)); }
                        }
                    if (_zoom >= 5) { for (int x = x0; x <= x1; x++) g.DrawLine(grid, _offsetX + x * _zoom, _offsetY + y0 * _zoom, _offsetX + x * _zoom, _offsetY + y1 * _zoom); for (int y = y0; y <= y1; y++) g.DrawLine(grid, _offsetX + x0 * _zoom, _offsetY + y * _zoom, _offsetX + x1 * _zoom, _offsetY + y * _zoom); }
                }
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                Focus(); _last = e.Location;
                if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle) { _panning = true; Capture = true; return; }
                if (e.Button == MouseButtons.Left) { _painting = true; Capture = true; int x, y; if (CellAt(e.Location, out x, out y)) { _paintValue = _owner._cells[x, y] == 0; PaintCell(x, y); } }
                base.OnMouseDown(e);
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (_panning) { _offsetX += e.X - _last.X; _offsetY += e.Y - _last.Y; _last = e.Location; Invalidate(); }
                else if (_painting) { int x, y; if (CellAt(e.Location, out x, out y)) PaintCell(x, y); }
                base.OnMouseMove(e);
            }
            protected override void OnMouseUp(MouseEventArgs e) { _painting = false; _panning = false; Capture = false; base.OnMouseUp(e); }
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                float old = _zoom; _zoom = Math.Max(3f, Math.Min(40f, _zoom * (e.Delta > 0 ? 1.15f : 1f / 1.15f)));
                _offsetX = e.X - (e.X - _offsetX) * _zoom / old; _offsetY = e.Y - (e.Y - _offsetY) * _zoom / old; Invalidate(); base.OnMouseWheel(e);
            }
            protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { _owner.ToggleRunning(); e.Handled = true; } else if (e.KeyCode == Keys.Right) { _owner.Step(); e.Handled = true; } else if (e.KeyCode == Keys.Delete) { _owner.Clear(); e.Handled = true; } base.OnKeyDown(e); }
            private bool CellAt(Point p, out int x, out int y) { x = (int)Math.Floor((p.X - _offsetX) / _zoom); y = (int)Math.Floor((p.Y - _offsetY) / _zoom); return x >= 0 && x < _owner._columns && y >= 0 && y < _owner._rows; }
            private void PaintCell(int x, int y) { byte v = _paintValue ? (byte)1 : (byte)0; if (_owner._cells[x, y] != v) { _owner._cells[x, y] = v; _owner.Recount(); _owner.RaiseChanged(); Invalidate(); } }
        }
    }
}

//Watashi wa watashi sore dake