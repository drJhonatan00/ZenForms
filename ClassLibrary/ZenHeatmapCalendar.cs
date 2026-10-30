using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Annual heatmap calendar for visualizing activity.")]
    public class ZenHeatmapCalendar : Control
    {
        private readonly Dictionary<DateTime, int> values = new Dictionary<DateTime, int>(); private readonly ToolTip tip; private DateTime year = new DateTime(DateTime.Today.Year, 1, 1); private Color low = Color.FromArgb(31, 43, 60); private Color high = ZenTheme.Accent; private int cellSize = 12; private int gap = 3;
        [Category("ZenForms Heatmap")] public DateTime Year { get { return year; } set { year = new DateTime(value.Year, 1, 1); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color LowColor { get { return low; } set { low = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color HighColor { get { return high; } set { high = value; Invalidate(); } }
        [Category("ZenForms Appearance"), DefaultValue(12)] public int CellSize { get { return cellSize; } set { cellSize = Math.Max(5, Math.Min(24, value)); Invalidate(); } }
        public ZenHeatmapCalendar() { Size = new Size(760, 150); MinimumSize = new Size(260, 100); BackColor = ZenTheme.Surface; ForeColor = ZenTheme.Foreground; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); tip = new ToolTip(); Disposed += (s, e) => tip.Dispose(); }
        public void SetValue(DateTime date, int intensity) { values[date.Date] = Math.Max(0, Math.Min(4, intensity)); Invalidate(); }
        public int GetValue(DateTime date) { int v; return values.TryGetValue(date.Date, out v) ? v : 0; }
        public void Clear() { values.Clear(); Invalidate(); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); DateTime? date = DateAt(e.Location); tip.SetToolTip(this, date.HasValue ? date.Value.ToString("ddd, dd MMM yyyy", CultureInfo.CurrentCulture) + " — level " + GetValue(date.Value) : String.Empty); }
        private DateTime? DateAt(Point p) { int x0 = 42, y0 = 30, step = cellSize + gap; int col = (p.X - x0) / step, row = (p.Y - y0) / step; if (col < 0 || col > 52 || row < 0 || row > 6) return null; DateTime first = year; int offset = ((int)first.DayOfWeek + 6) % 7; DateTime d = first.AddDays(col * 7 + row - offset); if (d.Year != year.Year || col * step + x0 > Width) return null; Rectangle r = new Rectangle(x0 + col * step, y0 + row * step, cellSize, cellSize); return r.Contains(p) ? (DateTime?)d : null; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor); int x0 = 42, y0 = 30, step = cellSize + gap; using (Font f = new Font("Segoe UI", 7.5f)) for (int row = 0; row < 7; row += 2) TextRenderer.DrawText(g, new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"  }[row], f, new Rectangle(2, y0 + row * step, 34, cellSize + 2), ZenTheme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right); DateTime first = year; int offset = ((int)first.DayOfWeek + 6) % 7; int total = DateTime.IsLeapYear(year.Year) ? 366 : 365; for (int day = 0; day < total; day++) { DateTime date = first.AddDays(day); int slot = day + offset, col = slot / 7, row = slot % 7, x = x0 + col * step, y = y0 + row * step; int v = GetValue(date); Color c = v == 0 ? low : Blend(low, high, v / 4f); using (SolidBrush b = new SolidBrush(c)) g.FillRoundedRectangleCompat(b, new Rectangle(x, y, cellSize, cellSize), 3); if (date.Date == DateTime.Today) { using (Pen p = new Pen(Color.White, 1)) g.DrawRectangle(p, x, y, cellSize, cellSize); } } for (int month = 0; month < 12; month++) { DateTime d = new DateTime(year.Year, month + 1, 1); int col = ((d - first).Days + offset) / 7; TextRenderer.DrawText(g, d.ToString("MMM", CultureInfo.CurrentCulture), Font, new Rectangle(x0 + col * step, y0 - 19, 38, 16), ZenTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); } string caption = year.Year.ToString() + " activity"; TextRenderer.DrawText(g, caption, new Font("Segoe UI Semibold", 9f), new Rectangle(Width - 115, Height - 22, 108, 18), ZenTheme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter); }
        private static Color Blend(Color a, Color b, float t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
    }
    internal static class HeatmapGraphicsExtensions { public static void FillRoundedRectangleCompat(this Graphics g, Brush b, Rectangle r, int radius) { using (GraphicsPath p = new GraphicsPath()) { int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height)); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); g.FillPath(b, p); } } }
}
