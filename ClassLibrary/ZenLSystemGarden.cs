using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Build and explore procedual plants.")]
    public class ZenLSystemGarden : UserControl
    {
        private TextBox _axiomBox, _rulesBox;
        private NumericUpDown _iterations, _angle, _length, _seed, _width;
        private ComboBox _presetBox;
        private Button _generateButton, _exportButton;
        private Panel _canvas;
        private Label _status;
        private Bitmap _image;
        private CancellationTokenSource _renderCancellation;
        private float _zoom = 1f;
        private PointF _pan = PointF.Empty;
        private Point _dragOrigin;
        private PointF _panOrigin;
        private bool _dragging;
        private string _selectedRule;
        private bool _suppress;
        private const int MaxSymbols = 250000;
        private const int MaxIterations = 8;
        private readonly Color[] _palette = { Color.FromArgb(100, 214, 135), Color.FromArgb(67, 180, 139), Color.FromArgb(184, 219, 104), Color.FromArgb(146, 116, 232), Color.FromArgb(237, 154, 199) };
        public event EventHandler GenerationCompleted;
        public ZenLSystemGarden()
        {
            MinimumSize = new Size(480, 360); Size = new Size(820, 560); DoubleBuffered = true;
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            BuildUi(); ApplyPreset("Fern");
        }
        [Category("L-System"), Description("Sybol or initial gramatical group.")]
        public string Axiom { get { return _axiomBox.Text; } set { _axiomBox.Text = value ?? string.Empty; } }
        [Category("L-System"), Description("Production rules on format symbol=replace.")]
        public string Rules { get { return _rulesBox.Text; } set { _rulesBox.Text = value ?? string.Empty; } }
        [Category("L-System"), Description("Iterations number, 0 - 8.")]
        public int Iterations { get { return (int)_iterations.Value; } set { _iterations.Value = Math.Max(0, Math.Min(MaxIterations, value)); } }
        [Category("L-System"), Description("Angle to commands + e -.")]
        public float TurnAngle { get { return (float)_angle.Value; } set { _angle.Value = (decimal)Math.Max(1, Math.Min(180, value)); } }
        [Category("L-System"), Description("Step width on pixels to every segment.")]
        public float StepLength { get { return (float)_length.Value; } set { _length.Value = (decimal)Math.Max(1, Math.Min(300, value)); } }
        [Category("L-System"), Description("Seed to determinated variation.")]
        public int VariationSeed { get { return (int)_seed.Value; } set { _seed.Value = Math.Max(0, Math.Min(999999, value)); } }
        [Category("Appearance"), Description("Initial pixel width.")]
        public float BranchWidth { get { return (float)_width.Value; } set { _width.Value = (decimal)Math.Max(1, Math.Min(30, value)); } }

        private void BuildUi()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = ZenTheme.Surface, Padding = new Padding(8) };
            Controls.Add(top);
            _presetBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 112, Left = 2, Top = 22 };
            _presetBox.Items.AddRange(new object[] { "Fern", "Tree", "Coral", "Flake" }); _presetBox.SelectedIndexChanged += delegate { if (!_suppress) ApplyPreset(Convert.ToString(_presetBox.SelectedItem)); };
            top.Controls.Add(_presetBox);
            AddField(top, "Iter", 124, 0, 8, 4, out _iterations); AddField(top, "Angle", 214, 1, 180, 25, out _angle);
            AddField(top, "Step", 304, 1, 300, 48, out _length); AddField(top, "Seed", 394, 0, 999999, 17, out _seed); AddField(top, "Width", 484, 1, 30, 4, out _width);
            _generateButton = new Button { Text = "Generate", Left = 574, Top = 20, Width = 78, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.Primary, ForeColor = Color.White };
            _generateButton.Click += delegate { Generate(); }; top.Controls.Add(_generateButton);
            _exportButton = new Button { Text = "Export PNG", Left = 658, Top = 20, Width = 112, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground };
            _exportButton.Click += delegate { ExportPng(); }; top.Controls.Add(_exportButton);
            var editor = new Panel { Dock = DockStyle.Left, Width = 245, BackColor = ZenTheme.Surface, Padding = new Padding(8) }; Controls.Add(editor);
            var axLabel = new Label { Text = "Axiom", Dock = DockStyle.Top, Height = 20, ForeColor = ZenTheme.Accent }; editor.Controls.Add(axLabel);
            _axiomBox = MakeEditor(30); editor.Controls.Add(_axiomBox);
            var ruleLabel = new Label { Text = "Rules (click on a line to select)", Dock = DockStyle.Top, Height = 24, ForeColor = ZenTheme.Accent }; editor.Controls.Add(ruleLabel);
            _rulesBox = MakeEditor(190); _rulesBox.Multiline = true; _rulesBox.ScrollBars = ScrollBars.Vertical; _rulesBox.AcceptsReturn = true; _rulesBox.TextChanged += delegate { if (!_suppress) _status.Text = "Rules changed — click Generate to update."; }; _rulesBox.MouseClick += RuleClicked; editor.Controls.Add(_rulesBox);
            _status = new Label { Dock = DockStyle.Bottom, Height = 42, ForeColor = ZenTheme.Muted, Text = "Choose a gramatic and generate." }; editor.Controls.Add(_status);
            _canvas = new Panel { Dock = DockStyle.Fill, BackColor = ZenTheme.Background, Cursor = Cursors.Hand }; _canvas.Paint += PaintCanvas; _canvas.MouseDown += CanvasDown; _canvas.MouseMove += CanvasMove; _canvas.MouseUp += delegate { _dragging = false; }; _canvas.MouseWheel += CanvasWheel; _canvas.Resize += delegate { _canvas.Invalidate(); }; Controls.Add(_canvas); _canvas.BringToFront();
            editor.BringToFront(); top.BringToFront();
        }
        private static TextBox MakeEditor(int height) { return new TextBox { Dock = DockStyle.Top, Height = height, BackColor = ZenTheme.Background, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle, Font = SystemFonts.MessageBoxFont }; }
        private static void AddField(Control parent, string label, int left, int min, int max, int val, out NumericUpDown field)
        {
            var l = new Label { Text = label, Left = left, Top = 0, Width = 82, Height = 18, ForeColor = ZenTheme.Muted }; parent.Controls.Add(l);
            field = new NumericUpDown { Left = left, Top = 23, Width = 80, Minimum = min, Maximum = max, Value = val, BackColor = ZenTheme.Background, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle }; parent.Controls.Add(field);
        }
        private void ApplyPreset(string name)
        {
            _suppress = true;
            if (name == "Tree") { Axiom = "F"; Rules = "F=F[+F]F[-F]F"; Iterations = 4; TurnAngle = 25.7f; StepLength = 72; BranchWidth = 5; }
            else if (name == "Coral") { Axiom = "F"; Rules = "F=F[+F]F[-F][F]"; Iterations = 4; TurnAngle = 22.5f; StepLength = 61; BranchWidth = 4; }
            else if (name == "Flake") { Axiom = "F--F--F"; Rules = "F=F+F--F+F"; Iterations = 3; TurnAngle = 60; StepLength = 64; BranchWidth = 3; }
            else { Axiom = "X"; Rules = "X=F+[[X]-X]-F[-FX]+X\nF=FF"; Iterations = 4; TurnAngle = 25; StepLength = 66; BranchWidth = 4; }
            _suppress = false; if (_presetBox.SelectedItem == null || Convert.ToString(_presetBox.SelectedItem) != name) _presetBox.SelectedItem = name;
        }
        private void RuleClicked(object sender, MouseEventArgs e)
        {
            int index = _rulesBox.GetLineFromCharIndex(_rulesBox.GetCharIndexFromPosition(e.Location)); string[] lines = _rulesBox.Lines;
            if (index >= 0 && index < lines.Length) { string line = lines[index]; int eq = line.IndexOf('='); _selectedRule = eq > 0 ? line.Substring(0, eq).Trim() : null; _status.Text = _selectedRule == null ? "Line without valid rule." : "Selected rule: " + _selectedRule + " — click Generate to redraw."; }
        }
        public void Generate()
        {
            if (IsDisposed || !IsHandleCreated) return;
            Dictionary<char, string> rules; string axiom = Axiom; int iterations = Iterations; float angle = TurnAngle, length = StepLength, width = BranchWidth; int seed = VariationSeed;
            try { rules = ParseRules(Rules); ValidateGrammar(axiom, rules); }
            catch (Exception ex) { _status.Text = ex.Message; return; }
            if (_renderCancellation != null) { _renderCancellation.Cancel(); _renderCancellation.Dispose(); }
            _renderCancellation = new CancellationTokenSource(); CancellationToken token = _renderCancellation.Token;
            _generateButton.Enabled = false; _status.Text = "Expanding and drawing…";
            Task.Run(delegate {
                Stopwatch sw = Stopwatch.StartNew(); Bitmap result = null; long segments = 0;
                try { string sequence = Expand(axiom, rules, iterations, token); result = Render(sequence, angle, length, width, seed, token, out segments); sw.Stop(); }
                catch (OperationCanceledException) { if (result != null) result.Dispose(); return; }
                catch (Exception ex) { if (result != null) result.Dispose(); BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) { _generateButton.Enabled = true; _status.Text = ex.Message; } }); return; }
                Bitmap ready = result; long count = segments; long elapsed = sw.ElapsedMilliseconds;
                try { BeginInvoke((MethodInvoker)delegate { if (IsDisposed || token.IsCancellationRequested) { ready.Dispose(); return; } Bitmap old = _image; _image = ready; if (old != null) old.Dispose(); _pan = PointF.Empty; _zoom = 1f; _generateButton.Enabled = true; _status.Text = count.ToString("N0") + " segments · " + elapsed + " ms · " + sequenceLengthInfo(ready); _canvas.Invalidate(); EventHandler handler = GenerationCompleted; if (handler != null) handler(this, EventArgs.Empty); }); }
                catch (InvalidOperationException) { ready.Dispose(); }
            }, token);
        }
        private static string sequenceLengthInfo(Bitmap bitmap) { return bitmap.Width + "×" + bitmap.Height; }
        private static Dictionary<char, string> ParseRules(string text)
        {
            var result = new Dictionary<char, string>(); string[] lines = (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string raw in lines) { string line = raw.Trim(); int eq = line.IndexOf('='); if (eq != 1 || line.IndexOf('=', eq + 1) >= 0) throw new ArgumentException("Every rule must be a sybol + '=' + a raplace."); char key = line[0]; if (char.IsWhiteSpace(key) || "[]+-\\/|".IndexOf(key) >= 0) throw new ArgumentException("Invalid rule symbol: " + key); string value = line.Substring(eq + 1).Trim(); if (value.Length > MaxSymbols) throw new ArgumentException("One rule exceds the symbols limit."); if (result.ContainsKey(key)) throw new ArgumentException("Duplicated rule " + key + "."); result.Add(key, value); }
            if (result.Count == 0) throw new ArgumentException("Add a rule."); return result;
        }
        private static void ValidateGrammar(string axiom, Dictionary<char, string> rules)
        {
            if (string.IsNullOrWhiteSpace(axiom) || axiom.Length > MaxSymbols) throw new ArgumentException("The axiom must contain between 1 and " + MaxSymbols + " symbols.");
            int depth = 0; foreach (char c in axiom) { if (c == '[') depth++; if (c == ']' && --depth < 0) throw new ArgumentException("The axiom have desiquilibred square brackets."); }
            if (depth != 0) throw new ArgumentException("The axiom have desiquilibred square brackets.");
            foreach (KeyValuePair<char, string> rule in rules) { depth = 0; foreach (char c in rule.Value) { if (c == '[') depth++; else if (c == ']' && --depth < 0) throw new ArgumentException("Square brackets are unbalanced on rule " + rule.Key + "."); } if (depth != 0) throw new ArgumentException("Square brackets are unbalanced on rule " + rule.Key + "."); }
        }
        private static string Expand(string axiom, Dictionary<char, string> rules, int iterations, CancellationToken token)
        {
            string current = axiom;
            for (int i = 0; i < iterations; i++) { token.ThrowIfCancellationRequested(); var next = new System.Text.StringBuilder(Math.Min(MaxSymbols, current.Length * 2)); foreach (char c in current) { string replacement; string append = rules.TryGetValue(c, out replacement) ? replacement : c.ToString(); if (next.Length + append.Length > MaxSymbols) throw new InvalidOperationException("It will exceds the secure max of" + MaxSymbols + " symbols."); next.Append(append); } current = next.ToString(); }
            return current;
        }
        private struct Turtle { public double X, Y, Heading; public float Width; public Turtle(double x, double y, double heading, float width) { X = x; Y = y; Heading = heading; Width = width; } }
        private sealed class Segment { public float X1, Y1, X2, Y2, Width; public Color Color; }
        private static Bitmap Render(string grammar, float angle, float step, float branchWidth, int seed, CancellationToken token, out long count)
        {
            var segments = new List<Segment>(); var stack = new Stack<Turtle>(); var rng = new Random(seed); var t = new Turtle(0, 0, -Math.PI / 2, branchWidth); double minX = 0, maxX = 0, minY = 0, maxY = 0; double radians = angle * Math.PI / 180.0;
            foreach (char c in grammar)
            {
                if ((segments.Count & 1023) == 0) token.ThrowIfCancellationRequested(); if (c == 'F' || c == 'G') { double jitter = (rng.NextDouble() - .5) * .045; double heading = t.Heading + jitter; double nx = t.X + Math.Cos(heading) * step, ny = t.Y + Math.Sin(heading) * step; segments.Add(new Segment { X1 = (float)t.X, Y1 = (float)t.Y, X2 = (float)nx, Y2 = (float)ny, Width = t.Width, Color = Color.Empty }); t.X = nx; t.Y = ny; minX = Math.Min(minX, nx); maxX = Math.Max(maxX, nx); minY = Math.Min(minY, ny); maxY = Math.Max(maxY, ny); }
                else if (c == '+') t.Heading += radians; else if (c == '-') t.Heading -= radians; else if (c == '&') t.Heading += radians / 2; else if (c == '^') t.Heading -= radians / 2; else if (c == '|') t.Heading += Math.PI; else if (c == '[') stack.Push(t); else if (c == ']') { if (stack.Count == 0) throw new InvalidOperationException("Comand ']' without correpondent destination."); t = stack.Pop(); t.Width = Math.Max(1f, t.Width * .72f); }
            }
            if (stack.Count != 0) throw new InvalidOperationException("Some '[' wasn't closed."); count = segments.Count; int w = 1100, h = 760; var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            bool completed = false;
            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(ZenTheme.Background); g.SmoothingMode = SmoothingMode.AntiAlias; double spanX = Math.Max(1, maxX - minX), spanY = Math.Max(1, maxY - minY); float fit = (float)Math.Min((w - 60) / spanX, (h - 60) / spanY); double ox = (w - (maxX - minX) * fit) / 2 - minX * fit, oy = (h - (maxY - minY) * fit) / 2 - minY * fit;
                    for (int i = 0; i < segments.Count; i++) { if ((i & 1023) == 0) token.ThrowIfCancellationRequested(); Segment s = segments[i]; using (Pen p = new Pen(_ColorAt(i, segments.Count), Math.Max(.6f, s.Width * fit / Math.Max(step, 1)))) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawLine(p, (float)(ox + s.X1 * fit), (float)(oy + s.Y1 * fit), (float)(ox + s.X2 * fit), (float)(oy + s.Y2 * fit)); } }
                }
                completed = true;
                return bitmap;
            }
            finally { if (!completed) bitmap.Dispose(); }
        }
        private static Color _ColorAt(int index, int total) { double u = total <= 1 ? 0 : (double)index / total; return Color.FromArgb((int)(100 + 80 * u), (int)(85 + 125 * u), (int)(100 + 55 * (1 - u))); }
        private void PaintCanvas(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(ZenTheme.Background); if (_image == null) { using (Brush b = new SolidBrush(ZenTheme.Muted)) using (Font f = new Font(Font.FontFamily, 10f)) g.DrawString("The plant spawn here after generate", f, b, new PointF(20, 20)); return; }
            g.InterpolationMode = InterpolationMode.HighQualityBicubic; float scale = _zoom; float dw = _image.Width * scale, dh = _image.Height * scale; float x = (_canvas.ClientSize.Width - dw) / 2 + _pan.X, y = (_canvas.ClientSize.Height - dh) / 2 + _pan.Y; g.DrawImage(_image, new RectangleF(x, y, dw, dh));
            if (!string.IsNullOrEmpty(_selectedRule)) { using (Pen p = new Pen(Color.FromArgb(55, ZenTheme.Accent), 1)) { p.DashStyle = DashStyle.Dot; g.DrawRectangle(p, 5, 5, _canvas.Width - 11, _canvas.Height - 11); } }
        }
        private void CanvasDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _dragging = true; _dragOrigin = e.Location; _panOrigin = _pan; _canvas.Cursor = Cursors.SizeAll; } }
        private void CanvasMove(object sender, MouseEventArgs e) { if (_dragging) { _pan = new PointF(_panOrigin.X + e.X - _dragOrigin.X, _panOrigin.Y + e.Y - _dragOrigin.Y); _canvas.Invalidate(); } }
        private void CanvasWheel(object sender, MouseEventArgs e) { float old = _zoom; _zoom = Math.Max(.15f, Math.Min(8f, _zoom * (e.Delta > 0 ? 1.15f : .87f))); if (Math.Abs(_zoom - old) > .001f) _canvas.Invalidate(); }
        public void ExportPng()
        {
            if (_image == null) { _status.Text = "Generate an image before export."; return; }
            using (var dialog = new SaveFileDialog { Filter = "PNG Image (*.png)|*.png", DefaultExt = "png", AddExtension = true, FileName = "zen-plant.png", Title = "Export procedual plant" }) { if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { try { _image.Save(dialog.FileName, ImageFormat.Png); _status.Text = "PNG saved: " + Path.GetFileName(dialog.FileName); } catch (Exception ex) { _status.Text = "Failed to save: " + ex.Message; } } }
        }
        protected override void Dispose(bool disposing) { if (disposing) { if (_renderCancellation != null) { _renderCancellation.Cancel(); _renderCancellation.Dispose(); _renderCancellation = null; } if (_image != null) { _image.Dispose(); _image = null; } } base.Dispose(disposing); }
    }
}

// watashi wa watashi sore dake