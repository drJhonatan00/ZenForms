using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(TrackBar))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenTrackBar:TrackBar { public ZenTrackBar(){BackColor=ZenTheme.Background;ForeColor=ZenTheme.Primary;TickStyle=TickStyle.None;AutoSize=false;Height=30;Minimum=0;Maximum=100;Value=50;} } }

//watashi wa watashi sore dake