using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Horizontal neon spring slider with smooth physical animation.")]
    [ToolboxBitmap(typeof(TrackBar))]
    public class ZenSpringSlider : Control
    {
        private double _minimum = 0.0;
        private double _maximum = 100.0;
        private double _value;
        private double _visualValue;
        private double _velocity;
        private bool _dragging;
        private bool _hovered;
        private bool _settled = true;
        private Timer _animationTimer;
        private Color _trackColor = Color.FromArgb(55, 65, 91);
        private Color _accentColor = ZenTheme.Accent;
        private Color _thumbColor = ZenTheme.Foreground;
        private Color _textColor = ZenTheme.Foreground;
        private bool _showValueLabel = true;
        private bool _showMinMaxLabels;
        private bool _showTrackGlow = true;
        private int _cornerRadius = 9;
        private int _trackHeight = 6;
        private int _thumbRadius = 8;
        private int _sidePadding = 14;
        private double _springStrength = 0.22;
        private double _damping = 0.78;
        private double _step = 1.0;
        private string _valueFormat = "0.##";

        public ZenSpringSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(260, 58);
            MinimumSize = new Size(52, 32);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            TabStop = true;
            Cursor = Cursors.Hand;
            _animationTimer = new Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [Category("ZenForms Behavior"), DefaultValue(0.0)]
        public double Minimum { get { return _minimum; } set { SetRange(value, _maximum); } }

        [Category("ZenForms Behavior"), DefaultValue(100.0)]
        public double Maximum { get { return _maximum; } set { SetRange(_minimum, value); } }

        [Category("ZenForms Behavior"), DefaultValue(0.0)]
        public double Value
        {
            get { return _value; }
            set { SetValue(value, true); }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double VisualValue { get { return _visualValue; } }

        [Category("ZenForms Behavior"), DefaultValue(1.0)]
        public double Step { get { return _step; } set { _step = Math.Max(0.000001, value); } }

        [Category("ZenForms Behavior"), DefaultValue("0.##")]
        public string ValueFormat { get { return _valueFormat; } set { _valueFormat = value ?? "0.##"; Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        public bool ShowValueLabel { get { return _showValueLabel; } set { _showValueLabel = value; Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(false)]
        public bool ShowMinMaxLabels { get { return _showMinMaxLabels; } set { _showMinMaxLabels = value; Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        public bool ShowTrackGlow { get { return _showTrackGlow; } set { _showTrackGlow = value; Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(9)]
        public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = Math.Max(0, value); Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(6)]
        public int TrackHeight { get { return _trackHeight; } set { _trackHeight = Math.Max(2, value); Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(8)]
        public int ThumbRadius { get { return _thumbRadius; } set { _thumbRadius = Math.Max(4, value); Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(14)]
        public int SidePadding { get { return _sidePadding; } set { _sidePadding = Math.Max(4, value); Invalidate(); } }

        [Category("ZenForms Appearance"), DefaultValue(0.22)]
        public double SpringStrength { get { return _springStrength; } set { _springStrength = Math.Max(0.01, Math.Min(1.0, value)); } }

        [Category("ZenForms Appearance"), DefaultValue(0.78)]
        public double Damping { get { return _damping; } set { _damping = Math.Max(0.1, Math.Min(0.99, value)); } }

        [Category("ZenForms Appearance")]
        public Color TrackColor { get { return _trackColor; } set { _trackColor = value; Invalidate(); } }

        [Category("ZenForms Appearance")]
        public Color AccentColor { get { return _accentColor; } set { _accentColor = value; Invalidate(); } }

        [Category("ZenForms Appearance")]
        public Color ThumbColor { get { return _thumbColor; } set { _thumbColor = value; Invalidate(); } }

        [Category("ZenForms Appearance")]
        public Color TextColor { get { return _textColor; } set { _textColor = value; Invalidate(); } }

        public event EventHandler ValueChanged;
        public event EventHandler AnimationCompleted;

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            EnsureTimerState();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            EnsureTimerState();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_dragging) _hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            _dragging = true;
            Capture = true;
            SetValue(ValueFromPoint(e.X), true);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging && e.Button == MouseButtons.Left) SetValue(ValueFromPoint(e.X), true);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = false;
                Capture = false;
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            double amount = _step;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { SetValue(_value - amount, true); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { SetValue(_value + amount, true); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Home) { SetValue(_minimum, true); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.End) { SetValue(_maximum, true); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.PageDown) { SetValue(_value - (_maximum - _minimum) * 0.1, true); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.PageUp) { SetValue(_value + (_maximum - _minimum) * 0.1, true); e.Handled = true; e.SuppressKeyPress = true; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int left = Math.Max(4, _sidePadding);
            int right = Math.Max(left + 1, Width - _sidePadding);
            int labelHeight = _showValueLabel ? 18 : 0;
            int trackY = Math.Max(ThumbRadius + 2, (Height + labelHeight) / 2);
            int trackWidth = Math.Max(1, right - left);
            double range = _maximum - _minimum;
            double fraction = range <= 0.0 ? 0.0 : (_visualValue - _minimum) / range;
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            int thumbX = left + (int)Math.Round(trackWidth * fraction);
            int h = Math.Max(2, Math.Min(_trackHeight, Height));
            Rectangle track = new Rectangle(left, trackY - h / 2, trackWidth, h);
            using (GraphicsPath trackPath = RoundPath(track, Math.Min(_cornerRadius, h / 2)))
            using (SolidBrush trackBrush = new SolidBrush(_trackColor)) g.FillPath(trackBrush, trackPath);
            int filledWidth = Math.Max(0, thumbX - left);
            if (filledWidth > 0)
            {
                Rectangle fill = new Rectangle(left, track.Y, filledWidth, h);
                using (GraphicsPath fillPath = RoundPath(fill, Math.Min(_cornerRadius, h / 2)))
                {
                    if (_showTrackGlow && (_hovered || _dragging)) using (Pen glow = new Pen(Color.FromArgb(48, _accentColor), Math.Max(4, h + 5))) g.DrawPath(glow, fillPath);
                    using (SolidBrush accent = new SolidBrush(_accentColor)) g.FillPath(accent, fillPath);
                }
            }
            int tr = _thumbRadius + (_hovered || _dragging ? 2 : 0);
            if (_showTrackGlow) using (Pen glow = new Pen(Color.FromArgb(_hovered || _dragging ? 90 : 38, _accentColor), tr + 7)) g.DrawEllipse(glow, thumbX - tr, trackY - tr, tr * 2, tr * 2);
            using (SolidBrush thumb = new SolidBrush(_thumbColor)) g.FillEllipse(thumb, thumbX - tr, trackY - tr, tr * 2, tr * 2);
            using (Pen thumbBorder = new Pen(_accentColor, 1.2f)) g.DrawEllipse(thumbBorder, thumbX - tr, trackY - tr, tr * 2, tr * 2);
            if (_showValueLabel)
            {
                string text = FormatValue(_visualValue);
                Rectangle valueRect = new Rectangle(0, 2, Width, Math.Max(14, labelHeight));
                TextRenderer.DrawText(g, text, Font, valueRect, _textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            if (_showMinMaxLabels)
            {
                string minText = FormatValue(_minimum), maxText = FormatValue(_maximum);
                int y = Math.Min(Height - 16, trackY + tr + 3);
                TextRenderer.DrawText(g, minText, Font, new Rectangle(left, y, Math.Max(1, trackWidth / 2), 16), ZenTheme.Muted, TextFormatFlags.Left | TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, maxText, Font, new Rectangle(left + trackWidth / 2, y, Math.Max(1, trackWidth / 2), 16), ZenTheme.Muted, TextFormatFlags.Right | TextFormatFlags.SingleLine);
            }
        }

        private void SetRange(double minimum, double maximum)
        {
            if (double.IsNaN(minimum) || double.IsInfinity(minimum)) minimum = 0.0;
            if (double.IsNaN(maximum) || double.IsInfinity(maximum)) maximum = minimum + 1.0;
            if (maximum <= minimum) maximum = minimum + 1.0;
            _minimum = minimum;
            _maximum = maximum;
            SetValue(_value, false);
            Invalidate();
        }

        private void SetValue(double value, bool raiseEvent)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) value = _minimum;
            value = Math.Max(_minimum, Math.Min(_maximum, value));
            if (Math.Abs(value - _value) < 0.0000001) { EnsureTimerState(); return; }
            _value = value;
            _settled = false;
            if (raiseEvent && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            EnsureTimerState();
            Invalidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            double difference = _value - _visualValue;
            _velocity = (_velocity + difference * _springStrength) * _damping;
            _visualValue += _velocity;
            if (Math.Abs(difference) < 0.0005 && Math.Abs(_velocity) < 0.0005)
            {
                _visualValue = _value;
                _velocity = 0.0;
                if (!_settled)
                {
                    _settled = true;
                    if (AnimationCompleted != null) AnimationCompleted(this, EventArgs.Empty);
                }
                _animationTimer.Stop();
            }
            Invalidate();
        }

        private void EnsureTimerState()
        {
            if (_animationTimer == null || IsDisposed || !IsHandleCreated || !Visible) return;
            if (Math.Abs(_value - _visualValue) > 0.0005 || Math.Abs(_velocity) > 0.0005) _animationTimer.Start();
            else _animationTimer.Stop();
        }

        private double ValueFromPoint(int x)
        {
            int left = Math.Max(4, _sidePadding), right = Math.Max(left + 1, Width - _sidePadding);
            double fraction = (x - left) / (double)Math.Max(1, right - left);
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            return _minimum + (_maximum - _minimum) * fraction;
        }

        private string FormatValue(double value)
        {
            try { return value.ToString(_valueFormat, System.Globalization.CultureInfo.CurrentCulture); }
            catch (FormatException) { return value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture); }
        }

        private static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            int d = Math.Min(Math.Max(0, radius) * 2, Math.Min(r.Width, r.Height));
            if (d < 2) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Tick -= AnimationTimer_Tick;
                _animationTimer.Dispose();
                _animationTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake