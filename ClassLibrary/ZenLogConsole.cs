using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public enum ZenLogLevel { Trace, Information, Success, Warning, Error }
    public sealed class ZenLogEntry : EventArgs { public DateTime Timestamp { get; private set; } public ZenLogLevel Level { get; private set; } public string Message { get; private set; } internal ZenLogEntry(ZenLogLevel level, string message) { Timestamp = DateTime.Now; Level = level; Message = message; } }
    [Category("ZenForms Modern Components")]
    [Description("Application console with color-coded logs, search funcionality, and a configurable history limit..")]
    public class ZenLogConsole : UserControl
    {
        private readonly TextBox filter; private readonly RichTextBox output; private readonly List<ZenLogEntry> entries = new List<ZenLogEntry>(); private int maxEntries = 3000; private bool autoScroll = true; private bool showTimestamp = true;
        [Category("ZenForms Logs"), DefaultValue(3000)] public int MaxEntries { get { return maxEntries; } set { maxEntries = Math.Max(10, value); Trim(); Render(); } }
        [Category("ZenForms Logs"), DefaultValue(true)] public bool AutoScroll { get { return autoScroll; } set { autoScroll = value; } }
        [Category("ZenForms Logs"), DefaultValue(true)] public bool ShowTimestamp { get { return showTimestamp; } set { showTimestamp = value; Render(); } }
        [Browsable(false)] public IList<ZenLogEntry> Entries { get { return entries.AsReadOnly(); } }
        public event EventHandler<ZenLogEntry> EntryAdded;
        public ZenLogConsole() { Size = new Size(640, 320); BackColor = ZenTheme.Background; Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = ZenTheme.Surface, Padding = new Padding(7, 5, 7, 4) }; filter = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont }; filter.TextChanged += (s, e) => Render(); Button clear = new Button { Dock = DockStyle.Right, Width = 65, Text = "Clear", FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground }; clear.Click += (s, e) => Clear(); toolbar.Controls.Add(filter); toolbar.Controls.Add(clear); output = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(12, 16, 27), ForeColor = ZenTheme.Foreground, Font = new Font("Consolas", 9f), DetectUrls = false, WordWrap = false }; Controls.Add(output); Controls.Add(toolbar); }
        public void Write(string message) { Write(ZenLogLevel.Information, message); }
        public void Write(ZenLogLevel level, string message) { if (InvokeRequired) { BeginInvoke(new Action<ZenLogLevel, string>(Write), level, message); return; } ZenLogEntry entry = new ZenLogEntry(level, message ?? String.Empty); entries.Add(entry); Trim(); if (String.IsNullOrEmpty(filter.Text) || entry.Message.IndexOf(filter.Text, StringComparison.OrdinalIgnoreCase) >= 0) Append(entry); EventHandler<ZenLogEntry> h = EntryAdded; if (h != null) h(this, entry); }
        public void Clear() { entries.Clear(); output.Clear(); }
        private void Trim() { if (entries.Count > maxEntries) entries.RemoveRange(0, entries.Count - maxEntries); }
        private void Render() { if (output == null) return; output.SuspendLayout(); output.Clear(); foreach (ZenLogEntry e in entries) if (String.IsNullOrEmpty(filter.Text) || e.Message.IndexOf(filter.Text, StringComparison.OrdinalIgnoreCase) >= 0) Append(e); output.ResumeLayout(); }
        private void Append(ZenLogEntry e) { bool atBottom = output.SelectionStart >= output.TextLength - 2; Color c = e.Level == ZenLogLevel.Error ? ZenTheme.Danger : e.Level == ZenLogLevel.Warning ? Color.FromArgb(255, 190, 70) : e.Level == ZenLogLevel.Success ? ZenTheme.Accent : e.Level == ZenLogLevel.Trace ? ZenTheme.Muted : ZenTheme.Foreground; output.SelectionStart = output.TextLength; output.SelectionLength = 0; output.SelectionColor = ZenTheme.Muted; if (showTimestamp) output.AppendText("[" + e.Timestamp.ToString("HH:mm:ss.fff") + "] "); output.SelectionColor = c; output.AppendText("[" + e.Level.ToString().ToUpperInvariant() + "] "); output.SelectionColor = ZenTheme.Foreground; output.AppendText(e.Message + Environment.NewLine); if (autoScroll && atBottom) { output.SelectionStart = output.TextLength; output.ScrollToCaret(); } }
    }
}

// watashi wa watashi sore dake
