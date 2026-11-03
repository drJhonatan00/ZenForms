using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Container with an animated Zen aurora of gradients and softly moving light blobs.")]
    [ToolboxBitmap(typeof(Panel))]
    public class ZenAuroraPanel : Control
    {
        private readonly Timer _animationTimer;
        private readonly List<PointF> _blobSeeds = new List<PointF>();
        private float _phase;
        private bool _animationEnabled = true;
        private bool _timerSuspended;
        private int _cornerRadius = 18;
        private int _intensity = 72;
        private int _blobCount = 4;
        private float _animationSpeed = 1.0f;
        private int _animationInterval = 33;
        private Color _primaryColor;
        private Color _accentColor;

        public ZenAuroraPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(320, 190);
            MinimumSize = new Size(48, 36);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            _primaryColor = ZenTheme.Primary;
            _accentColor = ZenTheme.Accent;

            _animationTimer = new Timer();
            _animationTimer.Interval = _animationInterval;
            _animationTimer.Tick += AnimationTimer_Tick;
            EnsureBlobSeeds();
        }

        [Category("ZenForms Appearance"), Description("Radius of the rounded panel surface."), DefaultValue(18)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Overall opacity of the aurora, from 0 to 100."), DefaultValue(72)]
        public int Intensity
        {
            get { return _intensity; }
            set { _intensity = Clamp(value, 0, 100); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Number of moving light blobs."), DefaultValue(4)]
        public int BlobCount
        {
            get { return _blobCount; }
            set { _blobCount = Clamp(value, 0, 8); EnsureBlobSeeds(); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Whether the aurora animation is active."), DefaultValue(true)]
        public bool AnimationEnabled
        {
            get { return _animationEnabled; }
            set
            {
                _animationEnabled = value;
                UpdateTimerState();
                Invalidate();
            }
        }

        [Category("ZenForms Behavior"), Description("Animation multiplier. Values are limited to a practical range."), DefaultValue(1.0f)]
        public float AnimationSpeed
        {
            get { return _animationSpeed; }
            set { _animationSpeed = Clamp(value, 0.05f, 4.0f); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Timer interval in milliseconds used for animation updates."), DefaultValue(33)]
        public int AnimationInterval
        {
            get { return _animationInterval; }
            set
            {
                _animationInterval = Clamp(value, 16, 100);
                if (_animationTimer != null) _animationTimer.Interval = _animationInterval;
            }
        }

        [Category("ZenForms Appearance"), Description("Primary neon color used by the aurora."), DefaultValue(typeof(Color), "124, 92, 255")]
        public Color PrimaryColor
        {
            get { return _primaryColor; }
            set { _primaryColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Accent neon color used by the aurora."), DefaultValue(typeof(Color), "42, 211, 190")]
        public Color AccentColor
        {
            get { return _accentColor; }
            set { _accentColor = value; Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Raised after each animation update.")]
        public event EventHandler AnimationTick;

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            UpdateRegion();
            UpdateTimerState();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            _timerSuspended = !Visible;
            UpdateTimerState();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
            EnsureBlobSeeds();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = CreateRoundPath(bounds, _cornerRadius))
            {
                GraphicsState state = g.Save();
                g.SetClip(path);
                using (SolidBrush baseBrush = new SolidBrush(BackColor)) g.FillRectangle(baseBrush, ClientRectangle);
                PaintAurora(g, bounds);
                g.Restore(state);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = CreateRoundPath(bounds, _cornerRadius))
            using (Pen border = new Pen(Color.FromArgb(125, ZenTheme.Border), 1f))
            {
                border.Alignment = PenAlignment.Inset;
                g.DrawPath(border, path);
            }
        }

        private void PaintAurora(Graphics g, Rectangle bounds)
        {
            if (_intensity <= 0 || bounds.Width < 2 || bounds.Height < 2) return;
            EnsureBlobSeeds();
            float opacity = _intensity / 100.0f;
            using (LinearGradientBrush wash = new LinearGradientBrush(bounds,
                Color.FromArgb((int)(30 * opacity), _primaryColor),
                Color.FromArgb((int)(18 * opacity), _accentColor), 28f))
            {
                g.FillRectangle(wash, bounds);
            }

            for (int i = 0; i < _blobCount && i < _blobSeeds.Count; i++)
            {
                PointF seed = _blobSeeds[i];
                float t = _phase + i * 1.71f;
                float x = bounds.Left + seed.X * bounds.Width + (float)Math.Sin(t * (0.72f + i * 0.04f)) * bounds.Width * 0.13f;
                float y = bounds.Top + seed.Y * bounds.Height + (float)Math.Cos(t * (0.58f + i * 0.03f)) * bounds.Height * 0.16f;
                float size = Math.Max(22f, Math.Min(bounds.Width, bounds.Height) * (0.52f + (i % 3) * 0.08f));
                Color color = (i % 2 == 0) ? _primaryColor : _accentColor;
                DrawGlow(g, x, y, size, color, opacity);
            }
        }

        private static void DrawGlow(Graphics g, float x, float y, float size, Color color, float opacity)
        {
            int layers = 6;
            for (int i = layers; i >= 1; i--)
            {
                float factor = i / (float)layers;
                float diameter = size * factor;
                int alpha = (int)(Math.Max(1, 42f * opacity) * (1f - factor * 0.55f));
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, color)))
                {
                    g.FillEllipse(brush, x - diameter / 2f, y - diameter / 2f, diameter, diameter);
                }
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!_animationEnabled || _timerSuspended || IsDisposed || !IsHandleCreated) return;
            _phase += 0.045f * _animationSpeed;
            if (_phase > 10000f) _phase -= 10000f;
            Invalidate();
            EventHandler handler = AnimationTick;
            if (handler != null)
            {
                try { handler(this, EventArgs.Empty); }
                catch { }
            }
        }

        private void UpdateTimerState()
        {
            if (_animationTimer == null || IsDisposed) return;
            bool shouldRun = _animationEnabled && !_timerSuspended && Visible && IsHandleCreated && !DesignMode;
            if (shouldRun)
            {
                if (!_animationTimer.Enabled) _animationTimer.Start();
            }
            else if (_animationTimer.Enabled)
            {
                _animationTimer.Stop();
            }
        }

        private void EnsureBlobSeeds()
        {
            while (_blobSeeds.Count < _blobCount)
            {
                int i = _blobSeeds.Count;
                _blobSeeds.Add(new PointF(0.16f + (i * 0.29f) % 0.72f, 0.18f + (i * 0.43f) % 0.66f));
            }
            while (_blobSeeds.Count > _blobCount) _blobSeeds.RemoveAt(_blobSeeds.Count - 1);
        }

        private void UpdateRegion()
        {
            if (!IsHandleCreated || IsDisposed || Width <= 0 || Height <= 0) return;
            try
            {
                using (GraphicsPath path = CreateRoundPath(new Rectangle(0, 0, Width, Height), _cornerRadius))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            }
            catch { }
        }

        private static GraphicsPath CreateRoundPath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rectangle.Width <= 0 || rectangle.Height <= 0) return path;
            int diameter = Math.Min(Math.Max(0, radius) * 2, Math.Min(rectangle.Width, rectangle.Height));
            if (diameter < 2) { path.AddRectangle(rectangle); return path; }
            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return minimum;
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Tick -= AnimationTimer_Tick;
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake