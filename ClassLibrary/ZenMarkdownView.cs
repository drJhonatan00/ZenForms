using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace ZenForms.Core
{
    [Category("ZenForms Modern Components")]
    [Description("MD files viewer.")]
    public class ZenMarkdownView : UserControl
    {
        private readonly RichTextBox preview; private string markdown = String.Empty; private Color accent = ZenTheme.Primary;
        [Category("ZenForms Markdown"), DefaultValue("")] public string Markdown { get { return markdown; } set { markdown = value ?? String.Empty; RenderMarkdown(); } }
        [Category("ZenForms Appearance")] public Color AccentColor { get { return accent; } set { accent = value; RenderMarkdown(); } }
        [Browsable(false)] public RichTextBox PreviewControl { get { return preview; } }
        public ZenMarkdownView() { Size = new Size(560, 420); BackColor = ZenTheme.Background; preview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = ZenTheme.Background, ForeColor = ZenTheme.Foreground, Font = new Font("Segoe UI", 10f), DetectUrls = true, ScrollBars = RichTextBoxScrollBars.Vertical, HideSelection = false }; Controls.Add(preview); }
        public void SetMarkdown(string text) { Markdown = text; }
        public void Clear() { Markdown = String.Empty; }
        private void RenderMarkdown() { if (preview == null) return; preview.SuspendLayout(); preview.Clear(); string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'); bool code = false; for (int i = 0; i < lines.Length; i++) { string line = lines[i]; if (line.TrimStart().StartsWith("```")) { code = !code; continue; } int start = preview.TextLength; string rendered = line; FontStyle style = FontStyle.Regular; float size = 10f; Color color = ZenTheme.Foreground; if (code) { style = FontStyle.Regular; color = ZenTheme.Accent; size = 9.5f; } else if (line.StartsWith("### ")) { rendered = line.Substring(4); style = FontStyle.Bold; size = 12f; color = accent; } else if (line.StartsWith("## ")) { rendered = line.Substring(3); style = FontStyle.Bold; size = 15f; color = accent; } else if (line.StartsWith("# ")) { rendered = line.Substring(2); style = FontStyle.Bold; size = 19f; color = accent; } else if (line.StartsWith("> ")) { rendered = "  │  " + line.Substring(2); color = ZenTheme.Muted; style = FontStyle.Italic; } else if (line.StartsWith("- ") || line.StartsWith("* ")) { rendered = "  •  " + line.Substring(2); } else if (Regex.IsMatch(line, "^\\d+\\. ")) { rendered = "  " + line; } if (!code) rendered = Regex.Replace(rendered, "\\*\\*(.+?)\\*\\*", "$1"); preview.AppendText(rendered + Environment.NewLine); int length = preview.TextLength - start; if (length > 0) { preview.Select(start, length); preview.SelectionColor = color; using (Font f = new Font("Segoe UI", size, style)) preview.SelectionFont = f; if (!code && !line.StartsWith("#")) ApplyInlineBold(start, rendered); if (code) preview.SelectionBackColor = Color.FromArgb(24, ZenTheme.Accent); } } preview.Select(0, 0); preview.SelectionColor = ZenTheme.Foreground; preview.ResumeLayout(); }
        private void ApplyInlineBold(int start, string text) { MatchCollection matches = Regex.Matches(text, "\\*\\*(.+?)\\*\\*"); foreach (Match m in matches) { int a = start + m.Index; int len = m.Length - 4; if (len <= 0) continue; preview.Select(a, len); using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold)) preview.SelectionFont = f; preview.SelectionColor = ZenTheme.Foreground; } }
    }
}

// watashi wa watashi sore dake
