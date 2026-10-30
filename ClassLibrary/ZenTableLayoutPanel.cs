using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(TableLayoutPanel))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenTableLayoutPanel:TableLayoutPanel { public ZenTableLayoutPanel(){BackColor=ZenTheme.Background;ForeColor=ZenTheme.Foreground;Padding=new Padding(6);GrowStyle=TableLayoutPanelGrowStyle.AddRows;} } }
// watashi wa watashi sore dake