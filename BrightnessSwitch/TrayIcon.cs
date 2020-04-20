using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace BrightnessSwitch
{
    class TrayIcon
    {
        private NotifyIcon trayIcon;
        private ToolStripMenuItem? autoSwitchItem = null;
        private ToolStripMenuItem autorunItem;
        private ToolStripItem switchItem;
        public event EventHandler<int>? OnExit;
        public event EventHandler<bool>? OnThemeSwitch;
        private Icon DarkIcon;
        private Icon LightIcon;
        public bool AutoSwitchEnabled { get; private set; }
        private bool SwitchToLightMode;

        public TrayIcon(bool autoSwitchEnabled = true, bool autoModeAvailable = true)
        {
            AutoSwitchEnabled = autoModeAvailable && autoSwitchEnabled;

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("BrightnessSwitch.Resources.IconSunDark"))
                DarkIcon = new Icon(stream!);
            using (var stream = assembly.GetManifestResourceStream("BrightnessSwitch.Resources.IconSunLight"))
                LightIcon = new Icon(stream!);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Switch Theme brightness";
            var contextMenu = trayIcon.ContextMenuStrip = new ContextMenuStrip();

            contextMenu.Items.Add("Exit", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                trayIcon.Visible = false;
                if (OnExit != null) OnExit(this, 0);
            });

#if !STORE
            contextMenu.Items.Add("Check for updates", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                CheckUpdates();
            });
#endif

            autorunItem = new ToolStripMenuItem("Autostart with Windows");
            GetAutorun().ContinueWith((Task<bool> task) => autorunItem.Checked = task.Result);
            autorunItem.CheckedChanged += async (object? sender, EventArgs e) => autorunItem.Checked = await SetAutorun(autorunItem.Checked);
            autorunItem.CheckOnClick = true;
            contextMenu.Items.Add(autorunItem);

            contextMenu.Items.Add("-");

            switchItem = contextMenu.Items.Add("Switch theme", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                if (OnThemeSwitch != null) OnThemeSwitch(this, SwitchToLightMode);
            });

            if (autoModeAvailable)
            {
                autoSwitchItem = new ToolStripMenuItem("Auto switch theme");
                autoSwitchItem.Checked = autoSwitchEnabled && AutoSwitchEnabled;
                autoSwitchItem.CheckOnClick = true;
                autoSwitchItem.CheckedChanged += (object? sender, EventArgs e) =>
                {
                    AutoSwitchEnabled = autoSwitchItem.Checked;
                    UpdateContextMenu();
                };
                contextMenu.Items.Add(autoSwitchItem);
            }

            trayIcon.Click += (object? sender, EventArgs e) =>
            {
                // taken from https://stackoverflow.com/a/2208910
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi!.Invoke(trayIcon, null);
            };
            contextMenu.Opening += (object? sender, CancelEventArgs e) => UpdateContextMenu();
            contextMenu.Closing += (object? sender, ToolStripDropDownClosingEventArgs e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                {
                    e.Cancel = true;
                }
            };

            SetTheme(ThemeUtils.IsLightTheme());
        }

        ~TrayIcon()
        {
            trayIcon.Visible = false;
        }

        public void UpdateContextMenu()
        {
            var lightThemeEnabled = ThemeUtils.IsLightTheme();
            switchItem.Text = $"Switch to {(lightThemeEnabled ? "dark" : "light")} theme{(AutoSwitchEnabled ? " (and learn)" : "")}";
            SwitchToLightMode = !lightThemeEnabled;
        }

        public void SetTheme(bool useLightTheme)
        {
            trayIcon.Icon = useLightTheme ? LightIcon : DarkIcon;
            trayIcon.Visible = true;
        }

#if STORE
        private async Task<bool> GetAutorun()
        {
            var startupTask = await StartupTask.GetAsync("BrightnessSwitch");
            return startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy;
        }

        private async Task<bool> SetAutorun(bool activate)
        {
            var startupTask = await StartupTask.GetAsync("BrightnessSwitch");
            if (activate)
            {
                switch (startupTask.State)
                {
                    case StartupTaskState.Disabled:
                        var newState = await startupTask.RequestEnableAsync();
                        return newState == StartupTaskState.Enabled || newState == StartupTaskState.EnabledByPolicy;
                    case StartupTaskState.DisabledByPolicy:
                        MessageBox.Show("Can't enable autostart (disabled by policy)", "BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    case StartupTaskState.DisabledByUser:
                        MessageBox.Show("To enable autostart, you have to use the Task Manager -> Startup tab.", "BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return false;
                    case StartupTaskState.Enabled:
                    case StartupTaskState.EnabledByPolicy:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                startupTask.Disable();
                return false;
            }
        }
#else
        private const string autorunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string autorunValue = "BrightnessSwitch";

        private Task<bool> GetAutorun()
        {
            var arKey = Registry.CurrentUser.OpenSubKey(autorunKey)!;
            return Task.FromResult((string?)arKey.GetValue(autorunValue) == Application.ExecutablePath.ToString());
        }

        private Task<bool> SetAutorun(bool activate)
        {
            var arKey = Registry.CurrentUser.OpenSubKey(autorunKey, true)!;
            if (activate)
            {
                arKey.SetValue(autorunValue, Application.ExecutablePath.ToString());
            }
            else
            {
                arKey.DeleteValue(autorunValue);
            }
            return Task.FromResult(activate);
        }
#endif

#if !STORE
        private async void CheckUpdates(bool showOptionalMessages = true)
        {
            const string updateUrl = "https://api.github.com/repos/stephtr/BrightnessSwitch/releases/latest";

            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            using var wc = new WebClient();
            wc.Headers.Add("User-Agent", "BrightnessSwitch");
            try
            {
                var data = await wc.DownloadStringTaskAsync(updateUrl);
                var tagMatch = new Regex("\"tag_name\"[^\"]+\"([^\"]+)\"").Match(data);
                var urlMatch = new Regex("\"html_url\"[^\"]+\"(https://github\\.com/[^\"]+)\"").Match(data);
                var newVersion = new Version(tagMatch.Groups[1].Value);
                var releaseUrl = urlMatch.Groups[1].Value;
                if (currentVersion < newVersion)
                {
                    if (MessageBox.Show("There is a new version available. Do you want to go to the download page?", "Updating BrightnessSwitch", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = releaseUrl,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                }
                else if (showOptionalMessages)
                {
                    MessageBox.Show("You are already using the latest version.", "Updating BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                if (showOptionalMessages)
                {
                    MessageBox.Show("Error downloading the update information", "Updating BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
#endif
    }
}