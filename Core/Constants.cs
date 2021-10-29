namespace AnimKit.Core
{
    /// <summary>
    /// Random global stuff used across the whole project. Perhaps this can be ditched one day.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Repository page URL.
        /// </summary>
        public static string RepoURL { get; } = "https://github.com/CitizenFX/AnimKit";

        /// <summary>
        /// Folder name of the tool for storing Roaming data.
        /// </summary>
        public static string RoamingFolderName { get; } = "AnimKitFive";

        /// <summary>
        /// Branding name.
        /// </summary>
        public static string BrandingName { get; } = "AnimKit by Cfx.re";

        /// <summary>
        /// File name of asset JSON manifest.
        /// </summary>
        public static string AssetManifestName { get; } = "fxanimation.json";
    }
}
