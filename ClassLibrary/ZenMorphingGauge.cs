using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Animated neon circular gauge with smooth morphing progress.")]
    public class ZenMorphingGauge : Control
    {
        private readonly Timer _animationTimer;
        private float _minimum;
        private float _maximum = 100f;
        private float _value;
        private float _targetValue;
        private float _animationFrom;
        private int _animationStartTick;
        private bool _animating;
        private bool _disposed;
        private int _animationDuration = 850;
        private int _strokeWidth = 10;
        private int _cornerRadius = 18;
        private string _centerText = "{0:0}%";
        private bool _showCenterText = true;
        private bool _showTarget;
        private Color _trackColor = Color.FromArgb(55, ZenTheme.Border);
        private Color _progressColor = ZenTheme.Accent;
        private Color _glowColor = ZenTheme.Primary;
        private Color _centerTextColor = ZenTheme.Foreground;
        private Color _labelColor = ZenTheme.Muted;
        private string _label = "PROGRESS";
        private bool _pulseGlow = true;

        public ZenMorphingGauge()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(190, 190);
            MinimumSize = new Size(48, 48);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            _animationTimer = new Timer { Interval = 16 };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [Category("ZenForms Behavior"), DefaultValue(0f)]
        public float Minimum { get { return _minimum; } set { SetRange(value, _maximum); } }

        [Category("ZenForms Behavior"), DefaultValue(100f)]
        public float Maximum { get { return _maximum; } set { SetRange(_minimum, value); } }

        [Category("ZenForms Behavior"), DefaultValue(0f)]
        public float Value
        {
            get { return _value; }
            set { SetValue(value, false); }
        }

        [Category("ZenForms Behavior"), DefaultValue(0f)]
        public float TargetValue
        {
            get { return _targetValue; }
            set { _targetValue = Clamp(value); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(850)]
        public int AnimationDuration
        {
            get { return _animationDuration; }
            set { _animationDuration = Math.Max(1, value); }
        }

        [Category("ZenForms Appearance"), DefaultValue(10)]
        public int StrokeWidth
        {
            get { return _strokeWidth; }
            set { _strokeWidth = Math.Max(1, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue("{0:0}%")]
        public string CenterText
        {
            get { return _centerText; }
            set { _centerText = value ?? ""; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        public bool ShowCenterText
        {
            get { return _showCenterText; }
            set { _showCenterText = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(false)]
        public bool ShowTarget
        {
            get { return _showTarget; }
            set { _showTarget = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue("PROGRESS")]
        public string Label
        {
            get { return _label; }
            set { _label = value ?? ""; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        public bool PulseGlow
        {
            get { return _pulseGlow; }
            set { _pulseGlow = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color TrackColor
        {
            get { return _trackColor; }
            set { _trackColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color ProgressColor
        {
            get { return _progressColor; }
            set { _progressColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color GlowColor
        {
            get { return _glowColor; }
            set { _glowColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color CenterTextColor
        {
            get { return _centerTextColor; }
            set { _centerTextColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color LabelColor
        {
            get { return _labelColor; }
            set { _labelColor = value; Invalidate(); }
        }

        public event EventHandler AnimationCompleted;

        public bool IsAnimating { get { return _animating; } }

        public void StartAnimation()
        {
            StartAnimation(_targetValue);
        }

        public void StartAnimation(float target)
        {
            if (_disposed) return;
            _targetValue = Clamp(target);
            _animationFrom = _value;
            _animationStartTick = Environment.TickCount;
            _animating = Math.Abs(_targetValue - _value) > 0.001f;
            if (_animating && Visible) _animationTimer.Start();
            else if (!_animating) Invalidate();
        }

        public void StopAnimation()
        {
            if (_disposed) return;
            _animationTimer.Stop();
            _animating = false;
            Invalidate();
        }

        private void SetRange(float minimum, float maximum)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum)) minimum = 0f;
            if (float.IsNaN(maximum) || float.IsInfinity(maximum)) maximum = minimum + 1f;
            if (maximum <= minimum) maximum = minimum + 1f;
            _minimum = minimum;
            _maximum = maximum;
            _value = Clamp(_value);
            _targetValue = Clamp(_targetValue);
            Invalidate();
        }

        private void SetValue(float value, bool animate)
        {
            value = Clamp(value);
            if (animate) StartAnimation(value);
            else { _value = value; _targetValue = value; _animating = false; _animationTimer.Stop(); Invalidate(); }
        }

        private float Clamp(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return _minimum;
            return Math.Max(_minimum, Math.Min(_maximum, value));
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_disposed || !_animating) { _animationTimer.Stop(); return; }
            int elapsed = unchecked(Environment.TickCount - _animationStartTick);
            float progress = Math.Min(1f, Math.Max(0f, elapsed / (float)Math.Max(1, _animationDuration)));
            float eased = 1f - (float)Math.Pow(1f - progress, 3f);
            _value = _animationFrom + ((_targetValue - _animationFrom) * eased);
            Invalidate();
            if (progress >= 1f)
            {
                _value = _targetValue;
                _animating = false;
                _animationTimer.Stop();
                OnAnimationCompleted(EventArgs.Empty);
            }
        }

        protected virtual void OnAnimationCompleted(EventArgs e)
        {
            EventHandler handler = AnimationCompleted;
            if (handler != null) handler(this, e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
            {
                _animationTimer.Stop();
            }
            else if (_animating && !_disposed)
            {
                _animationStartTick = Environment.TickCount;
                _animationTimer.Start();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (Width < 2 || Height < 2) return;

            Rectangle surface = new Rectangle(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
            using (GraphicsPath surfacePath = ZenButtonBase.RoundPath(surface, _cornerRadius))
            {
                using (SolidBrush background = new SolidBrush(BackColor == Color.Empty ? ZenTheme.Surface : BackColor))
                    g.FillPath(background, surfacePath);
                using (Pen border = new Pen(Color.FromArgb(110, ZenTheme.Border), 1f))
                    g.DrawPath(border, surfacePath);
            }

            int side = Math.Min(Width, Height);
            int inset = Math.Max(12, side / 8);
            int diameter = Math.Max(2, side - inset * 2);
            Rectangle arc = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
            float range = Math.Max(0.0001f, _maximum - _minimum);
            float fraction = Math.Max(0f, Math.Min(1f, (_value - _minimum) / range));
            float sweep = fraction * 360f;
            float pulse = _pulseGlow && _animating ? (float)((Math.Sin(Environment.TickCount / 135.0) + 1.0) * 0.5) : 0f;
            float center = Math.Min(arc.Width, arc.Height) / 2f;
            float width = Math.Min(_strokeWidth, Math.Max(1f, center - 2f));

            using (Pen track = new Pen(_trackColor, width))
            {
                track.StartCap = LineCap.Round; track.EndCap = LineCap.Round;
                g.DrawArc(track, arc, -90f, 359.5f);
            }
            if (sweep > 0.01f)
            {
                using (Pen glowWide = new Pen(Color.FromArgb((int)(38 + pulse * 35), _glowColor), width + 7f))
                using (Pen glow = new Pen(Color.FromArgb((int)(80 + pulse * 45), _glowColor), width + 3f))
                using (Pen progressPen = new Pen(_progressColor, width))
                {
                    glowWide.StartCap = glow.StartCap = progressPen.StartCap = LineCap.Round;
                    glowWide.EndCap = glow.EndCap = progressPen.EndCap = LineCap.Round;
                    g.DrawArc(glowWide, arc, -90f, sweep);
                    g.DrawArc(glow, arc, -90f, sweep);
                    g.DrawArc(progressPen, arc, -90f, sweep);
                }
            }

            if (_showCenterText && diameter > 24)
            {
                string text = FormatCenterText();
                Rectangle textRect = new Rectangle(arc.Left + 3, arc.Top + arc.Height / 3, Math.Max(1, arc.Width - 6), Math.Max(1, arc.Height / 3));
                using (Font valueFont = new Font("Segoe UI Semibold", Math.Max(8f, Math.Min(24f, diameter / 4.5f)), FontStyle.Bold))
                    TextRenderer.DrawText(g, text, valueFont, textRect, _centerTextColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
            if (!string.IsNullOrEmpty(_label) && diameter > 54)
            {
                Rectangle labelRect = new Rectangle(arc.Left + 5, arc.Top + arc.Height / 2 + 13, Math.Max(1, arc.Width - 10), 22);
                TextRenderer.DrawText(g, _label, Font, labelRect, _labelColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
            if (_showTarget && diameter > 70)
            {
                string targetText = "Target " + FormatNumber(_targetValue);
                Rectangle targetRect = new Rectangle(arc.Left + 5, arc.Bottom - 27, Math.Max(1, arc.Width - 10), 18);
                TextRenderer.DrawText(g, targetText, Font, targetRect, ZenTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
        }

        private string FormatCenterText()
        {
            try { return string.Format(_centerText ?? "", _value); }
            catch (FormatException) { return FormatNumber(_value); }
        }

        private string FormatNumber(float number)
        {
            return number.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _animating = false;
                if (_animationTimer != null)
                {
                    _animationTimer.Stop();
                    _animationTimer.Tick -= AnimationTimer_Tick;
                    _animationTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake