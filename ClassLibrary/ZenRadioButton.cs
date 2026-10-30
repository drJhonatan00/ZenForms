using System.ComponentModel; //watashi wa watashi sore dake
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(RadioButton))][Category("ZenForms Modern Controls")] public class ZenRadioButton:RadioButton { public ZenRadioButton(){AutoSize=true;FlatStyle=FlatStyle.Flat; Color.FromArgb(10, 10, 10); ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;FlatAppearance.BorderColor=ZenTheme.Border;FlatAppearance.CheckedBackColor=ZenTheme.Primary;} } }