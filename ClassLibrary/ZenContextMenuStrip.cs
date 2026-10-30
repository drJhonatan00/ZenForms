using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(ContextMenuStrip))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenContextMenuStrip:ContextMenuStrip { public ZenContextMenuStrip(){Renderer=new ZenMenuRenderer();BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;ShowImageMargin=false;RenderMode=ToolStripRenderMode.Professional;} } }


// watashi wa watashi sore dake