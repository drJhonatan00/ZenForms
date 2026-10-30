using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Button))]
    [Category("ZenForms Modern Controls")]
    [Description("A actual blue button.")]
    public class ZenButtonBase : Control
    {
        private int _cornerRadius = 9;
        private int _borderWidth;
        private Color _normalBackColor = Color.FromArgb(36, 50, 73);
        private Color _hoverBackColor = Color.FromArgb(56, 189, 248);
        private Color _textColor = Color.FromArgb(226, 232, 240);
        private Color _borderColor = Color.FromArgb(226, 232, 240);
        private bool _hovered, _pressed, _focused;

        public ZenButtonBase()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(150, 42); MinimumSize = new Size(32, 24); Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Regular); BackColor = _normalBackColor; ForeColor = _textColor; TabStop = true;
        }
        [Category("ZenForms Appearance"), DefaultValue(9)] public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = Math.Max(0, value); Invalidate(); } }
        [Category("ZenForms Appearance")] public Color NormalBackColor { get { return _normalBackColor; } set { _normalBackColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color HoverBackColor { get { return _hoverBackColor; } set { _hoverBackColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color TextColor { get { return _textColor; } set { _textColor = value; Invalidate(); } }
        [Category("ZenForms Appearance")] public Color BorderColor { get { return _borderColor; } set { _borderColor = value; Invalidate(); } }
        [Category("ZenForms Appearance"), DefaultValue(0)] public int BorderWidth { get { return _borderWidth; } set { _borderWidth = Math.Max(0, value); Invalidate(); } }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Focus(); Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); _focused = true; Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _focused = false; Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { _pressed = true; Invalidate(); } }
        protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); if (_pressed && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)) { _pressed = false; Invalidate(); OnClick(EventArgs.Empty); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle r=ClientRectangle; r.Width=Math.Max(0,r.Width-1); r.Height=Math.Max(0,r.Height-1); if(r.Width<1||r.Height<1)return;
            if(_pressed){r.Offset(0,1); r.Height=Math.Max(1,r.Height-1);}
            Color fill=_hovered?_hoverBackColor:_normalBackColor; if(_pressed) fill=ControlPaint.Dark(fill,.08f);
            using(GraphicsPath path=RoundPath(r,_cornerRadius))
            {
                using(SolidBrush b=new SolidBrush(fill))g.FillPath(b,path);
                if(_borderWidth>0){using(Pen p=new Pen(_borderColor,_borderWidth)){p.Alignment=PenAlignment.Inset;g.DrawPath(p,path);}}
                if(_focused){Rectangle focus=Rectangle.Inflate(r,-4,-4);ControlPaint.DrawFocusRectangle(g,focus,Color.FromArgb(110,_textColor),Color.Transparent);}
            }
            TextFormatFlags flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine;
            Rectangle textRect=ClientRectangle; if(_pressed)textRect.Offset(0,1); TextRenderer.DrawText(g,Text,Font,textRect,_textColor,flags);
        }
        internal static GraphicsPath RoundPath(Rectangle r,int radius)
        {
            GraphicsPath p=new GraphicsPath(); if(r.Width<=0||r.Height<=0)return p; int d=Math.Min(Math.Max(0,radius)*2,Math.Min(r.Width,r.Height));
            if(d<2){p.AddRectangle(r);return p;} p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
        }
    }
}


//watashi wa watashi sore dake
