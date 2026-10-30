using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenFilesDroppedEventArgs : EventArgs { public string[] Files { get; private set; } public ZenFilesDroppedEventArgs(string[] files) { Files = files; } }
    [Category("ZenForms Modern Component")]
    [Description("File drag-and-drop zone with a list and drop event handlers.")]
    public class ZenFileDropZone : UserControl
    {
        private readonly Label prompt; private readonly ListBox list; private readonly List<string> files = new List<string>(); private string filter = "*.*"; private Color accent = ZenTheme.Primary;
        [Category("ZenForms Files"), DefaultValue("*.*")] public string ExtensionFilter { get { return filter; } set { filter = String.IsNullOrWhiteSpace(value) ? "*.*" : value; } }
        [Category("ZenForms Appearance")] public Color AccentColor { get { return accent; } set { accent = value; Invalidate(); } }
        [Browsable(false)] public IList<string> Files { get { return files.AsReadOnly(); } }
        public event EventHandler<ZenFilesDroppedEventArgs> FilesAdded;
        public ZenFileDropZone() { Size = new Size(420, 220); MinimumSize = new Size(200, 120); AllowDrop = true; BackColor = ZenTheme.Surface; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Panel head = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ZenTheme.Surface }; prompt = new Label { Dock = DockStyle.Fill, Text = "⇩   Drag files here\r\nor click to select", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 10f), ForeColor = ZenTheme.Foreground, Cursor = Cursors.Hand }; head.Controls.Add(prompt); Panel actions = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = ZenTheme.Surface }; Button browse = new Button { Dock = DockStyle.Left, Width = 100, Text = "Select…", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground }; Button remove = new Button { Dock = DockStyle.Right, Width = 86, Text = "Remove all", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Muted }; actions.Controls.Add(browse); actions.Controls.Add(remove); list = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8.5f), IntegralHeight = false }; Controls.Add(list); Controls.Add(actions); Controls.Add(head); browse.Click += (s, e) => Browse(); remove.Click += (s, e) => ClearFiles(); prompt.Click += (s, e) => Browse(); DragEnter += OnDragEnter; DragDrop += OnDragDrop; list.DragEnter += OnDragEnter; list.DragDrop += OnDragDrop; prompt.AllowDrop = true; prompt.DragEnter += OnDragEnter; prompt.DragDrop += OnDragDrop; }
        public void AddFiles(IEnumerable<string> paths) { List<string> accepted = new List<string>(); foreach (string path in paths ?? Enumerable.Empty<string>()) { if (!File.Exists(path) || !Matches(path) || files.Contains(path, StringComparer.OrdinalIgnoreCase)) continue; files.Add(path); accepted.Add(path); list.Items.Add(Path.GetFileName(path) + "     •     " + FormatSize(new FileInfo(path).Length)); } if (accepted.Count > 0) { EventHandler<ZenFilesDroppedEventArgs> h = FilesAdded; if (h != null) h(this, new ZenFilesDroppedEventArgs(accepted.ToArray())); } Invalidate(); }
        public void ClearFiles() { files.Clear(); list.Items.Clear(); Invalidate(); }
        private void Browse() { using (OpenFileDialog d = new OpenFileDialog()) { d.Title = "Choose the files"; d.Multiselect = true; d.Filter = BuildFilter(); if (d.ShowDialog(FindForm()) == DialogResult.OK) AddFiles(d.FileNames); } }
        private string[] Patterns() { List<string> result = new List<string>(); foreach (string item in filter.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)) { string x = item.Trim(); if (x == "*.*") return new string[] { "*.*" }; if (!x.StartsWith("*")) x = x.StartsWith(".") ? "*" + x : "*." + x.TrimStart('.'); result.Add(x); } return result.Count == 0 ? new string[] { "*.*" } : result.ToArray(); }
        private string BuildFilter() { string[] patterns = Patterns(); string pattern = String.Join(";", patterns); return "Permitted files (" + pattern + ")|" + pattern + "|All files|*.*"; }
        private bool Matches(string path) { string ext = Path.GetExtension(path); foreach (string x in Patterns()) if (x == "*.*" || String.Equals(x, "*" + ext, StringComparison.OrdinalIgnoreCase)) return true; return false; }
        private void OnDragEnter(object sender, DragEventArgs e) { e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; }
        private void OnDragDrop(object sender, DragEventArgs e) { string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[]; if (paths != null) AddFiles(paths); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using (Pen p = new Pen(accent, 2f)) { p.DashPattern = new float[] { 5, 4 }; e.Graphics.DrawRectangle(p, 1, 1, Width - 3, Height - 3); } }
        private static string FormatSize(long bytes) { string[] units = { "B", "KB", "MB", "GB" }; double n = bytes; int i = 0; while (n >= 1024 && i < units.Length - 1) { n /= 1024; i++; } return n.ToString(i == 0 ? "0" : "0.0") + " " + units[i]; }
    }
}

// watashi wa watashi sore dake