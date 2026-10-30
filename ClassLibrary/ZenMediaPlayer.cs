using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Panel))]
    [Category("ZenForms Modern Controls")]
    [Description("A Media Player with vibrations chart.")]
    public class ZenMediaPlayer : UserControl
    {
        private const string WindowsMediaPlayerClsid = "6BF52A52-394A-11d3-B153-00C04F79FAA6";
        private readonly MediaActiveXHost mediaHost;
        private readonly Panel display;
        private readonly Panel toolbar;
        private readonly Label titleLabel;
        private readonly Label timeLabel;
        private readonly TrackBar seekBar;
        private readonly TrackBar volumeBar;
        private readonly Timer timer;
        private dynamic player;
        private bool seeking;
        private bool initialized;
        private int lastPlayState = -1;
        private string mediaFile = String.Empty;

        public event EventHandler MediaOpened;
        public event EventHandler MediaEnded;
        public event EventHandler<MediaPlayerErrorEventArgs> MediaError;

        [Category("ZenForms Media"), Browsable(false)]
        public string FileName { get { return mediaFile; } }

        [Category("ZenForms Media"), DefaultValue(70)]
        public int Volume
        {
            get { return volumeBar.Value; }
            set
            {
                int safeValue = Math.Max(0, Math.Min(100, value));
                volumeBar.Value = safeValue;
                SetVolume(safeValue);
            }
        }

        [Category("ZenForms Media"), DefaultValue(false)]
        public bool FullScreen
        {
            get { try { return player != null && Convert.ToBoolean(player.fullScreen); } catch { return false; } }
            set { try { if (player != null) player.fullScreen = value; } catch (Exception ex) { RaiseError("Unable to switch to Full Screen.", ex); } }
        }

        public ZenMediaPlayer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            MinimumSize = new Size(360, 250);
            Size = new Size(640, 400);

            display = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, MinimumSize = new Size(0, 120) };
            mediaHost = new MediaActiveXHost(WindowsMediaPlayerClsid) { Dock = DockStyle.Fill, BackColor = Color.Black };
            display.Controls.Add(mediaHost);

            toolbar = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = ZenTheme.Surface, Padding = new Padding(10, 6, 10, 5) };
            titleLabel = new Label { Dock = DockStyle.Top, Height = 22, Text = "No Media Selected", ForeColor = ZenTheme.Muted, Font = ZenTheme.DefaultFont, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            timeLabel = new Label { Dock = DockStyle.Right, Width = 98, Text = "00:00 / 00:00", ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleRight };
            seekBar = new TrackBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000, TickStyle = TickStyle.None, AutoSize = false, Height = 28, BackColor = ZenTheme.Surface, Enabled = false };
            volumeBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 70, TickStyle = TickStyle.None, AutoSize = false, Width = 92, Height = 28, BackColor = ZenTheme.Surface, AccessibleName = "Volume" };

            var seekRow = new Panel { Dock = DockStyle.Top, Height = 29 };
            seekRow.Controls.Add(seekBar);
            seekRow.Controls.Add(timeLabel);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = ZenTheme.Surface, Padding = new Padding(0, 4, 0, 0) };
            var openButton = CreateButton("Open", 68);
            var playButton = CreateButton("Play", 100);
            var pauseButton = CreateButton("Pause", 76);
            var stopButton = CreateButton("Stop", 64);
            var volumeLabel = new Label { Text = "Volume", ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8f), Width = 48, Height = 34, TextAlign = ContentAlignment.MiddleRight, Margin = new Padding(6, 2, 2, 0) };
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(playButton);
            buttons.Controls.Add(pauseButton);
            buttons.Controls.Add(stopButton);
            buttons.Controls.Add(volumeLabel);
            buttons.Controls.Add(volumeBar);

            toolbar.Controls.Add(buttons);
            toolbar.Controls.Add(seekRow);
            toolbar.Controls.Add(titleLabel);
            Controls.Add(display);
            Controls.Add(toolbar);

            openButton.Click += delegate { OpenFileDialogAndPlay(); };
            playButton.Click += delegate { Play(); };
            pauseButton.Click += delegate { Pause(); };
            stopButton.Click += delegate { Stop(); };
            volumeBar.Scroll += delegate { SetVolume(volumeBar.Value); };
            seekBar.MouseDown += delegate { seeking = true; };
            seekBar.MouseUp += delegate { SeekToBarPosition(); seeking = false; };
            seekBar.Scroll += delegate { if (seeking) UpdateTimeLabelFromBar(); };

            timer = new Timer { Interval = 400 };
            timer.Tick += delegate { UpdatePlaybackState(); };
            mediaHost.HandleCreated += delegate { InitializePlayer(); };
            mediaHost.HandleDestroyed += delegate { initialized = false; player = null; };
            Disposed += delegate { timer.Stop(); timer.Dispose(); };
            timer.Start();
        }

        public void Open(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter the file path.", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("The media file wasn't found.", path);
            if (!EnsurePlayer()) return;
            try
            {
                mediaFile = Path.GetFullPath(path);
                titleLabel.Text = Path.GetFileName(mediaFile);
                seekBar.Enabled = true;
                player.uiMode = "none";
                player.enableContextMenu = false;
                player.stretchToFit = true;
                player.URL = mediaFile;
                lastPlayState = -1;
                player.controls.play();
                SetVolume(volumeBar.Value);
                RaiseEvent(MediaOpened);
            }
            catch (Exception ex) { RaiseError("Unable to open this file. Check that Windows Media Player and the necessary codec are available.", ex); }
        }

        public void Play()
        {
            if (String.IsNullOrEmpty(mediaFile)) { OpenFileDialogAndPlay(); return; }
            if (EnsurePlayer()) TryPlayer(delegate { player.controls.play(); });
        }

        public void Pause()
        {
            if (EnsurePlayer()) TryPlayer(delegate { player.controls.pause(); });
        }

        public void Stop()
        {
            if (EnsurePlayer()) TryPlayer(delegate { player.controls.stop(); });
        }

        public void OpenFileDialogAndPlay()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Open Media File";
                dialog.Filter = "Media File|*.mp3;*.mp4;*.wma;*.wav;*.m4a;*.wmv;*.avi|Audio MP3|*.mp3|Video MP4|*.mp4|All Files|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) Open(dialog.FileName);
            }
        }

        private static ZenButtonBase CreateButton(string text, int width)
        {
            return new ZenButtonBase { Text = text, Width = width, Height = 32, Margin = new Padding(2, 0, 3, 0), CornerRadius = 7, Font = new Font("Segoe UI", 8.5f) };
        }

        private bool EnsurePlayer()
        {
            if (!mediaHost.IsHandleCreated)
            {
                try { mediaHost.CreateControl(); } catch (Exception ex) { RaiseError("Unable to activate the Windows Media Player ActiveX component.", ex); return false; }
            }
            if (!initialized) InitializePlayer();
            if (initialized && player != null) return true;
            RaiseError("The Windows Media Player wasn't found in the Windows.", null);
            return false;
        }

        private void InitializePlayer()
        {
            try
            {
                if (!mediaHost.IsHandleCreated) return;
                player = mediaHost.GetPlayerObject();
                player.uiMode = "none";
                player.enableContextMenu = false;
                player.stretchToFit = true;
                player.settings.autoStart = false;
                player.settings.volume = volumeBar.Value;
                initialized = true;
                titleLabel.Text = "Any media selected";
            }
            catch (Exception ex)
            {
                initialized = false;
                player = null;
                RaiseError("Failed to start Windows Media Player. Confirm that it's correctly enabled in Windows.", ex);
            }
        }

        private void SetVolume(int value)
        {
            if (initialized && player != null) TryPlayer(delegate { player.settings.volume = value; });
        }

        private void SeekToBarPosition()
        {
            if (!initialized || player == null) return;
            try
            {
                double duration = Convert.ToDouble(player.currentMedia.duration);
                if (duration > 0) player.controls.currentPosition = duration * seekBar.Value / seekBar.Maximum;
            }
            catch (Exception ex) { RaiseError("Unable to change the playback position.", ex); }
        }

        private void UpdateTimeLabelFromBar()
        {
            try
            {
                double duration = Convert.ToDouble(player.currentMedia.duration);
                double position = duration * seekBar.Value / seekBar.Maximum;
                timeLabel.Text = FormatTime(position) + " / " + FormatTime(duration);
            }
            catch { }
        }

        private void UpdatePlaybackState()
        {
            if (!initialized || player == null || String.IsNullOrEmpty(mediaFile)) return;
            try
            {
                double duration = Convert.ToDouble(player.currentMedia.duration);
                double position = Convert.ToDouble(player.controls.currentPosition);
                if (!seeking)
                {
                    seekBar.Enabled = duration > 0;
                    seekBar.Value = duration > 0 ? Math.Max(0, Math.Min(seekBar.Maximum, (int)(position / duration * seekBar.Maximum))) : 0;
                    timeLabel.Text = FormatTime(position) + " / " + FormatTime(duration);
                }
                int state = Convert.ToInt32(player.playState);
                if (state == 8 && lastPlayState != 8) RaiseEvent(MediaEnded);
                lastPlayState = state;
            }
            catch { /* Moshika shitara nanika jūyōna kotona no kamo shirenai */ }
        }

        private static string FormatTime(double seconds)
        {
            if (Double.IsNaN(seconds) || Double.IsInfinity(seconds) || seconds < 0) seconds = 0;
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
        }

        private void TryPlayer(Action action)
        {
            try { action(); }
            catch (Exception ex) { RaiseError("The selected action could not be performed.", ex); }
        }

        private void RaiseEvent(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void RaiseError(string message, Exception exception)
        {
            EventHandler<MediaPlayerErrorEventArgs> handler = MediaError;
            if (handler != null) handler(this, new MediaPlayerErrorEventArgs(message, exception));
            titleLabel.Text = message;
        }

        private sealed class MediaActiveXHost : AxHost
        {
            public MediaActiveXHost(string clsid) : base(clsid) { }
            public object GetPlayerObject() { return GetOcx(); }
        }


    }

    public sealed class MediaPlayerErrorEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public MediaPlayerErrorEventArgs(string message, Exception exception) { Message = message; Exception = exception; }
    }
}

//watashi wa watashi sore dake
