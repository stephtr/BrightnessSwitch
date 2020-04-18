using System;
using System.Drawing;
using System.Windows.Forms;

namespace BrightnessSwitch
{
    class TrayIcon
    {
        private NotifyIcon trayIcon;
        public event EventHandler<int>? OnExit;
        public event EventHandler<bool>? OnThemeSwitch;
        private Icon DarkIcon = new Icon("sun_dark.ico");
        private Icon LightIcon = new Icon("sun_light.ico");

        public TrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = DarkIcon;
            trayIcon.Visible = true;
            trayIcon.Text = "Switch Theme brightness";
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Switch to light theme", null, (object? sender, EventArgs args) =>
            {
                if (OnThemeSwitch != null) OnThemeSwitch(this, true);
            });
            trayIcon.ContextMenuStrip.Items.Add("Switch to dark theme", null, (object? sender, EventArgs args) =>
            {
                if (OnThemeSwitch != null) OnThemeSwitch(this, false);
            });
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (object? sender, EventArgs args) =>
            {
                if (OnExit != null) OnExit(this, 0);
            });
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