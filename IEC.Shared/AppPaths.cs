using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.IO;

namespace IEC.Shared
{
    public static class AppInfo
    {
        public const string Product = "VEMT";
        public const string Company = "Vertex Automation System Pvt Ltd";
        public const string Version = "1.0.0";
    }
    public static class AppPaths
    {
        public static readonly string Root =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                AppInfo.Product);

        public static readonly string Configuration =
            Path.Combine(Root, "Configuration");

        public static readonly string Data =
            Path.Combine(Root, "Data");

        public static readonly string Reports =
            Path.Combine(Root, "Reports");

        public static readonly string Logs =
            Path.Combine(Root, "Logs");

        // Configuration files
        public static readonly string ProjectConfigurationFile =
            Path.Combine(Configuration, "ProjectConfig.json");

        public static readonly string IecConfigurationFile =
            Path.Combine(Configuration, "iec61850_config.json");

        public static readonly string ReportFormatFile =
            Path.Combine(Configuration, "ReportFormats.json");

        static AppPaths()
        {
            Directory.CreateDirectory(Configuration);
            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Reports);
            Directory.CreateDirectory(Logs);
        }

        public static void EnsureDefaultFile(string destinationFile, string defaultFile)
        {
            try
            {
                if (!File.Exists(destinationFile) && File.Exists(defaultFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                    File.Copy(defaultFile, destinationFile);
                }
            }
            catch
            {
                // Ignore copy failures
            }
        }
    }

}