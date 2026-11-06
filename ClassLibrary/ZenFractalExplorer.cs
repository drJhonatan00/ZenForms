using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public enum ZenFractalMode
    {
        Mandelbrot,
        Julia
    }

    [ToolboxItem(true)]
    [Category("ZenForms")]
    [Description("Interactive Mandelbrot and Julia fractal explorer.")]
    public class ZenFractalExplorer : UserControl
    {
        private readonly ToolTip _toolTip;
        private readonly Panel _surface;
        private readonly Label _status;
        private readonly Button _resetButton;
        private readonly Button _modeButton;
        private readonly NumericUpDown _iterationsInput;
        private Bitmap _image;
        private BackgroundWorker _worker;
        private int _generation;
        private int _maxIterations = 120;
        private double _zoom = 1.0;
        private double _centerX = -0.5;
        private double _centerY;
        private ZenFractalMode _mode;
        private bool _dragging;
        private Point _lastMouse;
        private Point _dragStart;
        private double _dragCenterX;
        private double _dragCenterY;
        private bool _disposed;

        public ZenFractalExplorer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            Font = ZenTheme.DefaultFont;
            MinimumSize = new Size(280, 220);
            TabStop = true;

            _toolTip = new ToolTip();
            _surface = new Panel();
            _surface.Dock = DockStyle.Fill;
            _surface.BackColor = ZenTheme.Surface;
            _surface.TabStop = true;
            _surface.Paint += SurfacePaint;
            _surface.MouseWheel += SurfaceMouseWheel;
            _surface.MouseDown += SurfaceMouseDown;
            _surface.MouseMove += SurfaceMouseMove;
            _surface.MouseUp += SurfaceMouseUp;
            _surface.Resize += SurfaceResize;
            _surface.KeyDown += SurfaceKeyDown;
            _surface.MouseEnter += delegate { _surface.Focus(); };

            Panel toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 42;
            toolbar.BackColor = ZenTheme.SurfaceRaised;
            _modeButton = CreateButton("Mandelbrot", 8, 7, 102);
            _modeButton.Click += delegate { Mode = Mode == ZenFractalMode.Mandelbrot ? ZenFractalMode.Julia : ZenFractalMode.Mandelbrot; };
            _iterationsInput = new NumericUpDown();
            _iterationsInput.Minimum = 20;
            _iterationsInput.Maximum = 2000;
            _iterationsInput.Value = _maxIterations;
            _iterationsInput.Width = 78;
            _iterationsInput.Location = new Point(120, 9);
            _iterationsInput.BackColor = ZenTheme.Surface;
            _iterationsInput.ForeColor = ZenTheme.Foreground;
            _iterationsInput.BorderStyle = BorderStyle.FixedSingle;
            _iterationsInput.ValueChanged += delegate { MaxIterations = (int)_iterationsInput.Value; };
            Button applyButton = CreateButton("Render", 208, 7, 68);
            applyButton.Click += delegate { Render(); };
            _resetButton = CreateButton("Reset", 282, 7, 68);
            _resetButton.Click += delegate { ResetView(); };
            toolbar.Controls.Add(_modeButton);
            toolbar.Controls.Add(_iterationsInput);
            toolbar.Controls.Add(applyButton);
            toolbar.Controls.Add(_resetButton);
            _toolTip.SetToolTip(_modeButton, "Change the groups Mandelbrot and Julia.");
            _toolTip.SetToolTip(_iterationsInput, "Max iterations number (20–2000).");
            _toolTip.SetToolTip(applyButton, "Reload fractal.");
            _toolTip.SetToolTip(_resetButton, "Restore the framing to original position.");

            _status = new Label();
            _status.Dock = DockStyle.Bottom;
            _status.Height = 26;
            _status.Padding = new Padding(9, 5, 8, 3);
            _status.BackColor = ZenTheme.SurfaceRaised;
            _status.ForeColor = ZenTheme.Muted;
            _status.AutoEllipsis = true;
            _status.Text = "Ready · drag to move · scrool to zoom · +/− to zoom · R to reset";

            Controls.Add(_surface);
            Controls.Add(_status);
            Controls.Add(toolbar);
            _toolTip.SetToolTip(_surface, "Drag to move; Scrool to zoom.");
            _surface.Resize += delegate { if (IsHandleCreated && _surface.ClientSize.Width > 0 && _surface.ClientSize.Height > 0) Render(); };
            _surface.HandleCreated += delegate { Render(); };
        }

        [Category("Appearance"), Description("Fractal group.")]
        [DefaultValue(ZenFractalMode.Mandelbrot)]
        public ZenFractalMode Mode
        {
            get { return _mode; }
            set
            {
                if (value != ZenFractalMode.Mandelbrot && value != ZenFractalMode.Julia) throw new ArgumentOutOfRangeException("value");
                if (_mode == value) return;
                _mode = value;
                _modeButton.Text = value == ZenFractalMode.Mandelbrot ? "Mandelbrot" : "Julia";
                ResetView();
            }
        }

        [Category("Behavior"), Description("Max iterations number by pixel.")]
        [DefaultValue(120)]
        public int MaxIterations
        {
            get { return _maxIterations; }
            set
            {
                if (value < 20 || value > 2000) throw new ArgumentOutOfRangeException("value", "The value must be between 20 and 2000.");
                if (_maxIterations == value) return;
                _maxIterations = value;
                if (_iterationsInput.Value != value) _iterationsInput.Value = value;
                Render();
            }
        }

        [Category("Behavior"), Description("Current magnification factor; Higher values zoom in on the image.")]
        [DefaultValue(1.0)]
        public double Zoom
        {
            get { return _zoom; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.25 || value > 1000000.0)
                    throw new ArgumentOutOfRangeException("value", "Zoom must be between 0,25 and 1.000.000.");
                if (_zoom == value) return;
                _zoom = value;
                Render();
            }
        }

        [Category("Action"), Description("Triggered when the generation of a current image finishes.")]
        public event EventHandler RenderCompleted;

        [Category("Action"), Description("Triggered when a render failed.")]
        public event EventHandler<ZenFractalRenderErrorEventArgs> RenderFailed;

        public void Render()
        {
            if (_disposed || IsDisposed || !IsHandleCreated || _surface.ClientSize.Width < 1 || _surface.ClientSize.Height < 1) return;
            CancelCurrentRender();
            int generation = ++_generation;
            int width = Math.Min(_surface.ClientSize.Width, 1600);
            int height = Math.Min(_surface.ClientSize.Height, 1200);
            int iterations = _maxIterations;
            double zoom = _zoom, cx = _centerX, cy = _centerY;
            ZenFractalMode mode = _mode;
            _status.Text = "Renderizing · " + mode + " · " + iterations + " iterations";
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += delegate (object sender, DoWorkEventArgs e)
            {
                BackgroundWorker current = (BackgroundWorker)sender;
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                try
                {
                    BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        int stride = bitmapData.Stride;
                        byte[] pixels = new byte[stride * height];
                        double scale = 4.0 / (zoom * Math.Min(width, height));
                        for (int y = 0; y < height; y++)
                        {
                            if (current.CancellationPending) { e.Cancel = true; return; }
                            for (int x = 0; x < width; x++)
                            {
                                double px = cx + (x - width / 2.0) * scale;
                                double py = cy + (y - height / 2.0) * scale;
                                double zx, zy, qx, qy;
                                if (mode == ZenFractalMode.Mandelbrot) { zx = 0; zy = 0; qx = px; qy = py; }
                                else { zx = px; zy = py; qx = -0.8; qy = 0.156; }
                                int n = 0;
                                while (n < iterations && zx * zx + zy * zy <= 256.0)
                                {
                                    double next = zx * zx - zy * zy + qx;
                                    zy = 2.0 * zx * zy + qy;
                                    zx = next;
                                    n++;
                                }
                                Color color = NeonColor(n, iterations, zx, zy);
                                int index = y * stride + x * 3;
                                pixels[index] = color.B; pixels[index + 1] = color.G; pixels[index + 2] = color.R;
                            }
                        }
                        Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                    }
                    finally { bmp.UnlockBits(bitmapData); }
                    e.Result = bmp;
                    bmp = null;
                }
                finally { if (bmp != null) bmp.Dispose(); }
            };
            worker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e)
            {
                BackgroundWorker finished = (BackgroundWorker)sender;
                if (ReferenceEquals(_worker, finished)) _worker = null;
                finished.Dispose();
                if (_disposed || IsDisposed || generation != _generation)
                {
                    if (!e.Cancelled && e.Error == null && e.Result is Bitmap) ((Bitmap)e.Result).Dispose();
                    return;
                }
                if (e.Error != null)
                {
                    _status.Text = "Renderize failed: " + e.Error.GetBaseException().Message;
                    EventHandler<ZenFractalRenderErrorEventArgs> failed = RenderFailed;
                    if (failed != null) failed(this, new ZenFractalRenderErrorEventArgs(e.Error.GetBaseException()));
                    return;
                }
                if (e.Cancelled) return;
                Bitmap old = _image;
                _image = (Bitmap)e.Result;
                if (old != null) old.Dispose();
                _surface.Invalidate();
                _status.Text = string.Format("{0} · zoom {1:0.##}× · center {2:0.000000}, {3:0.000000}", mode, zoom, cx, cy);
                EventHandler completed = RenderCompleted;
                if (completed != null) completed(this, EventArgs.Empty);
            };
            _worker = worker;
            try { worker.RunWorkerAsync(); }
            catch (Exception ex)
            {
                _worker = null;
                worker.Dispose();
                _status.Text = "Failed to start render: " + ex.Message;
                EventHandler<ZenFractalRenderErrorEventArgs> failed = RenderFailed;
                if (failed != null) failed(this, new ZenFractalRenderErrorEventArgs(ex));
            }
        }

        public void ResetView()
        {
            _zoom = 1.0;
            _centerX = _mode == ZenFractalMode.Mandelbrot ? -0.5 : 0.0;
            _centerY = 0.0;
            Render();
        }

        private static Color NeonColor(int n, int max, double zx, double zy)
        {
            if (n >= max) return Color.FromArgb(13, 9, 24);
            double smooth = n + 1.0 - Math.Log(Math.Max(1.0, Math.Log(Math.Sqrt(zx * zx + zy * zy)))) / Math.Log(2.0);
            double t = Math.Max(0, smooth) * 0.085;
            int r = (int)(115 + 110 * Math.Sin(t + 0.3));
            int g = (int)(65 + 150 * Math.Sin(t + 2.0));
            int b = (int)(150 + 100 * Math.Sin(t + 4.1));
            return Color.FromArgb(255, Clamp(r), Clamp(g), Clamp(b));
        }
        private static int Clamp(int value) { return Math.Max(0, Math.Min(255, value)); }
        private Button CreateButton(string text, int x, int y, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, 28);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = ZenTheme.Border;
            button.BackColor = ZenTheme.Surface;
            button.ForeColor = ZenTheme.Foreground;
            button.Font = ZenTheme.DefaultFont;
            button.TabStop = true;
            return button;
        }
        private void CancelCurrentRender()
        {
            if (_worker != null && _worker.IsBusy)
            {
                try { _worker.CancelAsync(); } catch (ObjectDisposedException) { }
            }
        }
        private void SurfacePaint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(ZenTheme.Surface);
            if (_image != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                e.Graphics.DrawImage(_image, _surface.ClientRectangle);
            }
            else
            {
                using (Brush brush = new SolidBrush(ZenTheme.Muted))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString("Waiting render…", Font, brush, _surface.ClientRectangle, format);
            }
        }
        private void SurfaceResize(object sender, EventArgs e) { }
        private void SurfaceMouseWheel(object sender, MouseEventArgs e)
        {
            if (_surface.ClientSize.Width < 1 || _surface.ClientSize.Height < 1) return;
            Point p = _surface.PointToClient(Cursor.Position);
            double scale = 4.0 / (_zoom * Math.Min(_surface.ClientSize.Width, _surface.ClientSize.Height));
            double wx = _centerX + (p.X - _surface.Width / 2.0) * scale;
            double wy = _centerY + (p.Y - _surface.Height / 2.0) * scale;
            double factor = e.Delta > 0 ? 1.25 : 0.8;
            double nextZoom = Math.Max(0.25, Math.Min(1000000.0, _zoom * factor));
            double nextScale = 4.0 / (nextZoom * Math.Min(_surface.Width, _surface.Height));
            _centerX = wx - (p.X - _surface.Width / 2.0) * nextScale;
            _centerY = wy - (p.Y - _surface.Height / 2.0) * nextScale;
            _zoom = nextZoom;
            Render();
        }
        private void SurfaceMouseDown(object sender, MouseEventArgs e)
        {
            _surface.Focus();
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _lastMouse = e.Location;
                _dragStart = e.Location;
                _dragCenterX = _centerX;
                _dragCenterY = _centerY;
                _surface.Cursor = Cursors.Hand;
            }
        }
        private void SurfaceMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                _lastMouse = e.Location;
                double scale = 4.0 / (_zoom * Math.Min(_surface.ClientSize.Width, _surface.ClientSize.Height));
                _centerX = _dragCenterX - (e.X - _dragStart.X) * scale;
                _centerY = _dragCenterY - (e.Y - _dragStart.Y) * scale;
                Render();
            }
            if (_surface.ClientSize.Width > 0 && _surface.ClientSize.Height > 0)
            {
                double scale = 4.0 / (_zoom * Math.Min(_surface.ClientSize.Width, _surface.ClientSize.Height));
                double x = _centerX + (e.X - _surface.Width / 2.0) * scale;
                double y = _centerY + (e.Y - _surface.Height / 2.0) * scale;
                _status.Text = string.Format("{0} · zoom {1:0.##}× · cursor {2:0.000000}, {3:0.000000}", _mode, _zoom, x, y);
            }
        }
        private void SurfaceMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = false; _surface.Cursor = Cursors.Default; }
        }
        private void SurfaceKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R) { ResetView(); e.Handled = true; }
            else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) { Zoom = Math.Min(1000000.0, _zoom * 1.25); e.Handled = true; }
            else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) { Zoom = Math.Max(0.25, _zoom / 1.25); e.Handled = true; }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _generation++;
                CancelCurrentRender();
                if (_image != null) { _image.Dispose(); _image = null; }
                if (_toolTip != null) _toolTip.Dispose();
                if (_worker != null && !_worker.IsBusy) { _worker.Dispose(); _worker = null; }
            }
            base.Dispose(disposing);
        }
    }

    public class ZenFractalRenderErrorEventArgs : EventArgs
    {
        public ZenFractalRenderErrorEventArgs(Exception exception) { Exception = exception; }
        public Exception Exception { get; private set; }
    }
}
//watashi wa watashi sore dake