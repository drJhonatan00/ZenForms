using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Transparent animated confetti celebration overlay for forms and user controls.")]
    public class ZenConfettiCelebration : Control
    {
        private sealed class ConfettiParticle
        {
            public float X;
            public float Y;
            public float VelocityX;
            public float VelocityY;
            public float Rotation;
            public float RotationSpeed;
            public float Width;
            public float Height;
            public int ColorIndex;
            public bool Circle;
        }

        private readonly Timer _timer;
        private readonly List<ConfettiParticle> _particles = new List<ConfettiParticle>();
        private readonly Random _random = new Random();
        private Color[] _palette;
        private int _quantity = 90;
        private float _gravity = 420f;
        private int _duration = 2400;
        private int _timerInterval = 20;
        private string _celebrationText = "Celebration!";
        private bool _playing;
        private long _startedAt;
        private long _lastTick;
        private float _progress;
        private bool _disposed;

        public ZenConfettiCelebration()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold);
            Size = new Size(420, 260);
            MinimumSize = new Size(24, 24);
            _palette = new Color[] { ZenTheme.Primary, ZenTheme.Accent, ZenTheme.Danger, Color.FromArgb(255, 205, 92) };
            _timer = new Timer();
            _timer.Interval = _timerInterval;
            _timer.Tick += TimerTick;
            TabStop = false;
        }

        [Category("ZenForms Behavior"), DefaultValue(90)]
        [Description("Number of confetti pieces emitted when Play is called.")]
        public int Quantity
        {
            get { return _quantity; }
            set { _quantity = Clamp(value, 0, 600); if (_playing) RestartParticles(); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(420f)]
        [Description("Downward acceleration applied to each confetti piece, in pixels per second squared.")]
        public float Gravity
        {
            get { return _gravity; }
            set { _gravity = Clamp(value, 0f, 3000f); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(2400)]
        [Description("Celebration duration in milliseconds.")]
        public int Duration
        {
            get { return _duration; }
            set { _duration = Clamp(value, 100, 60000); Invalidate(); }
        }

        [Category("ZenForms Appearance"), Description("Colors cycled through by the confetti pieces.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Color[] Palette
        {
            get { return (Color[])_palette.Clone(); }
            set
            {
                if (value == null || value.Length == 0)
                    return;
                List<Color> colors = new List<Color>();
                for (int i = 0; i < value.Length; i++)
                    colors.Add(value[i]);
                _palette = colors.ToArray();
                Invalidate();
            }
        }

        [Category("ZenForms Appearance"), DefaultValue("Celebration!")]
        [Description("Text displayed in the center of the overlay while celebrating.")]
        public string CelebrationText
        {
            get { return _celebrationText; }
            set { _celebrationText = value ?? string.Empty; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(20)]
        [Description("Animation timer interval in milliseconds. Values below 15 are raised to 15.")]
        public int TimerInterval
        {
            get { return _timerInterval; }
            set
            {
                _timerInterval = Clamp(value, 15, 100);
                if (_timer != null) _timer.Interval = _timerInterval;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPlaying { get { return _playing; } }

        public event EventHandler Completed;

        public void Play()
        {
            if (_disposed) return;
            try
            {
                _playing = true;
                _progress = 0f;
                _startedAt = Environment.TickCount;
                _lastTick = _startedAt;
                RestartParticles();
                if (Visible && IsHandleCreated) _timer.Start();
                Invalidate();
            }
            catch (ObjectDisposedException) { }
        }

        public void Stop()
        {
            if (_disposed) return;
            bool wasPlaying = _playing;
            _playing = false;
            _timer.Stop();
            _particles.Clear();
            _progress = 0f;
            Invalidate();
            if (wasPlaying) RaiseCompleted();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible)
                _timer.Stop();
            else if (_playing && IsHandleCreated)
            {
                _lastTick = Environment.TickCount;
                _timer.Start();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_playing && Visible) _timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (_disposed || !_playing) { _timer.Stop(); return; }
            try
            {
                long now = Environment.TickCount;
                float elapsed = (uint)(now - _startedAt);
                float seconds = Math.Min(0.08f, Math.Max(0.001f, (uint)(now - _lastTick) / 1000f));
                _lastTick = now;
                _progress = Math.Min(1f, elapsed / Math.Max(1f, _duration));
                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    ConfettiParticle p = _particles[i];
                    p.VelocityY += _gravity * seconds;
                    p.X += p.VelocityX * seconds;
                    p.Y += p.VelocityY * seconds;
                    p.Rotation += p.RotationSpeed * seconds;
                    if (p.Y - p.Height > Height + 16 || p.X + p.Width < -24 || p.X > Width + 24)
                        _particles.RemoveAt(i);
                }
                Invalidate();
                if (elapsed >= _duration)
                {
                    _playing = false;
                    _timer.Stop();
                    _particles.Clear();
                    Invalidate();
                    RaiseCompleted();
                }
            }
            catch (ObjectDisposedException) { _timer.Stop(); }
            catch (InvalidOperationException) { _timer.Stop(); }
        }

        private void RestartParticles()
        {
            _particles.Clear();
            if (Quantity <= 0 || Width <= 0 || Height <= 0 || _palette.Length == 0) return;
            for (int i = 0; i < Quantity; i++)
            {
                ConfettiParticle p = new ConfettiParticle();
                p.X = Width * 0.5f + RandomFloat(-Width * 0.28f, Width * 0.28f);
                p.Y = Height * 0.43f + RandomFloat(-Height * 0.13f, Height * 0.13f);
                p.VelocityX = RandomFloat(-Width * 0.9f, Width * 0.9f);
                p.VelocityY = RandomFloat(-Height * 2.2f, -Height * 0.75f);
                p.Width = RandomFloat(4f, 9f);
                p.Height = RandomFloat(7f, 15f);
                p.Rotation = RandomFloat(0f, 360f);
                p.RotationSpeed = RandomFloat(-420f, 420f);
                p.ColorIndex = _random.Next(_palette.Length);
                p.Circle = _random.Next(5) == 0;
                _particles.Add(p);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_playing || Width < 1 || Height < 1) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            for (int i = 0; i < _particles.Count; i++)
            {
                ConfettiParticle p = _particles[i];
                Color color = _palette[p.ColorIndex % _palette.Length];
                float alpha = 255f * Math.Min(1f, Math.Max(0.12f, 1f - _progress * 0.45f));
                GraphicsState state = g.Save();
                g.TranslateTransform(p.X, p.Y);
                g.RotateTransform(p.Rotation);
                using (SolidBrush glow = new SolidBrush(Color.FromArgb((int)(alpha * 0.16f), color)))
                    g.FillEllipse(glow, -p.Width * 0.8f, -p.Height * 0.8f, p.Width * 1.6f, p.Height * 1.6f);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)alpha, color)))
                {
                    if (p.Circle) g.FillEllipse(brush, -p.Width * 0.5f, -p.Width * 0.5f, p.Width, p.Width);
                    else g.FillRectangle(brush, -p.Width * 0.5f, -p.Height * 0.5f, p.Width, p.Height);
                }
                g.Restore(state);
            }
            if (!string.IsNullOrEmpty(_celebrationText))
            {
                Rectangle textRect = new Rectangle(8, Math.Max(8, Height / 2 - 28), Math.Max(1, Width - 16), 56);
                int textAlpha = (int)(235f * Math.Min(1f, Math.Max(0f, 1f - _progress * 0.55f)));
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(Math.Min(80, textAlpha / 3), ZenTheme.Primary)))
                    g.FillEllipse(glow, Width / 2 - Math.Min(Width, 210), textRect.Top - 12, Math.Min(Width, 420), 80);
                TextRenderer.DrawText(g, _celebrationText, Font, textRect, Color.FromArgb(textAlpha, ForeColor), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
        }

        private float RandomFloat(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private void RaiseCompleted()
        {
            EventHandler handler = Completed;
            if (handler != null)
            {
                try { handler(this, EventArgs.Empty); }
                catch { }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return min;
            return Math.Min(max, Math.Max(min, value));
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _playing = false;
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Tick -= TimerTick;
                        _timer.Dispose();
                    }
                    _particles.Clear();
                }
            }
            base.Dispose(disposing);
        }
    }
}
// watashi wa watashi sore dake