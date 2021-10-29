using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnimKit.UI
{
    /// <summary>
    /// Settings manager.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Global settings instance.
        /// </summary>
        public static Settings? Instance { get; set; }

        /// <summary>
        /// Settings version.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Path of 3ds Max installation.
        /// </summary>
        public string MaxPath { get; set; } = "";

        /// <summary>
        /// List of path to known assets.
        /// </summary>
        public List<string> Assets { get; set; } = new();

        /// <summary>
        /// File name of settings JSON.
        /// </summary>
        private static string SettingsName { get; } = "settings.json";

        /// <summary>
        /// Load settings from roaming folder.
        /// </summary>
        /// <returns>Whether or not settings was loaded</returns>
        public static async Task<bool> Load()
        {
            string jsonSettings = await Utils.ReadRoamingFileContentAsync(SettingsName);

            if (string.IsNullOrEmpty(jsonSettings))
            {
                Instance = new Settings();
                return await Instance.Save();
            }

            Instance = JsonSerializer.Deserialize<Settings>(jsonSettings);

            return Instance != null;
        }

        /// <summary>
        /// Save settings to roaming folder.
        /// </summary>
        /// <returns>Whether or not settings was saved</returns>
        public async Task<bool> Save()
        {
            string jsonSettings = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return await Utils.WriteRoamingFileContentAsync(SettingsName, jsonSettings);
        }
    }
}
