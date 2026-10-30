using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Controls")]
    [Description("Circular progress indicator with a percentage in the center.")]
    public class ZenCircularProgress : Control
    {
        private int value; private Color progressColor = ZenTheme.Accent; private Color trackColor = ZenTheme.SurfaceRaised; private int thickness = 9;
        [Category("ZenForms Data"), DefaultValue(0)] public int Value { get { return value; } set { int v = Math.Max(0, Math.Min(100, value)); if (this.value == v) return; this.value = v; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color ProgressColor { get { return progressColor; } set { progressColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color TrackColor { get { return trackColor; } set { trackColor = value; Invalidate(); } }
        [Category("ZenForms Appearance"), DefaultValue(9)] public int Thickness { get { return thickness; } set { thickness = Math.Max(1, value); Invalidate(); } }
        [DefaultValue(true)] public bool ShowPercentage { get; set; } = true;
        public ZenCircularProgress() { Size = new Size(100, 100); MinimumSize = new Size(40, 40); Font = new Font("Segoe UI Semibold", 14f); ForeColor = ZenTheme.Foreground; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; int inset = thickness + 2; Rectangle r = new Rectangle(inset, inset, Math.Max(1, Width - 2 * inset), Math.Max(1, Height - 2 * inset)); using (Pen bg = new Pen(trackColor, thickness)) { bg.StartCap = LineCap.Round; bg.EndCap = LineCap.Round; e.Graphics.DrawArc(bg, r, -90, 359.9f); } if (value > 0) using (Pen fg = new Pen(progressColor, thickness)) { fg.StartCap = LineCap.Round; fg.EndCap = LineCap.Round; e.Graphics.DrawArc(fg, r, -90, Math.Max(0.1f, 360f * value / 100f)); } if (ShowPercentage) TextRenderer.DrawText(e.Graphics, value + "%", Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
    }
}

//watashi wa watashi sore dake