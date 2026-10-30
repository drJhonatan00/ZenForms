using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(RichTextBox))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenRichTextBox:RichTextBox { public ZenRichTextBox(){BorderStyle=BorderStyle.None;BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;DetectUrls=true;EnableAutoDragDrop=true;Padding=new Padding(8);} } }
// watashi wa watashi sore dake