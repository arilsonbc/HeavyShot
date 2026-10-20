using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HeavyShot
{
    public enum Tool { Select, Pen, Marker, Line, Arrow, Rectangle, Ellipse, Text }

    public sealed class Annotation
    {
        public Tool Tool;
        public Color Color = Color.FromArgb(255, 89, 94);
        public float Width = 3;
        public List<Point> Points = new List<Point>();
        public string Text = "";
        public Point Start { get { return Points[0]; } }
        public Point End { get { return Points[Points.Count - 1]; } }

        public void Draw(Graphics g)
        {
            if (Points.Count == 0) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color ink = Tool == Tool.Marker ? Color.FromArgb(85, Color) : Color;
            using (Pen pen = new Pen(ink, Tool == Tool.Marker ? Width * 5 : Width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                Rectangle r = Geometry.Between(Start, End);
                if (Tool == Tool.Pen || Tool == Tool.Marker)
                {
                    if (Points.Count > 1) g.DrawLines(pen, Points.ToArray());
                    else using (Brush dot = new SolidBrush(ink))
                        g.FillEllipse(dot, Start.X - pen.Width / 2, Start.Y - pen.Width / 2, pen.Width, pen.Width);
                }
                else if (Tool == Tool.Line) g.DrawLine(pen, Start, End);
                else if (Tool == Tool.Arrow)
                {
                    using (AdjustableArrowCap cap = new AdjustableArrowCap(4, 5, true))
                    { pen.CustomEndCap = cap; g.DrawLine(pen, Start, End); }
                }
                else if (Tool == Tool.Rectangle && r.Width > 0 && r.Height > 0) g.DrawRectangle(pen, r);
                else if (Tool == Tool.Ellipse && r.Width > 0 && r.Height > 0) g.DrawEllipse(pen, r);
                else if (Tool == Tool.Text)
                {
                    using (Font font = new Font("Segoe UI", Math.Max(16, Width * 5), FontStyle.Bold, GraphicsUnit.Pixel))
                    using (Brush brush = new SolidBrush(Color))
                        g.DrawString(Text, font, brush, Start, StringFormat.GenericTypographic);
                }
            }
        }
    }

    public static class Geometry
    {
        public static Rectangle Between(Point a, Point b)
        { return Rectangle.FromLTRB(Math.Min(a.X,b.X), Math.Min(a.Y,b.Y), Math.Max(a.X,b.X), Math.Max(a.Y,b.Y)); }

        public static Rectangle Clamp(Rectangle r, Size size)
        { return Rectangle.Intersect(new Rectangle(Point.Empty, size), r); }
    }

    public sealed class CaptureDocument : IDisposable
    {
        public readonly Bitmap Background;
        public Rectangle Selection;
        public readonly List<Annotation> Items = new List<Annotation>();
        private readonly Stack<Annotation> redo = new Stack<Annotation>();
        public CaptureDocument(Bitmap bitmap) { Background = bitmap; }
        public bool CanUndo { get { return Items.Count > 0; } }
        public bool CanRedo { get { return redo.Count > 0; } }
        public void Add(Annotation item) { Items.Add(item); redo.Clear(); }
        public void Undo()
        {
            if (!CanUndo) return;
            Annotation item = Items[Items.Count - 1]; Items.RemoveAt(Items.Count - 1); redo.Push(item);
        }
        public void Redo() { if (CanRedo) Items.Add(redo.Pop()); }
        public void Clear() { Items.Clear(); redo.Clear(); }
        public Bitmap RenderSelection()
        {
            Rectangle crop = Geometry.Clamp(Selection, Background.Size);
            if (crop.Width < 2 || crop.Height < 2) throw new InvalidOperationException("Selecione uma área com pelo menos 2 × 2 pixels.");
            Bitmap output = new Bitmap(crop.Width, crop.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(output))
            {
                g.DrawImage(Background, new Rectangle(Point.Empty, crop.Size), crop, GraphicsUnit.Pixel);
                g.TranslateTransform(-crop.X, -crop.Y);
                foreach (Annotation item in Items) item.Draw(g);
            }
            return output;
        }
        public void Dispose() { Background.Dispose(); }
    }
}
