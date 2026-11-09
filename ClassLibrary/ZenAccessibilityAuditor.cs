using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum AccessibilityFindingSeverity { Info, Warning, Error }

    public sealed class AccessibilityFinding
    {
        internal AccessibilityFinding(AccessibilityFindingSeverity severity, string type, string target, string control, string guidance, Control instance)
        { Severity = severity; Type = type; Target = target; ControlDescription = control; Guidance = guidance; TargetControl = instance; }
        public AccessibilityFindingSeverity Severity { get; private set; }
        public string Type { get; private set; }
        public string Target { get; private set; }
        public string ControlDescription { get; private set; }
        public string Guidance { get; private set; }
        [Browsable(false)] public Control TargetControl { get; private set; }
    }

    public sealed class AccessibilityAuditCompletedEventArgs : EventArgs
    {
        internal AccessibilityAuditCompletedEventArgs(int count, bool truncated) { FindingCount = count; ResultsTruncated = truncated; }

        public int FindingCount { get; private set; }

        public bool ResultsTruncated { get; private set; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Heuristic, non-destructive audit of the provided WinForms control tree.")]
    public class ZenAccessibilityAuditor : UserControl
    {
        private readonly int _uiThreadId;
        private readonly List<AccessibilityFinding> _findings = new List<AccessibilityFinding>();
        private readonly DataGridView _grid;
        private readonly Label _status;
        private Control _rootControl;
        private const int MaximumFindings = 500;

        public ZenAccessibilityAuditor()
        {
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(440, 260); Size = new Size(760, 430);
            TableLayoutPanel layout = new TableLayoutPanel(); layout.Dock = DockStyle.Fill; layout.Padding = new Padding(10); layout.ColumnCount = 1; layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); Controls.Add(layout);
            _grid = new DataGridView(); _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false; _grid.MultiSelect = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoGenerateColumns = false; _grid.RowHeadersVisible = false; _grid.BackgroundColor = ZenTheme.Surface; _grid.BorderStyle = BorderStyle.FixedSingle;
            _grid.EnableHeadersVisualStyles = false; _grid.ColumnHeadersDefaultCellStyle.BackColor = ZenTheme.SurfaceRaised; _grid.ColumnHeadersDefaultCellStyle.ForeColor = ZenTheme.Foreground;
            _grid.DefaultCellStyle.BackColor = ZenTheme.Surface; _grid.DefaultCellStyle.ForeColor = ZenTheme.Foreground; _grid.DefaultCellStyle.SelectionBackColor = ZenTheme.Primary; _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            AddColumn("Severity", "Severity", 75); AddColumn("Target", "Target", 160); AddColumn("Type", "Type", 125); AddColumn("ControlDescription", "ControlDescription", 170); AddColumn("Guidance", "Guidance", 300);
            _grid.CellDoubleClick += NavigateSelection; layout.Controls.Add(_grid, 0, 0);
            _status = new Label(); _status.Dock = DockStyle.Fill; _status.ForeColor = ZenTheme.Muted; _status.TextAlign = ContentAlignment.MiddleLeft; layout.Controls.Add(_status, 0, 1);
            _status.Text = "Define RootControl and start RunAudit(). Double-click try localize the target.";
        }
        [Category("Behavior"), Description("The WinForms controls root was analyzed by RunAudit().")]
        public Control RootControl { get { return _rootControl; } set { EnsureUiThread(); _rootControl = value; } }

        [Browsable(false)] public IList<AccessibilityFinding> Findings { get { return _findings.AsReadOnly(); } }

        public event EventHandler<AccessibilityAuditCompletedEventArgs> AuditCompleted;

        public void RunAudit()
        {
            EnsureUiThread(); _findings.Clear(); _grid.Rows.Clear();
            if (_rootControl == null || _rootControl.IsDisposed) { _status.Text = "Define a valid root on RootControl."; RaiseCompleted(false); return; }
            bool truncated = false;
            try { Scan(_rootControl, _rootControl.Name, ref truncated); }
            catch (ObjectDisposedException) { _status.Text = "The tree was changed or deleted; start again."; }
            catch (InvalidOperationException) { _status.Text = "The tree was changed; start again."; }
            foreach (AccessibilityFinding f in _findings)
                _grid.Rows.Add(f.Severity.ToString(), f.Target, f.Type, f.ControlDescription, f.Guidance);
            _status.Text = _findings.Count.ToString(CultureInfo.InvariantCulture) + " found(s)" + (truncated ? " (500 limit; additional results was hidden)." : ".") + " Double-click navigates only if the target accepts focus.";
            RaiseCompleted(truncated);
        }

        public void ExportCsv(string filePath)
        {
            EnsureUiThread(); if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Enter with CSV path.", "filePath");
            using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Severity,Target,Type,ControlDescription,Guidance");
                foreach (AccessibilityFinding f in _findings)
                    writer.WriteLine(Csv(f.Severity.ToString()) + "," + Csv(f.Target) + "," + Csv(f.Type) + "," + Csv(f.ControlDescription) + "," + Csv(f.Guidance));
            }
        }

        private void AddColumn(string title, string property, int width)
        { DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn(); c.HeaderText = title; c.DataPropertyName = property; c.Name = property; c.Width = width; c.SortMode = DataGridViewColumnSortMode.NotSortable; _grid.Columns.Add(c); }

        private void Scan(Control node, string path, ref bool truncated)
        {
            if (node == null || node.IsDisposed || truncated) return;
            Type t = node.GetType(); string name = Safe(node.AccessibleName); string text = Safe(node.Text);
            bool interactive = node is ButtonBase || node is LinkLabel || node is TextBoxBase || node is ComboBox || node is ListControl || node is CheckBox || node is RadioButton || node is NumericUpDown || node is TrackBar || node is DateTimePicker;
            string accessible = String.IsNullOrWhiteSpace(name) ? text : name;
            if (interactive && String.IsNullOrWhiteSpace(accessible)) Add(AccessibilityFindingSeverity.Error, "Accessible name wasn't found", path, t.Name, "Define a descriptive AccessibleName (or visible text).", node, ref truncated);
            if ((node is ButtonBase || node is LinkLabel) && String.IsNullOrWhiteSpace(text)) Add(AccessibilityFindingSeverity.Error, "Action text is empty", path, t.Name, "Provide concise visible text; for an icon, also define an AccessibleName.", node, ref truncated);
            Label label = node as Label;
            if (label != null && (String.IsNullOrWhiteSpace(text) || IsGeneric(text)))
                Add(AccessibilityFindingSeverity.Warning, "Label with no useful content", path, t.Name, "Use text that identifies the field/action and confirms the appropriate association; automatic association cannot be inferred in all layouts.", node, ref truncated);
            else if (label != null && !HasNearbyInput(label))
                Add(AccessibilityFindingSeverity.Warning, "Label with no detectable association", path, t.Name, "Review the association via the mnemonic (&) and the TabIndex order; validate the relationship using a screen reader.", node, ref truncated);
            if (node.Visible && interactive && !HasKeyboardPath(node))
                Add(AccessibilityFindingSeverity.Warning, "Without keyboard access", path, t.Name, "Verify TabStop, tab sequence and keyboard acess; Don't change the properties.", node, ref truncated);
            Control[] children = new Control[node.Controls.Count]; node.Controls.CopyTo(children, 0);
            Dictionary<int, int> tabCounts = new Dictionary<int, int>(); int previous = -1;
            foreach (Control child in children) if (child != null && !child.IsDisposed)
                {
                    int count; tabCounts.TryGetValue(child.TabIndex, out count); tabCounts[child.TabIndex] = count + 1;
                    if (child.TabIndex < previous) Add(AccessibilityFindingSeverity.Warning, "Inconsistent Tab Order", path + "/" + ChildName(child), child.GetType().Name, "Review TabIndex and new logic order in this container.", child, ref truncated);
                    previous = child.TabIndex;
                }
            foreach (KeyValuePair<int, int> pair in tabCounts) if (pair.Value > 1)
                    Add(AccessibilityFindingSeverity.Warning, "TabIndex is duplicate", path, t.Name, "Assign a clear tab sequence to the sibling controls.", node, ref truncated);
            foreach (Control child in children)
            {
                if (truncated) break; if (child == null || child.IsDisposed) continue;
                Scan(child, path + "/" + ChildName(child), ref truncated);
            }
        }

        private static bool HasNearbyInput(Label label)
        {
            Control parent = label.Parent; if (parent == null) return false;
            try { foreach (Control c in parent.Controls) if (c != label && !c.IsDisposed && (c is TextBoxBase || c is ComboBox || c is ListControl || c is NumericUpDown || c is DateTimePicker) && c.TabIndex == label.TabIndex + 1) return true; }
            catch (ObjectDisposedException) { }
            return false;
        }
        private static bool HasKeyboardPath(Control c)
        {
            if (!c.TabStop || !c.Enabled || !c.Visible) return false;
            Control p = c.Parent; while (p != null) { if (!p.Enabled || !p.Visible) return false; p = p.Parent; }
            return true;
        }
        private static bool IsGeneric(string text) { return String.IsNullOrWhiteSpace(text) || text.Trim() == "&" || text.Trim().Length < 2; }
        private static string Safe(string value) { return value == null ? String.Empty : value.Trim(); }
        private static string ChildName(Control c) { string id = Safe(c.Name); return String.IsNullOrEmpty(id) ? c.GetType().Name : id; }
        private void Add(AccessibilityFindingSeverity severity, string type, string target, string control, string guidance, Control instance, ref bool truncated)
        {
            if (_findings.Count >= MaximumFindings) { truncated = true; return; }
            _findings.Add(new AccessibilityFinding(severity, type, target, control, guidance, instance));
        }
        private void NavigateSelection(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _findings.Count) return;
            Control target = _findings[e.RowIndex].TargetControl;
            if (target == null || target.IsDisposed || !target.CanSelect || !target.Enabled || !target.Visible) return;
            try { target.Select(); if (target.CanFocus) target.Focus(); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
        private static string Csv(string value)
        {
            if (value == null) value = String.Empty;
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && (trimmed[0] == '=' || trimmed[0] == '+' || trimmed[0] == '-' || trimmed[0] == '@' || trimmed[0] == '\t' || trimmed[0] == '\r')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        private void EnsureUiThread() { if (Thread.CurrentThread.ManagedThreadId != _uiThreadId) throw new InvalidOperationException("This application has been executed on UI thread that create the control."); }
        private void RaiseCompleted(bool truncated) { EventHandler<AccessibilityAuditCompletedEventArgs> h = AuditCompleted; if (h != null) h(this, new AccessibilityAuditCompletedEventArgs(_findings.Count, truncated)); }
    }
}

//watashi wa watashi sore dake