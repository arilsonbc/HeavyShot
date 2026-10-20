using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HeavyShot
{
    internal static class Theme
    {
        internal static readonly Color Background = Color.FromArgb(19, 22, 29);
        internal static readonly Color Surface = Color.FromArgb(31, 36, 46);
        internal static readonly Color Accent = Color.FromArgb(126, 157, 255);
        internal static readonly Color Muted = Color.FromArgb(165, 175, 195);
        internal static readonly Font Body = new Font("Segoe UI", 10);
        internal static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath(); int d = radius * 2;
            p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90);
            p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
        }
        internal static Label Label(string text, int x, int y, int w, int h, float size, Color color)
        { return new Label { Text=text, Location=new Point(x,y), Size=new Size(w,h), ForeColor=color, Font=new Font("Segoe UI",size), BackColor=Color.Transparent }; }
    }
    internal sealed class ActionButton : Button
    {
        internal bool Active;
        internal bool Primary;
        private bool hover;
        internal ActionButton(string text, string hint)
        {
            Text=text; AccessibleName=hint; FlatStyle=FlatStyle.Flat; FlatAppearance.BorderSize=0;
            BackColor=Theme.Surface; ForeColor=Color.White; Font=Theme.Body; Cursor=Cursors.Hand;
            Size=new Size(40,36); Margin=new Padding(3); TabStop=false;
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=Primary ? Theme.Accent : Active ? Color.FromArgb(60,78,124) : hover ? Color.FromArgb(52,60,76) : Theme.Surface;
            using (GraphicsPath p=Theme.Rounded(new Rectangle(0,0,Width-1,Height-1),8))
            using (Brush b=new SolidBrush(fill)) e.Graphics.FillPath(b,p);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled ? Primary ? Theme.Background : ForeColor : Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }
    }
}
