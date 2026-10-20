using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HeavyShot
{
    internal static class Brand
    {
        internal static void Draw(Graphics g, Rectangle bounds, Color ink)
        {
            GraphicsState state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/64f,bounds.Height/64f);
            using(Brush b=new SolidBrush(ink))
            {
                g.FillRectangle(b,8,8,16,6);g.FillRectangle(b,8,8,6,16);
                g.FillRectangle(b,40,8,16,6);g.FillRectangle(b,50,8,6,16);
                g.FillRectangle(b,8,50,16,6);g.FillRectangle(b,8,40,6,16);
                g.FillRectangle(b,40,50,16,6);g.FillRectangle(b,50,40,6,16);
                g.FillRectangle(b,22,20,6,24);g.FillRectangle(b,36,20,6,24);g.FillRectangle(b,28,29,8,6);
            }
            g.Restore(state);
        }
    }
    internal sealed class HomeSurface : Panel
    {
        internal HomeSurface(){DoubleBuffered=true;BackColor=Color.White;}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            Brand.Draw(g,new Rectangle(28,22,40,40),Color.Black);
            using(Pen p=new Pen(Color.FromArgb(230,230,230))) {g.DrawLine(p,32,84,608,84);g.DrawLine(p,32,546,608,546);}
            using(GraphicsPath p=Theme.Rounded(new Rectangle(32,388,576,136),16))
            using(Brush b=new SolidBrush(Color.FromArgb(247,247,247)))g.FillPath(b,p);
            using(Pen p=new Pen(Color.FromArgb(228,228,228)))g.DrawLine(p,52,456,588,456);
            using(GraphicsPath p=Theme.Rounded(new Rectangle(432,126,168,112),16))
            using(Brush b=new SolidBrush(Color.FromArgb(246,246,246)))g.FillPath(b,p);
            using(Pen p=new Pen(Color.Black,2))
            {
                p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,452,146,112,66);
                p.DashStyle=DashStyle.Solid;
                foreach(Point corner in new Point[]{new Point(452,146),new Point(564,146),new Point(452,212),new Point(564,212)})
                {g.FillRectangle(Brushes.White,corner.X-3,corner.Y-3,6,6);g.DrawRectangle(p,corner.X-3,corner.Y-3,6,6);}
            }
            using(Pen p=new Pen(Color.FromArgb(200,200,200),4)){g.DrawLine(p,470,168,536,168);g.DrawLine(p,470,180,520,180);}
            Icons.Draw(g,"select",new Rectangle(548,198,38,38),Color.Black);
        }
    }
    internal sealed class ToggleSwitch : CheckBox
    {
        internal ToggleSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);AutoSize=false;Size=new Size(44,28);Text="";Cursor=Cursors.Hand;}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath path=Theme.Rounded(new Rectangle(1,3,42,24),12))
            using(Brush b=new SolidBrush(Checked?Color.Black:Color.FromArgb(211,211,211)))e.Graphics.FillPath(b,path);
            e.Graphics.FillEllipse(Brushes.White,Checked?23:5,7,16,16);
            if(Focused)using(Pen p=new Pen(Color.Black)){p.DashStyle=DashStyle.Dot;e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
        }
    }
}
