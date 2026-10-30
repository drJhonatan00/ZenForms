using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ZenForms.Core
{
    public sealed class ZenCommandEventArgs : EventArgs { public string Name { get; private set; } public string Shortcut { get; private set; } public ZenCommandEventArgs(string name, string shortcut) { Name = name; Shortcut = shortcut; } }
    [Category("ZenForms Modern Controls")]
    [Description("Searchable command palette.")]
    public class ZenCommandPalette : UserControl
    {
        private sealed class Command { public string Name; public string Shortcut; public Action Execute; public Command(string n, string s, Action a) { Name = n; Shortcut = s; Execute = a; } }
        private readonly TextBox searchBox; private readonly ListBox results; private readonly Label hint; private readonly List<Command> commands = new List<Command>(); private List<Command> filtered = new List<Command>(); private Color accent = ZenTheme.Primary;
        [Category("ZenForms Appearance")] public Color AccentColor { get { return accent; } set { accent = value; Invalidate(); } }
        public event EventHandler<ZenCommandEventArgs> CommandExecuted;
        public ZenCommandPalette() { Size = new Size(520, 330); MinimumSize = new Size(320, 180); BackColor = ZenTheme.Surface; Padding = new Padding(1); SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); searchBox = new TextBox { Dock = DockStyle.Top, Height = 38, BorderStyle = BorderStyle.None, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = new Font("Segoe UI", 12f), Margin = new Padding(8), AccessibleName = "Search commands" }; results = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Foreground, Font = ZenTheme.DefaultFont, IntegralHeight = false, ItemHeight = 28 }; hint = new Label { Dock = DockStyle.Bottom, Height = 24, Text = "↑ ↓ Search     Enter execute     Esc clean", BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8f), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 8, 0) }; Controls.Add(results); Controls.Add(hint); Controls.Add(searchBox); searchBox.TextChanged += (s, e) => Filter(); searchBox.KeyDown += SearchKeyDown; results.DoubleClick += (s, e) => ExecuteSelected(); results.KeyDown += ResultsKeyDown; }
        public void AddCommand(string name, Action action) { AddCommand(name, String.Empty, action); }
        public void AddCommand(string name, string shortcut, Action action) { if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Enter with command name.", "name"); commands.Add(new Command(name, shortcut ?? String.Empty, action)); Filter(); }
        public bool RemoveCommand(string name) { int i = commands.FindIndex(c => String.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)); if (i < 0) return false; commands.RemoveAt(i); Filter(); return true; }
        public void ClearCommands() { commands.Clear(); Filter(); }
        public void Open() { Visible = true; BringToFront(); searchBox.Text = String.Empty; searchBox.Focus(); Filter(); }
        private void Filter() { string q = searchBox == null ? String.Empty : searchBox.Text.Trim(); filtered = commands.Where(c => Matches(c.Name, q) || Matches(c.Shortcut, q)).OrderBy(c => Score(c.Name, q)).ThenBy(c => c.Name).ToList(); if (results == null) return; results.BeginUpdate(); results.Items.Clear(); foreach (Command c in filtered) results.Items.Add(c.Name + (String.IsNullOrEmpty(c.Shortcut) ? String.Empty : "     " + c.Shortcut)); results.EndUpdate(); if (results.Items.Count > 0) results.SelectedIndex = 0; }
        private static bool Matches(string value, string query) { return String.IsNullOrEmpty(query) || value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0; }
        private static int Score(string value, string query) { if (String.IsNullOrEmpty(query)) return 0; if (value.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 0; return value.IndexOf(query, StringComparison.OrdinalIgnoreCase) + 1; }
        private void SearchKeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Down) { if (results.Items.Count > 0) results.SelectedIndex = Math.Min(results.Items.Count - 1, results.SelectedIndex + 1); results.Focus(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Enter) { ExecuteSelected(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { Visible = false; e.SuppressKeyPress = true; } }
        private void ResultsKeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { ExecuteSelected(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { Visible = false; searchBox.Focus(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Up && results.SelectedIndex <= 0) { searchBox.Focus(); e.SuppressKeyPress = true; } }
        private void ExecuteSelected() { if (results.SelectedIndex < 0 || results.SelectedIndex >= filtered.Count) return; Command c = filtered[results.SelectedIndex]; if (c.Execute != null) c.Execute(); EventHandler<ZenCommandEventArgs> h = CommandExecuted; if (h != null) h(this, new ZenCommandEventArgs(c.Name, c.Shortcut)); Visible = false; }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using (Pen p = new Pen(accent, 2f)) e.Graphics.DrawRectangle(p, 1, 1, Width - 3, Height - 3); }
    }
}

// watashi wa watashi sore dake
