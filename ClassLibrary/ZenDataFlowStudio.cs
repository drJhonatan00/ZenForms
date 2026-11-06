using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Visual pipeline for filtering, sorting, selecting, and calculated columns on a DataTable")]
    public class ZenDataFlowStudio : UserControl
    {
        public enum PipelineStepKind { Filter, Sort, SelectColumns, ComputedColumn }

        public sealed class PipelineStep
        {
            internal PipelineStep(PipelineStepKind kind, string name, string expression)
            { Kind = kind; Name = name; Expression = expression; Enabled = true; }
            public PipelineStepKind Kind { get; private set; }
            public string Name { get; internal set; }
            public string Expression { get; internal set; }
            public bool Enabled { get; internal set; }
            public string Error { get; internal set; }
            public int RowCount { get; internal set; }
            public DataTable Preview { get; internal set; }
            public override string ToString() { return Name + (Enabled ? "" : " (desactivated)") + (String.IsNullOrEmpty(Error) ? "" : " — error: " + Error); }
        }

        public sealed class PipelineStepEventArgs : EventArgs
        {
            public PipelineStepEventArgs(PipelineStep step) { Step = step; }
            public PipelineStep Step { get; private set; }
        }

        private readonly List<PipelineStep> _steps = new List<PipelineStep>();
        private readonly ListView _list;
        private readonly DataGridView _grid;
        private readonly Label _status;
        private readonly ToolTip _tips;
        private DataTable _source;
        private DataTable _result;
        private bool _lastRunSucceeded;
        private string _lastError;
        private bool _updating;

        public ZenDataFlowStudio()
        {
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            _tips = new ToolTip();
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = ZenTheme.Background, Padding = new Padding(10) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = ZenTheme.Background, Padding = new Padding(0, 0, 0, 8) };
            root.Controls.Add(toolbar, 0, 0);
            AddButton(toolbar, "Add filter", "Add stage DataView.RowFilter.", delegate { PromptStep(PipelineStepKind.Filter); });
            AddButton(toolbar, "Sort", "Add expression DataView.Sort.", delegate { PromptStep(PipelineStepKind.Sort); });
            AddButton(toolbar, "Select columns", "Add a list separated by comma.", delegate { PromptStep(PipelineStepKind.SelectColumns); });
            AddButton(toolbar, "Calculated column", "Add one column with expression DataColumn.Expression.", delegate { PromptComputed(); });
            AddButton(toolbar, "↑", "Move to top.", delegate { MoveSelected(-1); });
            AddButton(toolbar, "↓", "Move to bottom.", delegate { MoveSelected(1); });
            AddButton(toolbar, "Activate/desactivate", "Change the selected stage.", delegate { ToggleSelected(); });
            AddButton(toolbar, "Remove", "Remove the selected stage.", delegate { RemoveSelected(); });
            AddButton(toolbar, "Apply", "Play the pipeline without modify the source table", delegate { Apply(); });
            AddButton(toolbar, "Export CSV", "Export how CSV file.", delegate { ExportCsv(); });

            var split = new SplitContainer { Dock = DockStyle.Fill, BackColor = ZenTheme.Background, SplitterDistance = 260, FixedPanel = FixedPanel.Panel1 };
            root.Controls.Add(split, 0, 1);
            _list = new ListView { Dock = DockStyle.Fill, View = View.List, FullRowSelect = true, HideSelection = false, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, BorderStyle = BorderStyle.FixedSingle, MultiSelect = false, TabIndex = 0 };
            split.Panel1.Controls.Add(_list);
            _grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, BackgroundColor = ZenTheme.Surface, GridColor = ZenTheme.Border, BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false, ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground }, DefaultCellStyle = new DataGridViewCellStyle { BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, SelectionBackColor = ZenTheme.Primary, SelectionForeColor = ZenTheme.Foreground }, TabIndex = 1 };
            split.Panel2.Controls.Add(_grid);
            _status = new Label { Dock = DockStyle.Fill, AutoSize = true, ForeColor = ZenTheme.Muted, Padding = new Padding(0, 8, 0, 0), Text = "Vincule um DataTable para começar." };
            root.Controls.Add(_status, 0, 2);
            _list.SelectedIndexChanged += delegate { ShowSelectedPreview(); };
            _list.KeyDown += ListKeyDown;
            _tips.SetToolTip(_list, "Pipeline stages. Use Ctrl+Arrows to sort and Space to active/desactive.");
            _tips.SetToolTip(_grid, "Preview of up to 200 lines of stage selected or of the results.");
            _tips.SetToolTip(this, "Local data pipeline; the source table never is changed.");
            AccessibleName = "ZenDataFlowStudio";
            AccessibleDescription = "Pipeline visual editor of DataTable.";
            RefreshSteps();
        }

        [Category("Data"), Description("Source DataTable.")]
        public DataTable Source
        {
            get { return _source; }
            set { _source = value; _result = null; _lastRunSucceeded = false; _lastError = null; foreach (PipelineStep s in _steps) { s.Error = null; s.Preview = null; s.RowCount = 0; } RefreshSteps(); SourceChanged(this, EventArgs.Empty); }
        }
        [Browsable(false)] public DataTable Result { get { return _result; } }
        [Browsable(false)] public bool LastRunSucceeded { get { return _lastRunSucceeded; } }
        [Browsable(false)] public string LastError { get { return _lastError; } }
        [Browsable(false)] public IList<PipelineStep> Steps { get { return _steps.AsReadOnly(); } }
        public event EventHandler SourceChanged = delegate { };
        public event EventHandler ResultChanged = delegate { };
        public event EventHandler<PipelineStepEventArgs> StepChanged = delegate { };
        public event EventHandler<PipelineStepEventArgs> StepError = delegate { };
        public event EventHandler<PipelineStepEventArgs> StepCompleted = delegate { };
        public event EventHandler PipelineApplied = delegate { };
        public event EventHandler PipelineFailed = delegate { };

        public PipelineStep AddFilter(string rowFilter) { return Add(PipelineStepKind.Filter, "Filter", rowFilter); }
        public PipelineStep AddSort(string sortExpression) { return Add(PipelineStepKind.Sort, "Sort", sortExpression); }
        public PipelineStep SelectColumns(params string[] columns) { return Add(PipelineStepKind.SelectColumns, "Select columns", String.Join(",", columns ?? new string[0])); }
        public PipelineStep AddComputedColumn(string columnName, string expression, Type dataType)
        {
            if (String.IsNullOrWhiteSpace(columnName)) throw new ArgumentException("The column name is obrigatory.", "columnName");
            if (dataType == null) throw new ArgumentNullException("dataType");
            return Add(PipelineStepKind.ComputedColumn, "Computed: " + columnName, columnName + "|" + dataType.AssemblyQualifiedName + "|" + (expression ?? String.Empty));
        }
        public void MoveStep(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _steps.Count || toIndex < 0 || toIndex >= _steps.Count) throw new ArgumentOutOfRangeException();
            if (fromIndex == toIndex) return;
            PipelineStep step = _steps[fromIndex]; _steps.RemoveAt(fromIndex); _steps.Insert(toIndex, step); RefreshSteps(); StepChanged(this, new PipelineStepEventArgs(step));
        }
        public void SetStepEnabled(int index, bool enabled)
        {
            PipelineStep step = _steps[index]; step.Enabled = enabled; step.Error = null; RefreshSteps(); StepChanged(this, new PipelineStepEventArgs(step));
        }
        public DataTable Apply()
        {
            _lastRunSucceeded = false; _lastError = null;
            if (_source == null)
            {
                _lastError = "Link a source table first."; _result = null; ShowResult(); RefreshSteps(); _status.Text = _lastError;
                ResultChanged(this, EventArgs.Empty); PipelineFailed(this, EventArgs.Empty); return null;
            }
            foreach (PipelineStep step in _steps) { step.Error = null; step.Preview = null; step.RowCount = 0; }
            DataTable working = _source.Copy();
            foreach (PipelineStep step in _steps)
            {
                if (step.Enabled)
                {
                    try { working = ExecuteStep(working, step); step.RowCount = working.Rows.Count; step.Preview = working.Copy(); StepCompleted(this, new PipelineStepEventArgs(step)); }
                    catch (Exception ex)
                    {
                        step.Error = ex.Message; step.RowCount = 0; step.Preview = null; _lastError = "Stage '" + step.Name + "' failed: " + ex.Message;
                        StepError(this, new PipelineStepEventArgs(step)); _result = null; RefreshSteps(); ShowResult(); _status.Text = "Pipeline interrupted." + _lastError;
                        ResultChanged(this, EventArgs.Empty); PipelineFailed(this, EventArgs.Empty); return null;
                    }
                }
                else { step.RowCount = working.Rows.Count; step.Preview = working.Copy(); }
            }
            _result = working.Copy(); _lastRunSucceeded = true; RefreshSteps(); ShowResult(); ResultChanged(this, EventArgs.Empty); PipelineApplied(this, EventArgs.Empty); return _result;
        }

        private PipelineStep Add(PipelineStepKind kind, string name, string expression)
        {
            var step = new PipelineStep(kind, name, expression ?? String.Empty); _steps.Add(step); RefreshSteps(); _list.Items[_list.Items.Count - 1].Selected = true; StepChanged(this, new PipelineStepEventArgs(step)); return step;
        }
        private DataTable ExecuteStep(DataTable input, PipelineStep step)
        {
            if (step.Kind == PipelineStepKind.Filter || step.Kind == PipelineStepKind.Sort)
            {
                using (DataView view = new DataView(input))
                {
                    if (step.Kind == PipelineStepKind.Filter) view.RowFilter = step.Expression; else view.Sort = step.Expression;
                    return view.ToTable();
                }
            }
            if (step.Kind == PipelineStepKind.SelectColumns)
            {
                string[] columns = step.Expression.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                if (columns.Length == 0) throw new InvalidOperationException("Enter with one column.");
                foreach (string col in columns) if (!input.Columns.Contains(col)) throw new InvalidOperationException("This column doesn't exists: " + col);
                return input.DefaultView.ToTable(false, columns);
            }
            string[] parts = step.Expression.Split(new char[] { '|' }, 3);
            if (parts.Length != 3) throw new InvalidOperationException("Calculated colum definition is invalid.");
            Type type = Type.GetType(parts[1], false); if (type == null) throw new InvalidOperationException("Column type inlegible.");
            if (input.Columns.Contains(parts[0])) throw new InvalidOperationException("Already exists a column with this name: " + parts[0] + ".");
            DataTable computed = input.Copy();
            computed.Columns.Add(new DataColumn(parts[0], type) { Expression = parts[2] });
            return computed;
        }
        private void RefreshSteps()
        {
            if (_list == null) return; _updating = true; _list.BeginUpdate(); _list.Items.Clear();
            foreach (PipelineStep s in _steps)
            {
                string desc = s.ToString() + " — " + s.Expression;
                if (s.RowCount > 0 || s.Preview != null) desc += " [" + s.RowCount.ToString(CultureInfo.InvariantCulture) + " rows]";
                var item = new ListViewItem(desc) { Tag = s, ForeColor = String.IsNullOrEmpty(s.Error) ? (s.Enabled ? ZenTheme.Foreground : ZenTheme.Muted) : ZenTheme.Danger };
                _list.Items.Add(item);
            }
            _list.EndUpdate(); _updating = false;
            _status.Text = _source == null ? "Link a DataTable to start." : "Source: " + _source.TableName + " · " + _source.Rows.Count + " lines · " + _steps.Count + " stages" + (_result == null ? "" : " · Results: " + _result.Rows.Count + " lines");
        }
        private void ShowSelectedPreview()
        {
            if (_updating || _grid == null) return;
            PipelineStep step = _list.SelectedItems.Count == 0 ? null : _list.SelectedItems[0].Tag as PipelineStep;
            if (step != null && step.Preview != null) BindPreview(step.Preview); else ShowResult();
        }
        private void ShowResult() { if (_grid != null) BindPreview(_result); }
        private void BindPreview(DataTable table)
        {
            if (table == null) { _grid.DataSource = null; return; }
            DataTable preview = table.Clone(); int count = Math.Min(200, table.Rows.Count);
            for (int i = 0; i < count; i++) preview.ImportRow(table.Rows[i]);
            _grid.DataSource = preview;
        }
        private void PromptStep(PipelineStepKind kind)
        {
            string title = kind == PipelineStepKind.Filter ? "DataView.RowFilter" : kind == PipelineStepKind.Sort ? "DataView.Sort" : "Columns separated by comma";
            string value = Prompt(title, "Expressão:", ""); if (value != null) { if (kind == PipelineStepKind.Filter) AddFilter(value); else if (kind == PipelineStepKind.Sort) AddSort(value); else SelectColumns(value.Split(',')); }
        }
        private void PromptComputed()
        {
            string name = Prompt("Calculated column", "Column name:", "Calculated"); if (name == null) return;
            string expr = Prompt("Calculated column", "Expression DataColumn.Expression:", ""); if (expr == null) return;
            string typeText = Prompt("Calculated column", "CLR Type (ex.: System.String, System.Decimal, System.Int32):", "System.String"); if (typeText == null) return;
            Type type = Type.GetType(typeText, false); if (type == null) { MessageBox.Show(this, "CLR type invalid.", "Calculated column", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try { AddComputedColumn(name, expr, type); } catch (Exception ex) { ShowError(ex.Message); }
        }
        private static string Prompt(string title, string label, string initial)
        {
            using (Form f = new Form())
            using (TextBox t = new TextBox())
            using (Button ok = new Button())
            using (Button cancel = new Button())
            using (Label l = new Label())
            {
                f.Text = title; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.StartPosition = FormStartPosition.CenterParent; f.MinimizeBox = false; f.MaximizeBox = false; f.ClientSize = new Size(430, 115); f.BackColor = ZenTheme.Surface; f.ForeColor = ZenTheme.Foreground;
                l.Text = label; l.SetBounds(12, 12, 400, 20); t.Text = initial; t.SetBounds(12, 36, 405, 24); ok.Text = "OK"; ok.DialogResult = DialogResult.OK; ok.SetBounds(245, 75, 80, 28); cancel.Text = "Cancel"; cancel.DialogResult = DialogResult.Cancel; cancel.SetBounds(335, 75, 82, 28);
                f.Controls.AddRange(new Control[] { l, t, ok, cancel }); f.AcceptButton = ok; f.CancelButton = cancel; return f.ShowDialog() == DialogResult.OK ? t.Text : null;
            }
        }
        private void AddButton(Control parent, string text, string tip, EventHandler handler)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, Margin = new Padding(0, 0, 6, 4) };
            b.FlatAppearance.BorderColor = ZenTheme.Border; b.Click += handler; parent.Controls.Add(b); _tips.SetToolTip(b, tip); b.AccessibleName = text; b.AccessibleDescription = tip;
        }
        private void MoveSelected(int delta) { if (_list.SelectedIndices.Count == 0) return; int i = _list.SelectedIndices[0], to = i + delta; if (to >= 0 && to < _steps.Count) { MoveStep(i, to); _list.Items[to].Selected = true; } }
        private void ToggleSelected() { if (_list.SelectedIndices.Count > 0) { int i = _list.SelectedIndices[0]; SetStepEnabled(i, !_steps[i].Enabled); _list.Items[i].Selected = true; } }
        private void RemoveSelected() { if (_list.SelectedIndices.Count == 0) return; int i = _list.SelectedIndices[0]; PipelineStep step = _steps[i]; _steps.RemoveAt(i); _result = null; RefreshSteps(); StepChanged(this, new PipelineStepEventArgs(step)); }
        private void ListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Up) { MoveSelected(-1); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Down) { MoveSelected(1); e.Handled = true; }
            else if (e.KeyCode == Keys.Space) { ToggleSelected(); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { RemoveSelected(); e.Handled = true; }
        }
        private void ExportCsv()
        {
            if (_result == null) { ShowError("Apply the pipeline before exports."); return; }
            using (var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", DefaultExt = "csv", AddExtension = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try { using (var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true))) { writer.WriteLine(String.Join(",", _result.Columns.Cast<DataColumn>().Select(c => Csv(c.ColumnName)))); foreach (DataRow row in _result.Rows) writer.WriteLine(String.Join(",", _result.Columns.Cast<DataColumn>().Select(c => Csv(row[c] == DBNull.Value ? "" : Convert.ToString(row[c], CultureInfo.InvariantCulture))))); } }
                catch (Exception ex) { ShowError("CSV exportation failed: " + ex.Message); }
            }
        }
        private static string Csv(string text) { if (text == null) text = ""; return "\"" + text.Replace("\"", "\"\"") + "\""; }
        private void ShowError(string message) { MessageBox.Show(this, message, "ZenDataFlowStudio", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (_tips != null) _tips.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake