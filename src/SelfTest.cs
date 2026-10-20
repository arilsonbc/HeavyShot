using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace HeavyShot
{
    internal static class SelfTest
    {
        private static int checks;
        private static readonly StringBuilder log=new StringBuilder();
        private static void Check(bool condition,string message)
        { if(!condition) throw new Exception("FAIL: "+message); checks++; log.AppendLine("PASS: "+message); }
        internal static Bitmap Demo(Size size)
        {
            Bitmap bmp=new Bitmap(size.Width,size.Height);
            using(Graphics g=Graphics.FromImage(bmp))
            {
                using(LinearGradientBrush brush=new LinearGradientBrush(new Rectangle(Point.Empty,size),Color.FromArgb(26,45,72),Color.FromArgb(75,61,110),30)) g.FillRectangle(brush,0,0,size.Width,size.Height);
                using(Brush b=new SolidBrush(Color.FromArgb(246,248,252))) g.FillRectangle(b,60,70,Math.Max(400,Math.Min(960,size.Width-120)),Math.Max(330,Math.Min(620,size.Height-140)));
                using(Font title=new Font("Segoe UI",30,FontStyle.Bold)) g.DrawString("Uma ideia merece destaque.",title,Brushes.MidnightBlue,100,130);
                using(Font body=new Font("Segoe UI",15))
                {
                    g.DrawString("Selecione uma área e transforme a captura em uma mensagem clara.",body,Brushes.SlateGray,103,205);
                    g.DrawString("Captura local  •  Anotações  •  PNG / JPEG  •  Clipboard",body,Brushes.SlateGray,103,250);
                }
                Color[] colors={Color.FromArgb(126,157,255),Color.FromArgb(138,201,166),Color.FromArgb(255,202,122)};
                for(int i=0;i<3;i++) using(Brush b=new SolidBrush(colors[i])) g.FillRectangle(b,105+i*240,330,210,170);
                using(Font f=new Font("Segoe UI",16,FontStyle.Bold))
                { g.DrawString("01  Capture",f,Brushes.MidnightBlue,122,353); g.DrawString("02  Anote",f,Brushes.MidnightBlue,362,353); g.DrawString("03  Compartilhe",f,Brushes.MidnightBlue,602,353); }
            }
            return bmp;
        }
        private static Annotation Shape(Tool tool,Point a,Point b,Color c)
        { Annotation item=new Annotation { Tool=tool,Color=c,Width=4,Text="HeavyShot" }; item.Points.Add(a); item.Points.Add(b); return item; }
        private static void Mouse(CaptureForm form,string method,int x,int y)
        { typeof(CaptureForm).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(form,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)}); }
        internal static int Run(string[] args)
        {
            string destination=AppDomain.CurrentDomain.BaseDirectory;
            int index=Array.IndexOf(args,"--test-output"); if(index>=0 && index+1<args.Length) destination=Path.GetFullPath(args[index+1]);
            Directory.CreateDirectory(destination);
            try
            {
                using(HomeForm home=new HomeForm(false,false,true))
                using(Bitmap preview=new Bitmap(home.ClientSize.Width,home.ClientSize.Height))
                {
                    home.Show();Application.DoEvents();home.Controls[0].DrawToBitmap(preview,home.Controls[0].ClientRectangle);home.Hide();
                    preview.Save(Path.Combine(destination,"home-preview.png"),ImageFormat.Png);
                    Check(home.ClientSize==new Size(640,654),"tela inicial redesenhada renderiza sem iniciar atalhos ou bandeja");
                }
                using(ActionButton button=new ActionButton("","Teste sem borda"))
                {
                    button.Size=new Size(40,40);button.Glyph="pen";button.CreateControl();
                    Check(!typeof(ButtonBase).IsAssignableFrom(button.GetType()),"ícones independentes do acabamento nativo de botões");
                    foreach(bool active in new bool[]{false,true})
                    using(Bitmap image=new Bitmap(40,40))
                    {
                        button.Active=active;button.DrawToBitmap(image,new Rectangle(0,0,40,40));
                        bool clean=true;
                        for(int x=0;x<40;x++)for(int y=0;y<40;y++)
                            if(x<8 || x>=32 || y<8 || y>=32)
                            {Color pixel=image.GetPixel(x,y);if(pixel.R<200 || pixel.G<200 || pixel.B<200)clean=false;}
                        Check(clean,"ausência de contorno escuro ao redor do ícone: ativo="+active);
                    }
                    int clicks=0;button.Click+=delegate{clicks++;};button.PerformClick();button.Enabled=false;button.PerformClick();
                    Check(clicks==1,"botão personalizado respeita clique e estado desabilitado");
                }
                Check(Geometry.Between(new Point(80,70),new Point(10,20))==new Rectangle(10,20,70,50),"seleção em direção inversa");
                using(CaptureDocument d=new CaptureDocument(new Bitmap(300,200)))
                {
                    using(Graphics g=Graphics.FromImage(d.Background)) { g.Clear(Color.White); g.FillRectangle(Brushes.Blue,70,60,15,15); }
                    d.Selection=new Rectangle(50,40,100,80);
                    using(Bitmap crop=d.RenderSelection())
                    { Check(crop.Size==new Size(100,80),"recorte sem borda ou barra de ferramentas"); Check(crop.GetPixel(22,22).ToArgb()==Color.Blue.ToArgb(),"origem não nula preserva pixels do recorte"); }
                    d.Add(Shape(Tool.Line,new Point(60,90),new Point(140,90),Color.Red));
                    using(Bitmap crop=d.RenderSelection()) Check(crop.GetPixel(40,50).R>240 && crop.GetPixel(40,50).G<30,"anotação deslocada para coordenadas do recorte");
                    d.Undo(); Check(d.Items.Count==0 && d.CanRedo,"desfazer"); d.Redo(); Check(d.Items.Count==1 && !d.CanRedo,"refazer");
                    d.Undo(); d.Add(Shape(Tool.Rectangle,new Point(60,50),new Point(120,100),Color.Red)); Check(!d.CanRedo,"nova anotação invalida refazer");
                    d.Selection=new Rectangle(-10,-10,40,30); using(Bitmap crop=d.RenderSelection()) Check(crop.Size==new Size(30,20),"recorte limitado à imagem");
                    d.Selection=Rectangle.Empty; bool rejected=false; try { using(Bitmap crop=d.RenderSelection()) {} } catch(InvalidOperationException) { rejected=true; } Check(rejected,"seleção vazia rejeitada");
                }
                foreach(Tool tool in new Tool[]{Tool.Pen,Tool.Marker,Tool.Line,Tool.Arrow,Tool.Rectangle,Tool.Ellipse,Tool.Text})
                {
                    using(CaptureDocument d=new CaptureDocument(new Bitmap(220,160)))
                    {
                        using(Graphics g=Graphics.FromImage(d.Background)) g.Clear(Color.White);
                        d.Selection=new Rectangle(0,0,220,160); d.Add(Shape(tool,new Point(20,25),new Point(140,100),Color.Red));
                        using(Bitmap result=d.RenderSelection())
                        {
                            int changed=0; for(int x=0;x<result.Width;x+=2) for(int y=0;y<result.Height;y+=2) if(result.GetPixel(x,y).ToArgb()!=Color.White.ToArgb()) changed++;
                            Check(changed>5,"renderização "+tool);
                        }
                    }
                }
                using(CaptureDocument alphaDoc=new CaptureDocument(new Bitmap(100,100)))
                {
                    using(Graphics g=Graphics.FromImage(alphaDoc.Background))g.Clear(Color.White);
                    alphaDoc.Selection=new Rectangle(0,0,100,100);
                    Annotation translucent=Shape(Tool.Line,new Point(10,50),new Point(90,50),Color.Red);translucent.Opacity=50;alphaDoc.Add(translucent);
                    using(Bitmap b=alphaDoc.RenderSelection())Check(b.GetPixel(50,50).R>240 && b.GetPixel(50,50).G>110 && b.GetPixel(50,50).G<150,"opacidade aplicada à imagem exportada");
                }
                Settings editorSettings=new Settings{Persist=false};
                using(CaptureDocument d=new CaptureDocument(Demo(new Size(1200,800))))
                using(CaptureForm form=new CaptureForm(d,new Rectangle(0,0,1200,800),editorSettings,false))
                {
                    form.CreateControl();
                    Mouse(form,"OnMouseDown",900,600); Mouse(form,"OnMouseMove",100,120); Mouse(form,"OnMouseUp",100,120);
                    Check(d.Selection==new Rectangle(100,120,800,480),"fluxo de arrastar seleção na interface");
                    Mouse(form,"OnMouseDown",400,300); Mouse(form,"OnMouseMove",420,310); Mouse(form,"OnMouseUp",420,310);
                    Check(d.Selection==new Rectangle(120,130,800,480),"mover seleção");
                    typeof(CaptureForm).GetMethod("SetTool",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(form,new object[]{Tool.Arrow});
                    Mouse(form,"OnMouseDown",200,260); Mouse(form,"OnMouseMove",350,350); Mouse(form,"OnMouseUp",350,350);
                    Check(d.Items.Count==1 && d.Items[0].Tool==Tool.Arrow,"fluxo de desenho da seta na interface");
                    d.Add(Shape(Tool.Rectangle,new Point(550,325),new Point(800,505),Color.FromArgb(255,89,94)));
                    using(Bitmap rendered=d.RenderSelection())
                    {
                        string png=Path.Combine(destination,"sample-capture.png"),jpg=Path.Combine(destination,"sample-capture.jpg");
                        rendered.Save(png,ImageFormat.Png); rendered.Save(jpg,ImageFormat.Jpeg);
                        using(Bitmap roundtrip=new Bitmap(png)) Check(roundtrip.Size==d.Selection.Size,"PNG exportado reabre com dimensões exatas");
                        using(Bitmap roundtrip=new Bitmap(jpg)) Check(roundtrip.Size==d.Selection.Size,"JPEG exportado reabre com dimensões exatas");
                    }
                    form.Show(); Application.DoEvents(); form.RefreshState(); Application.DoEvents();
                    Panel toolbar=(Panel)typeof(CaptureForm).GetField("toolbar",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                    Panel actions=(Panel)typeof(CaptureForm).GetField("actions",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                    Panel palette=(Panel)typeof(CaptureForm).GetField("options",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                    Check(toolbar.Height>toolbar.Width*5,"barra de ferramentas vertical");
                    Rectangle viewport=new Rectangle(0,0,1200,800);
                    foreach(Rectangle area in new Rectangle[]{new Rectangle(5,5,150,140),new Rectangle(1000,580,195,215),new Rectangle(0,0,1200,800)})
                    {
                        d.Selection=area;form.PositionPanels(viewport);
                        Check(viewport.Contains(toolbar.Bounds) && viewport.Contains(actions.Bounds),"barras visíveis junto às bordas: "+area);
                        Check(!toolbar.Bounds.IntersectsWith(actions.Bounds),"barras não se sobrepõem: "+area);
                    }
                    d.Selection=new Rectangle(120,130,800,480);form.RefreshState();
                    using(Bitmap preview=new Bitmap(toolbar.Width,toolbar.Height)) { toolbar.DrawToBitmap(preview,toolbar.ClientRectangle); preview.Save(Path.Combine(destination,"toolbar-preview.png"),ImageFormat.Png); }
                    using(Bitmap preview=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(preview,form.ClientRectangle); preview.Save(Path.Combine(destination,"editor-preview.png"),ImageFormat.Png); }
                    ActionButton color=(ActionButton)typeof(CaptureForm).GetField("colorButton",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
                    color.PerformClick();Application.DoEvents();Check(palette.Visible,"painel de cor abre pelo botão");
                    Check(viewport.Contains(palette.Bounds),"painel de ajustes permanece dentro da tela");
                    Check(!palette.Bounds.IntersectsWith(actions.Bounds),"painel de ajustes não encobre barra de ações");
                    OpacitySlider alpha=null;NumericUpDown width=null;
                    foreach(Control c in palette.Controls){if(c is OpacitySlider)alpha=(OpacitySlider)c;if(c is NumericUpDown)width=(NumericUpDown)c;}
                    alpha.Value=45;width.Value=6;
                    Check(editorSettings.Opacity==45 && editorSettings.Stroke==6,"controles alteram opacidade e espessura");
                    using(Bitmap preview=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(preview,form.ClientRectangle);preview.Save(Path.Combine(destination,"palette-preview.png"),ImageFormat.Png);}
                    color.PerformClick();Check(!palette.Visible,"painel de cor fecha pelo botão");
                    form.Hide();
                }
                log.AppendLine("TOTAL: "+checks+" verificações aprovadas."); File.WriteAllText(Path.Combine(destination,"test-results.txt"),log.ToString(),Encoding.UTF8); return 0;
            }
            catch(Exception ex) { log.AppendLine(ex.ToString()); File.WriteAllText(Path.Combine(destination,"test-results.txt"),log.ToString(),Encoding.UTF8); return 1; }
        }
    }
}
