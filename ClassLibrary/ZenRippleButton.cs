using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Button))]
    [Category("ZenForms Modern Components")]
    [Description("Button with a smooth radial ripple, hover glow and keyboard support.")]
    public class ZenRippleButton : Control
    {
        private readonly Timer _animationTimer;
        private bool _hovered;
        private bool _pressed;
        private bool _mousePressed;
        private bool _disposed;
        private Point _rippleOrigin;
        private float _rippleProgress;
        private int _cornerRadius = 10;
        private int _rippleDuration = 420;
        private int _borderWidth = 1;
        private int _hoverGlow = 70;
        private Color _normalBackColor = ZenTheme.Surface;
        private Color _hoverBackColor = ZenTheme.SurfaceRaised;
        private Color _rippleColor = ZenTheme.Accent;
        private Color _hoverGlowColor = ZenTheme.Primary;
        private Color _borderColor = ZenTheme.Border;
        private Color _textColor = ZenTheme.Foreground;

        public ZenRippleButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(150, 44);
            MinimumSize = new Size(36, 24);
            Cursor = Cursors.Hand;
            TabStop = true;
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular);
            BackColor = _normalBackColor;
            ForeColor = _textColor;
            _animationTimer = new Timer();
            _animationTimer.Interval = 16;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [Category("ZenForms Appearance"), DefaultValue(10)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(1)]
        public int BorderWidth
        {
            get { return _borderWidth; }
            set { _borderWidth = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(70)]
        public int HoverGlowIntensity
        {
            get { return _hoverGlow; }
            set { _hoverGlow = Math.Max(0, Math.Min(180, value)); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(420)]
        public int RippleDuration
        {
            get { return _rippleDuration; }
            set { _rippleDuration = Math.Max(80, Math.Min(3000, value)); }
        }

        [Category("ZenForms Appearance")]
        public Color NormalBackColor
        {
            get { return _normalBackColor; }
            set { _normalBackColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color HoverBackColor
        {
            get { return _hoverBackColor; }
            set { _hoverBackColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color RippleColor
        {
            get { return _rippleColor; }
            set { _rippleColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color HoverGlowColor
        {
            get { return _hoverGlowColor; }
            set { _hoverGlowColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; ForeColor = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            StartAnimation();
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _mousePressed = false;
            _pressed = false;
            StopAnimationIfIdle();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _disposed) return;
            Focus();
            Capture = true;
            _mousePressed = true;
            _pressed = true;
            BeginRipple(e.Location);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            bool shouldClick = _mousePressed && ClientRectangle.Contains(e.Location);
            _mousePressed = false;
            _pressed = false;
            if (Capture) Capture = false;
            Invalidate();
            if (shouldClick) OnClick(EventArgs.Empty);
            StopAnimationIfIdle();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && !e.Handled)
            {
                if (!_pressed)
                {
                    _pressed = true;
                    BeginRipple(new Point(Math.Max(0, Width / 2), Math.Max(0, Height / 2)));
                    Invalidate();
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && _pressed)
            {
                _pressed = false;
                Invalidate();
                OnClick(EventArgs.Empty);
                StopAnimationIfIdle();
                e.Handled = true;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) _animationTimer.Stop();
            else if (_hovered || _rippleProgress < 1f) StartAnimation();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_disposed || ClientSize.Width < 1 || ClientSize.Height < 1) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle r = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            if (_pressed && r.Height > 2) r.Offset(0, 1);
            using (GraphicsPath path = ZenButtonBase.RoundPath(r, _cornerRadius))
            {
                Color fill = _hovered ? _hoverBackColor : _normalBackColor;
                if (_pressed) fill = ControlPaint.Dark(fill, 0.08f);
                using (SolidBrush brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (_hovered && _hoverGlow > 0)
                {
                    using (Pen glow = new Pen(Color.FromArgb(Math.Min(255, _hoverGlow), _hoverGlowColor), 5f))
                        g.DrawPath(glow, path);
                }
                if (_rippleProgress < 1f && _rippleColor.A > 0)
                {
                    GraphicsState state = g.Save();
                    g.SetClip(path);
                    float maxRadius = (float)Math.Sqrt(Width * Width + Height * Height);
                    float radius = Math.Max(1f, maxRadius * _rippleProgress);
                    int alpha = Math.Max(0, Math.Min(150, (int)(105f * (1f - _rippleProgress))));
                    using (SolidBrush ripple = new SolidBrush(Color.FromArgb(alpha, _rippleColor)))
                        g.FillEllipse(ripple, _rippleOrigin.X - radius, _rippleOrigin.Y - radius, radius * 2f, radius * 2f);
                    g.Restore(state);
                }
                if (_borderWidth > 0)
                {
                    using (Pen border = new Pen(_borderColor, _borderWidth))
                    {
                        border.Alignment = PenAlignment.Inset;
                        g.DrawPath(border, path);
                    }
                }
                if (Focused)
                {
                    Rectangle focus = Rectangle.Inflate(r, -4, -4);
                    if (focus.Width > 0 && focus.Height > 0)
                        ControlPaint.DrawFocusRectangle(g, focus, Color.FromArgb(150, _textColor), Color.Transparent);
                }
            }
            Rectangle textRect = ClientRectangle;
            if (_pressed) textRect.Offset(0, 1);
            TextRenderer.DrawText(g, Text ?? String.Empty, Font, textRect, _textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        private void BeginRipple(Point origin)
        {
            _rippleOrigin = new Point(Math.Max(0, Math.Min(Math.Max(0, Width), origin.X)),
                                       Math.Max(0, Math.Min(Math.Max(0, Height), origin.Y)));
            _rippleProgress = 0f;
            StartAnimation();
        }

        private void StartAnimation()
        {
            if (_disposed || !IsHandleCreated || !Visible) return;
            if (!_animationTimer.Enabled) _animationTimer.Start();
        }

        private void StopAnimationIfIdle()
        {
            if (!_hovered && _rippleProgress >= 1f) _animationTimer.Stop();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_disposed || !IsHandleCreated || !Visible)
            {
                _animationTimer.Stop();
                return;
            }
            if (_rippleProgress < 1f)
            {
                float step = _animationTimer.Interval / (float)Math.Max(80, _rippleDuration);
                _rippleProgress = Math.Min(1f, _rippleProgress + step);
            }
            Invalidate();
            StopAnimationIfIdle();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
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

//watashi wa watashi sore dake