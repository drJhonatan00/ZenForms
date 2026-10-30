using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(Panel))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenPanel:Panel { public int CornerRadius{get;set;}=12; public Color BorderColor{get;set;}=ZenTheme.Border; public ZenPanel(){BackColor=ZenTheme.Surface;Padding=new Padding(12);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);} protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Rectangle r=new Rectangle(0,0,Width-1,Height-1);using(GraphicsPath p=ZenButtonBase.RoundPath(r,CornerRadius)){using(SolidBrush b=new SolidBrush(BackColor))e.Graphics.FillPath(b,p);using(Pen pen=new Pen(BorderColor))e.Graphics.DrawPath(pen,p);}base.OnPaint(e);} } }

//watashi wa watashi sore dake