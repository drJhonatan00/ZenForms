using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Precision analog clock with a neon dial.")]
    public class ZenAnalogClock : Control
    {
        private readonly Timer timer; private Color dialColor = ZenTheme.Primary; private Color faceColor = ZenTheme.Surface; private string timeZoneId = String.Empty; private bool showDigital = true; private DateTime displayed = DateTime.Now;
        [Category("ZenForms Appearance")] public Color DialColor { get { return dialColor; } set { dialColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color FaceColor { get { return faceColor; } set { faceColor = value; Invalidate(); } }
        [Category("ZenForms Time"), DefaultValue("")] public string TimeZoneId { get { return timeZoneId; } set { timeZoneId = value ?? String.Empty; UpdateTime(); Invalidate(); } }
        [Category("ZenForms Time"), DefaultValue(true)] public bool ShowDigital { get { return showDigital; } set { showDigital = value; Invalidate(); } }
        public DateTime DisplayedTime { get { return displayed; } }
        public ZenAnalogClock() { Size = new Size(190, 210); MinimumSize = new Size(100, 110); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); timer = new Timer { Interval = 250 }; timer.Tick += (s, e) => { UpdateTime(); Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible) timer.Start(); else timer.Stop(); }
        private void UpdateTime() { try { displayed = String.IsNullOrWhiteSpace(timeZoneId) ? DateTime.Now : TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); } catch { displayed = DateTime.Now; } }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; int d = Math.Max(20, Math.Min(Width - 16, Height - (showDigital ? 52 : 16))); Rectangle r = new Rectangle((Width - d) / 2, 8, d, d); using (SolidBrush face = new SolidBrush(faceColor)) g.FillEllipse(face, r); using (Pen halo = new Pen(Color.FromArgb(26, dialColor), 8)) g.DrawEllipse(halo, r); using (Pen rim = new Pen(Color.FromArgb(170, dialColor), 1.5f)) g.DrawEllipse(rim, r); float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f, rad = r.Width * .42f; using (Font numberFont = new Font("Segoe UI", Math.Max(7f, r.Width * .055f), FontStyle.Bold)) for (int i = 0; i < 60; i++) { double a = i * Math.PI / 30 - Math.PI / 2; float inner = rad * (i % 5 == 0 ? .84f : .94f); using (Pen p = new Pen(i % 5 == 0 ? ZenTheme.Foreground : Color.FromArgb(95, ZenTheme.Muted), i % 5 == 0 ? 2f : 1f)) g.DrawLine(p, cx + (float)Math.Cos(a) * inner, cy + (float)Math.Sin(a) * inner, cx + (float)Math.Cos(a) * rad, cy + (float)Math.Sin(a) * rad); if (i % 5 == 0) { string label = (i == 0 ? 12 : i / 5).ToString(); PointF pos = new PointF(cx + (float)Math.Cos(a) * rad * .72f, cy + (float)Math.Sin(a) * rad * .72f); TextRenderer.DrawText(g, label, numberFont, new Rectangle((int)pos.X - 12, (int)pos.Y - 10, 24, 20), ZenTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } } double sec = displayed.Second / 60.0, min = (displayed.Minute + sec) / 60.0, hour = (displayed.Hour % 12 + min) / 12.0; DrawHand(g, cx, cy, rad * .52f, hour * 2 * Math.PI - Math.PI / 2, 4, ZenTheme.Foreground); DrawHand(g, cx, cy, rad * .75f, min * 2 * Math.PI - Math.PI / 2, 2.5f, dialColor); DrawHand(g, cx, cy, rad * .83f, sec * 2 * Math.PI - Math.PI / 2, 1.2f, ZenTheme.Accent); using (SolidBrush hub = new SolidBrush(Color.White)) g.FillEllipse(hub, cx - 4, cy - 4, 8, 8); if (showDigital) TextRenderer.DrawText(g, displayed.ToString("HH:mm:ss"), new Font("Consolas", 12f, FontStyle.Bold), new Rectangle(0, r.Bottom + 7, Width, 22), ZenTheme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
        private static void DrawHand(Graphics g, float cx, float cy, float len, double angle, float width, Color color) { using (Pen p = new Pen(color, width)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawLine(p, cx, cy, cx + (float)Math.Cos(angle) * len, cy + (float)Math.Sin(angle) * len); } }
    }
}

// watashi wa watashi sore dake
