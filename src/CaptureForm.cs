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
        private readonly FloatingPanel toolbar=new FloatingPanel();
        private readonly FloatingPanel options=new FloatingPanel();
        private readonly FloatingPanel actions=new FloatingPanel();
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
        private int opacity;
        private bool paletteOpen;
        private Label hexLabel,opacityLabel;
        private ComboBox format;
        private readonly bool demo;

        internal CaptureForm(CaptureDocument doc,Rectangle bounds,Settings settings,bool demo)
        {
            document=doc; desktop=bounds; this.settings=settings; this.demo=demo;
            ink=Color.FromArgb(settings.Ink); stroke=Math.Max(1,Math.Min(12,settings.Stroke));
            opacity=Math.Max(1,Math.Min(100,settings.Opacity));
            Text="HeavyShot — captura"; FormBorderStyle=FormBorderStyle.None;
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.Manual; Bounds=bounds;
            TopMost=true; ShowInTaskbar=false; DoubleBuffered=true; KeyPreview=true; Cursor=Cursors.Cross;
            SetStyle(ControlStyles.ResizeRedraw,true);
            BuildToolbar();
            if (demo) { document.Selection=new Rectangle(100,120,Math.Min(850,bounds.Width-180),Math.Min(520,bounds.Height-230)); tool=Tool.Arrow; }
        }
        private void BuildToolbar()
        {
            toolbar.Size=new Size(76,534); toolbar.Visible=false; Controls.Add(toolbar);
            Tool[] kinds={Tool.Select,Tool.Pen,Tool.Line,Tool.Arrow,Tool.Rectangle,Tool.Ellipse,Tool.Marker,Tool.Text};
            string[] icons={"select","pen","line","arrow","rect","ellipse","marker","text"};
            string[] names={"Selecionar / mover (V)","Caneta (P)","Linha (L)","Seta (A)","Retângulo (R)","Elipse (E)","Marcador (M)","Texto (T)"};
            for(int i=0;i<kinds.Length;i++)
            {
                Tool choice=kinds[i]; ActionButton b=AddButton(toolbar,"",names[i],18,14+i*44,40);b.Height=40;b.Glyph=icons[i];
                b.Click+=delegate { SetTool(choice); }; tools.Add(choice,b);
            }
            AddDivider(toolbar,18,368,40,1);
            colorButton=AddButton(toolbar,"","Cor e espessura",18,378,40);colorButton.Height=40;colorButton.Swatch=ink;
            colorButton.Click+=delegate { paletteOpen=!paletteOpen;RefreshState(); };
            AddDivider(toolbar,18,425,40,1);
            undo=AddButton(toolbar,"","Desfazer (Ctrl+Z)",18,435,40);undo.Height=40;undo.Glyph="undo";undo.Click+=delegate { document.Undo();RefreshState(); };
            redo=AddButton(toolbar,"","Refazer (Ctrl+Y)",18,479,40);redo.Height=40;redo.Glyph="redo";redo.Click+=delegate { document.Redo();RefreshState(); };

            actions.Size=new Size(370,72);actions.Visible=false;Controls.Add(actions);
            format=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Theme.Surface,ForeColor=Theme.Text,Font=new Font("Segoe UI",9),AccessibleName="Formato da imagem" };
            format.Items.AddRange(new object[]{"PNG","JPEG","BMP"});format.SelectedIndex=0;format.SetBounds(20,23,65,28);actions.Controls.Add(format);
            AddDivider(actions,96,23,1,24);
            ActionButton save=AddButton(actions,"","Salvar imagem (Ctrl+S)",104,16,40);save.Height=40;save.Glyph="save";save.Click+=delegate{SaveImage();};
            ActionButton copy=AddButton(actions,"Copiar  ↵","Copiar imagem (Ctrl+C / Enter)",154,16,144);copy.Height=40;copy.Glyph="copy";copy.Primary=true;copy.Click+=delegate{CopyImage();};
            AddDivider(actions,307,23,1,24);
            ActionButton close=AddButton(actions,"","Cancelar (Esc)",316,16,36);close.Height=40;close.Glyph="close";close.Click+=delegate{Close();};

            options.Size=new Size(300,296);options.Visible=false;Controls.Add(options);
            options.Controls.Add(Theme.Label("Cor e espessura",26,24,210,26,10,Theme.Text));
            ActionButton dismiss=AddButton(options,"","Fechar painel",254,18,26);dismiss.Glyph="close";dismiss.Click+=delegate{paletteOpen=false;RefreshState();};
            int[] colors={unchecked((int)0xfff15c62),unchecked((int)0xfff4bb47),unchecked((int)0xff46a789),unchecked((int)0xff4268eb),unchecked((int)0xff8e6bd1),unchecked((int)0xff202633)};
            for(int i=0;i<colors.Length;i++)
            {
                Color c=Color.FromArgb(colors[i]); ActionButton swatch=AddButton(options,"","Cor "+c.Name,24+i*40,62,34);swatch.Swatch=c;
                swatch.Click+=delegate { ink=c; RememberStyle(); };
            }
            hexLabel=Theme.Label("#"+ink.R.ToString("X2")+ink.G.ToString("X2")+ink.B.ToString("X2"),26,109,150,24,9,Theme.Muted);options.Controls.Add(hexLabel);
            AddButton(options,"Mais cores…","Escolher outra cor",168,102,108).Click+=delegate{PickColor();};
            options.Controls.Add(Theme.Label("Espessura",26,150,120,22,9,Theme.Muted));
            NumericUpDown width=new NumericUpDown { Minimum=1,Maximum=12,Value=(decimal)stroke,Location=new Point(214,148),Size=new Size(60,26),BackColor=Theme.Surface,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Espessura" };
            width.ValueChanged+=delegate { stroke=(float)width.Value; RememberStyle(); }; options.Controls.Add(width); tips.SetToolTip(width,"Espessura do traço");
            for(int i=0;i<3;i++){int value=new int[]{1,3,6}[i];ActionButton preset=AddButton(options,value+" px","Espessura "+value,26+i*60,180,52);preset.Click+=delegate{width.Value=value;};}
            options.Controls.Add(Theme.Label("Opacidade",26,224,140,22,9,Theme.Muted));opacityLabel=Theme.Label(opacity+"%",220,224,60,22,9,Theme.Text);options.Controls.Add(opacityLabel);
            OpacitySlider alpha=new OpacitySlider{Value=opacity};alpha.SetBounds(24,246,252,28);alpha.ValueChanged+=delegate{opacity=alpha.Value;opacityLabel.Text=opacity+"%";RememberStyle();};options.Controls.Add(alpha);
        }
        private void AddDivider(Panel parent,int x,int y,int w,int h){Panel p=new Panel{BackColor=Theme.Border};p.SetBounds(x,y,w,h);parent.Controls.Add(p);}
        private ActionButton AddButton(Panel parent,string text,string hint,int x,int y,int width)
        {
            ActionButton b=new ActionButton(text,hint); b.SetBounds(x,y,width,34);
            parent.Controls.Add(b); tips.SetToolTip(b,hint); return b;
        }
        private void RememberStyle() { settings.Ink=ink.ToArgb(); settings.Stroke=(int)stroke;settings.Opacity=opacity;colorButton.Swatch=ink;colorButton.Invalidate();if(hexLabel!=null)hexLabel.Text="#"+ink.R.ToString("X2")+ink.G.ToString("X2")+ink.B.ToString("X2");settings.Save(); }
        private void PickColor()
        { using(ColorDialog d=new ColorDialog { Color=ink,FullOpen=true }) if(d.ShowDialog(this)==DialogResult.OK) { ink=d.Color; RememberStyle(); } }
        private void SetTool(Tool next) { tool=next; Cursor=next==Tool.Select ? Cursors.SizeAll : next==Tool.Text ? Cursors.IBeam : Cursors.Cross; RefreshState(); }
        protected override void OnShown(EventArgs e) { base.OnShown(e); Activate(); Focus(); RefreshState(); }
        internal void RefreshState()
        {
            bool selected=document.Selection.Width>=2 && document.Selection.Height>=2;
            toolbar.Visible=actions.Visible=selected && !selecting && !moving && !resizing;
            options.Visible=toolbar.Visible && paletteOpen;
            foreach(KeyValuePair<Tool,ActionButton> pair in tools) { pair.Value.Active=pair.Key==tool; pair.Value.Invalidate(); }
            undo.Enabled=document.CanUndo; redo.Enabled=document.CanRedo;
            if(selected) PlaceToolbar(); Invalidate();
        }
        private void PlaceToolbar()
        {
            Rectangle screen=Screen.FromRectangle(new Rectangle(document.Selection.X+desktop.X,document.Selection.Y+desktop.Y,document.Selection.Width,document.Selection.Height)).Bounds;
            screen.Offset(-desktop.X,-desktop.Y);
            screen=Rectangle.Intersect(screen,ClientRectangle);
            PositionPanels(screen);
        }
        internal void PositionPanels(Rectangle screen)
        {
            Rectangle s=document.Selection;
            int x=s.Right+6;
            if(x+toolbar.Width>screen.Right) x=s.Left-toolbar.Width-6;
            x=Math.Max(screen.Left,Math.Min(x,screen.Right-toolbar.Width));
            int y=Math.Max(screen.Top,Math.Min(s.Top-5,screen.Bottom-toolbar.Height));
            toolbar.Location=new Point(x,y);
            int ax=Math.Max(screen.Left,Math.Min(s.Right-actions.Width+6,screen.Right-actions.Width));
            int ay=s.Bottom+6;
            if(ay+actions.Height>screen.Bottom)ay=s.Top-actions.Height-6;
            ay=Math.Max(screen.Top,Math.Min(ay,screen.Bottom-actions.Height));
            // For a full-screen selection, avoid overlapping the lower toolbar buttons.
            Rectangle actionBounds=new Rectangle(ax,ay,actions.Width,actions.Height);
            if(actionBounds.IntersectsWith(toolbar.Bounds))
            {
                Rectangle[] candidates={new Rectangle(toolbar.Left-actions.Width-6,ay,actions.Width,actions.Height),new Rectangle(toolbar.Right+6,ay,actions.Width,actions.Height),new Rectangle(ax,toolbar.Top-actions.Height-6,actions.Width,actions.Height),new Rectangle(ax,toolbar.Bottom+6,actions.Width,actions.Height)};
                foreach(Rectangle candidate in candidates)if(screen.Contains(candidate) && !candidate.IntersectsWith(toolbar.Bounds)){ax=candidate.X;ay=candidate.Y;break;}
            }
            actions.Location=new Point(ax,ay);
            int px=toolbar.Right+6;
            if(px+options.Width>screen.Right)px=toolbar.Left-options.Width-6;
            px=Math.Max(screen.Left,Math.Min(px,screen.Right-options.Width));
            int py=Math.Max(screen.Top,Math.Min(toolbar.Top+Math.Min(360,toolbar.Height-options.Height),screen.Bottom-options.Height));
            if(new Rectangle(px,py,options.Width,options.Height).IntersectsWith(actions.Bounds))
            { int above=actions.Top-options.Height-6;py=above>=screen.Top?above:Math.Min(screen.Bottom-options.Height,actions.Bottom+6); }
            options.Location=new Point(px,py);
            toolbar.BringToFront();actions.BringToFront();options.BringToFront();
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
            using(Brush shade=new SolidBrush(Color.FromArgb(100,32,38,51)))
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
                foreach(Rectangle handle in Handles(r)) { using(Brush b=new SolidBrush(Color.White))g.FillRectangle(b,handle);using(Pen p=new Pen(Theme.Accent))g.DrawRectangle(p,handle); }
                string text=r.Width+" × "+r.Height+" px";
                Size s=TextRenderer.MeasureText(text,Theme.Body);
                Rectangle badge=new Rectangle(r.Left,Math.Max(0,r.Top-29),s.Width+16,25);
                using(GraphicsPath p=Theme.Rounded(badge,6))using(Brush b=new SolidBrush(Theme.Surface))g.FillPath(b,p);
                TextRenderer.DrawText(g,text,Theme.Body,badge,Theme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                string hint="Esc  cancelar     ·     Ctrl+Z  desfazer     ·     Enter  copiar";
                Size hintSize=TextRenderer.MeasureText(hint,Theme.Body);
                Rectangle help=new Rectangle((Width-hintSize.Width-24)/2,Height-46,hintSize.Width+24,32);
                if(!help.IntersectsWith(toolbar.Bounds) && !help.IntersectsWith(actions.Bounds) && (!options.Visible || !help.IntersectsWith(options.Bounds)))
                {using(GraphicsPath p=Theme.Rounded(help,9))using(Brush b=new SolidBrush(Theme.Surface))g.FillPath(b,p);TextRenderer.DrawText(g,hint,Theme.Body,help,Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
            }
            else
            {
                Rectangle display=Screen.FromPoint(Cursor.Position).Bounds;
                display.Offset(-desktop.X,-desktop.Y);
                string title="Arraste para selecionar uma área";
                Rectangle message=new Rectangle(display.Left+Math.Max(0,(display.Width-600)/2),display.Top+Math.Max(25,display.Height/2-36),600,72);
                using(Brush b=new SolidBrush(Theme.Background)) g.FillRectangle(b,message);
                using(Font titleFont=new Font("Segoe UI",18)) TextRenderer.DrawText(g,title,titleFont,new Rectangle(message.X,message.Y+8,600,34),Theme.Text,TextFormatFlags.HorizontalCenter);
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
                    { Annotation text=new Annotation { Tool=tool,Color=ink,Width=stroke,Text=prompt.Value,Opacity=opacity }; text.Points.Add(anchor); document.Add(text); RefreshState(); }
                }
                return;
            }
            draft=new Annotation { Tool=tool,Color=ink,Width=stroke,Opacity=opacity }; draft.Points.Add(anchor); Capture=true;
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
                string extension=format.SelectedIndex==1?"jpg":format.SelectedIndex==2?"bmp":"png";
                dialog.FilterIndex=format.SelectedIndex+1;dialog.FileName="HeavyShot-"+DateTime.Now.ToString("yyyy-MM-dd-HHmmss")+"."+extension;
                dialog.DefaultExt=extension; dialog.AddExtension=true; dialog.OverwritePrompt=true;
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
            if(key==Keys.Escape) { if(paletteOpen){paletteOpen=false;RefreshState();}else Close(); return true; }
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
            BackColor=Theme.Background; ForeColor=Theme.Text; Font=Theme.Body;
            input.SetBounds(20,22,390,100); input.Multiline=true; input.AcceptsReturn=true; input.BackColor=Theme.Surface; input.ForeColor=Theme.Text; input.BorderStyle=BorderStyle.FixedSingle; Controls.Add(input);
            ActionButton ok=new ActionButton("Adicionar","Adicionar texto"); ok.SetBounds(285,140,125,34); ok.Primary=true; ok.DialogResult=DialogResult.OK; Controls.Add(ok);
            ActionButton cancel=new ActionButton("Cancelar","Cancelar texto"); cancel.SetBounds(150,140,125,34); cancel.DialogResult=DialogResult.Cancel; Controls.Add(cancel); CancelButton=cancel;
            Shown+=delegate { input.Focus(); };
        }
    }
}
