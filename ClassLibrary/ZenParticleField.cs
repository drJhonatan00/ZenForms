using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Animated particle field with trails, connections, whitout cursor interaction.")]
    public class ZenParticleField : Control
    {
        private struct Particle { public float X, Y, VX, VY, Size; }
        private readonly Timer timer; private readonly Random random = new Random(8021); private Particle[] particles; private int count = 55; private float linkDistance = 95; private Point mouse; private bool mouseInside; private Color particleColor = ZenTheme.Accent;
        [Category("ZenForms Animation"), DefaultValue(55)] public int ParticleCount { get { return count; } set { count = Math.Max(8, Math.Min(180, value)); ResetParticles(); Invalidate(); } }
        [Category("ZenForms Animation"), DefaultValue(95f)] public float LinkDistance { get { return linkDistance; } set { linkDistance = Math.Max(20, Math.Min(240, value)); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color ParticleColor { get { return particleColor; } set { particleColor = value; Invalidate(); } }
        public ZenParticleField() { Size = new Size(420, 280); BackColor = Color.FromArgb(12, 16, 29); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); ResetParticles(); timer = new Timer { Interval = 30 }; timer.Tick += (s, e) => { Advance(); Invalidate(); }; timer.Start(); Disposed += (s, e) => timer.Dispose(); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); ResetParticles(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (timer == null) return; if (Visible) timer.Start(); else timer.Stop(); }
        private void ResetParticles() { if (Width <= 0 || Height <= 0) return; particles = new Particle[count]; for (int i = 0; i < count; i++) particles[i] = new Particle { X = random.Next(Width), Y = random.Next(Height), VX = (float)(random.NextDouble() - .5) * 1.3f, VY = (float)(random.NextDouble() - .5) * 1.3f, Size = (float)(1.5 + random.NextDouble() * 2.6) }; }
        private void Advance() { if (particles == null) return; for (int i = 0; i < particles.Length; i++) { Particle p = particles[i]; p.X += p.VX; p.Y += p.VY; if (p.X < 0 || p.X > Width) p.VX = -p.VX; if (p.Y < 0 || p.Y > Height) p.VY = -p.VY; if (mouseInside) { float dx = p.X - mouse.X, dy = p.Y - mouse.Y, d = (float)Math.Sqrt(dx * dx + dy * dy); if (d < 100 && d > 1) { p.VX += dx / d * .035f; p.VY += dy / d * .035f; } } float speed = (float)Math.Sqrt(p.VX * p.VX + p.VY * p.VY); if (speed > 1.7f) { p.VX *= 1.7f / speed; p.VY *= 1.7f / speed; } particles[i] = p; } }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); mouse = e.Location; mouseInside = true; }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); mouseInside = false; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(BackColor); if (particles == null) return; for (int i = 0; i < particles.Length; i++) for (int j = i + 1; j < particles.Length; j++) { float dx = particles[i].X - particles[j].X, dy = particles[i].Y - particles[j].Y, d = (float)Math.Sqrt(dx * dx + dy * dy); if (d < linkDistance) { int a = (int)(72 * (1 - d / linkDistance)); using (Pen p = new Pen(Color.FromArgb(a, particleColor), .8f)) g.DrawLine(p, particles[i].X, particles[i].Y, particles[j].X, particles[j].Y); } } for (int i = 0; i < particles.Length; i++) { Particle p = particles[i]; using (SolidBrush glow = new SolidBrush(Color.FromArgb(30, particleColor))) g.FillEllipse(glow, p.X - p.Size * 3, p.Y - p.Size * 3, p.Size * 6, p.Size * 6); using (SolidBrush dot = new SolidBrush(Color.FromArgb(220, particleColor))) g.FillEllipse(dot, p.X - p.Size, p.Y - p.Size, p.Size * 2, p.Size * 2); } }
    }
}

// watashi wa watashi sore dake