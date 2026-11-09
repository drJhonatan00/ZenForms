using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenOpticsElement { ConvergingLens, DivergingLens, Mirror }

    public sealed class ZenOpticsImage
    {
        public double Distance { get; private set; }
        public double Magnification { get; private set; }
        public bool IsReal { get; private set; }
        internal ZenOpticsImage(double distance, double magnification, bool isReal)
        { Distance = distance; Magnification = magnification; IsReal = isReal; }
    }
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Mirrors and lens lab.")]
    public class ZenOpticsLab : UserControl
    {
        private ZenOpticsElement _element = ZenOpticsElement.ConvergingLens;
        private double _focalLength = 5.0;
        private double _objectDistance = 8.0;
        private double _objectHeight = 2.0;
        private bool _dragging;
        private bool _invalid;
        private string _message = String.Empty;
        private const double MinDistance = 0.15;
        private const double MinFocal = 0.15;
        private ZenOpticsImage _image;

        public event EventHandler OpticsChanged;

        public ZenOpticsLab()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            Size = new Size(680, 390); MinimumSize = new Size(320, 250); TabStop = true;
            Recalculate();
        }

        [Category("Optics"), Description("Optical element type.")]
        public ZenOpticsElement Element { get { return _element; } set { if (!Enum.IsDefined(typeof(ZenOpticsElement), value)) throw new ArgumentOutOfRangeException("value"); if (_element != value) { _element = value; Recalculate(); } } }

        [Category("Optics"), Description("Focal length (absolute value), in arbitrary units.")]
        public double FocalLength { get { return _focalLength; } set { ValidatePositive(value, "value"); if (_focalLength != value) { _focalLength = value; Recalculate(); } } }
        [Category("Optics"), Description("Distance from the element to the object.")]
        public double ObjectDistance { get { return _objectDistance; } set { ValidatePositive(value, "value"); if (_objectDistance != value) { _objectDistance = value; Recalculate(); } } }
        [Category("Optics"), Description("Height of an object; the sign indicates its direction.")]
        public double ObjectHeight { get { return _objectHeight; } set { if (Double.IsNaN(value) || Double.IsInfinity(value) || Math.Abs(value) > 100000) throw new ArgumentOutOfRangeException("value"); if (_objectHeight != value) { _objectHeight = value; Recalculate(); } } }
        [Browsable(false)] public ZenOpticsImage Image { get { return _image; } }
        public void SetParameters(ZenOpticsElement element, double focalLength, double objectDistance)
        { ValidatePositive(focalLength, "focalLength"); ValidatePositive(objectDistance, "objectDistance"); if (!Enum.IsDefined(typeof(ZenOpticsElement), element)) throw new ArgumentOutOfRangeException("element"); _element = element; _focalLength = focalLength; _objectDistance = objectDistance; Recalculate(); }

        private static void ValidatePositive(double value, string name)
        { if (Double.IsNaN(value) || Double.IsInfinity(value) || value < MinDistance || value > 100000) throw new ArgumentOutOfRangeException(name, "The value must be finite and fall within the range 0.15-100,000."); }
        private double SignedFocal { get { return _element == ZenOpticsElement.DivergingLens ? -_focalLength : _focalLength; } }
        private void Recalculate()
        {
            double inv = 1.0 / SignedFocal - 1.0 / _objectDistance;
            _invalid = Math.Abs(inv) < 1e-9;
            if (_invalid) { _image = null; _message = "Image at infinity: 1/f − 1/do = 0"; }
            else
            {
                double di = 1.0 / inv;
                double m = -di / _objectDistance;
                if (Double.IsInfinity(di) || Double.IsNaN(di) || Math.Abs(di) > 1000000 || Double.IsInfinity(m))
                { _image = null; _invalid = true; _message = "Parameters outside the display range."; }
                else { _image = new ZenOpticsImage(di, m, di > 0); _message = String.Empty; }
            }
            Invalidate(); EventHandler handler = OpticsChanged; if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(ZenTheme.Background)) g.FillRectangle(bg, ClientRectangle);
            if (Width < 100 || Height < 100) return;
            int header = Math.Min(46, Height / 6), footer = Math.Min(66, Height / 4); 
            Rectangle area = new Rectangle(12, header, Math.Max(1, Width - 24), Math.Max(1, Height - header - footer));
            using (SolidBrush card = new SolidBrush(ZenTheme.Surface)) ZenOpticsGraphicsExtensions.FillRoundedRectangleCompat(g, card, area, 12);
            using (Pen border = new Pen(ZenTheme.Border)) g.DrawRoundedRectangleCompat(border, area, 12);
            using (Font title = new Font(Font.FontFamily, Math.Max(9f, Font.Size + 1f), FontStyle.Bold))
            using (SolidBrush white = new SolidBrush(ZenTheme.Foreground)) g.DrawString("ZEN OPTICS LAB", title, white, 18, 12);
            string mode = _element == ZenOpticsElement.Mirror ? "MIRROR" : (_element == ZenOpticsElement.DivergingLens ? "DIVERGING LENS" : "CONVERGING LENS");
            using (SolidBrush accent = new SolidBrush(ZenTheme.Accent)) g.DrawString(mode + "   |   f " + (_element == ZenOpticsElement.DivergingLens ? "−" : "+") + _focalLength.ToString("0.##"), Font, accent, Math.Max(150, Width - 240), 14);

            float midY = area.Top + area.Height * 0.57f;
            float centerX = area.Left + area.Width * 0.52f;
            float scale = (float)Math.Min(area.Width / Math.Max(12.0, _objectDistance + Math.Abs(_image == null ? 0 : _image.Distance) + 3), area.Height / 10.0);
            scale = Math.Max(1.5f, Math.Min(scale, 30f));
            float objX = centerX - (float)_objectDistance * scale;
            float focalX = centerX - (float)_focalLength * scale;
            using (Pen axis = new Pen(ZenTheme.Muted, 1)) { axis.DashStyle = DashStyle.Dash; g.DrawLine(axis, area.Left + 8, midY, area.Right - 8, midY); }
            using (Pen focal = new Pen(ZenTheme.Primary, 1)) { focal.DashStyle = DashStyle.Dot; g.DrawLine(focal, focalX, midY - 9, focalX, midY + 9); g.DrawLine(focal, centerX + (centerX - focalX), midY - 9, centerX + (centerX - focalX), midY + 9); }
            DrawElement(g, centerX, midY, scale);
            float objTip = midY - (float)_objectHeight * scale;
            DrawArrow(g, objX, midY, objTip, ZenTheme.Accent, 2.5f);
            using (SolidBrush label = new SolidBrush(ZenTheme.Muted)) g.DrawString("O", Font, label, objX - 6, midY + 5);
            if (_image != null)
            {
                float ix = centerX + (float)_image.Distance * scale;
                if (ix > area.Left + 4 && ix < area.Right - 4)
                {
                    float iy = midY - (float)(_objectHeight * _image.Magnification) * scale;
                    DrawRay(g, objX, objTip, centerX, objTip, ix, iy, ZenTheme.Accent);
                    DrawRay(g, objX, objTip, centerX, midY, ix, iy, ZenTheme.Primary);
                    DrawRay(g, objX, objTip, centerX, midY - (float)(_objectHeight * _image.Magnification) * scale, ix, iy, ZenTheme.Accent);
                    DrawArrow(g, ix, midY, iy, _image.IsReal ? ZenTheme.Primary : ZenTheme.Accent, 2.2f);
                    using (SolidBrush label = new SolidBrush(ZenTheme.Foreground)) g.DrawString(_image.IsReal ? "Real I." : "Virtual I.", Font, label, ix - 17, midY + 6);
                }
            }
            if (_element == ZenOpticsElement.DivergingLens && _image != null)
            { using (Pen ext = new Pen(Color.FromArgb(120, ZenTheme.Accent), 1)) { ext.DashStyle = DashStyle.Dash; float ix = centerX + (float)_image.Distance * scale; float iy = midY - (float)(_objectHeight * _image.Magnification) * scale; if (ix > area.Left && ix < area.Right) g.DrawLine(ext, centerX, midY, ix, iy); } }
            DrawFooter(g, footer);
        }
        private void DrawElement(Graphics g, float x, float y, float scale)
        {
            using (Pen p = new Pen(ZenTheme.Primary, 3))
            {
                if (_element == ZenOpticsElement.Mirror) { g.DrawArc(p, x - 8, y - 33, 18, 66, -90, 180); for (int k = -24; k <= 24; k += 12) g.DrawLine(p, x + 8, y + k, x + 14, y + k - 5); }
                else { g.DrawLine(p, x, y - 34, x, y + 34); if (_element == ZenOpticsElement.ConvergingLens) { g.DrawLine(p, x - 7, y - 28, x, y - 34); g.DrawLine(p, x + 7, y - 28, x, y - 34); g.DrawLine(p, x - 7, y + 28, x, y + 34); g.DrawLine(p, x + 7, y + 28, x, y + 34); } else { g.DrawLine(p, x - 7, y - 34, x, y - 28); g.DrawLine(p, x + 7, y - 34, x, y - 28); g.DrawLine(p, x - 7, y + 34, x, y + 28); g.DrawLine(p, x + 7, y + 34, x, y + 28); } }
            }
            using (SolidBrush b = new SolidBrush(ZenTheme.Primary)) g.FillEllipse(b, x - 4, y - 4, 8, 8);
        }
        private void DrawRay(Graphics g, float x1, float y1, float xc, float yc, float x2, float y2, Color color)
        { using (Pen p = new Pen(Color.FromArgb(210, color), 1.5f)) { g.DrawLine(p, x1, y1, xc, yc); if (_element == ZenOpticsElement.Mirror) g.DrawLine(p, xc, yc, x2, y2); else g.DrawLine(p, xc, yc, x2, y2); } }
        private static void DrawArrow(Graphics g, float x, float baseY, float tipY, Color color, float width)
        { using (Pen p = new Pen(color, width)) { g.DrawLine(p, x, baseY, x, tipY); float sign = tipY < baseY ? 1 : -1; g.DrawLine(p, x, tipY, x - 5, tipY + sign * 8); g.DrawLine(p, x, tipY, x + 5, tipY + sign * 8); } }
        private void DrawFooter(Graphics g, int height)
        {
            int y = Height - height + 2;
            string formula = _image == null ? _message : "1/f = 1/do + 1/di    |    di = " + _image.Distance.ToString("0.##") + "    m = " + _image.Magnification.ToString("0.##") + "    " + (_image.IsReal ? "real / inverse" : "virtual / right");
            using (SolidBrush accent = new SolidBrush(_invalid ? ZenTheme.Danger : ZenTheme.Foreground)) g.DrawString(formula, Font, accent, 16, y);
            using (SolidBrush muted = new SolidBrush(ZenTheme.Muted)) g.DrawString("Drag the arrow O to adjust.   •   F: change element   |   + / −: focus adjust", Font, muted, 16, y + 22);
            using (Pen p = new Pen(ZenTheme.Accent, 2)) g.DrawLine(p, 18, y + 47, 42, y + 47);
            using (SolidBrush b = new SolidBrush(ZenTheme.Muted)) g.DrawString("Principals", Font, b, 48, y + 39);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { float cx = Width * 0.52f; float sc = Math.Max(1.5f, Math.Min((float)Math.Min((Width - 24) / Math.Max(12.0, _objectDistance + Math.Abs(_image == null ? 0 : _image.Distance) + 3), Math.Max(1, Height - 100) / 10.0), 30f)); if (Math.Abs(e.X - (cx - _objectDistance * sc)) < 18) { _dragging = true; Capture = true; Focus(); } } }
        protected override void OnMouseMove(MouseEventArgs e)
        { base.OnMouseMove(e); if (_dragging) { float cx = Width * 0.52f; float sc = Math.Max(1.5f, Math.Min((float)Math.Min((Width - 24) / Math.Max(12.0, _objectDistance + Math.Abs(_image == null ? 0 : _image.Distance) + 3), Math.Max(1, Height - 100) / 10.0), 30f)); double d = Math.Max(MinDistance, Math.Min(100000, (cx - e.X) / sc)); ObjectDistance = d; } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _dragging = false; Capture = false; }
        protected override void OnKeyDown(KeyEventArgs e)
        { base.OnKeyDown(e); if (e.KeyCode == Keys.F) { Element = (ZenOpticsElement)(((int)Element + 1) % 3); e.Handled = true; } else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) { FocalLength = Math.Min(100000, FocalLength + 0.5); e.Handled = true; } else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) { FocalLength = Math.Max(MinFocal, FocalLength - 0.5); e.Handled = true; } }
    }

    internal static class ZenOpticsGraphicsExtensions
    {
        internal static void FillRoundedRectangleCompat(this Graphics g, Brush brush, Rectangle r, int radius)
        { using (GraphicsPath path = Rounded(r, radius)) g.FillPath(brush, path); }
        internal static void DrawRoundedRectangleCompat(this Graphics g, Pen pen, Rectangle r, int radius)
        { using (GraphicsPath path = Rounded(r, radius)) g.DrawPath(pen, path); }
        private static GraphicsPath Rounded(Rectangle r, int radius)
        { GraphicsPath p = new GraphicsPath(); int d = Math.Max(2, radius * 2); p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
    }
}

// watashi wa watashi sore dake