using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(NumericUpDown))][Category("ZenForms Modern Controls")] public class ZenNumericUpDown:NumericUpDown { public ZenNumericUpDown(){BorderStyle=BorderStyle.FixedSingle;BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;Size=new Size(120,28);} } }

//watashi wa watashi sore dake