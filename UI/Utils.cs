using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text.Json;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Versions of AnimKit projects.
    /// </summary>
    internal class KitVersion
    {
        public Version CLI { get; set; }
        public Version UI { get; set; }
        public Version Plugin { get; set; }

        public bool IsLoaded
        {
            get => WasLoaded;
        }

        private bool WasLoaded { get; set; } = false;

        public KitVersion()
        {
            CLI = new(0, 0, 0);
            UI = new(0, 0, 0);
            Plugin = new(0, 0, 0);
        }

        public bool Update(string versionJson)
        {
            KitVersion? version = JsonSerializer.Deserialize<KitVersion>(versionJson);

            if (version == null)
            {
                return false;
            }

            return Update(version);
        }

        public bool Update(KitVersion version)
        {
            CLI = version.CLI;
            UI = version.UI;
            Plugin = version.Plugin;
            WasLoaded = true;

            return true;
        }
    }

    internal enum UpdateAction
    {
        Continue,
        Updating,
        Shutdown,
        Manual,
    }

    internal enum VersionStatus
    {
        Unknown = -1,
        Actual,
        Outdated,
        Abandoned
    }

    /// <summary>
    /// Random utils used across various places in the tool.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Global random instance.
        /// </summary>
        public static Random Randomizer { get; } = new Random();

        /// <summary>
        /// Global HTTP client instance.
        /// </summary>
        public static HttpClient HTTPClient { get; } = new HttpClient();

        /// <summary>
        /// Cached roaming path.
        /// </summary>
        public static string RoamingPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// AnimKit versions.
        /// </summary>
        public static KitVersion ActualVersions { get; } = new KitVersion();

        /// <summary>
        /// Generate random boolean.
        /// </summary>
        /// <returns>A random boolean</returns>
        public static bool RandomBool()
        {
            return Randomizer.Next(2) == 0;
        }

        /// <summary>
        /// Gets window instance by class type.
        /// </summary>
        /// <typeparam name="T">Window class</typeparam>
        /// <returns>First opened window or null</returns>
        public static T? GetOpenedWindowByType<T>() where T : Window
        {
            return Application.Current.Windows.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets window instances by class type.
        /// </summary>
        /// <typeparam name="T">Array of windows by class</typeparam>
        /// <returns>List of opened windows</returns>
        public static T[] GetOpenedWindowsByType<T>() where T : Window
        {
            return Application.Current.Windows.OfType<T>().ToArray();
        }

        /// <summary>
        /// Gets asset window instance by asset path.
        /// </summary>
        /// <returns>First opened asset window</returns>
        public static AssetWindow? GetOpenedAssetWindow(string assetPath)
        {
            return Application.Current.Windows.OfType<AssetWindow>().FirstOrDefault(_ => _.CurrentAsset.AssetPath == assetPath);
        }

        /// <summary>
        /// Reads file by name synchronously in application's roaming folder and returns it's content as string.
        /// </summary>
        /// <param name="fileName">Target file name</param>
        /// <returns>File content or empty string</returns>
        public static string ReadRoamingFileContent(string fileName)
        {
            string filePath = GetRoamingFolderFilePath(fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }
            catch { }

            return string.Empty;
        }

        /// <summary>
        /// Writes file by name synchronously in application's roaming folder.
        /// </summary>
        /// <param name="fileName">Target file name</param>
        /// <param name="content">File string content</param>
        /// <returns>Whether or not content was written successfully</returns>
        public static bool WriteRoamingFileContent(string fileName, string content)
        {
            string filePath = GetRoamingFolderFilePath(fileName);

            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Reads file by name asynchronously in application's roaming folder and returns it's content as string.
        /// </summary>
        /// <param name="fileName">Target file name</param>
        /// <returns>File content or empty string</returns>
        public static async Task<string> ReadRoamingFileContentAsync(string fileName)
        {
            string filePath = GetRoamingFolderFilePath(fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    return await File.ReadAllTextAsync(filePath);
                }
            }
            catch { }

            return string.Empty;
        }

        /// <summary>
        /// Writes file by name asynchronously in application's roaming folder.
        /// </summary>
        /// <param name="fileName">Target file name</param>
        /// <param name="content">File string content</param>
        /// <returns>Whether or not content was written successfully</returns>
        public static async Task<bool> WriteRoamingFileContentAsync(string fileName, string content)
        {
            string filePath = GetRoamingFolderFilePath(fileName);

            try
            {
                await File.WriteAllTextAsync(filePath, content);
                return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Returns is asset/clip/animation name is valid.
        /// </summary>
        /// <param name="name">Name string</param>
        /// <param name="maxLength">Max string length</param>
        /// <param name="whitespaces">Allow whitespaces</param>
        /// <returns>Whether or not name has passed checks</returns>
        public static bool IsNameValid(string name, int maxLength = 64, bool whitespaces = true)
        {
            if (string.IsNullOrEmpty(name) || name.Length > maxLength)
            {
                return false;
            }

            bool matches = whitespaces ? Regex.IsMatch(name, @"[^\p{L}\d\s\-_@]") : Regex.IsMatch(name, @"[^\p{L}\d\-_@]");
            return !matches;
        }

        /// <summary>
        /// Opens AnimKit's repository page in a browser.
        /// </summary>
        public static void OpenRepoPage()
        {
            _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.RepoURL}") { CreateNoWindow = true });
        }

        /// <summary>
        /// Fetches and updates AnimKit actual version.
        /// </summary>
        /// <returns>Whether or not was successful</returns>
        public static async Task<bool> UpdateActualVersion()
        {
            const string versionURL = "https://raw.githubusercontent.com/CitizenFX/AnimKit/master/VERSIONS.json";
            string versionJson = await GetContentFromUrl(versionURL);

            // If version string is empty, this means we haven't got a successful response.
            // This might be caused by connection issues or downtages.
            if (string.IsNullOrEmpty(versionJson))
            {
                return false;
            }

            return ActualVersions.Update(versionJson);
        }

        /// <summary>
        /// Returns version status.
        /// </summary>
        /// <param name="actual">Actual version</param>
        /// <returns>Version status</returns>
        public static VersionStatus GetVersionStatus(Version target, Version actual)
        {
            if (IsVersionAbandoned(target, actual))
            {
                return VersionStatus.Abandoned;
            }

            if (IsVersionOutdated(target, actual))
            {
                return VersionStatus.Outdated;
            }

            return VersionStatus.Actual;
        }

        /// <summary>
        /// Download file from remote with progress.
        /// </summary>
        /// <param name="downloadURL">Download URL</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="loadProgress">Progress callback</param>
        /// <returns>Whether or not content was downloaded successfully</returns>
        public static async Task<bool> DownloadFile(string downloadURL, string filePath, Action<long, long>? loadProgress)
        {
            HttpResponseMessage? response = await HTTPClient.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return false;
            }

            long? totalSize = response.Content.Headers.ContentLength;

            if (totalSize is null or 0)
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            byte[] buffer = new byte[8192];
            long totalRead = 0L;

            FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            Stream contentStream = await response.Content.ReadAsStreamAsync();

            while (true)
            {
                int bytesRead = await contentStream.ReadAsync(buffer);

                if (bytesRead == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                totalRead += bytesRead;

                loadProgress?.Invoke(totalRead, (long)totalSize);
            }

            contentStream.Close();
            fileStream.Close();

            return true;
        }

        /// <summary>
        /// Returns is version abandoned.
        /// </summary>
        /// <param name="target">Target version</param>
        /// <param name="actual">Actual version</param>
        /// <returns>Is target version abandoned comparing to actual</returns>
        private static bool IsVersionAbandoned(Version target, Version actual)
        {
            return actual.Major > target.Major || actual.Minor > target.Minor;
        }

        /// <summary>
        /// Returns is version outdated.
        /// </summary>
        /// <param name="target">Target version</param>
        /// <param name="actual">Actual version</param>
        /// <returns>Is target version outdated comparing to actual</returns>
        private static bool IsVersionOutdated(Version target, Version actual)
        {
            return actual.Major > target.Major || actual.Minor > target.Minor || actual.Build > target.Build;
        }

        /// <summary>
        /// Combines path for a file in application's roaming folder.
        /// </summary>
        /// <param name="fileName">Target file name</param>
        /// <returns>File roaming path</returns>
        private static string GetRoamingFolderFilePath(string fileName)
        {
            return Path.Combine(RoamingPath, Constants.RoamingFolderName, fileName);
        }

        /// <summary>
        /// Gets content from URL.
        /// </summary>
        /// <param name="requestUri">Content URL</param>
        /// <returns>Content string</returns>
        private static async Task<string> GetContentFromUrl(string requestUri)
        {
            HttpResponseMessage response = await HTTPClient.GetAsync(requestUri);

            // Return content if status code is OK, otherwise just an empty string.
            return response.StatusCode == System.Net.HttpStatusCode.OK ? await response.Content.ReadAsStringAsync() : "";
        }
    }
}
