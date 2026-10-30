using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core { [Category("ZenForms Modern Controls")] public class ZenCard:Panel { [DefaultValue(16)] public int CornerRadius{get;set;}=16; public ZenCard(){BackColor=ZenTheme.Surface;Padding=new Padding(18);Size=new Size(300,180);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);} protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Rectangle r=new Rectangle(0,0,Width-1,Height-1);using(GraphicsPath p=ZenButtonBase.RoundPath(r,CornerRadius)){using(SolidBrush b=new SolidBrush(BackColor))e.Graphics.FillPath(b,p);using(Pen pen=new Pen(ZenTheme.Border))e.Graphics.DrawPath(pen,p);}using(LinearGradientBrush g=new LinearGradientBrush(new Rectangle(16,0,System.Math.Max(1,Width-32),2),ZenTheme.Primary,ZenTheme.Accent,0f))e.Graphics.FillRectangle(g,16,0,System.Math.Max(1,Width-32),2);base.OnPaint(e);} } }


//Watashi wa watashi sore dake