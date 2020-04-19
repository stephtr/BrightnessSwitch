using System;
using Microsoft.Win32;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace BrightnessSwitch
{
    public delegate bool? PredictionCallback(float illuminanceInLux);

    public class LightControl
    {
        private LightSensor Sensor;
        private bool LightThemeEnabled;
        public float IlluminanceThreshold;
        private DateTime LastAutomaticThemeChange = DateTime.MinValue;
        public TimeSpan MinimumThemeChangeDuration = TimeSpan.FromSeconds(60);
        private RegistryKey PersonalizationRegKey;
        public event PredictionCallback? PredictionCallback;
        public event EventHandler<bool>? OnThemeSwitch;

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

        public float GetCurrentIlluminance()
        {
            return Sensor.GetCurrentReading()?.IlluminanceInLux ?? 0;
        }

        private void LightReadingChanged(LightSensor sensor, LightSensorReadingChangedEventArgs sensorEvent)
        {
            if (sensorEvent.Reading == null || DateTime.UtcNow - LastAutomaticThemeChange < MinimumThemeChangeDuration)
            {
                return;
            }

            bool? useLightTheme = null;
            if (PredictionCallback != null)
            {
                useLightTheme = PredictionCallback(sensorEvent.Reading.IlluminanceInLux);
            }
            else
            {
                if (sensorEvent.Reading.IlluminanceInLux > IlluminanceThreshold * 1.1)
                {
                    useLightTheme = true;
                }
                if (sensorEvent.Reading.IlluminanceInLux < IlluminanceThreshold / 1.1)
                {
                    useLightTheme = false;
                }
            }

            if (useLightTheme != null && LightThemeEnabled != useLightTheme)
            {
                LastAutomaticThemeChange = DateTime.UtcNow;
                LightThemeEnabled = useLightTheme.Value;
                SetTheme(LightThemeEnabled);
            }
        }

        public void SetTheme(bool useLightTheme)
        {
            LightThemeEnabled = useLightTheme;
            PersonalizationRegKey.SetValue("AppsUseLightTheme", useLightTheme, RegistryValueKind.DWord);
            PersonalizationRegKey.SetValue("SystemUsesLightTheme", useLightTheme, RegistryValueKind.DWord);
            if (OnThemeSwitch != null)
            {
                OnThemeSwitch(this, useLightTheme);
            }
        }
    }
}