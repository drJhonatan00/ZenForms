using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Bitmap font editor: draw glyphs on grid, view the text and export how PNG or C# data.")]
    public class ZenBitmapFontForge : UserControl
    {
        private sealed class FontProject
        {
            public int Version { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int FirstCode { get; set; }
            public int LastCode { get; set; }
            public string[] Glyphs { get; set; }
        }

        private sealed class Snapshot
        {
            public bool[,] Pixels;
            public Snapshot(bool[,] pixels) { Pixels = (bool[,])pixels.Clone(); }
        }

        private sealed class PixelGrid : Control
        {
            private readonly ZenBitmapFontForge owner;
            public PixelGrid(ZenBitmapFontForge owner)
            {
                this.owner = owner; DoubleBuffered = true; TabStop = true; BackColor = ZenTheme.Surface;
                Cursor = Cursors.Cross; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); owner.PaintGrid(e.Graphics, ClientRectangle);
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e); Focus(); owner.PaintAt(e.Location, e.Button); Capture = true;
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e); if (Capture && e.Button != MouseButtons.None) owner.PaintAt(e.Location, e.Button);
            }
            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; }
            protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down || base.IsInputKey(keyData); }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.Control && e.KeyCode == Keys.Z) { owner.Undo(); e.Handled = true; }
                else if (e.Control && e.KeyCode == Keys.Y) { owner.Redo(); e.Handled = true; }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                { owner.MoveCell(e.KeyCode); e.Handled = true; }
                else if (!e.Control && !e.Alt && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                { owner.SelectCharacter((char)('A' + (e.KeyCode - Keys.A))); e.Handled = true; }
            }
        }

        private readonly PixelGrid grid;
        private readonly ComboBox glyphPicker;
        private readonly TextBox previewText;
        private readonly Label status;
        private readonly Stack<Snapshot> undoStack = new Stack<Snapshot>();
        private readonly Stack<Snapshot> redoStack = new Stack<Snapshot>();
        private readonly Dictionary<int, bool[,]> glyphs = new Dictionary<int, bool[,]>();
        private bool[,] pixels;
        private int glyphWidth = 5, glyphHeight = 7, characterCode = 65, zoom = 24;
        private int brushX, brushY, lineStartX = -1, lineStartY = -1;
        private Color foreground = Color.FromArgb(239, 242, 255), background = Color.FromArgb(25, 31, 51);
        private bool mirrorX, mirrorY, eraser, lineMode, suppressPicker;
        private int historyLimit = 50;
        public ZenBitmapFontForge()
        {
            pixels = new bool[glyphHeight, glyphWidth];
            Size = new Size(760, 500); MinimumSize = new Size(430, 320); BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = ZenTheme.Background, Padding = new Padding(10) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = ZenTheme.Background, Margin = new Padding(0, 0, 0, 8) };
            AddButton(bar, "New", NewProject); AddButton(bar, "Open", OpenProject); AddButton(bar, "Save", SaveProject);
            AddButton(bar, "Undo", delegate { Undo(); }); AddButton(bar, "Redo", delegate { Redo(); }); AddButton(bar, "Line", delegate { lineMode = !lineMode; UpdateStatus(); });
            AddButton(bar, "Mirror X", delegate { MirrorX = !MirrorX; }); AddButton(bar, "Mirror Y", delegate { MirrorY = !MirrorY; }); AddButton(bar, "Eraser", delegate { eraser = !eraser; UpdateStatus(); });
            AddButton(bar, "−", delegate { Zoom = zoom - 4; }); AddButton(bar, "+", delegate { Zoom = zoom + 4; });
            glyphPicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, FlatStyle = FlatStyle.Flat, Margin = new Padding(5) };
            for (int i = 32; i <= 126; i++) glyphPicker.Items.Add(((char)i) + "  U+" + i.ToString("X4"));
            glyphPicker.SelectedIndexChanged += delegate { if (!suppressPicker && glyphPicker.SelectedIndex >= 0) SelectCharacter((char)(glyphPicker.SelectedIndex + 32)); };
            bar.Controls.Add(glyphPicker);
            AddButton(bar, "Atlas PNG", ExportAtlas); AddButton(bar, "C# code", ExportCSharp);
            grid = new PixelGrid(this) { Dock = DockStyle.Fill, Margin = new Padding(0), MinimumSize = new Size(100, 100) };
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = ZenTheme.Background, Padding = new Padding(0, 8, 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            previewText = new TextBox { Text = "The quick brown fox 0123", Dock = DockStyle.Fill, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 3, 8, 3) };
            previewText.TextChanged += delegate { grid.Invalidate(); };
            status = new Label { AutoSize = true, ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill };
            footer.Controls.Add(new Label { Text = "Text preview", AutoSize = true, ForeColor = ZenTheme.Muted, Anchor = AnchorStyles.Left }, 0, 0);
            footer.Controls.Add(status, 1, 0); footer.Controls.Add(previewText, 0, 1); footer.SetColumnSpan(previewText, 2);
            root.Controls.Add(bar, 0, 0); root.Controls.Add(grid, 0, 1); root.Controls.Add(footer, 0, 2); Controls.Add(root);
            glyphPicker.SelectedIndex = 65 - 32; UpdateStatus();
        }

        [Category("Behavior"), Description("The editor content was changed.")]
        public event EventHandler ContentChanged;
        [Category("Font"), Description("Glyph width on pixels (1–32).")]
        public int GlyphWidth { get { return glyphWidth; } set { SetDimensions(value, glyphHeight); } }
        [Category("Font"), Description("Glyph height on pixels (1–32).")]
        public int GlyphHeight { get { return glyphHeight; } set { SetDimensions(glyphWidth, value); } }
        [Category("Font"), Description("Selected characters on editor.")]
        public char SelectedCharacter { get { return (char)characterCode; } set { SelectCharacter(value); } }
        [Category("Appearance"), Description("Size of every pixel (8–64).")]
        public int Zoom { get { return zoom; } set { zoom = Math.Max(8, Math.Min(64, value)); if (grid != null) grid.Invalidate(); } }
        [Category("Appearance"), Description("Active pixels foreground colours.")]
        public Color ForegroundColor { get { return foreground; } set { foreground = value; if (grid != null) grid.Invalidate(); } }
        [Category("Appearance"), Description("Grid background colour.")]
        public Color BackgroundColor { get { return background; } set { background = value; if (grid != null) grid.Invalidate(); } }
        [Category("Behavior"), Description("Mirror every trait on glyph vertical axis.")]
        public bool MirrorX { get { return mirrorX; } set { mirrorX = value; UpdateStatus(); } }
        [Category("Behavior"), Description("Mirror every trait on glyph horizontal axis.")]
        public bool MirrorY { get { return mirrorY; } set { mirrorY = value; UpdateStatus(); } }
        [Category("Behavior"), Description("Undo state limit (1–500).")]
        public int HistoryLimit { get { return historyLimit; } set { historyLimit = Math.Max(1, Math.Min(500, value)); TrimHistory(); } }
        public void SelectCharacter(char character)
        {
            if (character < 32 || character > 126) throw new ArgumentOutOfRangeException("character", "Use printables ASCII characters (32–126).");
            SaveCurrentGlyph(); characterCode = character; bool[,] stored;
            pixels = glyphs.TryGetValue(characterCode, out stored) ? (bool[,])stored.Clone() : new bool[glyphHeight, glyphWidth];
            suppressPicker = true; glyphPicker.SelectedIndex = characterCode - 32; suppressPicker = false;
            grid.Invalidate(); UpdateStatus();
        }
        public void SetPixel(int x, int y, bool value)
        {
            if (x < 0 || x >= glyphWidth || y < 0 || y >= glyphHeight) throw new ArgumentOutOfRangeException("x/y");
            if (pixels[y, x] == value) return; PushUndo(); pixels[y, x] = value; SaveCurrentGlyph(); grid.Invalidate(); RaiseChanged();
        }
        public bool GetPixel(int x, int y)
        {
            if (x < 0 || x >= glyphWidth || y < 0 || y >= glyphHeight) throw new ArgumentOutOfRangeException("x/y"); return pixels[y, x];
        }
        public void Undo()
        {
            if (undoStack.Count == 0) return; redoStack.Push(new Snapshot(pixels)); pixels = undoStack.Pop().Pixels; SaveCurrentGlyph(); grid.Invalidate(); RaiseChanged(); UpdateStatus();
        }
        public void Redo()
        {
            if (redoStack.Count == 0) return; undoStack.Push(new Snapshot(pixels)); pixels = redoStack.Pop().Pixels; SaveCurrentGlyph(); grid.Invalidate(); RaiseChanged(); UpdateStatus();
        }
        public bool OpenProject(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Obrigatory path.", "path");
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Project larger than 4 MiB.");
            var serializer = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 };
            FontProject p = serializer.Deserialize<FontProject>(File.ReadAllText(path, Encoding.UTF8));
            if (p == null || p.Version != 1 || p.Width < 1 || p.Width > 32 || p.Height < 1 || p.Height > 32 || p.FirstCode != 32 || p.LastCode != 126 || p.Glyphs == null || p.Glyphs.Length != 95)
                throw new InvalidDataException("Project format or dimensions are invalids.");
            var loaded = new Dictionary<int, bool[,]>();
            for (int n = 0; n < p.Glyphs.Length; n++)
            {
                string bits = p.Glyphs[n]; if (bits == null || bits.Length != p.Width * p.Height) throw new InvalidDataException("Invalid glyph data.");
                var map = new bool[p.Height, p.Width]; for (int k = 0; k < bits.Length; k++) { if (bits[k] != '0' && bits[k] != '1') throw new InvalidDataException("Invalid glyph bit."); map[k / p.Width, k % p.Width] = bits[k] == '1'; }
                loaded[n + 32] = map;
            }
            glyphWidth = p.Width; glyphHeight = p.Height; glyphs.Clear(); foreach (var pair in loaded) glyphs.Add(pair.Key, pair.Value);
            undoStack.Clear(); redoStack.Clear(); pixels = new bool[glyphHeight, glyphWidth]; SelectCharacter((char)characterCode); RaiseChanged(); return true;
        }
        public void SaveProject(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Obrigatory path.", "path"); SaveCurrentGlyph();
            var p = new FontProject { Version = 1, Width = glyphWidth, Height = glyphHeight, FirstCode = 32, LastCode = 126, Glyphs = new string[95] };
            for (int i = 0; i < 95; i++) { bool[,] map; glyphs.TryGetValue(i + 32, out map); p.Glyphs[i] = Encode(map ?? new bool[glyphHeight, glyphWidth]); }
            File.WriteAllText(path, new JavaScriptSerializer().Serialize(p), new UTF8Encoding(false));
        }
        public void ExportAtlas(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Obrigatory path.", "path"); SaveCurrentGlyph();
            using (var bmp = new Bitmap(glyphWidth * 16, glyphHeight * 6, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            using (var bg = new SolidBrush(background)) using (var fg = new SolidBrush(foreground))
            {
                g.Clear(background); g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
                for (int i = 0; i < 95; i++)
                {
                    bool[,] map; if (!glyphs.TryGetValue(i + 32, out map)) continue; int ox = (i % 16) * glyphWidth, oy = (i / 16) * glyphHeight;
                    for (int y = 0; y < glyphHeight; y++) for (int x = 0; x < glyphWidth; x++) if (map[y, x]) g.FillRectangle(fg, ox + x, oy + y, 1, 1);
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }
        public void ExportCSharp(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Obrigatory path.", "path"); SaveCurrentGlyph();
            var sb = new StringBuilder(); sb.AppendLine("// Generated by ZenBitmapFontForge"); sb.AppendLine("// Glyphs: ASCII 32..126; rows are packed MSB-first.");
            sb.AppendLine("public static readonly byte[,] GlyphData = new byte[95," + (glyphHeight * ((glyphWidth + 7) / 8)) + "] {");
            for (int i = 0; i < 95; i++)
            {
                bool[,] map; if (!glyphs.TryGetValue(i + 32, out map)) map = new bool[glyphHeight, glyphWidth]; sb.Append("    {"); bool first = true;
                for (int y = 0; y < glyphHeight; y++) for (int block = 0; block < (glyphWidth + 7) / 8; block++)
                    { int v = 0; for (int b = 0; b < 8; b++) { int x = block * 8 + b; if (x < glyphWidth && map[y, x]) v |= 1 << (7 - b); } if (!first) sb.Append(", "); sb.Append("0x").Append(v.ToString("X2")); first = false; }
                sb.AppendLine("},");
            }
            sb.AppendLine("};"); File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }
        private void AddButton(FlowLayoutPanel panel, string text, Action action)
        {
            var b = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Margin = new Padding(3), Padding = new Padding(5, 2, 5, 2) };
            b.FlatAppearance.BorderColor = ZenTheme.Border; b.Click += delegate { action(); }; panel.Controls.Add(b);
        }
        private void NewProject()
        {
            using (var dialog = new Form { Text = "New bitmap font", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(250, 120), BackColor = ZenTheme.Background, ForeColor = ZenTheme.Foreground, Font = Font })
            {
                var w = new NumericUpDown { Minimum = 1, Maximum = 32, Value = glyphWidth, Left = 15, Top = 18, Width = 80 };
                var h = new NumericUpDown { Minimum = 1, Maximum = 32, Value = glyphHeight, Left = 110, Top = 18, Width = 80 };
                var ok = new Button { Text = "Create", DialogResult = DialogResult.OK, Left = 15, Top = 65, Width = 80 }; dialog.Controls.AddRange(new Control[] { w, h, ok }); dialog.AcceptButton = ok;
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return; SetDimensions((int)w.Value, (int)h.Value); glyphs.Clear(); undoStack.Clear(); redoStack.Clear(); SelectCharacter('A'); RaiseChanged();
            }
        }
        private void OpenProject() { using (var d = new OpenFileDialog { Filter = "Zen bitmap font (*.json)|*.json|All files (*.*)|*.*" }) if (d.ShowDialog(FindForm()) == DialogResult.OK) RunFileAction(delegate { OpenProject(d.FileName); }); }
        private void SaveProject() { using (var d = new SaveFileDialog { Filter = "Zen bitmap font (*.json)|*.json", DefaultExt = "json" }) if (d.ShowDialog(FindForm()) == DialogResult.OK) RunFileAction(delegate { SaveProject(d.FileName); }); }
        private void ExportAtlas() { using (var d = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", DefaultExt = "png" }) if (d.ShowDialog(FindForm()) == DialogResult.OK) RunFileAction(delegate { ExportAtlas(d.FileName); }); }
        private void ExportCSharp() { using (var d = new SaveFileDialog { Filter = "C# source (*.cs)|*.cs", DefaultExt = "cs" }) if (d.ShowDialog(FindForm()) == DialogResult.OK) RunFileAction(delegate { ExportCSharp(d.FileName); }); }
        private void RunFileAction(Action action) { try { action(); } catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "ZenBitmapFontForge", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        private void SetDimensions(int w, int h)
        {
            if (w < 1 || w > 32 || h < 1 || h > 32) throw new ArgumentOutOfRangeException("Accepted dimensions: 1–32 pixels.");
            if (w == glyphWidth && h == glyphHeight) return; SaveCurrentGlyph(); glyphWidth = w; glyphHeight = h;
            foreach (int key in new List<int>(glyphs.Keys)) { bool[,] old = glyphs[key], resized = new bool[h, w]; for (int y = 0; y < Math.Min(h, old.GetLength(0)); y++) for (int x = 0; x < Math.Min(w, old.GetLength(1)); x++) resized[y, x] = old[y, x]; glyphs[key] = resized; }
            pixels = new bool[h, w]; bool[,] current; if (glyphs.TryGetValue(characterCode, out current)) pixels = (bool[,])current.Clone(); undoStack.Clear(); redoStack.Clear(); if (grid != null) grid.Invalidate(); RaiseChanged();
        }
        private void SaveCurrentGlyph() { if (pixels != null) glyphs[characterCode] = (bool[,])pixels.Clone(); }
        private string Encode(bool[,] map) { var s = new StringBuilder(glyphWidth * glyphHeight); for (int y = 0; y < glyphHeight; y++) for (int x = 0; x < glyphWidth; x++) s.Append(map[y, x] ? '1' : '0'); return s.ToString(); }
        private void PushUndo() { undoStack.Push(new Snapshot(pixels)); TrimHistory(); redoStack.Clear(); }
        private void TrimHistory() { if (undoStack.Count <= historyLimit) return; var a = undoStack.ToArray(); undoStack.Clear(); for (int i = historyLimit - 1; i >= 0; i--) undoStack.Push(a[i]); }
        private void RaiseChanged() { EventHandler h = ContentChanged; if (h != null) h(this, EventArgs.Empty); }
        private void UpdateStatus() { if (status != null) status.Text = "U+" + characterCode.ToString("X4") + " · " + glyphWidth + "×" + glyphHeight + (lineMode ? " · Line" : " · Brush") + (eraser ? " · Eraser" : "") + (mirrorX ? " · ↔" : "") + (mirrorY ? " · ↕" : ""); }
        private void MoveCell(Keys key) { if (key == Keys.Left) brushX--; if (key == Keys.Right) brushX++; if (key == Keys.Up) brushY--; if (key == Keys.Down) brushY++; brushX = Math.Max(0, Math.Min(glyphWidth - 1, brushX)); brushY = Math.Max(0, Math.Min(glyphHeight - 1, brushY)); grid.Invalidate(); }
        private void PaintAt(Point p, MouseButtons button)
        {
            int cell = Math.Max(1, Math.Min(zoom, Math.Min((grid.ClientSize.Width - 24) / glyphWidth, (grid.ClientSize.Height - 50) / (glyphHeight + 2))));
            int gw = cell * glyphWidth, gh = cell * glyphHeight, left = (grid.Width - gw) / 2, top = Math.Max(18, (grid.Height - gh - 34) / 2);
            int x = (p.X - left) / cell, y = (p.Y - top) / cell; if (x < 0 || x >= glyphWidth || y < 0 || y >= glyphHeight) return;
            bool value = button == MouseButtons.Right ? false : !eraser;
            if (lineMode && lineStartX >= 0) { PushUndo(); DrawLine(lineStartX, lineStartY, x, y, value); lineStartX = -1; lineStartY = -1; SaveCurrentGlyph(); grid.Invalidate(); RaiseChanged(); return; }
            if (lineMode) { lineStartX = x; lineStartY = y; }
            brushX = x; brushY = y; PushUndo(); PutPixel(x, y, value); SaveCurrentGlyph(); grid.Invalidate(); RaiseChanged();
        }
        private void PutPixel(int x, int y, bool value)
        {
            pixels[y, x] = value; if (mirrorX) pixels[y, glyphWidth - 1 - x] = value; if (mirrorY) pixels[glyphHeight - 1 - y, x] = value;
            if (mirrorX && mirrorY) pixels[glyphHeight - 1 - y, glyphWidth - 1 - x] = value;
        }
        private void DrawLine(int x0, int y0, int x1, int y1, bool value)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true) { PutPixel(x0, y0, value); if (x0 == x1 && y0 == y1) break; int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; } }
        }
        private void PaintGrid(Graphics g, Rectangle bounds)
        {
            g.Clear(BackColor); int cell = Math.Max(1, Math.Min(zoom, Math.Min((bounds.Width - 24) / glyphWidth, (bounds.Height - 50) / (glyphHeight + 2))));
            int left = (bounds.Width - glyphWidth * cell) / 2, top = Math.Max(18, (bounds.Height - glyphHeight * cell - 34) / 2);
            using (var bg = new SolidBrush(background)) using (var fg = new SolidBrush(foreground)) using (var pen = new Pen(Color.FromArgb(70, ZenTheme.Muted))) using (var accent = new Pen(ZenTheme.Primary, 2)) using (var font = new Font("Segoe UI", 9f)) using (var muted = new SolidBrush(ZenTheme.Muted))
            {
                for (int y = 0; y < glyphHeight; y++) for (int x = 0; x < glyphWidth; x++) { Rectangle r = new Rectangle(left + x * cell, top + y * cell, cell, cell); g.FillRectangle(pixels[y, x] ? fg : bg, r); g.DrawRectangle(pen, r); }
                g.DrawString("'" + (char)characterCode + "'   U+" + characterCode.ToString("X4"), font, muted, left, Math.Max(0, top - 18));
                if (brushX >= 0 && brushY >= 0 && brushX < glyphWidth && brushY < glyphHeight) g.DrawRectangle(accent, left + brushX * cell + 1, top + brushY * cell + 1, Math.Max(1, cell - 2), Math.Max(1, cell - 2));
                string text = previewText == null ? "" : previewText.Text; int px = 10, py = bounds.Height - 25, stride = Math.Max(1, cell / 3);
                foreach (char ch in text) { int code = ch >= 32 && ch <= 126 ? ch : 63; bool[,] map; glyphs.TryGetValue(code, out map); if (map != null) for (int yy = 0; yy < glyphHeight; yy++) for (int xx = 0; xx < glyphWidth; xx++) if (map[yy, xx]) g.FillRectangle(fg, px + xx * stride, py + yy * stride, stride, stride); px += (glyphWidth + 1) * stride; if (px > bounds.Width - glyphWidth * stride - 5) break; }
            }
        }
    }
}
// Watashi wa watashi sore dake