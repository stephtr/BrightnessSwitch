using Windows.UI.ViewManagement;
using Microsoft.Win32;
using System;

namespace BrightnessSwitch
{
    public class ThemeUtils
    {
        public static bool IsLightTheme()
        {
            var uiSettings = new UISettings();
            return uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FFFFFFFF";
        }

        private static RegistryKey? PersonalizationRegKey = null;
        public static void SetTheme(bool useLightTheme)
        {
            if (PersonalizationRegKey == null)
            {
                PersonalizationRegKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true)
                    ?? throw new Exception("A required registry key can't be accessed.");
            }
            PersonalizationRegKey.SetValue("AppsUseLightTheme", useLightTheme, RegistryValueKind.DWord);
            PersonalizationRegKey.SetValue("SystemUsesLightTheme", useLightTheme, RegistryValueKind.DWord);
        }
    }
}