using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Hexadecimal explorer and editor.")]
    public class ZenHexExplorer : UserControl
    {
        private const int MaximumFileSize = 64 * 1024 * 1024;
        private readonly DataGridView _grid;
        private readonly ToolStrip _toolbar;
        private readonly ToolStripButton _openButton;
        private readonly ToolStripButton _saveButton;
        private readonly ToolStripTextBox _offsetBox;
        private readonly ToolStripButton _goButton;
        private readonly ToolStripTextBox _findBox;
        private readonly ToolStripButton _findButton;
        private readonly StatusStrip _status;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolTip _toolTip;
        private readonly Font _hexFont;
        private byte[] _data;
        private string _filePath;
        private bool _dirty;
        private bool _readOnly;
        private bool _suppressSelection;
        private long _selectedOffset;

        public event EventHandler FileOpened;
        public event EventHandler FileChanged;
        public event EventHandler DirtyStateChanged;
        public event EventHandler SelectedOffsetChanged;

        public ZenHexExplorer()
        {
            _data = new byte[0];
            _hexFont = new Font(FontFamily.GenericMonospace, 9.0f);
            _toolbar = new ToolStrip();
            _toolbar.Dock = DockStyle.Top;
            _toolbar.GripStyle = ToolStripGripStyle.Hidden;
            _toolbar.BackColor = ZenTheme.Surface;
            _toolbar.ForeColor = ZenTheme.Foreground;
            _toolbar.RenderMode = ToolStripRenderMode.System;
            _openButton = new ToolStripButton("Open");
            _saveButton = new ToolStripButton("Salve");
            _offsetBox = new ToolStripTextBox();
            _offsetBox.AutoSize = false;
            _offsetBox.Width = 105;
            _goButton = new ToolStripButton("Go");
            _findBox = new ToolStripTextBox();
            _findBox.AutoSize = false;
            _findBox.Width = 150;
            _findButton = new ToolStripButton("Search hex");
            _toolbar.Items.Add(_openButton);
            _toolbar.Items.Add(_saveButton);
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripLabel("Offset:"));
            _toolbar.Items.Add(_offsetBox);
            _toolbar.Items.Add(_goButton);
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripLabel("Hex:"));
            _toolbar.Items.Add(_findBox);
            _toolbar.Items.Add(_findButton);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.VirtualMode = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            _grid.AutoGenerateColumns = false;
            _grid.BackgroundColor = ZenTheme.Background;
            _grid.BorderStyle = BorderStyle.None;
            _grid.GridColor = ZenTheme.Border;
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = ZenTheme.SurfaceRaised;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = ZenTheme.Foreground;
            _grid.ColumnHeadersDefaultCellStyle.Font = ZenTheme.DefaultFont;
            _grid.DefaultCellStyle.BackColor = ZenTheme.Surface;
            _grid.DefaultCellStyle.ForeColor = ZenTheme.Foreground;
            _grid.DefaultCellStyle.SelectionBackColor = ZenTheme.Primary;
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.DefaultCellStyle.Font = _hexFont;
            _grid.RowTemplate.Height = 23;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Offset", HeaderText = "Offset", Width = 100, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
            for (int i = 0; i < 16; i++)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "B" + i.ToString(CultureInfo.InvariantCulture), HeaderText = i.ToString("X2", CultureInfo.InvariantCulture), Width = 35, SortMode = DataGridViewColumnSortMode.NotSortable });
            }
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ascii", HeaderText = "ASCII", Width = 150, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });

            _status = new StatusStrip();
            _status.Dock = DockStyle.Bottom;
            _status.BackColor = ZenTheme.Surface;
            _status.ForeColor = ZenTheme.Muted;
            _statusLabel = new ToolStripStatusLabel("No file open");
            _status.Items.Add(_statusLabel);
            _toolTip = new ToolTip();
            _toolTip.SetToolTip(_grid, "Hexadecimal bytes; press F2 or write a value between 00 and FF to edit.");
            _toolTip.SetToolTip(_toolbar, "Open and save files.");
            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(_toolbar);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(420, 180);

            _openButton.Click += delegate { OpenWithDialog(); };
            _saveButton.Click += delegate { SaveWithDialog(); };
            _goButton.Click += delegate { NavigateFromBox(); };
            _offsetBox.KeyDown += OffsetBoxKeyDown;
            _findButton.Click += delegate { FindFromBox(); };
            _findBox.KeyDown += FindBoxKeyDown;
            _grid.CellValueNeeded += GridCellValueNeeded;
            _grid.CellValuePushed += GridCellValuePushed;
            _grid.CellBeginEdit += GridCellBeginEdit;
            _grid.CurrentCellChanged += GridCurrentCellChanged;
            _grid.KeyDown += GridKeyDown;
            UpdateUiState();
        }

        [Category("Data")]
        [Description("Path of the currently open file; empty when no file is open.")]
        [Browsable(false)]
        public string FilePath { get { return _filePath ?? String.Empty; } }

        [Category("Behavior")]
        [Description("Prevents the editing of bytes in the control.")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _readOnly; }
            set { if (_readOnly != value) { _readOnly = value; UpdateUiState(); } }
        }

        [Category("Data")]
        [Description("Indicates whether there are unsaved changes.")]
        [Browsable(false)]
        public bool IsDirty { get { return _dirty; } }

        [Category("Data")]
        [Description("Absolute Offset byte selected.")]
        [Browsable(false)]
        public long SelectedOffset
        {
            get { return _selectedOffset; }
            set { NavigateToOffset(value); }
        }

        [Category("Data")]
        [Description("Size on bytes.")]
        [Browsable(false)]
        public long FileLength { get { return _data.LongLength; } }

        public void OpenFile(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter with a file path.", "path");
            byte[] loaded;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > MaximumFileSize)
                    throw new InvalidOperationException("The file exceeds the 64MiB limit. Please choose a smaller file");
                loaded = new byte[(int)stream.Length];
                int total = 0;
                while (total < loaded.Length)
                {
                    int count = stream.Read(loaded, total, loaded.Length - total);
                    if (count == 0) throw new EndOfStreamException("The file was truncated during reading.");
                    total += count;
                }
            }
            _data = loaded;
            _filePath = Path.GetFullPath(path);
            _selectedOffset = 0;
            SetDirty(false);
            _suppressSelection = true;
            _grid.RowCount = RowCount;
            _suppressSelection = false;
            if (_grid.RowCount > 0) _grid.CurrentCell = _grid.Rows[0].Cells[1];
            RefreshStatus();
            EventHandler handler = FileOpened;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void SaveFile(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter with a destiny path.", "path");
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("The destiny folder doesn't exists.");
            string temp = Path.Combine(directory, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(_data, 0, _data.Length);
                    stream.Flush(true);
                }
                if (File.Exists(fullPath))
                {
                    try { File.Replace(temp, fullPath, null); }
                    catch (PlatformNotSupportedException) { File.Delete(fullPath); File.Move(temp, fullPath); }
                }
                else File.Move(temp, fullPath);
                _filePath = fullPath;
                SetDirty(false);
                RefreshStatus();
                EventHandler handler = FileChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public long FindHex(string hexSequence)
        {
            byte[] pattern = ParseHex(hexSequence);
            if (pattern.Length == 0 || _data.Length < pattern.Length) return -1;
            int start = _data.Length == 0 ? 0 : (int)Math.Min(_data.Length, Math.Max(0L, _selectedOffset + 1));
            int found = IndexOf(_data, pattern, start);
            if (found < 0 && start > 0) found = IndexOf(_data, pattern, 0);
            if (found >= 0) NavigateToOffset(found);
            return found;
        }

        public void NavigateToOffset(long offset)
        {
            if (_data.Length == 0)
            {
                if (offset != 0) throw new ArgumentOutOfRangeException("offset", "No bytes loaded.");
                _selectedOffset = 0;
                RefreshStatus();
                return;
            }
            if (offset < 0 || offset >= _data.LongLength) throw new ArgumentOutOfRangeException("offset", "The offset must be beetween 0 and the file size minus 1.");
            _selectedOffset = offset;
            int row = (int)(offset / 16);
            int column = 1 + (int)(offset % 16);
            if (row < _grid.RowCount)
            {
                _grid.CurrentCell = _grid.Rows[row].Cells[column];
                _grid.FirstDisplayedScrollingRowIndex = row;
            }
            RefreshStatus();
            EventHandler handler = SelectedOffsetChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private int RowCount { get { return (_data.Length + 15) / 16; } }

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            long offset = (long)e.RowIndex * 16;
            if (e.ColumnIndex == 0) { e.Value = offset.ToString("X8", CultureInfo.InvariantCulture); return; }
            if (e.ColumnIndex == 17)
            {
                StringBuilder text = new StringBuilder(16);
                for (int i = 0; i < 16 && offset + i < _data.LongLength; i++)
                {
                    byte b = _data[(int)(offset + i)];
                    text.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
                e.Value = text.ToString();
                return;
            }
            long index = offset + e.ColumnIndex - 1;
            e.Value = index < _data.LongLength ? _data[(int)index].ToString("X2", CultureInfo.InvariantCulture) : String.Empty;
        }

        private void GridCellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_readOnly || e.ColumnIndex < 1 || e.ColumnIndex > 16) return;
            long index = (long)e.RowIndex * 16 + e.ColumnIndex - 1;
            if (index >= _data.LongLength) return;
            byte value;
            string text = Convert.ToString(e.Value, CultureInfo.InvariantCulture).Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text.Substring(2);
            if (text.Length < 1 || text.Length > 2 || !Byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                MessageBox.Show(this, "Invalid Byte. Enter with a hexadecimal value between 00 and FF.", "Hexadecimal edition", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.CancelEdit();
                return;
            }
            if (_data[(int)index] != value)
            {
                _data[(int)index] = value;
                SetDirty(true);
                _grid.InvalidateRow(e.RowIndex);
                RefreshStatus();
                EventHandler handler = FileChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        private void GridCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (_readOnly || e.ColumnIndex < 1 || e.ColumnIndex > 16 || (long)e.RowIndex * 16 + e.ColumnIndex - 1 >= _data.LongLength) e.Cancel = true;
        }

        private void GridCurrentCellChanged(object sender, EventArgs e)
        {
            if (_suppressSelection || _grid.CurrentCell == null) return;
            DataGridViewCell cell = _grid.CurrentCell;
            if (cell.ColumnIndex >= 1 && cell.ColumnIndex <= 16)
            {
                long offset = (long)cell.RowIndex * 16 + cell.ColumnIndex - 1;
                if (offset < _data.LongLength)
                {
                    _selectedOffset = offset;
                    RefreshStatus();
                    EventHandler handler = SelectedOffsetChanged;
                    if (handler != null) handler(this, EventArgs.Empty);
                }
            }
        }

        private void GridKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Control && e.Control) return;
            if (e.Control && e.KeyCode == Keys.G) { _offsetBox.Focus(); _offsetBox.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.F) { _findBox.Focus(); _findBox.SelectAll(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S) { SaveWithDialog(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.O) { OpenWithDialog(); e.Handled = true; }
        }

        private void OpenWithDialog()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Open binary file";
                if (dialog.ShowDialog(this) == DialogResult.OK) RunUiAction(delegate { OpenFile(dialog.FileName); });
            }
        }

        private void SaveWithDialog()
        {
            if (String.IsNullOrEmpty(_filePath))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Title = "Save binary file";
                    if (dialog.ShowDialog(this) == DialogResult.OK) RunUiAction(delegate { SaveFile(dialog.FileName); });
                }
            }
            else RunUiAction(delegate { SaveFile(_filePath); });
        }

        private void NavigateFromBox()
        {
            string text = _offsetBox.Text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text.Substring(2);
            long value;
            if (!Int64.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                MessageBox.Show(this, "Invalid Offset. Use hexadecimal, example: 1A0.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RunUiAction(delegate { NavigateToOffset(value); });
        }

        private void FindFromBox()
        {
            RunUiAction(delegate
            {
                long result = FindHex(_findBox.Text);
                if (result < 0) MessageBox.Show(this, "Hexa sequence not found.", "Search hex", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private void OffsetBoxKeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { NavigateFromBox(); e.SuppressKeyPress = true; } }
        private void FindBoxKeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { FindFromBox(); e.SuppressKeyPress = true; } }

        private void RunUiAction(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "ZenHexExplorer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static byte[] ParseHex(string input)
        {
            if (String.IsNullOrWhiteSpace(input)) throw new ArgumentException("Enter wit ha hexadecimal sequence, example: DE AD BE EF.", "hexSequence");
            StringBuilder digits = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (Uri.IsHexDigit(c)) digits.Append(c);
                else if (Char.IsWhiteSpace(c) || c == ',' || c == '-' || c == ':') continue;
                else throw new FormatException("Invalid sequence: use only hexadecimal digits separated by a space, comma, hyphan, or colon.");
            }
            if (digits.Length == 0 || (digits.Length & 1) != 0) throw new FormatException("The sequence must contain an even number of hexadecimal digits.");
            byte[] bytes = new byte[digits.Length / 2];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = Byte.Parse(digits.ToString(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return bytes;
        }

        private static int IndexOf(byte[] source, byte[] pattern, int start)
        {
            for (int i = start; i <= source.Length - pattern.Length; i++)
            {
                int j = 0;
                while (j < pattern.Length && source[i + j] == pattern[j]) j++;
                if (j == pattern.Length) return i;
            }
            return -1;
        }

        private void SetDirty(bool value)
        {
            if (_dirty == value) return;
            _dirty = value;
            UpdateUiState();
            EventHandler handler = DirtyStateChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void UpdateUiState()
        {
            if (_saveButton != null) _saveButton.Enabled = !_readOnly && _dirty;
            if (_grid != null) _grid.ReadOnly = _readOnly;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_statusLabel == null) return;
            if (_data == null || _data.Length == 0) _statusLabel.Text = String.IsNullOrEmpty(_filePath) ? "No file open" : Path.GetFileName(_filePath) + "  |  0 bytes";
            else _statusLabel.Text = (String.IsNullOrEmpty(_filePath) ? "File whitout name" : Path.GetFileName(_filePath)) + "  |  " + _data.LongLength.ToString("N0", CultureInfo.InvariantCulture) + " bytes  |  Offset: " + _selectedOffset.ToString("X8", CultureInfo.InvariantCulture) + (_dirty ? "  |  Altered" : String.Empty);
            if (_saveButton != null) _saveButton.Enabled = !_readOnly && _dirty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (_toolTip != null) _toolTip.Dispose(); if (_hexFont != null) _hexFont.Dispose(); }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake