using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenKanbanMoveEventArgs : EventArgs { public string Card { get; private set; } public string FromColumn { get; private set; } public string ToColumn { get; private set; } public ZenKanbanMoveEventArgs(string card, string from, string to) { Card = card; FromColumn = from; ToColumn = to; } }
    [Category("ZenForms Modern Components")]
    [Description("Kanban board with cards that can be dragged between columns.")]
    public class ZenKanbanBoard : UserControl
    {
        private sealed class Column { public string Name; public Panel Root; public FlowLayoutPanel Cards; public Label Count; }
        private sealed class DragInfo { public Label Card; public FlowLayoutPanel Source; public string SourceName; }
        private readonly FlowLayoutPanel host; private readonly List<Column> columns = new List<Column>(); private Color cardColor = ZenTheme.SurfaceRaised;
        [Category("ZenForms Appearance")] public Color CardColor { get { return cardColor; } set { cardColor = value; RefreshCards(); } }
        public event EventHandler<ZenKanbanMoveEventArgs> CardMoved;
        public ZenKanbanBoard() { Size = new Size(760, 440); BackColor = ZenTheme.Background; host = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8), BackColor = ZenTheme.Background }; Controls.Add(host); }
        public void AddColumn(string name) { if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter with column name.", "name"); if (columns.Exists(c => String.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("A column with this name already exists.", "name"); Panel root = new Panel { Width = 220, Height = Math.Max(180, Height - 34), BackColor = ZenTheme.Surface, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(8) }; Label title = new Label { Dock = DockStyle.Top, Height = 34, Text = name, Font = new Font("Segoe UI Semibold", 10f), ForeColor = ZenTheme.Foreground, TextAlign = ContentAlignment.MiddleLeft }; Label count = new Label { Dock = DockStyle.Right, Width = 30, Text = "0", Font = new Font("Segoe UI", 8f), ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleCenter }; FlowLayoutPanel cards = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, AllowDrop = true, Padding = new Padding(0, 6, 0, 0), BackColor = ZenTheme.Surface }; Panel header = new Panel { Dock = DockStyle.Top, Height = 34 }; header.Controls.Add(count); header.Controls.Add(title); root.Controls.Add(cards); root.Controls.Add(header); Column col = new Column { Name = name, Root = root, Cards = cards, Count = count }; columns.Add(col); host.Controls.Add(root); cards.DragEnter += CardDragEnter; cards.DragDrop += (s, e) => CardDragDrop(col, e); }
        public void AddCard(string columnName, string title) { Column col = FindColumn(columnName); Label card = new Label { Text = title ?? "", Tag = title, Width = 190, Height = 64, Padding = new Padding(10), Margin = new Padding(0, 0, 0, 8), BackColor = cardColor, ForeColor = ZenTheme.Foreground, Font = new Font("Segoe UI", 9f), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.SizeAll, AutoEllipsis = true }; card.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { DragInfo data = new DragInfo { Card = card, Source = col.Cards, SourceName = col.Name }; card.DoDragDrop(data, DragDropEffects.Move); } }; col.Cards.Controls.Add(card); UpdateCount(col); }
        public void RemoveColumn(string name) { Column c = FindColumn(name); columns.Remove(c); host.Controls.Remove(c.Root); c.Root.Dispose(); }
        public void ClearCards(string columnName) { Column c = FindColumn(columnName); c.Cards.Controls.Clear(); UpdateCount(c); }
        public IEnumerable<string> GetCardTitles(string columnName) { Column c = FindColumn(columnName); List<string> result = new List<string>(); foreach (Control item in c.Cards.Controls) result.Add(item.Text); return result; }
        private Column FindColumn(string name) { Column c = columns.Find(x => String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)); if (c == null) throw new ArgumentException("The column doesn't exists: " + name, "name"); return c; }
        private void CardDragEnter(object sender, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(typeof(DragInfo)) ? DragDropEffects.Move : DragDropEffects.None; }
        private void CardDragDrop(Column target, DragEventArgs e) { DragInfo d = e.Data.GetData(typeof(DragInfo)) as DragInfo; if (d == null || d.Source == target.Cards) return; d.Source.Controls.Remove(d.Card); target.Cards.Controls.Add(d.Card); UpdateCount(FindColumn(d.SourceName)); UpdateCount(target); EventHandler<ZenKanbanMoveEventArgs> h = CardMoved; if (h != null) h(this, new ZenKanbanMoveEventArgs(d.Card.Text, d.SourceName, target.Name)); }
        private static void UpdateCount(Column c) { c.Count.Text = c.Cards.Controls.Count.ToString(); }
        private void RefreshCards() { foreach (Column c in columns) foreach (Control card in c.Cards.Controls) card.BackColor = cardColor; }
    }
}

// watashi wa watashi sore dake