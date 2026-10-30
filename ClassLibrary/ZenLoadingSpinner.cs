using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Controls")]
    [Description("Animated circular loading indicator.")]
    public class ZenLoadingSpinner : Control
    {
        private readonly Timer timer; private int angle; private Color spinnerColor = ZenTheme.Primary; private int speed = 35; private bool spinning = true;
        [Category("ZenForms Appearance")] public Color SpinnerColor { get { return spinnerColor; } set { spinnerColor = value; Invalidate(); } }
        [Category("ZenForms Animation"), DefaultValue(35)] public int Speed { get { return speed; } set { speed = Math.Max(10, Math.Min(500, value)); timer.Interval = speed; } }
        [Category("ZenForms Animation"), DefaultValue(true)] public bool Spinning { get { return spinning; } set { spinning = value; if (value) timer.Start(); else timer.Stop(); Invalidate(); } }
        public ZenLoadingSpinner() { Size = new Size(48, 48); MinimumSize = new Size(16, 16); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); timer = new Timer { Interval = speed }; timer.Tick += (s, e) => { angle = (angle + 12) % 360; Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible && spinning) timer.Start(); else timer.Stop(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; int inset = Math.Max(3, Math.Min(Width, Height) / 9); Rectangle r = new Rectangle(inset, inset, Math.Max(1, Width - 2 * inset), Math.Max(1, Height - 2 * inset)); using (Pen pen = new Pen(Color.FromArgb(50, spinnerColor), Math.Max(2, Math.Min(Width, Height) / 10))) e.Graphics.DrawArc(pen, r, 0, 360); using (Pen pen = new Pen(spinnerColor, Math.Max(2, Math.Min(Width, Height) / 10))) { pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; e.Graphics.DrawArc(pen, r, angle, 95); } }
    }
}
// watashi wa watashi sore dake