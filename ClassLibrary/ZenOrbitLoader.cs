using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Animated neon orbit loader with configurable satellites, speed, direction and text.")]
    public class ZenOrbitLoader : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private float _angle;
        private int _satelliteCount = 5;
        private float _orbitSpeed = 2.4f;
        private bool _clockwise = true;
        private bool _indeterminate = true;
        private bool _running;
        private bool _resumeWhenVisible;
        private int _cornerRadius = 14;
        private int _orbitRadius = 25;
        private int _satelliteSize = 7;
        private int _coreSize = 16;
        private int _animationInterval = 16;
        private Color _surfaceColor = ZenTheme.Surface;
        private Color _coreColor = ZenTheme.Primary;
        private Color _satelliteColor = ZenTheme.Accent;
        private Color _textColor = ZenTheme.Foreground;
        private Color _mutedColor = ZenTheme.Muted;

        public ZenOrbitLoader()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(220, 92);
            MinimumSize = new Size(64, 48);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = _animationInterval;
            _timer.Tick += TimerTick;
        }

        [Category("ZenForms Behavior"), Description("Starts or stops the orbit animation."), DefaultValue(false), Browsable(false)]
        public bool IsRunning { get { return _running; } }

        [Category("ZenForms Behavior"), Description("When enabled, the loader continuously orbits its satellites."), DefaultValue(true)]
        public bool Indeterminate
        {
            get { return _indeterminate; }
            set { _indeterminate = value; Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Number of orbiting satellites."), DefaultValue(5)]
        public int SatelliteCount
        {
            get { return _satelliteCount; }
            set { _satelliteCount = Math.Max(1, Math.Min(24, value)); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Orbit speed in revolutions per second."), DefaultValue(2.4f)]
        public float OrbitSpeed
        {
            get { return _orbitSpeed; }
            set { _orbitSpeed = Math.Max(0f, Math.Min(20f, value)); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Direction of satellite travel."), DefaultValue(true)]
        public bool Clockwise
        {
            get { return _clockwise; }
            set { _clockwise = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Background surface of the loader."), DefaultValue(typeof(Color), "Surface")]
        public Color SurfaceColor
        {
            get { return _surfaceColor; }
            set { _surfaceColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Color of the central core."), DefaultValue(typeof(Color), "Primary")]
        public Color CoreColor
        {
            get { return _coreColor; }
            set { _coreColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Color of the orbiting satellites."), DefaultValue(typeof(Color), "Accent")]
        public Color SatelliteColor
        {
            get { return _satelliteColor; }
            set { _satelliteColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Color of the loader text."), DefaultValue(typeof(Color), "Foreground")]
        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Color used for secondary text."), DefaultValue(typeof(Color), "Muted")]
        public Color MutedColor
        {
            get { return _mutedColor; }
            set { _mutedColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Corner radius of the surface."), DefaultValue(14)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, Math.Min(64, value)); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Radius of the satellite orbit."), DefaultValue(25)]
        public int OrbitRadius
        {
            get { return _orbitRadius; }
            set { _orbitRadius = Math.Max(4, Math.Min(500, value)); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Diameter of each satellite."), DefaultValue(7)]
        public int SatelliteSize
        {
            get { return _satelliteSize; }
            set { _satelliteSize = Math.Max(2, Math.Min(40, value)); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Diameter of the central core."), DefaultValue(16)]
        public int CoreSize
        {
            get { return _coreSize; }
            set { _coreSize = Math.Max(4, Math.Min(80, value)); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Timer interval in milliseconds."), DefaultValue(16)]
        public int AnimationInterval
        {
            get { return _animationInterval; }
            set
            {
                _animationInterval = Math.Max(10, Math.Min(100, value));
                if (_timer != null) _timer.Interval = _animationInterval;
            }
        }

        [Category("ZenForms Behavior"), Description("Begin animating the loader.")]
        public void Start()
        {
            if (IsDisposed || _running) return;
            _running = true;
            _resumeWhenVisible = true;
            if (Visible && IsHandleCreated) _timer.Start();
            Invalidate();
        }

        [Category("ZenForms Behavior"), Description("Stop animating the loader.")]
        public void Stop()
        {
            _running = false;
            _resumeWhenVisible = false;
            if (_timer != null) _timer.Stop();
            Invalidate();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (!_running || !Visible || IsDisposed) return;
            float step = _orbitSpeed * 360f * (_timer.Interval / 1000f);
            _angle += _clockwise ? step : -step;
            if (_angle > 360f || _angle < -360f) _angle %= 360f;
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
            {
                _timer.Stop();
            }
            else if (_running && _resumeWhenVisible && IsHandleCreated)
            {
                _timer.Start();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_running && Visible) _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Rectangle bounds = ClientRectangle;
            if (bounds.Width < 2 || bounds.Height < 2) return;
            bounds.Width--; bounds.Height--;

            using (GraphicsPath surfacePath = RoundPath(bounds, _cornerRadius))
            {
                using (SolidBrush surfaceBrush = new SolidBrush(_surfaceColor)) g.FillPath(surfaceBrush, surfacePath);
                using (Pen border = new Pen(Color.FromArgb(90, ZenTheme.Border), 1f)) g.DrawPath(border, surfacePath);
            }

            int textSpace = string.IsNullOrEmpty(Text) ? 0 : Math.Max(0, Math.Min(120, bounds.Width / 2));
            int visualWidth = Math.Max(30, bounds.Width - textSpace - 18);
            int centerX = bounds.Left + Math.Max(18, visualWidth / 2 + 5);
            int centerY = bounds.Top + bounds.Height / 2;
            int radius = Math.Min(_orbitRadius, Math.Max(4, Math.Min(visualWidth, bounds.Height) / 2 - 8));
            float phase = _angle * (float)Math.PI / 180f;

            using (SolidBrush halo = new SolidBrush(Color.FromArgb(26, _coreColor)))
                g.FillEllipse(halo, centerX - _coreSize, centerY - _coreSize, _coreSize * 2, _coreSize * 2);
            using (SolidBrush core = new SolidBrush(_coreColor))
                g.FillEllipse(core, centerX - _coreSize / 2, centerY - _coreSize / 2, _coreSize, _coreSize);
            using (Pen coreHighlight = new Pen(Color.FromArgb(150, Color.White), 1f))
                g.DrawEllipse(coreHighlight, centerX - _coreSize / 2 + 1, centerY - _coreSize / 2 + 1, Math.Max(1, _coreSize - 2), Math.Max(1, _coreSize - 2));

            int count = Math.Max(1, _satelliteCount);
            for (int i = 0; i < count; i++)
            {
                double radians = phase + (Math.PI * 2d * i / count);
                int x = centerX + (int)(Math.Cos(radians) * radius);
                int y = centerY + (int)(Math.Sin(radians) * radius);
                float front = (float)((Math.Sin(radians) + 1d) * .5d);
                int size = Math.Max(2, _satelliteSize + (int)(front * 2f));
                int alpha = 105 + (int)(front * 130f);
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(Math.Min(100, alpha / 3), _satelliteColor)))
                    g.FillEllipse(glow, x - size, y - size, size * 2, size * 2);
                using (SolidBrush satellite = new SolidBrush(Color.FromArgb(alpha, _satelliteColor)))
                    g.FillEllipse(satellite, x - size / 2, y - size / 2, size, size);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Rectangle textRect = new Rectangle(bounds.Left + visualWidth + 7, bounds.Top + 10, Math.Max(1, textSpace - 10), bounds.Height - 20);
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(g, Text, Font, textRect, _textColor, flags);
            }
        }

        private static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            int diameter = Math.Min(Math.Max(0, radius) * 2, Math.Min(r.Width, r.Height));
            if (diameter < 2) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, diameter, diameter, 180, 90);
            path.AddArc(r.Right - diameter, r.Y, diameter, diameter, 270, 90);
            path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(r.X, r.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _running = false;
                _resumeWhenVisible = false;
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Tick -= TimerTick;
                    _timer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake