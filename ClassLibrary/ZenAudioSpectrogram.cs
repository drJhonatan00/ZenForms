using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZenForms.Core
{
    public sealed class ZenAudioSpectrogramErrorEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public ZenAudioSpectrogramErrorEventArgs(string message) { Message = message; }
    }

    public sealed class ZenAudioSelectionEventArgs : EventArgs
    {
        public double StartSeconds { get; private set; }
        public double EndSeconds { get; private set; }
        public ZenAudioSelectionEventArgs(double startSeconds, double endSeconds) { StartSeconds = startSeconds; EndSeconds = endSeconds; }
    }

    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("WAV PCM/IEEE-float spectrogram with FFT, time/frequence and temporal selection.")]
    public sealed class ZenAudioSpectrogram : UserControl
    {
        private const long MaxAudioBytes = 268435456L;
        private const long MaxFrames = 32000000L;
        private const int MaxColumns = 8192;
        private const long MaxImagePixels = 16000000L;
        private readonly Panel plot;
        private readonly Label status;
        private readonly Button open;
        private Bitmap spectrogram;
        private CancellationTokenSource currentCancellation;
        private int generation;
        private double duration;
        private int sampleRate;
        private int channels;
        private int fftSize = 1024;
        private int hopSize = 256;
        private double cursor = -1;
        private double selectionStart = -1;
        private double selectionEnd = -1;
        private Color lowColor = Color.FromArgb(12, 18, 40);
        private Color highColor = ZenTheme.Accent;
        private Color peakColor = ZenTheme.Primary;
        public event EventHandler<ZenAudioSelectionEventArgs> SelectionChanged;
        public event EventHandler<ZenAudioSpectrogramErrorEventArgs> LoadError;
        [Category("ZenForms Audio"), DefaultValue(1024)]
        public int FftSize { get { return fftSize; } set { if (value < 256 || value > 32768 || (value & (value - 1)) != 0) throw new ArgumentOutOfRangeException("value", "FFT must be power beetween 256 and 32768."); fftSize = value; } }
        [Category("ZenForms Audio"), DefaultValue(256)]
        public int HopSize { get { return hopSize; } set { if (value < 1 || value > fftSize) throw new ArgumentOutOfRangeException("value"); hopSize = value; } }
        [Category("ZenForms Audio")] public Color LowColor { get { return lowColor; } set { lowColor = value; plot.Invalidate(); } }
        [Category("ZenForms Audio")] public Color HighColor { get { return highColor; } set { highColor = value; plot.Invalidate(); } }
        [Category("ZenForms Audio")] public Color PeakColor { get { return peakColor; } set { peakColor = value; plot.Invalidate(); } }
        [Browsable(false)] public double DurationSeconds { get { return duration; } }
        [Browsable(false)] public int SampleRate { get { return sampleRate; } }
        [Browsable(false)] public int Channels { get { return channels; } }
        [Browsable(false)] public double PositionSeconds { get { return cursor; } }

        public ZenAudioSpectrogram()
        {
            Size = new Size(720, 300); BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground; Font = ZenTheme.DefaultFont;
            Panel bar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = ZenTheme.Surface };
            open = new Button { Dock = DockStyle.Left, Width = 108, Text = "Open WAV", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground };
            status = new Label { Dock = DockStyle.Fill, Text = "Open a WAV PCM or IEEE-float", ForeColor = ZenTheme.Muted, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
            bar.Controls.Add(status); bar.Controls.Add(open);
            plot = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 16, 28) };
            plot.Paint += PaintPlot; plot.MouseDown += PlotMouseDown; plot.MouseMove += PlotMouseMove; plot.MouseUp += PlotMouseUp;
            Controls.Add(plot); Controls.Add(bar); open.Click += OpenFile;
        }

        public async Task LoadWaveAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Enter with WAV path.", "path");
            if (IsDisposed) throw new ObjectDisposedException("ZenAudioSpectrogram");
            if (currentCancellation != null) currentCancellation.Cancel();
            CancellationTokenSource cts = new CancellationTokenSource(); currentCancellation = cts;
            int myGeneration = Interlocked.Increment(ref generation); status.Text = "Analyzing the WAV…";
            try
            {
                Color loadLow = lowColor, loadHigh = highColor, loadPeak = peakColor;
                WaveData data = await Task.Run(delegate { return Analyze(path, fftSize, hopSize, loadLow, loadHigh, loadPeak, cts.Token); }, cts.Token);
                if (IsDisposed || myGeneration != generation || cts.IsCancellationRequested) return;
                Bitmap old = spectrogram; spectrogram = data.Image; duration = data.Duration; sampleRate = data.SampleRate; channels = data.Channels; cursor = 0; selectionStart = selectionEnd = -1;
                if (old != null) old.Dispose();
                status.Text = Path.GetFileName(path) + "   •   " + sampleRate.ToString("N0") + " Hz   •   " + channels + " ch   •   " + TimeSpan.FromSeconds(duration).ToString(@"mm\:ss"); plot.Invalidate();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed && myGeneration == generation) { Bitmap old = spectrogram; spectrogram = null; if (old != null) old.Dispose(); duration = 0; cursor = -1; status.Text = "Wasn't possible analyze the WAV"; plot.Invalidate(); EventHandler<ZenAudioSpectrogramErrorEventArgs> h = LoadError; if (h != null) h(this, new ZenAudioSpectrogramErrorEventArgs(ex.Message)); }
            }
            finally { if (currentCancellation == cts) currentCancellation = null; cts.Dispose(); }
        }

        private void OpenFile(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "WAV|*.wav|All Files|*.*", CheckFileExists = true })
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { Task ignored = LoadWaveAsync(dialog.FileName); }
        }

        private sealed class WaveData { public Bitmap Image; public double Duration; public int SampleRate, Channels; }
        private sealed class WaveFormat { public ushort Tag, Channels, Bits, Align; public uint Rate; }

        private static WaveData Analyze(string path, int size, int hop, Color lowColor, Color highColor, Color peakColor, CancellationToken token)
        {
            float[] samples; int rate, channelCount;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (stream.Length < 12 || ReadFourCC(reader) != "RIFF") throw new InvalidDataException("Incomplete or invalid RIFF header.");
                uint riffSize = reader.ReadUInt32(); if (ReadFourCC(reader) != "WAVE") throw new InvalidDataException("RIFF file has not WAVE format.");
                long riffEnd = Math.Min(stream.Length, 8L + riffSize); if (riffEnd < 12) throw new InvalidDataException("Invalid RIFF size.");
                WaveFormat fmt = null; long dataOffset = -1, dataLength = 0;
                while (stream.Position + 8 <= riffEnd)
                {
                    token.ThrowIfCancellationRequested(); string id = ReadFourCC(reader); uint chunkSize = reader.ReadUInt32(); long start = stream.Position;
                    long end; try { end = checked(start + (long)chunkSize); } catch (OverflowException) { throw new InvalidDataException("Invalid chunk WAV size."); }
                    if (end > riffEnd || end > stream.Length) throw new InvalidDataException("Chunk WAV truncated or out of RIFF bounds.");
                    if (id == "fmt ")
                    {
                        if (chunkSize < 16) throw new InvalidDataException("Invalid Chunk fmt (min. 16 bytes).");
                        WaveFormat f = new WaveFormat(); f.Tag = reader.ReadUInt16(); f.Channels = reader.ReadUInt16(); f.Rate = reader.ReadUInt32(); uint byteRate = reader.ReadUInt32(); f.Align = reader.ReadUInt16(); f.Bits = reader.ReadUInt16();
                        if (f.Tag == 0xFFFE)
                        {
                            if (chunkSize < 40) throw new InvalidDataException("Chunk WAVE_FORMAT_EXTENSIBLE incompleto.");
                            ushort extra = reader.ReadUInt16(); if (extra < 22) throw new InvalidDataException("Invalid WAV extension."); reader.ReadUInt16(); reader.ReadUInt32(); byte[] guid = reader.ReadBytes(16);
                            if (guid.Length != 16) throw new InvalidDataException("Extensible subformat incomplete.");
                            byte[] pcmGuid = new byte[] { 1, 0, 0, 0, 0, 0, 16, 0, 128, 0, 0, 170, 0, 56, 155, 113 };
                            byte[] floatGuid = new byte[] { 3, 0, 0, 0, 0, 0, 16, 0, 128, 0, 0, 170, 0, 56, 155, 113 };
                            if (EqualBytes(guid, pcmGuid)) f.Tag = 1; else if (EqualBytes(guid, floatGuid)) f.Tag = 3; else throw new NotSupportedException("Extensible WAV subformat unsupported.");
                        }
                        ValidateFormat(f, byteRate); fmt = f;
                    }
                    else if (id == "data" && dataOffset < 0) { dataOffset = start; dataLength = chunkSize; }
                    long next = end + (chunkSize & 1); if (next > riffEnd) { if (end == riffEnd) stream.Position = end; else throw new InvalidDataException("Padding of chunk WAV is out of bounds."); } else stream.Position = next;
                }
                if (fmt == null) throw new InvalidDataException("Chunk fmt wasn't found."); if (dataOffset < 0) throw new InvalidDataException("Chunk data wasn't found.");
                if (dataLength > MaxAudioBytes) throw new InvalidDataException("This audio axceds the bound: 256 MiB.");
                long frames = dataLength / fmt.Align; if (dataLength % fmt.Align != 0) throw new InvalidDataException("Chunk data has not a integer number of frames.");
                if (frames < 1 || frames > MaxFrames || frames > (long)fmt.Rate * 1800L) throw new InvalidDataException("Empty audio or more biggest than 30 minutes.");
                samples = new float[(int)frames]; stream.Position = dataOffset;
                for (int i = 0; i < samples.Length; i++)
                {
                    if ((i & 16383) == 0) token.ThrowIfCancellationRequested(); double sum = 0;
                    for (int ch = 0; ch < fmt.Channels; ch++) sum += ReadSample(reader, fmt);
                    double value = sum / fmt.Channels; samples[i] = double.IsNaN(value) || double.IsInfinity(value) ? 0f : (float)Math.Max(-1, Math.Min(1, value));
                }
                rate = (int)fmt.Rate; channelCount = fmt.Channels;
            }
            int bins = size / 2; long frameCount = samples.Length <= size ? 1 : 1L + (samples.Length - size) / hop;
            int columns = (int)Math.Min(Math.Min(MaxColumns, MaxImagePixels / bins), frameCount); Bitmap image = new Bitmap(columns, bins);
            try
            {
                double[] real = new double[size], imaginary = new double[size]; double[] hann = new double[size];
                for (int i = 0; i < size; i++) hann[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)));
                for (int x = 0; x < columns; x++)
                {
                    if ((x & 31) == 0) token.ThrowIfCancellationRequested(); long frame = columns == frameCount ? x : (long)x * (frameCount - 1) / Math.Max(1, columns - 1); long offset = frame * hop;
                    for (int i = 0; i < size; i++) { long index = offset + i; real[i] = index < samples.Length ? samples[(int)index] * hann[i] : 0; imaginary[i] = 0; }
                    Fft(real, imaginary);
                    for (int y = 0; y < bins; y++)
                    {
                        double magnitude = Math.Sqrt(real[y] * real[y] + imaginary[y] * imaginary[y]) / size;
                        double db = 20 * Math.Log10(Math.Max(1e-7, magnitude)); double level = Math.Max(0, Math.Min(1, (db + 90) / 90.0));
                        Color c = level < 0.55 ? Blend(lowColor, highColor, level / 0.55) : Blend(highColor, peakColor, (level - 0.55) / 0.45);
                        image.SetPixel(x, bins - 1 - y, c);
                    }
                }
                return new WaveData { Image = image, Duration = (double)samples.Length / rate, SampleRate = rate, Channels = channelCount };
            }
            catch { image.Dispose(); throw; }
        }

        private static void ValidateFormat(WaveFormat f, uint byteRate)
        {
            if (f.Channels < 1 || f.Channels > 32 || f.Rate < 1 || f.Rate > 384000) throw new InvalidDataException("Chanel numbers or sampling rate are invalids (limits: 1–32 chanels, 1–384 kHz).");
            if (f.Tag != 1 && f.Tag != 3) throw new NotSupportedException("WAV format wasn't supported; use integral PCM or IEEE-float.");
            if ((f.Tag == 1 && f.Bits != 8 && f.Bits != 16 && f.Bits != 24 && f.Bits != 32) || (f.Tag == 3 && f.Bits != 32 && f.Bits != 64)) throw new NotSupportedException("Accepted PCM: 8/16/24/32-bit; IEEE-float accepted: 32/64-bit.");
            int expected = f.Channels * (f.Bits / 8); if (f.Align != expected || byteRate != f.Rate * (uint)f.Align) throw new InvalidDataException("Block-align or byte-rate incompatible with WAV format.");
        }
        private static string ReadFourCC(BinaryReader r) { byte[] b = r.ReadBytes(4); if (b.Length != 4) throw new InvalidDataException("FourCC WAV truncated."); return System.Text.Encoding.ASCII.GetString(b); }
        private static bool EqualBytes(byte[] a, byte[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
        private static double ReadSample(BinaryReader r, WaveFormat f)
        {
            if (f.Tag == 3) return f.Bits == 32 ? r.ReadSingle() : r.ReadDouble();
            switch (f.Bits)
            {
                case 8: return (r.ReadByte() - 128) / 128.0;
                case 16: return r.ReadInt16() / 32768.0;
                case 24: { int v = r.ReadByte() | (r.ReadByte() << 8) | (r.ReadByte() << 16); if ((v & 0x800000) != 0) v |= unchecked((int)0xFF000000); return v / 8388608.0; }
                default: return r.ReadInt32() / 2147483648.0;
            }
        }
        private static void Fft(double[] real, double[] imag)
        {
            int n = real.Length, j = 0;
            for (int i = 1; i < n; i++) { int bit = n >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit; if (i < j) { double t = real[i]; real[i] = real[j]; real[j] = t; t = imag[i]; imag[i] = imag[j]; imag[j] = t; } }
            for (int length = 2; length <= n; length <<= 1)
            {
                double angle = -2 * Math.PI / length, wr0 = Math.Cos(angle), wi0 = Math.Sin(angle);
                for (int start = 0; start < n; start += length) { double wr = 1, wi = 0; int half = length >> 1; for (int k = 0; k < half; k++) { int even = start + k, odd = even + half; double tr = wr * real[odd] - wi * imag[odd], ti = wr * imag[odd] + wi * real[odd]; real[odd] = real[even] - tr; imag[odd] = imag[even] - ti; real[even] += tr; imag[even] += ti; double next = wr * wr0 - wi * wi0; wi = wr * wi0 + wi * wr0; wr = next; } }
            }
        }
        private static Color Blend(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }

        private void PaintPlot(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.None; g.Clear(plot.BackColor);
            Rectangle area = new Rectangle(54, 8, Math.Max(1, plot.ClientSize.Width - 66), Math.Max(1, plot.ClientSize.Height - 34));
            if (spectrogram == null) { TextRenderer.DrawText(g, "The spectrogram will be renderized here", Font, plot.ClientRectangle, ZenTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
            g.DrawImage(spectrogram, area);
            using (Pen grid = new Pen(Color.FromArgb(70, ZenTheme.Muted))) using (Brush label = new SolidBrush(ZenTheme.Muted)) using (Font axis = new Font("Segoe UI", 8f))
            {
                for (int i = 0; i <= 4; i++) { int y = area.Bottom - i * area.Height / 4; g.DrawLine(grid, area.Left, y, area.Right, y); string hz = (sampleRate * 0.5 * i / 4.0 >= 1000 ? (sampleRate * 0.5 * i / 4000.0).ToString("0.#") + " kHz" : (sampleRate * 0.5 * i / 4.0).ToString("0") + " Hz"); TextRenderer.DrawText(g, hz, axis, new Rectangle(0, y - 8, 50, 16), ZenTheme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter); }
                for (int i = 0; i <= 4; i++) { int x = area.Left + i * area.Width / 4; g.DrawLine(grid, x, area.Top, x, area.Bottom); string sec = (duration * i / 4.0).ToString("0.0") + "s"; TextRenderer.DrawText(g, sec, axis, new Rectangle(x - 24, area.Bottom + 2, 48, 18), ZenTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
            }
            if (selectionStart >= 0) { int x1 = area.Left + (int)(area.Width * selectionStart / duration), x2 = area.Left + (int)(area.Width * selectionEnd / duration); using (Brush shade = new SolidBrush(Color.FromArgb(55, ZenTheme.Accent))) g.FillRectangle(shade, Math.Min(x1, x2), area.Top, Math.Max(1, Math.Abs(x2 - x1)), area.Height); }
            if (cursor >= 0) { int x = area.Left + (int)(area.Width * cursor / duration); using (Pen p = new Pen(Color.White, 1.5f)) g.DrawLine(p, x, area.Top, x, area.Bottom); }
        }
        private void PlotMouseDown(object sender, MouseEventArgs e) { if (duration <= 0) return; selectionStart = selectionEnd = XToSeconds(e.X); cursor = selectionEnd; plot.Capture = true; plot.Invalidate(); }
        private void PlotMouseMove(object sender, MouseEventArgs e) { if (plot.Capture && duration > 0) { selectionEnd = XToSeconds(e.X); cursor = selectionEnd; plot.Invalidate(); } }
        private void PlotMouseUp(object sender, MouseEventArgs e) { if (duration <= 0 || !plot.Capture) return; selectionEnd = XToSeconds(e.X); cursor = selectionEnd; plot.Capture = false; plot.Invalidate(); EventHandler<ZenAudioSelectionEventArgs> h = SelectionChanged; if (h != null) h(this, new ZenAudioSelectionEventArgs(Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd))); }
        private double XToSeconds(int x) { int left = 54, width = Math.Max(1, plot.ClientSize.Width - 66); return Math.Max(0, Math.Min(duration, (double)(x - left) / width * duration)); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { generation++; if (currentCancellation != null) { currentCancellation.Cancel(); currentCancellation.Dispose(); currentCancellation = null; } if (spectrogram != null) { spectrogram.Dispose(); spectrogram = null; } }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake