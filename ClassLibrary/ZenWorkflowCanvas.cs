using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenWorkflowNode { public string Id { get; internal set; } public string Title { get; set; } public string Subtitle { get; set; } public Point Location { get; set; } public Color Color { get; set; } public ZenWorkflowNode(string title, Point location, Color color) { Id = Guid.NewGuid().ToString("N"); Title = title; Subtitle = String.Empty; Location = location; Color = color; } }
    public sealed class ZenWorkflowConnection { public string FromId { get; internal set; } public string ToId { get; internal set; } }
    public sealed class ZenWorkflowNodeEventArgs : EventArgs { public ZenWorkflowNode Node { get; private set; } public ZenWorkflowNodeEventArgs(ZenWorkflowNode node) { Node = node; } }
    [Category("ZenForms Modern Components")]
    [Description("Intercative canvas to prototype.")]
    public class ZenWorkflowCanvas : Control
    {
        private readonly List<ZenWorkflowNode> nodes = new List<ZenWorkflowNode>(); private readonly List<ZenWorkflowConnection> links = new List<ZenWorkflowConnection>(); private ZenWorkflowNode dragging; private Point dragOffset; private int nodeWidth = 170, nodeHeight = 66; private Color linkColor = ZenTheme.Accent;
        [Browsable(false)] public IList<ZenWorkflowNode> Nodes { get { return nodes.AsReadOnly(); } }
        [Browsable(false)] public IList<ZenWorkflowConnection> Connections { get { return links.AsReadOnly(); } }
        [Category("ZenForms Appearance")] public Color LinkColor { get { return linkColor; } set { linkColor = value; Invalidate(); } }
        public event EventHandler<ZenWorkflowNodeEventArgs> NodeSelected;
        public ZenWorkflowCanvas() { Size = new Size(720, 420); BackColor = Color.FromArgb(13, 17, 30); ForeColor = ZenTheme.Foreground; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); Cursor = Cursors.Hand; TabStop = true; }
        public ZenWorkflowNode AddNode(string title, int x, int y) { return AddNode(title, String.Empty, new Point(x, y), ZenTheme.Primary); }
        public ZenWorkflowNode AddNode(string title, string subtitle, Point location, Color color) { ZenWorkflowNode n = new ZenWorkflowNode(title ?? "", location, color); n.Subtitle = subtitle ?? ""; nodes.Add(n); Invalidate(); return n; }
        public void Connect(ZenWorkflowNode from, ZenWorkflowNode to) { if (from == null || to == null || from == to) throw new ArgumentException("Select two different nodes."); if (!nodes.Contains(from) || !nodes.Contains(to)) throw new ArgumentException("The nodes must belong to this canvas."); if (!links.Exists(x => x.FromId == from.Id && x.ToId == to.Id)) links.Add(new ZenWorkflowConnection { FromId = from.Id, ToId = to.Id }); Invalidate(); }
        public void RemoveNode(ZenWorkflowNode node) { if (node == null) return; nodes.Remove(node); links.RemoveAll(x => x.FromId == node.Id || x.ToId == node.Id); Invalidate(); }
        public void ClearWorkflow() { nodes.Clear(); links.Clear(); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); if (e.Button != MouseButtons.Left) return; ZenWorkflowNode n = Hit(e.Location); if (n != null) { dragging = n; dragOffset = new Point(e.X - n.Location.X, e.Y - n.Location.Y); EventHandler<ZenWorkflowNodeEventArgs> h = NodeSelected; if (h != null) h(this, new ZenWorkflowNodeEventArgs(n)); } }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging != null) { dragging.Location = new Point(Math.Max(0, e.X - dragOffset.X), Math.Max(0, e.Y - dragOffset.Y)); Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = null; }
        private ZenWorkflowNode Hit(Point p) { for (int i = nodes.Count - 1; i >= 0; i--) { Rectangle r = new Rectangle(nodes[i].Location, new Size(nodeWidth, nodeHeight)); if (r.Contains(p)) return nodes[i]; } return null; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor); using (Pen grid = new Pen(Color.FromArgb(22, 120, 140, 170))) for (int x = 0; x < Width; x += 24) g.DrawLine(grid, x, 0, x, Height); using (Pen grid = new Pen(Color.FromArgb(22, 120, 140, 170))) for (int y = 0; y < Height; y += 24) g.DrawLine(grid, 0, y, Width, y); foreach (ZenWorkflowConnection c in links) { ZenWorkflowNode a = nodes.Find(n => n.Id == c.FromId), b = nodes.Find(n => n.Id == c.ToId); if (a == null || b == null) continue; Point p1 = new Point(a.Location.X + nodeWidth, a.Location.Y + nodeHeight / 2), p2 = new Point(b.Location.X, b.Location.Y + nodeHeight / 2); DrawConnection(g, p1, p2); } foreach (ZenWorkflowNode n in nodes) DrawNode(g, n); }
        private void DrawConnection(Graphics g, Point a, Point b) { int bend = Math.Max(35, Math.Abs(b.X - a.X) / 2); Point c1 = new Point(a.X + bend, a.Y), c2 = new Point(b.X - bend, b.Y); using (Pen p = new Pen(linkColor, 2f)) { p.EndCap = LineCap.ArrowAnchor; g.DrawBezier(p, a, c1, c2, b); } }
        private void DrawNode(Graphics g, ZenWorkflowNode n) { Rectangle r = new Rectangle(n.Location, new Size(nodeWidth, nodeHeight)); using (GraphicsPath path = ZenButtonBase.RoundPath(r, 10)) { using (SolidBrush b = new SolidBrush(ZenTheme.SurfaceRaised)) g.FillPath(b, path); using (Pen border = new Pen(n.Color, 2f)) g.DrawPath(border, path); using (SolidBrush dot = new SolidBrush(n.Color)) g.FillEllipse(dot, r.X + 12, r.Y + 16, 9, 9); using (Font titleFont = new Font("Segoe UI Semibold", 9f)) using (Font subFont = new Font("Segoe UI", 8f)) { TextRenderer.DrawText(g, n.Title, titleFont, new Rectangle(r.X + 28, r.Y + 9, r.Width - 38, 23), ZenTheme.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); TextRenderer.DrawText(g, n.Subtitle, subFont, new Rectangle(r.X + 28, r.Y + 33, r.Width - 38, 20), ZenTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); } } }
    }
}

// watashi wa watashi sore dake
