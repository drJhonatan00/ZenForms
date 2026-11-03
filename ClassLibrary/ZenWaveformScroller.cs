using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Animated and navigable waveform viewer with playback controls and mouse selection.")]
    public class ZenWaveformScroller : Control
    {
        private readonly Timer _animationTimer;
        private readonly List<float> _amplitudes;
        private float _playbackPosition;
        private float _playbackSpeed = 1f;
        private float _selectionStart = -1f;
        private float _selectionEnd = -1f;
        private float _demoPhase;
        private bool _isPlaying;
        private bool _loop;
        private bool _draggingSelection;
        private bool _suppressPositionEvent;
        private int _cornerRadius = 12;
        private int _waveformPadding = 12;
        private Color _waveColor = ZenTheme.Primary;
        private Color _playedWaveColor = ZenTheme.Accent;
        private Color _playheadColor = ZenTheme.Accent;
        private Color _selectionColor = ZenTheme.Primary;

        public ZenWaveformScroller()
        {
            _amplitudes = new List<float>();
            _animationTimer = new Timer();
            _animationTimer.Interval = 33;
            _animationTimer.Tick += AnimationTimer_Tick;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Size = new Size(420, 150);
            MinimumSize = new Size(80, 52);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f);
            TabStop = true;
        }

        [Category("ZenForms Data")]
        [Description("Mutable collection of normalized waveform amplitudes. Values outside -1 to 1 are clamped when painted.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IList<float> Amplitudes
        {
            get { return _amplitudes; }
        }

        [Category("ZenForms Behavior")]
        [Description("Current playback position from 0 to 1.")]
        [DefaultValue(0f)]
        public float PlaybackPosition
        {
            get { return _playbackPosition; }
            set { SetPlaybackPosition(value, true); }
        }

        [Category("ZenForms Behavior")]
        [Description("Playback speed multiplier.")]
        [DefaultValue(1f)]
        public float PlaybackSpeed
        {
            get { return _playbackSpeed; }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) value = 1f;
                _playbackSpeed = Math.Max(0.1f, Math.Min(8f, value));
                Invalidate();
            }
        }

        [Category("ZenForms Behavior")]
        [Description("When enabled, playback returns to the beginning after reaching the end.")]
        [DefaultValue(false)]
        public bool Loop
        {
            get { return _loop; }
            set { _loop = value; Invalidate(); }
        }

        [Category("ZenForms Behavior")]
        [Description("Indicates whether the waveform is currently playing.")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPlaying { get { return _isPlaying; } }

        [Category("ZenForms Behavior")]
        [Description("Start of the current mouse selection from 0 to 1, or -1 when there is no selection.")]
        [DefaultValue(-1f)]
        public float SelectionStart
        {
            get { return _selectionStart; }
            set { SetSelection(value, _selectionEnd); }
        }

        [Category("ZenForms Behavior")]
        [Description("End of the current mouse selection from 0 to 1, or -1 when there is no selection.")]
        [DefaultValue(-1f)]
        public float SelectionEnd
        {
            get { return _selectionEnd; }
            set { SetSelection(_selectionStart, value); }
        }

        [Category("ZenForms Appearance")]
        [Description("Corner radius of the waveform surface.")]
        [DefaultValue(12)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Horizontal and vertical inset used for drawing the waveform.")]
        [DefaultValue(12)]
        public int WaveformPadding
        {
            get { return _waveformPadding; }
            set { _waveformPadding = Math.Max(3, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Color of the unplayed waveform.")]
        public Color WaveColor
        {
            get { return _waveColor; }
            set { _waveColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Color of the portion of the waveform before the playhead.")]
        public Color PlayedWaveColor
        {
            get { return _playedWaveColor; }
            set { _playedWaveColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Color of the playback position line.")]
        public Color PlayheadColor
        {
            get { return _playheadColor; }
            set { _playheadColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Color of the selected range overlay.")]
        public Color SelectionColor
        {
            get { return _selectionColor; }
            set { _selectionColor = value; Invalidate(); }
        }

        public event EventHandler PositionChanged;

        public void SetAmplitudes(IEnumerable<float> values)
        {
            _amplitudes.Clear();
            if (values != null)
            {
                foreach (float value in values) _amplitudes.Add(value);
            }
            Invalidate();
        }

        public void Play()
        {
            if (IsDisposed) return;
            _isPlaying = true;
            StartTimerIfPossible();
            Invalidate();
        }

        public void Pause()
        {
            _isPlaying = false;
            StopTimer();
            Invalidate();
        }

        public void Stop()
        {
            _isPlaying = false;
            StopTimer();
            SetPlaybackPosition(0f, true);
            Invalidate();
        }

        public void TogglePlayPause()
        {
            if (_isPlaying) Pause(); else Play();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && _isPlaying) StartTimerIfPossible();
            else if (!Visible) StopTimer();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || Width < 1 || Height < 1) return;
            Focus();
            _draggingSelection = true;
            float position = PositionFromPoint(e.X);
            SetSelection(position, position);
            SetPlaybackPosition(position, true);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_draggingSelection) return;
            float position = PositionFromPoint(e.X);
            SetSelection(_selectionStart, position);
            SetPlaybackPosition(position, true);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) _draggingSelection = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_draggingSelection) Cursor = Cursors.Default;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space)
            {
                TogglePlayPause();
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (Width < 4 || Height < 4) return;

            Rectangle surface = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (GraphicsPath path = ZenButtonBase.RoundPath(surface, Math.Min(_cornerRadius, Math.Min(surface.Width, surface.Height) / 2)))
            {
                using (SolidBrush fill = new SolidBrush(ZenTheme.Surface)) g.FillPath(fill, path);
                using (Pen border = new Pen(Color.FromArgb(150, ZenTheme.Border), 1f)) g.DrawPath(border, path);

                GraphicsState state = g.Save();
                g.SetClip(path);
                DrawSelection(g, surface);
                DrawWaveform(g, surface);
                DrawPlayhead(g, surface);
                g.Restore(state);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopTimer();
                if (_animationTimer != null)
                {
                    _animationTimer.Tick -= AnimationTimer_Tick;
                    _animationTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPlaying || IsDisposed || !IsHandleCreated || !Visible) return;
            float next = _playbackPosition + (0.033f * _playbackSpeed);
            if (next >= 1f)
            {
                if (_loop) next = next - (float)Math.Floor(next);
                else
                {
                    next = 1f;
                    _isPlaying = false;
                    StopTimer();
                }
            }
            _demoPhase += 0.11f * _playbackSpeed;
            SetPlaybackPosition(next, true);
            Invalidate();
        }

        private void DrawWaveform(Graphics g, Rectangle surface)
        {
            int left = surface.Left + _waveformPadding;
            int right = surface.Right - _waveformPadding;
            int top = surface.Top + _waveformPadding;
            int bottom = surface.Bottom - _waveformPadding;
            if (right <= left || bottom <= top) return;
            float center = (top + bottom) * 0.5f;
            float halfHeight = Math.Max(2f, (bottom - top) * 0.46f);
            int count = _amplitudes.Count;
            if (count == 0) count = Math.Max(18, (right - left) / 5);
            float step = count > 1 ? (right - left) / (float)(count - 1) : 1f;
            using (Pen wave = new Pen(Color.FromArgb(205, _waveColor), Math.Max(1f, Math.Min(3f, step * 0.38f))))
            using (Pen played = new Pen(Color.FromArgb(235, _playedWaveColor), Math.Max(1f, Math.Min(3f, step * 0.38f))))
            {
                for (int i = 0; i < count; i++)
                {
                    float normalized = count == 1 ? 0f : i / (float)(count - 1);
                    float amplitude = _amplitudes.Count == 0 ? DemoAmplitude(normalized) : SafeAmplitude(_amplitudes[i]);
                    float height = Math.Max(2f, Math.Abs(amplitude) * halfHeight);
                    float x = left + normalized * (right - left);
                    Pen pen = normalized <= _playbackPosition ? played : wave;
                    g.DrawLine(pen, x, center - height, x, center + height);
                }
            }
        }

        private void DrawSelection(Graphics g, Rectangle surface)
        {
            if (_selectionStart < 0f || _selectionEnd < 0f) return;
            float start = Math.Min(_selectionStart, _selectionEnd);
            float end = Math.Max(_selectionStart, _selectionEnd);
            int left = surface.Left + _waveformPadding;
            int right = surface.Right - _waveformPadding;
            int x1 = left + (int)((right - left) * start);
            int x2 = left + (int)((right - left) * end);
            if (x2 <= x1) x2 = Math.Min(right, x1 + 2);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(38, _selectionColor)))
                g.FillRectangle(brush, x1, surface.Top, Math.Max(1, x2 - x1), surface.Height);
        }

        private void DrawPlayhead(Graphics g, Rectangle surface)
        {
            int left = surface.Left + _waveformPadding;
            int right = surface.Right - _waveformPadding;
            if (right <= left) return;
            int x = left + (int)((right - left) * _playbackPosition);
            using (Pen glow = new Pen(Color.FromArgb(55, _playheadColor), 6f)) g.DrawLine(glow, x, surface.Top + 5, x, surface.Bottom - 5);
            using (Pen line = new Pen(Color.FromArgb(235, _playheadColor), 1.5f)) g.DrawLine(line, x, surface.Top + 4, x, surface.Bottom - 4);
            using (SolidBrush marker = new SolidBrush(_playheadColor))
            {
                Point[] triangle = new Point[] { new Point(x - 4, surface.Top + 3), new Point(x + 4, surface.Top + 3), new Point(x, surface.Top + 9) };
                g.FillPolygon(marker, triangle);
            }
        }

        private float DemoAmplitude(float position)
        {
            double wave = Math.Sin(position * Math.PI * 11.0 + _demoPhase) * 0.40;
            wave += Math.Sin(position * Math.PI * 27.0 - _demoPhase * 0.55) * 0.18;
            double envelope = 0.42 + 0.52 * Math.Sin(position * Math.PI);
            return (float)(Math.Abs(wave) * envelope + 0.045);
        }

        private static float SafeAmplitude(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Math.Max(-1f, Math.Min(1f, value));
        }

        private float PositionFromPoint(int x)
        {
            int left = 1 + _waveformPadding;
            int right = Math.Max(left + 1, Width - 3 - _waveformPadding);
            return Math.Max(0f, Math.Min(1f, (x - left) / (float)(right - left)));
        }

        private void SetPlaybackPosition(float value, bool raiseEvent)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0f;
            value = Math.Max(0f, Math.Min(1f, value));
            bool changed = Math.Abs(value - _playbackPosition) > 0.0001f;
            _playbackPosition = value;
            if (changed && raiseEvent && !_suppressPositionEvent && PositionChanged != null)
                PositionChanged(this, EventArgs.Empty);
            Invalidate();
        }

        private void SetSelection(float start, float end)
        {
            _selectionStart = NormalizeSelectionValue(start);
            _selectionEnd = NormalizeSelectionValue(end);
            if (_selectionStart >= 0f && _selectionEnd >= 0f && Math.Abs(_selectionEnd - _selectionStart) < 0.002f)
                _selectionEnd = _selectionStart;
            Invalidate();
        }

        private static float NormalizeSelectionValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) return -1f;
            return Math.Min(1f, value);
        }

        private void StartTimerIfPossible()
        {
            if (IsDisposed || !Visible || !IsHandleCreated) return;
            if (!_animationTimer.Enabled) _animationTimer.Start();
        }

        private void StopTimer()
        {
            if (_animationTimer != null && _animationTimer.Enabled) _animationTimer.Stop();
        }
    }
}
// watashi wa watashi sore dake