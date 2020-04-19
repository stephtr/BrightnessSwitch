using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace BrightnessSwitch
{
    class Program
    {
        static string settingsFilename = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\BrightnessSwitch.config";
        static string settingsFileHeader = "BSv1";
        static bool canSaveSettings = true;

        static List<double> interventionDarkList = new List<double>();
        static List<double> interventionLightList = new List<double>();
        static int maxInterventionCount = 100;
        static SupportVectorMachine predictionModel = new SupportVectorMachine();

        public static void LoadSettings()
        {
            float defaultIlluminanceThreshold = 5000; // Lux
            interventionDarkList.Clear();
            interventionLightList.Clear();

            try
            {
                using var reader = new BinaryReader(new FileStream(settingsFilename, FileMode.Open), Encoding.ASCII);
                if (new string(reader.ReadChars(4)) != settingsFileHeader)
                {
                    canSaveSettings = false;
                    throw new IOException();
                }
                predictionModel.b = reader.ReadDouble();
                predictionModel.w = reader.ReadDouble();
                var darkCount = reader.ReadInt32();
                var lightCount = reader.ReadInt32();
                for (var i = 0; i < darkCount; i++)
                {
                    interventionDarkList.Add(reader.ReadDouble());
                }
                for (var i = 0; i < lightCount; i++)
                {
                    interventionLightList.Add(reader.ReadDouble());
                }
            }
            catch
            {
                predictionModel.b = Math.Log(defaultIlluminanceThreshold);
                predictionModel.w = Math.Log(defaultIlluminanceThreshold) * 0.1;

                interventionDarkList.Add(Math.Log(defaultIlluminanceThreshold) * 0.9);
                interventionLightList.Add(Math.Log(defaultIlluminanceThreshold) * 1.1);
            }
        }

        public static void SaveSettings()
        {
            if (!canSaveSettings) return;
            try
            {
                using var writer = new BinaryWriter(new FileStream(settingsFilename, FileMode.Create), Encoding.ASCII);
                writer.Write(settingsFileHeader.ToCharArray());
                writer.Write(predictionModel.b);
                writer.Write(predictionModel.w);
                writer.Write(interventionDarkList.Count);
                writer.Write(interventionLightList.Count);
                interventionDarkList.ForEach((v) => writer.Write(v));
                interventionLightList.ForEach((v) => writer.Write(v));
                writer.Close();
            }
            catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            LoadSettings();

            var lightControl = new LightControl();
            lightControl.PredictionCallback += (float illuminanceInLux) =>
            {
                var (prediction, certainty) = predictionModel.Predict(Math.Log(illuminanceInLux));
                return certainty > 0.5 ? (bool?)prediction : null;
            };

            var trayIcon = new TrayIcon();
            trayIcon.OnExit += (object? sender, int reason) => Application.Exit();
            trayIcon.OnThemeSwitch += (object? sender, bool useLightTheme) =>
            {
                var currentIlluminance = lightControl.GetCurrentIlluminance();
                if (currentIlluminance > 0)
                {
                    for (var additionRun = 0; additionRun < 10; additionRun++)
                    {
                        if (useLightTheme)
                        {
                            interventionLightList.Add(Math.Log(currentIlluminance));
                            while (interventionLightList.Count > maxInterventionCount)
                            {
                                interventionLightList.RemoveAt(0);
                            }
                        }
                        else
                        {
                            interventionDarkList.Add(Math.Log(currentIlluminance));
                            while (interventionDarkList.Count > maxInterventionCount)
                            {
                                interventionDarkList.RemoveAt(0);
                            }
                        }
                        if (interventionDarkList.Count < 1 || interventionLightList.Count < 1)
                        {
                            break;
                        }
                        var interventionCount = interventionDarkList.Count + interventionLightList.Count;
                        var illuminances = new double[interventionCount];
                        var lightThemes = new bool[interventionCount];
                        var weights = new double[interventionCount];
                        int listIndex = 0;
                        for (var i = 0; i < interventionDarkList.Count; i++)
                        {
                            illuminances[listIndex] = interventionDarkList[i];
                            lightThemes[listIndex] = false;
                            weights[listIndex] = (i + 1) / (double)interventionDarkList.Count;
                            listIndex++;
                        }
                        for (var i = 0; i < interventionLightList.Count; i++)
                        {
                            illuminances[listIndex] = interventionLightList[i];
                            lightThemes[listIndex] = true;
                            weights[listIndex] = (i + 1) / (double)interventionLightList.Count;
                            listIndex++;
                        }
                        predictionModel.Train(illuminances, lightThemes, weights);
                        var prediction = predictionModel.Predict(Math.Log(currentIlluminance));
                        if (prediction.result == useLightTheme)
                        {
                            SaveSettings();
                            break;
                        }
                    }
                }

                lightControl.SetTheme(useLightTheme);
            };
            lightControl.OnThemeSwitch += (object? sender, bool useLightTheme) => trayIcon.SetTheme(useLightTheme);

            Application.Run();
            SaveSettings();
        }
    }
}
