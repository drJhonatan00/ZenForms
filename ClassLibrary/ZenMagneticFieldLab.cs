using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenFieldIntegrator { MidpointRK2, ClassicalRK4 }

    public enum ZenFieldMode { Electric, MagneticDipole }

    public sealed class ZenFieldCharge
    {
        public Guid Id { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public double Strength { get; private set; }
        internal ZenFieldCharge(Guid id, float x, float y, double strength) { Id = id; X = x; Y = y; Strength = strength; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Interactive 2D electric fields and magnetic-dipole field lines, heatmaps and equipotentials.")]
    public class ZenMagneticFieldLab : UserControl
    {
        private sealed class Source { public Guid Id; public float X, Y; public double Q; }
        private readonly List<Source> sources = new List<Source>();
        private Bitmap heatCache;
        private Bitmap lineCache;
        private bool cacheDirty = true;
        private Guid? selected;
        private Point lastMouse;
        private bool dragging;
        private Rectangle modeRect, heatRect, potentialRect, integratorRect, densityRect, clearRect;
        private int density = 18;
        private bool showHeatmap = true, showPotential = true, showGrid = true;
        private ZenFieldMode mode = ZenFieldMode.Electric;
        private ZenFieldIntegrator integrator = ZenFieldIntegrator.MidpointRK2;
        private float strengthScale = 1f;

        [Category("Action"), Description("Raised when sources or visualization settings change.")]
        public event EventHandler FieldChanged;
        [Category("Behavior"), Description("Electric charges, or an educational magnetic-dipole analogy.")]
        public ZenFieldMode Mode { get { return mode; } set { if (mode != value) { mode = value; InvalidateField(); } } }
        [Category("Behavior"), Description("RK2 midpoint or classical fourth-order Runge-Kutta.")]
        public ZenFieldIntegrator Integrator { get { return integrator; } set { if (integrator != value) { integrator = value; InvalidateField(); } } }
        [Category("Behavior"), Description("Controls the field-line density; values are clamped to 4..48.")]
        public int LineDensity { get { return density; } set { int n = Math.Max(4, Math.Min(48, value)); if (density != n) { density = n; InvalidateField(); } } }
        [Category("Appearance"), Description("Show the purple-to-cyan magnitude heatmap.")]
        public bool ShowHeatmap { get { return showHeatmap; } set { if (showHeatmap != value) { showHeatmap = value; Invalidate(); RaiseChanged(); } } }
        [Category("Appearance"), Description("Show approximate potential contours.")]
        public bool ShowEquipotentials { get { return showPotential; } set { if (showPotential != value) { showPotential = value; InvalidateField(); } } }
        [Category("Appearance"), Description("Show coordinate grid and scale.")]
        public bool ShowGrid { get { return showGrid; } set { if (showGrid != value) { showGrid = value; Invalidate(); } } }
        [Browsable(false)]
        public IList<ZenFieldCharge> Charges { get { return Snapshot(); } }

        public ZenMagneticFieldLab()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(260, 190); Size = new Size(620, 420); TabStop = true;
        }

        public Guid AddCharge(float x, float y, double strength)
        {
            if (InvokeRequired) throw new InvalidOperationException("AddCharge must be called on the UI thread.");
            if (sources.Count >= 32) throw new InvalidOperationException("A maximum of 32 sources is supported.");
            x = Clamp01(x); y = Clamp01(y); strength = Math.Max(-10, Math.Min(10, strength));
            if (Math.Abs(strength) < 0.01) throw new ArgumentOutOfRangeException("strength", "Strength must be non-zero.");
            Guid id = Guid.NewGuid(); sources.Add(new Source { Id = id, X = x, Y = y, Q = strength }); InvalidateField(); return id;
        }
        public void ClearCharges() { if (InvokeRequired) throw new InvalidOperationException("ClearCharges must be called on the UI thread."); sources.Clear(); selected = null; InvalidateField(); }
        public bool RemoveCharge(Guid id) { Source s = sources.FirstOrDefault(a => a.Id == id); if (s == null) return false; sources.Remove(s); selected = null; InvalidateField(); return true; }
        public bool SetChargeStrength(Guid id, double strength) { Source s = sources.FirstOrDefault(a => a.Id == id); if (s == null) return false; if (Math.Abs(strength) < .01) throw new ArgumentOutOfRangeException("strength"); s.Q = Math.Max(-10, Math.Min(10, strength)); InvalidateField(); return true; }
        public IList<ZenFieldCharge> Snapshot() { return sources.Select(s => new ZenFieldCharge(s.Id, s.X, s.Y, s.Q)).ToList().AsReadOnly(); }
        public void SaveImage(string fileName) { if (String.IsNullOrEmpty(fileName)) throw new ArgumentException("A file name is required.", "fileName"); using (Bitmap image = new Bitmap(Math.Max(1, Width), Math.Max(1, Height))) { DrawToBitmap(image, new Rectangle(0, 0, image.Width, image.Height)); image.Save(fileName, ImageFormat.Png); } }

        private static float Clamp01(float v) { return Math.Max(0, Math.Min(1, v)); }
        private void InvalidateField() { cacheDirty = true; Invalidate(); RaiseChanged(); }
        private void RaiseChanged() { EventHandler h = FieldChanged; if (h != null) h(this, EventArgs.Empty); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); DisposeCaches(); cacheDirty = true; }
        protected override void Dispose(bool disposing) { if (disposing) DisposeCaches(); base.Dispose(disposing); }
        private void DisposeCaches() { if (heatCache != null) { heatCache.Dispose(); heatCache = null; } if (lineCache != null) { lineCache.Dispose(); lineCache = null; } }

        private Rectangle PlotRect { get { return new Rectangle(12, 48, Math.Max(1, Width - 24), Math.Max(1, Height - 60)); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ZenTheme.Background); LayoutToolbar(); DrawToolbar(g);
            Rectangle plot = PlotRect; using (SolidBrush b = new SolidBrush(ZenTheme.Surface)) g.FillRectangle(b, plot);
            if (plot.Width < 24 || plot.Height < 24) return;
            if (cacheDirty) BuildCaches(plot);
            if (showHeatmap && heatCache != null) g.DrawImage(heatCache, plot);
            if (lineCache != null) g.DrawImage(lineCache, plot);
            if (showGrid) DrawGrid(g, plot);
            DrawSources(g, plot); DrawScale(g, plot);
        }
        private void LayoutToolbar()
        {
            int x = 10, y = 8, h = 30;
            modeRect = new Rectangle(x, y, 116, h); x += 120; heatRect = new Rectangle(x, y, 66, h); x += 70;
            potentialRect = new Rectangle(x, y, 72, h); x += 76; integratorRect = new Rectangle(x, y, 72, h); x += 76;
            densityRect = new Rectangle(x, y, 100, h); x += 104; clearRect = new Rectangle(x, y, 58, h);
        }
        private void DrawToolbar(Graphics g)
        {
            DrawButton(g, modeRect, mode == ZenFieldMode.Electric ? "Electric field" : "Magnetic dipole", true);
            DrawButton(g, heatRect, showHeatmap ? "Heat: on" : "Heat: off", showHeatmap);
            DrawButton(g, potentialRect, showPotential ? "Equip: on" : "Equip: off", showPotential);
            DrawButton(g, integratorRect, integrator == ZenFieldIntegrator.MidpointRK2 ? "RK2" : "RK4", false);
            DrawButton(g, densityRect, "Lines " + density, false); DrawButton(g, clearRect, "Clear", false);
        }
        private void DrawButton(Graphics g, Rectangle r, string text, bool active)
        { using (SolidBrush b = new SolidBrush(active ? ZenTheme.SurfaceRaised : ZenTheme.Surface)) g.FillRectangle(b, r); using (Pen p = new Pen(active ? ZenTheme.Primary : ZenTheme.Border)) g.DrawRectangle(p, r); using (Brush b = new SolidBrush(ZenTheme.Foreground)) using (StringFormat f = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) g.DrawString(text, Font, b, r, f); }
        private void BuildCaches(Rectangle plot)
        {
            cacheDirty = false; DisposeCaches(); int w = Math.Min(420, Math.Max(1, plot.Width)), h = Math.Min(320, Math.Max(1, plot.Height));
            try
            {
                heatCache = new Bitmap(w, h, PixelFormat.Format32bppArgb); using (Graphics hg = Graphics.FromImage(heatCache)) { hg.Clear(Color.Transparent); if (showHeatmap) PaintHeat(hg, w, h); }
                lineCache = new Bitmap(w, h, PixelFormat.Format32bppArgb); using (Graphics lg = Graphics.FromImage(lineCache)) { lg.SmoothingMode = SmoothingMode.AntiAlias; PaintLines(lg, w, h); if (showPotential) PaintEquipotentials(lg, w, h); }
            }
            catch { DisposeCaches(); }
        }
        private void PaintHeat(Graphics g, int w, int h)
        {
            if (sources.Count == 0) return; using (Bitmap b = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { double ex, ey, p; Field((x + .5) / w, (y + .5) / h, out ex, out ey, out p); double m = Math.Sqrt(ex * ex + ey * ey); double t = (float)(Math.Log(1 + m * 0.035) / Math.Log(1 + Math.Max(1, MaxStrength() * 4) * .035)); t = Math.Max(0, Math.Min(1, t)); Color c = Blend(Color.FromArgb(28, 16, 48), Color.FromArgb(40, 210, 195), t); b.SetPixel(x, y, Color.FromArgb(140, c)); }
                g.DrawImage(b, 0, 0, w, h);
            }
        }
        private static Color Blend(Color a, Color b, double t) { return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private double MaxStrength() { return Math.Max(1, sources.Sum(s => Math.Abs(s.Q))); }
        private void Field(double x, double y, out double ex, out double ey, out double potential)
        {
            ex = 0; ey = 0; potential = 0; foreach (Source s in sources)
            {
                double dx = x - s.X, dy = y - s.Y, r2 = dx * dx + dy * dy; if (r2 < 0.00009) r2 = 0.00009; double r = Math.Sqrt(r2), q = s.Q * strengthScale;
                if (mode == ZenFieldMode.Electric) { double f = q / (r2 * r); ex += f * dx; ey += f * dy; potential += q / r; }
                else
                {
                    double f = q / r2; ex += f * dx / r; ey += f * dy / r; potential += q / r;
                }
            }
        }
        private void PaintLines(Graphics g, int w, int h)
        {
            if (sources.Count == 0) return; int count = 0;
            foreach (Source s in sources.Where(a => a.Q > 0)) for (int i = 0; i < density && count < 180; i++, count++) { double angle = 2 * Math.PI * i / density; double sx = s.X + Math.Cos(angle) * .012, sy = s.Y + Math.Sin(angle) * .012; Trace(g, w, h, sx, sy, 1); }
            foreach (Source s in sources.Where(a => a.Q < 0)) for (int i = 0; i < density && count < 180; i++, count++) { double angle = 2 * Math.PI * i / density; double sx = s.X + Math.Cos(angle) * .012, sy = s.Y + Math.Sin(angle) * .012; Trace(g, w, h, sx, sy, -1); }
        }
        private void Trace(Graphics g, int w, int h, double x, double y, int direction)
        {
            List<PointF> points = new List<PointF>(); int max = 1000; double step = .0035;
            for (int i = 0; i < max; i++) { if (x < 0 || x > 1 || y < 0 || y > 1) break; points.Add(new PointF((float)(x * w), (float)(y * h))); double nx, ny; Step(x, y, direction * step, out nx, out ny); double dx = nx - x, dy = ny - y; if (dx * dx + dy * dy < 1e-12) break; x = nx; y = ny; if (NearSource(x, y, .014)) break; }
            if (points.Count > 1) { using (Pen p = new Pen(Color.FromArgb(205, ZenTheme.Accent), 1.15f)) { g.DrawLines(p, points.ToArray()); int k = points.Count * 3 / 4; if (k > 1 && k < points.Count) DrawArrow(g, points[k - 1], points[k], ZenTheme.Accent); } }
        }
        private void Step(double x, double y, double step, out double ox, out double oy)
        {
            double ax, ay, p; Unit(x, y, out ax, out ay); if (integrator == ZenFieldIntegrator.MidpointRK2) { double bx, by; Unit(x + ax * step * .5, y + ay * step * .5, out bx, out by); ox = x + bx * step; oy = y + by * step; }
            else { double b1x, b1y, b2x, b2y, b3x, b3y, b4x, b4y; Unit(x, y, out b1x, out b1y); Unit(x + b1x * step / 2, y + b1y * step / 2, out b2x, out b2y); Unit(x + b2x * step / 2, y + b2y * step / 2, out b3x, out b3y); Unit(x + b3x * step, y + b3y * step, out b4x, out b4y); ox = x + step * (b1x + 2 * b2x + 2 * b3x + b4x) / 6; oy = y + step * (b1y + 2 * b2y + 2 * b3y + b4y) / 6; }
        }
        private void Unit(double x, double y, out double ux, out double uy) { double ex, ey, p; Field(x, y, out ex, out ey, out p); double m = Math.Sqrt(ex * ex + ey * ey); if (m < 1e-12) { ux = uy = 0; } else { ux = ex / m; uy = ey / m; } }
        private bool NearSource(double x, double y, double r) { foreach (Source s in sources) { double dx = x - s.X, dy = y - s.Y; if (dx * dx + dy * dy < r * r) return true; } return false; }
        private static void DrawArrow(Graphics g, PointF a, PointF b, Color c) { double angle = Math.Atan2(b.Y - a.Y, b.X - a.X); PointF p1 = new PointF((float)(b.X - 5 * Math.Cos(angle - .55)), (float)(b.Y - 5 * Math.Sin(angle - .55))), p2 = new PointF((float)(b.X - 5 * Math.Cos(angle + .55)), (float)(b.Y - 5 * Math.Sin(angle + .55))); using (Pen p = new Pen(c, 1.3f)) { g.DrawLine(p, b, p1); g.DrawLine(p, b, p2); } }
        private void PaintEquipotentials(Graphics g, int w, int h)
        {
            if (sources.Count == 0) return; int step = 4; double[,] v = new double[w / step + 1, h / step + 1]; for (int yy = 0; yy < v.GetLength(1); yy++) for (int xx = 0; xx < v.GetLength(0); xx++) { double a, b, p; Field((double)(xx * step) / w, (double)(yy * step) / h, out a, out b, out p); v[xx, yy] = p; }
            double max = Math.Min(20, MaxStrength() * 5); using (Pen pen = new Pen(Color.FromArgb(105, ZenTheme.Primary), 1f)) for (double level = -max; level <= max; level += max / 7.0) { if (Math.Abs(level) < .001) continue; for (int yy = 0; yy < v.GetLength(1) - 1; yy++) for (int xx = 0; xx < v.GetLength(0) - 1; xx++) { double a = v[xx, yy], b = v[xx + 1, yy], c = v[xx + 1, yy + 1], d = v[xx, yy + 1]; List<PointF> hits = new List<PointF>(); AddCross(hits, a, b, level, xx * step, yy * step, (xx + 1) * step, yy * step); AddCross(hits, b, c, level, (xx + 1) * step, yy * step, (xx + 1) * step, (yy + 1) * step); AddCross(hits, c, d, level, (xx + 1) * step, (yy + 1) * step, xx * step, (yy + 1) * step); AddCross(hits, d, a, level, xx * step, (yy + 1) * step, xx * step, yy * step); if (hits.Count >= 2) g.DrawLine(pen, hits[0], hits[1]); } }
        }
        private static void AddCross(List<PointF> p, double a, double b, double level, int x1, int y1, int x2, int y2) { if ((a < level && b >= level) || (b < level && a >= level)) { double t = (level - a) / (b - a); p.Add(new PointF((float)(x1 + (x2 - x1) * t), (float)(y1 + (y2 - y1) * t))); } }
        private void DrawGrid(Graphics g, Rectangle r) { using (Pen p = new Pen(Color.FromArgb(35, ZenTheme.Muted))) { for (int i = 1; i < 10; i++) { int x = r.Left + r.Width * i / 10, y = r.Top + r.Height * i / 10; g.DrawLine(p, x, r.Top, x, r.Bottom); g.DrawLine(p, r.Left, y, r.Right, y); } } }
        private void DrawSources(Graphics g, Rectangle r)
        {
            foreach (Source s in sources) { float x = r.Left + s.X * r.Width, y = r.Top + s.Y * r.Height; float rad = 7; using (SolidBrush b = new SolidBrush(s.Q > 0 ? ZenTheme.Accent : ZenTheme.Primary)) g.FillEllipse(b, x - rad, y - rad, rad * 2, rad * 2); using (Pen p = new Pen(ZenTheme.Foreground, 1.5f)) { g.DrawEllipse(p, x - rad, y - rad, rad * 2, rad * 2); g.DrawLine(p, x - 3, y, x + 3, y); if (s.Q > 0) g.DrawLine(p, x, y - 3, x, y + 3); } using (Brush b = new SolidBrush(ZenTheme.Foreground)) g.DrawString(s.Q.ToString("+0.##;-0.##"), Font, b, x + 9, y - 8); if (selected == s.Id) using (Pen p = new Pen(ZenTheme.Accent, 1)) { g.DrawEllipse(p, x - rad - 3, y - rad - 3, rad * 2 + 6, rad * 2 + 6); } }
        }
        private void DrawScale(Graphics g, Rectangle r) { using (Brush b = new SolidBrush(ZenTheme.Muted)) { g.DrawString(showGrid ? "0.0 — 1.0 normalized · drag sources · click + · right-click remove" : "Drag sources · click + · right-click remove", Font, b, r.Left + 5, r.Bottom - 19); g.DrawString(mode == ZenFieldMode.MagneticDipole ? "Educational convention: signed magnetic poles with radial inverse-square H (not a full 3D solenoid model)" : "E-field: signed point charges; arrows follow E", Font, b, r.Left + 5, r.Top + 3); } }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus(); LayoutToolbar(); if (modeRect.Contains(e.Location)) { Mode = mode == ZenFieldMode.Electric ? ZenFieldMode.MagneticDipole : ZenFieldMode.Electric; return; }
            if (heatRect.Contains(e.Location)) { ShowHeatmap = !showHeatmap; return; }
            if (potentialRect.Contains(e.Location)) { ShowEquipotentials = !showPotential; return; }
            if (integratorRect.Contains(e.Location)) { Integrator = integrator == ZenFieldIntegrator.MidpointRK2 ? ZenFieldIntegrator.ClassicalRK4 : ZenFieldIntegrator.MidpointRK2; return; }
            if (densityRect.Contains(e.Location)) { LineDensity = density >= 40 ? 8 : density + 4; return; }
            if (clearRect.Contains(e.Location)) { ClearCharges(); return; }
            Rectangle r = PlotRect; if (!r.Contains(e.Location)) return; Source hit = FindSource(e.Location, r); if (e.Button == MouseButtons.Right) { if (hit != null) RemoveCharge(hit.Id); else if (sources.Count < 32) AddCharge((float)(e.X - r.Left) / r.Width, (float)(e.Y - r.Top) / r.Height, -1); return; }
            if (e.Button == MouseButtons.Left) { if (hit != null) { selected = hit.Id; dragging = true; lastMouse = e.Location; Invalidate(); } else if (sources.Count < 32) { Guid id = AddCharge((float)(e.X - r.Left) / r.Width, (float)(e.Y - r.Top) / r.Height, 1); selected = id; } }
        }
        private Source FindSource(Point p, Rectangle r) { return sources.OrderBy(s => Math.Pow((r.Left + s.X * r.Width - p.X), 2) + Math.Pow((r.Top + s.Y * r.Height - p.Y), 2)).FirstOrDefault(s => Math.Pow((r.Left + s.X * r.Width - p.X), 2) + Math.Pow((r.Top + s.Y * r.Height - p.Y), 2) < 18 * 18); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (!dragging || selected == null) return; Rectangle r = PlotRect; Source s = sources.FirstOrDefault(a => a.Id == selected.Value); if (s != null) { s.X = Clamp01((float)(e.X - r.Left) / r.Width); s.Y = Clamp01((float)(e.Y - r.Top) / r.Height); InvalidateField(); } lastMouse = e.Location; }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); if (selected.HasValue) { Source s = sources.FirstOrDefault(a => a.Id == selected.Value); if (s != null) { s.Q = Math.Max(-10, Math.Min(10, s.Q + (e.Delta > 0 ? .25 : -.25))); if (Math.Abs(s.Q) < .1) s.Q = e.Delta > 0 ? .1 : -.1; InvalidateField(); } } else LineDensity = density + (e.Delta > 0 ? 2 : -2); }
        protected override bool IsInputKey(Keys keyData) { if (keyData == Keys.Delete || keyData == Keys.Back || keyData == Keys.Add || keyData == Keys.Subtract) return true; return base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) { if (selected.HasValue) RemoveCharge(selected.Value); e.Handled = true; } else if (selected.HasValue && (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)) { Source s = sources.FirstOrDefault(a => a.Id == selected.Value); if (s != null) { double d = (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) ? .25 : -.25; s.Q = Math.Max(-10, Math.Min(10, s.Q + d)); InvalidateField(); } e.Handled = true; } }
    }
}

// watashi wa watashi sore dake