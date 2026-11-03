using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(TextBox))]
    [Category("ZenForms Modern Components")]
    [Description("Focusable keyboard shortcut recorder with modifier support and clear action.")]
    public class ZenShortcutRecorder : Control
    {
        private Keys _shortcutKeys;
        private bool _allowModifierOnly;
        private bool _hovered;
        private bool _pressed;
        private bool _clearHovered;
        private bool _clearPressed;
        private bool _modifierSequenceActive;
        private bool _hasCommittedKey;
        private int _cornerRadius = 10;
        private string _emptyText = "Press keys to record";
        private readonly Font _componentFont;
        private readonly ToolTip _toolTip;

        public ZenShortcutRecorder()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);

            Size = new Size(250, 42);
            MinimumSize = new Size(96, 30);
            TabStop = true;
            Cursor = Cursors.IBeam;
            BackColor = ZenTheme.Surface;
            ForeColor = ZenTheme.Foreground;
            _componentFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Font = _componentFont;
            _toolTip = new ToolTip();
            _toolTip.SetToolTip(this, "Clique e pressione uma combinação de teclas");
        }

        [Category("ZenForms Behavior")]
        [Description("The currently recorded keyboard shortcut.")]
        [DefaultValue(Keys.None)]
        public Keys ShortcutKeys
        {
            get { return _shortcutKeys; }
            set { SetShortcut(value); }
        }

        [Category("ZenForms Behavior")]
        [Description("Allows a shortcut made only from modifier keys, such as Ctrl+Shift.")]
        [DefaultValue(false)]
        public bool AllowModifierOnly
        {
            get { return _allowModifierOnly; }
            set
            {
                if (_allowModifierOnly == value) return;
                _allowModifierOnly = value;
                if (!_allowModifierOnly && IsModifierOnly(_shortcutKeys)) SetShortcut(Keys.None);
                Invalidate();
            }
        }

        [Category("ZenForms Appearance")]
        [Description("Corner radius of the recorder surface.")]
        [DefaultValue(10)]
        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZenForms Appearance")]
        [Description("Text displayed when no shortcut has been recorded.")]
        [DefaultValue("Press keys to record")]
        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value ?? String.Empty; Invalidate(); }
        }

        [Browsable(false)]
        public bool HasShortcut { get { return _shortcutKeys != Keys.None; } }

        public event EventHandler ShortcutChanged;

        public void Clear()
        {
            SetShortcut(Keys.None);
            _modifierSequenceActive = false;
            _hasCommittedKey = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _clearHovered = false;
            _pressed = false;
            _clearPressed = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool clearArea = IsClearArea(e.Location);
            if (_clearHovered != clearArea)
            {
                _clearHovered = clearArea;
                Cursor = clearArea ? Cursors.Hand : Cursors.IBeam;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            if (IsClearArea(e.Location) && HasShortcut)
            {
                _clearPressed = true;
            }
            else
            {
                _pressed = true;
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            bool clear = _clearPressed && IsClearArea(e.Location);
            _pressed = false;
            _clearPressed = false;
            if (clear) Clear();
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _pressed = false;
            _clearPressed = false;
            _modifierSequenceActive = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            try
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Escape)
                {
                    Clear();
                    return;
                }

                Keys keyCode = e.KeyCode;
                if (IsModifierKey(keyCode))
                {
                    _modifierSequenceActive = true;
                    if (_allowModifierOnly)
                    {
                        Keys modifiers = NormalizeModifiers(e.Modifiers | ModifierForKey(keyCode));
                        SetShortcut(modifiers);
                    }
                    return;
                }

                Keys recorded = NormalizeModifiers(e.Modifiers) | keyCode;
                if (keyCode != Keys.None)
                {
                    _hasCommittedKey = true;
                    _modifierSequenceActive = true;
                    SetShortcut(recorded);
                }
            }
            catch
            {
                // watashitachiha ahiru yori mo jagaimo no yō ni narubekida. Nazenara, jagaimo wa nani ni demo auga, ahiru wa nandemodekiru ga, dore mo umaku dekinaikarada
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            try
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (IsModifierKey(e.KeyCode) && _modifierSequenceActive && !_hasCommittedKey && _allowModifierOnly)
                {
                    Keys remaining = NormalizeModifiers(e.Modifiers);
                    if (remaining != Keys.None) SetShortcut(remaining);
                }
                if (IsModifierKey(e.KeyCode))
                {
                    _modifierSequenceActive = false;
                    _hasCommittedKey = false;
                }
            }
            catch
            {
                // watashitachiha ahiru yori mo jagaimo no yō ni narubekida. Nazenara, jagaimo wa nani ni demo auga, ahiru wa nandemodekiru ga, dore mo umaku dekinaikarada
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle outer = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
            if (outer.Width < 2 || outer.Height < 2) return;
            int radius = Math.Max(0, CornerRadius);
            using (GraphicsPath path = ZenButtonBase.RoundPath(outer, radius))
            {
                Color surface = _hovered || Focused ? ZenTheme.SurfaceRaised : ZenTheme.Surface;
                using (SolidBrush fill = new SolidBrush(surface)) g.FillPath(fill, path);
                if (Focused || _hovered)
                {
                    using (Pen glow = new Pen(Color.FromArgb(Focused ? 105 : 55, ZenTheme.Primary), 5f)) g.DrawPath(glow, path);
                }
                using (Pen border = new Pen(Color.FromArgb(Focused ? 220 : 125, Focused ? ZenTheme.Accent : ZenTheme.Border), 1f))
                    g.DrawPath(border, path);

                string preview = FormatShortcut(_shortcutKeys);
                if (String.IsNullOrEmpty(preview)) preview = EmptyText ?? "Press keys to record";
                Color previewColor = HasShortcut ? ForeColor : ZenTheme.Muted;
                Rectangle textRect = new Rectangle(16, 5, Math.Max(1, Width - 58), Math.Max(1, Height - 10));
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(g, preview, Font, textRect, previewColor, flags);

                if (HasShortcut)
                {
                    Rectangle clearRect = GetClearRectangle();
                    Color clearColor = _clearHovered ? ZenTheme.Danger : ZenTheme.Muted;
                    using (Pen clearPen = new Pen(clearColor, 1.6f))
                    {
                        g.DrawLine(clearPen, clearRect.Left + 4, clearRect.Top + 4, clearRect.Right - 4, clearRect.Bottom - 4);
                        g.DrawLine(clearPen, clearRect.Right - 4, clearRect.Top + 4, clearRect.Left + 4, clearRect.Bottom - 4);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_toolTip != null) _toolTip.Dispose();
                if (_componentFont != null) _componentFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void SetShortcut(Keys value)
        {
            Keys normalized = NormalizeShortcut(value);
            if (!_allowModifierOnly && IsModifierOnly(normalized)) normalized = Keys.None;
            if (_shortcutKeys == normalized) { Invalidate(); return; }
            _shortcutKeys = normalized;
            Invalidate();
            EventHandler handler = ShortcutChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private static Keys NormalizeShortcut(Keys value)
        {
            Keys key = value & Keys.KeyCode;
            Keys modifiers = NormalizeModifiers(value & Keys.Modifiers);
            if (key == Keys.None) return modifiers;
            return modifiers | key;
        }

        private static Keys NormalizeModifiers(Keys value)
        {
            Keys result = Keys.None;
            if ((value & Keys.Control) != Keys.None) result |= Keys.Control;
            if ((value & Keys.Shift) != Keys.None) result |= Keys.Shift;
            if ((value & Keys.Alt) != Keys.None) result |= Keys.Alt;
            return result;
        }

        private static bool IsModifierOnly(Keys value)
        {
            return value != Keys.None && (value & Keys.KeyCode) == Keys.None;
        }

        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey ||
                   key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                   key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu;
        }

        private static Keys ModifierForKey(Keys key)
        {
            if (key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey) return Keys.Control;
            if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey) return Keys.Shift;
            if (key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu) return Keys.Alt;
            return Keys.None;
        }

        private static string FormatShortcut(Keys value)
        {
            if (value == Keys.None) return String.Empty;
            Keys modifiers = value & Keys.Modifiers;
            Keys key = value & Keys.KeyCode;
            string result = String.Empty;
            if ((modifiers & Keys.Control) != Keys.None) result += "Ctrl + ";
            if ((modifiers & Keys.Shift) != Keys.None) result += "Shift + ";
            if ((modifiers & Keys.Alt) != Keys.None) result += "Alt + ";
            if (key != Keys.None) result += key.ToString();
            return result.TrimEnd(' ', '+');
        }

        private Rectangle GetClearRectangle()
        {
            int size = Math.Min(20, Math.Max(14, Height - 12));
            return new Rectangle(Math.Max(0, Width - size - 12), (Height - size) / 2, size, size);
        }

        private bool IsClearArea(Point location)
        {
            return HasShortcut && GetClearRectangle().Contains(location);
        }
    }
}

// watashi wa watashi sore dake