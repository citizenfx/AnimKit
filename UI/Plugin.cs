using AnimKit.Core;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;

namespace AnimKit.UI
{
    internal enum PluginStatus
    {
        Missing,
        Installed,
        Outdated,
    }

    internal static class Plugin
    {
        public static Version? CurrentVersion { get; set; }

        public static Version ActualVersion => Utils.ActualVersions.Plugin;

        public static string MaxPath
        {
            get => (Settings.Instance != null) ? Settings.Instance.MaxPath : string.Empty;
            set
            {
                if (Settings.Instance != null)
                {
                    Settings.Instance.MaxPath = value;
                    _ = Settings.Instance.Save();
                }
            }
        }

        /// <summary>
        /// Required 3ds Max plugin files list.
        /// </summary>
        public static IEnumerable RequiredFiles { get; } = new string[]
        {
            "ofio.v.anim.ms",
            "ofio.v.gui.ms",
            "ofio.v.log.ms",
            "ofio.v.main.ms",
            "ofio.v.utils.ms",
            "ofio.v.version.ms",
        };

        private static string PluginArchive { get; } = "AnimKit.Plugin.zip";

        public static async Task<bool> DownloadPlugin(Action<long, long>? loadProgress)
        {
            if (MaxPath == string.Empty)
            {
                return false;
            }

            string downloadURL = $"{Constants.RepoURL}/releases/download/v{ActualVersion}/AnimKit.Plugin.zip";
            return await Utils.DownloadFile(downloadURL, PluginArchive, loadProgress);
        }

        public static bool UnpackDownloaded()
        {
            if (!File.Exists(PluginArchive))
            {
                return false;
            }

            string pluginsPath = GetMaxPluginsFolder();

            if (pluginsPath == string.Empty)
            {
                return false;
            }

            try
            {
                // Open archive from downloaded target.
                ZipArchive archive = ZipFile.Open(PluginArchive, ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    string filePath = Path.Combine(pluginsPath, entry.FullName);

                    // Delete if plugin file already exist.
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // Extract file path.
                    entry.ExtractToFile(filePath);
                }

                // Dispose opened archive.
                archive.Dispose();

                // Remove downloaded archive.
                File.Delete(PluginArchive);

                return true;
            }
            catch { }

            return false;
        }

        public static bool UninstallPlugin()
        {
            string pluginsPath = GetMaxPluginsFolder();

            if (pluginsPath == string.Empty)
            {
                return false;
            }

            try
            {
                foreach (string fileName in RequiredFiles)
                {
                    string filePath = Path.Combine(pluginsPath, fileName);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch
            {
                return false;
            }

            CurrentVersion = null;

            return true;
        }

        public static bool CheckFiles()
        {
            string pluginsPath = GetMaxPluginsFolder();

            if (pluginsPath == string.Empty)
            {
                return false;
            }

            foreach (string fileName in RequiredFiles)
            {
                if (!File.Exists(Path.Combine(pluginsPath, fileName)))
                {
                    return false;
                }
            }

            return true;
        }

        public static UpdateAction CheckVersion()
        {
            if (!Utils.ActualVersions.IsLoaded || CurrentVersion == null)
            {
                // Let continue.
                return UpdateAction.Continue;
            }

            VersionStatus versionStatus = Utils.GetVersionStatus(CurrentVersion, ActualVersion);

            // If the current version is actual - continue launching.
            if (versionStatus == VersionStatus.Actual)
            {
                return UpdateAction.Continue;
            }

            string messageText = string.Join("\n",
                $"Your plugin version ({CurrentVersion}) is outdated!",
                "You might miss important fixes, improvements and etc.",
                "Please download the latest plugin version from the Manage Plugin window.");

            _ = MessageBox.Show(messageText,
                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

            return UpdateAction.Continue;
        }

        public static bool LoadVersion()
        {
            string pluginsPath = GetMaxPluginsFolder();

            if (pluginsPath == string.Empty)
            {
                return false;
            }

            string[] versionLines = File.ReadAllLines(Path.Combine(pluginsPath, "ofio.v.version.ms"));

            foreach (string fileLine in versionLines)
            {
                string safeLine = fileLine.Trim().ToLower();

                if (safeLine.StartsWith("global ofioversion"))
                {
                    string? versionValue = safeLine.Split("=")?[1]?.Trim();

                    if (versionValue != null)
                    {
                        // Make from "\"1.0.0\"" "1.0.0" string.
                        string versionString = versionValue.Replace("\"", "");

                        CurrentVersion = new(versionString);
                        return true;
                    }

                    // If line is found, don't search for more.
                    break;
                }
            }

            return false;
        }

        private static string GetMaxPluginsFolder()
        {
            return (MaxPath == string.Empty) ? string.Empty : Path.Combine(MaxPath, "scripts", "Startup");
        }
    }
}
