using System.ComponentModel; //watashi wa watashi sore dake
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(ListBox))][Category("ZenForms Modern Controls")] public class ZenListBox:ListBox { public ZenListBox(){BorderStyle=BorderStyle.FixedSingle;BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;IntegralHeight=false;ItemHeight=24;} } }