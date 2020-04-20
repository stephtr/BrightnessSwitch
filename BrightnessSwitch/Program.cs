using System;
using System.Threading;
using System.Windows.Forms;

namespace BrightnessSwitch
{
    class Program
    {
        static int maxInterventionCount = 50;
        static SupportVectorMachine predictionModel = new SupportVectorMachine();

        static Mutex mutex = new Mutex(true, "{22C62CE3-AEA8-4639-9919-F5F426795B26}");
        [STAThread]
        static void Main(string[] args)
        {
            if (!ThemeUtils.TestThemeAccess())
            {
                MessageBox.Show("This app unfortunately can't run on your PC.\nIt doesn't support Windows 10 in S mode.", "BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                return certainty > 0.3 ? (bool?)prediction : null;
            };
            trayIcon.OnExit += (object? sender, int reason) => Application.Exit();
            trayIcon.OnThemeSwitch += (object? sender, bool useLightTheme) =>
            {
                if (trayIcon.AutoSwitchEnabled) // Otherwise we don't have to learn from the current action
                {
                    var currentIlluminance = lightControl.GetCurrentIlluminance();
                    if (currentIlluminance > 0)
                    {
                        var illuminanceLog = Math.Log(currentIlluminance);

                        var positiveList = useLightTheme ? settings.interventionLightList : settings.interventionDarkList;
                        var negativeList = useLightTheme ? settings.interventionDarkList : settings.interventionLightList;

                        // If it's the first time the learn feature is being used,
                        // use the chance to set reasonable default values.
                        if (negativeList.Count == 0)
                        {
                            negativeList.Add(illuminanceLog * (useLightTheme ? 0.8 : 1.2));
                            predictionModel.b = illuminanceLog;
                        }

                        var maxIterations = 20;
                        for (var iteration = 0; iteration <= maxIterations; iteration++)
                        {
                            positiveList.Add(illuminanceLog);
                            while (positiveList.Count > maxInterventionCount)
                            {
                                positiveList.RemoveAt(0);
                            }

                            if (iteration > maxIterations / 2)
                            {
                                if (iteration >= maxIterations || negativeList.Count <= 1)
                                {
                                    // either we are already failing or we are working with the defaults
                                    // => reset the list
                                    negativeList.Clear();
                                    negativeList.Add(illuminanceLog * (useLightTheme ? 0.8 : 1.2));
                                }
                                else
                                {
                                    // just try to get rid of the least trustworthy data points
                                    negativeList.RemoveAt(useLightTheme ? negativeList.GetMaxValIndex() : negativeList.GetMinValIndex());
                                }
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
                            var prediction = predictionModel.Predict(illuminanceLog);
                            if (prediction.result == useLightTheme && prediction.certainty > 0.2)
                            {
                                settings.SaveSettings(predictionModel.b, predictionModel.w, trayIcon.AutoSwitchEnabled);
                                break;
                            }
                        }
                    }
                }

                if (!lightControl.SetTheme(useLightTheme))
                {
                    MessageBox.Show("It seems like the theme couldn't be switched.", "BrightnessSwitch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            lightControl.OnThemeSwitch += (object? sender, bool useLightTheme) => trayIcon.SetTheme(useLightTheme);

            Application.Run();
            settings.SaveSettings(predictionModel.b, predictionModel.w, trayIcon.AutoSwitchEnabled);
            mutex.ReleaseMutex();
        }
    }
}
