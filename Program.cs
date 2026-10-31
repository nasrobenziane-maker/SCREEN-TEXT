using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Win32;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenText;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private const int HotkeyId = 1;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private readonly NotifyIcon tray;
    private readonly HotkeyWindow hotkeyWindow;
    private bool disposed;

    public TrayContext()
    {
        if (!IsStartupEnabled()) SetStartup(true);

        var menu = new ContextMenuStrip();
        menu.Items.Add("استخراج نص من الشاشة  (Ctrl+E)", null, async (_, _) => await SelectAndRecognizeAsync());
        var startup = new ToolStripMenuItem("التشغيل مع Windows") { CheckOnClick = true, Checked = IsStartupEnabled() };
        startup.CheckedChanged += (_, _) => SetStartup(startup.Checked);
        menu.Items.Add(startup);
        menu.Items.Add("حول البرنامج", null, (_, _) => MessageBox.Show("ScreenText\nحدد أي جزء من الشاشة وسيتم نسخ النص إلى الحافظة.\n\nالاختصار: Ctrl+E", "حول البرنامج"));
        menu.Items.Add("خروج", null, (_, _) => ExitThread());

        tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "ScreenText — Ctrl+E", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += async (_, _) => await SelectAndRecognizeAsync();
        hotkeyWindow = new HotkeyWindow(OnHotkey);
        if (!RegisterHotKey(hotkeyWindow.Handle, HotkeyId, ModControl, (uint)Keys.E))
            tray.ShowBalloonTip(5000, "ScreenText", "تعذر تسجيل Ctrl+E؛ ربما يستخدمه برنامج آخر.", ToolTipIcon.Warning);
    }

    private void OnHotkey() => _ = SelectAndRecognizeAsync();

    private async Task SelectAndRecognizeAsync()
    {
        using var selector = new RegionSelector();
        if (selector.ShowDialog() != DialogResult.OK || selector.Selection.Width < 3 || selector.Selection.Height < 3) return;

        try
        {
            await Task.Delay(150);
            using var bitmap = new Bitmap(selector.Selection.Width, selector.Selection.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
                graphics.CopyFromScreen(selector.Selection.Location, Point.Empty, selector.Selection.Size, CopyPixelOperation.SourceCopy);

            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine is null)
            {
                MessageBox.Show("لم يتم العثور على محرك OCR أو لغة OCR مثبتة في Windows. أضف لغة تتضمن إمكانات التعرف على الكتابة.", "ScreenText", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[Math.Abs(data.Stride) * data.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bitmap.UnlockBits(data);
            using var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels.AsBuffer(), BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Ignore);
            var result = await engine.RecognizeAsync(softwareBitmap);
            var text = result.Text.Trim();
            if (text.Length == 0)
            {
                tray.ShowBalloonTip(2500, "ScreenText", "لم يتم العثور على نص في المنطقة المحددة.", ToolTipIcon.Info);
                return;
            }
            Clipboard.SetText(text);
            tray.ShowBalloonTip(2500, "ScreenText", "تم استخراج النص ونسخه. الصقه باستخدام Ctrl+V.", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذر استخراج النص.\n{ex.Message}", "ScreenText", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue("ScreenText") is string;
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key is null) return;
        if (enabled) key.SetValue("ScreenText", $"\"{Application.ExecutablePath}\"");
        else key.DeleteValue("ScreenText", throwOnMissingValue: false);
    }

    protected override void ExitThreadCore()
    {
        if (!disposed)
        {
            disposed = true;
            UnregisterHotKey(hotkeyWindow.Handle, HotkeyId);
            hotkeyWindow.Dispose();
            tray.Visible = false;
            tray.Dispose();
        }
        base.ExitThreadCore();
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        private readonly Action callback;
        public HotkeyWindow(Action callback) { this.callback = callback; CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId) callback();
            base.WndProc(ref m);
        }
        public void Dispose() => DestroyHandle();
    }
}

internal sealed class RegionSelector : Form
{
    private Point start;
    private Point end;
    private bool dragging;
    private readonly Bitmap desktop;
    public Rectangle Selection { get; private set; }

    public RegionSelector()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Opacity = 1.0;
        KeyPreview = true;
        desktop = new Bitmap(Bounds.Width, Bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(desktop))
            graphics.CopyFromScreen(Bounds.Location, Point.Empty, Bounds.Size, CopyPixelOperation.SourceCopy);
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        dragging = true;
        start = PointToScreen(e.Location);
        end = start;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!dragging) return;
        end = PointToScreen(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!dragging) return;
        dragging = false;
        end = PointToScreen(e.Location);
        Selection = Rectangle.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.DrawImageUnscaled(desktop, 0, 0);
        using (var dim = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
            e.Graphics.FillRectangle(dim, ClientRectangle);

        if (dragging)
        {
            var current = Rectangle.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
            current.Offset(-Bounds.Left, -Bounds.Top);
            if (current.Width > 0 && current.Height > 0)
            {
                e.Graphics.DrawImage(desktop, current, current, GraphicsUnit.Pixel);
                using var border = new Pen(Color.DeepSkyBlue, 3);
                using var fill = new SolidBrush(Color.FromArgb(35, Color.DeepSkyBlue));
                e.Graphics.FillRectangle(fill, current);
                e.Graphics.DrawRectangle(border, current);
            }
        }

        const string hint = "اسحب لتحديد المنطقة — Esc للإلغاء";
        var hintRect = new Rectangle(18, 14, 440, 44);
        using (var panel = new SolidBrush(Color.FromArgb(200, 20, 25, 32)))
            e.Graphics.FillRectangle(panel, hintRect);
        TextRenderer.DrawText(e.Graphics, hint, new Font("Segoe UI", 14, FontStyle.Bold), hintRect, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.RightToLeft);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) desktop.Dispose();
        base.Dispose(disposing);
    }
}
