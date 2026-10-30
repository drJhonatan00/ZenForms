using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(DateTimePicker))][Category("ZenForms Modern Controls")] public class ZenDateTimePicker:DateTimePicker { public ZenDateTimePicker(){CalendarMonthBackground=ZenTheme.Surface;CalendarForeColor=ZenTheme.Foreground;CalendarTitleBackColor=ZenTheme.Primary;CalendarTitleForeColor=Color.White;CalendarTrailingForeColor=ZenTheme.Muted;Font=ZenTheme.DefaultFont;Format=DateTimePickerFormat.Short;} } }

//watashi wa watashi sore dake