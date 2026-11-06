using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum VisionSimulationMode { None, Protanopia, Deuteranopia, Tritanopia, Grayscale }

    [ToolboxItem(true)]
    [Category("ZenForms")]
    [Description("Visual lab to explore vision deficiences.")]
    public class ZenVisionAccessibility : UserControl
    {
        private Bitmap _source;
        private Bitmap _simulated;
        private VisionSimulationMode _mode;
        private PictureBox _originalView, _resultView;
        private ComboBox _modeSelector;
        private Label _status, _ratioLabel;
        private Button _loadButton, _exportButton, _pickFirstButton, _pickSecondButton;
        private Panel _firstSwatch, _secondSwatch;
        private Color _firstColor = Color.Black, _secondColor = Color.White;
        private Color _contrastBackground = Color.White;
        private bool _pickingFirst;
        private readonly ToolTip _tips;
        private readonly Font _captionFont;
        private bool _disposed;

        public event EventHandler SimulationChanged;

        public ZenVisionAccessibility()
        {
            _tips = new ToolTip();
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            _captionFont = new Font(Font, FontStyle.Bold);
            MinimumSize = new Size(380, 300); Size = new Size(760, 500); TabStop = true;
            BuildUi(); UpdateContrast();
        }

        [Category("Appearance"), Description("Colour vision tranform with image.")]
        public VisionSimulationMode SimulationMode
        {
            get { return _mode; }
            set { if (!Enum.IsDefined(typeof(VisionSimulationMode), value)) throw new ArgumentOutOfRangeException("value"); SetMode(value); }
        }

        [Browsable(false)] public Bitmap SourceImage { get { return _source == null ? null : (Bitmap)_source.Clone(); } }
        [Browsable(false)] public Bitmap SimulatedImage { get { return _simulated == null ? null : (Bitmap)_simulated.Clone(); } }
        [Browsable(false)] public Color FirstContrastColor { get { return _firstColor; } set { _firstColor = value; UpdateContrast(); } }
        [Browsable(false)] public Color SecondContrastColor { get { return _secondColor; } set { _secondColor = value; UpdateContrast(); } }
        [Browsable(false)] public double ContrastRatio { get { return GetContrastRatio(_firstColor, _secondColor); } }
        [Category("Appearance"), Description("Opacity background.")]
        public Color ContrastBackground { get { return _contrastBackground; } set { _contrastBackground = Color.FromArgb(255, value.R, value.G, value.B); UpdateContrast(); } }
        [Category("Behavior"), Description("Raised after a different simulation mode is applied.")] public event EventHandler SimulationModeChanged { add { SimulationChanged += value; } remove { SimulationChanged -= value; } }

        public void LoadImage(Image image)
        {
            if (image == null) throw new ArgumentNullException("image");
            Bitmap copy = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(image, new Rectangle(0, 0, copy.Width, copy.Height));
            ReplaceSource(copy);
        }

        public void LoadImage(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Enter with image path.", "filePath");
            using (Image image = Image.FromFile(filePath)) LoadImage(image);
        }
        public Bitmap Simulate()
        {
            if (_source == null) throw new InvalidOperationException("Load one image before generate.");
            Bitmap result = Transform(_source, _mode);
            SetSimulation(result);
            return (Bitmap)result.Clone();
        }
        public double GetContrastRatio(Color first, Color second) { return CalculateContrastRatio(Composite(first, _contrastBackground), Composite(second, _contrastBackground)); }

        public bool ExportImage(string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Enter with destiny path.", "filePath");
            if (_simulated == null) return false;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            ImageFormat format;
            if (ext == ".jpg" || ext == ".jpeg") format = ImageFormat.Jpeg;
            else if (ext == ".bmp") format = ImageFormat.Bmp;
            else if (ext == ".gif") format = ImageFormat.Gif;
            else if (ext == ".png") format = ImageFormat.Png;
            else throw new ArgumentException("Use a extensions: .png, .jpg, .jpeg, .bmp ou .gif.", "filePath");
            _simulated.Save(filePath, format); return true;
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel(); root.Dock = DockStyle.Fill; root.Padding = new Padding(14); root.ColumnCount = 1; root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            Controls.Add(root);
            FlowLayoutPanel top = new FlowLayoutPanel(); top.Dock = DockStyle.Fill; top.WrapContents = false; top.BackColor = ZenTheme.Background; top.Padding = new Padding(0, 5, 0, 0); root.Controls.Add(top, 0, 0);
            _loadButton = MakeButton("Load image…"); _loadButton.Click += delegate { using (OpenFileDialog d = new OpenFileDialog()) { d.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All Files|*.*"; if (d.ShowDialog(this) == DialogResult.OK) try { LoadImage(d.FileName); } catch (Exception ex) { SetStatus("Wasn't possible open: " + ex.Message, true); } } }; top.Controls.Add(_loadButton);
            _modeSelector = new ComboBox(); _modeSelector.DropDownStyle = ComboBoxStyle.DropDownList; _modeSelector.Width = 160; _modeSelector.BackColor = ZenTheme.SurfaceRaised; _modeSelector.ForeColor = ZenTheme.Foreground; _modeSelector.FlatStyle = FlatStyle.Flat;
            _modeSelector.Items.AddRange(new object[] { "Original", "Protanopia", "Deuteranopia", "Tritanopia", "Grayscale" }); _modeSelector.SelectedIndex = 0; _modeSelector.SelectedIndexChanged += delegate { if (_modeSelector.SelectedIndex >= 0) SetMode((VisionSimulationMode)_modeSelector.SelectedIndex); }; top.Controls.Add(_modeSelector);
            _exportButton = MakeButton("Export simulation…"); _exportButton.Click += delegate { if (_simulated == null) { SetStatus("Carregue uma imagem para habilitar a exportação.", true); return; } using (SaveFileDialog d = new SaveFileDialog()) { d.Filter = "PNG|*.png|JPEG|*.jpg|Bitmap|*.bmp|GIF|*.gif"; d.DefaultExt = "png"; if (d.ShowDialog(this) == DialogResult.OK) try { ExportImage(d.FileName); SetStatus("Simulated image exported.", false); } catch (Exception ex) { SetStatus("Exportation error: " + ex.Message, true); } } }; top.Controls.Add(_exportButton);
            TableLayoutPanel views = new TableLayoutPanel(); views.Dock = DockStyle.Fill; views.ColumnCount = 2; views.RowCount = 2; views.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); views.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); views.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); views.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.Controls.Add(views, 0, 1);
            Label originalTitle = MakeCaption("ORIGINAL"); Label resultTitle = MakeCaption("SIMULATION"); views.Controls.Add(originalTitle, 0, 0); views.Controls.Add(resultTitle, 1, 0);
            _originalView = MakePicture(); _resultView = MakePicture(); views.Controls.Add(_originalView, 0, 1); views.Controls.Add(_resultView, 1, 1);
            _originalView.MouseClick += PickColor; _resultView.MouseClick += PickColor; _originalView.MouseEnter += delegate { Focus(); }; _resultView.MouseEnter += delegate { Focus(); };
            TableLayoutPanel footer = new TableLayoutPanel(); footer.Dock = DockStyle.Fill; footer.ColumnCount = 1; footer.RowCount = 2; footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 55)); footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); footer.BackColor = ZenTheme.Surface; footer.Padding = new Padding(10, 6, 10, 4); root.Controls.Add(footer, 0, 2);
            FlowLayoutPanel colors = new FlowLayoutPanel(); colors.Dock = DockStyle.Fill; colors.WrapContents = false; colors.BackColor = ZenTheme.Surface; footer.Controls.Add(colors, 0, 0);
            _pickFirstButton = MakeButton("Choose colour 1"); _pickSecondButton = MakeButton("Choose colour 2"); _firstSwatch = MakeSwatch(); _secondSwatch = MakeSwatch(); _ratioLabel = MakeCaption("Contrast: 21:1"); _ratioLabel.AutoSize = true;
            _pickFirstButton.Click += delegate { _pickingFirst = true; SetStatus("Clique em uma imagem para escolher a primeira cor.", false); Focus(); }; _pickSecondButton.Click += delegate { _pickingFirst = false; SetStatus("Click on image to select second colour.", false); Focus(); };
            colors.Controls.Add(_pickFirstButton); colors.Controls.Add(_firstSwatch); colors.Controls.Add(_pickSecondButton); colors.Controls.Add(_secondSwatch); colors.Controls.Add(_ratioLabel);
            _status = new Label(); _status.Dock = DockStyle.Fill; _status.ForeColor = ZenTheme.Muted; _status.TextAlign = ContentAlignment.MiddleLeft; footer.Controls.Add(_status, 0, 1);
            _tips.SetToolTip(_loadButton, "Load a local image to compare."); _tips.SetToolTip(_modeSelector, "Simule colour vision: protanopia, deuteranopia, tritanopia ou tons de cinza."); _tips.SetToolTip(_exportButton, "Save image on selected format."); _tips.SetToolTip(_originalView, "Click to capture colour on this image."); _tips.SetToolTip(_resultView, "Click to capture colour on this image");
        }

        private Button MakeButton(string text) { Button b = new Button(); b.Text = text; b.AutoSize = true; b.Height = 32; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = ZenTheme.Border; b.BackColor = ZenTheme.SurfaceRaised; b.ForeColor = ZenTheme.Foreground; b.Margin = new Padding(3, 2, 5, 2); b.TabStop = true; return b; }
        private Label MakeCaption(string text) { Label l = new Label(); l.Text = text; l.Dock = DockStyle.Fill; l.ForeColor = ZenTheme.Accent; l.Font = _captionFont; l.TextAlign = ContentAlignment.MiddleLeft; return l; }
        private PictureBox MakePicture() { PictureBox p = new PictureBox(); p.Dock = DockStyle.Fill; p.BackColor = ZenTheme.Surface; p.BorderStyle = BorderStyle.FixedSingle; p.SizeMode = PictureBoxSizeMode.Zoom; p.TabStop = true; p.AccessibleRole = AccessibleRole.Graphic; return p; }
        private Panel MakeSwatch() { Panel p = new Panel(); p.Size = new Size(28, 28); p.Margin = new Padding(1, 5, 8, 1); p.BorderStyle = BorderStyle.FixedSingle; return p; }
        private void ReplaceSource(Bitmap bitmap) { Bitmap transformed = null; try { transformed = Transform(bitmap, _mode); } catch { bitmap.Dispose(); throw; } if (_source != null) _source.Dispose(); _source = bitmap; SetSimulation(transformed); SetStatus("Image " + bitmap.Width.ToString(CultureInfo.InvariantCulture) + " × " + bitmap.Height.ToString(CultureInfo.InvariantCulture) + " loaded. Choose two colour to contrast.", false); }
        private void SetSimulation(Bitmap bitmap) { if (_simulated != null) _simulated.Dispose(); _simulated = bitmap; if (_originalView != null) { _originalView.Image = _source; _resultView.Image = _simulated; } }
        private void SetMode(VisionSimulationMode mode) { if (_mode == mode && (_source == null || _simulated != null)) return; Bitmap transformed = _source == null ? null : Transform(_source, mode); _mode = mode; if (_modeSelector != null && _modeSelector.SelectedIndex != (int)mode) _modeSelector.SelectedIndex = (int)mode; if (transformed != null) SetSimulation(transformed); SetStatus(_source == null ? "Selected mode: " + mode + ". Load one image to simule." : "Applyed simulation: " + mode + ".", false); EventHandler handler = SimulationChanged; if (handler != null) handler(this, EventArgs.Empty); }
        private static Bitmap Transform(Bitmap source, VisionSimulationMode mode)
        {
            Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb); if (mode == VisionSimulationMode.None) { using (Graphics g = Graphics.FromImage(result)) g.DrawImage(source, 0, 0, source.Width, source.Height); return result; }
            Rectangle rect = new Rectangle(0, 0, source.Width, source.Height); BitmapData input = null, output = null;
            try
            {
                input = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); output = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb); int inStride = input.Stride, outStride = output.Stride; byte[] src = new byte[Math.Abs(inStride) * source.Height], dst = new byte[Math.Abs(outStride) * source.Height]; Marshal.Copy(input.Scan0, src, 0, src.Length);
                for (int y = 0; y < source.Height; y++) for (int x = 0; x < source.Width; x++)
                    {
                        int si = y * inStride + x * 4, di = y * outStride + x * 4; double b = src[si], g = src[si + 1], r = src[si + 2], nr, ng, nb;
                        if (mode == VisionSimulationMode.Grayscale) { nr = ng = nb = .2126 * r + .7152 * g + .0722 * b; }
                        else if (mode == VisionSimulationMode.Protanopia) { nr = .567 * r + .433 * g; ng = .558 * r + .442 * g; nb = .242 * g + .758 * b; }
                        else if (mode == VisionSimulationMode.Deuteranopia) { nr = .625 * r + .375 * g; ng = .7 * r + .3 * g; nb = .3 * g + .7 * b; }
                        else { nr = .95 * r + .05 * g; ng = .433 * g + .567 * b; nb = .475 * g + .525 * b; }
                        dst[di] = Clamp(nb); dst[di + 1] = Clamp(ng); dst[di + 2] = Clamp(nr); dst[di + 3] = src[si + 3];
                    }
                Marshal.Copy(dst, 0, output.Scan0, dst.Length); return result;
            }
            catch { result.Dispose(); throw; }
            finally { if (input != null) source.UnlockBits(input); if (output != null) result.UnlockBits(output); }
        }
        private static byte Clamp(double v) { return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v))); }
        private static double Luminance(Color c) { double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0; r = r <= .04045 ? r / 12.92 : Math.Pow((r + .055) / 1.055, 2.4); g = g <= .04045 ? g / 12.92 : Math.Pow((g + .055) / 1.055, 2.4); b = b <= .04045 ? b / 12.92 : Math.Pow((b + .055) / 1.055, 2.4); return .2126 * r + .7152 * g + .0722 * b; }
        private static Color Composite(Color foreground, Color background) { int a = foreground.A; return Color.FromArgb(255, (foreground.R * a + background.R * (255 - a) + 127) / 255, (foreground.G * a + background.G * (255 - a) + 127) / 255, (foreground.B * a + background.B * (255 - a) + 127) / 255); }
        private static double CalculateContrastRatio(Color a, Color b) { double l1 = Luminance(a), l2 = Luminance(b); return (Math.Max(l1, l2) + .05) / (Math.Min(l1, l2) + .05); }
        private void UpdateContrast() { if (_firstSwatch != null) _firstSwatch.BackColor = _firstColor; if (_secondSwatch != null) _secondSwatch.BackColor = _secondColor; if (_ratioLabel == null) return; double ratio = ContrastRatio; string aa = ratio >= 7 ? "AAA" : ratio >= 4.5 ? "AA" : "Fail AA"; string large = ratio >= 4.5 ? "AAA large / AA" : ratio >= 3 ? "AA large" : "Fail large"; _ratioLabel.Text = " " + ratio.ToString("0.00", CultureInfo.InvariantCulture) + ":1  " + aa + " text · " + large; _ratioLabel.ForeColor = ratio >= 4.5 ? ZenTheme.Accent : ZenTheme.Danger; _tips.SetToolTip(_ratioLabel, "WCAG: normal text AA ≥ 4,5:1 and AAA ≥ 7:1; largest text AA ≥ 3:1 and AAA ≥ 4,5:1."); }
        private void PickColor(object sender, MouseEventArgs e) { if (_source == null || e.Button != MouseButtons.Left) return; PictureBox view = (PictureBox)sender; Bitmap image = view == _resultView ? _simulated : _source; if (image == null) return; int x, y; if (!TryMapPoint(view, e.Location, image.Size, out x, out y)) return; Color selected = image.GetPixel(x, y); if (_pickingFirst) _firstColor = selected; else _secondColor = selected; UpdateContrast(); SetStatus((_pickingFirst ? "Colour 1" : "Colour 2") + " selected: #" + selected.R.ToString("X2") + selected.G.ToString("X2") + selected.B.ToString("X2") + ".", false); }
        private static bool TryMapPoint(PictureBox box, Point point, Size image, out int x, out int y) { x = y = 0; float scale = Math.Min((float)box.ClientSize.Width / image.Width, (float)box.ClientSize.Height / image.Height); if (scale <= 0) return false; int w = (int)(image.Width * scale), h = (int)(image.Height * scale), ox = (box.ClientSize.Width - w) / 2, oy = (box.ClientSize.Height - h) / 2; if (point.X < ox || point.Y < oy || point.X >= ox + w || point.Y >= oy + h) return false; x = Math.Min(image.Width - 1, (int)((point.X - ox) / scale)); y = Math.Min(image.Height - 1, (int)((point.Y - oy) / scale)); return true; }
        private void SetStatus(string text, bool error) { if (_status == null) return; _status.Text = text; _status.ForeColor = error ? ZenTheme.Danger : ZenTheme.Muted; }
        protected override bool IsInputKey(Keys keyData) { if (keyData == Keys.Space || keyData == Keys.Enter) return true; return base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { if (_modeSelector != null && _modeSelector.Focused) { int next = (_modeSelector.SelectedIndex + 1) % _modeSelector.Items.Count; _modeSelector.SelectedIndex = next; } else if (_pickFirstButton != null && _pickFirstButton.Focused) _pickFirstButton.PerformClick(); else if (_pickSecondButton != null && _pickSecondButton.Focused) _pickSecondButton.PerformClick(); e.Handled = true; } }
        protected override void Dispose(bool disposing) { if (disposing && !_disposed) { _disposed = true; if (_originalView != null) _originalView.Image = null; if (_resultView != null) _resultView.Image = null; if (_source != null) { _source.Dispose(); _source = null; } if (_simulated != null) { _simulated.Dispose(); _simulated = null; } if (_tips != null) _tips.Dispose(); if (_captionFont != null) _captionFont.Dispose(); } base.Dispose(disposing); }
    }
}
// watashi wa watashi sore dake