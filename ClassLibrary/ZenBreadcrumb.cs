using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class ZenBreadcrumbSegmentClickedEventArgs : EventArgs
    {
        public ZenBreadcrumbSegmentClickedEventArgs(int index, string text)
        {
            Index = index;
            Text = text ?? string.Empty;
        }

        public int Index { get; private set; }
        public string Text { get; private set; }
    }

    public sealed class ZenBreadcrumbSegmentCollection : Collection<string>
    {
        private readonly ZenBreadcrumb _owner;

        internal ZenBreadcrumbSegmentCollection(ZenBreadcrumb owner)
        {
            _owner = owner;
        }

        protected override void InsertItem(int index, string item)
        {
            if (string.IsNullOrEmpty(item)) return;
            base.InsertItem(index, item);
            _owner.NotifySegmentsChanged();
        }

        protected override void SetItem(int index, string item)
        {
            if (string.IsNullOrEmpty(item)) return;
            base.SetItem(index, item);
            _owner.NotifySegmentsChanged();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            _owner.NotifySegmentsChanged();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _owner.NotifySegmentsChanged();
        }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Neon breadcrumb navigation bar with selectable segments.")]
    public class ZenBreadcrumb : Control
    {
        private readonly ZenBreadcrumbSegmentCollection _segments;
        private readonly System.Collections.Generic.List<Rectangle> _segmentBounds;
        private Font _ownedFont;
        private string _separator = "/";
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;
        private int _cornerRadius = 10;
        private bool _focused;

        public ZenBreadcrumb()
        {
            _segments = new ZenBreadcrumbSegmentCollection(this);
            _segmentBounds = new System.Collections.Generic.List<Rectangle>();
            _ownedFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Font = _ownedFont;
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Size = new Size(420, 42);
            MinimumSize = new Size(70, 30);
            TabStop = true;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
        }

        [Category("ZenForms Data")]
        [Description("The breadcrumb segments displayed from left to right.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ZenBreadcrumbSegmentCollection Segments { get { return _segments; } }

        [Category("ZenForms Appearance")]
        [Description("The text drawn between adjacent segments.")]
        [DefaultValue("/")]
        public string Separator
        {
            get { return _separator; }
            set { _separator = value ?? string.Empty; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Corner radius of the breadcrumb surface.")]
        [DefaultValue(10)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Behavior")]
        [Description("Index of the currently selected segment, or -1 when nothing is selected.")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int next = value < 0 || value >= _segments.Count ? -1 : value;
                if (_selectedIndex != next)
                {
                    _selectedIndex = next;
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public string SelectedSegment
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _segments.Count ? _segments[_selectedIndex] : null; }
        }

        [Category("ZenForms Action")]
        [Description("Raised when a breadcrumb segment is clicked or activated from the keyboard.")]
        public event EventHandler<ZenBreadcrumbSegmentClickedEventArgs> SegmentClicked;

        public void AddSegment(string segment)
        {
            if (!string.IsNullOrEmpty(segment)) _segments.Add(segment);
        }

        public void ClearSegments()
        {
            _segments.Clear();
            _selectedIndex = -1;
            _hoverIndex = -1;
            Invalidate();
        }

        internal void NotifySegmentsChanged()
        {
            if (_selectedIndex >= _segments.Count) _selectedIndex = _segments.Count - 1;
            if (_segments.Count == 0) _selectedIndex = -1;
            if (_hoverIndex >= _segments.Count) _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTest(e.Location);
            if (_hoverIndex != index) { _hoverIndex = index; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1) { _hoverIndex = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                int index = HitTest(e.Location);
                if (index >= 0) ActivateSegment(index);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _focused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _focused = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_segments.Count == 0) return;
            int next = _selectedIndex;
            if (e.KeyCode == Keys.Left) next = next <= 0 ? 0 : next - 1;
            else if (e.KeyCode == Keys.Right) next = next < 0 || next >= _segments.Count - 1 ? _segments.Count - 1 : next + 1;
            else if (e.KeyCode == Keys.Home) next = 0;
            else if (e.KeyCode == Keys.End) next = _segments.Count - 1;
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ActivateSegment(_selectedIndex < 0 ? 0 : _selectedIndex);
                e.Handled = true;
                return;
            }
            else return;
            SelectedIndex = next;
            e.Handled = true;
        }

        private void ActivateSegment(int index)
        {
            if (index < 0 || index >= _segments.Count) return;
            SelectedIndex = index;
            EventHandler<ZenBreadcrumbSegmentClickedEventArgs> handler = SegmentClicked;
            if (handler != null) handler(this, new ZenBreadcrumbSegmentClickedEventArgs(index, _segments[index]));
        }

        private int HitTest(Point point)
        {
            for (int i = 0; i < _segmentBounds.Count; i++)
                if (_segmentBounds[i].Contains(point)) return i;
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _segmentBounds.Clear();
            Rectangle outer = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (GraphicsPath path = ZenButtonBase.RoundPath(outer, _cornerRadius))
            {
                using (SolidBrush fill = new SolidBrush(ZenTheme.Surface)) g.FillPath(fill, path);
                using (Pen border = new Pen(Color.FromArgb(_focused ? 180 : 110, ZenTheme.Border), 1f)) g.DrawPath(border, path);
            }

            int x = 12;
            int y = 5;
            int h = Math.Max(18, Height - 10);
            for (int i = 0; i < _segments.Count; i++)
            {
                string text = _segments[i] ?? string.Empty;
                Size measured = TextRenderer.MeasureText(g, text, Font, new Size(Math.Max(1, Width), h), TextFormatFlags.SingleLine);
                int width = Math.Max(18, measured.Width + 18);
                if (x + width > Width - 8) width = Math.Max(12, Width - 8 - x);
                Rectangle segment = new Rectangle(x, y, width, h);
                _segmentBounds.Add(segment);
                bool active = i == _selectedIndex;
                bool hover = i == _hoverIndex;
                if (active || hover)
                {
                    Color glow = active ? ZenTheme.Primary : ZenTheme.Accent;
                    using (GraphicsPath pill = ZenButtonBase.RoundPath(segment, Math.Min(8, h / 2)))
                    {
                        if (hover) using (Pen soft = new Pen(Color.FromArgb(55, glow), 5f)) g.DrawPath(soft, pill);
                        using (SolidBrush pillFill = new SolidBrush(Color.FromArgb(active ? 55 : 30, glow))) g.FillPath(pillFill, pill);
                        using (Pen pillBorder = new Pen(Color.FromArgb(active ? 170 : 125, glow), 1f)) g.DrawPath(pillBorder, pill);
                    }
                }
                Color textColor = active || hover ? ZenTheme.Foreground : ZenTheme.Muted;
                TextRenderer.DrawText(g, text, Font, segment, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                x += width;
                if (i < _segments.Count - 1)
                {
                    Size sep = TextRenderer.MeasureText(g, _separator, Font, new Size(Math.Max(1, Width), h), TextFormatFlags.SingleLine);
                    int sepWidth = Math.Max(8, sep.Width + 8);
                    Rectangle sepRect = new Rectangle(x, y, sepWidth, h);
                    TextRenderer.DrawText(g, _separator, Font, sepRect, ZenTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    x += sepWidth;
                }
                if (x >= Width - 4) break;
            }
            if (_focused) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(outer, -4, -4), Color.FromArgb(120, ZenTheme.Accent), Color.Transparent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _ownedFont != null)
            {
                _ownedFont.Dispose();
                _ownedFont = null;
            }
            base.Dispose(disposing);
        }
    }
}


// watashi wa watashi sore dake