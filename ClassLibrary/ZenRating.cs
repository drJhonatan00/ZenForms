using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Controls")]
    [Description("Interactive star-based rating control.")]
    public class ZenRating : Control
    {
        private int value; private int hoverValue; private int maximum = 5; private Color activeColor = Color.FromArgb(255, 190, 70); private Color inactiveColor = ZenTheme.SurfaceRaised;
        [Category("ZenForms Data"), DefaultValue(0)] public int Value { get { return value; } set { int v = Math.Max(0, Math.Min(maximum, value)); if (this.value == v) return; this.value = v; Invalidate(); EventHandler h = ValueChanged; if (h != null) h(this, EventArgs.Empty); } }
        [Category("ZenForms Data"), DefaultValue(5)] public int Maximum { get { return maximum; } set { maximum = Math.Max(1, Math.Min(20, value)); this.value = Math.Min(this.value, maximum); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color ActiveColor { get { return activeColor; } set { activeColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color InactiveColor { get { return inactiveColor; } set { inactiveColor = value; Invalidate(); } }
        public event EventHandler ValueChanged;
        public ZenRating() { Size = new Size(150, 32); MinimumSize = new Size(40, 24); Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); TabStop = true; }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); hoverValue = Math.Max(0, Math.Min(maximum, (e.X * maximum / Math.Max(1, ClientSize.Width)) + 1)); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hoverValue = 0; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); Value = Math.Max(1, Math.Min(maximum, (e.X * maximum / Math.Max(1, ClientSize.Width)) + 1)); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Right) Value++; else if (e.KeyCode == Keys.Left) Value--; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; int active = hoverValue > 0 ? hoverValue : value; float cell = (float)Width / maximum; for (int i = 0; i < maximum; i++) { PointF[] pts = Star(cell * i + cell / 2f, Height / 2f, Math.Min(cell, Height) * .44f, Math.Min(cell, Height) * .20f); using (SolidBrush b = new SolidBrush(i < active ? activeColor : inactiveColor)) e.Graphics.FillPolygon(b, pts); } }
        private static PointF[] Star(float cx, float cy, float outer, float inner) { PointF[] p = new PointF[10]; for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5; float r = i % 2 == 0 ? outer : inner; p[i] = new PointF(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r); } return p; }
    }
}

//Watashi wa watashi sore dake