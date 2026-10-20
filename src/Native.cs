using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HeavyShot
{
    internal static class Native
    {
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] private static extern bool GetCursorInfo(ref CursorInfo info);
        [DllImport("user32.dll")] private static extern bool GetIconInfo(IntPtr icon, out IconInfo info);
        [DllImport("user32.dll")] private static extern bool DrawIconEx(IntPtr dc, int x, int y, IntPtr icon, int width, int height, uint step, IntPtr brush, uint flags);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [StructLayout(LayoutKind.Sequential)] private struct CursorInfo { public int Size, Flags; public IntPtr Cursor; public Point Position; }
        [StructLayout(LayoutKind.Sequential)] private struct IconInfo { public bool IsIcon; public int X, Y; public IntPtr Mask, Color; }
        internal static void EnableDpi()
        { try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { SetProcessDPIAware(); } }
        internal static Bitmap CaptureDesktop(bool cursor, out Rectangle bounds)
        {
            bounds = SystemInformation.VirtualScreen;
            Bitmap result = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                    if (cursor) PaintCursor(g, bounds.Location);
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        private static void PaintCursor(Graphics g, Point origin)
        {
            CursorInfo ci = new CursorInfo(); ci.Size = Marshal.SizeOf(typeof(CursorInfo));
            if (!GetCursorInfo(ref ci) || ci.Flags != 1) return;
            IconInfo ii;
            if (!GetIconInfo(ci.Cursor, out ii)) return;
            try
            {
                IntPtr dc = g.GetHdc();
                try { DrawIconEx(dc, ci.Position.X - origin.X - ii.X, ci.Position.Y - origin.Y - ii.Y, ci.Cursor, 0, 0, 0, IntPtr.Zero, 3); }
                finally { g.ReleaseHdc(dc); }
            }
            finally { if (ii.Mask != IntPtr.Zero) DeleteObject(ii.Mask); if (ii.Color != IntPtr.Zero) DeleteObject(ii.Color); }
        }
    }
}
