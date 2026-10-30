using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(MaskedTextBox))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenMaskedTextBox:MaskedTextBox { public ZenMaskedTextBox(){BorderStyle=BorderStyle.FixedSingle;BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;TextMaskFormat=MaskFormat.IncludeLiterals;} } }

//watashi wa watashi sore dake