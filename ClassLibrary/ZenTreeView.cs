using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core { [ToolboxBitmap(typeof(TreeView))][System.ComponentModel.Category("ZenForms Modern Controls")] public class ZenTreeView:TreeView { public ZenTreeView(){BorderStyle=BorderStyle.None;BackColor=ZenTheme.Surface;ForeColor=ZenTheme.Foreground;Font=ZenTheme.DefaultFont;HideSelection=false;FullRowSelect=true;ShowLines=false;ShowPlusMinus=true;DrawMode=TreeViewDrawMode.OwnerDrawText;ItemHeight=26;} protected override void OnDrawNode(DrawTreeNodeEventArgs e){if(e.Node==null)return;bool selected=(e.State&TreeNodeStates.Selected)==TreeNodeStates.Selected;Rectangle row=new Rectangle(0,e.Bounds.Top,ClientSize.Width,e.Bounds.Height);using(SolidBrush b=new SolidBrush(selected?ZenTheme.Primary:BackColor))e.Graphics.FillRectangle(b,row);TextRenderer.DrawText(e.Graphics,e.Node.Text,Font,e.Bounds,selected?Color.White:ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);} } }
















//watashi wa watashi sore dake