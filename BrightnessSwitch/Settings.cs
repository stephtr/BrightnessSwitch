using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrightnessSwitch
{
    public class Settings
    {
        string settingsFilename = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\BrightnessSwitch.config";
        bool canSaveSettings = true;

        public List<double> interventionDarkList = new List<double>();
        public List<double> interventionLightList = new List<double>();

        public (double predictionB, double predictionW, bool enableLightAutomatic) LoadSettings()
        {
            float defaultIlluminanceThreshold = 5000; // Lux
            interventionDarkList.Clear();
            interventionLightList.Clear();

            try
            {
                var enableLightAutomatic = true;
                using var reader = new BinaryReader(new FileStream(settingsFilename, FileMode.Open), Encoding.ASCII);
                if (new string(reader.ReadChars(2)) != "BS")
                {
                    canSaveSettings = false;
                    throw new IOException();
                }
                var versionSelector = reader.ReadChar();
                switch (versionSelector)
                {
                    case 'v':
                        if (reader.ReadChar() != '1') throw new IOException();
                        break;
                    case '+':
                        var version = reader.ReadUInt32();
                        if (version != 2) throw new IOException();
                        enableLightAutomatic = reader.ReadBoolean();
                        break;
                    default: throw new IOException();
                }
                var b = reader.ReadDouble();
                var w = reader.ReadDouble();
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
                return (b, w, enableLightAutomatic);
            }
            catch
            {
                interventionDarkList.Add(Math.Log(defaultIlluminanceThreshold) * 0.9);
                interventionLightList.Add(Math.Log(defaultIlluminanceThreshold) * 1.1);
                return (Math.Log(defaultIlluminanceThreshold), Math.Log(defaultIlluminanceThreshold) * 0.1, true);
            }
        }

        private const uint CurrentFileFormatVersion = 2;
        public void SaveSettings(double predictionB, double predictionW, bool enableLightAutomatic)
        {
            if (!canSaveSettings) return;
            try
            {
                using var writer = new BinaryWriter(new FileStream(settingsFilename, FileMode.Create), Encoding.ASCII);
                writer.Write("BS+".ToCharArray());
                writer.Write(CurrentFileFormatVersion);
                writer.Write(enableLightAutomatic);
                writer.Write(predictionB);
                writer.Write(predictionW);
                writer.Write(interventionDarkList.Count);
                writer.Write(interventionLightList.Count);
                interventionDarkList.ForEach((v) => writer.Write(v));
                interventionLightList.ForEach((v) => writer.Write(v));
                writer.Close();
            }
            catch { }
        }
    }
}