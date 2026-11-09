using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class ZenOrbitalBody
    {
        public string Name { get; set; }
        public double Mass { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
        internal readonly List<PointF> Trail = new List<PointF>();
        public ZenOrbitalBody(string name, double mass, double x, double y, double velocityX, double velocityY, float radius, Color color)
        { Name = name ?? "Body"; Mass = mass; X = x; Y = y; VelocityX = velocityX; VelocityY = velocityY; Radius = radius; Color = color; }
    }

    public sealed class ZenOrbitalBodySnapshot : EventArgs
    {
        public readonly string Name; public readonly double Mass, X, Y, VelocityX, VelocityY; public readonly float Radius; public readonly Color Color;
        internal ZenOrbitalBodySnapshot(ZenOrbitalBody b) { Name = b.Name; Mass = b.Mass; X = b.X; Y = b.Y; VelocityX = b.VelocityX; VelocityY = b.VelocityY; Radius = b.Radius; Color = b.Color; }
    }

    public sealed class ZenOrbitalSnapshot : EventArgs
    {
        public readonly ZenOrbitalBodySnapshot[] Bodies;
        public readonly double CenterOfMassX, CenterOfMassY, TotalEnergy;
        public readonly bool IsPaused;
        public readonly double SimulationTime;
        internal ZenOrbitalSnapshot(ZenOrbitalBodySnapshot[] bodies, double cx, double cy, double energy, bool paused, double time)
        { Bodies = bodies; CenterOfMassX = cx; CenterOfMassY = cy; TotalEnergy = energy; IsPaused = paused; SimulationTime = time; }
    }


    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Newton Orbital Mechanic simulator.")]
    public class ZenOrbitalSandbox : UserControl
    {
        private const int MaximumBodies = 64;
        private const int MaximumTrailPoints = 500;
        private readonly List<ZenOrbitalBody> _bodies = new List<ZenOrbitalBody>();
        private readonly Timer _timer;
        private int _selected = -1;
        private double _cameraX, _cameraY, _zoom = 1.0, _gravity = 1.0, _speed = 1.0, _simulationTime;
        private int _substeps = 4, _trailLimit = 160;
        private bool _paused, _draggingPan, _draggingCreate, _showTrails = true, _showVelocity = true;
        private Point _lastMouse, _createStart, _createEnd;
        private double _createWorldX, _createWorldY;
        private string _status = "Drag on space to create a body";
        [Category("Behavior")]
        public event EventHandler<ZenOrbitalSnapshot> SimulationUpdated;
        [Category("Behavior")]
        public event EventHandler<ZenOrbitalBodySnapshot> SelectedBodyChanged;

        public ZenOrbitalSandbox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(240, 180); TabStop = true;
            _timer = new Timer(); _timer.Interval = 16; _timer.Tick += TimerTick; _timer.Start();
        }

        [Category("Simulation"), Description("Gravitacional constant.")]
        public double GravitationalConstant { get { return _gravity; } set { _gravity = ClampFinite(value, 0, 10000, 1); Invalidate(); } }
        [Category("Simulation"), Description("Global simulation speed.")]
        public double SimulationSpeed { get { return _speed; } set { _speed = ClampFinite(value, 0, 20, 1); } }
        [Category("Simulation"), Description("Substeps every update by velocity-Verlet.")]
        public int Substeps { get { return _substeps; } set { _substeps = Math.Max(1, Math.Min(16, value)); } }
        [Category("Appearance"), Description("Points limits.")]
        public int TrailPointLimit { get { return _trailLimit; } set { _trailLimit = Math.Max(0, Math.Min(MaximumTrailPoints, value)); TrimTrails(); Invalidate(); } }
        [Browsable(false)] public bool IsPaused { get { return _paused; } }
        [Browsable(false)] public ZenOrbitalBody SelectedBody { get { return _selected >= 0 && _selected < _bodies.Count ? _bodies[_selected] : null; } }
        [Browsable(false)] public int BodyCount { get { return _bodies.Count; } }
        [Category("Appearance"), Description("Show or hide orbital/gravitacional trajetorys.")]
        public bool ShowTrails { get { return _showTrails; } set { _showTrails = value; Invalidate(); } }
        [Category("Appearance"), Description("Show/Hide the selected body speed vector.")]
        public bool ShowSelectedVelocity { get { return _showVelocity; } set { _showVelocity = value; Invalidate(); } }

        public ZenOrbitalBody AddBody(string name, double mass, double x, double y, double velocityX, double velocityY, float radius, Color color)
        {
            EnsureUiThread(); if (_bodies.Count >= MaximumBodies) throw new InvalidOperationException("64 bodys is the limit.");
            mass = ClampFinite(mass, 0.000001, 1.0e12, 1); x = ClampFinite(x, -1.0e9, 1.0e9, 0); y = ClampFinite(y, -1.0e9, 1.0e9, 0);
            velocityX = ClampFinite(velocityX, -1.0e6, 1.0e6, 0); velocityY = ClampFinite(velocityY, -1.0e6, 1.0e6, 0);
            radius = (float)ClampFinite(radius, 1, 10000, 8);
            ZenOrbitalBody body = new ZenOrbitalBody(name, mass, x, y, velocityX, velocityY, radius, color.IsEmpty ? ZenTheme.Accent : color);
            _bodies.Add(body); _selected = _bodies.Count - 1; RaiseUpdated(); RaiseSelectionChanged(); Invalidate(); return body;
        }
        public void ClearBodies() { EnsureUiThread(); _bodies.Clear(); _selected = -1; _simulationTime = 0; RaiseUpdated(); RaiseSelectionChanged(); Invalidate(); }
        public void TogglePause() { EnsureUiThread(); _paused = !_paused; RaiseUpdated(); Invalidate(); }
        public void SetPaused(bool paused) { EnsureUiThread(); _paused = paused; RaiseUpdated(); Invalidate(); }
        public void StepOnce() { EnsureUiThread(); Advance(1.0 / 60.0); Invalidate(); RaiseUpdated(); }
        public void FrameAll() { EnsureUiThread(); if (_bodies.Count == 0) return; double minX = _bodies[0].X, maxX = minX, minY = _bodies[0].Y, maxY = minY; foreach (ZenOrbitalBody b in _bodies) { minX = Math.Min(minX, b.X); maxX = Math.Max(maxX, b.X); minY = Math.Min(minY, b.Y); maxY = Math.Max(maxY, b.Y); } _cameraX = (minX + maxX) / 2; _cameraY = (minY + maxY) / 2; double span = Math.Max(maxX - minX, maxY - minY) + 40; _zoom = Math.Max(0.08, Math.Min(8, Math.Min(Math.Max(1, ClientSize.Width - 80) / span, Math.Max(1, ClientSize.Height - 100) / span))); Invalidate(); }
        public void LoadBinaryOrbitPreset() { ClearBodies(); AddBody("Aster", 120, -55, 0, 0, -0.72, 13, ZenTheme.Primary); AddBody("Vesper", 120, 55, 0, 0, 0.72, 13, ZenTheme.Accent); FrameAll(); }
        public void LoadSimpleStarSystemPreset() { ClearBodies(); AddBody("Sun", 1200, 0, 0, 0, 0, 17, Color.FromArgb(255, 205, 92)); AddBody("Iris", 2, 95, 0, 0, 3.56, 7, ZenTheme.Accent); AddBody("Cinder", 5, 165, 0, 0, 2.70, 9, ZenTheme.Primary); AddBody("Lumen", 9, 245, 0, 0, 2.23, 10, Color.FromArgb(255, 135, 177)); FrameAll(); }
        public ZenOrbitalSnapshot CreateSnapshot() { return MakeSnapshot(); }

        private void TimerTick(object sender, EventArgs e) { if (!_paused && !IsDisposed) { Advance(ClampFinite(_timer.Interval / 1000.0 * _speed, 0, 0.05, 0)); Invalidate(); RaiseUpdated(); } }
        private void Advance(double dt)
        {
            if (dt <= 0 || _bodies.Count == 0) return; dt = Math.Min(0.05, dt); double h = dt / _substeps;
            for (int s = 0; s < _substeps; s++)
            {
                double[] ax, ay; ComputeAcceleration(out ax, out ay);
                for (int i = 0; i < _bodies.Count; i++) { ZenOrbitalBody b = _bodies[i]; b.X += b.VelocityX * h + 0.5 * ax[i] * h * h; b.Y += b.VelocityY * h + 0.5 * ay[i] * h * h; }
                double[] bx, by; ComputeAcceleration(out bx, out by);
                for (int i = 0; i < _bodies.Count; i++) { ZenOrbitalBody b = _bodies[i]; b.VelocityX += 0.5 * (ax[i] + bx[i]) * h; b.VelocityY += 0.5 * (ay[i] + by[i]) * h; }
            }
            _simulationTime += dt; RecordTrails();
        }
        private void ComputeAcceleration(out double[] ax, out double[] ay)
        {
            int n = _bodies.Count; ax = new double[n]; ay = new double[n];
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
                {
                    ZenOrbitalBody a = _bodies[i], b = _bodies[j]; double dx = b.X - a.X, dy = b.Y - a.Y, d2 = dx * dx + dy * dy;
                    if (d2 < 1e-8 || double.IsInfinity(d2) || double.IsNaN(d2)) continue;
                    double d = Math.Sqrt(d2), factor = _gravity / (d2 + Math.Max(1e-6, (a.Radius + b.Radius) * 0.025));
                    double ux = dx / d * factor; double uy = dy / d * factor;
                    ax[i] += ux * b.Mass; ay[i] += uy * b.Mass; ax[j] -= ux * a.Mass; ay[j] -= uy * a.Mass;
                }
        }
        private void RecordTrails() { if (_trailLimit <= 0) return; foreach (ZenOrbitalBody b in _bodies) { b.Trail.Add(new PointF((float)b.X, (float)b.Y)); if (b.Trail.Count > _trailLimit) b.Trail.RemoveRange(0, b.Trail.Count - _trailLimit); } }
        private void TrimTrails() { foreach (ZenOrbitalBody b in _bodies) if (b.Trail.Count > _trailLimit) b.Trail.RemoveRange(0, b.Trail.Count - _trailLimit); }
        private ZenOrbitalSnapshot MakeSnapshot()
        {
            ZenOrbitalBodySnapshot[] copies = new ZenOrbitalBodySnapshot[_bodies.Count]; double mass = 0, cx = 0, cy = 0, energy = 0;
            for (int i = 0; i < _bodies.Count; i++) { ZenOrbitalBody b = _bodies[i]; copies[i] = new ZenOrbitalBodySnapshot(b); mass += b.Mass; cx += b.X * b.Mass; cy += b.Y * b.Mass; energy += 0.5 * b.Mass * (b.VelocityX * b.VelocityX + b.VelocityY * b.VelocityY); }
            if (mass > 0) { cx /= mass; cy /= mass; }
            double potential = 0;
            for (int i = 0; i < _bodies.Count; i++) for (int j = i + 1; j < _bodies.Count; j++) { ZenOrbitalBody a = _bodies[i], b = _bodies[j]; double dx = b.X - a.X, dy = b.Y - a.Y, d = Math.Sqrt(dx * dx + dy * dy); if (d > 1e-8 && !double.IsInfinity(d)) potential -= _gravity * a.Mass * b.Mass / Math.Sqrt(d * d + Math.Max(1e-6, (a.Radius + b.Radius) * 0.025)); }
            return new ZenOrbitalSnapshot(copies, cx, cy, energy + potential, _paused, _simulationTime);
        }
        private void RaiseUpdated() { EventHandler<ZenOrbitalSnapshot> h = SimulationUpdated; if (h != null) h(this, MakeSnapshot()); }
        private void RaiseSelectionChanged() { EventHandler<ZenOrbitalBodySnapshot> h = SelectedBodyChanged; if (h != null) h(this, SelectedBody == null ? null : new ZenOrbitalBodySnapshot(SelectedBody)); }
        private void EnsureUiThread() { if (IsHandleCreated && InvokeRequired) throw new InvalidOperationException("The API must be called on interface thread."); }
        private static double ClampFinite(double v, double min, double max, double fallback) { if (double.IsNaN(v) || double.IsInfinity(v)) return fallback; return Math.Max(min, Math.Min(max, v)); }
        private PointF WorldToScreen(double x, double y) { return new PointF((float)(ClientSize.Width / 2.0 + (x - _cameraX) * _zoom), (float)(ClientSize.Height / 2.0 + (y - _cameraY) * _zoom)); }
        private PointF ScreenToWorld(Point p) { return new PointF((float)(_cameraX + (p.X - ClientSize.Width / 2.0) / _zoom), (float)(_cameraY + (p.Y - ClientSize.Height / 2.0) / _zoom)); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(ZenTheme.Background);
            using (Pen grid = new Pen(Color.FromArgb(24, ZenTheme.Border), 1)) { for (int x = ClientSize.Width / 2 % 40; x < ClientSize.Width; x += 40) g.DrawLine(grid, x, 0, x, ClientSize.Height); for (int y = ClientSize.Height / 2 % 40; y < ClientSize.Height; y += 40) g.DrawLine(grid, 0, y, ClientSize.Width, y); }
            if (_showTrails) foreach (ZenOrbitalBody b in _bodies) if (b.Trail.Count > 1) { using (Pen p = new Pen(Color.FromArgb(100, b.Color), 1.5f)) { PointF[] pts = new PointF[b.Trail.Count]; for (int k = 0; k < pts.Length; k++) pts[k] = WorldToScreen(b.Trail[k].X, b.Trail[k].Y); g.DrawLines(p, pts); } }
            for (int i = 0; i < _bodies.Count; i++) { ZenOrbitalBody b = _bodies[i]; PointF p = WorldToScreen(b.X, b.Y); float r = (float)Math.Max(3, Math.Min(45, b.Radius * _zoom)); using (Brush brush = new SolidBrush(b.Color)) g.FillEllipse(brush, p.X - r, p.Y - r, 2 * r, 2 * r); if (i == _selected) { using (Pen ring = new Pen(ZenTheme.Foreground, 1.5f)) g.DrawEllipse(ring, p.X - r - 4, p.Y - r - 4, 2 * r + 8, 2 * r + 8); if (_showVelocity) { using (Pen vpen = new Pen(ZenTheme.Accent, 2)) { vpen.EndCap = LineCap.ArrowAnchor; g.DrawLine(vpen, p.X, p.Y, p.X + (float)(b.VelocityX * 12 * _zoom), p.Y + (float)(b.VelocityY * 12 * _zoom)); } } } using (Brush text = new SolidBrush(ZenTheme.Foreground)) g.DrawString(b.Name ?? "Body", Font, text, p.X + r + 4, p.Y - r); }
            if (_draggingCreate) { using (Pen p = new Pen(ZenTheme.Accent, 2)) { p.DashStyle = DashStyle.Dash; g.DrawLine(p, _createStart, _createEnd); PointF w = ScreenToWorld(_createStart); g.DrawEllipse(p, _createStart.X - 7, _createStart.Y - 7, 14, 14); } }
            DrawHud(g);
        }
        private void DrawHud(Graphics g)
        {
            using (Brush panel = new SolidBrush(Color.FromArgb(225, ZenTheme.Surface))) g.FillRectangle(panel, 10, 10, Math.Min(350, ClientSize.Width - 20), 68);
            string state = _paused ? "PAUSED" : "EXECUTING"; string details = state + "  |  bodys " + _bodies.Count + "/" + MaximumBodies + "  |  zoom " + _zoom.ToString("0.00") + "x\n" + _status;
            using (Brush b = new SolidBrush(ZenTheme.Foreground)) g.DrawString(details, Font, b, 18, 16);
            ZenOrbitalSnapshot snap = MakeSnapshot(); string stats = "CM (" + snap.CenterOfMassX.ToString("0.0") + ", " + snap.CenterOfMassY.ToString("0.0") + ")   Energy ~ " + snap.TotalEnergy.ToString("0.00");
            using (Brush b = new SolidBrush(ZenTheme.Muted)) g.DrawString(stats, Font, b, 18, 56);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus(); if (e.Button == MouseButtons.Right) { _draggingPan = true; _lastMouse = e.Location; Cursor = Cursors.Hand; return; }
            if (e.Button != MouseButtons.Left) return;
            int hit = HitBody(e.Location); if (hit >= 0) { _selected = hit; RaiseSelectionChanged(); _status = "Selected: " + _bodies[hit].Name; _draggingPan = true; _lastMouse = e.Location; Cursor = Cursors.Hand; Invalidate(); return; }
            if (_bodies.Count < MaximumBodies) { _draggingCreate = true; _createStart = e.Location; _createEnd = e.Location; PointF w = ScreenToWorld(e.Location); _createWorldX = w.X; _createWorldY = w.Y; Invalidate(); }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); if (_draggingPan) { int dx = e.X - _lastMouse.X, dy = e.Y - _lastMouse.Y; _cameraX -= dx / _zoom; _cameraY -= dy / _zoom; _lastMouse = e.Location; Invalidate(); } else if (_draggingCreate) { _createEnd = e.Location; Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e); if (e.Button == MouseButtons.Right || _draggingPan) { _draggingPan = false; Cursor = Cursors.Default; }
            if (e.Button == MouseButtons.Left && _draggingCreate) { _draggingCreate = false; double vx = (e.X - _createStart.X) / _zoom * 0.018, vy = (e.Y - _createStart.Y) / _zoom * 0.018; try { AddBody("Body " + (_bodies.Count + 1), 10, _createWorldX, _createWorldY, vx, vy, 7, ZenTheme.Primary); _status = "New body created"; } catch (InvalidOperationException) { _status = "Bodys limit"; } Invalidate(); }
        }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); double old = _zoom; _zoom = Math.Max(0.05, Math.Min(12, _zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15))); PointF before = ScreenToWorld(e.Location); _cameraX += before.X - _cameraX - (e.X - ClientSize.Width / 2.0) / _zoom; _cameraY += before.Y - _cameraY - (e.Y - ClientSize.Height / 2.0) / _zoom; Invalidate(); }
        private int HitBody(Point p) { for (int i = _bodies.Count - 1; i >= 0; i--) { PointF s = WorldToScreen(_bodies[i].X, _bodies[i].Y); double r = Math.Max(8, _bodies[i].Radius * _zoom + 4), dx = s.X - p.X, dy = s.Y - p.Y; if (dx * dx + dy * dy <= r * r) return i; } return -1; }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e); if (e.KeyCode == Keys.Space) { TogglePause(); e.Handled = true; } else if (e.KeyCode == Keys.Right) { StepOnce(); e.Handled = true; } else if (e.KeyCode == Keys.F) { FrameAll(); e.Handled = true; } else if (e.KeyCode == Keys.Delete && _selected >= 0) { _bodies.RemoveAt(_selected); _selected = Math.Min(_selected, _bodies.Count - 1); RaiseSelectionChanged(); RaiseUpdated(); Invalidate(); e.Handled = true; }
        }
        protected override void Dispose(bool disposing) { if (disposing && _timer != null) { _timer.Stop(); _timer.Tick -= TimerTick; _timer.Dispose(); } base.Dispose(disposing); }
    }
}
//watashi wa watashi sore dake