using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Modern card with animations that follow the mouse.")]
    public class ZenHoloCard : Control
    {
        private float tiltX, tiltY; private bool hovering; private Color glow = ZenTheme.Primary; private string subtitle = "Hover your mouse to reveal the effect"; private int radius = 18;
        [Category("ZenForms Appearance")] public Color GlowColor { get { return glow; } set { glow = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public string Subtitle { get { return subtitle; } set { subtitle = value ?? ""; Invalidate(); } }
        [Category("ZenForms Appearance"), DefaultValue(18)] public int CornerRadius { get { return radius; } set { radius = Math.Max(0, value); Invalidate(); } }
        public ZenHoloCard() { Size = new Size(280, 180); BackColor = ZenTheme.Surface; ForeColor = ZenTheme.Foreground; Font = new Font("Segoe UI Semibold", 16f); Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); hovering = true; tiltX = (e.Y / (float)Math.Max(1, Height) - .5f) * 2; tiltY = (e.X / (float)Math.Max(1, Width) - .5f) * 2; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovering = false; tiltX = tiltY = 0; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = new Rectangle(5, 5, Width - 11, Height - 12); using (GraphicsPath path = ZenButtonBase.RoundPath(r, radius)) { using (LinearGradientBrush bg = new LinearGradientBrush(r, Color.FromArgb(255, ZenTheme.SurfaceRaised), Color.FromArgb(255, ZenTheme.Background), 35f)) g.FillPath(bg, path); if (hovering) { using (Pen glowPen = new Pen(Color.FromArgb(110, glow), 8f)) g.DrawPath(glowPen, path); } using (Pen border = new Pen(Color.FromArgb(hovering ? 210 : 90, glow), 1.2f)) g.DrawPath(border, path); GraphicsState state = g.Save(); g.SetClip(path); float sx = tiltY * 8, sy = -tiltX * 6; using (LinearGradientBrush sheen = new LinearGradientBrush(new Rectangle(r.Left + (int)(tiltY * 15), r.Top, r.Width, r.Height), Color.FromArgb(hovering ? 72 : 20, Color.White), Color.FromArgb(0, Color.White), 45f)) g.FillRectangle(sheen, r); using (SolidBrush orb = new SolidBrush(Color.FromArgb(45, glow))) g.FillEllipse(orb, r.Right - 85 + sx, r.Top - 65 + sy, 150, 150); g.Restore(state); TextRenderer.DrawText(g, Text, Font, new Rectangle(r.Left + 22, r.Top + 29, r.Width - 44, 38), ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); using (Font subtitleFont = new Font("Segoe UI", 9f)) TextRenderer.DrawText(g, subtitle, subtitleFont, new Rectangle(r.Left + 23, r.Top + 76, r.Width - 46, 42), ZenTheme.Muted, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak); using (Pen p = new Pen(Color.FromArgb(hovering ? 210 : 90, glow), 2f)) g.DrawLine(p, r.Left + 22, r.Bottom - 20, r.Left + Math.Max(24, r.Width / 3), r.Bottom - 20); } }
    }
}
// watashi wa watashi sore dake