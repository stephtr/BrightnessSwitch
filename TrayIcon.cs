using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
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

            contextMenu.Items.Add("-");

            autorunItem = new ToolStripMenuItem("Autostart with Windows");
            autorunItem.Checked = GetAutorun();
            autorunItem.CheckedChanged += (object? sender, EventArgs e) => SetAutorun(autorunItem.Checked);
            autorunItem.CheckOnClick = true;
            contextMenu.Items.Add(autorunItem);

            contextMenu.Items.Add("Exit", null, (object? sender, EventArgs args) =>
            {
                contextMenu.Close();
                if (OnExit != null) OnExit(this, 0);
            });

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

        ~TrayIcon()
        {
            trayIcon.Visible = false;
        }
    }
}