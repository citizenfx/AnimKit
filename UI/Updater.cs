using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using AnimKit.Core;

namespace AnimKit.UI
{
    internal static class Updater
    {
        public static Version CurrentVersion { get; } = new(1, 1, 0);

        public static Version ActualVersion => Utils.ActualVersions.UI;

        private static string UpdaterArchive { get; } = "AnimKit.new.zip";

        private static string UpdaterExecutable { get; } = "AnimKit.new.exe";

        private static string TargetExecutable { get; } = "AnimKit.UI.exe";

        public static UpdateAction CheckVersion()
        {
            if (!Utils.ActualVersions.IsLoaded)
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

            // Abandoned version is a version that missing major or minor updates.
            bool isAbandoned = versionStatus == VersionStatus.Abandoned;

            string versionStatusHelp = isAbandoned ?
                "adandoned (very outdated, missing important fixes, improvements and etc)" :
                $"outdated (missing {ActualVersion.Build - CurrentVersion.Build} patches)";

            string warningHelp = isAbandoned ?
                "Otherwise the program will be shut down" :
                "You can continue, but update as soon as possible";

            string messageText = string.Join("\n",
                $"Your AnimKit version ({CurrentVersion}) is {versionStatusHelp}!",
                $"Current version: {ActualVersion}",
                "Do you want to download it now?",
                warningHelp);

            MessageBoxResult outdatedMessage = MessageBox.Show(messageText,
                Constants.BrandingName, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (outdatedMessage == MessageBoxResult.Yes)
            {
                return UpdateAction.Updating;
            }

            // If the app is not too outdated allow user to use it.
            return isAbandoned ? UpdateAction.Shutdown : UpdateAction.Continue;
        }

        public static async Task<bool> DownloadLatest(Action<long, long>? loadProgress)
        {
            string downloadURL = $"{Constants.RepoURL}/releases/download/v{ActualVersion}/AnimKit.UI.zip";
            return await Utils.DownloadFile(downloadURL, UpdaterArchive, loadProgress);
        }

        public static bool UnpackLatest()
        {
            if (!File.Exists(UpdaterArchive))
            {
                return false;
            }

            try
            {
                // Open archive from downloaded target.
                ZipArchive archive = ZipFile.Open(UpdaterArchive, ZipArchiveMode.Read);

                // Getting executable entry.
                ZipArchiveEntry? entry = archive.GetEntry(TargetExecutable);

                if (entry == null)
                {
                    return false;
                }

                string filePath = Path.Combine(Directory.GetCurrentDirectory(), UpdaterExecutable);

                // Delete if "new" executable already exist.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Extract "new" executable.
                entry.ExtractToFile(filePath);

                // Dispose opened archive.
                archive.Dispose();

                // Remove downloaded archive.
                File.Delete(UpdaterArchive);

                return true;
            }
            catch { }

            return false;
        }

        public static bool EnsureUpdater()
        {
            string processName = Process.GetCurrentProcess().ProcessName;

            if (!processName.EndsWith(".new"))
            {
                return false;
            }

            // Wait until we will be able to remove executable.
            while (true)
            {
                try
                {
                    if (!File.Exists(TargetExecutable))
                    {
                        break;
                    }

                    File.Delete(TargetExecutable);
                }
                catch { }
            }

            // Then copy updater executable as a target name.
            File.Copy(UpdaterExecutable, TargetExecutable);

            _ = Process.Start(TargetExecutable);

            return true;
        }
    }
}
