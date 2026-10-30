using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Sci-fi terminal-style glyph rain with subtle animation.")]
    public class ZenCodeRain : Control
    {
        private readonly Timer timer; private readonly Random random = new Random(1997); private float[] drops = new float[0]; private int cell = 18; private int speed = 45; private Color rainColor = Color.FromArgb(42, 211, 190); private bool running = true; private const string Glyphs = "01アイウエオカキクケコサシスセソタチツテトナニヌネノ";
        [Category("ZenForms Animation"), DefaultValue(18)] public int CellSize { get { return cell; } set { cell = Math.Max(10, Math.Min(36, value)); ResetDrops(); Invalidate(); } }
        [Category("ZenForms Animation"), DefaultValue(45)] public int Speed { get { return speed; } set { speed = Math.Max(15, Math.Min(250, value)); timer.Interval = speed; } }
        [Category("ZenForms Appearance")] public Color RainColor { get { return rainColor; } set { rainColor = value; Invalidate(); } }
        [Category("ZenForms Animation"), DefaultValue(true)] public bool Running { get { return running; } set { running = value; if (value) timer.Start(); else timer.Stop(); } }
        public ZenCodeRain() { Size = new Size(400, 250); BackColor = Color.FromArgb(5, 12, 15); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); ResetDrops(); timer = new Timer { Interval = speed }; timer.Tick += (s, e) => { for (int i = 0; i < drops.Length; i++) { drops[i] += random.Next(1, 4); if (drops[i] * cell > Height && random.NextDouble() > .97) drops[i] = -random.Next(1, 30); } Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); ResetDrops(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible && running) timer.Start(); else timer.Stop(); }
        private void ResetDrops() { if (cell <= 0) return; int n = Math.Max(1, Width / cell); drops = new float[n]; for (int i = 0; i < n; i++) drops[i] = random.Next(Math.Max(1, Height / cell)); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.Clear(BackColor); using (Font f = new Font("Consolas", Math.Max(8, cell - 4), FontStyle.Regular)) for (int col = 0; col < drops.Length; col++) { int head = (int)drops[col]; for (int trail = 0; trail < 12; trail++) { int row = head - trail; if (row < 0) continue; int alpha = Math.Max(12, Math.Min(255, 230 - trail * 19)); Color c = Color.FromArgb(alpha, rainColor); if (trail == 0) c = Color.FromArgb(245, 220, 255, 239); string glyph = Glyphs[random.Next(Glyphs.Length)].ToString(); using (SolidBrush b = new SolidBrush(c)) e.Graphics.DrawString(glyph, f, b, col * cell, row * cell); } } }
    }
}

// Watashi wa watashi sore dake