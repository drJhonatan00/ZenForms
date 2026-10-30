using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenOrbitItemEventArgs : EventArgs { public int Index { get; private set; } public string Item { get; private set; } public ZenOrbitItemEventArgs(int index, string item) { Index = index; Item = item; } }
    [Category("ZenForms Modern Components")]
    [Description("Expandable radial menu with orbiting commands.")]
    public class ZenOrbitMenu : Control
    {
        private readonly List<string> items = new List<string>(); private int hovered = -1; private int selected = -1; private bool expanded = true; private Color orbitColor = ZenTheme.Primary;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)] public List<string> Items { get { return items; } }
        [Category("ZenForms Appearance")] public Color OrbitColor { get { return orbitColor; } set { orbitColor = value; Invalidate(); } }
        [Category("ZenForms Orbit"), DefaultValue(true)] public bool Expanded { get { return expanded; } set { expanded = value; Invalidate(); } }
        public event EventHandler<ZenOrbitItemEventArgs> ItemSelected;
        public void AddItem(string item) { if (String.IsNullOrWhiteSpace(item)) throw new ArgumentException("Enter with item name.", "item"); items.Add(item); Invalidate(); }
        public void ClearItems() { items.Clear(); selected = -1; hovered = -1; Invalidate(); }
        public ZenOrbitMenu() { Size = new Size(250, 250); MinimumSize = new Size(120, 120); BackColor = ZenTheme.Surface; ForeColor = ZenTheme.Foreground; Font = new Font("Segoe UI", 8.5f, FontStyle.Bold); Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); items.AddRange(new string[] { "New", "Open", "Save", "Export", "Config." }); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int old = hovered; hovered = HitTest(e.Location); if (old != hovered) Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = -1; Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); int idx = HitTest(e.Location); if (idx >= 0) { selected = idx; EventHandler<ZenOrbitItemEventArgs> h = ItemSelected; if (h != null) h(this, new ZenOrbitItemEventArgs(idx, items[idx])); } else expanded = !expanded; Invalidate(); }
        private PointF Center { get { return new PointF(Width / 2f, Height / 2f); } }
        private float Radius { get { return Math.Max(24, Math.Min(Width, Height) * .34f); } }
        private PointF Position(int i) { double a = -Math.PI / 2 + i * Math.PI * 2 / Math.Max(1, items.Count); PointF c = Center; return new PointF(c.X + (float)Math.Cos(a) * Radius, c.Y + (float)Math.Sin(a) * Radius); }
        private int HitTest(Point p) { if (!expanded) return -1; for (int i = 0; i < items.Count; i++) { PointF c = Position(i); float dx = p.X - c.X, dy = p.Y - c.Y; if (dx * dx + dy * dy < 29 * 29) return i; } return -1; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; PointF c = Center; float orbit = Radius; using (Pen ring = new Pen(Color.FromArgb(80, orbitColor), 1.2f)) { ring.DashPattern = new float[] { 3, 4 }; g.DrawEllipse(ring, c.X - orbit, c.Y - orbit, 2 * orbit, 2 * orbit); } for (int i = 0; i < items.Count; i++) { PointF p = Position(i); using (Pen link = new Pen(Color.FromArgb(55, orbitColor), 1f)) g.DrawLine(link, c, p); float r = i == hovered ? 29 : 25; Color fill = i == hovered ? orbitColor : (i == selected ? ZenTheme.Accent : ZenTheme.SurfaceRaised); using (SolidBrush b = new SolidBrush(fill)) g.FillEllipse(b, p.X - r, p.Y - r, 2 * r, 2 * r); using (Pen edge = new Pen(Color.FromArgb(180, orbitColor), 1f)) g.DrawEllipse(edge, p.X - r, p.Y - r, 2 * r, 2 * r); TextRenderer.DrawText(g, items[i], Font, new Rectangle((int)p.X - 34, (int)p.Y - 17, 68, 34), i == hovered ? Color.White : ZenTheme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); } using (SolidBrush core = new SolidBrush(orbitColor)) g.FillEllipse(core, c.X - 31, c.Y - 31, 62, 62); using (Font centerFont = new Font("Segoe UI Semibold", 8f)) TextRenderer.DrawText(g, expanded ? "ZEN" : "MENU", centerFont, new Rectangle((int)c.X - 30, (int)c.Y - 15, 60, 30), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
    }
}

// watashi wa watashi sore dake