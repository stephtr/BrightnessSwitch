using System;
using Microsoft.Win32;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace BrightnessSwitch
{
    public class LightControl
    {
        private LightSensor Sensor;
        private bool LightThemeEnabled;
        private float IlluminanceThreshold;
        private RegistryKey PersonalizationRegKey;

        public LightControl(uint sensorInterval = 10_000, float illuminanceThreshold = 5000)
        {
            IlluminanceThreshold = illuminanceThreshold;

            Sensor = LightSensor.GetDefault();
            Sensor.ReportInterval = Math.Max(Sensor.MinimumReportInterval, sensorInterval);
            Sensor.ReadingChanged += new TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(LightReadingChanged);

            var uiSettings = new UISettings();
            LightThemeEnabled = uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FFFFFFFF";

            PersonalizationRegKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true)
                ?? throw new Exception("A required registry key can't be accessed.");
        }

        private void LightReadingChanged(LightSensor sensor, LightSensorReadingChangedEventArgs sensorEvent)
        {
            if (sensorEvent.Reading == null)
            {
                return;
            }
            if (!LightThemeEnabled && sensorEvent.Reading.IlluminanceInLux > IlluminanceThreshold * 1.1)
            {
                SetTheme(true);
            }
            if (LightThemeEnabled && sensorEvent.Reading.IlluminanceInLux < IlluminanceThreshold / 1.1)
            {
                SetTheme(false);
            }
        }

        private void SetTheme(bool useLightTheme)
        {
            LightThemeEnabled = useLightTheme;
            PersonalizationRegKey.SetValue("AppsUseLightTheme", useLightTheme, RegistryValueKind.DWord);
            PersonalizationRegKey.SetValue("SystemUsesLightTheme", useLightTheme, RegistryValueKind.DWord);
        }
    }
}