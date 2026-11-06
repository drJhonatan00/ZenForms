using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Screenshoter.")]
    public class ZenScreenCaptureStudio : UserControl
    {
        private Bitmap _image;
        private Bitmap _original;
        private readonly CaptureCanvas _canvas;
        private readonly ToolTip _toolTips;
        private readonly Button _captureButton, _cropButton, _redactButton, _undoButton, _copyButton, _saveButton;
        private bool _redactionMode;
        private Rectangle _selection;
        private readonly List<Rectangle> _redactions = new List<Rectangle>();
        private Rectangle _dragRect;

        public event EventHandler ImageCaptured;

        public ZenScreenCaptureStudio()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(360, 260);
            _toolTips = new ToolTip();

            var bar = new FlowLayoutPanel();
            bar.Dock = DockStyle.Top;
            bar.Height = 44;
            bar.Padding = new Padding(7, 5, 5, 4);
            bar.WrapContents = false;
            bar.BackColor = ZenTheme.Surface;
            _captureButton = MakeButton("Capture", "Take a screenshot of your current screen now. Click again to do it");
            _cropButton = MakeButton("Crop", "Active and drag to select one region. Click again to do it");
            _redactButton = MakeButton("Redact", "Active and drag image areas. Click again to do it");
            _undoButton = MakeButton("Restore", "Restore to original. Click again to do it");
            _copyButton = MakeButton("Copy", "Copy the current image. Click again to do it");
            _saveButton = MakeButton("Save…", "Save screenshot how a PNG or JPG image. Click again to do it");
            bar.Controls.AddRange(new Control[] { _captureButton, _cropButton, _redactButton, _undoButton, _copyButton, _saveButton });
            _canvas = new CaptureCanvas(this);
            _canvas.Dock = DockStyle.Fill;
            _canvas.TabStop = true;
            _canvas.AccessibleName = "Screenshot preview";
            _canvas.AccessibleDescription = "Drag to select different areas.";
            Controls.Add(_canvas);
            Controls.Add(bar);

            _captureButton.Click += delegate { using (Bitmap captured = CaptureScreen()) { } };
            _cropButton.Click += delegate { if (!_selection.IsEmpty) ApplyCrop(_selection); else ToggleMode(false); };
            _redactButton.Click += delegate { ToggleMode(true); };
            _undoButton.Click += delegate { RestoreOriginal(); };
            _copyButton.Click += delegate { CopyToClipboard(); };
            _saveButton.Click += delegate { SaveWithDialog(); };
            UpdateButtons();
        }

        [Category("Appearance")]
        [Description("Current final image; get and set return a independent value.")]
        public Image Image
        {
            get { return _image == null ? null : (Image)_image.Clone(); }
            set { SetImage(value); }
        }

        public Bitmap CaptureScreen()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen == null ? SystemInformation.VirtualScreen : Screen.PrimaryScreen.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("Wasn't possible determinate the limits of the principal monitor connections.");
                Bitmap captured = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                try
                {
                    using (Graphics graphics = Graphics.FromImage(captured))
                        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    SetImage(captured);
                }
                finally { captured.Dispose(); }
                OnImageCaptured(EventArgs.Empty);
                return _image == null ? null : (Bitmap)_image.Clone();
            }
            catch (Exception ex)
            {
                ShowError("Wasn't possible take a screenshot. Verify the graphic session and try again.", ex);
                return null;
            }
        }
        public bool SaveImage(string path)
        {
            if (_image == null) throw new InvalidOperationException("Isn't there image to save.");
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path file is obrigatory.", "path");
            string ext = Path.GetExtension(path).ToLowerInvariant();
            ImageFormat format;
            if (ext == ".png") format = ImageFormat.Png;
            else if (ext == ".jpg" || ext == ".jpeg") format = ImageFormat.Jpeg;
            else throw new ArgumentException("Use an extension .png, .jpg ou .jpeg.", "path");
            if (File.Exists(path))
            {
                DialogResult answer = MessageBox.Show(FindForm(), "This file already exists. Are you want replace?", "Confirm replace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return false;
            }
            try
            {
                using (Bitmap snapshot = (Bitmap)_image.Clone()) snapshot.Save(path, format);
                return true;
            }
            catch (Exception ex) { ShowError("Wasn't possible save the image.", ex); return false; }
        }
        public void CopyToClipboard()
        {
            if (_image == null) throw new InvalidOperationException("Isn't there image to copy.");
            try { using (Bitmap copy = (Bitmap)_image.Clone()) Clipboard.SetImage(copy); }
            catch (Exception ex) { ShowError("Wasn't possible copy the image to clipboard.", ex); }
        }

        protected virtual void OnImageCaptured(EventArgs e)
        {
            EventHandler handler = ImageCaptured;
            if (handler != null) handler(this, e);
        }

        private Button MakeButton(string text, string tip)
        {
            Button b = new Button();
            b.Text = text; b.AutoSize = true; b.Height = 31; b.FlatStyle = FlatStyle.Flat;
            b.BackColor = ZenTheme.SurfaceRaised; b.ForeColor = ZenTheme.Foreground;
            b.FlatAppearance.BorderColor = ZenTheme.Border;
            _toolTips.SetToolTip(b, tip);
            return b;
        }

        private void SetImage(Image source)
        {
            Bitmap next = source == null ? null : new Bitmap(source);
            Bitmap original = source == null ? null : new Bitmap(source);
            if (_image != null) _image.Dispose();
            if (_original != null) _original.Dispose();
            _image = next; _original = original;
            _selection = Rectangle.Empty; _dragRect = Rectangle.Empty; _redactions.Clear();
            _canvas.Invalidate(); UpdateButtons();
        }

        private void RestoreOriginal()
        {
            if (_original == null) return;
            Bitmap restored = (Bitmap)_original.Clone();
            if (_image != null) _image.Dispose();
            _image = restored; _selection = Rectangle.Empty; _dragRect = Rectangle.Empty; _redactions.Clear();
            _redactionMode = false; _canvas.Invalidate(); UpdateButtons();
        }

        private void ToggleMode(bool redact)
        {
            if (_image == null) return;
            _redactionMode = redact ? !_redactionMode : false;
            if (!redact) _selection = Rectangle.Empty;
            _canvas.Cursor = _redactionMode ? Cursors.Cross : (_selection.IsEmpty ? Cursors.Default : Cursors.Cross);
            UpdateButtons(); _canvas.Focus(); _canvas.Invalidate();
        }

        private void UpdateButtons()
        {
            bool has = _image != null;
            if (_captureButton == null) return;
            _cropButton.Enabled = has; _redactButton.Enabled = has; _undoButton.Enabled = has;
            _copyButton.Enabled = has; _saveButton.Enabled = has;
            _redactButton.BackColor = _redactionMode ? ZenTheme.Primary : ZenTheme.SurfaceRaised;
        }

        private void SaveWithDialog()
        {
            if (_image == null) return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save screenshot"; dialog.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg";
                dialog.DefaultExt = "png"; dialog.AddExtension = true; dialog.OverwritePrompt = false;
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) SaveImage(dialog.FileName);
            }
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show(FindForm(), message + Environment.NewLine + ex.Message, "ZenScreenCaptureStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private Rectangle ImageBounds
        {
            get
            {
                if (_image == null || _canvas.ClientSize.Width <= 0 || _canvas.ClientSize.Height <= 0) return Rectangle.Empty;
                float scale = Math.Min((float)_canvas.ClientSize.Width / _image.Width, (float)_canvas.ClientSize.Height / _image.Height);
                int w = Math.Max(1, (int)(_image.Width * scale)), h = Math.Max(1, (int)(_image.Height * scale));
                return new Rectangle((_canvas.ClientSize.Width - w) / 2, (_canvas.ClientSize.Height - h) / 2, w, h);
            }
        }

        private Point ToImage(Point p)
        {
            Rectangle b = ImageBounds;
            if (b.IsEmpty) return Point.Empty;
            int x = Math.Max(0, Math.Min(_image.Width, (int)((p.X - b.X) * (double)_image.Width / b.Width)));
            int y = Math.Max(0, Math.Min(_image.Height, (int)((p.Y - b.Y) * (double)_image.Height / b.Height)));
            return new Point(x, y);
        }

        private void ApplyCrop(Rectangle r)
        {
            if (_image == null || r.Width < 2 || r.Height < 2) return;
            Rectangle bounds = new Rectangle(0, 0, _image.Width, _image.Height);
            r = Rectangle.Intersect(r, bounds);
            if (r.Width < 2 || r.Height < 2) return;
            Bitmap cropped = _image.Clone(r, PixelFormat.Format32bppArgb);
            _image.Dispose(); _image = cropped; _redactions.Clear(); _selection = Rectangle.Empty;
            _redactionMode = false; _canvas.Invalidate(); UpdateButtons();
        }

        private void ApplyRedaction(Rectangle r)
        {
            if (_image == null || r.Width < 2 || r.Height < 2) return;
            r = Rectangle.Intersect(r, new Rectangle(0, 0, _image.Width, _image.Height));
            if (r.Width < 1 || r.Height < 1) return;
            using (Graphics g = Graphics.FromImage(_image)) { using (Brush brush = new SolidBrush(Color.Black)) g.FillRectangle(brush, r); }
            _redactions.Add(r); _canvas.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_image != null) { _image.Dispose(); _image = null; }
                if (_original != null) { _original.Dispose(); _original = null; }
                if (_toolTips != null) _toolTips.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class CaptureCanvas : Control
        {
            private readonly ZenScreenCaptureStudio _owner;
            private Point _start;
            private bool _dragging;
            internal CaptureCanvas(ZenScreenCaptureStudio owner) { _owner = owner; BackColor = ZenTheme.Background; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.Clear(BackColor);
                Rectangle b = _owner.ImageBounds;
                if (_owner._image == null || b.IsEmpty)
                {
                    using (Brush br = new SolidBrush(ZenTheme.Muted)) e.Graphics.DrawString("Take a screenshot to start", Font, br, 16, 18);
                    return;
                }
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(_owner._image, b);
                using (Pen border = new Pen(ZenTheme.Border, 1)) e.Graphics.DrawRectangle(border, b);
                Rectangle r = _owner._dragRect;
                if (!r.IsEmpty)
                {
                    Rectangle view = ImageRectToView(r, b);
                    using (Brush fill = new SolidBrush(Color.FromArgb(95, _owner._redactionMode ? Color.Black : ZenTheme.Accent))) e.Graphics.FillRectangle(fill, view);
                    using (Pen pen = new Pen(_owner._redactionMode ? Color.Black : ZenTheme.Accent, 2)) e.Graphics.DrawRectangle(pen, view);
                }
                if (!_owner._selection.IsEmpty && !_owner._redactionMode)
                {
                    Rectangle view = ImageRectToView(_owner._selection, b);
                    using (Pen pen = new Pen(ZenTheme.Accent, 2)) { pen.DashStyle = DashStyle.Dash; e.Graphics.DrawRectangle(pen, view); }
                }
            }
            private Rectangle ImageRectToView(Rectangle r, Rectangle b)
            {
                if (_owner._image == null) return Rectangle.Empty;
                int x = b.X + (int)(r.X * (double)b.Width / _owner._image.Width);
                int y = b.Y + (int)(r.Y * (double)b.Height / _owner._image.Height);
                int w = Math.Max(1, (int)(r.Width * (double)b.Width / _owner._image.Width));
                int h = Math.Max(1, (int)(r.Height * (double)b.Height / _owner._image.Height));
                return new Rectangle(x, y, w, h);
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e); if (e.Button != MouseButtons.Left || _owner._image == null) return;
                Focus(); _start = _owner.ToImage(e.Location); _dragging = true; _owner._dragRect = Rectangle.Empty;
            }
            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e); if (!_dragging) return;
                Point end = _owner.ToImage(e.Location);
                _owner._dragRect = Rectangle.FromLTRB(Math.Min(_start.X, end.X), Math.Min(_start.Y, end.Y), Math.Max(_start.X, end.X), Math.Max(_start.Y, end.Y)); Invalidate();
            }
            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e); if (!_dragging) return; _dragging = false;
                Rectangle r = _owner._dragRect; _owner._dragRect = Rectangle.Empty;
                if (_owner._redactionMode) _owner.ApplyRedaction(r); else { _owner._selection = r; Invalidate(); }
            }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Escape) { _owner._selection = Rectangle.Empty; _owner._dragRect = Rectangle.Empty; _owner._redactionMode = false; Invalidate(); _owner.UpdateButtons(); e.Handled = true; }
                else if (e.KeyCode == Keys.Enter && !_owner._selection.IsEmpty) { Rectangle r = _owner._selection; _owner.ApplyCrop(r); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete && !_owner._selection.IsEmpty) { _owner.ApplyRedaction(_owner._selection); _owner._selection = Rectangle.Empty; e.Handled = true; }
            }
        }
    }
}
// watashi wa watashi sore dake