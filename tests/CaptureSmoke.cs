using System;
using System.Drawing;
using System.Reflection;
class CaptureSmoke
{
    [STAThread] static int Main(string[] args)
    {
        try
        {
            Assembly assembly=Assembly.LoadFrom(args[0]);
            Type native=assembly.GetType("HeavyShot.Native",true);
            native.GetMethod("EnableDpi",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            foreach(bool cursor in new bool[]{false,true})
            {
                object[] values={cursor,Rectangle.Empty};
                using(Bitmap result=(Bitmap)native.GetMethod("CaptureDesktop",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,values))
                {
                    Rectangle bounds=(Rectangle)values[1];
                    if(result.Size!=bounds.Size || result.Width<1 || result.Height<1) throw new Exception("Dimensões inválidas");
                    int first=result.GetPixel(0,0).ToArgb(),different=0,minAlpha=255;
                    for(int y=0;y<result.Height;y+=47) for(int x=0;x<result.Width;x+=47) { Color pixel=result.GetPixel(x,y); if(pixel.ToArgb()!=first) different++; minAlpha=Math.Min(minAlpha,pixel.A); }
                    if(different==0) throw new Exception("Imagem uniforme: captura não corroborada");
                    if(minAlpha!=255) throw new Exception("Captura contém alpha inesperado: "+minAlpha);
                    Console.WriteLine("PASS desktop capture: "+result.Width+"x"+result.Height+", cursor="+cursor+", varied samples="+different+", alpha="+minAlpha);
                }
            }
            return 0;
        }
        catch(Exception ex) { Console.WriteLine(ex.ToString()); return 1; }
    }
}
