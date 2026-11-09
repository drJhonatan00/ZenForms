using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class VoronoiSite
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        internal VoronoiSite(double x, double y) { X = x; Y = y; }
    }

    public sealed class VoronoiSiteSelectedEventArgs : EventArgs
    {
        public int Index { get; private set; }
        public VoronoiSite Site { get; private set; }
        internal VoronoiSiteSelectedEventArgs(int index, double x, double y) { Index = index; Site = new VoronoiSite(x, y); }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Interactive editor.")]
    public class ZenVoronoiForge : UserControl
    {
        private struct PointD { public double X, Y; public PointD(double x, double y) { X = x; Y = y; } }
        private sealed class Cell { public PointD[] Points; public Cell(PointD[] p) { Points = p; } }
        private readonly List<PointD> _sites = new List<PointD>();
        private readonly List<Cell> _cells = new List<Cell>();
        private readonly Color[] _palette = new Color[] { Color.FromArgb(56, 255, 208), Color.FromArgb(255, 79, 216), Color.FromArgb(125, 112, 255), Color.FromArgb(255, 203, 79), Color.FromArgb(67, 195, 255), Color.FromArgb(255, 105, 130), Color.FromArgb(175, 255, 93), Color.FromArgb(194, 112, 255) };
        private int _maxSites = 250, _selected = -1, _hover = -1;
        private bool _showLabels = true, _dragging, _panning;
        private double _zoom = 1.0, _offsetX, _offsetY;
        private Point _lastMouse;
        private bool _dirty = true;
        private const double Epsilon = 0.000001;
        public event EventHandler<VoronoiSiteSelectedEventArgs> SiteSelected;

        public ZenVoronoiForge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont; TabStop = true; MinimumSize = new Size(120, 100); Size = new Size(500, 360); Cursor = Cursors.Cross;
        }
        [Category("Behavior"), Description("Maximum number of sites, from 1 to 1000.")]
        public int MaxSites { get { return _maxSites; } set { _maxSites = Math.Max(1, Math.Min(1000, value)); if (_sites.Count > _maxSites) _sites.RemoveRange(_maxSites, _sites.Count - _maxSites); Rebuild(); } }
        [Category("Behavior"), Description("Number of Lloyd relaxation iterations.")]
        public int LloydIterations { get; set; }
        [Category("Appearance"), Description("Show site index labels.")]
        public bool ShowLabels { get { return _showLabels; } set { _showLabels = value; Invalidate(); } }
        [Browsable(false)] public int SelectedIndex { get { return _selected; } }
        [Browsable(false)]
        public IList<VoronoiSite> Sites
        {
            get { List<VoronoiSite> copy = new List<VoronoiSite>(_sites.Count); for (int i = 0; i < _sites.Count; i++) copy.Add(new VoronoiSite(_sites[i].X, _sites[i].Y)); return copy.AsReadOnly(); }
        }
        public int AddSite(double x, double y)
        {
            EnsureUiThread(); if (_sites.Count >= _maxSites || !IsFinite(x) || !IsFinite(y)) return -1;
            for (int i = 0; i < _sites.Count; i++) if (DistanceSquared(_sites[i], new PointD(x, y)) < Epsilon * Epsilon) return -1;
            _sites.Add(new PointD(x, y)); Rebuild(); return _sites.Count - 1;
        }
        public void ClearSites() { EnsureUiThread(); _sites.Clear(); _selected = -1; Rebuild(); }
        public void GenerateRandomSites(int count, int seed)
        {
            EnsureUiThread(); count = Math.Max(0, Math.Min(_maxSites, count)); _sites.Clear(); _selected = -1;
            Random random = new Random(seed); double w = Math.Max(1, ClientSize.Width), h = Math.Max(1, ClientSize.Height);
            for (int i = 0; i < count; i++) { bool added = false; for (int attempt = 0; attempt < 30 && !added; attempt++) { PointD p = ScreenToModel(new PointD(random.NextDouble() * w, random.NextDouble() * h)); added = AddRaw(p); } }
            Rebuild();
        }
        public void RelaxLloyd(int iterations)
        {
            EnsureUiThread(); int n = Math.Max(1, Math.Min(100, iterations));
            for (int step = 0; step < n; step++) { EnsureCells(); List<PointD> moved = new List<PointD>(_sites.Count); for (int i = 0; i < _sites.Count; i++) { Cell c = _cells[i]; if (c.Points.Length < 3) { moved.Add(_sites[i]); continue; } double sx = 0, sy = 0, a = 0; for (int j = 0; j < c.Points.Length; j++) { PointD p = c.Points[j], q = c.Points[(j + 1) % c.Points.Length]; double cross = p.X * q.Y - q.X * p.Y; a += cross; sx += (p.X + q.X) * cross; sy += (p.Y + q.Y) * cross; } if (Math.Abs(a) < Epsilon) moved.Add(_sites[i]); else moved.Add(new PointD(sx / (3 * a), sy / (3 * a))); } _sites.Clear(); _sites.AddRange(moved); Rebuild(); }
        }
        public void ZoomAt(Point location, double factor)
        {
            EnsureUiThread(); if (!IsFinite(factor) || factor <= 0) return; PointD before = ScreenToModel(new PointD(location.X, location.Y)); _zoom = Math.Max(0.1, Math.Min(10, _zoom * factor)); PointD after = ScreenToModel(new PointD(location.X, location.Y)); _offsetX += (after.X - before.X) * _zoom; _offsetY += (after.Y - before.Y) * _zoom; Rebuild();
        }
        public void ExportPng(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A path is required.", "path"); EnsureCells(); int w = Math.Max(1, ClientSize.Width), h = Math.Max(1, ClientSize.Height);
            using (Bitmap bitmap = new Bitmap(w, h)) using (Graphics g = Graphics.FromImage(bitmap)) { g.SmoothingMode = SmoothingMode.AntiAlias; DrawScene(g, w, h); bitmap.Save(path, ImageFormat.Png); }
        }
        private bool AddRaw(PointD p) { for (int k = 0; k < _sites.Count; k++) if (DistanceSquared(_sites[k], p) < 1.0) return false; _sites.Add(p); return true; }
        private void EnsureUiThread() { if (IsHandleCreated && InvokeRequired) throw new InvalidOperationException("Call this method on the control's UI thread."); }
        private static bool IsFinite(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        private static double DistanceSquared(PointD a, PointD b) { double x = a.X - b.X, y = a.Y - b.Y; return x * x + y * y; }
        private void Rebuild() { _dirty = true; EnsureCells(); Invalidate(); }
        private void EnsureCells()
        {
            if (!_dirty) return; _cells.Clear(); double w = Math.Max(1, ClientSize.Width), h = Math.Max(1, ClientSize.Height); PointD a = ScreenToModel(new PointD(0, 0)), b = ScreenToModel(new PointD(w, h)); double left = Math.Min(a.X, b.X), right = Math.Max(a.X, b.X), top = Math.Min(a.Y, b.Y), bottom = Math.Max(a.Y, b.Y);
            for (int i = 0; i < _sites.Count; i++)
            {
                List<PointD> poly = new List<PointD>(); poly.Add(new PointD(left, top)); poly.Add(new PointD(right, top)); poly.Add(new PointD(right, bottom)); poly.Add(new PointD(left, bottom)); PointD s = _sites[i];
                for (int j = 0; j < _sites.Count && poly.Count > 0; j++) if (i != j) { PointD q = _sites[j]; double nx = 2 * (q.X - s.X), ny = 2 * (q.Y - s.Y), limit = q.X * q.X + q.Y * q.Y - s.X * s.X - s.Y * s.Y; poly = Clip(poly, nx, ny, limit); }
                _cells.Add(new Cell(poly.ToArray()));
            }
            _dirty = false;
        }
        private static List<PointD> Clip(List<PointD> input, double nx, double ny, double limit)
        {
            List<PointD> output = new List<PointD>(); if (input.Count == 0) return output; PointD prev = input[input.Count - 1]; double dp = nx * prev.X + ny * prev.Y - limit; bool inp = dp <= Epsilon;
            for (int i = 0; i < input.Count; i++) { PointD cur = input[i]; double dc = nx * cur.X + ny * cur.Y - limit; bool inc = dc <= Epsilon; if (inc != inp) { double den = dp - dc; if (Math.Abs(den) > Epsilon) { double t = dp / den; output.Add(new PointD(prev.X + t * (cur.X - prev.X), prev.Y + t * (cur.Y - prev.Y))); } } if (inc) output.Add(cur); prev = cur; dp = dc; inp = inc; }
            return output;
        }
        private PointD ScreenToModel(PointD p) { return new PointD((p.X - ClientSize.Width / 2.0) / _zoom + _offsetX, (p.Y - ClientSize.Height / 2.0) / _zoom + _offsetY); }
        private PointF ModelToScreen(PointD p) { return new PointF((float)((p.X - _offsetX) * _zoom + ClientSize.Width / 2.0), (float)((p.Y - _offsetY) * _zoom + ClientSize.Height / 2.0)); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); EnsureCells(); DrawScene(e.Graphics, ClientSize.Width, ClientSize.Height); }
        private void DrawScene(Graphics g, int width, int height)
        {
            g.Clear(ZenTheme.Background); g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (Pen border = new Pen(ZenTheme.Border)) g.DrawRectangle(border, 0, 0, Math.Max(0, width - 1), Math.Max(0, height - 1));
            for (int i = 0; i < _cells.Count; i++) { PointD[] points = _cells[i].Points; if (points.Length < 3) continue; PointF[] screen = new PointF[points.Length]; for (int j = 0; j < points.Length; j++) screen[j] = ModelToScreen(points[j]); Color neon = _palette[i % _palette.Length]; using (SolidBrush fill = new SolidBrush(Color.FromArgb(35, neon))) using (Pen edge = new Pen(Color.FromArgb(175, neon), 1.2f)) { g.FillPolygon(fill, screen); g.DrawPolygon(edge, screen); } }
            for (int i = 0; i < _sites.Count; i++) { PointF p = ModelToScreen(_sites[i]); float r = i == _selected ? 7f : 5f; Color c = _palette[i % _palette.Length]; using (SolidBrush glow = new SolidBrush(Color.FromArgb(45, c))) using (SolidBrush dot = new SolidBrush(c)) using (Pen ring = new Pen(i == _selected ? ZenTheme.Foreground : Color.FromArgb(200, c), 1.2f)) { g.FillEllipse(glow, p.X - r - 5, p.Y - r - 5, (r + 5) * 2, (r + 5) * 2); g.FillEllipse(dot, p.X - r, p.Y - r, r * 2, r * 2); g.DrawEllipse(ring, p.X - r, p.Y - r, r * 2, r * 2); } if (_showLabels) { using (SolidBrush text = new SolidBrush(ZenTheme.Foreground)) g.DrawString(i.ToString(), Font, text, p.X + 8, p.Y - 8); } }
            if (_hover >= 0 && _hover < _sites.Count) { PointF p = ModelToScreen(_sites[_hover]); using (Pen pen = new Pen(ZenTheme.Accent, 1.3f)) g.DrawEllipse(pen, p.X - 11, p.Y - 11, 22, 22); }
            string info = "Sites: " + _sites.Count + " / " + _maxSites + "   |   O(n²) cells   |   Click add/select · Drag move · Right-click delete · Shift-drag pan · Wheel zoom"; using (SolidBrush b = new SolidBrush(Color.FromArgb(210, ZenTheme.Background))) using (SolidBrush t = new SolidBrush(ZenTheme.Muted)) { SizeF s = g.MeasureString(info, Font); g.FillRectangle(b, 5, 5, s.Width + 12, s.Height + 6); g.DrawString(info, Font, t, 10, 8); }
        }
        private int FindNearest(Point location, double maxDistance) { int best = -1; double dmax = maxDistance * maxDistance; for (int i = 0; i < _sites.Count; i++) { PointF p = ModelToScreen(_sites[i]); double d = (p.X - location.X) * (p.X - location.X) + (p.Y - location.Y) * (p.Y - location.Y); if (d < dmax) { dmax = d; best = i; } } return best; }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus(); _lastMouse = e.Location;
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Shift) == Keys.Shift)) { _panning = true; Capture = true; return; }
            if (e.Button == MouseButtons.Right) { int hit = FindNearest(e.Location, 18); if (hit >= 0) { _sites.RemoveAt(hit); if (_selected == hit) _selected = -1; else if (_selected > hit) _selected--; Rebuild(); } return; }
            if (e.Button == MouseButtons.Left) { int index = FindNearest(e.Location, 16); if (index >= 0) { _selected = index; _dragging = true; Capture = true; EventHandler<VoronoiSiteSelectedEventArgs> handler = SiteSelected; if (handler != null) handler(this, new VoronoiSiteSelectedEventArgs(index, _sites[index].X, _sites[index].Y)); Invalidate(); } else { PointD p = ScreenToModel(new PointD(e.X, e.Y)); int added = AddSite(p.X, p.Y); if (added >= 0) { _selected = added; EventHandler<VoronoiSiteSelectedEventArgs> handler = SiteSelected; if (handler != null) handler(this, new VoronoiSiteSelectedEventArgs(added, p.X, p.Y)); Invalidate(); } } }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (_panning) { _offsetX -= (e.X - _lastMouse.X) / _zoom; _offsetY -= (e.Y - _lastMouse.Y) / _zoom; _lastMouse = e.Location; Rebuild(); return; }
            if (_dragging && _selected >= 0) { PointD p = ScreenToModel(new PointD(e.X, e.Y)); bool collision = false; for (int i = 0; i < _sites.Count; i++) if (i != _selected && DistanceSquared(p, _sites[i]) < Epsilon * Epsilon) { collision = true; break; } if (!collision) { _sites[_selected] = p; Rebuild(); } return; }
            int hover = FindNearest(e.Location, 14); if (hover != _hover) { _hover = hover; Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _dragging = false; _panning = false; Capture = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); ZoomAt(e.Location, e.Delta > 0 ? 1.15 : 1.0 / 1.15); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Delete && _selected >= 0) { _sites.RemoveAt(_selected); _selected = -1; Rebuild(); e.Handled = true; } else if (e.KeyCode == Keys.Escape) { _selected = -1; Invalidate(); } }
        protected override void OnResize(EventArgs e) { base.OnResize(e); _dirty = true; Invalidate(); }
    }
}
