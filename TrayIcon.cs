using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using Windows.UI.ViewManagement;

namespace BrightnessSwitch
{
    class TrayIcon
    {
        private NotifyIcon trayIcon;
        private ToolStripMenuItem autoSwitchItem;
        private ToolStripMenuItem autorunItem;
        private ToolStripItem switchItem;
        public event EventHandler<int>? OnExit;
        public event EventHandler<bool>? OnThemeSwitch;
        private Icon DarkIcon;
        private Icon LightIcon;
        public bool AutoSwitchEnabled { get; private set; } = true;
        private bool SwitchToLightMode;

        private const string autorunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string autorunValue = "BrightnessSwitch";
        private const string updateUrl = "https://api.github.com/repos/stephtr/BrightnessSwitch/releases/latest";

        public TrayIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("BrightnessSwitch.Resources.IconSunDark"))
                DarkIcon = new Icon(stream);
            using (var stream = assembly.GetManifestResourceStream("BrightnessSwitch.Resources.IconSunLight"))
                LightIcon = new Icon(stream);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = DarkIcon;
            trayIcon.Visible = true;
            trayIcon.Text = "Switch Theme brightness";
            var contextMenu = trayIcon.ContextMenuStrip = new ContextMenuStrip();

            contextMenu.Items.Add("Exit", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                if (OnExit != null) OnExit(this, 0);
            });

            contextMenu.Items.Add("Check for updates", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                CheckUpdates();
            });

            autorunItem = new ToolStripMenuItem("Autostart with Windows");
            autorunItem.Checked = GetAutorun();
            autorunItem.CheckedChanged += (object? sender, EventArgs e) => SetAutorun(autorunItem.Checked);
            autorunItem.CheckOnClick = true;
            contextMenu.Items.Add(autorunItem);

            contextMenu.Items.Add("-");

            switchItem = contextMenu.Items.Add("Switch theme", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                if (OnThemeSwitch != null) OnThemeSwitch(this, SwitchToLightMode);
            });

            autoSwitchItem = new ToolStripMenuItem("Auto switch theme");
            autoSwitchItem.Checked = AutoSwitchEnabled;
            autoSwitchItem.CheckOnClick = true;
            autoSwitchItem.CheckedChanged += (object? sender, EventArgs e) =>
            {
                AutoSwitchEnabled = autoSwitchItem.Checked;
                UpdateContextMenu();
            };
            contextMenu.Items.Add(autoSwitchItem);

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
        }

        ~TrayIcon()
        {
            trayIcon.Visible = false;
        }

        public void UpdateContextMenu()
        {
            var uiSettings = new UISettings();
            var lightThemeEnabled = uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FFFFFFFF";
            switchItem.Text = $"Switch to {(lightThemeEnabled ? "dark" : "light")} theme{(AutoSwitchEnabled ? " (and learn)" : "")}";
            SwitchToLightMode = !lightThemeEnabled;
        }

        public void SetTheme(bool useLightTheme)
        {
            trayIcon.Icon = useLightTheme ? LightIcon : DarkIcon;
        }

        private bool GetAutorun()
        {
            var arKey = Registry.CurrentUser.OpenSubKey(autorunKey)!;
            return (string?)arKey.GetValue(autorunValue) == Application.ExecutablePath.ToString();
        }

        private void SetAutorun(bool activate)
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
        }

        private async void CheckUpdates(bool showErrors = true)
        {
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
                    if (MessageBox.Show("There is a new version available. Do you want to go to the download page?", "Updating BrightnessSwitch", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = releaseUrl,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                }
            }
            catch
            {
                if (showErrors)
                {
                    MessageBox.Show("Error downloading the update information", "Updating BrightnessSwitch");
                }
            }
        }
    }
}