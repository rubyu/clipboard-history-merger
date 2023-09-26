using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Crevice.Logging;
using Crevice.WinAPI.WindowsHookEx;

public static class KeyboardHookManager
{
    private static KeyboardHookForm form;
    public static event EventHandler ShortcutActivate;

    public static void Initialize()
    {
        form = new KeyboardHookForm();
        form.SetHook();
    }

    public static void Uninitialize()
    {
        form.Unhook();
        form.Dispose();
    }

    private class KeyboardHookForm : Form
    {
        static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern short GetKeyState(int nVirtKey);

            public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        }

        private readonly LowLevelKeyboardHook keyboardHook;


        public KeyboardHookForm()
        {
            NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
            keyboardHook = new LowLevelKeyboardHook(KeyboardProc);
        }

        public void SetHook() => keyboardHook.SetHook();
        public void Unhook() => keyboardHook.Unhook();


        public WindowsHook.Result KeyboardProc(LowLevelKeyboardHook.Event evnt, LowLevelKeyboardHook.KBDLLHOOKSTRUCT data)
        {
            Verbose.Print("KeyboardProc() called");
            if (0 <= NativeMethods.GetKeyState(((int)Keys.ControlKey)) ||
                0 <= NativeMethods.GetKeyState(((int)Keys.Menu)))
            {
                Verbose.Print("Shift+Alt wasn't pressed");
                return WindowsHook.Result.Transfer;
            }

            if (data.vkCode != ((int)Keys.V))
            {
                Verbose.Print("V wasn't pressed");
                return WindowsHook.Result.Transfer;
            }

            if (evnt == LowLevelKeyboardHook.Event.WM_KEYDOWN ||
                evnt == LowLevelKeyboardHook.Event.WM_SYSKEYDOWN)
            {
                using (Verbose.PrintElapsed("Invoking events of 'ShortcutActivate'"))
                {
                    ShortcutActivate?.Invoke(null, EventArgs.Empty);
                }
            }
            return WindowsHook.Result.Cancel;
        }
    }
}