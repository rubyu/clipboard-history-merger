using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class ClipboardUpdatedEventArgs : EventArgs
{
    public string ClipboardText { get; }

    public ClipboardUpdatedEventArgs(string clipboardText)
    {
        ClipboardText = clipboardText;
    }
}

public static class ClipboardNotification
{
    private class NotificationForm : Form
    {
        private static readonly int maxDebounceWaitMs = 100;
        private static readonly int clipboardUpdateWaitMs = 1;
        private static readonly int maxClipboardUpdateCheck = 1000;

        private readonly System.Timers.Timer debounceTimer = new System.Timers.Timer(maxDebounceWaitMs) { AutoReset = false };
        private uint lastClipboardSequenceNumber = 0;

        public NotificationForm()
        {
            NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
            NativeMethods.AddClipboardFormatListener(Handle);
            debounceTimer.Elapsed += OnDebouncedClipboardUpdate;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            }
            base.WndProc(ref m);
        }

        private void OnDebouncedClipboardUpdate(object sender, System.Timers.ElapsedEventArgs e)
        {
            uint initialSequenceNumber = lastClipboardSequenceNumber;
            uint attempts = 0;
            while (attempts < maxClipboardUpdateCheck) 
            {
                lastClipboardSequenceNumber = NativeMethods.GetClipboardSequenceNumber();
                if (lastClipboardSequenceNumber != initialSequenceNumber)
                {
                    string clipboardText = string.Empty;
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(() => {
                            clipboardText = Clipboard.GetText();
                        }));
                    }
                    else
                    {
                        clipboardText = Clipboard.GetText();
                    }
                    OnClipboardUpdate(clipboardText);
                    break;
                }
                Thread.Sleep(clipboardUpdateWaitMs);
                attempts++;
            }
        }

        private static void OnClipboardUpdate(string clipboardText)
        {
            ClipboardUpdate?.Invoke(null, new ClipboardUpdatedEventArgs(clipboardText));
        }
    }

    private static class NativeMethods
    {
        public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern uint GetClipboardSequenceNumber();

        public const int WM_CLIPBOARDUPDATE = 0x031D;
    }

    private static NotificationForm form;
    private static Thread formThread;

    public static event EventHandler<ClipboardUpdatedEventArgs> ClipboardUpdate;

    public static void Initialize()
    {
        formThread = new Thread(() =>
        {
            form = new NotificationForm();
            Application.Run(form);
        })
        {
            IsBackground = true,
            Name = "ClipboardNotificationThread"
        };
        formThread.SetApartmentState(ApartmentState.STA);
        formThread.Start();
    }

    public static void Uninitialize()
    {
        if (form != null && !form.IsDisposed)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(new Action(form.Close));
            }
            else
            {
                form.Close();
            }
        }
        formThread?.Join();
    }
}
