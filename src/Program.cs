using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

[assembly: AssemblyTitle("HeavyShot")]
[assembly: AssemblyDescription("Captura e anotação portátil para Windows")]
[assembly: AssemblyCompany("HeavyShot")]
[assembly: AssemblyProduct("HeavyShot")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]

namespace HeavyShot
{
    public sealed class Settings
    {
        public bool CaptureCursor;
        public bool UsePrintScreen;
        public string LastDirectory="";
        public int Ink=Color.FromArgb(241,92,98).ToArgb();
        public int Stroke=3;
        public int Opacity=100;
        internal bool Persist=true;
        public static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"HeavyShot.settings.xml"); } }
        public static Settings Load()
        {
            try { using (FileStream f=File.OpenRead(FilePath)) return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(f); }
            catch { return new Settings(); }
        }
        public bool Save()
        {
            if(!Persist)return true;
            try
            {
                string temp=FilePath+".tmp";
                using (FileStream f=File.Create(temp)) new XmlSerializer(typeof(Settings)).Serialize(f,this);
                if (File.Exists(FilePath)) File.Replace(temp,FilePath,null); else File.Move(temp,FilePath);
                return true;
            }
            catch { return false; }
        }
    }

    internal static class Program
    {
        internal static readonly string Version="0.2.0";
        [STAThread] private static int Main(string[] args)
        {
            Native.EnableDpi(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (Array.IndexOf(args,"--self-test")>=0) return SelfTest.Run(args);
            bool owned;
            using (Mutex mutex=new Mutex(true,"Local\\HeavyShot.Portable",out owned))
            {
                if (!owned) { MessageBox.Show("O HeavyShot já está aberto. Use Ctrl+Shift+S ou o ícone na bandeja.","HeavyShot"); return 0; }
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { MessageBox.Show(e.Exception.Message,"HeavyShot — erro",MessageBoxButtons.OK,MessageBoxIcon.Error); };
                Application.Run(new HomeForm(Array.IndexOf(args,"--tray")>=0,Array.IndexOf(args,"--demo")>=0));
                mutex.ReleaseMutex();
            }
            return 0;
        }
    }

    internal sealed class HomeForm : Form
    {
        private readonly Settings settings=Settings.Load();
        private readonly bool trayStart, demo;
        private bool exiting, capturing, shortcut;
        private NotifyIcon tray;
        private Label status;
        private CheckBox print;
        private readonly bool previewMode;
        internal HomeForm(bool trayStart,bool demo,bool previewMode=false)
        {
            this.previewMode=previewMode;if(previewMode)settings.Persist=false;
            this.trayStart=trayStart; this.demo=demo;
            Text="HeavyShot"; ClientSize=new Size(640,654); AutoScaleMode=AutoScaleMode.Dpi;
            FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
            BackColor=Theme.Background; ForeColor=Theme.Text; Font=Theme.Body;
            try { Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon=SystemIcons.Application; }
            HomeSurface surface=new HomeSurface { Dock=DockStyle.Fill };Controls.Add(surface);
            surface.Controls.Add(Theme.Label("HeavyShot",76,27,230,35,18,Color.Black));
            surface.Controls.Add(Theme.Label("PORTABLE",511,34,97,22,9,Theme.Muted));
            Label headline=Theme.Label("Sua tela.\nSua mensagem.",30,110,400,130,30,Color.Black);
            headline.Font=new Font("Segoe UI",30,FontStyle.Bold);surface.Controls.Add(headline);
            surface.Controls.Add(Theme.Label("Capture, anote e compartilhe em segundos.",33,244,570,26,11,Theme.Muted));
            ActionButton capture=new ActionButton("Nova captura      Ctrl + Shift + S","Nova captura");
            capture.SetBounds(32,286,576,60);capture.Primary=true;capture.Font=new Font("Segoe UI",12,FontStyle.Bold);
            capture.Click+=delegate { BeginCapture(); };surface.Controls.Add(capture);
            surface.Controls.Add(Theme.Label("Selecione uma área. O editor aparece na hora.",33,354,560,24,10,Theme.Muted));
            surface.Controls.Add(Theme.Label("Capturar o cursor",52,401,460,24,11,Color.Black));
            surface.Controls.Add(Theme.Label("Inclua o ponteiro na imagem",52,427,460,20,9,Theme.Muted));
            ToggleSwitch cursor=new ToggleSwitch { Checked=settings.CaptureCursor,Location=new Point(544,409),BackColor=Color.FromArgb(247,247,247),AccessibleName="Incluir o cursor na captura" };
            cursor.CheckedChanged+=delegate { settings.CaptureCursor=cursor.Checked; SaveSettings(); };surface.Controls.Add(cursor);
            surface.Controls.Add(Theme.Label("Atalho Print Screen",52,469,460,24,11,Color.Black));
            surface.Controls.Add(Theme.Label("Use também a tecla de captura do teclado",52,495,460,20,9,Theme.Muted));
            print=new ToggleSwitch { Checked=settings.UsePrintScreen,Location=new Point(544,477),BackColor=Color.FromArgb(247,247,247),AccessibleName="Usar também a tecla Print Screen" };
            print.CheckedChanged+=delegate { settings.UsePrintScreen=print.Checked; RegisterShortcuts(); SaveSettings(); };surface.Controls.Add(print);
            status=Theme.Label("",32,562,576,40,9,Theme.Muted);surface.Controls.Add(status);
            surface.Controls.Add(Theme.Label("LOCAL POR NATUREZA",32,611,420,20,8,Theme.Muted));
            surface.Controls.Add(Theme.Label("v"+Program.Version,550,609,70,22,9,Theme.Muted));
            if(previewMode) { status.Text="Pronto para capturar. Disponível também na bandeja.";return; }
            ContextMenuStrip menu=new ContextMenuStrip();
            menu.Items.Add("Nova captura",null,delegate { BeginCapture(); }); menu.Items.Add("Abrir HeavyShot",null,delegate { ShowHome(); });
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Sair",null,delegate { exiting=true; Close(); });
            tray=new NotifyIcon { Icon=Icon,Text="HeavyShot — Ctrl+Shift+S",ContextMenuStrip=menu,Visible=true };
            tray.DoubleClick+=delegate { BeginCapture(); };
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); if(!previewMode)RegisterShortcuts(); }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e); if(previewMode)return; RegisterShortcuts();
            if (trayStart) Hide();
            if (demo) BeginInvoke(new Action(BeginCapture));
        }
        private void SaveSettings()
        { if (!settings.Save()) status.Text="Preferências em memória: extraia o portátil para uma pasta com permissão de escrita."; }
        private void RegisterShortcuts()
        {
            if (!IsHandleCreated) return;
            Native.UnregisterHotKey(Handle,1); Native.UnregisterHotKey(Handle,2);
            shortcut=Native.RegisterHotKey(Handle,1,0x4006,(uint)Keys.S);
            bool printOk=!settings.UsePrintScreen || Native.RegisterHotKey(Handle,2,0x4000,(uint)Keys.PrintScreen);
            if (status!=null) status.Text=!shortcut ? "Ctrl+Shift+S está em uso. Capture pelo botão ou pela bandeja." : !printOk ? "Print Screen está em uso por outro aplicativo. Ctrl+Shift+S está ativo." : "Pronto. Fechar esta janela mantém o HeavyShot na bandeja.";
        }
        protected override void WndProc(ref Message m)
        { if (m.Msg==0x312 && (m.WParam.ToInt32()==1 || m.WParam.ToInt32()==2)) { BeginCapture(); return; } base.WndProc(ref m); }
        private void ShowHome() { Show(); WindowState=FormWindowState.Normal; Activate(); }
        private void BeginCapture()
        {
            if (capturing) return;
            capturing=true; bool restore=Visible; Hide();
            System.Windows.Forms.Timer delay=new System.Windows.Forms.Timer { Interval=180 };
            delay.Tick+=delegate
            {
                delay.Stop(); delay.Dispose();
                try
                {
                    Rectangle bounds; Bitmap bitmap;
                    if (demo) { bounds=Screen.PrimaryScreen.Bounds; bitmap=SelfTest.Demo(bounds.Size); }
                    else bitmap=Native.CaptureDesktop(settings.CaptureCursor,out bounds);
                    using (CaptureDocument document=new CaptureDocument(bitmap))
                    using (CaptureForm overlay=new CaptureForm(document,bounds,settings,demo)) overlay.ShowDialog();
                }
                catch (Exception ex) { MessageBox.Show("Não foi possível capturar a tela.\n"+ex.Message,"HeavyShot",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
                finally { capturing=false; if (restore) ShowHome(); }
            };
            delay.Start();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); return; }
            if (IsHandleCreated) { Native.UnregisterHotKey(Handle,1); Native.UnregisterHotKey(Handle,2); }
            tray.Visible=false; tray.Dispose(); base.OnFormClosing(e);
        }
    }
}
