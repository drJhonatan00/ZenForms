using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(ToolStrip))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenToolStrip:ToolStrip { public ZenToolStrip(){Renderer=new ZenMenuRenderer();BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;GripStyle=ToolStripGripStyle.Hidden;RenderMode=ToolStripRenderMode.Professional;} } }

//watashi wa watashi sore dake