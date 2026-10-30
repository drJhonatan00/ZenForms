using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(CheckBox))][Category("ZenForms Modern Controls")] public class ZenCheckBox:CheckBox { public ZenCheckBox(){AutoSize=true;FlatStyle=FlatStyle.Flat;BackColor=Color.FromArgb(36, 50, 73); ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;FlatAppearance.BorderColor=ZenTheme.Border;FlatAppearance.CheckedBackColor=ZenTheme.Primary;} } }


//  Watashi wa watashi sore dake