﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Todo: Crevice をLibに分離かな　→そのままNuget公開だとそのまま扱えていいのでは
//     →依存を切れるのが大きい
//     →RoslynもApp側かな
//     →んで、依存プラットフォームを下げれる？
//     →Androidなども含んだプラットフォームにも対応できるのでは
// Todo: Crevice.Facet.UI 
namespace CreviceApp.App.UI
{
    public partial class MainForm : MouseGestureForm
    {
        // Forcely make this application invisible from task switcher applications.
        const int WS_EX_TOOLWINDOW = 0x00000080;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        private readonly TooltipNotifier tooltip;

        private FileSystemWatcher userScriptWatcher;

        private LauncherForm launcherForm = null;

        public MainForm(AppConfig AppConfig) : base(AppConfig)
        {
            this.tooltip = new TooltipNotifier(this);
            this.userScriptWatcher = new UserScript.DirectoryWatcher(this, AppConfig.UserDirectory, "*.csx");
            InitializeComponent();
        }

        private void ReloadGestureMachine(object source, FileSystemEventArgs e)
        {
            Task.Run(() =>
            {
                reloadableGestureMachine.HotReload();
            });
        }

        private void SetupUserScriptWatcher()
        {
            userScriptWatcher.Changed += new FileSystemEventHandler(ReloadGestureMachine);
            userScriptWatcher.Created += new FileSystemEventHandler(ReloadGestureMachine);
            userScriptWatcher.Renamed += new RenamedEventHandler(ReloadGestureMachine);
            userScriptWatcher.EnableRaisingEvents = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RegisterNotifyIcon();
            UpdateTasktrayMessage("Configuration not loaded.");
            SetupUserScriptWatcher();
            reloadableGestureMachine.HotReload();
            try
            {
                EnableHook = true;
            }
            catch (Win32Exception)
            {
                ShowFatalErrorDialog("SetWindowsHookEX(WH_MOUSE_LL) was failed.");
                Application.Exit();
            }
            Verbose.Print("CreviceApp was started.");
        }
        
        private void RegisterNotifyIcon()
        {
            if (!appConfig.CLIOption.NoGUI)
            {
                while (true)
                {
                    var stopwatch = Stopwatch.StartNew();
                    notifyIcon1.Visible = true;
                    stopwatch.Stop();
                    if (stopwatch.ElapsedMilliseconds < 4000)
                    {
                        break;
                    }
                    notifyIcon1.Visible = false;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            userScriptWatcher.EnableRaisingEvents = false;
            userScriptWatcher.Dispose();
            try
            {
                EnableHook = false;
            }
            catch (Win32Exception)
            {
                ShowFatalErrorDialog("UnhookWindowsHookEx(WH_MOUSE_LL) was failed.");
            }
            notifyIcon1.Visible = false;
            tooltip.Dispose();
            Verbose.Print("CreviceApp was ended.");
            WinAPI.Console.Console.FreeConsole();
        }

        private void InvokeProperly(MethodInvoker invoker)
        {
            if (InvokeRequired)
            {
                Invoke(invoker);
            }
            else
            {
                invoker.Invoke();
            }
        }

        public void UpdateTasktrayMessage(string message)
        {
            var text = string.Format("Crevice {0}\n{1}", Application.ProductVersion, message);
            InvokeProperly(delegate ()
            {
                if (!appConfig.CLIOption.NoGUI)
                {
                    notifyIcon1.Text = text.Length > 63 ? text.Substring(0, 63) : text;
                }
            });
        }

        public void UpdateTasktrayMessage(string formattedtext, params object[] args)
        {
            UpdateTasktrayMessage(string.Format(formattedtext, args));
        }
        
        public void ShowFatalErrorDialog(string text)
        {
            InvokeProperly(delegate ()
            {
                Verbose.Print("ShowFatalErrorDialog: {0}", text);
                MessageBox.Show(text,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            });
        }

        public void ShowFatalErrorDialog(string formattedtext, params object[] objects)
        {
            ShowFatalErrorDialog(string.Format(formattedtext, objects));
        }

        public void ShowTooltip(string text, Point point, int duration)
        {
            InvokeProperly(delegate ()
            {
                Verbose.Print("ShowTooltip: {0}", text);
                tooltip.Show(text, point, duration);
            });
        }

        public void ShowBalloon(string text, string title, ToolTipIcon icon, int timeout)
        {
            InvokeProperly(delegate ()
            {
                Verbose.Print("ShowBalloon: {0}", text);
                if (!appConfig.CLIOption.NoGUI)
                {
                    notifyIcon1.BalloonTipText = text;
                    notifyIcon1.BalloonTipTitle = title;
                    notifyIcon1.BalloonTipIcon = icon;
                    notifyIcon1.ShowBalloonTip(timeout);
                }
            });
        }

        public void StartExternalProcess(string fileName)
        {
            InvokeProperly(delegate ()
            {
                if (!appConfig.CLIOption.NoGUI)
                {
                    Process.Start(fileName);
                }
            });
        }
        
        public void StartExternalProcess(string fileName, string arguments)
        {
            InvokeProperly(delegate ()
            {
                if (!appConfig.CLIOption.NoGUI)
                {
                    Process.Start(fileName, arguments);
                }
            });
        }

        private void OpenLauncherForm()
        {
            if (launcherForm == null || launcherForm.IsDisposed)
            {
                launcherForm = new LauncherForm(appConfig);
                launcherForm.Opacity = 0;
                launcherForm.Show();
            }
            else
            {
                launcherForm.Activate();
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            OpenLauncherForm();
        }

        private string _lastErrorMessage = "";
        public string LastErrorMessage
        {
            get { return _lastErrorMessage; }
            set
            {
                Verbose.Print("LastErrorMessage: {0}", value);
                _lastErrorMessage = value;
            }
        }

        private void OpenWithNotepad(string path, string text)
        {
            try
            {
                File.WriteAllText(path, text);
                StartExternalProcess("notepad.exe", path);
            } 
            catch (Exception) { }
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(LastErrorMessage))
            {
                OpenLauncherForm();
            }
            else
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "Crevice4.ErrorInformation.txt");
                OpenWithNotepad(tempPath, LastErrorMessage);
            }
        }
    }
}
