using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HeavyShot
{
    internal static class Theme
    {
        internal static readonly Color Background = Color.FromArgb(246,246,246);
        internal static readonly Color Surface = Color.White;
        internal static readonly Color Accent = Color.Black;
        internal static readonly Color AccentSoft = Color.FromArgb(235,235,235);
        internal static readonly Color Muted = Color.FromArgb(110,110,110);
        internal static readonly Color Text = Color.FromArgb(20,20,20);
        internal static readonly Color Icon = Color.FromArgb(100,100,100);
        internal static readonly Color Border = Color.FromArgb(225,225,225);
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
    internal sealed class ActionButton : Control, IButtonControl
    {
        internal bool Active;
        internal bool Primary;
        internal string Glyph;
        internal Color Swatch=Color.Empty;
        private bool hover;
        internal ActionButton(string text, string hint)
        {
            Text=text; AccessibleName=hint; AccessibleRole=AccessibleRole.PushButton;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            BackColor=Theme.Surface; ForeColor=Theme.Text; Font=Theme.Body; Cursor=Cursors.Hand;
            Size=new Size(40,36); Margin=new Padding(3); TabStop=false;
        }
        public DialogResult DialogResult { get; set; }
        public void NotifyDefault(bool value) { }
        public void PerformClick() { if(Enabled && Visible) OnClick(EventArgs.Empty); }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Form owner=FindForm();
            if(owner!=null && !owner.IsDisposed && DialogResult!=DialogResult.None) owner.DialogResult=DialogResult;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode==Keys.Space || e.KeyCode==Keys.Enter) { PerformClick(); e.Handled=true; }
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=Primary ? Theme.Accent : Active ? Theme.AccentSoft : hover && Enabled ? Theme.Background : Theme.Surface;
            using (GraphicsPath p=Theme.Rounded(new Rectangle(0,0,Width-1,Height-1),8))
            using (Brush b=new SolidBrush(fill)) e.Graphics.FillPath(b,p);
            Color color=!Enabled ? Color.FromArgb(200,206,216) : Primary ? Color.White : Active ? Theme.Accent : String.IsNullOrEmpty(Glyph) ? ForeColor : Theme.Icon;
            if(!Swatch.IsEmpty)
            { using(Brush b=new SolidBrush(Swatch)) e.Graphics.FillEllipse(b,Width/2-10,Height/2-10,20,20); }
            else if(!String.IsNullOrEmpty(Glyph))
            {
                Rectangle icon=String.IsNullOrEmpty(Text) ? new Rectangle((Width-24)/2,(Height-24)/2,24,24) : new Rectangle(14,(Height-20)/2,20,20);
                Icons.Draw(e.Graphics,Glyph,icon,color);
                if(!String.IsNullOrEmpty(Text)) TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(40,0,Width-46,Height),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            else TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class FloatingPanel : Panel
    {
        internal FloatingPanel()
        { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true); BackColor=Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            for(int i=6;i>=1;i--)
                using(GraphicsPath p=Theme.Rounded(new Rectangle(7-i,9-i,Width-14+i*2,Height-18+i*2),18+i))
                using(Brush b=new SolidBrush(Color.FromArgb(4,24,34,58))) e.Graphics.FillPath(b,p);
            using(GraphicsPath p=Theme.Rounded(new Rectangle(6,5,Width-13,Height-15),18))
            using(Brush b=new SolidBrush(Theme.Surface)) e.Graphics.FillPath(b,p);
            base.OnPaint(e);
        }
    }

    internal sealed class OpacitySlider : Control
    {
        private int value=100;
        internal event EventHandler ValueChanged;
        internal int Value { get { return value; } set { int next=Math.Max(1,Math.Min(100,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty); } }
        internal OpacitySlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Theme.Surface;TabStop=true;AccessibleRole=AccessibleRole.Slider;AccessibleName="Opacidade";Cursor=Cursors.Hand;}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float x=8+(Width-16)*(Value-1)/99f,y=Height/2f;
            using(Pen track=new Pen(Theme.Border,4))e.Graphics.DrawLine(track,8,y,Width-8,y);
            using(Pen active=new Pen(Theme.Accent,4))e.Graphics.DrawLine(active,8,y,x,y);
            using(Brush b=new SolidBrush(Color.White))e.Graphics.FillEllipse(b,x-6,y-6,12,12);
            using(Pen p=new Pen(Theme.Accent,2))e.Graphics.DrawEllipse(p,x-6,y-6,12,12);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
        }
        private void UpdateValue(int x){Value=1+(int)Math.Round(99.0*(x-8)/Math.Max(1,Width-16));}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;UpdateValue(e.X);}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)UpdateValue(e.X);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
        protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Home||key==Keys.End||base.IsInputKey(key);}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value--;if(e.KeyCode==Keys.Right)Value++;if(e.KeyCode==Keys.Home)Value=1;if(e.KeyCode==Keys.End)Value=100;}
    }

    internal static class Icons
    {
        internal static void Draw(Graphics g,string kind,Rectangle bounds,Color color)
        {
            GraphicsState state=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/24f,bounds.Height/24f);
            using(Pen p=new Pen(color,1.35f))
            using(Brush ink=new SolidBrush(color))
            {
                p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
                if(kind=="select")g.FillPolygon(ink,new Point[]{new Point(6,3),new Point(18,12),new Point(12,13),new Point(9,19)});
                else if(kind=="pen") {g.FillPolygon(ink,new Point[]{new Point(5,19),new Point(6,15),new Point(16,5),new Point(19,8),new Point(9,18)});}
                else if(kind=="line")g.DrawLine(p,5,19,19,5);
                else if(kind=="arrow") { g.DrawLine(p,5,19,19,5);g.DrawLines(p,new Point[]{new Point(8,5),new Point(19,5),new Point(19,16)}); }
                else if(kind=="rect")g.DrawRectangle(p,4,5,16,14);
                else if(kind=="ellipse")g.DrawEllipse(p,4,5,16,14);
                else if(kind=="marker") {g.FillPolygon(ink,new Point[]{new Point(7,15),new Point(14,5),new Point(19,8),new Point(12,18)});g.FillPolygon(ink,new Point[]{new Point(6,17),new Point(10,20),new Point(4,20)});g.DrawLine(p,4,22,16,22);}
                else if(kind=="text") { g.DrawLine(p,4,5,20,5);g.DrawLine(p,12,5,12,20);g.DrawLine(p,8,20,16,20); }
                else if(kind=="undo" || kind=="redo")
                { if(kind=="redo") {g.TranslateTransform(24,0);g.ScaleTransform(-1,1);}g.DrawLines(p,new Point[]{new Point(9,5),new Point(4,10),new Point(9,15)});g.DrawLine(p,4,10,14,10);g.DrawArc(p,8,10,12,12,-90,180); }
                else if(kind=="copy") {g.DrawRectangle(p,8,8,12,13);g.DrawLines(p,new Point[]{new Point(16,8),new Point(16,3),new Point(3,3),new Point(3,16),new Point(8,16)});}
                else if(kind=="save") {g.DrawLines(p,new Point[]{new Point(7,10),new Point(12,15),new Point(17,10)});g.DrawLine(p,12,3,12,15);g.DrawLines(p,new Point[]{new Point(4,16),new Point(4,21),new Point(20,21),new Point(20,16)});}
                else if(kind=="close") {g.DrawLine(p,6,6,18,18);g.DrawLine(p,18,6,6,18);}
                else if(kind=="crop") {g.DrawLines(p,new Point[]{new Point(6,3),new Point(6,18),new Point(21,18)});g.DrawLines(p,new Point[]{new Point(3,6),new Point(18,6),new Point(18,21)});}
                else if(kind=="check")g.DrawLines(p,new Point[]{new Point(5,12),new Point(9,16),new Point(19,6)});
            }
            g.Restore(state);
        }
    }
}
