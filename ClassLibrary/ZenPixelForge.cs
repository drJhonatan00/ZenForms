using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Pixel art studio!")]
    public class ZenPixelForge : UserControl
    {
        public enum EditorTool { Pencil, Eraser, Fill }

        private sealed class PixelCanvas : Control
        {
            private readonly ZenPixelForge _owner;
            public PixelCanvas(ZenPixelForge owner) { _owner = owner; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); TabStop = true; BackColor = Color.FromArgb(23, 19, 31); }
            protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); _owner.PaintCanvas(e.Graphics, ClientRectangle); }
            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); _owner.CanvasMouseDown(e); }
            protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); _owner.CanvasMouseMove(e); }
            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _owner.CanvasMouseUp(e); }
            protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (_owner.HandleEditorKey(e)) e.Handled = true; }
        }

        private readonly PixelCanvas _canvas;
        private readonly Panel _palettePanel;
        private readonly ComboBox _toolBox;
        private readonly NumericUpDown _widthInput, _heightInput, _pixelInput;
        private readonly CheckBox _gridToggle;
        private readonly ToolTip _tips;
        private readonly Font _titleFont;
        private readonly Stack<Bitmap> _undo = new Stack<Bitmap>(), _redo = new Stack<Bitmap>();
        private readonly List<Color> _palette = new List<Color>();
        private Bitmap _bitmap;
        private Color _currentColor = Color.FromArgb(239, 225, 255);
        private bool _drawing, _disposed, _updating;
        private Point _lastPixel;
        private int _pixelSize = 18;
        private Point _viewOffset = Point.Empty;
        private bool _panning;
        private Point _panStartMouse;
        private Point _panStartOffset;
        private bool _showGrid = true;
        private EditorTool _tool = EditorTool.Pencil;
        private const int HistoryLimit = 32;

        public event EventHandler CanvasChanged;
        public event EventHandler CurrentColorChanged;
        public event EventHandler ToolChanged;

        public ZenPixelForge()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            _tips = new ToolTip(); _tips.ShowAlways = true;
            _titleFont = new Font(Font, FontStyle.Bold);
            _palette.AddRange(new Color[] { Color.FromArgb(239, 225, 255), Color.FromArgb(177, 117, 255), Color.FromArgb(126, 75, 190), Color.FromArgb(67, 48, 91), Color.FromArgb(36, 29, 47), Color.FromArgb(255, 100, 146), Color.FromArgb(255, 174, 92), Color.FromArgb(255, 225, 112), Color.FromArgb(113, 224, 171), Color.FromArgb(92, 190, 255), Color.FromArgb(135, 135, 151), Color.FromArgb(255, 255, 255) });
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = ZenTheme.Background, Padding = new Padding(10) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(root);
            var side = new Panel { Dock = DockStyle.Fill, BackColor = ZenTheme.Surface, Padding = new Padding(12) }; root.Controls.Add(side, 0, 0);
            var title = new Label { Text = "PIXEL FORGE", Dock = DockStyle.Top, Height = 30, ForeColor = ZenTheme.Accent, Font = _titleFont, TextAlign = ContentAlignment.MiddleLeft }; side.Controls.Add(title);
            var toolsLabel = MakeLabel("Tools"); toolsLabel.Top = 39; side.Controls.Add(toolsLabel);
            _toolBox = new ComboBox { Left = 0, Top = 60, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground };
            _toolBox.Items.AddRange(new object[] { "Pen", "Eraser", "Filling" }); _toolBox.SelectedIndex = 0; _toolBox.SelectedIndexChanged += delegate { _tool = (EditorTool)_toolBox.SelectedIndex; if (ToolChanged != null) ToolChanged(this, EventArgs.Empty); }; side.Controls.Add(_toolBox); _tips.SetToolTip(_toolBox, "Choose pen, eraser or filling.");
            var dimLabel = MakeLabel("Grid (pixels)"); dimLabel.Top = 99; side.Controls.Add(dimLabel);
            _widthInput = MakeNumber(0, 120); _heightInput = MakeNumber(0, 149); _pixelInput = MakeNumber(0, 178); side.Controls.Add(_widthInput); side.Controls.Add(_heightInput); side.Controls.Add(_pixelInput);
            ConfigureNumber(_widthInput, "Width", 1, 512, 32); ConfigureNumber(_heightInput, "Heigth", 1, 512, 32); ConfigureNumber(_pixelInput, "Zoom (px/pixel)", 2, 64, 18);
            _widthInput.ValueChanged += DimensionChanged; _heightInput.ValueChanged += DimensionChanged; _pixelInput.ValueChanged += delegate { if (!_updating) { _pixelSize = (int)_pixelInput.Value; _canvas.Invalidate(); } };
            _gridToggle = new CheckBox { Text = "Show grids", Left = 0, Top = 210, Width = 180, Height = 27, ForeColor = ZenTheme.Foreground, Checked = true, FlatStyle = FlatStyle.Flat }; _gridToggle.CheckedChanged += delegate { _showGrid = _gridToggle.Checked; _canvas.Invalidate(); }; side.Controls.Add(_gridToggle);
            side.Controls.Add(MakeLabel("Palete · click to use")); side.Controls[side.Controls.Count - 1].Top = 246;
            _palettePanel = new Panel { Left = 0, Top = 270, Width = 184, Height = 116, BackColor = ZenTheme.Surface }; side.Controls.Add(_palettePanel); BuildPalette();
            var colorButton = MakeButton("＋ Add colour", 394); colorButton.Click += AddPaletteColor; side.Controls.Add(colorButton); _tips.SetToolTip(colorButton, "Add one colour to pallete.");
            var historyUndo = MakeButton("↶  Undo", 431); historyUndo.Click += delegate { Undo(); }; side.Controls.Add(historyUndo);
            var historyRedo = MakeButton("↷  Redo", 468); historyRedo.Click += delegate { Redo(); }; side.Controls.Add(historyRedo);
            var newButton = MakeButton("＋  New", 505); newButton.Click += delegate { NewCanvas((int)_widthInput.Value, (int)_heightInput.Value, Color.Transparent); }; side.Controls.Add(newButton);
            var loadButton = MakeButton("Open PNG…", 542); loadButton.Click += LoadPngDialog; side.Controls.Add(loadButton);
            var saveButton = MakeButton("Export PNG…", 579); saveButton.Click += SavePngDialog; side.Controls.Add(saveButton);
            var hint = new Label { Text = "Ctrl+Z undo  ·  Ctrl+Y redo\nScrool: zoom", Left = 0, Top = 621, Width = 184, Height = 38, ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleCenter }; side.Controls.Add(hint);
            _canvas = new PixelCanvas(this) { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.FromArgb(23, 19, 31) }; root.Controls.Add(_canvas, 1, 0); _canvas.MouseWheel += CanvasMouseWheel; _tips.SetToolTip(_canvas, "Click to draw. Ctrl+Z undo, Ctrl+Y redo.");
            NewCanvas(32, 32, Color.Transparent);
        }

        [Category("Pixel Forge"), Description("Width grid on pixels.")]
        public int WidthPixels { get { return _bitmap == null ? 0 : _bitmap.Width; } }
        [Category("Pixel Forge"), Description("Heigth grid on pixels.")]
        public int HeightPixels { get { return _bitmap == null ? 0 : _bitmap.Height; } }
        [Category("Pixel Forge"), Description("Size per pixel.")]
        public int PixelSize { get { return _pixelSize; } set { if (value < 2 || value > 64) throw new ArgumentOutOfRangeException("value", "PixelSize must be between 2 and 64."); _pixelSize = value; if (_pixelInput != null) _pixelInput.Value = value; if (_canvas != null) _canvas.Invalidate(); } }
        [Category("Pixel Forge"), Description("Selected colour.")]
        public Color CurrentColor { get { return _currentColor; } set { if (!_currentColor.Equals(value)) { _currentColor = value; if (CurrentColorChanged != null) CurrentColorChanged(this, EventArgs.Empty); } } }
        [Category("Pixel Forge"), Description("Current editing tool.")]
        public EditorTool ActiveTool { get { return _tool; } set { _tool = value; if (_toolBox != null) _toolBox.SelectedIndex = (int)value; if (ToolChanged != null) ToolChanged(this, EventArgs.Empty); } }
        [Category("Pixel Forge"), Description("Show the grid on pixels.")]
        public bool ShowGrid { get { return _showGrid; } set { _showGrid = value; if (_gridToggle != null) _gridToggle.Checked = value; if (_canvas != null) _canvas.Invalidate(); } }
        [Browsable(false)] public Bitmap Sprite { get { return _bitmap == null ? null : (Bitmap)_bitmap.Clone(); } }
        [Browsable(false)] public int UndoCount { get { return _undo.Count; } }
        [Browsable(false)] public int RedoCount { get { return _redo.Count; } }

        public void NewCanvas(int width, int height) { NewCanvas(width, height, Color.Transparent); }
        public void NewCanvas(int width, int height, Color background)
        {
            if (width < 1 || width > 512) throw new ArgumentOutOfRangeException("width", "Width must be between 1 and 512 pixels.");
            if (height < 1 || height > 512) throw new ArgumentOutOfRangeException("height", "Heigth must be between 1 and 512 pixels.");
            Bitmap next = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            if (background.A != 0) using (Graphics g = Graphics.FromImage(next)) g.Clear(background);
            ReplaceBitmap(next); ClearStack(_undo); ClearStack(_redo); SyncDimensions(); Changed();
        }
        public void LoadPng(string path)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentException("Enter the PNG path.", "path");
            Bitmap loaded;
            try { using (Image image = Image.FromFile(path)) { if (image.Width < 1 || image.Height < 1 || image.Width > 512 || image.Height > 512) throw new ArgumentException("PNG deve ter dimensões de 1 a 512 pixels.", "path"); loaded = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb); using (Graphics g = Graphics.FromImage(loaded)) { g.CompositingMode = CompositingMode.SourceCopy; g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height)); } } }
            catch (ArgumentException) { throw; }
            catch (Exception ex) { throw new InvalidOperationException("Wasn't possible load the PNG: " + ex.Message, ex); }
            ReplaceBitmap(loaded); ClearStack(_undo); ClearStack(_redo); SyncDimensions(); Changed();
        }
        public void SavePng(string path) { SavePng(path, false); }
        public void SavePng(string path, bool exportEnlarged)
        {
            if (_bitmap == null) throw new InvalidOperationException("It's empty.");
            if (String.IsNullOrEmpty(path)) throw new ArgumentException("Enter with PNG path.", "path");
            Bitmap output = null;
            try { if (exportEnlarged) { output = new Bitmap(_bitmap.Width * _pixelSize, _bitmap.Height * _pixelSize, PixelFormat.Format32bppArgb); using (Graphics g = Graphics.FromImage(output)) { g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.DrawImage(_bitmap, new Rectangle(0, 0, output.Width, output.Height)); } } else output = (Bitmap)_bitmap.Clone(); output.Save(path, ImageFormat.Png); }
            catch (Exception ex) { throw new InvalidOperationException("Wasn't possible save the PNG: " + ex.Message, ex); }
            finally { if (output != null) output.Dispose(); }
        }
        public bool Undo() { if (_undo.Count == 0) return false; PushHistory(_redo, _bitmap); ReplaceBitmap(_undo.Pop()); Changed(); return true; }
        public bool Redo() { if (_redo.Count == 0) return false; PushHistory(_undo, _bitmap); ReplaceBitmap(_redo.Pop()); Changed(); return true; }

        private static Label MakeLabel(string text) { return new Label { Text = text, Left = 0, Width = 184, Height = 20, ForeColor = ZenTheme.Muted }; }
        private static NumericUpDown MakeNumber(int x, int y) { return new NumericUpDown { Left = x, Top = y, Width = x == 0 ? 82 : 82, Height = 25, Minimum = 1, Maximum = 512, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle }; }
        private void ConfigureNumber(NumericUpDown n, string tip, int min, int max, int value) { n.Minimum = min; n.Maximum = max; n.Value = value; _tips.SetToolTip(n, tip); }
        private Button MakeButton(string text, int top) { return new Button { Text = text, Left = 0, Top = top, Width = 184, Height = 31, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Cursor = Cursors.Hand }; }
        private void SyncDimensions() { _updating = true; _widthInput.Value = _bitmap.Width; _heightInput.Value = _bitmap.Height; _updating = false; }
        private void DimensionChanged(object sender, EventArgs e) { if (!_updating && _bitmap != null && ((int)_widthInput.Value != _bitmap.Width || (int)_heightInput.Value != _bitmap.Height)) NewCanvas((int)_widthInput.Value, (int)_heightInput.Value, Color.Transparent); }
        private void ReplaceBitmap(Bitmap bitmap) { if (_bitmap != null) _bitmap.Dispose(); _bitmap = bitmap; if (_canvas != null) _canvas.Invalidate(); }
        private void ClearStack(Stack<Bitmap> stack) { while (stack.Count != 0) stack.Pop().Dispose(); }
        private void PushHistory(Stack<Bitmap> stack, Bitmap value) { if (value == null) return; stack.Push((Bitmap)value.Clone()); while (stack.Count > HistoryLimit) { Bitmap[] items = stack.ToArray(); stack.Clear(); for (int i = items.Length - 1; i >= 0; i--) { if (i == items.Length - 1) items[i].Dispose(); else stack.Push(items[i]); } } }
        private void BeginEdit() { PushHistory(_undo, _bitmap); ClearStack(_redo); }
        private void Changed() { if (_canvas != null) _canvas.Invalidate(); if (CanvasChanged != null) CanvasChanged(this, EventArgs.Empty); }

        private void BuildPalette()
        {
            if (_palettePanel == null) return;
            while (_palettePanel.Controls.Count > 0) { Control old = _palettePanel.Controls[0]; _palettePanel.Controls.RemoveAt(0); old.Dispose(); }
            for (int i = 0; i < _palette.Count; i++) { int index = i; Button b = new Button { Left = (i % 6) * 29, Top = (i / 6) * 36, Width = 25, Height = 25, BackColor = _palette[i], FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = index }; b.FlatAppearance.BorderColor = _palette[i].ToArgb() == _currentColor.ToArgb() ? ZenTheme.Accent : ZenTheme.Border; b.FlatAppearance.BorderSize = 2; b.Click += delegate { CurrentColor = _palette[index]; BuildPalette(); }; _palettePanel.Controls.Add(b); _tips.SetToolTip(b, "Select " + _palette[i].Name); }
        }
        private void AddPaletteColor(object sender, EventArgs e) { using (ColorDialog dialog = new ColorDialog()) { dialog.Color = _currentColor; dialog.FullOpen = true; if (dialog.ShowDialog(this) == DialogResult.OK) { _palette.Add(dialog.Color); CurrentColor = dialog.Color; BuildPalette(); } } }
        private void LoadPngDialog(object sender, EventArgs e) { using (OpenFileDialog d = new OpenFileDialog { Filter = "PNG image (*.png)|*.png", Title = "Open PNG sprite" }) if (d.ShowDialog(this) == DialogResult.OK) try { LoadPng(d.FileName); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Failed to open PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void SavePngDialog(object sender, EventArgs e) { using (SaveFileDialog d = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", DefaultExt = "png", AddExtension = true, Title = "Export PNG sprite" }) if (d.ShowDialog(this) == DialogResult.OK) try { SavePng(d.FileName, false); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Failed to export PNG", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void CanvasMouseWheel(object sender, MouseEventArgs e) { PixelSize = Math.Max(2, Math.Min(64, _pixelSize + (e.Delta > 0 ? 2 : -2))); _pixelInput.Value = _pixelSize; }
        private Point PixelAt(Point p) 
        { 
            int x = (_canvas.ClientSize.Width - _bitmap.Width * _pixelSize) / 2 + _viewOffset.X;
            int y = (_canvas.ClientSize.Height - _bitmap.Height * _pixelSize) / 2 + _viewOffset.Y;

            int width = _bitmap.Width * _pixelSize;
            int height = _bitmap.Height * _pixelSize;

            if (p.X < x || p.Y < y || p.X >= x + width || p.Y >= y + height) return new Point(-1, -1);

            return new Point((p.X - x) / _pixelSize, (p.Y - y) / _pixelSize);

        }
        private bool ValidPixel(Point p) { return p.X >= 0 && p.Y >= 0 && p.X < _bitmap.Width && p.Y < _bitmap.Height; }
        private void CanvasMouseDown(MouseEventArgs e)
        {
            if (_bitmap == null)
                return;
            if (e.Button == MouseButtons.Right)
            {
                _drawing = false;
                _panning = true;
                _panStartMouse = e.Location;
                _panStartOffset = _viewOffset;
                _canvas.Capture = true;
                _canvas.Cursor = Cursors.SizeAll;
                return;
            }
            if (e.Button != MouseButtons.Left)
                return;

            Point p = PixelAt(e.Location);
            if (!ValidPixel(p))
                return;

            BeginEdit();

            if (_tool == EditorTool.Fill)
            {
                FloodFill(p.X, p.Y, _currentColor);
                Changed();
            }
            else
            {
                _drawing = true;
                _lastPixel = new Point(-1, -1);
                PaintPixel(p);
            }
        }

        private void CanvasMouseMove(MouseEventArgs e)
        {
            if (_panning && (e.Button & MouseButtons.Right) == MouseButtons.Right)
            {
                _viewOffset = new Point(
                    _panStartOffset.X + e.X - _panStartMouse.X,
                    _panStartOffset.Y + e.Y - _panStartMouse.Y);

                _canvas.Invalidate();
                return;
            }

            if (_drawing && (e.Button & MouseButtons.Left) == MouseButtons.Left)
                PaintPixel(PixelAt(e.Location));
        }

        private void CanvasMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _panning)
            {
                _panning = false;
                _canvas.Capture = false;
                _canvas.Cursor = Cursors.Default;
                return;
            }

            if (_drawing && e.Button == MouseButtons.Left)
            {
                _drawing = false;
                Changed();
            }
        }
        private void PaintPixel(Point p) { if (!ValidPixel(p) || p == _lastPixel) return; _bitmap.SetPixel(p.X, p.Y, _tool == EditorTool.Eraser ? Color.Transparent : _currentColor); _lastPixel = p; _canvas.Invalidate(); }
        private void FloodFill(int x, int y, Color replacement)
        {
            Color target = _bitmap.GetPixel(x, y); if (target.ToArgb() == replacement.ToArgb()) return; int w = _bitmap.Width, h = _bitmap.Height; bool[,] seen = new bool[w, h]; Queue<Point> q = new Queue<Point>(); q.Enqueue(new Point(x, y)); seen[x, y] = true;
            while (q.Count > 0) { Point p = q.Dequeue(); if (_bitmap.GetPixel(p.X, p.Y).ToArgb() != target.ToArgb()) continue; _bitmap.SetPixel(p.X, p.Y, replacement); if (p.X > 0 && !seen[p.X - 1, p.Y]) { seen[p.X - 1, p.Y] = true; q.Enqueue(new Point(p.X - 1, p.Y)); } if (p.X + 1 < w && !seen[p.X + 1, p.Y]) { seen[p.X + 1, p.Y] = true; q.Enqueue(new Point(p.X + 1, p.Y)); } if (p.Y > 0 && !seen[p.X, p.Y - 1]) { seen[p.X, p.Y - 1] = true; q.Enqueue(new Point(p.X, p.Y - 1)); } if (p.Y + 1 < h && !seen[p.X, p.Y + 1]) { seen[p.X, p.Y + 1] = true; q.Enqueue(new Point(p.X, p.Y + 1)); } }
        }
        private void PaintCanvas(Graphics g, Rectangle bounds)
        {
            g.Clear(Color.FromArgb(23, 19, 31)); if (_bitmap == null) return; int w = _bitmap.Width * _pixelSize, h = _bitmap.Height * _pixelSize; int ox = (bounds.Width - w) / 2 + _viewOffset.X; int oy = (bounds.Height - h) / 2 + _viewOffset.Y; Rectangle area = new Rectangle(ox, oy, w, h);
            using (SolidBrush dark = new SolidBrush(Color.FromArgb(52, 44, 64))) using (SolidBrush light = new SolidBrush(Color.FromArgb(66, 57, 79))) { int cell = Math.Max(8, _pixelSize / 2); for (int y = 0; y < h; y += cell) for (int x = 0; x < w; x += cell) g.FillRectangle(((x / cell + y / cell) & 1) == 0 ? dark : light, ox + x, oy + y, Math.Min(cell, w - x), Math.Min(cell, h - y)); }
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.DrawImage(_bitmap, area);
            if (_showGrid && _pixelSize >= 5) { using (Pen pen = new Pen(Color.FromArgb(75, ZenTheme.Border))) { for (int x = 0; x <= _bitmap.Width; x++) g.DrawLine(pen, ox + x * _pixelSize, oy, ox + x * _pixelSize, oy + h); for (int y = 0; y <= _bitmap.Height; y++) g.DrawLine(pen, ox, oy + y * _pixelSize, ox + w, oy + y * _pixelSize); } }
            using (Pen outline = new Pen(ZenTheme.Primary, 2)) g.DrawRectangle(outline, ox - 1, oy - 1, w + 1, h + 1);
        }
        private bool HandleEditorKey(KeyEventArgs e) { if (e.Control && e.KeyCode == Keys.Z) return Undo(); if (e.Control && e.KeyCode == Keys.Y) return Redo(); if (e.KeyCode == Keys.P) { ActiveTool = EditorTool.Pencil; return true; } if (e.KeyCode == Keys.E) { ActiveTool = EditorTool.Eraser; return true; } if (e.KeyCode == Keys.F) { ActiveTool = EditorTool.Fill; return true; } return false; }
        protected override void Dispose(bool disposing) { if (disposing && !_disposed) { _disposed = true; if (_bitmap != null) { _bitmap.Dispose(); _bitmap = null; } ClearStack(_undo); ClearStack(_redo); if (_tips != null) _tips.Dispose(); if (_titleFont != null) _titleFont.Dispose(); } base.Dispose(disposing); }
    }
}

// watashi wa watashi sore dake