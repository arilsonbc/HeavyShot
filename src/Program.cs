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
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]

namespace HeavyShot
{
    public sealed class Settings
    {
        public bool CaptureCursor;
        public bool UsePrintScreen;
        public string LastDirectory="";
        public int Ink=Color.FromArgb(255,89,94).ToArgb();
        public int Stroke=3;
        public static string FilePath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"HeavyShot.settings.xml"); } }
        public static Settings Load()
        {
            try { using (FileStream f=File.OpenRead(FilePath)) return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(f); }
            catch { return new Settings(); }
        }
        public bool Save()
        {
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
        internal static readonly string Version="0.1.0";
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
        internal HomeForm(bool trayStart,bool demo)
        {
            this.trayStart=trayStart; this.demo=demo;
            Text="HeavyShot"; ClientSize=new Size(580,460); AutoScaleMode=AutoScaleMode.Dpi;
            FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
            BackColor=Theme.Background; ForeColor=Color.White; Font=Theme.Body;
            try { Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { Icon=SystemIcons.Application; }
            Controls.Add(Theme.Label("HEAVYSHOT",32,30,400,38,22,Color.White));
            Controls.Add(Theme.Label("Capture uma ideia. Deixe sua marca.",34,75,500,28,12,Theme.Muted));
            ActionButton capture=new ActionButton("Nova captura     Ctrl + Shift + S","Nova captura");
            capture.SetBounds(34,132,512,54); capture.Primary=true; capture.Click+=delegate { BeginCapture(); }; Controls.Add(capture);
            Controls.Add(Theme.Label("Selecione • anote • copie ou salve",34,201,500,28,11,Theme.Muted));
            CheckBox cursor=new CheckBox { Text="Incluir o cursor na captura",Checked=settings.CaptureCursor,Location=new Point(36,250),Size=new Size(480,28),ForeColor=Color.White };
            cursor.CheckedChanged+=delegate { settings.CaptureCursor=cursor.Checked; SaveSettings(); }; Controls.Add(cursor);
            print=new CheckBox { Text="Usar também a tecla Print Screen",Checked=settings.UsePrintScreen,Location=new Point(36,284),Size=new Size(480,28),ForeColor=Color.White };
            print.CheckedChanged+=delegate { settings.UsePrintScreen=print.Checked; RegisterShortcuts(); SaveSettings(); }; Controls.Add(print);
            status=Theme.Label("",36,324,510,44,9,Theme.Muted); Controls.Add(status);
            Controls.Add(Theme.Label("PORTABLE  /  v"+Program.Version+"  /  processamento local",36,391,460,24,9,Theme.Muted));
            ContextMenuStrip menu=new ContextMenuStrip();
            menu.Items.Add("Nova captura",null,delegate { BeginCapture(); }); menu.Items.Add("Abrir HeavyShot",null,delegate { ShowHome(); });
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("Sair",null,delegate { exiting=true; Close(); });
            tray=new NotifyIcon { Icon=Icon,Text="HeavyShot — Ctrl+Shift+S",ContextMenuStrip=menu,Visible=true };
            tray.DoubleClick+=delegate { BeginCapture(); };
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); RegisterShortcuts(); }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e); RegisterShortcuts();
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
