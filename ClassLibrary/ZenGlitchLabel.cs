using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Label))]
    [Category("ZenForms Modern Components")]
    [Description("Animated neon label with controlled RGB glitch and scanline effects.")]
    public class ZenGlitchLabel : Control
    {
        private readonly Timer _animationTimer;
        private int _intensity = 5;
        private int _frequency = 18;
        private int _channelOffset = 3;
        private bool _scanlines = true;
        private int _scanlineSpacing = 4;
        private bool _animationEnabled = true;
        private int _cornerRadius = 10;
        private Color _surfaceColor = ZenTheme.Surface;
        private Color _glowColor = ZenTheme.Primary;
        private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;
        private float _phase;
        private int _glitchSeed;
        private bool _disposed;

        public ZenGlitchLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(180, 42);
            MinimumSize = new Size(24, 18);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Regular);
            TextAlign = ContentAlignment.MiddleCenter;
            _animationTimer = new Timer();
            _animationTimer.Interval = 33;
            _animationTimer.Tick += AnimationTimer_Tick;
            UpdateTimerState();
        }

        [Category("ZenForms Appearance"), DefaultValue(5)]
        [Description("Maximum RGB displacement and visual distortion in pixels.")]
        public int Intensity
        {
            get { return _intensity; }
            set { _intensity = Clamp(value, 0, 24); UpdateTimerState(); Invalidate(); }
        }

        [Category("ZenForms Animation"), DefaultValue(18)]
        [Description("Approximate number of glitch updates per second.")]
        public int Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = Clamp(value, 1, 60);
                _animationTimer.Interval = Math.Max(16, 1000 / _frequency);
                UpdateTimerState();
            }
        }

        [Category("ZenForms Appearance"), DefaultValue(3)]
        [Description("RGB channel separation in pixels.")]
        public int ChannelOffset
        {
            get { return _channelOffset; }
            set { _channelOffset = Clamp(value, 0, 16); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        [Description("Draws subtle horizontal scanlines over the label.")]
        public bool Scanlines
        {
            get { return _scanlines; }
            set { _scanlines = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(4)]
        [Description("Distance in pixels between scanlines.")]
        public int ScanlineSpacing
        {
            get { return _scanlineSpacing; }
            set { _scanlineSpacing = Clamp(value, 2, 16); Invalidate(); }
        }

        [Category("ZenForms Animation"), DefaultValue(true)]
        [Description("Enables the animated glitch effect.")]
        public bool AnimationEnabled
        {
            get { return _animationEnabled; }
            set { _animationEnabled = value; UpdateTimerState(); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(10)]
        [Description("Corner radius of the dark rounded surface.")]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Background surface used behind the label text.")]
        public Color SurfaceColor
        {
            get { return _surfaceColor; }
            set { _surfaceColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Accent used for the neon edge and RGB channels.")]
        public Color GlowColor
        {
            get { return _glowColor; }
            set { _glowColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(ContentAlignment.MiddleCenter)]
        [Description("Alignment of the label text inside the rounded surface.")]
        public ContentAlignment TextAlign
        {
            get { return _textAlign; }
            set { _textAlign = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue("")]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value ?? String.Empty; UpdateTimerState(); Invalidate(); }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            UpdateTimerState();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimerState();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateTimerState();
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Rectangle bounds = ClientRectangle;
            if (bounds.Width < 2 || bounds.Height < 2)
                return;

            bounds.Width = Math.Max(1, bounds.Width - 1);
            bounds.Height = Math.Max(1, bounds.Height - 1);
            int radius = Math.Min(_cornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);
            using (GraphicsPath path = CreateRoundPath(bounds, radius))
            {
                using (SolidBrush background = new SolidBrush(_surfaceColor))
                    g.FillPath(background, path);

                using (Pen border = new Pen(Color.FromArgb(90, _glowColor), 1f))
                    g.DrawPath(border, path);

                GraphicsState state = g.Save();
                g.SetClip(path);
                DrawTextLayers(g, bounds);
                if (_scanlines)
                    DrawScanlines(g, bounds);
                g.Restore(state);
            }
        }

        private void DrawTextLayers(Graphics g, Rectangle bounds)
        {
            string value = Text ?? String.Empty;
            if (value.Length == 0 || Font == null)
                return;

            Rectangle textBounds = new Rectangle(bounds.X + 7, bounds.Y + 3,
                Math.Max(1, bounds.Width - 14), Math.Max(1, bounds.Height - 6));
            StringFormat format = CreateStringFormat();
            bool active = _animationEnabled && Enabled && _intensity > 0;
            int displacement = active ? GetDisplacement() : 0;
            int separation = active ? Math.Min(16, _channelOffset + displacement / 2) : 0;

            if (separation > 0)
            {
                DrawString(g, value, textBounds, format, Color.FromArgb(125, 255, 55, 105), -separation, 0);
                DrawString(g, value, textBounds, format, Color.FromArgb(120, 55, 220, 255), separation, 0);
            }

            Color main = Enabled ? ForeColor : ZenTheme.Muted;
            DrawString(g, value, textBounds, format, Color.FromArgb(55, _glowColor), 0, 1);
            DrawString(g, value, textBounds, format, main, 0, 0);
            format.Dispose();
        }

        private void DrawString(Graphics g, string value, Rectangle bounds, StringFormat format, Color color, int x, int y)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                RectangleF target = new RectangleF(bounds.X + x, bounds.Y + y, bounds.Width, bounds.Height);
                g.DrawString(value, Font, brush, target, format);
            }
        }

        private void DrawScanlines(Graphics g, Rectangle bounds)
        {
            int spacing = Math.Max(2, _scanlineSpacing);
            int alpha = Enabled ? 22 : 13;
            using (Pen line = new Pen(Color.FromArgb(alpha, Color.Black), 1f))
            {
                for (int y = bounds.Top + 2; y < bounds.Bottom; y += spacing)
                    g.DrawLine(line, bounds.Left, y, bounds.Right, y);
            }
        }

        private StringFormat CreateStringFormat()
        {
            StringFormat format = new StringFormat(StringFormat.GenericDefault);
            format.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;
            format.Trimming = StringTrimming.EllipsisCharacter;
            if (_textAlign == ContentAlignment.TopLeft || _textAlign == ContentAlignment.MiddleLeft || _textAlign == ContentAlignment.BottomLeft)
                format.Alignment = StringAlignment.Near;
            else if (_textAlign == ContentAlignment.TopRight || _textAlign == ContentAlignment.MiddleRight || _textAlign == ContentAlignment.BottomRight)
                format.Alignment = StringAlignment.Far;
            else
                format.Alignment = StringAlignment.Center;

            if (_textAlign == ContentAlignment.TopLeft || _textAlign == ContentAlignment.TopCenter || _textAlign == ContentAlignment.TopRight)
                format.LineAlignment = StringAlignment.Near;
            else if (_textAlign == ContentAlignment.BottomLeft || _textAlign == ContentAlignment.BottomCenter || _textAlign == ContentAlignment.BottomRight)
                format.LineAlignment = StringAlignment.Far;
            else
                format.LineAlignment = StringAlignment.Center;
            return format;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_disposed || !ShouldAnimate())
            {
                UpdateTimerState();
                return;
            }
            _phase += 0.31f;
            _glitchSeed = unchecked((_glitchSeed * 1103515245) + 12345);
            Invalidate();
        }

        private int GetDisplacement()
        {
            int value = Math.Abs(_glitchSeed % (_intensity + 1));
            if (value == 0 && ((int)(_phase * 10f) % 7) == 0)
                value = Math.Min(_intensity, 1);
            return value;
        }

        private bool ShouldAnimate()
        {
            return !_disposed && IsHandleCreated && Visible && Enabled && _animationEnabled && _intensity > 0 && Text.Length > 0;
        }

        private void UpdateTimerState()
        {
            if (_animationTimer == null || _disposed)
                return;
            bool run = ShouldAnimate();
            if (run && !_animationTimer.Enabled)
                _animationTimer.Start();
            else if (!run && _animationTimer.Enabled)
                _animationTimer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_animationTimer != null)
                {
                    _animationTimer.Stop();
                    _animationTimer.Tick -= AnimationTimer_Tick;
                    _animationTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static GraphicsPath CreateRoundPath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                return path;
            int diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
            if (diameter < 2)
            {
                path.AddRectangle(rectangle);
                return path;
            }
            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

// watashi wa watashi sore dake
