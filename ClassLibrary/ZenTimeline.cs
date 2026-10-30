using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenTimelineItem { public string Title { get; set; } public string Description { get; set; } public string Time { get; set; } public Color Color { get; set; } public ZenTimelineItem(string title, string description, string time, Color color) { Title = title; Description = description; Time = time; Color = color; } }
    public sealed class ZenTimelineEventArgs : EventArgs { public int Index { get; private set; } public ZenTimelineItem Item { get; private set; } public ZenTimelineEventArgs(int index, ZenTimelineItem item) { Index = index; Item = item; } }
    [Category("ZenForms Modern Components")]
    [Description("Interactive vertical timeline for events and milestones.")]
    public class ZenTimeline : Control
    {
        private readonly List<ZenTimelineItem> items = new List<ZenTimelineItem>(); private int selected = -1; private int scrollOffset; private int itemHeight = 84;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)] public IList<ZenTimelineItem> Items { get { return items.AsReadOnly(); } }
        [Category("ZenForms Appearance"), DefaultValue(84)] public int ItemHeight { get { return itemHeight; } set { itemHeight = Math.Max(60, value); Invalidate(); } }
        [Browsable(false)] public int SelectedIndex { get { return selected; } }
        public event EventHandler<ZenTimelineEventArgs> ItemSelected;
        public ZenTimeline() { Size = new Size(360, 340); BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); TabStop = true; }
        public void AddItem(string title, string description, string time) { AddItem(title, description, time, ZenTheme.Primary); }
        public void AddItem(string title, string description, string time, Color color) { items.Add(new ZenTimelineItem(title ?? "", description ?? "", time ?? "", color)); Invalidate(); }
        public void ClearItems() { items.Clear(); selected = -1; scrollOffset = 0; Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); int max = Math.Max(0, items.Count * itemHeight - Height); scrollOffset = Math.Max(0, Math.Min(max, scrollOffset - (e.Delta / 120) * itemHeight)); Invalidate(); }
        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); int idx = (e.Y + scrollOffset) / itemHeight; if (idx >= 0 && idx < items.Count) { selected = idx; EventHandler<ZenTimelineEventArgs> h = ItemSelected; if (h != null) h(this, new ZenTimelineEventArgs(idx, items[idx])); Invalidate(); } }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor); GraphicsState state = g.Save(); g.SetClip(ClientRectangle); int axis = 31; using (Pen line = new Pen(ZenTheme.Border, 2)) g.DrawLine(line, axis, 12, axis, items.Count * itemHeight - scrollOffset); for (int i = 0; i < items.Count; i++) { int top = i * itemHeight - scrollOffset; if (top > Height || top + itemHeight < 0) continue; ZenTimelineItem item = items[i]; int cy = top + 23; using (SolidBrush dot = new SolidBrush(item.Color)) g.FillEllipse(dot, axis - 7, cy - 7, 14, 14); if (i == selected) using (Pen halo = new Pen(Color.FromArgb(90, item.Color), 5)) g.DrawEllipse(halo, axis - 9, cy - 9, 18, 18); Rectangle card = new Rectangle(54, top + 5, Math.Max(10, Width - 68), itemHeight - 12); using (GraphicsPath p = ZenButtonBase.RoundPath(card, 10)) { using (SolidBrush b = new SolidBrush(i == selected ? ZenTheme.SurfaceRaised : ZenTheme.Surface)) g.FillPath(b, p); using (Pen border = new Pen(i == selected ? item.Color : ZenTheme.Border, 1)) g.DrawPath(border, p); } using (Font smallFont = new Font("Segoe UI", 8f)) using (Font titleFont = new Font("Segoe UI Semibold", 9.5f)) { TextRenderer.DrawText(g, item.Time, smallFont, new Rectangle(card.X + 12, card.Y + 7, card.Width - 22, 15), item.Color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); TextRenderer.DrawText(g, item.Title, titleFont, new Rectangle(card.X + 12, card.Y + 23, card.Width - 22, 20), ZenTheme.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); TextRenderer.DrawText(g, item.Description, smallFont, new Rectangle(card.X + 12, card.Y + 44, card.Width - 22, Math.Max(12, card.Height - 48)), ZenTheme.Muted, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak); } } g.Restore(state); }
    }
}

// watashi wa watashi sore dake
