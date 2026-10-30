using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public enum ZenNotificationType { Information, Success, Warning, Error }
    [Category("ZenForms Modern Controls")]
    [Description("Colored inline message with variants and a dismiss action")]
    public class ZenNotification : UserControl
    {
        private readonly Panel stripe; private readonly Label messageLabel; private readonly Button closeButton; private ZenNotificationType type;
        [Category("ZenForms Notification"), DefaultValue(ZenNotificationType.Information)] public ZenNotificationType Type { get { return type; } set { type = value; ApplyType(); } }
        [Category("ZenForms Notification")] public override string Text { get { return messageLabel == null ? base.Text : messageLabel.Text; } set { if (messageLabel != null) messageLabel.Text = value ?? ""; else base.Text = value; } }
        [Category("ZenForms Notification"), DefaultValue(true)] public bool ShowCloseButton { get { return closeButton.Visible; } set { closeButton.Visible = value; } }
        public event EventHandler Dismissed;
        public ZenNotification() { Height = 44; MinimumSize = new Size(120, 36); BackColor = ZenTheme.Surface; Padding = new Padding(0); stripe = new Panel { Dock = DockStyle.Left, Width = 4 }; closeButton = new Button { Dock = DockStyle.Right, Width = 34, Text = "×", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 12f), Cursor = Cursors.Hand }; closeButton.FlatAppearance.BorderSize = 0; messageLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 6, 0), ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, AutoEllipsis = true }; Controls.Add(messageLabel); Controls.Add(closeButton); Controls.Add(stripe); closeButton.Click += (s, e) => { Visible = false; EventHandler h = Dismissed; if (h != null) h(this, EventArgs.Empty); }; ApplyType(); }
        private void ApplyType() { if (stripe == null) return; Color c = type == ZenNotificationType.Success ? ZenTheme.Accent : type == ZenNotificationType.Warning ? Color.FromArgb(255, 190, 70) : type == ZenNotificationType.Error ? ZenTheme.Danger : ZenTheme.Primary; stripe.BackColor = c; BackColor = Color.FromArgb(34, c); messageLabel.ForeColor = ZenTheme.Foreground; }
    }
}
