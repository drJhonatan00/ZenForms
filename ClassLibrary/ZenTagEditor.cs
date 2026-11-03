using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{

    public sealed class TagEventArgs : EventArgs
    {
        public TagEventArgs(string tag)
        {
            Tag = tag ?? string.Empty;
        }

        public string Tag { get; private set; }
    }

    [ToolboxBitmap(typeof(TextBox))]
    [Category("ZenForms Modern Components")]
    [Description("Dark neon chip editor with keyboard tag entry and removable tags.")]
    public class ZenTagEditor : Control
    {
        private readonly TextBox _editor;
        private readonly List<string> _tags = new List<string>();
        private readonly List<Rectangle> _chipBounds = new List<Rectangle>();
        private readonly List<Rectangle> _removeBounds = new List<Rectangle>();
        private bool _preventDuplicates = true;
        private int _maximumTags = -1;
        private int _cornerRadius = 10;
        private int _chipRadius = 8;
        private Color _chipColor = ZenTheme.SurfaceRaised;
        private Color _chipHoverColor = Color.FromArgb(48, 59, 88);
        private int _hoveredChip = -1;
        private int _hoveredRemove = -1;

        public ZenTagEditor()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(300, 48);
            MinimumSize = new Size(80, 34);
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            TabStop = true;
            AccessibleName = "Tag editor";

            _editor = new TextBox();
            _editor.BorderStyle = BorderStyle.None;
            _editor.BackColor = ZenTheme.Surface;
            _editor.ForeColor = ZenTheme.Foreground;
            _editor.Font = Font;
            _editor.Multiline = false;
            _editor.AcceptsReturn = false;
            _editor.TabStop = true;
            _editor.KeyDown += EditorKeyDown;
            _editor.TextChanged += EditorTextChanged;
            _editor.Enter += EditorEnter;
            _editor.Leave += EditorLeave;
            Controls.Add(_editor);
        }

        [Category("ZenForms Data")]
        [Description("The current tags. Use AddTag, RemoveTag or ClearTags to change the collection.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<string> Tags
        {
            get { return _tags; }
        }

        [Category("ZenForms Behavior")]
        [DefaultValue(true)]
        [Description("When true, a tag with the same text cannot be added twice.")]
        public bool PreventDuplicates
        {
            get { return _preventDuplicates; }
            set { _preventDuplicates = value; }
        }

        [Category("ZenForms Behavior")]
        [DefaultValue(-1)]
        [Description("Maximum number of tags; use -1 for no limit.")]
        public int MaximumTags
        {
            get { return _maximumTags; }
            set { _maximumTags = value < -1 ? -1 : value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [DefaultValue(10)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [DefaultValue(8)]
        public int ChipRadius
        {
            get { return _chipRadius; }
            set { _chipRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color ChipColor
        {
            get { return _chipColor; }
            set { _chipColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        public Color ChipHoverColor
        {
            get { return _chipHoverColor; }
            set { _chipHoverColor = value; Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Raised when a tag is successfully added.")]
        public event EventHandler<TagEventArgs> TagAdded;

        [Category("ZenForms Appearance")]
        [Description("Raised when a tag is successfully removed.")]
        public event EventHandler<TagEventArgs> TagRemoved;

        public bool AddTag(string tag)
        {
            tag = (tag ?? string.Empty).Trim();
            if (tag.Length == 0 || (_maximumTags >= 0 && _tags.Count >= _maximumTags))
                return false;
            if (_preventDuplicates && ContainsTag(tag))
                return false;

            _tags.Add(tag);
            _editor.Clear();
            Invalidate();
            PerformLayout();
            OnTagAdded(new TagEventArgs(tag));
            return true;
        }

        public bool RemoveTag(string tag)
        {
            if (tag == null)
                return false;
            int index = _tags.IndexOf(tag.Trim());
            if (index < 0)
                return false;
            RemoveTagAt(index);
            return true;
        }

        public void ClearTags()
        {
            if (_tags.Count == 0)
                return;
            string[] oldTags = _tags.ToArray();
            _tags.Clear();
            _hoveredChip = -1;
            _hoveredRemove = -1;
            _editor.Clear();
            Invalidate();
            PerformLayout();
            for (int i = 0; i < oldTags.Length; i++)
                OnTagRemoved(new TagEventArgs(oldTags[i]));
        }

        protected virtual void OnTagAdded(TagEventArgs e)
        {
            EventHandler<TagEventArgs> handler = TagAdded;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnTagRemoved(TagEventArgs e)
        {
            EventHandler<TagEventArgs> handler = TagRemoved;
            if (handler != null)
                handler(this, e);
        }

        private bool ContainsTag(string tag)
        {
            for (int i = 0; i < _tags.Count; i++)
                if (string.Equals(_tags[i], tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void RemoveTagAt(int index)
        {
            if (index < 0 || index >= _tags.Count)
                return;
            string tag = _tags[index];
            _tags.RemoveAt(index);
            _hoveredChip = -1;
            _hoveredRemove = -1;
            Invalidate();
            PerformLayout();
            OnTagRemoved(new TagEventArgs(tag));
        }

        private void EditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddTag(_editor.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Back && _editor.TextLength == 0 && _tags.Count > 0)
            {
                RemoveTagAt(_tags.Count - 1);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void EditorTextChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void EditorEnter(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void EditorLeave(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_editor != null)
                _editor.Font = Font;
            PerformLayout();
            Invalidate();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (_editor != null)
                _editor.ForeColor = ForeColor;
            Invalidate();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_editor != null)
                _editor.BackColor = BackColor;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutEditor();
        }

        private void LayoutEditor()
        {
            if (_editor == null)
                return;
            int left = 12;
            int top = Math.Max(4, (Height - _editor.PreferredHeight) / 2);
            int right = Math.Max(left + 2, Width - 12);
            for (int i = 0; i < _tags.Count; i++)
            {
                Size chip = MeasureChip(_tags[i]);
                left += chip.Width + 6;
                if (left >= right - 24)
                    break;
            }
            int width = Math.Max(2, right - left);
            _editor.SetBounds(left, top, width, Math.Max(18, _editor.PreferredHeight));
        }

        private Size MeasureChip(string tag)
        {
            Size text = TextRenderer.MeasureText(tag ?? string.Empty, Font, new Size(1000, 24), TextFormatFlags.NoPadding);
            return new Size(Math.Max(38, text.Width + 31), Math.Max(24, text.Height + 7));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            for (int i = 0; i < _removeBounds.Count; i++)
            {
                if (_removeBounds[i].Contains(e.Location))
                {
                    RemoveTagAt(i);
                    return;
                }
            }
            FocusEditor();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int chip = -1;
            int remove = -1;
            for (int i = 0; i < _chipBounds.Count; i++)
            {
                if (_chipBounds[i].Contains(e.Location))
                    chip = i;
                if (_removeBounds[i].Contains(e.Location))
                    remove = i;
            }
            if (chip != _hoveredChip || remove != _hoveredRemove)
            {
                _hoveredChip = chip;
                _hoveredRemove = remove;
                Cursor = remove >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredChip = -1;
            _hoveredRemove = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        private void FocusEditor()
        {
            if (_editor != null && !_editor.Focused)
                _editor.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle outer = new Rectangle(3, 3, Math.Max(1, Width - 7), Math.Max(1, Height - 7));
            if (outer.Width < 2 || outer.Height < 2)
                return;

            using (GraphicsPath path = ZenButtonBase.RoundPath(outer, _cornerRadius))
            {
                using (SolidBrush fill = new SolidBrush(BackColor))
                    g.FillPath(fill, path);
                Color borderColor = _editor.Focused ? Color.FromArgb(190, ZenTheme.Primary) : ZenTheme.Border;
                using (Pen border = new Pen(borderColor, _editor.Focused ? 1.4f : 1f))
                    g.DrawPath(border, path);
                if (_editor.Focused)
                {
                    using (Pen glow = new Pen(Color.FromArgb(42, ZenTheme.Primary), 4f))
                        g.DrawPath(glow, path);
                }
            }

            _chipBounds.Clear();
            _removeBounds.Clear();
            int x = 10;
            int center = Height / 2;
            for (int i = 0; i < _tags.Count; i++)
            {
                Size size = MeasureChip(_tags[i]);
                Rectangle chip = new Rectangle(x, center - size.Height / 2, size.Width, size.Height);
                Rectangle remove = new Rectangle(chip.Right - 22, chip.Top + 4, 18, chip.Height - 8);
                _chipBounds.Add(chip);
                _removeBounds.Add(remove);
                using (GraphicsPath chipPath = ZenButtonBase.RoundPath(chip, _chipRadius))
                {
                    Color chipFill = i == _hoveredChip ? _chipHoverColor : _chipColor;
                    using (SolidBrush fill = new SolidBrush(chipFill))
                        g.FillPath(fill, chipPath);
                    using (Pen border = new Pen(Color.FromArgb(i == _hoveredChip ? 150 : 90, ZenTheme.Accent), 1f))
                        g.DrawPath(border, chipPath);
                }
                Rectangle textRect = new Rectangle(chip.Left + 10, chip.Top, Math.Max(1, chip.Width - 31), chip.Height);
                TextRenderer.DrawText(g, _tags[i], Font, textRect, ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                Color closeColor = i == _hoveredRemove ? ZenTheme.Danger : ZenTheme.Muted;
                using (Pen close = new Pen(closeColor, 1.5f))
                {
                    g.DrawLine(close, remove.Left + 6, remove.Top + 5, remove.Right - 5, remove.Bottom - 5);
                    g.DrawLine(close, remove.Right - 5, remove.Top + 5, remove.Left + 6, remove.Bottom - 5);
                }
                x += size.Width + 6;
                if (x >= Width - 18)
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _editor != null)
            {
                _editor.KeyDown -= EditorKeyDown;
                _editor.TextChanged -= EditorTextChanged;
                _editor.Enter -= EditorEnter;
                _editor.Leave -= EditorLeave;
            }
            base.Dispose(disposing);
        }
    }
}

// watashi wa watashi sore dake