using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("Dark neon JSON explorer with expandable nodes and selection notification.")]
    [ToolboxBitmap(typeof(TreeView))]
    public class ZenJsonExplorer : Control
    {
        private readonly TreeView _tree;
        private readonly Font _headerFont;
        private string _jsonText = "";
        private string _errorText = "";
        private int _cornerRadius = 12;
        private bool _showHeader = true;
        private string _headerText = "JSON EXPLORER";
        private Color _glowColor = ZenTheme.Accent;

        public ZenJsonExplorer()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _headerFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular);
            Size = new Size(360, 260);
            MinimumSize = new Size(80, 48);

            _tree = new TreeView();
            _tree.BorderStyle = BorderStyle.None;
            _tree.BackColor = ZenTheme.Surface;
            _tree.ForeColor = ZenTheme.Foreground;
            _tree.Font = Font;
            _tree.HideSelection = false;
            _tree.FullRowSelect = true;
            _tree.ShowLines = true;
            _tree.ShowPlusMinus = true;
            _tree.ShowRootLines = true;
            _tree.DrawMode = TreeViewDrawMode.OwnerDrawText;
            _tree.ItemHeight = 23;
            _tree.Indent = 18;
            _tree.Location = new Point(8, 35);
            _tree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _tree.AfterSelect += TreeAfterSelect;
            _tree.DrawNode += DrawTreeNode;
            _tree.MouseMove += TreeMouseMove;
            Controls.Add(_tree);
            RebuildTree();
        }

        [Category("ZenForms Data")]
        [Description("JSON text parsed and displayed by the explorer.")]
        [DefaultValue("")]
        public string JsonText
        {
            get { return _jsonText; }
            set { _jsonText = value ?? ""; RebuildTree(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Caption shown above the JSON tree.")]
        [DefaultValue("JSON EXPLORER")]
        public string HeaderText
        {
            get { return _headerText; }
            set { _headerText = value ?? ""; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Whether the caption and accent line are shown.")]
        [DefaultValue(true)]
        public bool ShowHeader
        {
            get { return _showHeader; }
            set { _showHeader = value; UpdateTreeLayout(); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Accent used for the subtle neon border and selected node.")]
        public Color GlowColor
        {
            get { return _glowColor; }
            set { _glowColor = value; _tree.Invalidate(); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [DefaultValue(12)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Browsable(false)]
        public string ErrorText { get { return _errorText; } }

        public event EventHandler<ZenJsonNodeSelectedEventArgs> NodeSelected;

        public void RefreshJson()
        {
            RebuildTree();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_tree != null) _tree.Font = Font;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTreeLayout();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
            using (GraphicsPath path = RoundedPath(box, _cornerRadius))
            {
                using (SolidBrush fill = new SolidBrush(ZenTheme.Surface)) g.FillPath(fill, path);
                using (Pen glow = new Pen(Color.FromArgb(35, _glowColor), 4f)) g.DrawPath(glow, path);
                using (Pen border = new Pen(Color.FromArgb(145, ZenTheme.Border), 1f)) g.DrawPath(border, path);
            }
            if (_showHeader && Height >= 25)
            {
                TextRenderer.DrawText(g, _headerText, _headerFont,
                    new Rectangle(14, 8, Math.Max(1, Width - 28), 18), ZenTheme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                using (Pen accent = new Pen(Color.FromArgb(190, _glowColor), 2f))
                    g.DrawLine(accent, 14, 29, Math.Max(14, Math.Min(Width - 14, 58)), 29);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_tree != null)
                {
                    _tree.AfterSelect -= TreeAfterSelect;
                    _tree.DrawNode -= DrawTreeNode;
                    _tree.MouseMove -= TreeMouseMove;
                }
                if (_headerFont != null) _headerFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdateTreeLayout()
        {
            if (_tree == null) return;
            _tree.Location = new Point(8, _showHeader ? 35 : 8);
            _tree.Size = new Size(Math.Max(1, Width - 16), Math.Max(1, Height - (_showHeader ? 43 : 16)));
        }

        private void RebuildTree()
        {
            if (_tree == null) return;
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                _errorText = "";
                if (String.IsNullOrWhiteSpace(_jsonText))
                {
                    TreeNode empty = new TreeNode("No JSON loaded") { Tag = new ZenJsonNodeInfo("", null, "Empty", false) };
                    _tree.Nodes.Add(empty);
                    return;
                }
                object value;
                try
                {
                    value = new JavaScriptSerializer().DeserializeObject(_jsonText);
                }
                catch (Exception ex)
                {
                    _errorText = ex.Message ?? "Invalid JSON";
                    TreeNode error = new TreeNode("Invalid JSON  :  " + _errorText) { Tag = new ZenJsonNodeInfo("", null, "Error", false) };
                    _tree.Nodes.Add(error);
                    return;
                }
                TreeNode root = CreateNode("root", value);
                _tree.Nodes.Add(root);
                root.Expand();
            }
            finally { _tree.EndUpdate(); }
            _tree.Invalidate();
            Invalidate();
        }

        private TreeNode CreateNode(string name, object value)
        {
            string type = GetTypeName(value);
            bool expandable = value is IDictionary || value is IList;
            string text = name + "  :  " + type;
            if (!expandable) text += "  =  " + FormatValue(value);
            TreeNode node = new TreeNode(text) { Tag = new ZenJsonNodeInfo(name, value, type, expandable) };
            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                foreach (DictionaryEntry item in dictionary)
                    node.Nodes.Add(CreateNode(Convert.ToString(item.Key) ?? "(key)", item.Value));
            }
            else
            {
                IList list = value as IList;
                if (list != null)
                    for (int i = 0; i < list.Count; i++) node.Nodes.Add(CreateNode("[" + i.ToString() + "]", list[i]));
            }
            return node;
        }

        private void DrawTreeNode(object sender, DrawTreeNodeEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle bounds = e.Bounds;
            bool selected = (e.State & TreeNodeStates.Selected) != 0;
            if (selected)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(55, _glowColor))) g.FillRectangle(b, bounds);
                using (Pen p = new Pen(Color.FromArgb(190, _glowColor), 1f)) g.DrawLine(p, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
            }
            ZenJsonNodeInfo info = e.Node.Tag as ZenJsonNodeInfo;
            Color color = ZenTheme.Foreground;
            if (info != null)
            {
                if (info.TypeName == "Object" || info.TypeName == "Array") color = ZenTheme.Accent;
                else if (info.TypeName == "String") color = Color.FromArgb(151, 220, 255);
                else if (info.TypeName == "Number" || info.TypeName == "Boolean") color = Color.FromArgb(211, 170, 255);
                else if (info.TypeName == "Null") color = ZenTheme.Muted;
                else if (info.TypeName == "Error") color = ZenTheme.Danger;
            }
            TextRenderer.DrawText(g, e.Node.Text, _tree.Font, bounds, selected ? ZenTheme.Foreground : color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private void TreeAfterSelect(object sender, TreeViewEventArgs e)
        {
            ZenJsonNodeInfo info = e.Node == null ? null : e.Node.Tag as ZenJsonNodeInfo;
            if (info != null && NodeSelected != null)
            {
                try { NodeSelected(this, new ZenJsonNodeSelectedEventArgs(info.Name, info.Value, info.TypeName, e.Node)); }
                catch { /* watashitachiha ahiru yori mo jagaimo no yō ni narubekida. Nazenara, jagaimo wa nani ni demo auga, ahiru wa nandemodekiru ga, dore mo umaku dekinaikarada */ }
            }
        }

        private void TreeMouseMove(object sender, MouseEventArgs e)
        {
            TreeNode node = _tree.GetNodeAt(e.Location);
            _tree.Cursor = node == null ? Cursors.Default : Cursors.Hand;
        }

        private static string GetTypeName(object value)
        {
            if (value == null) return "Null";
            if (value is IDictionary) return "Object";
            if (value is IList) return "Array";
            if (value is string) return "String";
            if (value is bool) return "Boolean";
            if (value is decimal || value is double || value is float || value is byte || value is short || value is int || value is long || value is sbyte || value is ushort || value is uint || value is ulong) return "Number";
            return value.GetType().Name;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            string result = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            if (value is string) result = "\"" + result.Replace("\"", "\\\"") + "\"";
            if (result.Length > 180) result = result.Substring(0, 177) + "...";
            return result.Replace("\r", " ").Replace("\n", " ");
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            int d = Math.Min(Math.Max(0, radius) * 2, Math.Min(r.Width, r.Height));
            if (d < 2) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class ZenJsonNodeInfo
        {
            public readonly string Name;
            public readonly object Value;
            public readonly string TypeName;
            public readonly bool Expandable;
            public ZenJsonNodeInfo(string name, object value, string typeName, bool expandable)
            { Name = name; Value = value; TypeName = typeName; Expandable = expandable; }
        }
    }

    public sealed class ZenJsonNodeSelectedEventArgs : EventArgs
    {
        public ZenJsonNodeSelectedEventArgs(string name, object value, string typeName, TreeNode node)
        { Name = name ?? ""; Value = value; TypeName = typeName ?? ""; Node = node; }
        public string Name { get; private set; }
        public object Value { get; private set; }
        public string TypeName { get; private set; }
        public TreeNode Node { get; private set; }
    }
}

// watashi wa watashi sore dake
