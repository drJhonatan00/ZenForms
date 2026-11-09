using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class ZenDirectoryScanEventArgs : EventArgs
    {
        public string RootPath { get; private set; }
        public long TotalBytes { get; private set; }
        public int FileCount { get; private set; }
        public int FolderCount { get; private set; }
        public int AccessErrors { get; private set; }
        public int IgnoredItems { get; private set; }
        internal ZenDirectoryScanEventArgs(string root, long bytes, int files, int folders, int errors, int ignored)
        { RootPath = root; TotalBytes = bytes; FileCount = files; FolderCount = folders; AccessErrors = errors; IgnoredItems = ignored; }
    }
    public sealed class ZenDirectoryItemEventArgs : EventArgs
    {
        public string Path { get; private set; }
        public long SizeBytes { get; private set; }
        public bool IsDirectory { get; private set; }
        internal ZenDirectoryItemEventArgs(string path, long bytes, bool directory) { Path = path; SizeBytes = bytes; IsDirectory = directory; }
    }
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Directory local atlas of treemap by size, navigation, search and CSV exportation.")]
    public class ZenDirectoryAtlas : UserControl
    {
        private const int MaxEntries = 20000;
        private const int MaxDepth = 64;
        private const int MaxTiles = 800;
        private readonly TextBox pathBox, searchBox;
        private readonly Button scanButton, backButton, cancelButton, exportButton;
        private readonly Label statusLabel, metricsLabel, selectionLabel;
        private readonly AtlasCanvas canvas;
        private CancellationTokenSource cancellation;
        private int generation;
        private AtlasNode root;
        private AtlasNode current;
        private AtlasNode selected;
        private int fileCount, folderCount, accessErrors, ignoredItems;
        private long totalBytes;
        private string rootPath;
        private bool disposed;
        public event EventHandler<ZenDirectoryScanEventArgs> ScanCompleted;
        public event EventHandler<ZenDirectoryItemEventArgs> ItemSelected;
        public event EventHandler<ErrorEventArgs> ScanFailed;
        [Browsable(false)] public bool IsBusy { get; private set; }
        [Browsable(false)] public string RootPath { get { return rootPath; } }
        [Browsable(false)] public string SelectedPath { get { return selected == null ? null : selected.FullPath; } }

        public ZenDirectoryAtlas()
        {
            Size = new Size(720, 460); MinimumSize = new Size(340, 240);
            BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = ZenTheme.Background, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = ZenTheme.Background, Padding = new Padding(0, 2, 0, 0) };
            backButton = MakeButton("← Return", 82); scanButton = MakeButton("Analyze", 82); cancelButton = MakeButton("Cancel", 82); exportButton = MakeButton("Export CSV", 106);
            pathBox = new TextBox { Width = 250, Height = 26, BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            toolbar.Controls.Add(backButton); toolbar.Controls.Add(pathBox); toolbar.Controls.Add(scanButton); toolbar.Controls.Add(cancelButton); toolbar.Controls.Add(exportButton);
            FlowLayoutPanel info = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = ZenTheme.Background, Padding = new Padding(0, 4, 0, 0) };
            statusLabel = MakeLabel("Enter with a directory and click on Analyze.", ZenTheme.Muted, 270);
            metricsLabel = MakeLabel("Size: —   Files: 0   Folders: 0   Errors: 0   Ignoreds: 0", ZenTheme.Foreground, 520);
            info.Controls.Add(statusLabel); info.Controls.Add(metricsLabel);
            canvas = new AtlasCanvas { Dock = DockStyle.Fill, BackColor = ZenTheme.Surface };
            FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = ZenTheme.Background, Padding = new Padding(0, 6, 0, 0) };
            searchBox = new TextBox { Width = 190, BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground };
            selectionLabel = MakeLabel("Select a mosaic to view the path and size.", ZenTheme.Muted, 500);
            footer.Controls.Add(searchBox); footer.Controls.Add(selectionLabel);
            layout.Controls.Add(toolbar, 0, 0); layout.Controls.Add(info, 0, 1); layout.Controls.Add(canvas, 0, 2); layout.Controls.Add(footer, 0, 3); Controls.Add(layout);
            scanButton.Click += async delegate { await ScanAsync(pathBox.Text); };
            pathBox.KeyDown += async delegate (object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await ScanAsync(pathBox.Text); } };
            cancelButton.Click += delegate { CancelScan(); };
            backButton.Click += delegate { NavigateBack(); };
            exportButton.Click += delegate { ExportCsvWithDialog(); };
            searchBox.TextChanged += delegate { canvas.SearchText = searchBox.Text; canvas.Invalidate(); };
            canvas.TileClicked += OnTileClicked; UpdateButtons();
        }

        public async Task ScanAsync(string directoryPath)
        {
            if (disposed) throw new ObjectDisposedException("ZenDirectoryAtlas");
            if (String.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("Enter with a directory.", "directoryPath");
            string fullPath = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException("This directory doesn't exists: " + fullPath);
            CancelScan();
            int run = Interlocked.Increment(ref generation);
            CancellationTokenSource cts = new CancellationTokenSource(); cancellation = cts;
            IsBusy = true; statusLabel.Text = "Analyzing…"; pathBox.Text = fullPath; UpdateButtons();
            try
            {
                ScanResult result = await Task.Run(() => ScanDirectory(fullPath, cts.Token), cts.Token);
                if (disposed || run != generation || cts.IsCancellationRequested) return;
                root = result.Root; current = root; selected = null; rootPath = fullPath;
                totalBytes = result.Bytes; fileCount = result.Files; folderCount = result.Folders; accessErrors = result.Errors; ignoredItems = result.Ignored;
                canvas.Current = current; canvas.Selected = null; canvas.Invalidate(); UpdateMetrics();
                statusLabel.Text = "Concluded: " + Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                EventHandler<ZenDirectoryScanEventArgs> handler = ScanCompleted;
                if (handler != null) handler(this, new ZenDirectoryScanEventArgs(fullPath, totalBytes, fileCount, folderCount, accessErrors, ignoredItems));
            }
            catch (OperationCanceledException) { if (run == generation) statusLabel.Text = "Scan canceled."; }
            catch (Exception ex)
            {
                if (run == generation) { statusLabel.Text = "Scan failed."; EventHandler<ErrorEventArgs> handler = ScanFailed; if (handler != null) handler(this, new ErrorEventArgs(ex)); }
            }
            finally
            {
                if (Object.ReferenceEquals(cancellation, cts)) cancellation = null;
                cts.Dispose();
                if (!disposed && run == generation) { IsBusy = false; UpdateButtons(); }
            }
        }

        public void CancelScan()
        {
            CancellationTokenSource cts = cancellation;
            if (cts != null) { try { cts.Cancel(); } catch (ObjectDisposedException) { } }
        }

        public void NavigateBack()
        {
            if (current == null || current.Parent == null) return;
            current = current.Parent; selected = null; canvas.Current = current; canvas.Selected = null;
            canvas.Invalidate(); selectionLabel.Text = "Select a mosaic to view the path and size.";
        }

        public void ExportCsv(string filePath)
        {
            if (root == null) throw new InvalidOperationException("No verification has been completed yet.");
            if (String.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Enter with CSV path.", "filePath");
            using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("Tipo,Caminho,TamanhoBytes");
                WriteCsvNode(writer, root);
            }
        }

        private void ExportCsvWithDialog()
        {
            if (root == null) { statusLabel.Text = "Complete a scan before exporting."; return; }
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", DefaultExt = "csv", AddExtension = true, FileName = "atlas-path.csv" })
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { try { ExportCsv(dialog.FileName); statusLabel.Text = "CSV exportaded."; } catch (Exception ex) { statusLabel.Text = "Exportation failed: " + ex.Message; } }
        }

        private static void WriteCsvNode(StreamWriter writer, AtlasNode node)
        {
            if (node.Parent != null) writer.WriteLine((node.IsDirectory ? "Folder" : "File") + "," + CsvQuote(node.FullPath) + "," + node.Bytes.ToString(CultureInfo.InvariantCulture));
            foreach (AtlasNode child in node.Children) WriteCsvNode(writer, child);
        }
        private static string CsvQuote(string value) { return "\"" + (value ?? String.Empty).Replace("\"", "\"\"") + "\""; }

        private void OnTileClicked(AtlasNode node)
        {
            if (node == null) return;
            selected = node; canvas.Selected = node;
            selectionLabel.Text = node.FullPath + "   •   " + FormatSize(node.Bytes) + (node.IsDirectory ? "   •   Folder" : "   •   File");
            EventHandler<ZenDirectoryItemEventArgs> handler = ItemSelected;
            if (handler != null) handler(this, new ZenDirectoryItemEventArgs(node.FullPath, node.Bytes, node.IsDirectory));
            if (node.IsDirectory && node.Children.Count > 0) { current = node; canvas.Current = current; canvas.Invalidate(); }
        }

        private void UpdateMetrics() { metricsLabel.Text = "Size: " + FormatSize(totalBytes) + "   Files: " + fileCount + "   Folders: " + folderCount + "   Errors: " + accessErrors + "   Ignoreds: " + ignoredItems; }
        private void UpdateButtons() { if (scanButton != null) scanButton.Enabled = !IsBusy; if (cancelButton != null) cancelButton.Enabled = IsBusy; if (backButton != null) backButton.Enabled = current != null && current.Parent != null; if (exportButton != null) exportButton.Enabled = root != null; }
        private static Button MakeButton(string text, int width) { return new Button { Text = text, Width = width, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Margin = new Padding(0, 0, 6, 0), UseVisualStyleBackColor = false }; }
        private static Label MakeLabel(string text, Color color, int width) { return new Label { Text = text, AutoSize = false, Width = width, Height = 24, ForeColor = color, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }; }
        private static string FormatSize(long bytes) { string[] units = { "B", "KB", "MB", "GB", "TB" }; double n = Math.Max(0, bytes); int i = 0; while (n >= 1024 && i < units.Length - 1) { n /= 1024; i++; } return n.ToString(i == 0 ? "0" : "0.0", CultureInfo.CurrentCulture) + " " + units[i]; }

        private static ScanResult ScanDirectory(string path, CancellationToken token)
        {
            ScanResult result = new ScanResult();
            AtlasNode top = new AtlasNode { Name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), FullPath = path, IsDirectory = true };
            result.Root = top; result.Folders = 1;
            ScanFolder(top, 0, result, token);
            return result;
        }
        private static void ScanFolder(AtlasNode folder, int depth, ScanResult result, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (depth >= MaxDepth) { result.Ignored++; return; }
            try
            {
                foreach (string file in Directory.GetFiles(folder.FullPath))
                {
                    token.ThrowIfCancellationRequested();
                    if (result.Files + result.Folders >= MaxEntries) { result.Ignored++; continue; }
                    try
                    {
                        FileAttributes attr = File.GetAttributes(file);
                        if ((attr & FileAttributes.ReparsePoint) != 0) { result.Ignored++; continue; }
                        FileInfo info = new FileInfo(file); long len = info.Length;
                        AtlasNode item = new AtlasNode { Name = info.Name, FullPath = file, Bytes = len, IsDirectory = false, Parent = folder };
                        folder.Children.Add(item); AddBytes(folder, len); result.Files++;
                    }
                    catch (UnauthorizedAccessException) { result.Ignored++; }
                    catch (IOException) { result.Ignored++; }
                    catch (System.Security.SecurityException) { result.Ignored++; }
                }
                foreach (string dir in Directory.GetDirectories(folder.FullPath))
                {
                    token.ThrowIfCancellationRequested();
                    if (result.Files + result.Folders >= MaxEntries) { result.Ignored++; continue; }
                    try
                    {
                        FileAttributes attr = File.GetAttributes(dir);
                        if ((attr & FileAttributes.ReparsePoint) != 0) { result.Ignored++; continue; }
                        DirectoryInfo info = new DirectoryInfo(dir);
                        AtlasNode child = new AtlasNode { Name = info.Name, FullPath = dir, IsDirectory = true, Parent = folder };
                        folder.Children.Add(child); result.Folders++;
                        ScanFolder(child, depth + 1, result, token);
                        AddBytes(folder, child.Bytes);
                    }
                    catch (UnauthorizedAccessException) { result.Errors++; result.Ignored++; }
                    catch (IOException) { result.Errors++; result.Ignored++; }
                    catch (System.Security.SecurityException) { result.Errors++; result.Ignored++; }
                }
            }
            catch (UnauthorizedAccessException) { result.Errors++; result.Ignored++; }
            catch (IOException) { result.Errors++; result.Ignored++; }
            catch (System.Security.SecurityException) { result.Errors++; result.Ignored++; }
            if (folder.Parent == null) result.Bytes = folder.Bytes;
        }
        private static void AddBytes(AtlasNode node, long amount)
        {
            if (amount <= 0) return;
            unchecked { node.Bytes = node.Bytes > Int64.MaxValue - amount ? Int64.MaxValue : node.Bytes + amount; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed) { disposed = true; Interlocked.Increment(ref generation); CancelScan(); }
            base.Dispose(disposing);
        }

        private sealed class ScanResult { public AtlasNode Root; public long Bytes; public int Files, Folders, Errors, Ignored; }
        private sealed class AtlasNode
        {
            public string Name, FullPath; public bool IsDirectory; public long Bytes; public AtlasNode Parent;
            public readonly List<AtlasNode> Children = new List<AtlasNode>();
        }

        private sealed class AtlasCanvas : Control
        {
            public AtlasNode Current, Selected; public string SearchText = String.Empty;
            public event Action<AtlasNode> TileClicked;
            private readonly Font tileFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            public AtlasCanvas() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); e.Graphics.Clear(BackColor);
                if (Current == null) { using (Brush b = new SolidBrush(ZenTheme.Muted)) e.Graphics.DrawString("No folder analyzed", Font, b, new PointF(14, 14)); return; }
                List<AtlasNode> nodes = Current.Children.Where(n => String.IsNullOrEmpty(SearchText) || n.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0).Take(MaxTiles).ToList();
                if (nodes.Count == 0) { using (Brush b = new SolidBrush(ZenTheme.Muted)) e.Graphics.DrawString("No item found", Font, b, new PointF(14, 14)); return; }
                DrawTreemap(e.Graphics, nodes, new Rectangle(4, 4, Math.Max(1, ClientSize.Width - 8), Math.Max(1, ClientSize.Height - 8)), 0);
            }
            private void DrawTreemap(Graphics graphics, List<AtlasNode> nodes, Rectangle bounds, int depth)
            {
                if (nodes.Count == 0 || bounds.Width <= 1 || bounds.Height <= 1) return;
                if (nodes.Count == 1) { DrawTile(graphics, nodes[0], bounds); return; }
                long sum = 0; foreach (AtlasNode n in nodes) sum = sum > Int64.MaxValue - Math.Max(1, n.Bytes) ? Int64.MaxValue : sum + Math.Max(1, n.Bytes);
                bool vertical = bounds.Width >= bounds.Height;
                int split = vertical ? bounds.Width / 2 : bounds.Height / 2;
                long leftWeight = 0; int pivot = 0; long target = Math.Max(1, sum / 2);
                while (pivot < nodes.Count - 1 && leftWeight < target) { long w = Math.Max(1, nodes[pivot].Bytes); leftWeight = leftWeight > Int64.MaxValue - w ? Int64.MaxValue : leftWeight + w; pivot++; }
                List<AtlasNode> first = nodes.Take(Math.Max(1, pivot)).ToList(), second = nodes.Skip(Math.Max(1, pivot)).ToList();
                long firstWeight = 0; foreach (AtlasNode n in first) firstWeight += Math.Max(1, n.Bytes);
                int firstSize = (int)Math.Max(1, Math.Min((vertical ? bounds.Width : bounds.Height) - 1, (double)(vertical ? bounds.Width : bounds.Height) * firstWeight / Math.Max(1, sum)));
                if (vertical) { DrawTreemap(graphics, first, new Rectangle(bounds.X, bounds.Y, firstSize, bounds.Height), depth + 1); DrawTreemap(graphics, second, new Rectangle(bounds.X + firstSize, bounds.Y, bounds.Width - firstSize, bounds.Height), depth + 1); }
                else { DrawTreemap(graphics, first, new Rectangle(bounds.X, bounds.Y, bounds.Width, firstSize), depth + 1); DrawTreemap(graphics, second, new Rectangle(bounds.X, bounds.Y + firstSize, bounds.Width, bounds.Height - firstSize), depth + 1); }
            }
            private void DrawTile(Graphics g, AtlasNode node, Rectangle rect)
            {
                Color color = node.IsDirectory ? ZenTheme.Primary : ZenTheme.Accent;
                int shade = Math.Abs(node.Name.GetHashCode()) % 28;
                color = ControlPaint.Dark(color, shade / 100f);
                using (Brush brush = new SolidBrush(color)) g.FillRectangle(brush, rect);
                using (Pen pen = new Pen(Object.ReferenceEquals(node, Selected) ? ZenTheme.Foreground : ZenTheme.Background, Object.ReferenceEquals(node, Selected) ? 2f : 1f)) g.DrawRectangle(pen, rect.X, rect.Y, Math.Max(0, rect.Width - 1), Math.Max(0, rect.Height - 1));
                if (rect.Width > 44 && rect.Height > 24) { Rectangle text = Rectangle.Inflate(rect, -5, -4); using (Brush b = new SolidBrush(ZenTheme.Foreground)) { g.DrawString(node.Name, tileFont, b, text); if (rect.Height > 42) g.DrawString(FormatSize(node.Bytes), tileFont, b, new Rectangle(text.X, text.Y + 18, text.Width, text.Height - 18)); } }
            }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e); if (Current == null) return;
                List<AtlasNode> nodes = Current.Children.Where(n => String.IsNullOrEmpty(SearchText) || n.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0).Take(MaxTiles).ToList();
                AtlasNode hit = FindHit(nodes, new Rectangle(4, 4, Math.Max(1, ClientSize.Width - 8), Math.Max(1, ClientSize.Height - 8)), e.Location);
                Action<AtlasNode> handler = TileClicked; if (handler != null && hit != null) handler(hit);
            }
            private AtlasNode FindHit(List<AtlasNode> nodes, Rectangle bounds, Point point)
            {
                if (nodes.Count == 0 || !bounds.Contains(point)) return null;
                if (nodes.Count == 1) return nodes[0];
                long sum = nodes.Sum(n => Math.Max(1L, n.Bytes)); bool vertical = bounds.Width >= bounds.Height;
                int pivot = 0; long accumulated = 0, target = Math.Max(1, sum / 2);
                while (pivot < nodes.Count - 1 && accumulated < target) { accumulated += Math.Max(1L, nodes[pivot].Bytes); pivot++; }
                List<AtlasNode> first = nodes.Take(Math.Max(1, pivot)).ToList(), second = nodes.Skip(Math.Max(1, pivot)).ToList();
                long fw = first.Sum(n => Math.Max(1L, n.Bytes)); int len = (int)Math.Max(1, Math.Min((vertical ? bounds.Width : bounds.Height) - 1, (double)(vertical ? bounds.Width : bounds.Height) * fw / Math.Max(1, sum)));
                if (vertical) return point.X < bounds.X + len ? FindHit(first, new Rectangle(bounds.X, bounds.Y, len, bounds.Height), point) : FindHit(second, new Rectangle(bounds.X + len, bounds.Y, bounds.Width - len, bounds.Height), point);
                return point.Y < bounds.Y + len ? FindHit(first, new Rectangle(bounds.X, bounds.Y, bounds.Width, len), point) : FindHit(second, new Rectangle(bounds.X, bounds.Y + len, bounds.Width, bounds.Height - len), point);
            }
            protected override void Dispose(bool disposing) { if (disposing) tileFont.Dispose(); base.Dispose(disposing); }
        }
    }
}

// watashi wa watashi sore dake.