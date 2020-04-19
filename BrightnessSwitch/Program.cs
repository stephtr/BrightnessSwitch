using System;
using System.Threading;
using System.Windows.Forms;

namespace BrightnessSwitch
{
    class Program
    {
        static int maxInterventionCount = 100;
        static SupportVectorMachine predictionModel = new SupportVectorMachine();

        static Mutex mutex = new Mutex(true, "{22C62CE3-AEA8-4639-9919-F5F426795B26}");
        [STAThread]
        static void Main(string[] args)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                return;
            }

            var settings = new Settings();
            var currentSettings = settings.LoadSettings();
            predictionModel.b = currentSettings.predictionB;
            predictionModel.w = currentSettings.predictionW;

            LightControl lightControl = new LightControl();

            var trayIcon = new TrayIcon(currentSettings.enableLightAutomatic, lightControl.LightSensorAvailable);
            lightControl.PredictionCallback += (float illuminanceInLux) =>
            {
                if (!trayIcon.AutoSwitchEnabled)
                {
                    return null;
                }
                var (prediction, certainty) = predictionModel.Predict(Math.Log(illuminanceInLux));
                return certainty > 0.5 ? (bool?)prediction : null;
            };
            trayIcon.OnExit += (object? sender, int reason) => Application.Exit();
            trayIcon.OnThemeSwitch += (object? sender, bool useLightTheme) =>
            {
                if (trayIcon.AutoSwitchEnabled) // Otherwise we don't have to learn from the current action
                {
                    var currentIlluminance = lightControl.GetCurrentIlluminance();
                    if (currentIlluminance > 0)
                    {
                        for (var additionRun = 0; additionRun < 10; additionRun++)
                        {
                            if (useLightTheme)
                            {
                                settings.interventionLightList.Add(Math.Log(currentIlluminance));
                                while (settings.interventionLightList.Count > maxInterventionCount)
                                {
                                    settings.interventionLightList.RemoveAt(0);
                                }
                            }
                            else
                            {
                                settings.interventionDarkList.Add(Math.Log(currentIlluminance));
                                while (settings.interventionDarkList.Count > maxInterventionCount)
                                {
                                    settings.interventionDarkList.RemoveAt(0);
                                }
                            }
                            if (settings.interventionDarkList.Count < 1 || settings.interventionLightList.Count < 1)
                            {
                                break;
                            }
                            var interventionCount = settings.interventionDarkList.Count + settings.interventionLightList.Count;
                            var illuminances = new double[interventionCount];
                            var lightThemes = new bool[interventionCount];
                            var weights = new double[interventionCount];
                            int listIndex = 0;
                            for (var i = 0; i < settings.interventionDarkList.Count; i++)
                            {
                                illuminances[listIndex] = settings.interventionDarkList[i];
                                lightThemes[listIndex] = false;
                                weights[listIndex] = (i + 1.0 + maxInterventionCount - settings.interventionDarkList.Count) / maxInterventionCount;
                                listIndex++;
                            }
                            for (var i = 0; i < settings.interventionLightList.Count; i++)
                            {
                                illuminances[listIndex] = settings.interventionLightList[i];
                                lightThemes[listIndex] = true;
                                weights[listIndex] = (i + 1.0 + maxInterventionCount - settings.interventionLightList.Count) / maxInterventionCount;
                                listIndex++;
                            }
                            predictionModel.Train(illuminances, lightThemes, weights);
                            var prediction = predictionModel.Predict(Math.Log(currentIlluminance));
                            if (prediction.result == useLightTheme)
                            {
                                settings.SaveSettings(predictionModel.b, predictionModel.w, trayIcon.AutoSwitchEnabled);
                                break;
                            }
                        }
                    }
                }

                lightControl.SetTheme(useLightTheme);
            };
            lightControl.OnThemeSwitch += (object? sender, bool useLightTheme) => trayIcon.SetTheme(useLightTheme);

            Application.Run();
            settings.SaveSettings(predictionModel.b, predictionModel.w, trayIcon.AutoSwitchEnabled);
            mutex.ReleaseMutex();
        }
    }
}
