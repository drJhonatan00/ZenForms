using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenFormsModernControls")]
    [Description("Expandable section with header and area for child controls")]
    public class ZenAccordion : UserControl
    {
        private readonly Button header;
        private readonly Panel content;
        private bool expanded = true;
        private int contentHeight = 160;
        private int headerHeight = 40;
        [Category("ZenForms Data"), DefaultValue("Section")]
        public override string Text { get { return header == null ? base.Text : header.Text.Substring(2); } set { if (header != null) { header.Text = (expanded ? "∨ " : "> ") + (value ?? ""); } else { base.Text = value; } } }
        [Category("ZenForms Accordion"), DefaultValue(160)]
        public bool Expanded { get { return expanded; } set { if (expanded == value) return; expanded = value; ApplyState(); EventHandler h = ExpandedChanged; if (h != null) h(this, EventArgs.Empty); } }
        [Category("ZenForms Accordion"), DefaultValue(160)]
        public int ContentHeight { get { return contentHeight; } set { contentHeight = Math.Max(40, value);ApplyState(); } }
        [Browsable(false)]
        public Control.ControlCollection ContentControls { get { return content.Controls; } }
        [Browsable(false)]
        public Panel ContentPanel { get { return content; } }
        public event EventHandler ExpandedChanged;
        public ZenAccordion()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Surface;
            Width = 320;
            header = new Button { Dock = DockStyle.Top, Height = headerHeight, Text = "∨ Section", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = new Font("Segoe UI Semibold", 9.5f), Cursor = Cursors.Hand };
            header.FlatAppearance.BorderSize = 0;
            content = new Panel { Dock = DockStyle.Top, Height = contentHeight, BackColor = ZenTheme.Surface, Padding = new Padding(12), AutoScroll = true };
            Controls.Add(content);
            Controls.Add(header);
            header.Click += (s, e) => Expanded = !Expanded;
            Height = headerHeight + contentHeight;


        }
        private void ApplyState()
        {
            if(header == null)return;header.Text = (expanded ? "∨ " : "> ") + Text;content.Visible = expanded;content.Height = contentHeight;Height = headerHeight + (expanded ? contentHeight : 0);PerformLayout();
        }
    }
}

//watashi wa watashi sore dake

