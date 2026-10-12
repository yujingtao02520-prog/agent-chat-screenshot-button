using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace AgentSnip {
    internal static class Native {
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] internal static extern IntPtr GetAncestor(IntPtr h, uint flags);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] internal static extern uint SendInput(uint n, INPUT[] inputs, int size);
        [StructLayout(LayoutKind.Sequential)] internal struct INPUT { public uint type; public UNION data; }
        [StructLayout(LayoutKind.Explicit)] internal struct UNION {
            [FieldOffset(0)] public KEYBOARD keyboard;
            [FieldOffset(0)] public MOUSE mouse;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct KEYBOARD {
            public ushort key, scan; public uint flags, time; public UIntPtr extra;
        }
        [StructLayout(LayoutKind.Sequential)] internal struct MOUSE {
            public int x, y; public uint data, flags, time; public UIntPtr extra;
        }
        internal static bool Paste() {
            var keys = new INPUT[4];
            ushort[] codes = { 0x11, 0x56, 0x56, 0x11 };
            for (int i = 0; i < keys.Length; i++) {
                keys[i].type = 1; keys[i].data.keyboard.key = codes[i];
                keys[i].data.keyboard.flags = i >= 2 ? 2u : 0u;
            }
            uint sent = SendInput(4, keys, Marshal.SizeOf(typeof(INPUT)));
            if (sent != 4) {
                // Release modifiers if Windows accepted only part of the sequence.
                var release = new INPUT[2];
                release[0].type = release[1].type = 1;
                release[0].data.keyboard.key = 0x56; release[1].data.keyboard.key = 0x11;
                release[0].data.keyboard.flags = release[1].data.keyboard.flags = 2;
                SendInput(2, release, Marshal.SizeOf(typeof(INPUT)));
            }
            return sent == 4;
        }
    }

    internal static class Program {
        [STAThread] private static int Main(string[] args) {
            if (args.Length > 0 && args[0] == "--self-test") return SelfTest();
            bool created;
            using (var mutex = new Mutex(true, "Local\\AgentChatScreenshotButton", out created)) {
                if (!created) return 0;
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FloatingButton());
            }
            return 0;
        }
        private static int SelfTest() {
            try {
                if (Selection.Normalize(new Point(30, 40), new Point(10, 15)) != new Rectangle(10, 15, 20, 25)) return 1;
                using (var bitmap = new Bitmap(80, 60))
                using (var cropped = bitmap.Clone(new Rectangle(10, 15, 20, 25), PixelFormat.Format32bppArgb)) {
                    using (var memory = new MemoryStream()) {
                        cropped.Save(memory, ImageFormat.Png);
                        if (memory.Length < 8 || cropped.Size != new Size(20, 25)) return 2;
                    }
                }
                if (Marshal.SizeOf(typeof(Native.INPUT)) != (IntPtr.Size == 8 ? 40 : 28)) return 3;
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self-test-result.txt"), "PASS: selection geometry, PNG encoding, SendInput ABI");
                return 0;
            } catch { return 4; }
        }
    }

    internal sealed class FloatingButton : Form {
        private readonly NotifyIcon tray;
        private readonly System.Windows.Forms.Timer poll;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem pasteOption;
        private IntPtr target;
        private uint targetPid;
        private AutomationElement input;
        private bool capturing, moving, dragged;
        private Point dragStart, windowStart;
        private string lastFile;
        private bool hotkey;
        private readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentChatScreenshotButton");
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x00000080; return p; }
        }
        internal FloatingButton() {
            Text = "Agent Chat Screenshot Button";
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; AutoScaleMode = AutoScaleMode.None;
            Size = new Size(68, 48); BackColor = Color.FromArgb(26, 32, 44);
            StartPosition = FormStartPosition.Manual;
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - 92, area.Top + 180);
            menu = new ContextMenuStrip();
            menu.Items.Add("截图 / Capture  (Ctrl+Alt+S)", null, delegate { BeginCapture(); });
            pasteOption = new ToolStripMenuItem("自动粘贴 / Auto paste") { Checked = true, CheckOnClick = true };
            menu.Items.Add(pasteOption);
            menu.Items.Add("打开截图目录 / Open captures", null, delegate {
                Directory.CreateDirectory(Path.Combine(dataDir, "Captures"));
                Process.Start("explorer.exe", "\"" + Path.Combine(dataDir, "Captures") + "\"");
            });
            menu.Items.Add("复制上一张 / Copy last image", null, delegate {
                try { if (lastFile != null && File.Exists(lastFile)) using (var b = new Bitmap(lastFile)) PutClipboard(b, lastFile); }
                catch (Exception e) { Tell("复制失败", e.Message); }
            });
            menu.Items.Add("退出 / Exit", null, delegate { Close(); });
            ContextMenuStrip = menu;
            tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "Agent 截图：先点聊天输入框，再点悬浮按钮", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += delegate { Show(); };
            poll = new System.Windows.Forms.Timer { Interval = 200 };
            poll.Tick += delegate { RememberTarget(); }; poll.Start();
            MouseDown += delegate(object sender, MouseEventArgs e) {
                if (e.Button != MouseButtons.Left) return;
                RememberTarget(); moving = true; dragged = false; dragStart = Cursor.Position; windowStart = Location;
            };
            MouseMove += delegate {
                if (!moving) return;
                var delta = new Size(Cursor.Position.X - dragStart.X, Cursor.Position.Y - dragStart.Y);
                if (Math.Abs(delta.Width) + Math.Abs(delta.Height) > 5) dragged = true;
                if (dragged) Location = windowStart + delta;
            };
            MouseUp += delegate(object sender, MouseEventArgs e) {
                if (e.Button != MouseButtons.Left || !moving) return;
                moving = false; if (!dragged) BeginCapture();
            };
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            hotkey = Native.RegisterHotKey(Handle, 1, 0x4003, (uint)Keys.S);
            if (!hotkey) Tell("快捷键已被占用", "仍可点击悬浮按钮截图。");
        }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1) { RememberTarget(); BeginCapture(); }
            base.WndProc(ref m);
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(118, 224, 191), 2)) {
                e.Graphics.DrawRectangle(pen, 23, 10, 22, 17);
                e.Graphics.DrawEllipse(pen, 30, 14, 8, 8);
            }
            TextRenderer.DrawText(e.Graphics, "截图", Font, new Rectangle(0, 29, Width, 16), Color.White, TextFormatFlags.HorizontalCenter);
        }
        private void RememberTarget() {
            if (capturing) return;
            var h = Native.GetForegroundWindow(); uint pid;
            Native.GetWindowThreadProcessId(h, out pid);
            if (h == IntPtr.Zero || pid == (uint)Process.GetCurrentProcess().Id) return;
            target = h; targetPid = pid; input = null;
            try {
                var focused = AutomationElement.FocusedElement;
                if (focused != null && BelongsToWindow(focused, h) && IsEditable(focused)) input = focused;
            } catch { /* Screenshot still works if the app does not expose accessibility. */ }
        }
        private static bool BelongsToWindow(AutomationElement element, IntPtr window) {
            // Browsers can expose inputs from a renderer PID rather than the window's PID.
            for (int i = 0; element != null && i < 64; i++) {
                var handle = new IntPtr(element.Current.NativeWindowHandle);
                if (handle != IntPtr.Zero && (handle == window || Native.GetAncestor(handle, 2) == window)) return true;
                element = TreeWalker.ControlViewWalker.GetParent(element);
            }
            return false;
        }
        private static bool IsEditable(AutomationElement element) {
            if (!element.Current.IsEnabled || !element.Current.IsKeyboardFocusable) return false;
            object pattern;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern)) return !((ValuePattern)pattern).Current.IsReadOnly;
            return element.Current.ControlType == ControlType.Edit;
        }
        private async void BeginCapture() {
            if (capturing) return;
            capturing = true;
            var destination = target; var pid = targetPid; var editable = input;
            Hide(); menu.Close();
            try {
                // Let the compositor remove the button before freezing the screen.
                await System.Threading.Tasks.Task.Delay(180);
                var bounds = SystemInformation.VirtualScreen;
                using (var screen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb)) {
                    using (var g = Graphics.FromImage(screen)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    using (var selection = new Selection(screen, bounds)) {
                        if (selection.ShowDialog() != DialogResult.OK) { RestoreWindow(destination, pid); return; }
                        using (var crop = screen.Clone(selection.Selected, PixelFormat.Format32bppArgb)) {
                            string folder = Path.Combine(dataDir, "Captures"); Directory.CreateDirectory(folder);
                            lastFile = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".png");
                            crop.Save(lastFile, ImageFormat.Png);
                            PutClipboard(crop, lastFile);
                        }
                    }
                }
                bool restored = RestoreWindow(destination, pid);
                if (restored && pasteOption.Checked && editable != null) {
                    await System.Threading.Tasks.Task.Delay(180);
                    try {
                        // Wait for physical shortcut keys to be released before synthesizing Ctrl+V.
                        for (int i = 0; i < 20 && ModifiersDown(); i++) await System.Threading.Tasks.Task.Delay(75);
                        if (ModifiersDown()) { Tell("截图已复制并保存", "请松开快捷键后，在聊天输入框按 Ctrl+V。"); return; }
                        if (Native.GetForegroundWindow() == destination && BelongsToWindow(editable, destination) && IsEditable(editable)) editable.SetFocus();
                        await System.Threading.Tasks.Task.Delay(100);
                        if (Native.GetForegroundWindow() == destination && BelongsToWindow(editable, destination) && Automation.Compare(editable, AutomationElement.FocusedElement) && Native.Paste()) {
                            Tell("已尝试粘贴截图", "请检查聊天附件。若网页不支持图片粘贴，可从截图目录上传 PNG。");
                            return;
                        }
                    } catch { }
                }
                Tell("截图已复制并保存", "先点聊天输入框，再按 Ctrl+V。也可右键按钮打开截图目录上传 PNG。");
            } catch (Exception e) { Tell("截图失败", e.Message); }
            finally { capturing = false; Show(); }
        }
        private static bool ModifiersDown() {
            return (Native.GetAsyncKeyState(0x11) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x12) & 0x8000) != 0
                || (Native.GetAsyncKeyState(0x10) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5b) & 0x8000) != 0
                || (Native.GetAsyncKeyState(0x5c) & 0x8000) != 0;
        }
        private static bool RestoreWindow(IntPtr window, uint expectedPid) {
            uint pid;
            if (!Native.IsWindow(window) || !Native.IsWindowVisible(window)) return false;
            Native.GetWindowThreadProcessId(window, out pid);
            return pid == expectedPid && Native.SetForegroundWindow(window);
        }
        private static void PutClipboard(Bitmap bitmap, string path) {
            // Chromium/Electron consume PNG; native Windows programs can use Bitmap or FileDrop.
            using (var png = new MemoryStream()) {
                bitmap.Save(png, ImageFormat.Png); png.Position = 0;
                var data = new DataObject();
                data.SetData("PNG", false, png);
                data.SetData(DataFormats.Bitmap, true, bitmap);
                data.SetData(DataFormats.FileDrop, new string[] { path });
                Clipboard.SetDataObject(data, true, 5, 80);
            }
        }
        private void Tell(string title, string message) { tray.ShowBalloonTip(5000, title, message, ToolTipIcon.Info); }
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (hotkey) Native.UnregisterHotKey(Handle, 1);
                poll.Dispose(); tray.Visible = false; tray.Dispose(); menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class Selection : Form {
        private readonly Bitmap screenshot;
        private Point anchor, current;
        private bool selecting;
        internal Rectangle Selected { get; private set; }
        internal static Rectangle Normalize(Point a, Point b) {
            return Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }
        internal Selection(Bitmap image, Rectangle bounds) {
            screenshot = image; FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.None; StartPosition = FormStartPosition.Manual; Bounds = bounds;
            TopMost = true; ShowInTaskbar = false; DoubleBuffered = true; KeyPreview = true; Cursor = Cursors.Cross;
            MouseDown += delegate(object s, MouseEventArgs e) {
                if (e.Button == MouseButtons.Right) { DialogResult = DialogResult.Cancel; return; }
                if (e.Button != MouseButtons.Left) return;
                anchor = current = e.Location; selecting = true; Capture = true; Invalidate();
            };
            MouseMove += delegate(object s, MouseEventArgs e) {
                if (!selecting) return;
                current = new Point(Math.Max(0, Math.Min(ClientSize.Width, e.X)), Math.Max(0, Math.Min(ClientSize.Height, e.Y)));
                Invalidate();
            };
            MouseUp += delegate(object s, MouseEventArgs e) {
                if (!selecting || e.Button != MouseButtons.Left) return;
                selecting = false; Capture = false;
                current = new Point(Math.Max(0, Math.Min(ClientSize.Width, e.X)), Math.Max(0, Math.Min(ClientSize.Height, e.Y)));
                Selected = Normalize(anchor, current);
                if (Selected.Width >= 3 && Selected.Height >= 3) DialogResult = DialogResult.OK;
                else Invalidate();
            };
            KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel; };
            Shown += delegate { Activate(); Focus(); };
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.DrawImageUnscaled(screenshot, 0, 0);
            using (var dim = new SolidBrush(Color.FromArgb(105, Color.Black))) e.Graphics.FillRectangle(dim, ClientRectangle);
            var rectangle = Normalize(anchor, current);
            if (rectangle.Width > 0 && rectangle.Height > 0) {
                e.Graphics.DrawImage(screenshot, rectangle, rectangle, GraphicsUnit.Pixel);
                using (var outline = new Pen(Color.FromArgb(118, 224, 191), 2)) e.Graphics.DrawRectangle(outline, rectangle);
            }
            var hint = new Rectangle(20, 20, 460, 38);
            using (var background = new SolidBrush(Color.FromArgb(26, 32, 44))) e.Graphics.FillRectangle(background, hint);
            TextRenderer.DrawText(e.Graphics, "拖动框选 · 松开截图 · Esc / 右键取消", Font, hint, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
