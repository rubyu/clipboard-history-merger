﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Crevice
{
    using Crevice.Logging;
    using Crevice.WinAPI.SendInput;
    using Crevice.WinAPI.Console;

    static class Program
    {
        private static readonly Mutex mutex = new Mutex(true, "{21f3fdc8-5f2f-11ee-8c99-0242ac120002}");

        private static readonly List<string> clipboardHistory = new List<string>();
        private static DateTime lastShortcutTime = DateTime.MinValue;
        private static int consecutiveShortcutPresses = 0;
        private static System.Timers.Timer sendTextTimer;
        private static readonly SingleInputSender singleInputSender = new SingleInputSender();

        [STAThread]
        static void Main()
        {
#if DEBUG  
            Console.AttachConsole();
            Verbose.Enable();
            Verbose.Print("Verbose output: enabled");
#endif
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Verbose.Print("Exiting for another instance is already running");
                Application.Exit();
            }

            try
            {
                using (Verbose.PrintElapsed("Initializing the components"))
                {
                    KeyboardHookManager.Initialize();
                    ClipboardNotification.Initialize();

                    ClipboardNotification.ClipboardUpdate += ClipboardUpdated;
                    KeyboardHookManager.ShortcutActivate += OnShortcutActivated;

                    sendTextTimer = new System.Timers.Timer(1000);
                    sendTextTimer.Elapsed += OnTimerElapsed;
                    sendTextTimer.AutoReset = false;
                }

                using (Verbose.PrintElapsed("Starting the application"))
                {
                    Application.Run(new ApplicationContext());
                }

                using (Verbose.PrintElapsed("Shutting down the components"))
                {
                    KeyboardHookManager.Uninitialize();
                    ClipboardNotification.Uninitialize();
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private static void ClipboardUpdated(object sender, ClipboardUpdatedEventArgs e)
        {
            string text = e.ClipboardText;
            Verbose.Print($"Clipboard update detected: '{text}'");

            if (!string.IsNullOrEmpty(text))
            {
                clipboardHistory.Insert(0, text);
                Verbose.Print($"The item has been added to the list");
                if (clipboardHistory.Count > 10)
                {
                    clipboardHistory.RemoveAt(clipboardHistory.Count - 1);
                    Verbose.Print($"The first entry has been removed; reached to the limit");
                }
            }
        }

        private static void OnShortcutActivated(object sender, EventArgs e)
        {
            Verbose.Print($"Ctrl+Shift+V has been pressed");
            
            DateTime now = DateTime.Now;
            if ((now - lastShortcutTime).TotalMilliseconds <= 1000)
            {
                consecutiveShortcutPresses++;
            }
            else
            {
                consecutiveShortcutPresses = 1;
            }
            Verbose.Print($"{consecutiveShortcutPresses} times");

            lastShortcutTime = now;
            sendTextTimer.Stop();
            sendTextTimer.Start();
        }

        private static void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var xs = clipboardHistory.GetRange(0, Math.Min(consecutiveShortcutPresses, clipboardHistory.Count));
            xs.Reverse();
            string textToSend = string.Join(" ", xs);

            using (Verbose.PrintElapsed($"Sending the merged text the foreground application; '{textToSend}'"))
            {

                if (string.IsNullOrEmpty(textToSend))
                {
                    Verbose.Print("Skipped for the text was empty");
                    return;
                }
                singleInputSender.UnicodeKeyStroke(textToSend);
            }
                
            consecutiveShortcutPresses = 0;
        }
    }
}
