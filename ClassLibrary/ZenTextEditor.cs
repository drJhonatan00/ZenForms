using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(RichTextBox))]
    [Category("ZenForms")]
    [Description("Rich ZenForms text editor.")]
    public class ZenTextEditor : UserControl
    {
        private readonly ToolStrip _ribbon;
        private readonly Panel _accentLine;
        private readonly RichTextBox _editor;
        private readonly List<Font> _ownedFonts = new List<Font>();
        private readonly ToolStripStatusLabel _documentLabel;
        private readonly StatusStrip _status;
        private readonly ToolStripStatusLabel _counts;
        private readonly ToolStripStatusLabel _zoomLabel;
        private readonly ToolStripComboBox _fontBox;
        private readonly ToolStripComboBox _sizeBox;
        private readonly ToolStripButton _boldButton;
        private readonly ToolStripButton _italicButton;
        private readonly ToolStripButton _underlineButton;
        private readonly ToolStripButton _bulletButton;
        private readonly ToolStripButton _leftButton;
        private readonly ToolStripButton _centerButton;
        private readonly ToolStripButton _rightButton;
        private string _documentName = "Document1";
        private string _documentPath = String.Empty;
        private bool _dirty;
        private bool _suppressChange;
        private bool _updatingRibbon;
        private bool _showStatusBar = true;
        private bool _isReadOnly;
        private Color _accentColor = ZenTheme.Primary;
        private float _zoomFactor = 1.0f;
        private string _lastFind = String.Empty;
        private int _printPosition;
        private PrintDocument _printDocument;

        public event EventHandler DocumentChanged;
        public event EventHandler DocumentSaved;
        public event EventHandler DocumentOpened;

        public ZenTextEditor()
        {
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(320, 240);
            _accentLine = new Panel();
            _accentLine.Dock = DockStyle.Top;
            _accentLine.Height = 3;
            _accentLine.BackColor = _accentColor;

            _ribbon = new ToolStrip();
            _ribbon.Dock = DockStyle.Top;
            _ribbon.GripStyle = ToolStripGripStyle.Hidden;
            _ribbon.Stretch = true;
            _ribbon.Renderer = new ZenMenuRenderer();
            _ribbon.BackColor = ZenTheme.Surface;
            _ribbon.ForeColor = ZenTheme.Foreground;
            _ribbon.Padding = new Padding(5, 5, 5, 5);
            _ribbon.LayoutStyle = ToolStripLayoutStyle.Flow;

            _editor = new RichTextBox();
            _editor.Dock = DockStyle.Fill;
            _editor.BorderStyle = BorderStyle.FixedSingle;
            _editor.BackColor = ZenTheme.Background;
            _editor.ForeColor = ZenTheme.Foreground;
            _editor.Font = OwnFont(new Font("Segoe UI", 11.0f));
            _editor.AcceptsTab = true;
            _editor.DetectUrls = true;
            _editor.HideSelection = false;
            _editor.WordWrap = true;
            _editor.ScrollBars = RichTextBoxScrollBars.Vertical;

            _status = new StatusStrip();
            _status.Dock = DockStyle.Bottom;
            _status.Renderer = new ZenMenuRenderer();
            _status.BackColor = ZenTheme.Surface;
            _status.ForeColor = ZenTheme.Muted;
            _documentLabel = new ToolStripStatusLabel(_documentName);
            _documentLabel.ForeColor = _accentColor;
            _counts = new ToolStripStatusLabel();
            _zoomLabel = new ToolStripStatusLabel();
            ToolStripButton zoomOut = MakeButton("−", "Zoom out", delegate { ZoomFactor = Math.Max(0.5f, ZoomFactor - 0.1f); });
            ToolStripButton zoomIn = MakeButton("+", "Zoom In", delegate { ZoomFactor = Math.Min(3.0f, ZoomFactor + 0.1f); });
            _status.Items.Add(_documentLabel);
            _status.Items.Add(new ToolStripStatusLabel(" | ") { ForeColor = ZenTheme.Border });
            _status.Items.Add(_counts);
            _status.Items.Add(new ToolStripStatusLabel() { Spring = true });
            _status.Items.Add(zoomOut);
            _status.Items.Add(_zoomLabel);
            _status.Items.Add(zoomIn);

            _fontBox = new ToolStripComboBox();
            _fontBox.AutoSize = false;
            _fontBox.Width = 145;
            _fontBox.DropDownStyle = ComboBoxStyle.DropDown;
            _fontBox.BackColor = ZenTheme.SurfaceRaised;
            _fontBox.ForeColor = ZenTheme.Foreground;
            foreach (FontFamily family in FontFamily.Families)
            {
                try { _fontBox.Items.Add(family.Name); }
                finally { family.Dispose(); }
            }
            _fontBox.SelectedIndexChanged += delegate { ApplyFontFamily(); };
            _fontBox.KeyDown += delegate (object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) ApplyFontFamily(); };
            _sizeBox = new ToolStripComboBox();
            _sizeBox.AutoSize = false;
            _sizeBox.Width = 55;
            _sizeBox.DropDownStyle = ComboBoxStyle.DropDown;
            _sizeBox.BackColor = ZenTheme.SurfaceRaised;
            _sizeBox.ForeColor = ZenTheme.Foreground;
            _sizeBox.Items.AddRange(new object[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "36", "48", "72" });
            _sizeBox.Text = "11";
            _sizeBox.SelectedIndexChanged += delegate { ApplyFontSize(); };
            _sizeBox.KeyDown += delegate (object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) ApplyFontSize(); };
            _boldButton = MakeButton("B", "Bold", ToggleBold);
            _boldButton.CheckOnClick = true;
            _boldButton.Font = OwnFont(new Font("Segoe UI", 9, FontStyle.Bold));
            _italicButton = MakeButton("I", "Italic", ToggleItalic);
            _italicButton.CheckOnClick = true;
            _italicButton.Font = OwnFont(new Font("Segoe UI", 9, FontStyle.Italic));
            _underlineButton = MakeButton("U", "Underlined", ToggleUnderline);
            _underlineButton.CheckOnClick = true;
            _underlineButton.Font = OwnFont(new Font("Segoe UI", 9, FontStyle.Underline));
            _bulletButton = MakeButton("• List", "Toggle bulleted list", ToggleBullets);
            _leftButton = MakeButton("≡ L", "Align to the Left", delegate { SetAlignment(HorizontalAlignment.Left); });
            _centerButton = MakeButton("≡ C", "Centralize", delegate { SetAlignment(HorizontalAlignment.Center); });
            _rightButton = MakeButton("≡ R", "Align to the Right", delegate { SetAlignment(HorizontalAlignment.Right); });

            AddButton("New", "New document", delegate { RunUiAction(NewDocument); });
            AddButton("Open", "Open document", delegate { RunUiAction(OpenDocument); });
            AddButton("Save", "Save document", delegate { RunUiAction(SaveDocument); });
            AddButton("Save as", "Save a new document", delegate { RunUiAction(SaveAsDialog); });
            AddButton("Print", "Print document", delegate { RunUiAction(PrintDocumentDialog); });
            AddSeparator();
            AddButton("Undo", "Undo (Ctrl+Z)", delegate { Undo(); });
            AddButton("Redo", "Redo (Ctrl+Y)", delegate { Redo(); });
            AddButton("Cut", "Cut", delegate { _editor.Cut(); });
            AddButton("Copy", "Copy", delegate { _editor.Copy(); });
            AddButton("Paste", "Paste", delegate { _editor.Paste(); });
            AddButton("Localize", "Localize the next (Ctrl+F)", delegate { FindDialog(); });
            AddButton("Replace", "Replace all occurrences", delegate { ReplaceDialog(); });
            AddSeparator();
            _ribbon.Items.Add(_fontBox);
            _ribbon.Items.Add(_sizeBox);
            _ribbon.Items.Add(_boldButton);
            _ribbon.Items.Add(_italicButton);
            _ribbon.Items.Add(_underlineButton);
            AddButton("Color", "Text color", delegate { ChooseTextColor(); });
            AddButton("Highlight", "Highlight for background", delegate { ChooseHighlightColor(); });
            _ribbon.Items.Add(_leftButton);
            _ribbon.Items.Add(_centerButton);
            _ribbon.Items.Add(_rightButton);
            _ribbon.Items.Add(_bulletButton);
            AddButton("Date/Time", "Insert date/time", InsertDateTime);

            Controls.Add(_editor);
            Controls.Add(_status);
            Controls.Add(_ribbon);
            Controls.Add(_accentLine);
            _editor.TextChanged += EditorTextChanged;
            _editor.SelectionChanged += EditorSelectionChanged;
            UpdateStatus();
            UpdateZoom();
        }

        [Category("Document"), Description("Name for document.")]
        public string DocumentName { get { return _documentName; } set { _documentName = String.IsNullOrEmpty(value) ? "Sem título" : value; if (_documentLabel != null) _documentLabel.Text = _documentName; } }
        [Category("Document"), Description("Document path.")]
        public string DocumentPath { get { return _documentPath; } }
        [Browsable(false)] public bool IsDirty { get { return _dirty; } }
        [Browsable(false)] public int WordCount { get { return CountWords(_editor.Text); } }
        [Browsable(false)] public int CharacterCount { get { return _editor.TextLength; } }
        [Category("Appearance"), Description("Text zoom, between 0,5 and 3,0.")]
        public float ZoomFactor { get { return _zoomFactor; } set { _zoomFactor = Math.Max(0.5f, Math.Min(3.0f, value)); UpdateZoom(); } }
        [Category("Appearance"), Description("Interface color.")]
        public Color AccentColor { get { return _accentColor; } set { _accentColor = value; if (_accentLine != null) _accentLine.BackColor = value; if (_documentLabel != null) _documentLabel.ForeColor = value; Invalidate(); } }
        [Category("Behavior"), Description("Prevents editing of the document.")]
        public bool IsReadOnly { get { return _isReadOnly; } set { _isReadOnly = value; _editor.ReadOnly = value; } }
        [Category("Appearance"), Description("Show the footer.")]
        public bool ShowStatusBar { get { return _showStatusBar; } set { _showStatusBar = value; _status.Visible = value; } }
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RichTextBox InnerEditor { get { return _editor; } }

        public void NewDocument()
        {
            if (!ConfirmDiscard()) return;
            _suppressChange = true;
            _editor.Clear();
            _suppressChange = false;
            _documentName = "Document1";
            _documentPath = String.Empty;
            _documentLabel.Text = _documentName;
            SetDirty(false);
            UpdateStatus();
            RaiseDocumentChanged();
        }

        public void OpenDocument()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Open document";
                dialog.Filter = "Compatible documents|*.rtf;*.txt;*.docx|RTF Formated text|*.rtf|Simple text|*.txt|Basic DOCX file|*.docx|All Files|*.*";
                if (dialog.ShowDialog(this) == DialogResult.OK) OpenDocument(dialog.FileName);
            }
        }

        public void OpenDocument(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter with document path.", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("Document not found.", path);
            if (!ConfirmDiscard()) return;
            string extension = Path.GetExtension(path).ToLowerInvariant();
            _suppressChange = true;
            try
            {
                if (extension == ".rtf") _editor.LoadFile(path, RichTextBoxStreamType.RichText);
                else if (extension == ".txt") _editor.Text = File.ReadAllText(path, new UTF8Encoding(false));
                else if (extension == ".docx") LoadDocx(path);
                else throw new NotSupportedException("Unsupported file. Use RTF, TXT or DOCX.");
                _documentPath = Path.GetFullPath(path);
                _documentName = Path.GetFileName(path);
                _documentLabel.Text = _documentName;
                SetDirty(false);
            }
            finally { _suppressChange = false; }
            UpdateStatus();
            RaiseDocumentChanged();
            EventHandler handler = DocumentOpened;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void SaveDocument()
        {
            if (String.IsNullOrEmpty(_documentPath)) { SaveAsDialog(); return; }
            SaveDocument(_documentPath);
        }

        public void SaveDocument(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter with file path.", "path");
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".rtf" && extension != ".txt" && extension != ".docx")
                throw new NotSupportedException("Unsupported file. Use .rtf, .txt or .docx.");
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("The folder doesn't exists.");
            if (extension == ".rtf") _editor.SaveFile(fullPath, RichTextBoxStreamType.RichText);
            else if (extension == ".txt") File.WriteAllText(fullPath, _editor.Text, new UTF8Encoding(false));
            else SaveDocx(fullPath);
            _documentPath = fullPath;
            _documentName = Path.GetFileName(fullPath);
            _documentLabel.Text = _documentName;
            SetDirty(false);
            EventHandler handler = DocumentSaved;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void Undo() { if (_editor.CanUndo) _editor.Undo(); }
        public void Redo() { if (_editor.CanRedo) _editor.Redo(); }
        public bool FindNext(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return false;
            _lastFind = text;
            _editor.Select(0, 0);
            return FindNext();
        }
        public bool FindNext()
        {
            if (String.IsNullOrEmpty(_lastFind)) return false;
            if (_editor.TextLength == 0) return false;
            int start = _editor.SelectionStart + _editor.SelectionLength;
            if (start >= _editor.TextLength) start = 0;
            int found = _editor.Find(_lastFind, start, RichTextBoxFinds.None);
            if (found < 0 && start > 0) found = _editor.Find(_lastFind, 0, RichTextBoxFinds.None);
            return found >= 0;
        }
        public int ReplaceAll(string find, string replacement)
        {
            if (String.IsNullOrEmpty(find)) throw new ArgumentException("The text to find cannot be empty.", "find");
            if (replacement == null) replacement = String.Empty;
            int count = 0;
            int start = 0;
            _editor.SuspendLayout();
            try
            {
                while (start < _editor.TextLength)
                {
                    int index = _editor.Find(find, start, RichTextBoxFinds.None);
                    if (index < 0) break;
                    _editor.Select(index, find.Length);
                    _editor.SelectedText = replacement;
                    count++;
                    start = index + replacement.Length;
                }
            }
            finally { _editor.ResumeLayout(); }
            _editor.Focus();
            return count;
        }
        public void InsertDateTime() { _editor.SelectedText = DateTime.Now.ToString("g", CultureInfo.CurrentCulture); }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) == Keys.Control)
            {
                Keys key = keyData & Keys.KeyCode;
                if (key == Keys.N) { RunUiAction(NewDocument); return true; }
                if (key == Keys.O) { RunUiAction(OpenDocument); return true; }
                if (key == Keys.S) { RunUiAction(SaveDocument); return true; }
                if (key == Keys.F) { FindDialog(); return true; }
                if (key == Keys.Z) { Undo(); return true; }
                if (key == Keys.Y) { Redo(); return true; }
                if (key == Keys.B) { ToggleBold(); return true; }
                if (key == Keys.I) { ToggleItalic(); return true; }
                if (key == Keys.U) { ToggleUnderline(); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _printDocument != null)
            {
                _printDocument.PrintPage -= PrintPage;
                _printDocument.Dispose();
                _printDocument = null;
            }
            if (disposing)
            {
                foreach (Font ownedFont in _ownedFonts) ownedFont.Dispose();
                _ownedFonts.Clear();
            }
            base.Dispose(disposing);
        }

        private Font OwnFont(Font font)
        {
            _ownedFonts.Add(font);
            return font;
        }

        private ToolStripButton MakeButton(string text, string tip, Action action)
        {
            ToolStripButton button = new ToolStripButton(text);
            button.ToolTipText = tip;
            button.ForeColor = ZenTheme.Foreground;
            button.BackColor = ZenTheme.Surface;
            button.DisplayStyle = ToolStripItemDisplayStyle.Text;
            button.Click += delegate { action(); };
            return button;
        }
        private void AddButton(string text, string tip, Action action) { _ribbon.Items.Add(MakeButton(text, tip, action)); }
        private void AddSeparator() { _ribbon.Items.Add(new ToolStripSeparator()); }
        private void RunUiAction(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "ZenTextEditor", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private bool ConfirmDiscard()
        {
            if (!_dirty) return true;
            DialogResult result = MessageBox.Show(this, "There are unsaved changes. Discard changes?", "Document changed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return result == DialogResult.Yes;
        }
        private void SaveAsDialog()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save document";
                dialog.Filter = "Formated text RTF|*.rtf|Simple text|*.txt|DOCX file|*.docx";
                dialog.DefaultExt = "rtf";
                if (dialog.ShowDialog(this) == DialogResult.OK) SaveDocument(dialog.FileName);
            }
        }
        private void FindDialog()
        {
            string value = Prompt("Localize the next occurrence:", "Localize", _lastFind);
            if (value == null) return;
            _lastFind = value;
            if (!FindNext()) MessageBox.Show(this, "No occurrences found.", "Localize", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ReplaceDialog()
        {
            string find = Prompt("Text to locate:", "Replace", "");
            if (find == null) return;
            string replacement = Prompt("Replace by:", "Replace", String.Empty);
            if (replacement == null) return;
            int count = ReplaceAll(find, replacement);
            MessageBox.Show(this, count.ToString(CultureInfo.CurrentCulture) + " occurrence(s) replaced.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private string Prompt(string label, string title, string initial)
        {
            Form form = new Form();
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ClientSize = new Size(370, 112);
            form.BackColor = ZenTheme.Surface;
            Label caption = new Label(); caption.Text = label; caption.ForeColor = ZenTheme.Foreground; caption.Location = new Point(12, 12); caption.AutoSize = true;
            TextBox input = new TextBox(); input.Text = initial ?? String.Empty; input.Location = new Point(12, 38); input.Width = 345;
            Button ok = new Button(); ok.Text = "OK"; ok.DialogResult = DialogResult.OK; ok.Location = new Point(196, 74);
            Button cancel = new Button(); cancel.Text = "Cancel"; cancel.DialogResult = DialogResult.Cancel; cancel.Location = new Point(278, 74);
            form.Controls.AddRange(new Control[] { caption, input, ok, cancel }); form.AcceptButton = ok; form.CancelButton = cancel;
            DialogResult result = form.ShowDialog(this);
            string answer = result == DialogResult.OK ? input.Text : null; form.Dispose(); return answer;
        }
        private void ChooseTextColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = _editor.SelectionColor.IsEmpty ? ZenTheme.Foreground : _editor.SelectionColor;
                if (dialog.ShowDialog(this) == DialogResult.OK) { _editor.SelectionColor = dialog.Color; MarkFormattingChanged(); }
            }
        }
        private void ChooseHighlightColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = _editor.SelectionBackColor.IsEmpty ? ZenTheme.Primary : _editor.SelectionBackColor;
                if (dialog.ShowDialog(this) == DialogResult.OK) { _editor.SelectionBackColor = dialog.Color; MarkFormattingChanged(); }
            }
        }
        private void ApplyFontFamily()
        {
            if (_updatingRibbon) return;
            string name = _fontBox.Text;
            if (String.IsNullOrWhiteSpace(name)) return;
            try { _editor.SelectionFont = OwnFont(new Font(name, _editor.SelectionFont == null ? _editor.Font.Size : _editor.SelectionFont.Size, _editor.SelectionFont == null ? FontStyle.Regular : _editor.SelectionFont.Style)); MarkFormattingChanged(); }
            catch { }
            _editor.Focus();
        }
        private void ApplyFontSize()
        {
            if (_updatingRibbon) return;
            float size;
            if (!Single.TryParse(_sizeBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out size) || size < 1 || size > 200) return;
            Font current = _editor.SelectionFont ?? _editor.Font;
            _editor.SelectionFont = OwnFont(new Font(current.FontFamily, size, current.Style));
            MarkFormattingChanged();
            _editor.Focus();
        }
        private void ToggleBold() { ToggleStyle(FontStyle.Bold, _boldButton); }
        private void ToggleItalic() { ToggleStyle(FontStyle.Italic, _italicButton); }
        private void ToggleUnderline() { ToggleStyle(FontStyle.Underline, _underlineButton); }
        private void ToggleStyle(FontStyle style, ToolStripButton button)
        {
            Font current = _editor.SelectionFont ?? _editor.Font;
            FontStyle next = current.Style ^ style;
            _editor.SelectionFont = OwnFont(new Font(current, next));
            button.Checked = (next & style) != 0;
            MarkFormattingChanged();
            _editor.Focus();
        }
        private void ToggleBullets() { _editor.SelectionBullet = !_editor.SelectionBullet; MarkFormattingChanged(); _editor.Focus(); }
        private void SetAlignment(HorizontalAlignment alignment) { _editor.SelectionAlignment = alignment; MarkFormattingChanged(); _editor.Focus(); }
        private void PrintDocumentDialog()
        {
            PrintDocument document = new PrintDocument();
            _printDocument = document;
            document.DocumentName = _documentName;
            document.PrintPage += PrintPage;
            try
            {
                using (PrintDialog dialog = new PrintDialog())
                {
                    dialog.Document = document;
                    dialog.AllowSomePages = false;
                    if (dialog.ShowDialog(this) == DialogResult.OK) { _printPosition = 0; document.Print(); }
                }
            }
            finally
            {
                document.PrintPage -= PrintPage;
                if (Object.ReferenceEquals(_printDocument, document)) _printDocument = null;
                document.Dispose();
            }
        }
        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            string text = _editor.Text;
            using (Font printFont = new Font(_editor.Font.FontFamily, _editor.Font.Size))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                int start = _printPosition;
                int end = start;
                float y = e.MarginBounds.Top;
                while (end < text.Length)
                {
                    int lineEnd = text.IndexOf('\n', end);
                    if (lineEnd < 0) lineEnd = text.Length;
                    string line = text.Substring(end, lineEnd - end).TrimEnd('\r');
                    SizeF measured = e.Graphics.MeasureString(line.Length == 0 ? " " : line, printFont, e.MarginBounds.Width);
                    if (y + measured.Height > e.MarginBounds.Bottom && end > start) break;
                    e.Graphics.DrawString(line, printFont, brush, new RectangleF(e.MarginBounds.Left, y, e.MarginBounds.Width, measured.Height));
                    y += measured.Height;
                    end = lineEnd < text.Length ? lineEnd + 1 : text.Length;
                }
                _printPosition = end;
                e.HasMorePages = end < text.Length;
            }
        }
        private void EditorTextChanged(object sender, EventArgs e)
        {
            if (!_suppressChange) { SetDirty(true); RaiseDocumentChanged(); }
            UpdateStatus();
        }
        private void EditorSelectionChanged(object sender, EventArgs e)
        {
            Font font = _editor.SelectionFont;
            _updatingRibbon = true;
            try
            {
                if (font != null)
                {
                    _fontBox.Text = font.FontFamily.Name;
                    _sizeBox.Text = font.Size.ToString("0.#", CultureInfo.CurrentCulture);
                    _boldButton.Checked = (font.Style & FontStyle.Bold) != 0;
                    _italicButton.Checked = (font.Style & FontStyle.Italic) != 0;
                    _underlineButton.Checked = (font.Style & FontStyle.Underline) != 0;
                }
            }
            finally { _updatingRibbon = false; }
        }
        private void SetDirty(bool value) { _dirty = value; }
        private void MarkFormattingChanged()
        {
            if (_suppressChange) return;
            SetDirty(true);
            RaiseDocumentChanged();
        }
        private void RaiseDocumentChanged() { EventHandler handler = DocumentChanged; if (handler != null) handler(this, EventArgs.Empty); }
        private void UpdateStatus() { _counts.Text = WordCount.ToString(CultureInfo.CurrentCulture) + " words  |  " + CharacterCount.ToString(CultureInfo.CurrentCulture) + " characters"; }
        private void UpdateZoom()
        {
            if (_editor == null) return;
            _editor.ZoomFactor = _zoomFactor;
            if (_zoomLabel != null) _zoomLabel.Text = ((int)(_zoomFactor * 100)).ToString(CultureInfo.CurrentCulture) + "%";
        }
        private static int CountWords(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return 0;
            return text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private void SaveDocx(string path)
        {
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            int oldStart = _editor.SelectionStart, oldLength = _editor.SelectionLength;
            string source = _editor.Text;
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, false))
                {
                    AddZipText(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
                    AddZipText(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
                    List<XElement> paragraphs = new List<XElement>();
                    int position = 0;
                    while (position <= source.Length)
                    {
                        int lineEnd = position;
                        while (lineEnd < source.Length && source[lineEnd] != '\r' && source[lineEnd] != '\n') lineEnd++;
                        XElement paragraph = new XElement(w + "p");
                        XElement pPr = new XElement(w + "pPr");
                        HorizontalAlignment align = HorizontalAlignment.Left;
                        if (position < lineEnd) { _editor.Select(position, 1); align = _editor.SelectionAlignment; }
                        if (align == HorizontalAlignment.Center) pPr.Add(new XElement(w + "jc", new XAttribute(w + "val", "center")));
                        else if (align == HorizontalAlignment.Right) pPr.Add(new XElement(w + "jc", new XAttribute(w + "val", "right")));
                        if (pPr.HasElements) paragraph.Add(pPr);
                        int cursor = position;
                        while (cursor < lineEnd)
                        {
                            _editor.Select(cursor, 1);
                            Font f = _editor.SelectionFont ?? _editor.Font;
                            Color sourceColor = _editor.SelectionColor;
                            Color color = sourceColor;
                            if (color.ToArgb() == ZenTheme.Foreground.ToArgb()) color = Color.Black;
                            int runEnd = cursor + 1;
                            while (runEnd < lineEnd)
                            {
                                _editor.Select(runEnd, 1);
                                Font other = _editor.SelectionFont ?? _editor.Font;
                                if (other.FontFamily.Name != f.FontFamily.Name || Math.Abs(other.Size - f.Size) > 0.1f || other.Style != f.Style || _editor.SelectionColor != sourceColor) break;
                                runEnd++;
                            }
                            XElement props = new XElement(w + "rPr");
                            props.Add(new XElement(w + "rFonts", new XAttribute(w + "ascii", f.FontFamily.Name), new XAttribute(w + "hAnsi", f.FontFamily.Name)));
                            if ((f.Style & FontStyle.Bold) != 0) props.Add(new XElement(w + "b"));
                            if ((f.Style & FontStyle.Italic) != 0) props.Add(new XElement(w + "i"));
                            if ((f.Style & FontStyle.Underline) != 0) props.Add(new XElement(w + "u", new XAttribute(w + "val", "single")));
                            props.Add(new XElement(w + "sz", new XAttribute(w + "val", Math.Max(2, (int)Math.Round(f.Size * 2)).ToString(CultureInfo.InvariantCulture))));
                            props.Add(new XElement(w + "color", new XAttribute(w + "val", ColorToHex(color))));
                            paragraph.Add(new XElement(w + "r", props, new XElement(w + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), source.Substring(cursor, runEnd - cursor))));
                            cursor = runEnd;
                        }
                        if (cursor == position) paragraph.Add(new XElement(w + "r", new XElement(w + "t", String.Empty)));
                        paragraphs.Add(paragraph);
                        if (lineEnd == source.Length) break;
                        position = lineEnd + (source[lineEnd] == '\r' && lineEnd + 1 < source.Length && source[lineEnd + 1] == '\n' ? 2 : 1);
                    }
                    XElement document = new XElement(w + "document", new XAttribute(XNamespace.Xmlns + "r", rel), new XElement(w + "body", paragraphs, new XElement(w + "sectPr")));
                    AddZipText(archive, "word/document.xml", new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), document).ToString(SaveOptions.DisableFormatting));
                }
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            finally
            {
                _editor.Select(Math.Min(oldStart, _editor.TextLength), Math.Min(oldLength, Math.Max(0, _editor.TextLength - oldStart)));
                if (File.Exists(temporaryPath)) { try { File.Delete(temporaryPath); } catch { } }
            }
        }
        private void LoadDocx(string path)
        {
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Read, false))
            {
                ZipArchiveEntry entry = archive.GetEntry("word/document.xml");
                if (entry == null) throw new InvalidDataException("Invalid DOCX: Principal document wasn't found.");
                if (entry.Length > 16L * 1024L * 1024L) throw new InvalidDataException("DOCX file are very big to open.");
                XDocument doc;
                using (Stream input = entry.Open())
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.DtdProcessing = DtdProcessing.Prohibit;
                    settings.XmlResolver = null;
                    settings.MaxCharactersInDocument = 16L * 1024L * 1024L;
                    using (XmlReader reader = XmlReader.Create(input, settings)) doc = XDocument.Load(reader);
                }
                StringBuilder text = new StringBuilder();
                List<DocxStyleRange> ranges = new List<DocxStyleRange>();
                List<DocxParagraphRange> paragraphRanges = new List<DocxParagraphRange>();
                List<XElement> sourceParagraphs = doc.Descendants(w + "body").Elements(w + "p").ToList();
                for (int pi = 0; pi < sourceParagraphs.Count; pi++)
                {
                    XElement paragraph = sourceParagraphs[pi];
                    int paragraphStart = text.Length;
                    foreach (XElement run in paragraph.Elements(w + "r"))
                    {
                        XElement rpr = run.Element(w + "rPr");
                        FontStyle style = FontStyle.Regular;
                        if (rpr != null && IsDocxFlagEnabled(rpr.Element(w + "b"), w)) style |= FontStyle.Bold;
                        if (rpr != null && IsDocxFlagEnabled(rpr.Element(w + "i"), w)) style |= FontStyle.Italic;
                        if (rpr != null && IsDocxFlagEnabled(rpr.Element(w + "u"), w)) style |= FontStyle.Underline;
                        float size = 11;
                        if (rpr != null && rpr.Element(w + "sz") != null) { float parsed; if (Single.TryParse((string)rpr.Element(w + "sz").Attribute(w + "val"), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) size = parsed / 2.0f; }
                        string fontName = "Segoe UI";
                        if (rpr != null && rpr.Element(w + "rFonts") != null) fontName = (string)rpr.Element(w + "rFonts").Attribute(w + "ascii") ?? fontName;
                        Color color = ZenTheme.Foreground;
                        if (rpr != null && rpr.Element(w + "color") != null) { try { color = ColorTranslator.FromHtml("#" + (string)rpr.Element(w + "color").Attribute(w + "val")); } catch { } }
                        foreach (XElement item in run.Elements())
                        {
                            string value = item.Name == w + "t" ? (string)item : (item.Name == w + "tab" ? "\t" : String.Empty);
                            if (item.Name == w + "br") value = Environment.NewLine;
                            if (value.Length == 0) continue;
                            int start = text.Length;
                            text.Append(value);
                            ranges.Add(new DocxStyleRange(start, value.Length, size, style, color, fontName));
                        }
                    }
                    XElement pPr = paragraph.Element(w + "pPr");
                    XElement jc = pPr == null ? null : pPr.Element(w + "jc");
                    paragraphRanges.Add(new DocxParagraphRange(paragraphStart, text.Length - paragraphStart, jc == null ? String.Empty : (string)jc.Attribute(w + "val")));
                    if (pi + 1 < sourceParagraphs.Count) text.Append(Environment.NewLine);
                }
                _editor.Text = text.ToString();
                foreach (DocxStyleRange range in ranges)
                {
                    if (range.Length <= 0 || range.Start >= _editor.TextLength) continue;
                    _editor.Select(range.Start, Math.Min(range.Length, _editor.TextLength - range.Start));
                    _editor.SelectionFont = CreateDocxFont(range.FontName, Math.Max(1, Math.Min(200, range.Size)), range.Style);
                    _editor.SelectionColor = range.Color;
                }
                foreach (DocxParagraphRange paragraph in paragraphRanges)
                {
                    if (paragraph.Start > _editor.TextLength) continue;
                    _editor.Select(paragraph.Start, Math.Min(paragraph.Length, _editor.TextLength - paragraph.Start));
                    _editor.SelectionAlignment = paragraph.Alignment == "center" ? HorizontalAlignment.Center : (paragraph.Alignment == "right" ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                }
                _editor.Select(0, 0);
            }
        }
        private sealed class DocxStyleRange
        {
            public readonly int Start, Length; public readonly float Size; public readonly FontStyle Style; public readonly Color Color; public readonly string FontName;
            public DocxStyleRange(int start, int length, float size, FontStyle style, Color color, string fontName) { Start = start; Length = length; Size = size; Style = style; Color = color; FontName = fontName; }
        }
        private sealed class DocxParagraphRange
        {
            public readonly int Start, Length; public readonly string Alignment;
            public DocxParagraphRange(int start, int length, string alignment) { Start = start; Length = length; Alignment = alignment; }
        }
        private Font CreateDocxFont(string family, float size, FontStyle style)
        {
            try { return OwnFont(new Font(family, size, style)); }
            catch { return OwnFont(new Font("Segoe UI", size, style)); }
        }
        private static bool IsDocxFlagEnabled(XElement element, XNamespace w)
        {
            if (element == null) return false;
            string value = (string)element.Attribute(w + "val");
            return value != "0" && !String.Equals(value, "false", StringComparison.OrdinalIgnoreCase) && !String.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
        }
        private static void AddZipText(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(content);
        }
        private static string ColorToHex(Color color) { return color.R.ToString("X2", CultureInfo.InvariantCulture) + color.G.ToString("X2", CultureInfo.InvariantCulture) + color.B.ToString("X2", CultureInfo.InvariantCulture); }
    }
}
