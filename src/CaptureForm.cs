using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace HeavyShot
{
    internal sealed class CaptureForm : Form
    {
        private readonly CaptureDocument document;
        private readonly Settings settings;
        private readonly Rectangle desktop;
        private readonly Panel toolbar=new Panel();
        private readonly Panel options=new Panel();
        private readonly ToolTip tips=new ToolTip();
        private readonly Dictionary<Tool,ActionButton> tools=new Dictionary<Tool,ActionButton>();
        private ActionButton undo,redo,colorButton;
        private Tool tool=Tool.Select;
        private Point anchor;
        private Rectangle original;
        private bool selecting,moving,resizing;
        private int resizeHandle=-1;
        private Annotation draft;
        private Color ink;
        private float stroke;
        private readonly bool demo;

        internal CaptureForm(CaptureDocument doc,Rectangle bounds,Settings settings,bool demo)
        {
            document=doc; desktop=bounds; this.settings=settings; this.demo=demo;
            ink=Color.FromArgb(settings.Ink); stroke=Math.Max(1,Math.Min(12,settings.Stroke));
            Text="HeavyShot — captura"; FormBorderStyle=FormBorderStyle.None;
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.Manual; Bounds=bounds;
            TopMost=true; ShowInTaskbar=false; DoubleBuffered=true; KeyPreview=true; Cursor=Cursors.Cross;
            SetStyle(ControlStyles.ResizeRedraw,true);
            BuildToolbar();
            if (demo) { document.Selection=new Rectangle(100,120,Math.Min(850,bounds.Width-180),Math.Min(520,bounds.Height-230)); tool=Tool.Arrow; }
        }
        private void BuildToolbar()
        {
            toolbar.BackColor=Theme.Background; toolbar.Size=new Size(552,48); toolbar.Visible=false;
            toolbar.Padding=new Padding(5); Controls.Add(toolbar);
            Tool[] kinds={Tool.Select,Tool.Pen,Tool.Marker,Tool.Line,Tool.Arrow,Tool.Rectangle,Tool.Ellipse,Tool.Text};
            string[] icons={"↖","✎","▰","╱","↗","□","○","T"};
            string[] names={"Selecionar / mover (V)","Caneta (P)","Marcador (M)","Linha (L)","Seta (A)","Retângulo (R)","Elipse (E)","Texto (T)"};
            for(int i=0;i<kinds.Length;i++)
            {
                Tool choice=kinds[i]; ActionButton b=AddButton(toolbar,icons[i],names[i],6+i*41,6,38);
                b.Click+=delegate { SetTool(choice); }; tools.Add(choice,b);
            }
            AddButton(toolbar,"↶","Desfazer (Ctrl+Z)",340,6,38).Click+=delegate { document.Undo(); RefreshState(); };
            undo=(ActionButton)toolbar.Controls[toolbar.Controls.Count-1];
            redo=AddButton(toolbar,"↷","Refazer (Ctrl+Y)",381,6,38); redo.Click+=delegate { document.Redo(); RefreshState(); };
            ActionButton copy=AddButton(toolbar,"Copiar","Copiar imagem (Ctrl+C / Enter)",425,6,66); copy.Primary=true; copy.Click+=delegate { CopyImage(); };
            AddButton(toolbar,"×","Cancelar (Esc)",498,6,42).Click+=delegate { Close(); };

            options.Size=new Size(354,45); options.BackColor=Theme.Background; options.Visible=false; Controls.Add(options);
            colorButton=AddButton(options,"●","Escolher cor",5,5,36); colorButton.ForeColor=ink; colorButton.Click+=delegate { PickColor(); };
            int[] colors={unchecked((int)0xffff595e),unchecked((int)0xffffca3a),unchecked((int)0xff8ac926),unchecked((int)0xff1982c4),unchecked((int)0xffffffff)};
            for(int i=0;i<colors.Length;i++)
            {
                Color c=Color.FromArgb(colors[i]); ActionButton swatch=AddButton(options,"●","Cor "+c.Name,45+i*27,5,24); swatch.ForeColor=c;
                swatch.Click+=delegate { ink=c; RememberStyle(); };
            }
            NumericUpDown width=new NumericUpDown { Minimum=1,Maximum=12,Value=(decimal)stroke,Location=new Point(185,10),Size=new Size(45,26),BackColor=Theme.Surface,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Espessura" };
            width.ValueChanged+=delegate { stroke=(float)width.Value; RememberStyle(); }; options.Controls.Add(width); tips.SetToolTip(width,"Espessura do traço");
            AddButton(options,"Salvar","Salvar imagem (Ctrl+S)",239,5,68).Click+=delegate { SaveImage(); };
            AddButton(options,"⛶","Selecionar tudo (Ctrl+A)",311,5,36).Click+=delegate { document.Selection=new Rectangle(Point.Empty,document.Background.Size); RefreshState(); };
        }
        private ActionButton AddButton(Panel parent,string text,string hint,int x,int y,int width)
        {
            ActionButton b=new ActionButton(text,hint); b.SetBounds(x,y,width,34);
            parent.Controls.Add(b); tips.SetToolTip(b,hint); return b;
        }
        private void RememberStyle() { settings.Ink=ink.ToArgb(); settings.Stroke=(int)stroke; colorButton.ForeColor=ink; colorButton.Invalidate(); settings.Save(); }
        private void PickColor()
        { using(ColorDialog d=new ColorDialog { Color=ink,FullOpen=true }) if(d.ShowDialog(this)==DialogResult.OK) { ink=d.Color; RememberStyle(); } }
        private void SetTool(Tool next) { tool=next; Cursor=next==Tool.Select ? Cursors.SizeAll : next==Tool.Text ? Cursors.IBeam : Cursors.Cross; RefreshState(); }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Activate(); Focus(); RefreshState(); }
        internal void RefreshState()
        {
            bool selected=document.Selection.Width>=2 && document.Selection.Height>=2;
            toolbar.Visible=options.Visible=selected && !selecting && !moving && !resizing;
            foreach(KeyValuePair<Tool,ActionButton> pair in tools) { pair.Value.Active=pair.Key==tool; pair.Value.Invalidate(); }
            undo.Enabled=document.CanUndo; redo.Enabled=document.CanRedo;
            if(selected) PlaceToolbar(); Invalidate();
        }
        private void PlaceToolbar()
        {
            Rectangle screen=Screen.FromRectangle(new Rectangle(document.Selection.X+desktop.X,document.Selection.Y+desktop.Y,document.Selection.Width,document.Selection.Height)).Bounds;
            screen.Offset(-desktop.X,-desktop.Y);
            int x=Math.Max(screen.Left+8,Math.Min(document.Selection.Right-toolbar.Width,screen.Right-toolbar.Width-8));
            int y=document.Selection.Bottom+10;
            if(y+102>screen.Bottom) y=Math.Max(screen.Top+8,document.Selection.Top-104);
            toolbar.Location=new Point(x,y); options.Location=new Point(x+Math.Max(0,toolbar.Width-options.Width),y+53);
            toolbar.BringToFront(); options.BringToFront();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics; g.DrawImageUnscaled(document.Background,0,0);
            Rectangle r=document.Selection;
            if(r.Width>=2 && r.Height>=2)
            {
                GraphicsState state=g.Save(); g.SetClip(r);
                foreach(Annotation item in document.Items) item.Draw(g);
                if(draft!=null) draft.Draw(g); g.Restore(state);
            }
            using(Brush shade=new SolidBrush(Color.FromArgb(145,8,11,18)))
            {
                if(r.Width<2 || r.Height<2) g.FillRectangle(shade,ClientRectangle);
                else
                {
                    g.FillRectangle(shade,0,0,Width,r.Top);
                    g.FillRectangle(shade,0,r.Bottom,Width,Math.Max(0,Height-r.Bottom));
                    g.FillRectangle(shade,0,r.Top,r.Left,r.Height);
                    g.FillRectangle(shade,r.Right,r.Top,Math.Max(0,Width-r.Right),r.Height);
                }
            }
            if(r.Width>=2 && r.Height>=2)
            {
                using(Pen border=new Pen(Theme.Accent,1)) g.DrawRectangle(border,r.X,r.Y,r.Width-1,r.Height-1);
                foreach(Rectangle handle in Handles(r)) { using(Brush b=new SolidBrush(Color.White)) g.FillRectangle(b,handle); }
                string text=r.Width+" × "+r.Height+" px";
                Size s=TextRenderer.MeasureText(text,Theme.Body);
                Rectangle badge=new Rectangle(r.Left,Math.Max(0,r.Top-29),s.Width+16,25);
                using(Brush b=new SolidBrush(Theme.Background)) g.FillRectangle(b,badge);
                TextRenderer.DrawText(g,text,Theme.Body,badge,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            else
            {
                Rectangle display=Screen.FromPoint(Cursor.Position).Bounds;
                display.Offset(-desktop.X,-desktop.Y);
                string title="Arraste para selecionar uma área";
                Rectangle message=new Rectangle(display.Left+Math.Max(0,(display.Width-600)/2),display.Top+Math.Max(25,display.Height/2-36),600,72);
                using(Brush b=new SolidBrush(Theme.Background)) g.FillRectangle(b,message);
                using(Font titleFont=new Font("Segoe UI",18)) TextRenderer.DrawText(g,title,titleFont,new Rectangle(message.X,message.Y+8,600,34),Color.White,TextFormatFlags.HorizontalCenter);
                TextRenderer.DrawText(g,"HEAVYSHOT   •   Ctrl+A: tela inteira   •   Esc: cancelar",Theme.Body,new Rectangle(message.X,message.Y+45,600,24),Theme.Muted,TextFormatFlags.HorizontalCenter);
            }
        }
        private Rectangle[] Handles(Rectangle r)
        {
            int cx=r.Left+r.Width/2,cy=r.Top+r.Height/2;
            Point[] points={new Point(r.Left,r.Top),new Point(cx,r.Top),new Point(r.Right,r.Top),new Point(r.Right,cy),new Point(r.Right,r.Bottom),new Point(cx,r.Bottom),new Point(r.Left,r.Bottom),new Point(r.Left,cy)};
            Rectangle[] result=new Rectangle[8]; for(int i=0;i<8;i++) result[i]=new Rectangle(points[i].X-3,points[i].Y-3,7,7); return result;
        }
        private Point ClampPoint(Point p) { return new Point(Math.Max(0,Math.Min(Width,p.X)),Math.Max(0,Math.Min(Height,p.Y))); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button==MouseButtons.Right) { SetTool(Tool.Select); return; }
            if(e.Button!=MouseButtons.Left) return;
            anchor=ClampPoint(e.Location); original=document.Selection;
            if(tool==Tool.Select && original.Width>=2)
            {
                Rectangle[] handles=Handles(original);
                for(int i=0;i<handles.Length;i++) if(handles[i].Contains(e.Location)) { resizing=true; resizeHandle=i; break; }
            }
            if(resizing) { Capture=true; RefreshState(); return; }
            if(original.Width<2 || original.Height<2 || tool==Tool.Select)
            {
                moving=original.Contains(e.Location) && original.Width>=2;
                selecting=!moving;
                if(selecting) document.Selection=new Rectangle(anchor,Size.Empty);
                Capture=true; RefreshState(); return;
            }
            if(!original.Contains(e.Location)) return;
            if(tool==Tool.Text)
            {
                using(TextPrompt prompt=new TextPrompt())
                {
                    if(prompt.ShowDialog(this)==DialogResult.OK && !String.IsNullOrWhiteSpace(prompt.Value))
                    { Annotation text=new Annotation { Tool=tool,Color=ink,Width=stroke,Text=prompt.Value }; text.Points.Add(anchor); document.Add(text); RefreshState(); }
                }
                return;
            }
            draft=new Annotation { Tool=tool,Color=ink,Width=stroke }; draft.Points.Add(anchor); Capture=true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e); Point p=ClampPoint(e.Location);
            if(selecting) document.Selection=Geometry.Between(anchor,p);
            else if(moving)
            {
                document.Selection=new Rectangle(Math.Max(0,Math.Min(Width-original.Width,original.X+p.X-anchor.X)),Math.Max(0,Math.Min(Height-original.Height,original.Y+p.Y-anchor.Y)),original.Width,original.Height);
            }
            else if(resizing)
            {
                int left=original.Left,top=original.Top,right=original.Right,bottom=original.Bottom;
                if(resizeHandle==0 || resizeHandle==6 || resizeHandle==7) left=p.X;
                if(resizeHandle==2 || resizeHandle==3 || resizeHandle==4) right=p.X;
                if(resizeHandle==0 || resizeHandle==1 || resizeHandle==2) top=p.Y;
                if(resizeHandle==4 || resizeHandle==5 || resizeHandle==6) bottom=p.Y;
                document.Selection=Geometry.Between(new Point(left,top),new Point(right,bottom));
            }
            else if(draft!=null)
            {
                if(draft.Tool==Tool.Pen || draft.Tool==Tool.Marker) draft.Points.Add(p);
                else { if(draft.Points.Count>1) draft.Points.RemoveAt(1); draft.Points.Add(p); }
            }
            if(selecting || moving || resizing || draft!=null) Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e); if(e.Button!=MouseButtons.Left) return;
            if(draft!=null) { document.Add(draft); draft=null; }
            selecting=moving=resizing=false; resizeHandle=-1; Capture=false; RefreshState();
        }
        private bool Ready { get { return document.Selection.Width>=2 && document.Selection.Height>=2; } }
        private void CopyImage()
        {
            if(!Ready) return;
            try
            {
                using(Bitmap result=document.RenderSelection())
                {
                    DataObject data=new DataObject(); data.SetData(DataFormats.Bitmap,true,result);
                    Clipboard.SetDataObject(data,true,5,100);
                }
                Close();
            }
            catch(Exception ex) { MessageBox.Show(this,"Não foi possível copiar. Tente novamente ou salve a imagem.\n"+ex.Message,"HeavyShot"); }
        }
        private void SaveImage()
        {
            if(!Ready) return;
            using(SaveFileDialog dialog=new SaveFileDialog())
            {
                dialog.Title="Salvar captura — HeavyShot"; dialog.Filter="Imagem PNG|*.png|Imagem JPEG|*.jpg|Bitmap|*.bmp";
                dialog.FileName="HeavyShot-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+".png";
                dialog.DefaultExt="png"; dialog.AddExtension=true; dialog.OverwritePrompt=true;
                if(Directory.Exists(settings.LastDirectory)) dialog.InitialDirectory=settings.LastDirectory;
                if(dialog.ShowDialog(this)!=DialogResult.OK) return;
                try
                {
                    using(Bitmap result=document.RenderSelection())
                    {
                        string ext=Path.GetExtension(dialog.FileName).ToLowerInvariant();
                        result.Save(dialog.FileName,ext==".jpg" || ext==".jpeg" ? ImageFormat.Jpeg : ext==".bmp" ? ImageFormat.Bmp : ImageFormat.Png);
                    }
                    settings.LastDirectory=Path.GetDirectoryName(dialog.FileName); settings.Save(); Close();
                }
                catch(Exception ex) { MessageBox.Show(this,"Não foi possível salvar a imagem.\n"+ex.Message,"HeavyShot"); }
            }
        }
        protected override bool ProcessCmdKey(ref Message msg,Keys key)
        {
            if(key==Keys.Escape) { Close(); return true; }
            if(key==(Keys.Control|Keys.C) || key==Keys.Enter) { CopyImage(); return true; }
            if(key==(Keys.Control|Keys.S)) { SaveImage(); return true; }
            if(key==(Keys.Control|Keys.Z)) { document.Undo(); RefreshState(); return true; }
            if(key==(Keys.Control|Keys.Y) || key==(Keys.Control|Keys.Shift|Keys.Z)) { document.Redo(); RefreshState(); return true; }
            if(key==(Keys.Control|Keys.A)) { document.Selection=new Rectangle(Point.Empty,document.Background.Size); RefreshState(); return true; }
            if(key==Keys.V) SetTool(Tool.Select); else if(key==Keys.P) SetTool(Tool.Pen); else if(key==Keys.M) SetTool(Tool.Marker);
            else if(key==Keys.L) SetTool(Tool.Line); else if(key==Keys.A) SetTool(Tool.Arrow); else if(key==Keys.R) SetTool(Tool.Rectangle);
            else if(key==Keys.E) SetTool(Tool.Ellipse); else if(key==Keys.T) SetTool(Tool.Text); else return base.ProcessCmdKey(ref msg,key);
            return true;
        }
        protected override void Dispose(bool disposing) { if(disposing) tips.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class TextPrompt : Form
    {
        private readonly TextBox input=new TextBox();
        internal string Value { get { return input.Text; } }
        internal TextPrompt()
        {
            Text="HeavyShot — adicionar texto"; ClientSize=new Size(430,190); StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog; MinimizeBox=MaximizeBox=false; TopMost=true;
            BackColor=Theme.Background; ForeColor=Color.White; Font=Theme.Body;
            input.SetBounds(20,22,390,100); input.Multiline=true; input.AcceptsReturn=true; input.BackColor=Theme.Surface; input.ForeColor=Color.White; input.BorderStyle=BorderStyle.FixedSingle; Controls.Add(input);
            ActionButton ok=new ActionButton("Adicionar","Adicionar texto"); ok.SetBounds(285,140,125,34); ok.Primary=true; ok.DialogResult=DialogResult.OK; Controls.Add(ok);
            ActionButton cancel=new ActionButton("Cancelar","Cancelar texto"); cancel.SetBounds(150,140,125,34); cancel.DialogResult=DialogResult.Cancel; Controls.Add(cancel); CancelButton=cancel;
            Shown+=delegate { input.Focus(); };
        }
    }
}
