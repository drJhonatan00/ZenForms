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
    [Description("Animated Matrix-style character console with useful message output.")]
    public class ZenMatrixConsole : Control
    {
        private readonly Timer _animationTimer;
        private readonly Random _random;
        private readonly List<string> _messages;
        private float[] _drops;
        private int[] _lengths;
        private int[] _ages;
        private bool[] _active;
        private int _columnWidth;
        private bool _running;
        private bool _pauseWhenInvisible = true;
        private bool _rainMode = true;
        private int _density = 72;
        private int _speed = 55;
        private int _cornerRadius = 14;
        private int _messageLimit = 5;
        private int _trailLength = 13;
        private string _characterSet = "01アイウエオカキクケコサシスセソタチツテトナニヌネノ#$%&*+-<>/\\";
        private Color _rainColor = ZenTheme.Accent;
        private Color _messageColor = ZenTheme.Foreground;
        private Color _glowColor = ZenTheme.Primary;

        public ZenMatrixConsole()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(360, 210);
            MinimumSize = new Size(80, 52);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            _random = new Random();
            _messages = new List<string>();
            _animationTimer = new Timer();
            _animationTimer.Interval = _speed;
            _animationTimer.Tick += AnimationTimer_Tick;
            RebuildColumns();
        }

        [Category("ZenForms Behavior"), DefaultValue(false)]
        public bool IsRunning { get { return _running; } }

        [Category("ZenForms Behavior"), DefaultValue(true)]
        public bool PauseWhenInvisible
        {
            get { return _pauseWhenInvisible; }
            set { _pauseWhenInvisible = value; UpdateTimerState(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(true)]
        public bool RainMode
        {
            get { return _rainMode; }
            set { _rainMode = value; Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(72), Description("Percentage of columns that contain falling characters.")]
        public int Density
        {
            get { return _density; }
            set { _density = Clamp(value, 0, 100); RebuildColumns(); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(55), Description("Animation interval in milliseconds.")]
        public int Speed
        {
            get { return _speed; }
            set { _speed = Clamp(value, 20, 250); _animationTimer.Interval = _speed; }
        }

        [Category("ZenForms Appearance"), DefaultValue(14)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance"), DefaultValue(13)]
        public int TrailLength
        {
            get { return _trailLength; }
            set { _trailLength = Clamp(value, 3, 40); Invalidate(); }
        }

        [Category("ZenForms Behavior"), DefaultValue(5), Description("Number of recent messages shown at the bottom of the console.")]
        public int MessageLimit
        {
            get { return _messageLimit; }
            set
            {
                _messageLimit = Clamp(value, 0, 20);
                while (_messages.Count > _messageLimit && _messages.Count > 0) _messages.RemoveAt(0);
                Invalidate();
            }
        }

        [Category("ZenForms Appearance"), DefaultValue("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&*+-<>/\\")]
        public string CharacterSet
        {
            get { return _characterSet; }
            set { _characterSet = string.IsNullOrEmpty(value) ? "01" : value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color RainColor
        {
            get { return _rainColor; }
            set { _rainColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color MessageColor
        {
            get { return _messageColor; }
            set { _messageColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color GlowColor
        {
            get { return _glowColor; }
            set { _glowColor = value; Invalidate(); }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<string> Messages { get { return _messages.AsReadOnly(); } }

        public void Start()
        {
            _running = true;
            UpdateTimerState();
            Invalidate();
        }

        public void Stop()
        {
            _running = false;
            _animationTimer.Stop();
            Invalidate();
        }

        public void WriteLine(string message)
        {
            if (message == null) message = string.Empty;
            message = message.Replace("\r", " ").Replace("\n", " ");
            if (_messageLimit > 0)
            {
                _messages.Add(message);
                while (_messages.Count > _messageLimit) _messages.RemoveAt(0);
            }
            Invalidate();
        }

        public void ClearLines()
        {
            _messages.Clear();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateTimerState();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _animationTimer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimerState();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            Invalidate();
        }

        private void UpdateTimerState()
        {
            if (!_running || IsDisposed || Disposing || !IsHandleCreated || (_pauseWhenInvisible && !Visible))
                _animationTimer.Stop();
            else
                _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || Disposing) return;
            if (_drops == null || _drops.Length == 0) RebuildColumns();
            for (int i = 0; i < _drops.Length; i++)
            {
                if (!_active[i]) continue;
                _drops[i] += 0.72f + (i % 3) * 0.16f;
                _ages[i]++;
                if (_drops[i] - _lengths[i] > Math.Max(3, Height / Math.Max(1, _columnWidth)) + 5)
                {
                    _drops[i] = -_random.Next(2, Math.Max(4, Height / Math.Max(1, _columnWidth)));
                    _lengths[i] = _random.Next(5, Math.Max(6, _trailLength + 5));
                    _ages[i] = 0;
                }
            }
            Invalidate();
        }

        private void RebuildColumns()
        {
            _columnWidth = Math.Max(8, (int)Math.Ceiling(Math.Max(1f, Font.Size * 0.72f)));
            int count = Math.Max(1, (Width - 16) / _columnWidth);
            float[] oldDrops = _drops;
            _drops = new float[count];
            _lengths = new int[count];
            _ages = new int[count];
            _active = new bool[count];
            for (int i = 0; i < count; i++)
            {
                _active[i] = _random.Next(100) < _density;
                _drops[i] = oldDrops != null && i < oldDrops.Length ? oldDrops[i] : -_random.Next(0, Math.Max(2, Height / _columnWidth));
                _lengths[i] = _random.Next(5, Math.Max(6, _trailLength + 5));
                _ages[i] = _random.Next(0, 20);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Rectangle outer = new Rectangle(3, 3, Math.Max(0, Width - 7), Math.Max(0, Height - 7));
            if (outer.Width < 4 || outer.Height < 4) return;
            using (GraphicsPath path = ZenButtonBase.RoundPath(outer, _cornerRadius))
            {
                using (SolidBrush fill = new SolidBrush(BackColor == Color.Empty ? ZenTheme.Surface : BackColor)) g.FillPath(fill, path);
                using (Pen border = new Pen(Color.FromArgb(125, ZenTheme.Border), 1f)) g.DrawPath(border, path);
                GraphicsState state = g.Save();
                g.SetClip(path);
                DrawRain(g, outer);
                DrawMessages(g, outer);
                g.Restore(state);
                using (Pen accent = new Pen(Color.FromArgb(_running ? 180 : 75, _glowColor), 1f))
                    g.DrawPath(accent, path);
            }
        }

        private void DrawRain(Graphics g, Rectangle bounds)
        {
            if (_drops == null || _drops.Length == 0 || string.IsNullOrEmpty(_characterSet)) return;
            float cellHeight = Math.Max(10f, Font.GetHeight(g) * 0.92f);
            using (Font rainFont = new Font("Consolas", Math.Max(7f, Font.Size - 0.5f), FontStyle.Regular))
            {
                for (int i = 0; i < _drops.Length; i++)
                {
                    if (!_active[i]) continue;
                    int x = bounds.Left + 9 + i * _columnWidth;
                    int head = bounds.Top + 8 + (int)(_drops[i] * cellHeight);
                    int length = _rainMode ? _lengths[i] : 1;
                    for (int j = 0; j < length; j++)
                    {
                        int y = head - (int)(j * cellHeight);
                        if (y < bounds.Top + 4 || y > bounds.Bottom - 5) continue;
                        int alpha = j == 0 ? 235 : Math.Max(18, 170 - (j * 150 / Math.Max(1, length)));
                        Color c = Color.FromArgb(alpha, _rainColor);
                        string character = _characterSet[(_ages[i] + j + i) % _characterSet.Length].ToString();
                        using (SolidBrush brush = new SolidBrush(c)) g.DrawString(character, rainFont, brush, x, y);
                    }
                }
            }
        }

        private void DrawMessages(Graphics g, Rectangle bounds)
        {
            if (_messages.Count == 0 || _messageLimit == 0) return;
            int lineHeight = Math.Max(12, (int)Math.Ceiling(Font.GetHeight(g)) + 2);
            int visible = Math.Min(_messages.Count, Math.Max(1, (bounds.Height - 12) / lineHeight));
            int y = bounds.Bottom - 8 - visible * lineHeight;
            using (SolidBrush panel = new SolidBrush(Color.FromArgb(185, ZenTheme.Background)))
                g.FillRectangle(panel, bounds.Left + 7, Math.Max(bounds.Top + 5, y - 4), bounds.Width - 14, visible * lineHeight + 7);
            for (int i = _messages.Count - visible; i < _messages.Count; i++)
            {
                string text = _messages[i] ?? string.Empty;
                Rectangle textRect = new Rectangle(bounds.Left + 13, y, Math.Max(1, bounds.Width - 26), lineHeight);
                TextRenderer.DrawText(g, text, Font, textRect, _messageColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                y += lineHeight;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _running = false;
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
    }
}

// watashi wa watashi sore dake