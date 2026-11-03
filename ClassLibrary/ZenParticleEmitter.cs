using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Animated particle canvas with continuous emission and click explosions.")]
    public class ZenParticleEmitter : Control
    {
        private sealed class Particle
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public float Age;
            public float Life;
            public Color Color;
            public float Size;
        }

        private readonly List<Particle> _particles = new List<Particle>();
        private readonly Timer _timer;
        private readonly Random _random = new Random();
        private float _emitAccumulator;
        private long _lastTick;
        private bool _running;
        private bool _paused;
        private bool _autoStart = true;
        private int _emissionRate = 28;
        private int _particleSpeed = 55;
        private int _gravity;
        private int _particleLife = 1800;
        private int _maxParticles = 180;
        private int _particleSize = 4;
        private int _burstCount = 28;
        private int _cornerRadius = 16;
        private int _connectionDistance = 82;
        private bool _connectParticles;
        private Color _startColor = ZenTheme.Accent;
        private Color _endColor = ZenTheme.Primary;
        private Color _surfaceColor = ZenTheme.Surface;
        private float _elapsedSeconds;

        public ZenParticleEmitter()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(300, 190);
            MinimumSize = new Size(42, 34);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            _timer = new Timer() { Interval = 16 };
            _timer.Tick += TimerTick;
            TabStop = false;
        }

        [Category("ZenForms Behavior"), DefaultValue(true), Description("Starts animation automatically when the control is visible.")]
        public bool AutoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; if (_autoStart) StartAnimation(); else StopAnimation(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(false), Description("Pauses simulation while retaining the current particles.")]
        public bool Paused
        {
            get { return _paused; }
            set { _paused = value; if (_paused) StopAnimation(); else StartAnimation(); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(28), Description("Continuous particles emitted per second.")]
        public int EmissionRate
        {
            get { return _emissionRate; }
            set { _emissionRate = Clamp(value, 0, 1000); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(55), Description("Initial particle speed in pixels per second.")]
        public int ParticleSpeed
        {
            get { return _particleSpeed; }
            set { _particleSpeed = Clamp(value, 0, 1000); }
        }

        [Category("ZenForms Behavior"), DefaultValue(0), Description("Vertical acceleration in pixels per second squared.")]
        public int Gravity
        {
            get { return _gravity; }
            set { _gravity = Clamp(value, -3000, 3000); }
        }

        [Category("ZenForms Behavior"), DefaultValue(1800), Description("Particle lifetime in milliseconds.")]
        public int ParticleLife
        {
            get { return _particleLife; }
            set { _particleLife = Clamp(value, 50, 60000); }
        }

        [Category("ZenForms Behavior"), DefaultValue(180), Description("Maximum number of particles retained by the canvas.")]
        public int MaxParticles
        {
            get { return _maxParticles; }
            set { _maxParticles = Clamp(value, 1, 5000); TrimParticles(); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(4), Description("Base diameter of each particle in pixels.")]
        public int ParticleSize
        {
            get { return _particleSize; }
            set { _particleSize = Clamp(value, 1, 40); }
        }

        [Category("ZenForms Behavior"), DefaultValue(28), Description("Particles created by a click explosion.")]
        public int BurstCount
        {
            get { return _burstCount; }
            set { _burstCount = Clamp(value, 0, 500); }
        }

        [Category("ZenForms Appearance"), DefaultValue(typeof(Color), "42, 211, 190"), Description("Color used when particles are born.")]
        public Color StartColor
        {
            get { return _startColor; }
            set { _startColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(typeof(Color), "124, 92, 255"), Description("Color used as particles approach the end of life.")]
        public Color EndColor
        {
            get { return _endColor; }
            set { _endColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(typeof(Color), "25, 31, 51"), Description("Rounded canvas surface color.")]
        public Color SurfaceColor
        {
            get { return _surfaceColor; }
            set { _surfaceColor = value; BackColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(false), Description("Draws subtle lines between nearby particles.")]
        public bool ConnectParticles
        {
            get { return _connectParticles; }
            set { _connectParticles = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(82), Description("Maximum distance for particle connections.")]
        public int ConnectionDistance
        {
            get { return _connectionDistance; }
            set { _connectionDistance = Clamp(value, 8, 400); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(16), Description("Corner radius of the particle surface.")]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Clamp(value, 0, 80); Invalidate(); }
        }

        [Category("ZenForms Behavior"), Description("Raised after particles are emitted by a click.")]
        public event EventHandler BurstEmitted;

        public void EmitBurst()
        {
            EmitBurst(new PointF(Width * 0.5f, Height * 0.5f));
        }

        public void EmitBurst(Point location)
        {
            EmitBurst(new PointF(location.X, location.Y));
        }

        private void EmitBurst(PointF center)
        {
            if (IsDisposed || Disposing) return;
            int amount = Math.Min(_burstCount, _maxParticles);
            for (int i = 0; i < amount; i++) AddParticle(center, true);
            TrimParticles();
            Invalidate();
            EventHandler handler = BurstEmitted;
            if (handler != null) handler(this, EventArgs.Empty);
            StartAnimation();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left) EmitBurst(e.Location);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            StartAnimation();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) StartAnimation(); else StopAnimation();
        }

        private void StartAnimation()
        {
            if (!_autoStart || _paused || !Visible || IsDisposed || Disposing || !IsHandleCreated) return;
            if (!_running)
            {
                _lastTick = Environment.TickCount;
                _running = true;
                _timer.Start();
            }
        }

        private void StopAnimation()
        {
            _running = false;
            if (_timer != null) _timer.Stop();
            _lastTick = 0;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing || !Visible || _paused)
            {
                StopAnimation();
                return;
            }
            try
            {
                int now = Environment.TickCount;
                int milliseconds = _lastTick == 0 ? 16 : unchecked(now - (int)_lastTick);
                _lastTick = now;
                milliseconds = Clamp(milliseconds, 1, 80);
                float seconds = milliseconds / 1000f;
                _elapsedSeconds += seconds;
                UpdateParticles(seconds);
                Invalidate();
            }
            catch (ObjectDisposedException) { StopAnimation(); }
            catch (InvalidOperationException) { StopAnimation(); }
        }

        private void UpdateParticles(float seconds)
        {
            if (_emissionRate > 0 && _particles.Count < _maxParticles)
            {
                _emitAccumulator += _emissionRate * seconds;
                int count = (int)_emitAccumulator;
                if (count > 0)
                {
                    _emitAccumulator -= count;
                    count = Math.Min(count, _maxParticles - _particles.Count);
                    for (int i = 0; i < count; i++) AddParticle(new PointF(Width * 0.5f, Height * 0.5f), false);
                }
            }
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.Age += seconds * 1000f;
                p.VY += _gravity * seconds;
                p.X += p.VX * seconds;
                p.Y += p.VY * seconds;
                if (p.Age >= p.Life || p.X < -60 || p.X > Width + 60 || p.Y < -60 || p.Y > Height + 60)
                    _particles.RemoveAt(i);
            }
        }

        private void AddParticle(PointF origin, bool burst)
        {
            if (_particles.Count >= _maxParticles) return;
            double angle = burst ? _random.NextDouble() * Math.PI * 2.0 : _random.NextDouble() * Math.PI * 2.0;
            float speed = _particleSpeed * (0.45f + (float)_random.NextDouble() * 0.85f);
            Particle p = new Particle();
            p.X = origin.X;
            p.Y = origin.Y;
            p.VX = (float)Math.Cos(angle) * speed;
            p.VY = (float)Math.Sin(angle) * speed;
            p.Age = 0f;
            p.Life = Math.Max(50, _particleLife * (0.72f + (float)_random.NextDouble() * 0.56f));
            p.Color = _startColor;
            p.Size = _particleSize * (0.65f + (float)_random.NextDouble() * 0.75f);
            _particles.Add(p);
        }

        private void TrimParticles()
        {
            while (_particles.Count > _maxParticles) _particles.RemoveAt(0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(2, 2, Math.Max(0, Width - 5), Math.Max(0, Height - 5));
            if (bounds.Width < 2 || bounds.Height < 2) return;
            using (GraphicsPath path = ZenButtonBase.RoundPath(bounds, _cornerRadius))
            {
                using (SolidBrush background = new SolidBrush(_surfaceColor)) g.FillPath(background, path);
                using (Pen border = new Pen(Color.FromArgb(95, ZenTheme.Border), 1f)) g.DrawPath(border, path);
                GraphicsState state = g.Save();
                g.SetClip(path);
                DrawConnections(g);
                for (int i = 0; i < _particles.Count; i++) DrawParticle(g, _particles[i]);
                g.Restore(state);
            }
        }

        private void DrawConnections(Graphics g)
        {
            if (!_connectParticles || _particles.Count < 2) return;
            float maxDistance = _connectionDistance;
            float maxSquared = maxDistance * maxDistance;
            for (int i = 0; i < _particles.Count; i++)
                for (int j = i + 1; j < _particles.Count; j++)
                {
                    float dx = _particles[i].X - _particles[j].X;
                    float dy = _particles[i].Y - _particles[j].Y;
                    float squared = dx * dx + dy * dy;
                    if (squared <= maxSquared)
                    {
                        int alpha = (int)(54f * (1f - (float)Math.Sqrt(squared) / maxDistance));
                        if (alpha > 0) using (Pen line = new Pen(Color.FromArgb(alpha, ZenTheme.Primary), 1f))
                                g.DrawLine(line, _particles[i].X, _particles[i].Y, _particles[j].X, _particles[j].Y);
                    }
                }
        }

        private void DrawParticle(Graphics g, Particle p)
        {
            float lifeRatio = Clamp(p.Age / Math.Max(1f, p.Life), 0f, 1f);
            float fade = lifeRatio < 0.12f ? lifeRatio / 0.12f : (1f - lifeRatio) / 0.88f;
            fade = Clamp(fade, 0.05f, 1f);
            Color color = Blend(_startColor, _endColor, lifeRatio);
            int alpha = Clamp((int)(220f * fade), 8, 230);
            float diameter = Math.Max(1f, p.Size * (0.8f + 0.2f * fade));
            float x = p.X - diameter * 0.5f;
            float y = p.Y - diameter * 0.5f;
            float glowSize = diameter * 3.5f;
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(Math.Min(48, alpha / 3 + 8), color)))
                g.FillEllipse(glow, p.X - glowSize * 0.5f, p.Y - glowSize * 0.5f, glowSize, glowSize);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, color)))
                g.FillEllipse(brush, x, y, diameter, diameter);
        }

        private static Color Blend(Color first, Color second, float amount)
        {
            amount = Clamp(amount, 0f, 1f);
            return Color.FromArgb(
                (int)(first.A + (second.A - first.A) * amount),
                (int)(first.R + (second.R - first.R) * amount),
                (int)(first.G + (second.G - first.G) * amount),
                (int)(first.B + (second.B - first.B) * amount));
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : (value > maximum ? maximum : value);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return value < minimum ? minimum : (value > maximum ? maximum : value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAnimation();
                if (_timer != null)
                {
                    _timer.Tick -= TimerTick;
                    _timer.Dispose();
                }
                _particles.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake