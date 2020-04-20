using Windows.UI.ViewManagement;
using System.Diagnostics;

namespace BrightnessSwitch
{
    public class ThemeUtils
    {
        public static bool IsLightTheme()
        {
            var uiSettings = new UISettings();
            return uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FFFFFFFF";
        }

        public static bool SetTheme(bool useLightTheme)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = @"ADD HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize /v AppsUseLightTheme /t REG_DWORD /d " + (useLightTheme ? "1" : "0") + " /f",
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                psi.Arguments = @"ADD HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize /v SystemUsesLightTheme /t REG_DWORD /d " + (useLightTheme ? "1" : "0") + " /f";
                Process.Start(psi);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool TestThemeAccess()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "reg",
                    CreateNoWindow = true,
                };
                Process.Start(psi);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}