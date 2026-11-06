using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxItem(true)]
    [Category("ZenForms Modern Components")]
    [Description("Interactive visual simulator for digital logic circuits")]
    public class ZenCircuitLab : UserControl
    {
        public enum GateType { AND, OR, XOR, NOT, NAND }
        public enum NodeKind { Input, Gate, Output }

        public sealed class CircuitNode
        {
            internal CircuitNode(int id, string name, NodeKind kind, GateType gate, int inputs)
            { Id = id; Name = name; Kind = kind; Gate = gate; InputCount = inputs; Value = false; }
            public int Id { get; private set; }
            public string Name { get; set; }
            public NodeKind Kind { get; private set; }
            public GateType Gate { get; private set; }
            public int InputCount { get; private set; }
            public bool Value { get; internal set; }
            internal readonly List<CircuitNode> Sources = new List<CircuitNode>();
            internal readonly List<CircuitNode> Targets = new List<CircuitNode>();
        }

        public sealed class OutputChangedEventArgs : EventArgs
        {
            public OutputChangedEventArgs(CircuitNode output, bool value) { Output = output; Value = value; }
            public CircuitNode Output { get; private set; }
            public bool Value { get; private set; }
        }

        public sealed class TruthTable
        {
            public TruthTable(string[] inputs, string[] outputs, bool[][] rows)
            { InputNames = inputs; OutputNames = outputs; Rows = rows; }
            public string[] InputNames { get; private set; }
            public string[] OutputNames { get; private set; }
            public bool[][] Rows { get; private set; }
        }

        private readonly List<CircuitNode> nodes = new List<CircuitNode>();
        private readonly ToolTip toolTip = new ToolTip();
        private readonly Font titleFont;
        private int nextId = 1;
        private int scrollY;
        private CircuitNode selected;
        private bool evaluating;

        public ZenCircuitLab()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true; BackColor = ZenTheme.Background; ForeColor = ZenTheme.Foreground;
            titleFont = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            toolTip.SetToolTip(this, "Click on an entry to switch between them. Use Tab and Space/Enter to operate from the keyboard.");
            MinimumSize = new Size(300, 180);
            AccessibleName = "Digital Circuirts Lab";
            AccessibleDescription = "Logic gate canvas. Tab navigates through the components; Space toggles the selected input.";
        }

        [Category("Data"), Description("Circuit inputs, ports and exits")]
        public IList<CircuitNode> Nodes { get { return nodes.AsReadOnly(); } }

        [Category("Behavior"), Description("Triggers when the value of an output changes")]
        public event EventHandler<OutputChangedEventArgs> OutputChanged;

        [Category("Behavior"), Description("Triggers when a circuit evaluation failure occurs")]
        public event EventHandler<CircuitErrorEventArgs> CircuitError;

        public sealed class CircuitErrorEventArgs : EventArgs
        {
            public CircuitErrorEventArgs(string message, CircuitNode node) { Message = message; Node = node; }
            public string Message { get; private set; }
            public CircuitNode Node { get; private set; }
        }

        public CircuitNode AddInput(string name)
        {
            return AddInput(name, false);
        }
        public CircuitNode AddInput(string name, bool initialValue)
        {
            CircuitNode n = NewNode(name, NodeKind.Input, GateType.AND, 0); n.Value = initialValue; Invalidate(); return n;
        }
        public CircuitNode AddGate(GateType type, string name)
        {
            int count = type == GateType.NOT ? 1 : 2;
            if (!Enum.IsDefined(typeof(GateType), type)) throw new ArgumentOutOfRangeException("type");
            CircuitNode n = NewNode(name, NodeKind.Gate, type, count); Invalidate(); return n;
        }
        public CircuitNode AddGate(GateType type) { return AddGate(type, type.ToString() + " " + (nextId)); }
        public CircuitNode AddOutput(string name)
        {
            CircuitNode n = NewNode(name, NodeKind.Output, GateType.AND, 1); Invalidate(); return n;
        }
        private CircuitNode NewNode(string name, NodeKind kind, GateType type, int inputs)
        {
            if (String.IsNullOrWhiteSpace(name)) name = kind.ToString() + " " + nextId;
            CircuitNode n = new CircuitNode(nextId++, name, kind, type, inputs); nodes.Add(n); return n;
        }

        public void Connect(CircuitNode source, CircuitNode destination) { Connect(source, destination, -1); }
        public void Connect(CircuitNode source, CircuitNode destination, int inputPin)
        {
            if (source == null || destination == null) throw new ArgumentNullException(source == null ? "source" : "destination");
            if (!nodes.Contains(source) || !nodes.Contains(destination)) throw new ArgumentException("Both components must belong to this circuit");
            if (source == destination) throw new InvalidOperationException("A direct connection to the component itself would form a loop");
            if (source.Kind == NodeKind.Output) throw new InvalidOperationException("An output cannot power another component");
            if (destination.Kind == NodeKind.Input) throw new InvalidOperationException("An input cannot receive wires");
            int pin = inputPin < 0 ? destination.Sources.Count : inputPin;
            if (pin < 0 || pin >= destination.InputCount) throw new ArgumentOutOfRangeException("inputPin", "Pin nonexistent");
            if (destination.Sources.Count > pin) throw new InvalidOperationException("The pin is already connected");
            if (pin != destination.Sources.Count) throw new InvalidOperationException("Connect the pins in order, without gaps");
            if (HasPath(destination, source, new HashSet<CircuitNode>())) throw new InvalidOperationException("Connection refused: this would create a loop in the circuit");
            source.Targets.Add(destination); destination.Sources.Add(source); Invalidate();
        }
        private bool HasPath(CircuitNode from, CircuitNode sought, HashSet<CircuitNode> seen)
        {
            if (from == sought) return true;
            if (!seen.Add(from)) return false;
            foreach (CircuitNode n in from.Targets) if (HasPath(n, sought, seen)) return true;
            return false;
        }

        public void SetInput(CircuitNode input, bool value)
        {
            if (input == null || !nodes.Contains(input) || input.Kind != NodeKind.Input) throw new ArgumentException("Component isn't an input to this circuit.", "input");
            input.Value = value; Evaluate(); Invalidate();
        }
        public void SetInput(string name, bool value)
        {
            CircuitNode n = nodes.FirstOrDefault(x => x.Kind == NodeKind.Input && String.Equals(x.Name, name, StringComparison.Ordinal));
            if (n == null) throw new ArgumentException("Entry not found: " + name, "name"); SetInput(n, value);
        }
        public void Evaluate()
        {
            if (evaluating) throw new InvalidOperationException("Reentrant evaluation isn't permitted");
            evaluating = true;
            try
            {
                Dictionary<CircuitNode, bool> memo = new Dictionary<CircuitNode, bool>(); HashSet<CircuitNode> stack = new HashSet<CircuitNode>();
                foreach (CircuitNode n in nodes.Where(x => x.Kind == NodeKind.Output))
                {
                    bool old = n.Value; bool value = EvalNode(n, memo, stack); n.Value = value;
                    if (old != value && OutputChanged != null) OutputChanged(this, new OutputChangedEventArgs(n, value));
                }
                Invalidate();
            }
            catch (Exception ex) { EventHandler<CircuitErrorEventArgs> handler = CircuitError; if (handler != null) handler(this, new CircuitErrorEventArgs(ex.Message, null)); throw; }
            finally { evaluating = false; }
        }
        private bool EvalNode(CircuitNode n, Dictionary<CircuitNode, bool> memo, HashSet<CircuitNode> stack)
        {
            bool val; if (memo.TryGetValue(n, out val)) return val;
            if (!stack.Add(n)) throw new InvalidOperationException("Cycle detected during the evaluation on " + n.Name + ".");
            if (n.Kind == NodeKind.Input) val = n.Value;
            else
            {
                if (n.Sources.Count != n.InputCount) throw new InvalidOperationException("Component " + n.Name + " has a disconected pines");
                bool a = EvalNode(n.Sources[0], memo, stack), b = n.InputCount > 1 ? EvalNode(n.Sources[1], memo, stack) : false;
                if (n.Kind == NodeKind.Output) val = a;
                else switch (n.Gate) { case GateType.AND: val = a && b; break; case GateType.OR: val = a || b; break; case GateType.XOR: val = a ^ b; break; case GateType.NAND: val = !(a && b); break; case GateType.NOT: val = !a; break; default: throw new InvalidOperationException("Unsupported port"); }
            }
            stack.Remove(n); n.Value = val; memo[n] = val; return val;
        }
        public TruthTable CreateTruthTable()
        {
            CircuitNode[] inputs = nodes.Where(x => x.Kind == NodeKind.Input).ToArray(); CircuitNode[] outputs = nodes.Where(x => x.Kind == NodeKind.Output).ToArray();
            if (inputs.Length > 8) throw new InvalidOperationException("The Truth Table supports a maximum of 8 inputs");
            bool[] original = inputs.Select(x => x.Value).ToArray(); bool[][] rows = new bool[1 << inputs.Length][];
            try
            {
                for (int r = 0; r < rows.Length; r++)
                {
                    for (int i = 0; i < inputs.Length; i++) inputs[i].Value = ((r >> (inputs.Length - 1 - i)) & 1) != 0;
                    Dictionary<CircuitNode, bool> memo = new Dictionary<CircuitNode, bool>(); HashSet<CircuitNode> stack = new HashSet<CircuitNode>();
                    rows[r] = new bool[inputs.Length + outputs.Length];
                    for (int i = 0; i < inputs.Length; i++) rows[r][i] = inputs[i].Value;
                    for (int o = 0; o < outputs.Length; o++) rows[r][inputs.Length + o] = EvalNode(outputs[o], memo, stack);
                }
            }
            finally { for (int i = 0; i < inputs.Length; i++) inputs[i].Value = original[i]; Evaluate(); }
            return new TruthTable(inputs.Select(x => x.Name).ToArray(), outputs.Select(x => x.Name).ToArray(), rows);
        }
        public void ClearCircuit()
        {
            nodes.Clear(); selected = null; nextId = 1; scrollY = 0; Invalidate();
        }

        private Rectangle NodeRect(CircuitNode n)
        {
            int index = nodes.IndexOf(n); return new Rectangle(150 + (n.Kind == NodeKind.Output ? 330 : n.Kind == NodeKind.Gate ? 165 : 0), 65 + index * 64 - scrollY, 122, 42);
        }
        private Point Pin(CircuitNode n, bool output)
        {
            Rectangle r = NodeRect(n); return new Point(output ? r.Right : r.Left, r.Top + r.Height / 2);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // watashi wa watashi sore dake
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush bg = new SolidBrush(BackColor)) g.FillRectangle(bg, ClientRectangle);
            using (SolidBrush panel = new SolidBrush(ZenTheme.Surface)) g.FillRectangle(panel, 12, 12, Math.Max(0, Width - 24), 32);
            using (SolidBrush title = new SolidBrush(ZenTheme.Foreground)) g.DrawString("ZEN CIRCUIT LAB", titleFont, title, 22, 20);
            using (Pen wire = new Pen(ZenTheme.Border, 2))
            {
                foreach (CircuitNode n in nodes) foreach (CircuitNode d in n.Targets)
                    {
                        bool active = n.Value; wire.Color = active ? ZenTheme.Accent : ZenTheme.Muted;
                        Point a = Pin(n, true), b = Pin(d, false); int mid = (a.X + b.X) / 2;
                        g.DrawLine(wire, a.X, a.Y, mid, a.Y); g.DrawLine(wire, mid, a.Y, mid, b.Y); g.DrawLine(wire, mid, b.Y, b.X, b.Y);
                    }
            }
            foreach (CircuitNode n in nodes)
            {
                Rectangle r = NodeRect(n); if (r.Bottom < 45 || r.Top > Height) continue;
                Color fill = n.Kind == NodeKind.Input && n.Value || n.Kind == NodeKind.Output && n.Value ? ZenTheme.Primary : ZenTheme.SurfaceRaised;
                using (SolidBrush brush = new SolidBrush(fill)) g.FillRounded(r, brush, 7);
                using (Pen border = new Pen(selected == n ? ZenTheme.Accent : ZenTheme.Border, selected == n ? 2 : 1)) g.DrawRounded(r, border, 7);
                string label = n.Kind == NodeKind.Gate ? n.Gate.ToString() : n.Kind == NodeKind.Output ? "OUT" : "IN";
                using (SolidBrush fg = new SolidBrush(ZenTheme.Foreground)) { g.DrawString(label, Font, fg, r.X + 7, r.Y + 5); g.DrawString(n.Name + "  " + (n.Value ? "1" : "0"), Font, fg, r.X + 7, r.Y + 22); }
                if (n.Kind != NodeKind.Input) { using (SolidBrush p = new SolidBrush(ZenTheme.Muted)) g.FillEllipse(p, r.Left - 4, r.Top + 17, 8, 8); }
                if (n.Kind != NodeKind.Output) { using (SolidBrush p = new SolidBrush(ZenTheme.Accent)) g.FillEllipse(p, r.Right - 4, r.Top + 17, 8, 8); }
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus(); CircuitNode hit = nodes.FirstOrDefault(n => NodeRect(n).Contains(e.Location)); selected = hit;
            if (hit != null && hit.Kind == NodeKind.Input && e.Button == MouseButtons.Left) SetInput(hit, !hit.Value);
            Invalidate();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            { if (selected != null && selected.Kind == NodeKind.Input) SetInput(selected, !selected.Value); e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { scrollY += 48; Invalidate(); e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { scrollY = Math.Max(0, scrollY - 48); Invalidate(); e.Handled = true; }
            else if (e.KeyCode == Keys.Tab)
            { CircuitNode[] items = nodes.ToArray(); if (items.Length > 0) { int i = Array.IndexOf(items, selected); selected = items[(i + (e.Shift ? items.Length - 1 : 1)) % items.Length]; Invalidate(); } e.Handled = true; }
        }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); scrollY = Math.Max(0, scrollY - e.Delta / 3); Invalidate(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { toolTip.Dispose(); titleFont.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal static class ZenCircuitLabDrawingExtensions
    {
        internal static void FillRounded(this Graphics g, Rectangle r, Brush b, int radius)
        { using (GraphicsPath p = Rounded(r, radius)) g.FillPath(b, p); }
        internal static void DrawRounded(this Graphics g, Rectangle r, Pen pen, int radius)
        { using (GraphicsPath p = Rounded(r, radius)) g.DrawPath(pen, p); }
        private static GraphicsPath Rounded(Rectangle r, int d)
        {
            GraphicsPath p = new GraphicsPath(); int s = d * 2;
            p.AddArc(r.X, r.Y, s, s, 180, 90); p.AddArc(r.Right - s, r.Y, s, s, 270, 90);
            p.AddArc(r.Right - s, r.Bottom - s, s, s, 0, 90); p.AddArc(r.X, r.Bottom - s, s, s, 90, 90); p.CloseFigure(); return p;
        }
    }
}