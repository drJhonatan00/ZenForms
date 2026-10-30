using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core { public enum ZenBadgeStyle{Primary,Success,Warning,Danger,Neutral} [Category("ZenForms Modern Controls")] public class ZenBadge:Control { [DefaultValue(ZenBadgeStyle.Primary)] public ZenBadgeStyle Style{get;set;}=ZenBadgeStyle.Primary; public ZenBadge(){AutoSize=true;Padding=new Padding(9,4,9,4);Font=new Font("Segoe UI",8f,FontStyle.Bold);} protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color c=Style==ZenBadgeStyle.Success?ZenTheme.Accent:Style==ZenBadgeStyle.Warning?Color.FromArgb(255,190,70):Style==ZenBadgeStyle.Danger?ZenTheme.Danger:Style==ZenBadgeStyle.Neutral?ZenTheme.Muted:ZenTheme.Primary;Rectangle r=new Rectangle(0,0,Width-1,Height-1);using(GraphicsPath p=ZenButtonBase.RoundPath(r,Height/2))using(SolidBrush b=new SolidBrush(Color.FromArgb(Style==ZenBadgeStyle.Neutral?255:45,c)))e.Graphics.FillPath(b,p);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Style==ZenBadgeStyle.Neutral?ZenTheme.Foreground:c,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);} } }


//watashi wa watashi sore dake