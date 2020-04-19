using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Windows.UI.ViewManagement;

namespace BrightnessSwitch
{
    class TrayIcon
    {
        private NotifyIcon trayIcon;
        private ToolStripMenuItem autoSwitchItem;
        private ToolStripItem switchItem;
        public event EventHandler<int>? OnExit;
        public event EventHandler<bool>? OnThemeSwitch;
        private Icon DarkIcon = new Icon("sun_dark.ico");
        private Icon LightIcon = new Icon("sun_light.ico");
        public bool AutoSwitchEnabled { get; private set; } = true;
        private bool SwitchToLightMode;

        public TrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = DarkIcon;
            trayIcon.Visible = true;
            trayIcon.Text = "Switch Theme brightness";
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            switchItem = trayIcon.ContextMenuStrip.Items.Add("Switch theme", null, (object? sender, EventArgs args) =>
            {
                trayIcon.ContextMenuStrip.Close();
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
            trayIcon.ContextMenuStrip.Items.Add(autoSwitchItem);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (object? sender, EventArgs args) =>
            {
                trayIcon.ContextMenuStrip.Close();
                if (OnExit != null) OnExit(this, 0);
            });

            trayIcon.Click += (object? sender, EventArgs e) =>
            {
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi!.Invoke(trayIcon, null);
            };
            trayIcon.ContextMenuStrip.Opening += (object? sender, CancelEventArgs e) => UpdateContextMenu();
            trayIcon.ContextMenuStrip.Closing += (object? sender, ToolStripDropDownClosingEventArgs e) =>
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

        ~TrayIcon()
        {
            trayIcon.Visible = false;
        }
    }
}