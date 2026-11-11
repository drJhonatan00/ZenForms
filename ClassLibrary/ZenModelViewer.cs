using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZenForms.Core
{
    [ToolboxBitmap(typeof(Panel))]
    [Category("ZenForms Modern Controls")]
    [Description("3D Models viewer with four extensions support.")]
    public class ZenModelViewer : UserControl
    {
        private readonly Viewport viewport;
        private readonly Panel toolbar;
        private readonly FlowLayoutPanel actionBar;
        private readonly Label status;
        private readonly Label help;
        private readonly Button openButton;
        private readonly Button fitButton;
        private readonly Button wireButton;
        private readonly Button resetButton;
        private Mesh mesh;
        private string fileName = String.Empty;
        private float yaw = -0.65f;
        private float pitch = 0.38f;
        private float zoom = 1f;
        private bool wireframe;
        private bool showAxes = true;
        private Color modelColor = Color.FromArgb(95, 156, 255);

        public event EventHandler ModelLoaded;
        public event EventHandler<ModelViewerErrorEventArgs> ModelError;

        public ZenModelViewer()
        {
            BackColor = ZenTheme.Background;
            ForeColor = ZenTheme.Foreground;
            MinimumSize = new Size(300, 240);
            Size = new Size(640, 480);

            toolbar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ZenTheme.Surface, Padding = new Padding(7, 4, 7, 3) };
            actionBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true, BackColor = ZenTheme.Surface, Margin = Padding.Empty, Padding = new Padding(0) };
            openButton = MakeButton("Open Model", 92);
            fitButton = MakeButton("To Frame", 78);
            wireButton = MakeButton("Show Grids", 76);
            resetButton = MakeButton("Reload", 100);
            status = new Label { Dock = DockStyle.Bottom, Height = 22, Text = "Open a 3D Model — OBJ, STL, PLY or OFF", ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8.5f), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            actionBar.Controls.Add(openButton);
            actionBar.Controls.Add(fitButton);
            actionBar.Controls.Add(wireButton);
            actionBar.Controls.Add(resetButton);
            toolbar.Controls.Add(actionBar);
            toolbar.Controls.Add(status);

            viewport = new Viewport(this) { Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 18, 31) };
            help = new Label { Dock = DockStyle.Bottom, Height = 25, BackColor = ZenTheme.Surface, ForeColor = ZenTheme.Muted, Font = new Font("Segoe UI", 8f), Text = "Drag: orbit     •     Scroll: zoom     •     Right-Click: to move", TextAlign = ContentAlignment.MiddleCenter };
            Controls.Add(viewport);
            Controls.Add(help);
            Controls.Add(toolbar);

            openButton.Click += delegate { OpenFileDialogAndLoad(); };
            fitButton.Click += delegate { FitModel(); };
            wireButton.Click += delegate { Wireframe = !Wireframe; };
            resetButton.Click += delegate { ResetView(); };
        }

        [Browsable(false)]
        public string FileName { get { return fileName; } }

        [Browsable(false)]
        public int VertexCount { get { return mesh == null ? 0 : mesh.Vertices.Count; } }

        [Browsable(false)]
        public int TriangleCount { get { return mesh == null ? 0 : mesh.Triangles.Count; } }

        [Category("ZenForms 3D"), DefaultValue(false)]
        public bool Wireframe
        {
            get { return wireframe; }
            set { wireframe = value; wireButton.Text = value ? "Solid" : "Show Grids"; viewport.Invalidate(); }
        }

        [Category("ZenForms 3D"), DefaultValue(true)]
        public bool ShowAxes
        {
            get { return showAxes; }
            set { showAxes = value; viewport.Invalidate(); }
        }

        [Category("ZenForms 3D")]
        public Color ModelColor
        {
            get { return modelColor; }
            set { modelColor = value; viewport.Invalidate(); }
        }
        public void LoadModel(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Enter the model path.", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("The model wasn't found.", path);
            try
            {
                Mesh loaded = MeshReader.Read(path);
                if (loaded.Vertices.Count == 0 || loaded.Triangles.Count == 0)
                    throw new InvalidDataException("The file cannot was opened.");
                loaded.Normalize();
                mesh = loaded;
                fileName = Path.GetFullPath(path);
                status.Text = Path.GetFileName(path) + "  •  " + mesh.Vertices.Count.ToString("N0") + " vertices  •  " + mesh.Triangles.Count.ToString("N0") + " triangles";
                ResetView();
                EventHandler handler = ModelLoaded;
                if (handler != null) handler(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                status.Text = "The file cannot was opened";
                EventHandler<ModelViewerErrorEventArgs> handler = ModelError;
                if (handler != null) handler(this, new ModelViewerErrorEventArgs("Failed to load the model. Ensure that it has one of the specified extensions and that it isn't corrupted.", ex));
                else MessageBox.Show(FindForm(), "Failed to load 3D model:\r\n" + ex.Message, "ZenModelViewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void OpenFileDialogAndLoad()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Open 3D Model";
                dialog.Filter = "3D Models|*.obj;*.stl;*.ply;*.off|Wavefront OBJ|*.obj|STereoLithography|*.stl|Stanford PLY (ASCII)|*.ply|Object File Format|*.off|All Files|*.*";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadModel(dialog.FileName);
            }
        }

        public void FitModel()
        {
            zoom = 1f;
            viewport.Invalidate();
        }

        public void ResetView()
        {
            yaw = -0.65f;
            pitch = 0.38f;
            zoom = 1f;
            viewport.Invalidate();
        }

        private static Button MakeButton(string text, int width)
        {
            return new Button { Text = text, Width = width, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = ZenTheme.SurfaceRaised, ForeColor = ZenTheme.Foreground, Font = new Font("Segoe UI", 8f), Margin = new Padding(2, 0, 3, 0), Cursor = Cursors.Hand };
        }

        private sealed class Viewport : Control
        {
            private readonly ZenModelViewer owner;
            private Point lastPoint;
            private bool dragging;
            private bool panning;
            private float panX;
            private float panY;

            public Viewport(ZenModelViewer owner)
            {
                this.owner = owner;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
                TabStop = true;
                Cursor = Cursors.SizeAll;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
                {
                    dragging = true;
                    panning = e.Button == MouseButtons.Right;
                    lastPoint = e.Location;
                    Capture = true;
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                dragging = false;
                Capture = false;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (!dragging) return;
                int dx = e.X - lastPoint.X;
                int dy = e.Y - lastPoint.Y;
                if (panning) { panX += dx; panY += dy; }
                else
                {
                    owner.yaw += dx * 0.009f;
                    owner.pitch = Math.Max(-1.48f, Math.Min(1.48f, owner.pitch + dy * 0.009f));
                }
                lastPoint = e.Location;
                Invalidate();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                owner.zoom *= e.Delta > 0 ? 1.12f : 0.89f;
                owner.zoom = Math.Max(0.08f, Math.Min(30f, owner.zoom));
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                DrawGrid(g);
                if (owner.showAxes) DrawAxes(g);
                if (owner.mesh == null) { DrawEmptyHint(g); return; }
                DrawMesh(g, owner.mesh);
            }

            private void DrawGrid(Graphics g)
            {
                using (Pen pen = new Pen(Color.FromArgb(28, 104, 121, 151)))
                {
                    float cx = ClientSize.Width / 2f + panX;
                    float cy = ClientSize.Height * 0.72f + panY;
                    for (int i = -6; i <= 6; i++)
                    {
                        g.DrawLine(pen, cx + i * 26, cy - 90, cx + i * 48, cy + 105);
                        g.DrawLine(pen, cx - 155, cy + i * 15, cx + 155, cy + i * 15);
                    }
                }
            }

            private void DrawAxes(Graphics g)
            {
                float x = 48, y = ClientSize.Height - 42;
                DrawAxis(g, x, y, x + 23, y, Color.FromArgb(255, 105, 115), "X");
                DrawAxis(g, x, y, x, y - 23, Color.FromArgb(104, 220, 156), "Y");
                DrawAxis(g, x, y, x + 16, y + 13, Color.FromArgb(99, 163, 255), "Z");
            }

            private static void DrawAxis(Graphics g, float x1, float y1, float x2, float y2, Color color, string label)
            {
                using (Pen pen = new Pen(color, 2f)) g.DrawLine(pen, x1, y1, x2, y2);
                using (SolidBrush brush = new SolidBrush(color)) g.DrawString(label, SystemFonts.MessageBoxFont, brush, x2, y2);
            }

            private void DrawEmptyHint(Graphics g)
            {
                string text = "Open a 3D file to view.";
                using (Font font = new Font("Segoe UI", 13f, FontStyle.Regular))
                using (SolidBrush brush = new SolidBrush(ZenTheme.Muted))
                {
                    SizeF size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, (ClientSize.Width - size.Width) / 2f, (ClientSize.Height - size.Height) / 2f);
                }
            }

            private void DrawMesh(Graphics g, Mesh model)
            {
                float cx = ClientSize.Width / 2f + panX;
                float cy = ClientSize.Height / 2f + panY;
                float scale = Math.Min(ClientSize.Width, ClientSize.Height) * 0.37f * owner.zoom;
                float cyaw = (float)Math.Cos(owner.yaw), syaw = (float)Math.Sin(owner.yaw);
                float cpitch = (float)Math.Cos(owner.pitch), spitch = (float)Math.Sin(owner.pitch);
                List<ProjectedTriangle> projected = new List<ProjectedTriangle>(model.Triangles.Count);

                for (int i = 0; i < model.Triangles.Count; i++)
                {
                    Triangle t = model.Triangles[i];
                    Point3 a = Rotate(model.Vertices[t.A], cyaw, syaw, cpitch, spitch);
                    Point3 b = Rotate(model.Vertices[t.B], cyaw, syaw, cpitch, spitch);
                    Point3 c = Rotate(model.Vertices[t.C], cyaw, syaw, cpitch, spitch);
                    PointF pa = Project(a, cx, cy, scale);
                    PointF pb = Project(b, cx, cy, scale);
                    PointF pc = Project(c, cx, cy, scale);
                    float depth = (a.Z + b.Z + c.Z) / 3f;
                    Point3 normal = Cross(Subtract(b, a), Subtract(c, a));
                    float length = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                    if (length > 0) { normal.X /= length; normal.Y /= length; normal.Z /= length; }
                    float light = Math.Abs(normal.X * -0.35f + normal.Y * 0.65f + normal.Z * -0.68f);
                    int brightness = Math.Max(70, Math.Min(255, 92 + (int)(light * 155f)));
                    Color face = Color.FromArgb(brightness * owner.modelColor.R / 255, brightness * owner.modelColor.G / 255, brightness * owner.modelColor.B / 255);
                    projected.Add(new ProjectedTriangle { A = pa, B = pb, C = pc, Depth = depth, Color = face });
                }

                projected.Sort(delegate (ProjectedTriangle x, ProjectedTriangle y) { return y.Depth.CompareTo(x.Depth); });
                using (Pen edge = new Pen(Color.FromArgb(205, 18, 25, 41), 0.7f))
                {
                    for (int i = 0; i < projected.Count; i++)
                    {
                        ProjectedTriangle t = projected[i];
                        PointF[] points = new PointF[] { t.A, t.B, t.C };
                        if (!Visible(points)) continue;
                        if (owner.wireframe) { using (Pen wirePen = new Pen(owner.modelColor, 1f)) g.DrawPolygon(wirePen, points); }
                        else
                        {
                            using (SolidBrush brush = new SolidBrush(t.Color)) g.FillPolygon(brush, points);
                            g.DrawPolygon(edge, points);
                        }
                    }
                }
            }

            private bool Visible(PointF[] p)
            {
                RectangleF bounds = new RectangleF(Math.Min(p[0].X, Math.Min(p[1].X, p[2].X)), Math.Min(p[0].Y, Math.Min(p[1].Y, p[2].Y)), Math.Max(p[0].X, Math.Max(p[1].X, p[2].X)) - Math.Min(p[0].X, Math.Min(p[1].X, p[2].X)), Math.Max(p[0].Y, Math.Max(p[1].Y, p[2].Y)) - Math.Min(p[0].Y, Math.Min(p[1].Y, p[2].Y)));
                return bounds.IntersectsWith(ClientRectangle);
            }

            private static Point3 Rotate(Point3 p, float cyaw, float syaw, float cpitch, float spitch)
            {
                float x = p.X * cyaw - p.Z * syaw;
                float z = p.X * syaw + p.Z * cyaw;
                float y = p.Y * cpitch - z * spitch;
                z = p.Y * spitch + z * cpitch;
                return new Point3(x, y, z);
            }

            private static PointF Project(Point3 p, float cx, float cy, float scale)
            {
                float perspective = 3.2f / (3.2f + p.Z);
                perspective = Math.Max(0.35f, Math.Min(2.2f, perspective));
                return new PointF(cx + p.X * scale * perspective, cy - p.Y * scale * perspective);
            }

            private static Point3 Subtract(Point3 a, Point3 b) { return new Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
            private static Point3 Cross(Point3 a, Point3 b) { return new Point3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
        }

        private sealed class ProjectedTriangle
        {
            public PointF A, B, C;
            public float Depth;
            public Color Color;
        }

        private sealed class Mesh
        {
            public readonly List<Point3> Vertices = new List<Point3>();
            public readonly List<Triangle> Triangles = new List<Triangle>();

            public void Normalize()
            {
                if (Vertices.Count == 0) return;
                float minX = Vertices.Min(v => v.X), maxX = Vertices.Max(v => v.X);
                float minY = Vertices.Min(v => v.Y), maxY = Vertices.Max(v => v.Y);
                float minZ = Vertices.Min(v => v.Z), maxZ = Vertices.Max(v => v.Z);
                float cx = (minX + maxX) / 2f, cy = (minY + maxY) / 2f, cz = (minZ + maxZ) / 2f;
                float extent = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
                if (extent < 0.000001f) extent = 1f;
                float factor = 2f / extent;
                for (int i = 0; i < Vertices.Count; i++)
                {
                    Point3 v = Vertices[i];
                    Vertices[i] = new Point3((v.X - cx) * factor, (v.Y - cy) * factor, (v.Z - cz) * factor);
                }
            }
        }

        private struct Point3
        {
            public float X, Y, Z;
            public Point3(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        private struct Triangle
        {
            public int A, B, C;
            public Triangle(int a, int b, int c) { A = a; B = b; C = c; }
        }

        private static class MeshReader
        {
            public static Mesh Read(string path)
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".obj") return ReadObj(path);
                if (extension == ".stl") return ReadStl(path);
                if (extension == ".ply") return ReadPly(path);
                if (extension == ".off") return ReadOff(path);
                throw new NotSupportedException("This file extension wasn't supported. Use OBJ, STL, PLY ASCII or OFF.");
            }

            private static Mesh ReadObj(string path)
            {
                Mesh mesh = new Mesh();
                string[] lines = File.ReadAllLines(path);
                for (int lineNo = 0; lineNo < lines.Length; lineNo++)
                {
                    string line = lines[lineNo].Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;
                    if (parts[0] == "v" && parts.Length >= 4)
                        mesh.Vertices.Add(new Point3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                    else if (parts[0] == "f" && parts.Length >= 4)
                    {
                        List<int> face = new List<int>();
                        for (int i = 1; i < parts.Length; i++)
                        {
                            string token = parts[i].Split('/')[0];
                            int index = Int32.Parse(token, CultureInfo.InvariantCulture);
                            face.Add(index < 0 ? mesh.Vertices.Count + index : index - 1);
                        }
                        Triangulate(mesh, face);
                    }
                }
                return mesh;
            }

            private static Mesh ReadStl(string path)
            {
                Mesh mesh = new Mesh();
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length >= 84)
                {
                    uint count = BitConverter.ToUInt32(bytes, 80);
                    long expected = 84L + count * 50L;
                    if (expected == bytes.Length)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            int offset = 84 + i * 50 + 12;
                            int a = AddStlVertex(mesh, bytes, offset);
                            int b = AddStlVertex(mesh, bytes, offset + 12);
                            int c = AddStlVertex(mesh, bytes, offset + 24);
                            mesh.Triangles.Add(new Triangle(a, b, c));
                        }
                        return mesh;
                    }
                }
                using (StringReader reader = new StringReader(Encoding.ASCII.GetString(bytes)))
                {
                    string line;
                    List<int> face = new List<int>(3);
                    while ((line = reader.ReadLine()) != null)
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("vertex ", StringComparison.OrdinalIgnoreCase)) continue;
                        string[] p = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        int index = mesh.Vertices.Count;
                        mesh.Vertices.Add(new Point3(ParseFloat(p[1]), ParseFloat(p[2]), ParseFloat(p[3])));
                        face.Add(index);
                        if (face.Count == 3) { mesh.Triangles.Add(new Triangle(face[0], face[1], face[2])); face.Clear(); }
                    }
                }
                return mesh;
            }

            private static int AddStlVertex(Mesh mesh, byte[] data, int offset)
            {
                int index = mesh.Vertices.Count;
                mesh.Vertices.Add(new Point3(BitConverter.ToSingle(data, offset), BitConverter.ToSingle(data, offset + 4), BitConverter.ToSingle(data, offset + 8)));
                return index;
            }

            private static Mesh ReadPly(string path)
            {
                Mesh mesh = new Mesh();
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;
                    int vertexCount = 0, faceCount = 0;
                    bool ascii = false, headerDone = false;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] p = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length == 0) continue;
                        if (p[0] == "format" && p.Length > 1) ascii = p[1] == "ascii";
                        if (p[0] == "element" && p.Length > 2)
                        {
                            if (p[1] == "vertex") vertexCount = Int32.Parse(p[2], CultureInfo.InvariantCulture);
                            if (p[1] == "face") faceCount = Int32.Parse(p[2], CultureInfo.InvariantCulture);
                        }
                        if (p[0] == "end_header") { headerDone = true; break; }
                    }
                    if (!headerDone || !ascii) throw new NotSupportedException("This viewer supports PLY files on ASCII format (format ascii 1.0). Binary PLY files aren't yet supported.");
                    for (int i = 0; i < vertexCount; i++)
                    {
                        line = reader.ReadLine();
                        if (line == null) throw new InvalidDataException("PLY file truncated in the vertex list.");
                        string[] p = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length < 3) throw new InvalidDataException("PLY vertex is invalid.");
                        mesh.Vertices.Add(new Point3(ParseFloat(p[0]), ParseFloat(p[1]), ParseFloat(p[2])));
                    }
                    for (int i = 0; i < faceCount; i++)
                    {
                        line = reader.ReadLine();
                        if (line == null) throw new InvalidDataException("PLY file truncated in the faces list.");
                        string[] p = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        int n = Int32.Parse(p[0], CultureInfo.InvariantCulture);
                        if (p.Length < n + 1) throw new InvalidDataException("PLY face is invalid.");
                        List<int> face = new List<int>();
                        for (int j = 0; j < n; j++) face.Add(Int32.Parse(p[j + 1], CultureInfo.InvariantCulture));
                        Triangulate(mesh, face);
                    }
                }
                return mesh;
            }

            private static Mesh ReadOff(string path)
            {
                Mesh mesh = new Mesh();
                List<string> tokens = new List<string>();
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                    tokens.AddRange(trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                }
                int cursor = 0;
                if (tokens.Count == 0 || tokens[cursor++] != "OFF") throw new InvalidDataException("OFF Header is invalid.");
                if (tokens.Count < cursor + 3) throw new InvalidDataException("OFF Header is incompleted.");
                int vertices = Int32.Parse(tokens[cursor++], CultureInfo.InvariantCulture);
                int faces = Int32.Parse(tokens[cursor++], CultureInfo.InvariantCulture);
                cursor++; // ejji
                for (int i = 0; i < vertices; i++)
                {
                    mesh.Vertices.Add(new Point3(ParseFloat(tokens[cursor++]), ParseFloat(tokens[cursor++]), ParseFloat(tokens[cursor++])));
                }
                for (int i = 0; i < faces; i++)
                {
                    int count = Int32.Parse(tokens[cursor++], CultureInfo.InvariantCulture);
                    List<int> face = new List<int>();
                    for (int j = 0; j < count; j++) face.Add(Int32.Parse(tokens[cursor++], CultureInfo.InvariantCulture));
                    Triangulate(mesh, face);
                }
                return mesh;
            }

            private static void Triangulate(Mesh mesh, IList<int> face)
            {
                for (int i = 1; i < face.Count - 1; i++)
                {
                    int a = face[0], b = face[i], c = face[i + 1];
                    if (a >= 0 && b >= 0 && c >= 0 && a < mesh.Vertices.Count && b < mesh.Vertices.Count && c < mesh.Vertices.Count)
                        mesh.Triangles.Add(new Triangle(a, b, c));
                }
            }

            private static float ParseFloat(string text)
            {
                return Single.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

    }

    public sealed class ModelViewerErrorEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public ModelViewerErrorEventArgs(string message, Exception exception) { Message = message; Exception = exception; }
    }
}

//watashi wa watashi sore dake