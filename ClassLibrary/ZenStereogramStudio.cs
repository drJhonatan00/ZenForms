using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("A stereogram generator.")]
    public class ZenStereogramStudio : UserControl
    {
        private const int MaxPixels = 1400000;
        private readonly PictureBox _depthView;
        private readonly PictureBox _resultView;
        private readonly Button _generateButton;
        private readonly Button _saveButton;
        private readonly ComboBox _shapeBox;
        private readonly NumericUpDown _widthBox;
        private readonly NumericUpDown _heightBox;
        private readonly NumericUpDown _separationBox;
        private readonly NumericUpDown _depthBox;
        private readonly NumericUpDown _seedBox;
        private readonly Label _status;
        private CancellationTokenSource _generation;
        private Bitmap _depthBitmap;
        private Bitmap _resultBitmap;
        private int _generationId;
        private bool _disposed;
        private Color _dotColor = Color.FromArgb(224, 221, 255);
        private Color _backgroundColor = Color.FromArgb(23, 25, 42);
        private Color _depthColor = Color.FromArgb(124, 92, 255);

        [Category("Action")]
        public event EventHandler StereogramGenerated;

        public ZenStereogramStudio()
        {
            MinimumSize = new Size(540, 380);
            Size = new Size(820, 560);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            TabStop = true;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = ZenTheme.Background, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = ZenTheme.Background, Padding = new Padding(0, 0, 0, 8) };
            root.Controls.Add(bar, 0, 0);
            _shapeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 112 };
            _shapeBox.Items.AddRange(new object[] { "Circle", "Text", "Rings", "Rectangle" }); _shapeBox.SelectedIndex = 0;
            AddField(bar, "Form", _shapeBox);
            _widthBox = Numeric(160, 1024, 640); AddField(bar, "Width", _widthBox);
            _heightBox = Numeric(120, 768, 400); AddField(bar, "Height", _heightBox);
            _separationBox = Numeric(24, 160, 80); AddField(bar, "Separation", _separationBox);
            _depthBox = Numeric(1, 40, 12); AddField(bar, "Depth", _depthBox);
            _seedBox = Numeric(0, 999999999, 24680); AddField(bar, "Seed", _seedBox);
            _generateButton = new Button { Text = "Generate", AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.Primary, ForeColor = Color.White, Margin = new Padding(8, 3, 3, 3) };
            _generateButton.Click += delegate { GenerateAsync(); }; bar.Controls.Add(_generateButton);
            _saveButton = new Button { Text = "Save PNG", AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Margin = new Padding(3) };
            _saveButton.Click += SavePng; bar.Controls.Add(_saveButton);

            var views = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = ZenTheme.Background };
            views.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); views.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            views.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); views.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(views, 0, 1);
            _depthView = MakeView(); _resultView = MakeView();
            views.Controls.Add(_depthView, 0, 0); views.Controls.Add(_resultView, 1, 0);
            var depthLabel = Caption("MAP"); var resultLabel = Caption("STEREOGRAM · DIVERGENT FOCUS");
            views.Controls.Add(depthLabel, 0, 1); views.Controls.Add(resultLabel, 1, 1);
            _status = new Label { Dock = DockStyle.Bottom, AutoSize = true, ForeColor = ZenTheme.Muted, Text = "Configure and generate." };
            root.Controls.Add(_status, 0, 2);
            Resize += delegate { Invalidate(); };
            GenerateAsync();
        }

        [Category("Appearance")]
        public Color DotColor { get { return _dotColor; } set { _dotColor = value; } }
        [Category("Appearance")]
        public Color PatternBackground { get { return _backgroundColor; } set { _backgroundColor = value; } }
        [Category("Appearance")]
        public Color DepthColor { get { return _depthColor; } set { _depthColor = value; } }
        [Category("Layout")]
        public int ImageWidth { get { return (int)_widthBox.Value; } set { _widthBox.Value = Clamp(value, _widthBox.Minimum, _widthBox.Maximum); } }
        [Category("Layout")]
        public int ImageHeight { get { return (int)_heightBox.Value; } set { _heightBox.Value = Clamp(value, _heightBox.Minimum, _heightBox.Maximum); } }
        [Category("Behavior")]
        public int Separation { get { return (int)_separationBox.Value; } set { _separationBox.Value = Clamp(value, _separationBox.Minimum, _separationBox.Maximum); } }
        [Category("Behavior")]
        public int Seed { get { return (int)_seedBox.Value; } set { _seedBox.Value = Clamp(value, _seedBox.Minimum, _seedBox.Maximum); } }

        public async void GenerateAsync()
        {
            if (_disposed || IsDisposed || !IsHandleCreated) return;
            if ((long)ImageWidth * ImageHeight > MaxPixels) { _status.Text = "Pixels limit exceded."; return; }
            if (_generation != null) _generation.Cancel();
            var cts = new CancellationTokenSource(); _generation = cts;
            int id = Interlocked.Increment(ref _generationId);
            int w = ImageWidth, h = ImageHeight, sep = Separation, depth = (int)_depthBox.Value, seed = Seed, shape = _shapeBox.SelectedIndex;
            Color bg = _backgroundColor, dot = _dotColor, fg = _depthColor;
            _generateButton.Enabled = false; _status.Text = "Gerando…";
            try
            {
                Bitmap[] images = await Task.Run(() => Build(w, h, sep, depth, seed, shape, bg, dot, fg, cts.Token), cts.Token);
                if (_disposed || IsDisposed || id != _generationId) { images[0].Dispose(); images[1].Dispose(); return; }
                SwapImage(_depthView, ref _depthBitmap, images[0]); SwapImage(_resultView, ref _resultBitmap, images[1]);
                _status.Text = "Ready · seed " + seed + ".";
                var handler = StereogramGenerated; if (handler != null) handler(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!_disposed && !IsDisposed) _status.Text = "Fail: " + ex.Message; }
            finally { if (id == _generationId && !_disposed && !IsDisposed) _generateButton.Enabled = true; if (ReferenceEquals(_generation, cts)) _generation = null; cts.Dispose(); }
        }

        public void CancelGeneration() { if (_generation != null) _generation.Cancel(); }

        private static Bitmap[] Build(int w, int h, int sep, int maxDepth, int seed, int shape, Color bg, Color dot, Color fg, CancellationToken token)
        {
            int count = checked(w * h); var map = new byte[count]; var rnd = new Random(seed);
            int cx = w / 2, cy = h / 2; double rx = Math.Max(20, w * .27), ry = Math.Max(18, h * .32);
            string word = "ZEN";
            using (var font = new Font("Arial", Math.Max(32, h / 4), FontStyle.Bold, GraphicsUnit.Pixel))
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(Color.White))
            {
                g.Clear(Color.Black); g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (shape == 0) g.FillEllipse(brush, (float)(cx - rx), (float)(cy - ry), (float)(rx * 2), (float)(ry * 2));
                else if (shape == 1) { var size = g.MeasureString(word, font); g.DrawString(word, font, brush, (w - size.Width) / 2, (h - size.Height) / 2); }
                else if (shape == 2) { using (var ringPen = new Pen(Color.White, Math.Max(3, h / 45f))) for (int r = 1; r <= 4; r++) g.DrawEllipse(ringPen, cx - (float)(rx * r / 4), cy - (float)(ry * r / 4), (float)(rx * r / 2), (float)(ry * r / 2)); }
                else { for (int y = 0; y < h; y++) { byte v = (byte)(255.0 * y / Math.Max(1, h - 1)); for (int x = 0; x < w; x++) map[y * w + x] = v; } }
                if (shape != 3)
                {
                    BitmapData lockData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        int stride = lockData.Stride; byte[] raw = new byte[stride * h]; System.Runtime.InteropServices.Marshal.Copy(lockData.Scan0, raw, 0, raw.Length);
                        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (raw[y * stride + x * 4 + 3] > 0 && raw[y * stride + x * 4] > 128) map[y * w + x] = 255;
                    }
                    finally { bmp.UnlockBits(lockData); }
                }
            }
            token.ThrowIfCancellationRequested();
            Bitmap depthBmp = null, output = null;
            try
            {
                depthBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                for (int y = 0; y < h; y++) { token.ThrowIfCancellationRequested(); for (int x = 0; x < w; x++) { int d = map[y * w + x]; depthBmp.SetPixel(x, y, d == 0 ? ZenTheme.Surface : Color.FromArgb(45 + (int)(fg.R * .75), 35 + (int)(fg.G * .72), 80 + (int)(fg.B * .65))); } }
                output = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                int[] row = new int[w];
                for (int y = 0; y < h; y++)
                {
                    token.ThrowIfCancellationRequested(); int[] parent = new int[w]; for (int x = 0; x < w; x++) parent[x] = x;
                    for (int x = 0; x < w; x++)
                    {
                        int d = map[y * w + x]; int shift = d == 0 ? sep : Math.Max(2, sep - (int)((sep * .42) * Math.Min(maxDepth, 1 + d * maxDepth / 255.0) / maxDepth));
                        int left = x - shift / 2, right = left + shift;
                        if (left >= 0 && right < w && Compatible(parent, left, right)) Union(parent, left, right);
                    }
                    for (int x = 0; x < w; x++) { int root = Find(parent, x); row[x] = root == x ? RandomColor(rnd, bg, dot).ToArgb() : row[root]; }
                    for (int x = 0; x < w; x++) output.SetPixel(x, y, Color.FromArgb(row[x]));
                }
                Bitmap[] result = new Bitmap[] { depthBmp, output };
                depthBmp = null; output = null;
                return result;
            }
            finally { if (depthBmp != null) depthBmp.Dispose(); if (output != null) output.Dispose(); }
        }
        private static bool Compatible(int[] parent, int a, int b) { int lo = Math.Min(a, b), hi = Math.Max(a, b); for (int k = lo + 1; k < hi; k++) if (Find(parent, k) == Find(parent, lo)) return false; return true; }
        private static int Find(int[] p, int x) { while (p[x] != x) { p[x] = p[p[x]]; x = p[x]; } return x; }
        private static void Union(int[] p, int a, int b) { int ra = Find(p, a), rb = Find(p, b); if (ra != rb) p[rb] = ra; }
        private static Color RandomColor(Random r, Color bg, Color fg) { return (r.Next(2) == 0 ? bg : fg); }
        private static NumericUpDown Numeric(int min, int max, int value) { return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 62, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(3, 5, 3, 3) }; }
        private static void AddField(FlowLayoutPanel p, string label, Control input) { var box = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Margin = new Padding(2) }; box.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = ZenTheme.Muted, Margin = new Padding(2, 0, 0, 1) }); box.Controls.Add(input); p.Controls.Add(box); }
        private static PictureBox MakeView() { return new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = ZenTheme.Surface, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(4) }; }
        private static Label Caption(string text) { return new Label { Text = text, AutoSize = true, ForeColor = ZenTheme.Muted, Font = new Font(ZenTheme.DefaultFont, FontStyle.Bold), Padding = new Padding(7, 5, 0, 4) }; }
        private static decimal Clamp(int v, decimal min, decimal max) { return Math.Max(min, Math.Min(max, v)); }
        private static void SwapImage(PictureBox view, ref Bitmap old, Bitmap next) { view.Image = next; if (old != null) old.Dispose(); old = next; }
        private void SavePng(object sender, EventArgs e) { if (_resultBitmap == null) { _status.Text = "Generate an image before save."; return; } using (var dlg = new SaveFileDialog { Filter = "PNG image|*.png", DefaultExt = "png", FileName = "ZenStereogram.png" }) if (dlg.ShowDialog(this) == DialogResult.OK) _resultBitmap.Save(dlg.FileName, ImageFormat.Png); }
        protected override void Dispose(bool disposing) { if (disposing && !_disposed) { _disposed = true; if (_generation != null) _generation.Cancel(); if (_depthView != null) _depthView.Image = null; if (_resultView != null) _resultView.Image = null; if (_depthBitmap != null) _depthBitmap.Dispose(); if (_resultBitmap != null) _resultBitmap.Dispose(); } base.Dispose(disposing); }
    }
}

// watashi wa watshi sore dake