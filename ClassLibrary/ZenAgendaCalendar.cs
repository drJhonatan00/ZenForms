using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(MonthCalendar))]
    [Category("ZenForms Modern Components")]
    [Description("Monthly agenda calendar with selectable dates and colored event markers.")]
    public class ZenAgendaCalendar : Control
    {
        private readonly List<ZenAgendaEvent> _events = new List<ZenAgendaEvent>();
        private readonly ToolTip _toolTip;
        private DateTime _displayMonth;
        private DateTime? _selectedDate;
        private DateTime? _hotDate;
        private bool _hoverPrevious;
        private bool _hoverNext;
        private int _cornerRadius = 14;
        private Color _surfaceColor = ZenTheme.Surface;
        private Color _selectedColor = ZenTheme.Primary;
        private Color _todayColor = ZenTheme.Accent;
        private string _dateFormat = "dddd, dd 'de' MMMM 'de' yyyy";

        public ZenAgendaCalendar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(330, 290);
            MinimumSize = new Size(190, 180);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            TabStop = true;
            Cursor = Cursors.Hand;
            _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _toolTip = new ToolTip();
            _toolTip.AutoPopDelay = 7000;
            _toolTip.InitialDelay = 250;
            _toolTip.ReshowDelay = 100;
            _toolTip.ShowAlways = true;
        }

        [Category("ZenForms Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Description("The colored agenda events displayed on the calendar.")]
        public IList<ZenAgendaEvent> Events { get { return _events.AsReadOnly(); } }

        [Category("ZenForms Data"), Description("The month currently shown by the calendar.")]
        public DateTime DisplayMonth
        {
            get { return _displayMonth; }
            set { _displayMonth = new DateTime(value.Year, value.Month, 1); Invalidate(); UpdateToolTip(); }
        }

        [Category("ZenForms Data"), Description("The selected calendar date, or null when no date is selected.")]
        [TypeConverter(typeof(NullableDateTimeConverter))]
        public DateTime? SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                DateTime? normalized = value.HasValue ? value.Value.Date : (DateTime?)null;
                if (_selectedDate == normalized) return;
                _selectedDate = normalized;
                if (_selectedDate.HasValue) _displayMonth = new DateTime(_selectedDate.Value.Year, _selectedDate.Value.Month, 1);
                Invalidate();
            }
        }

        [Category("ZenForms Appearance"), DefaultValue(14)]
        public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = Math.Max(0, value); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color SurfaceColor { get { return _surfaceColor; } set { _surfaceColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color SelectedColor { get { return _selectedColor; } set { _selectedColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color TodayColor { get { return _todayColor; } set { _todayColor = value; Invalidate(); } }
        [Category("ZenForms Behavior"), DefaultValue("dddd, dd 'de' MMMM 'de' yyyy")]
        public string DateToolTipFormat { get { return _dateFormat; } set { _dateFormat = value ?? "d"; UpdateToolTip(); } }

        [Category("ZenForms Action"), Description("Occurs when a date is selected by the user.")]
        public event EventHandler<ZenDateSelectedEventArgs> DateSelected;

        public void AddEvent(DateTime date, string title, Color color)
        {
            AddEvent(new ZenAgendaEvent(date, title, color));
        }

        public void AddEvent(DateTime date, string title)
        {
            AddEvent(new ZenAgendaEvent(date, title, ZenTheme.Accent));
        }

        public void AddEvent(ZenAgendaEvent agendaEvent)
        {
            if (agendaEvent == null) return;
            _events.Add(agendaEvent);
            Invalidate();
            UpdateToolTip();
        }

        public void ClearEvents()
        {
            if (_events.Count == 0) return;
            _events.Clear();
            Invalidate();
            UpdateToolTip();
        }

        public void GoToPreviousMonth() { DisplayMonth = _displayMonth.AddMonths(-1); }
        public void GoToNextMonth() { DisplayMonth = _displayMonth.AddMonths(1); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool oldPrevious = _hoverPrevious, oldNext = _hoverNext;
            _hoverPrevious = PreviousButtonRectangle.Contains(e.Location);
            _hoverNext = NextButtonRectangle.Contains(e.Location);
            DateTime? oldDate = _hotDate;
            _hotDate = HitTestDate(e.Location);
            if (oldPrevious != _hoverPrevious || oldNext != _hoverNext || oldDate != _hotDate) Invalidate();
            UpdateToolTip();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverPrevious = false; _hoverNext = false; _hotDate = null;
            _toolTip.Hide(this); Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            if (PreviousButtonRectangle.Contains(e.Location)) { GoToPreviousMonth(); return; }
            if (NextButtonRectangle.Contains(e.Location)) { GoToNextMonth(); return; }
            DateTime? date = HitTestDate(e.Location);
            if (date.HasValue) SelectDate(date.Value);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) { SelectDate((_selectedDate ?? DateTime.Today).AddDays(-1)); e.Handled = true; }
            else if (e.KeyCode == Keys.Right) { SelectDate((_selectedDate ?? DateTime.Today).AddDays(1)); e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { SelectDate((_selectedDate ?? DateTime.Today).AddDays(-7)); e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { SelectDate((_selectedDate ?? DateTime.Today).AddDays(7)); e.Handled = true; }
            else if (e.KeyCode == Keys.PageUp) { GoToPreviousMonth(); e.Handled = true; }
            else if (e.KeyCode == Keys.PageDown) { GoToNextMonth(); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                if (!_selectedDate.HasValue) SelectDate(DateTime.Today);
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Rectangle outer = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
            using (GraphicsPath path = ZenButtonBase.RoundPath(outer, _cornerRadius))
            using (SolidBrush fill = new SolidBrush(_surfaceColor))
            using (Pen border = new Pen(Color.FromArgb(180, ZenTheme.Border), 1f))
            {
                g.FillPath(fill, path); g.DrawPath(border, path);
            }
            DrawHeader(g, outer);
            DrawWeekDays(g, outer);
            DrawDays(g, outer);
        }

        private void DrawHeader(Graphics g, Rectangle outer)
        {
            Rectangle header = new Rectangle(outer.Left + 14, outer.Top + 0, Math.Max(1, outer.Width - 28), 34);
            string caption = _displayMonth.ToString("MMMM\nyyyy", CultureInfo.CurrentCulture);
            using (Font titleFont = new Font("Segoe UI Semibold", 9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(caption), titleFont,
                    new Rectangle(header.Left + 30, header.Top, Math.Max(1, header.Width - 60), header.Height),
                    ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            DrawArrowButton(g, PreviousButtonRectangle, false, _hoverPrevious);
            DrawArrowButton(g, NextButtonRectangle, true, _hoverNext);
        }

        private void DrawArrowButton(Graphics g, Rectangle r, bool right, bool hovered)
        {
            using (SolidBrush b = new SolidBrush(hovered ? Color.FromArgb(65, ZenTheme.Primary) : Color.FromArgb(35, ZenTheme.SurfaceRaised)))
            using (Pen p = new Pen(hovered ? ZenTheme.Accent : ZenTheme.Muted, 1.5f))
            {
                g.FillEllipse(b, r); g.DrawEllipse(p, r);
                Point a = new Point(right ? r.Left + 10 : r.Right - 10, r.Top + 7);
                Point bpt = new Point(right ? r.Right - 10 : r.Left + 10, r.Top + r.Height / 2);
                Point c = new Point(right ? r.Left + 10 : r.Right - 10, r.Bottom - 7);
                using (Pen arrow = new Pen(hovered ? ZenTheme.Foreground : ZenTheme.Muted, 1.6f))
                { g.DrawLine(arrow, a, bpt); g.DrawLine(arrow, bpt, c); }
            }
        }

        private void DrawWeekDays(Graphics g, Rectangle outer)
        {
            Rectangle grid = GridRectangle(outer);
            int cellWidth = Math.Max(1, grid.Width / 7);
            using (Font dayFont = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold))
            {
                for (int i = 0; i < 7; i++)
                {
                    string name = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((i + 1) % 7));
                    Rectangle r = new Rectangle(grid.Left + i * cellWidth, grid.Top, cellWidth, 20);
                    TextRenderer.DrawText(g, name.Substring(0, Math.Min(2, name.Length)).ToUpperInvariant(), dayFont, r, ZenTheme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            }
        }

        private void DrawDays(Graphics g, Rectangle outer)
        {
            Rectangle grid = GridRectangle(outer);
            int cellWidth = Math.Max(1, grid.Width / 7);
            int rows = 6;
            int cellHeight = Math.Max(1, (grid.Height - 20) / rows);
            DateTime first = _displayMonth;
            int offset = ((int)first.DayOfWeek + 6) % 7;
            using (Font numberFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            {
                for (int index = 0; index < 42; index++)
                {
                    DateTime date = first.AddDays(index - offset);
                    Rectangle cell = new Rectangle(grid.Left + (index % 7) * cellWidth, grid.Top + 20 + (index / 7) * cellHeight, cellWidth, cellHeight);
                    bool inMonth = date.Month == _displayMonth.Month;
                    bool selected = _selectedDate.HasValue && _selectedDate.Value.Date == date.Date;
                    bool today = DateTime.Today == date.Date;
                    bool hot = _hotDate.HasValue && _hotDate.Value.Date == date.Date;
                    if (selected || hot)
                    {
                        using (SolidBrush b = new SolidBrush(selected ? _selectedColor : Color.FromArgb(55, ZenTheme.Primary)))
                            g.FillEllipse(b, new Rectangle(cell.Left + 4, cell.Top + 3, Math.Max(1, cellWidth - 8), Math.Max(1, cellHeight - 8)));
                    }
                    if (today && !selected)
                    {
                        using (Pen p = new Pen(_todayColor, 1.5f))
                            g.DrawEllipse(p, new Rectangle(cell.Left + 5, cell.Top + 4, Math.Max(1, cellWidth - 10), Math.Max(1, cellHeight - 10)));
                    }
                    Color textColor = !inMonth ? Color.FromArgb(75, ZenTheme.Muted) : (selected ? Color.White : ForeColor);
                    TextRenderer.DrawText(g, date.Day.ToString(CultureInfo.InvariantCulture), numberFont,
                        new Rectangle(cell.Left, cell.Top + 2, cell.Width, Math.Max(1, cell.Height - 12)), textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    List<ZenAgendaEvent> dayEvents = EventsOn(date);
                    int markerWidth = Math.Min(7, Math.Max(3, (cellWidth - 10) / Math.Max(1, dayEvents.Count)));
                    for (int marker = 0; marker < dayEvents.Count && marker < 4; marker++)
                    {
                        using (SolidBrush b = new SolidBrush(dayEvents[marker].Color))
                            g.FillEllipse(b, new Rectangle(cell.Left + 5 + marker * (markerWidth + 2), cell.Bottom - 9, markerWidth, markerWidth));
                    }
                }
            }
        }

        private Rectangle GridRectangle(Rectangle outer)
        {
            return new Rectangle(outer.Left + 12, outer.Top + 50, Math.Max(7, outer.Width - 24), Math.Max(7, outer.Height - 60));
        }

        private Rectangle PreviousButtonRectangle { get { return new Rectangle(Math.Max(5, Width / 2 - 62), 14, 24, 24); } }
        private Rectangle NextButtonRectangle { get { return new Rectangle(Math.Max(5, Width / 2 + 38), 14, 24, 24); } }

        private DateTime? HitTestDate(Point location)
        {
            Rectangle grid = GridRectangle(new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9)));
            if (!grid.Contains(location) || location.Y < grid.Top + 20) return null;
            int cellWidth = Math.Max(1, grid.Width / 7), cellHeight = Math.Max(1, (grid.Height - 20) / 6);
            int column = Math.Min(6, Math.Max(0, (location.X - grid.Left) / cellWidth));
            int row = Math.Min(5, Math.Max(0, (location.Y - grid.Top - 20) / cellHeight));
            int offset = ((int)_displayMonth.DayOfWeek + 6) % 7;
            DateTime date = _displayMonth.AddDays(row * 7 + column - offset);
            return date.Month == _displayMonth.Month ? date : (DateTime?)null;
        }

        private void SelectDate(DateTime date)
        {
            DateTime normalized = date.Date;
            _selectedDate = normalized;
            _displayMonth = new DateTime(normalized.Year, normalized.Month, 1);
            Invalidate();
            OnDateSelected(new ZenDateSelectedEventArgs(normalized));
            UpdateToolTip();
        }

        protected virtual void OnDateSelected(ZenDateSelectedEventArgs e)
        {
            EventHandler<ZenDateSelectedEventArgs> handler = DateSelected;
            if (handler != null) handler(this, e);
        }

        private List<ZenAgendaEvent> EventsOn(DateTime date)
        {
            List<ZenAgendaEvent> result = new List<ZenAgendaEvent>();
            for (int i = 0; i < _events.Count; i++) if (_events[i] != null && _events[i].Date.Date == date.Date) result.Add(_events[i]);
            return result;
        }

        private void UpdateToolTip()
        {
            if (_toolTip == null || !_hotDate.HasValue) return;
            List<ZenAgendaEvent> items = EventsOn(_hotDate.Value);
            if (items.Count == 0) { _toolTip.Hide(this); return; }
            string text = _hotDate.Value.ToString(_dateFormat, CultureInfo.CurrentCulture);
            for (int i = 0; i < items.Count; i++) if (!string.IsNullOrEmpty(items[i].Title)) text += Environment.NewLine + "• " + items[i].Title;
            _toolTip.SetToolTip(this, text);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _toolTip != null) _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ZenAgendaEvent
    {
        public ZenAgendaEvent() : this(DateTime.Today, "", ZenTheme.Accent) { }
        public ZenAgendaEvent(DateTime date, string title, Color color) { Date = date.Date; Title = title ?? ""; Color = color; }
        [Category("Data")] public DateTime Date { get; set; }
        [Category("Data")] public string Title { get; set; }
        [Category("Appearance")] public Color Color { get; set; }
        public override string ToString() { return string.IsNullOrEmpty(Title) ? Date.ToShortDateString() : Title; }
    }

    public class ZenDateSelectedEventArgs : EventArgs
    {
        public ZenDateSelectedEventArgs(DateTime date) { Date = date.Date; }
        public DateTime Date { get; private set; }
    }

    internal sealed class NullableDateTimeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType); }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string && string.IsNullOrWhiteSpace((string)value)) return null;
            return base.ConvertFrom(context, culture, value);
        }
    }
}

// watashi wa watashi sore dake