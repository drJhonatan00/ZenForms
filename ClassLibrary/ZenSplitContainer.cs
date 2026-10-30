using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(SplitContainer))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenSplitContainer:SplitContainer { public ZenSplitContainer(){BackColor=ZenTheme.Background;ForeColor=ZenTheme.Foreground;BorderStyle=BorderStyle.None;SplitterWidth=5;Panel1.BackColor=ZenTheme.Surface;Panel2.BackColor=ZenTheme.Surface;Panel1.Padding=new Padding(8);Panel2.Padding=new Padding(8);} } }


// watashi wa watashi sore dake