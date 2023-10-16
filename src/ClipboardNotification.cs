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
        public NotificationForm()
        {
            NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
            NativeMethods.AddClipboardFormatListener(Handle);
        }
        private static void OnClipboardUpdate(string clipboardText)
        {
            ClipboardUpdate?.Invoke(null, new ClipboardUpdatedEventArgs(clipboardText));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                Thread.Sleep(500);
                string clipboardText = string.Empty;

                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        clipboardText = Clipboard.GetText();
                    }));
                }
                else
                {
                    clipboardText = Clipboard.GetText();
                }

                OnClipboardUpdate(clipboardText);
            }
            base.WndProc(ref m);
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
