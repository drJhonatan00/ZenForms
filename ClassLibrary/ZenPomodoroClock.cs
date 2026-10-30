using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public enum ZenPomodoroPhase { Work, ShortBreak, LongBreak }
    public sealed class ZenPomodoroPhaseEventArgs : EventArgs { public ZenPomodoroPhase Phase { get; private set; } public ZenPomodoroPhaseEventArgs(ZenPomodoroPhase phase) { Phase = phase; } }
    [Category("ZenForms Modern Components")]
    [Description("Visual Pomodoro timer with configurable cycles and automatic breaks.")]
    public class ZenPomodoroClock : UserControl
    {
        private readonly Timer timer; private readonly Button toggle, reset; private readonly Label phaseLabel, sessionLabel; private int workMinutes = 25, shortBreakMinutes = 5, longBreakMinutes = 15, completedSessions, remaining; private ZenPomodoroPhase phase = ZenPomodoroPhase.Work; private bool running; private Color accent = ZenTheme.Primary;
        [Category("ZenForms Pomodoro"), DefaultValue(25)] public int WorkMinutes { get { return workMinutes; } set { workMinutes = Math.Max(1, value); if (phase == ZenPomodoroPhase.Work && !running) ResetClock(); } }
        [Category("ZenForms Pomodoro"), DefaultValue(5)] public int ShortBreakMinutes { get { return shortBreakMinutes; } set { shortBreakMinutes = Math.Max(1, value); if (phase == ZenPomodoroPhase.ShortBreak && !running) ResetClock(); } }
        [Category("ZenForms Pomodoro"), DefaultValue(15)] public int LongBreakMinutes { get { return longBreakMinutes; } set { longBreakMinutes = Math.Max(1, value); if (phase == ZenPomodoroPhase.LongBreak && !running) ResetClock(); } }
        [Category("ZenForms Appearance")] public Color AccentColor { get { return accent; } set { accent = value; toggle.BackColor = value; Invalidate(); } }
        [Browsable(false)] public ZenPomodoroPhase Phase { get { return phase; } }
        [Browsable(false)] public int CompletedSessions { get { return completedSessions; } }
        public event EventHandler<ZenPomodoroPhaseEventArgs> PhaseChanged;
        public ZenPomodoroClock() { Size = new Size(250, 300); MinimumSize = new Size(180, 230); BackColor = ZenTheme.Surface; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); phaseLabel = new Label { Dock = DockStyle.Top, Height = 36, Text = "FOCUS", Font = new Font("Segoe UI Semibold", 10f), ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleCenter }; sessionLabel = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Concluded sections: 0", Font = new Font("Segoe UI", 8f), ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleCenter }; Panel buttons = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(24, 4, 24, 4) }; toggle = new Button { Dock = DockStyle.Fill, Text = "Start focus", FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9f), Cursor = Cursors.Hand }; reset = new Button { Dock = DockStyle.Right, Width = 52, Text = "↺", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground }; buttons.Controls.Add(toggle); buttons.Controls.Add(reset); Controls.Add(buttons); Controls.Add(sessionLabel); Controls.Add(phaseLabel); timer = new Timer { Interval = 1000 }; timer.Tick += (s, e) => Tick(); timer.Start(); toggle.Click += (s, e) => Toggle(); reset.Click += (s, e) => ResetClock(); remaining = workMinutes * 60; }
        public void Start() { running = true; timer.Start(); toggle.Text = phase == ZenPomodoroPhase.Work ? "Pause focus" : "Pause pause"; Invalidate(); }
        public void Pause() { running = false; toggle.Text = phase == ZenPomodoroPhase.Work ? "Continue focus" : "Continue pause"; Invalidate(); }
        public void ResetClock() { running = false; remaining = DurationFor(phase) * 60; toggle.Text = phase == ZenPomodoroPhase.Work ? "Start focus" : "Start pause"; Invalidate(); }
        public void SkipPhase() { AdvancePhase(); }
        private void Toggle() { if (running) Pause(); else Start(); }
        private void Tick() { if (!running) return; if (remaining > 0) remaining--; if (remaining <= 0) AdvancePhase(); Invalidate(); }
        private int DurationFor(ZenPomodoroPhase p) { return p == ZenPomodoroPhase.Work ? workMinutes : p == ZenPomodoroPhase.ShortBreak ? shortBreakMinutes : longBreakMinutes; }
        private void AdvancePhase() { if (phase == ZenPomodoroPhase.Work) { completedSessions++; phase = completedSessions % 4 == 0 ? ZenPomodoroPhase.LongBreak : ZenPomodoroPhase.ShortBreak; } else phase = ZenPomodoroPhase.Work; remaining = DurationFor(phase) * 60; running = false; toggle.Text = phase == ZenPomodoroPhase.Work ? "Start focus" : "Start pause"; phaseLabel.Text = phase == ZenPomodoroPhase.Work ? "FOCUS" : phase == ZenPomodoroPhase.ShortBreak ? "pause SHORT" : "pause LONG"; sessionLabel.Text = "Concluded sections: " + completedSessions; EventHandler<ZenPomodoroPhaseEventArgs> h = PhaseChanged; if (h != null) h(this, new ZenPomodoroPhaseEventArgs(phase)); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; int d = Math.Min(Width - 38, Height - 120); d = Math.Max(80, d); Rectangle r = new Rectangle((Width - d) / 2, (Height - d) / 2 - 4, d, d); using (Pen track = new Pen(ZenTheme.SurfaceRaised, 9f)) g.DrawArc(track, r, -90, 359.9f); float total = Math.Max(1, DurationFor(phase) * 60); float progress = 1f - remaining / total; Color c = phase == ZenPomodoroPhase.Work ? accent : ZenTheme.Accent; using (Pen p = new Pen(c, 9f)) { p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; g.DrawArc(p, r, -90, Math.Max(.1f, 360 * progress)); } string text = String.Format("{0:00}:{1:00}", remaining / 60, remaining % 60); using (Font timerFont = new Font("Consolas", Math.Max(18, d * .19f), FontStyle.Bold)) TextRenderer.DrawText(g, text, timerFont, r, ZenTheme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
    }
}

// watashi wa watashi sore dake
