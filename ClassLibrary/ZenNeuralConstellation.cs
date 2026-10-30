using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Procedural, animated, and mouse-reactive neural web.")]
    public class ZenNeuralConstellation : Control
    {
        private readonly Timer timer; private PointF[] nodes; private Point mouse; private bool mouseInside; private int nodeCount = 42; private float phase; private Color nodeColor = ZenTheme.Accent;
        [Category("ZenForms Animation"), DefaultValue(42)] public int NodeCount { get { return nodeCount; } set { nodeCount = Math.Max(8, Math.Min(100, value)); CreateNodes(); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color NodeColor { get { return nodeColor; } set { nodeColor = value; Invalidate(); } }
        [Category("ZenForms Appearance"), DefaultValue(125)] public int LinkDistance { get; set; } = 125;
        public ZenNeuralConstellation() { Size = new Size(400, 260); BackColor = Color.FromArgb(15, 19, 34); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); CreateNodes(); timer = new Timer { Interval = 33 }; timer.Tick += (s, e) => { phase += .016f; Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); CreateNodes(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible) timer.Start(); else timer.Stop(); }
        private void CreateNodes() { if (Width <= 0 || Height <= 0) return; Random r = new Random(719 + Width * 31 + Height); nodes = new PointF[nodeCount]; for (int i = 0; i < nodes.Length; i++) nodes[i] = new PointF(r.Next(Math.Max(1, Width)), r.Next(Math.Max(1, Height))); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); mouseInside = true; }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouseInside = false; Invalidate(); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); mouse = e.Location; mouseInside = true; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); if (nodes == null || nodes.Length == 0) return; Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor); float[] x = new float[nodes.Length], y = new float[nodes.Length]; for (int i = 0; i < nodes.Length; i++) { x[i] = nodes[i].X + (float)Math.Sin(phase + i * .73f) * 8; y[i] = nodes[i].Y + (float)Math.Cos(phase * .83f + i * .51f) * 8; if (mouseInside) { float dx = x[i] - mouse.X, dy = y[i] - mouse.Y, dist = (float)Math.Sqrt(dx * dx + dy * dy); if (dist < 105 && dist > 0) { x[i] += dx / dist * (105 - dist) * .22f; y[i] += dy / dist * (105 - dist) * .22f; } } } for (int i = 0; i < nodes.Length; i++) for (int j = i + 1; j < nodes.Length; j++) { float dx = x[i] - x[j], dy = y[i] - y[j], d = (float)Math.Sqrt(dx * dx + dy * dy); if (d < LinkDistance) { int alpha = (int)(68 * (1 - d / LinkDistance)); using (Pen p = new Pen(Color.FromArgb(Math.Max(0, alpha), nodeColor), 1f)) g.DrawLine(p, x[i], y[i], x[j], y[j]); } } for (int i = 0; i < nodes.Length; i++) { float pulse = 2.5f + (float)Math.Sin(phase * 2 + i) * 1.2f; using (SolidBrush glow = new SolidBrush(Color.FromArgb(28, nodeColor))) g.FillEllipse(glow, x[i] - pulse * 3, y[i] - pulse * 3, pulse * 6, pulse * 6); using (SolidBrush dot = new SolidBrush(nodeColor)) g.FillEllipse(dot, x[i] - pulse, y[i] - pulse, pulse * 2, pulse * 2); } }
    }
}

//watashi wa watshi sore dake