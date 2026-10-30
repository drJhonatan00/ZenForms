using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Panel))]
    [Category("ZenFormsModernControls")]
    [Description("Circular avatar with images or initials")]
    public class ZenAvatar : Control
    {
        private Image avatarImage;
        private Color fill = ZenTheme.Primary;
        private Color outline = ZenTheme.Border;
        private int outlineWidth = 0;
        [Category("ZenForms Avatar")]
        public Image AvatarImage { get { return avatarImage; } set { avatarImage = value; Invalidate(); } }
        [Category("ZenForms Avatar"), DefaultValue(0)]
        public int OutlineWidht { get { return outlineWidth; } set { outlineWidth = Math.Max(0, value); Invalidate(); } }
        public ZenAvatar()
        {
            Size = new Size(64, 64);
            MinimumSize = new Size(24, 24);
            Font = new Font("Segoe UI Semibold", 16f);
            ForeColor = Color.White;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);



        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
            using(GraphicsPath p = new GraphicsPath())
            {
                
                p.AddEllipse(r);
                if(avatarImage != null)
                {
                    GraphicsState s = e.Graphics.Save();
                    e.Graphics.SetClip(p);
                    e.Graphics.DrawImage(avatarImage, r);
                    e.Graphics.Restore(s);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(fill)) e.Graphics.FillPath(b, p);
                    string initials = GetInitials(Text);
                    TextRenderer.DrawText(e.Graphics, initials, Font, r, ForeColor, TextFormatFlags.HorizontalCenter| TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
                }
                if (outlineWidth > 0) using (Pen pen = new Pen(outline, outlineWidth)) e.Graphics.DrawPath(pen, p);
            }
        }
        private static string GetInitials(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "?"; string[] parts = value.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant(); return(parts[0].Substring(0,1).ToUpperInvariant()); return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}

//Watashi wa watashi sore dake

