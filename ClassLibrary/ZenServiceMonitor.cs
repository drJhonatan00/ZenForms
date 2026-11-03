using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenServiceState
    {
        Unknown,
        Healthy,
        Warning,
        Error,
        Offline
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class ZenServiceItem
    {
        public ZenServiceItem()
        {
            Name = string.Empty;
            Message = string.Empty;
            State = ZenServiceState.Unknown;
        }

        public ZenServiceItem(string name, ZenServiceState state, int latencyMilliseconds, string message)
        {
            Name = name ?? string.Empty;
            State = state;
            LatencyMilliseconds = Math.Max(0, latencyMilliseconds);
            Message = message ?? string.Empty;
        }

        [Category("Service"), Description("Display name of the service or endpoint.")]
        public string Name { get; set; }

        [Category("Service"), Description("Current health state of the service.")]
        public ZenServiceState State { get; set; }

        [Category("Service"), Description("Response latency in milliseconds.")]
        public int LatencyMilliseconds { get; set; }

        [Category("Service"), Description("Short status message shown below the service name.")]
        public string Message { get; set; }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }

    public sealed class ServiceSelectedEventArgs : EventArgs
    {
        public ServiceSelectedEventArgs(ZenServiceItem service)
        {
            Service = service;
        }

        public ZenServiceItem Service { get; private set; }
    }

    [Category("ZenForms Modern Components")]
    [Description("Compact neon service and endpoint health monitor.")]
    [ToolboxBitmap(typeof(ListView))]
    public class ZenServiceMonitor : Control
    {
        private readonly List<ZenServiceItem> _services = new List<ZenServiceItem>();
        private readonly Font _serviceFont;
        private readonly Font _messageFont;
        private readonly Font _latencyFont;
        private readonly ToolTip _toolTip;
        private int _selectedIndex = -1;
        private int _scrollOffset;
        private int _rowHeight = 62;
        private int _cornerRadius = 12;
        private bool _hovered;
        private int _hoverIndex = -1;
        private string _emptyText = "No services monitored";

        public ZenServiceMonitor()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(330, 250);
            MinimumSize = new Size(180, 90);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _serviceFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular);
            _messageFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _latencyFont = new Font("Segoe UI Semibold", 8f, FontStyle.Regular);
            _toolTip = new ToolTip();
            TabStop = true;
            AccessibleRole = AccessibleRole.List;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<ZenServiceItem> Services
        {
            get { return _services.AsReadOnly(); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ZenServiceItem SelectedService
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _services.Count ? _services[_selectedIndex] : null; }
        }

        [Category("ZenForms Appearance"), DefaultValue(62)]
        [Description("Height of each service row in pixels.")]
        public int RowHeight
        {
            get { return _rowHeight; }
            set { _rowHeight = Math.Max(44, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(12)]
        [Description("Corner radius used for the monitor and service rows.")]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue("No services monitored")]
        [Description("Text shown when the monitor has no service items.")]
        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value ?? string.Empty; Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Raised when a service row is selected by the user.")]
        public event EventHandler<ServiceSelectedEventArgs> ServiceSelected;

        public void AddService(string name, ZenServiceState state, int latencyMilliseconds, string message)
        {
            string safeName = name ?? string.Empty;
            ZenServiceItem existing = FindService(safeName);
            if (existing != null)
            {
                existing.State = state;
                existing.LatencyMilliseconds = Math.Max(0, latencyMilliseconds);
                existing.Message = message ?? string.Empty;
            }
            else
            {
                _services.Add(new ZenServiceItem(safeName, state, latencyMilliseconds, message));
            }
            Invalidate();
        }

        public void AddService(ZenServiceItem service)
        {
            if (service == null) return;
            AddService(service.Name, service.State, service.LatencyMilliseconds, service.Message);
        }

        public bool UpdateService(string name, ZenServiceState state, int latencyMilliseconds, string message)
        {
            ZenServiceItem service = FindService(name);
            if (service == null) return false;
            service.State = state;
            service.LatencyMilliseconds = Math.Max(0, latencyMilliseconds);
            service.Message = message ?? string.Empty;
            Invalidate();
            return true;
        }

        public bool UpdateService(string name, ZenServiceItem service)
        {
            if (service == null) return false;
            return UpdateService(name, service.State, service.LatencyMilliseconds, service.Message);
        }

        public void ClearServices()
        {
            _services.Clear();
            _selectedIndex = -1;
            _hoverIndex = -1;
            _scrollOffset = 0;
            _toolTip.Hide(this);
            Invalidate();
        }

        public ZenServiceItem GetService(string name)
        {
            return FindService(name);
        }

        private ZenServiceItem FindService(string name)
        {
            string sought = name ?? string.Empty;
            for (int i = 0; i < _services.Count; i++)
            {
                if (string.Equals(_services[i].Name, sought, StringComparison.OrdinalIgnoreCase))
                    return _services[i];
            }
            return null;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maximum = Math.Max(0, _services.Count * _rowHeight - Math.Max(1, Height - 12));
            _scrollOffset = Math.Max(0, Math.Min(maximum, _scrollOffset - e.Delta / 3));
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTest(e.Location);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                _hovered = index >= 0;
                if (index >= 0 && index < _services.Count)
                    _toolTip.SetToolTip(this, BuildToolTip(_services[index]));
                else
                    _toolTip.Hide(this);
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            _hovered = false;
            _toolTip.Hide(this);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            int index = HitTest(e.Location);
            if (index >= 0 && index < _services.Count)
            {
                _selectedIndex = index;
                Invalidate();
                EventHandler<ServiceSelectedEventArgs> handler = ServiceSelected;
                if (handler != null) handler(this, new ServiceSelectedEventArgs(_services[index]));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_services.Count == 0) return;
            int next = _selectedIndex;
            if (e.KeyCode == Keys.Down) next = Math.Min(_services.Count - 1, next + 1);
            else if (e.KeyCode == Keys.Up) next = Math.Max(0, next < 0 ? 0 : next - 1);
            else if (e.KeyCode == Keys.Home) next = 0;
            else if (e.KeyCode == Keys.End) next = _services.Count - 1;
            else return;
            if (next != _selectedIndex)
            {
                _selectedIndex = next;
                EnsureSelectedVisible();
                Invalidate();
                EventHandler<ServiceSelectedEventArgs> handler = ServiceSelected;
                if (handler != null) handler(this, new ServiceSelectedEventArgs(_services[next]));
            }
            e.Handled = true;
        }

        private int HitTest(Point point)
        {
            int left = 8;
            int top = 8;
            if (point.X < left || point.X >= Width - 8 || point.Y < top || point.Y >= Height - 8) return -1;
            int index = (point.Y - top + _scrollOffset) / _rowHeight;
            return index >= 0 && index < _services.Count ? index : -1;
        }

        private void EnsureSelectedVisible()
        {
            int viewport = Math.Max(1, Height - 16);
            int top = _selectedIndex * _rowHeight;
            if (top < _scrollOffset) _scrollOffset = top;
            else if (top + _rowHeight > _scrollOffset + viewport) _scrollOffset = top + _rowHeight - viewport;
            int maximum = Math.Max(0, _services.Count * _rowHeight - viewport);
            _scrollOffset = Math.Max(0, Math.Min(maximum, _scrollOffset));
        }

        private string BuildToolTip(ZenServiceItem service)
        {
            if (service == null) return string.Empty;
            return (service.Name ?? string.Empty) + "\n" + StateText(service.State) + "  •  " +
                   Math.Max(0, service.LatencyMilliseconds).ToString() + " ms\n" + (service.Message ?? string.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Rectangle bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (GraphicsPath outer = ZenButtonBase.RoundPath(bounds, _cornerRadius))
            using (SolidBrush background = new SolidBrush(ZenTheme.Background))
            using (Pen border = new Pen(ZenTheme.Border, 1f))
            {
                g.FillPath(background, outer);
                g.DrawPath(border, outer);
            }

            if (_services.Count == 0)
            {
                TextRenderer.DrawText(g, _emptyText, _messageFont, bounds, ZenTheme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                return;
            }

            Rectangle clip = new Rectangle(4, 4, Math.Max(1, Width - 8), Math.Max(1, Height - 8));
            GraphicsState state = g.Save();
            g.SetClip(clip);
            for (int i = 0; i < _services.Count; i++)
            {
                int y = 8 + i * _rowHeight - _scrollOffset;
                if (y + _rowHeight < clip.Top || y > clip.Bottom) continue;
                DrawServiceRow(g, _services[i], i, new Rectangle(8, y, Math.Max(1, Width - 16), _rowHeight - 6));
            }
            g.Restore(state);
        }

        private void DrawServiceRow(Graphics g, ZenServiceItem service, int index, Rectangle row)
        {
            bool selected = index == _selectedIndex;
            bool hovered = _hovered && index == _hoverIndex;
            Color glow = StateColor(service.State);
            using (GraphicsPath path = ZenButtonBase.RoundPath(row, Math.Min(_cornerRadius, 14)))
            using (SolidBrush fill = new SolidBrush(selected ? ZenTheme.SurfaceRaised : (hovered ? Color.FromArgb(30, 38, 61) : ZenTheme.Surface)))
            using (Pen outline = new Pen(Color.FromArgb(selected ? 180 : 85, glow), selected ? 1.4f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(outline, path);
                if (selected)
                {
                    using (Pen accent = new Pen(Color.FromArgb(190, glow), 2.5f))
                        g.DrawLine(accent, row.Left + 1, row.Top + 8, row.Left + 1, row.Bottom - 8);
                }
            }

            int dotSize = Math.Min(11, Math.Max(7, row.Height / 5));
            Rectangle dot = new Rectangle(row.Left + 13, row.Top + 14, dotSize, dotSize);
            using (SolidBrush dotBrush = new SolidBrush(glow)) g.FillEllipse(dotBrush, dot);
            using (Pen halo = new Pen(Color.FromArgb(70, glow), 3f)) g.DrawEllipse(halo, Rectangle.Inflate(dot, 2, 2));

            Rectangle nameRect = new Rectangle(row.Left + 32, row.Top + 8, Math.Max(20, row.Width - 130), 21);
            TextRenderer.DrawText(g, service.Name ?? string.Empty, _serviceFont, nameRect, ZenTheme.Foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            Rectangle messageRect = new Rectangle(row.Left + 32, row.Top + 29, Math.Max(20, row.Width - 45), Math.Max(14, row.Height - 31));
            TextRenderer.DrawText(g, service.Message ?? string.Empty, _messageFont, messageRect, ZenTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            string latency = Math.Max(0, service.LatencyMilliseconds).ToString() + " ms";
            TextRenderer.DrawText(g, latency, _latencyFont, new Rectangle(row.Right - 86, row.Top + 9, 72, 18), glow,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, StateText(service.State), _messageFont, new Rectangle(row.Right - 110, row.Top + 29, 96, 17), glow,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        private static string StateText(ZenServiceState state)
        {
            switch (state)
            {
                case ZenServiceState.Healthy: return "Healthy";
                case ZenServiceState.Warning: return "Warning";
                case ZenServiceState.Error: return "Error";
                case ZenServiceState.Offline: return "Offline";
                default: return "Unknown";
            }
        }

        private static Color StateColor(ZenServiceState state)
        {
            switch (state)
            {
                case ZenServiceState.Healthy: return ZenTheme.Accent;
                case ZenServiceState.Warning: return Color.FromArgb(255, 190, 75);
                case ZenServiceState.Error: return ZenTheme.Danger;
                case ZenServiceState.Offline: return Color.FromArgb(125, 135, 160);
                default: return ZenTheme.Primary;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _serviceFont.Dispose();
                _messageFont.Dispose();
                _latencyFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake