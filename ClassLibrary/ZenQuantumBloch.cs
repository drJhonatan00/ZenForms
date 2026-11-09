using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenQuantumGate { H, X, Y, Z, S, T }

    public struct ZenComplex
    {
        public double Real { get; private set; }
        public double Imaginary { get; private set; }
        public ZenComplex(double real, double imaginary) { Real = real; Imaginary = imaginary; }
        internal double MagnitudeSquared { get { return Real * Real + Imaginary * Imaginary; } }
        internal ZenComplex Multiply(double r, double i) { return new ZenComplex(Real * r - Imaginary * i, Real * i + Imaginary * r); }
        internal static ZenComplex Add(ZenComplex a, ZenComplex b) { return new ZenComplex(a.Real + b.Real, a.Imaginary + b.Imaginary); }
        internal static ZenComplex Scale(ZenComplex a, double k) { return new ZenComplex(a.Real * k, a.Imaginary * k); }
    }

    public sealed class ZenQuantumMeasurementEventArgs : EventArgs
    {
        public int Outcome { get; private set; }
        public double Probability { get; private set; }
        internal ZenQuantumMeasurementEventArgs(int outcome, double probability) { Outcome = outcome; Probability = probability; }
    }


    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("One qubit viewer.")]
    public class ZenQuantumBloch : UserControl
    {
        private ZenComplex alpha = new ZenComplex(1, 0), beta = new ZenComplex(0, 0);
        private double viewYaw = -0.55, viewPitch = 0.35;
        private bool dragging;
        private Point lastMouse;
        private int randomSeed = 12345;
        private Random random = new Random(12345);
        private string status = "Initial State |0>";
        private const double Epsilon = 1e-10;
        private Rectangle gateH, gateX, gateY, gateZ, gateS, gateT, resetRect, measureRect;

        public event EventHandler StateChanged;
        public event EventHandler<ZenQuantumMeasurementEventArgs> MeasurementCompleted;
        public ZenQuantumBloch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(300, 300); Size = new Size(590, 500); TabStop = true;
        }

        [Category("State"), Description("|0>.")]
        public ZenComplex Alpha { get { return alpha; } }
        [Category("State"), Description("|1>.")]
        public ZenComplex Beta { get { return beta; } }
        [Category("State"), Description("|0>.")]
        public double ProbabilityZero { get { return Clamp01(alpha.MagnitudeSquared); } }
        [Category("State"), Description("|1>.")]
        public double ProbabilityOne { get { return Clamp01(beta.MagnitudeSquared); } }
        [Category("State"), Description("Theta in radians, between 0 and pi.")]
        public double Theta { get { return Math.Acos(Clamp(-1, 1, ProbabilityZero - ProbabilityOne)); } }
        [Category("State"), Description("[-pi, pi].")]
        public double Phi { get { if (ProbabilityZero * ProbabilityOne < Epsilon) return 0; double p = Math.Atan2(beta.Imaginary, beta.Real) - Math.Atan2(alpha.Imaginary, alpha.Real); while (p > Math.PI) p -= 2 * Math.PI; while (p < -Math.PI) p += 2 * Math.PI; return p; } }
        [Category("State")]
        public double RelativePhase { get { return Phi; } }
        [Category("Behavior")]
        public int RandomSeed { get { return randomSeed; } set { randomSeed = value; random = new Random(value); } }

        public void SetState(ZenComplex newAlpha, ZenComplex newBeta)
        {
            if (InvokeRequired) { Invoke((MethodInvoker)delegate { SetState(newAlpha, newBeta); }); return; }
            double norm = newAlpha.MagnitudeSquared + newBeta.MagnitudeSquared;
            if (Double.IsNaN(norm) || Double.IsInfinity(norm) || Math.Abs(norm - 1.0) > 1e-7)
                throw new ArgumentException("Normal state example: |alpha|² + |beta|² = 1.");
            alpha = newAlpha; beta = newBeta; status = "Defined state"; Changed();
        }

        public void SetState(double alphaReal, double alphaImaginary, double betaReal, double betaImaginary)
        { SetState(new ZenComplex(alphaReal, alphaImaginary), new ZenComplex(betaReal, betaImaginary)); }

        public void ApplyGate(ZenQuantumGate gate)
        {
            if (InvokeRequired) { Invoke((MethodInvoker)delegate { ApplyGate(gate); }); return; }
            ZenComplex a = alpha, b = beta, na, nb;
            switch (gate)
            {
                case ZenQuantumGate.H: na = ZenComplex.Scale(ZenComplex.Add(a, b), 1 / Math.Sqrt(2)); nb = ZenComplex.Scale(ZenComplex.Add(a, ZenComplex.Scale(b, -1)), 1 / Math.Sqrt(2)); break;
                case ZenQuantumGate.X: na = b; nb = a; break;
                case ZenQuantumGate.Y: na = new ZenComplex(b.Imaginary, -b.Real); nb = new ZenComplex(-a.Imaginary, a.Real); break;
                case ZenQuantumGate.Z: na = a; nb = ZenComplex.Scale(b, -1); break;
                case ZenQuantumGate.S: na = a; nb = b.Multiply(0, 1); break;
                case ZenQuantumGate.T: double q = Math.Sqrt(0.5); na = a; nb = b.Multiply(q, q); break;
                default: throw new ArgumentOutOfRangeException("gate", "Unsupported port.");
            }
            alpha = na; beta = nb; status = "Applied port " + gate; Changed();
        }

        public void ResetState() { if (InvokeRequired) { Invoke((MethodInvoker)delegate { ResetState(); }); return; } alpha = new ZenComplex(1, 0); beta = new ZenComplex(0, 0); status = "State reseted to |0>"; Changed(); }

        public int Measure()
        {
            if (InvokeRequired) return (int)Invoke(new Func<int>(delegate { return Measure(); }));
            double p0 = ProbabilityZero; int outcome = random.NextDouble() < p0 ? 0 : 1;
            double probability = outcome == 0 ? p0 : ProbabilityOne;
            alpha = outcome == 0 ? new ZenComplex(1, 0) : new ZenComplex(0, 0);
            beta = outcome == 1 ? new ZenComplex(1, 0) : new ZenComplex(0, 0);
            status = "Medition: " + outcome + " (" + (probability * 100).ToString("0.0") + "%)";
            Changed(); EventHandler<ZenQuantumMeasurementEventArgs> handler = MeasurementCompleted;
            if (handler != null) handler(this, new ZenQuantumMeasurementEventArgs(outcome, probability));
            return outcome;
        }

        private void Changed() { Invalidate(); EventHandler handler = StateChanged; if (handler != null) handler(this, EventArgs.Empty); }
        private static double Clamp01(double v) { return Clamp(0, 1, v); }
        private static double Clamp(double min, double max, double v) { return Math.Max(min, Math.Min(max, v)); }
        private static Color Blend(Color a, Color b, double t) { t = Clamp01(t); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (SolidBrush bg = new SolidBrush(ZenTheme.Background)) g.FillRectangle(bg, ClientRectangle);
            int pad = 14; Rectangle area = new Rectangle(pad, pad, Math.Max(1, Width - pad * 2), Math.Max(1, Height - pad * 2));
            using (GraphicsPath path = Rounded(area, 14))
            using (LinearGradientBrush panel = new LinearGradientBrush(area, ZenTheme.Surface, Blend(ZenTheme.Surface, Color.Black, .38), 35f))
            using (Pen border = new Pen(ZenTheme.Border, 1)) { g.FillPath(panel, path); g.DrawPath(border, path); }
            int headerY = area.Top + 14;
            using (Font title = new Font(Font.FontFamily, 12f, FontStyle.Bold)) using (SolidBrush b = new SolidBrush(ZenTheme.Foreground)) g.DrawString("QUBIT  /  BLOCH SPHERE", title, b, area.Left + 16, headerY);
            using (SolidBrush b = new SolidBrush(ZenTheme.Muted)) g.DrawString("SIMULATOR · 1 QUBIT", Font, b, area.Left + 17, headerY + 23);
            int footerH = 139, top = headerY + 49, bottom = area.Bottom - footerH;
            int diameter = Math.Max(100, Math.Min(area.Width - 38, bottom - top - 8));
            float radius = Math.Min(diameter / 2f, Math.Max(40, Math.Min(area.Width, bottom - top) * .39f));
            PointF center = new PointF(area.Left + area.Width * .50f, top + Math.Max(2, (bottom - top) / 2f));
            DrawSphere(g, center, radius);
            int infoY = area.Top + area.Height - footerH + 9;
            DrawState(g, area, infoY);
            DrawButtons(g, area, area.Bottom - 41);
        }

        private void DrawSphere(Graphics g, PointF c, float r)
        {
            using (GraphicsPath glowPath = new GraphicsPath())
            { glowPath.AddEllipse(c.X - r - 10, c.Y - r - 10, (r + 10) * 2, (r + 10) * 2); using (PathGradientBrush glow = new PathGradientBrush(glowPath)) { glow.CenterColor = Color.FromArgb(45, ZenTheme.Primary); glow.SurroundColors = new Color[] { Color.FromArgb(0, ZenTheme.Background) }; g.FillEllipse(glow, c.X - r - 10, c.Y - r - 10, (r + 10) * 2, (r + 10) * 2); } }
            using (LinearGradientBrush sphere = new LinearGradientBrush(new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r), Color.FromArgb(58, ZenTheme.Primary), ZenTheme.Surface, 35f))
            using (Pen rim = new Pen(Color.FromArgb(160, ZenTheme.Primary), 1.5f)) { g.FillEllipse(sphere, c.X - r, c.Y - r, 2 * r, 2 * r); g.DrawEllipse(rim, c.X - r, c.Y - r, 2 * r, 2 * r); }
            for (int lat = -60; lat <= 60; lat += 30)
            {
                float rr = r * (float)Math.Cos(lat * Math.PI / 180.0), yy = r * (float)Math.Sin(lat * Math.PI / 180.0);
                using (Pen p = new Pen(Color.FromArgb(lat == 0 ? 105 : 55, ZenTheme.Accent), lat == 0 ? 1.15f : .8f))
                { g.DrawEllipse(p, c.X - rr, c.Y - yy - rr * .25f, 2 * rr, .5f * rr); }
            }
            for (int mer = 0; mer < 4; mer++)
            {
                float width = (mer % 2 == 0) ? .46f : .72f;
                using (Pen p = new Pen(Color.FromArgb(78, ZenTheme.Primary), .85f)) g.DrawEllipse(p, c.X - r * width, c.Y - r, 2 * r * width, 2 * r);
            }
            PointF ox = Project(1, 0, 0, c, r), oy = Project(0, 1, 0, c, r), oz = Project(0, 0, 1, c, r);
            DrawAxis(g, c, ox, ZenTheme.Accent, "X"); DrawAxis(g, c, oy, Color.FromArgb(190, 170, 255), "Y"); DrawAxis(g, c, oz, Color.FromArgb(113, 182, 255), "Z");
            DrawKet(g, Project(0, 0, 1, c, r), "|0>", true); DrawKet(g, Project(0, 0, -1, c, r), "|1>", false);
            double bx = 2 * (alpha.Real * beta.Real + alpha.Imaginary * beta.Imaginary), by = 2 * (alpha.Real * beta.Imaginary - alpha.Imaginary * beta.Real), bz = ProbabilityZero - ProbabilityOne;
            PointF tip = Project(bx, by, bz, c, r);
            using (Pen vector = new Pen(Color.FromArgb(95, ZenTheme.Primary), 10)) { vector.StartCap = LineCap.Round; vector.EndCap = LineCap.Round; g.DrawLine(vector, c, tip); }
            using (Pen vector = new Pen(Color.FromArgb(230, ZenTheme.Primary), 3)) { vector.StartCap = LineCap.Round; vector.EndCap = LineCap.ArrowAnchor; g.DrawLine(vector, c, tip); }
            using (SolidBrush dot = new SolidBrush(Color.White)) g.FillEllipse(dot, tip.X - 4, tip.Y - 4, 8, 8);
            using (Pen centerPen = new Pen(ZenTheme.Foreground, 1)) g.DrawEllipse(centerPen, c.X - 2, c.Y - 2, 4, 4);
        }

        private PointF Project(double x, double y, double z, PointF c, float r)
        {
            double cy = Math.Cos(viewYaw), sy = Math.Sin(viewYaw), cp = Math.Cos(viewPitch), sp = Math.Sin(viewPitch);
            double xr = x * cy - y * sy, yr = x * sy + y * cy, yr2 = yr * cp - z * sp, zr = yr * sp + z * cp;
            return new PointF((float)(c.X + r * xr * .88), (float)(c.Y - r * yr2 * .88));
        }
        private void DrawAxis(Graphics g, PointF c, PointF end, Color color, string label)
        { using (Pen p = new Pen(Color.FromArgb(135, color), 1.1f)) { p.DashPattern = new float[] { 3, 3 }; g.DrawLine(p, c, end); } using (SolidBrush b = new SolidBrush(color)) using (Font f = new Font(Font.FontFamily, 8f, FontStyle.Bold)) g.DrawString(label, f, b, end.X - 4, end.Y - 15); }
        private void DrawKet(Graphics g, PointF p, string label, bool top)
        { using (Font f = new Font(Font.FontFamily, 8.5f, FontStyle.Bold)) using (SolidBrush b = new SolidBrush(ZenTheme.Foreground)) g.DrawString(label, f, b, p.X + 5, p.Y + (top ? 4 : -16)); }

        private void DrawState(Graphics g, Rectangle area, int y)
        {
            using (Pen line = new Pen(ZenTheme.Border, 1)) g.DrawLine(line, area.Left + 14, y, area.Right - 14, y);
            int x = area.Left + 17; string a = "|α|² " + ProbabilityZero.ToString("0.000") + "   |β|² " + ProbabilityOne.ToString("0.000");
            using (Font f = new Font(Font.FontFamily, 9f, FontStyle.Bold)) using (SolidBrush b = new SolidBrush(ZenTheme.Foreground)) g.DrawString(a, f, b, x, y + 7);
            string angles = "θ " + Theta.ToString("0.000") + " rad   φ " + Phi.ToString("0.000") + " rad   RELATIVE " + RelativePhase.ToString("0.000") + " rad";
            using (SolidBrush b = new SolidBrush(ZenTheme.Muted)) g.DrawString(angles, Font, b, x, y + 27);
            string amplitudes = "α = " + Format(alpha) + "     β = " + Format(beta);
            using (SolidBrush b = new SolidBrush(ZenTheme.Muted)) g.DrawString(amplitudes, Font, b, x, y + 46);
            using (SolidBrush b = new SolidBrush(ZenTheme.Accent)) g.DrawString(status, Font, b, x, y + 65);
            using (SolidBrush b = new SolidBrush(ZenTheme.Muted)) g.DrawString("", Font, b, x, y + 84);
        }
        private static string Format(ZenComplex n) { return "(" + n.Real.ToString("0.000") + (n.Imaginary < 0 ? " − " : " + ") + Math.Abs(n.Imaginary).ToString("0.000") + "i)"; }
        private void DrawButtons(Graphics g, Rectangle area, int y)
        {
            int x = area.Left + 15, gap = 5, h = 26, w = Math.Max(34, Math.Min(45, (area.Width - 190) / 6));
            gateH = Button(g, "H", x, y, w, h); x += w + gap; gateX = Button(g, "X", x, y, w, h); x += w + gap; gateY = Button(g, "Y", x, y, w, h); x += w + gap; gateZ = Button(g, "Z", x, y, w, h); x += w + gap; gateS = Button(g, "S", x, y, w, h); x += w + gap; gateT = Button(g, "T", x, y, w, h); x += w + gap + 5;
            resetRect = Button(g, "Reset", x, y, 58, h); x += 63; measureRect = Button(g, "Measure", x, y, 64, h);
        }
        private Rectangle Button(Graphics g, string text, int x, int y, int w, int h)
        { Rectangle r = new Rectangle(x, y, w, h); using (GraphicsPath p = Rounded(r, 6)) using (SolidBrush b = new SolidBrush(Color.FromArgb(42, ZenTheme.Primary))) using (Pen pen = new Pen(Color.FromArgb(115, ZenTheme.Primary))) { g.FillPath(b, p); g.DrawPath(pen, p); } using (Font f = new Font(Font.FontFamily, 8f, FontStyle.Bold)) using (SolidBrush b = new SolidBrush(ZenTheme.Foreground)) { StringFormat sf = new StringFormat(); sf.Alignment = StringAlignment.Center; sf.LineAlignment = StringAlignment.Center; g.DrawString(text, f, b, r, sf); sf.Dispose(); } return r; }
        private static GraphicsPath Rounded(Rectangle r, int radius)
        { GraphicsPath p = new GraphicsPath(); int d = Math.Max(2, radius * 2); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }

        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); Focus(); if (e.Button == MouseButtons.Left) { if (gateH.Contains(e.Location)) ApplyGate(ZenQuantumGate.H); else if (gateX.Contains(e.Location)) ApplyGate(ZenQuantumGate.X); else if (gateY.Contains(e.Location)) ApplyGate(ZenQuantumGate.Y); else if (gateZ.Contains(e.Location)) ApplyGate(ZenQuantumGate.Z); else if (gateS.Contains(e.Location)) ApplyGate(ZenQuantumGate.S); else if (gateT.Contains(e.Location)) ApplyGate(ZenQuantumGate.T); else if (resetRect.Contains(e.Location)) ResetState(); else if (measureRect.Contains(e.Location)) Measure(); else { dragging = true; lastMouse = e.Location; Capture = true; } } }
        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); if (dragging) { viewYaw += (e.X - lastMouse.X) * .012; viewPitch = Clamp(-1.35, 1.35, viewPitch + (e.Y - lastMouse.Y) * .012); lastMouse = e.Location; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (e.Button == MouseButtons.Left) { dragging = false; Capture = false; } }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
        protected override void OnKeyDown(KeyEventArgs e)
        { base.OnKeyDown(e); switch (e.KeyCode) { case Keys.H: ApplyGate(ZenQuantumGate.H); break; case Keys.X: ApplyGate(ZenQuantumGate.X); break; case Keys.Y: ApplyGate(ZenQuantumGate.Y); break; case Keys.Z: ApplyGate(ZenQuantumGate.Z); break; case Keys.S: ApplyGate(ZenQuantumGate.S); break; case Keys.T: ApplyGate(ZenQuantumGate.T); break; case Keys.R: ResetState(); break; case Keys.M: Measure(); break; default: return; } e.Handled = true; }
    }
}

// watashi wa watashi sore dake