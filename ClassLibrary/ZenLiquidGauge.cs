using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Circular gauge with animated liquid waves.")]
    public class ZenLiquidGauge : Control
    {
        private readonly Timer timer; private int value = 65; private float phase; private Color liquid = ZenTheme.Primary; private Color glass = ZenTheme.SurfaceRaised; private bool animate = true;
        [Category("ZenForms Data"), DefaultValue(65)] public int Value { get { return value; } set { int v = Math.Max(0, Math.Min(100, value)); if (this.value != v) { this.value = v; Invalidate(); } } }
        [Category("ZenForms Appearance")] public Color LiquidColor { get { return liquid; } set { liquid = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color GlassColor { get { return glass; } set { glass = value; Invalidate(); } }
        [Category("ZenForms Animation"), DefaultValue(true)] public bool Animate { get { return animate; } set { animate = value; if (value) timer.Start(); else timer.Stop(); } }
        public ZenLiquidGauge() { Size = new Size(150, 150); MinimumSize = new Size(60, 60); Font = new Font("Segoe UI Semibold", 18f); ForeColor = ZenTheme.Foreground; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); timer = new Timer { Interval = 45 }; timer.Tick += (s, e) => { phase += 0.11f; Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible && animate) timer.Start(); else timer.Stop(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; int d = Math.Max(1, Math.Min(Width, Height) - 12); Rectangle r = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d); using (GraphicsPath clip = new GraphicsPath()) { clip.AddEllipse(r); GraphicsState state = g.Save(); g.SetClip(clip); using (SolidBrush bg = new SolidBrush(glass)) g.FillEllipse(bg, r); float level = r.Bottom - r.Height * value / 100f; PointF[] points = new PointF[42]; points[0] = new PointF(r.Left, level); for (int i = 0; i <= 39; i++) { float x = r.Left + r.Width * i / 39f; float y = level + (float)Math.Sin(i * .42f + phase) * r.Height * .025f; points[i + 1] = new PointF(x, y); } points[41] = new PointF(r.Right, r.Bottom); using (GraphicsPath wave = new GraphicsPath()) { wave.AddPolygon(points); using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(225, liquid), Color.FromArgb(155, Color.FromArgb(Math.Max(0, liquid.R - 35), Math.Max(0, liquid.G - 25), Math.Max(0, liquid.B - 10))), 90f)) g.FillPath(b, wave); } g.Restore(state); } using (Pen edge = new Pen(Color.FromArgb(170, ZenTheme.Accent), 2f)) g.DrawEllipse(edge, r); using (SolidBrush shine = new SolidBrush(Color.FromArgb(24, Color.White))) g.FillEllipse(shine, r.Left + r.Width * .15f, r.Top + r.Height * .12f, r.Width * .7f, r.Height * .2f); TextRenderer.DrawText(g, value + "%", Font, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
    }
}

//watashi wa watashi sore dake