using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using CodeWalker.GameFiles;

namespace AnimKit.Core
{
    public enum AssetLoadType
    {
        /// <summary>
        /// Asset wasn't loaded yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// Load asset fully with all the data about animations.
        /// </summary>
        Full,

        /// <summary>
        /// Don't load animations of asset.
        /// </summary>
        NoAnimations
    }

    public enum AssetImportStage
    {
        /// <summary>
        /// Only load asset meta data.
        /// </summary>
        Meta = 0,

        /// <summary>
        /// Load meta data and animations.
        /// </summary>
        Animations = 1,

        /// <summary>
        /// Load meta data, animations and clips.
        /// </summary>
        Clips = 2,
    }

    public class AssetClipAnimation
    {
        /// <summary>
        /// Associated animation name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Clip start time.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// Clip end time.
        /// </summary>
        public float EndTime { get; set; }

        /// <summary>
        /// Clip rate.
        /// </summary>
        public float Rate { get; set; }
    }

    public class AssetClip
    {
        /// <summary>
        /// Clip name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Clip duration.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Animations associated to clip.
        /// </summary>
        public List<AssetClipAnimation> Animations { get; set; }
    }

    public class AssetFile
    {
        /// <summary>
        /// Manifest version for backward compatibility.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Timestamp of when asset what used last time.
        /// </summary>
        public string LastUpdated { get; set; }

        /// <summary>
        /// Asset name used only for user-friendliness.
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Output clip dictionary name, i.e. "clip@dictionary@five.ycd".
        /// </summary>
        public string ExportName { get; set; }

        /// <summary>
        /// Where the last time user exported clip dictionary.
        /// </summary>
        public string LastExportPath { get; set; }

        /// <summary>
        /// OAD file name.
        /// </summary>
        public string DictionaryName { get; set; }

        /// <summary>
        /// User defined clips.
        /// </summary>
        public AssetClip[] Clips { get; set; }
    }

    public class Asset
    {
        /// <summary>
        /// Asset name used only for user-friendliness.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Output clip dictionary name, i.e. "clip@dictionary@five.ycd".
        /// </summary>
        public string ExportName { get; set; }

        /// <summary>
        /// Saved last asset export path.
        /// </summary>
        public string LastExportPath { get; set; }

        /// <summary>
        /// Asset animation dictionary (oad file).
        /// </summary>
        public OadFile Dictionary { get; set; }

        /// <summary>
        /// Asset's last change date.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// List of asset's clips.
        /// </summary>
        public List<AssetClip> Clips { get; set; }

        /// <summary>
        /// Path to asset's root folder.
        /// </summary>
        public string AssetPath { get; set; }

        /// <summary>
        /// Asset load type.
        /// </summary>
        private AssetLoadType LoadType { get; set; } = AssetLoadType.None;

        /// <summary>
        /// Load from asset's data.
        /// </summary>
        /// <param name="assetData">Asset data struct</param>
        /// <param name="assetPath">Path to asset folder</param>
        /// <param name="loadType">Asset load type</param>
        /// <param name="loadProgress">Load progress callback</param>
        /// <returns>Whether or not was loaded successfully.</returns>
        public bool Load(AssetFile assetData, string assetPath, AssetLoadType loadType = AssetLoadType.Full, Action<int, int> loadProgress = null)
        {
            AssetPath = assetPath;
            Name = assetData.AssetName ?? "Your awesome asset";
            ExportName = assetData.ExportName ?? "your@awesome@asset";
            LastExportPath = assetData.LastExportPath ?? string.Empty;
            Clips = assetData.Clips?.ToList() ?? new List<AssetClip>();

            bool parsed = long.TryParse(assetData.LastUpdated, out long timestamp);
            LastUpdated = parsed ? DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime : DateTime.Now;

            Dictionary = new OadFile();
            Dictionary.Name = assetData.DictionaryName;
            Dictionary.Load(Path.Combine(assetPath, assetData.DictionaryName + ".oad"), loadType, loadProgress);
            LoadType = loadType;

            return true;
        }

        /// <summary>
        /// Load from asset's root path.
        /// </summary>
        /// <param name="assetPath">Path to asset folder</param>
        /// <param name="loadType">Asset load type</param>
        /// <param name="loadProgress">Load progress callback</param>
        /// <returns>Whether or not was loaded successfully.</returns>
        public bool Load(string assetPath, AssetLoadType loadType = AssetLoadType.Full, Action<int, int> loadProgress = null)
        {
            string filePath = Path.Combine(assetPath, Constants.AssetManifestName);

            if (!File.Exists(filePath))
            {
                return false;
            }

            string jsonString = File.ReadAllText(filePath);
            AssetFile assetData = JsonSerializer.Deserialize<AssetFile>(jsonString);

            if (assetData == null)
            {
                return false;
            }

            return Load(assetData, assetPath, loadType, loadProgress);
        }

        /// <summary>
        /// Save assset to specific folder.
        /// </summary>
        /// <param name="assetPath">Saving folder</param>
        /// <returns>Whether or not was saved successfully.</returns>
        /// <exception cref="Exception"></exception>
        public bool Save(string assetPath)
        {
            if (Dictionary == null)
            {
                throw new Exception("Attempt to save asset with no Dictionary");
            }

            if (LoadType != AssetLoadType.Full)
            {
                throw new Exception("Attempt to save a non-fully loaded assets");
            }

            Dictionary.Save(assetPath);

            string lastUpdated = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

            AssetFile assetFile = new AssetFile
            {
                AssetName = Name,
                LastUpdated = lastUpdated,
                DictionaryName = Dictionary.Name,
                LastExportPath = LastExportPath,
                ExportName = ExportName,
                Clips = Clips.ToArray()
            };

            string jsonString = JsonSerializer.Serialize(assetFile, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string filePath = Path.Combine(assetPath, Constants.AssetManifestName);
            string dictionaryPath = Path.Combine(assetPath, Dictionary.Name);

            File.WriteAllText(filePath, jsonString);

            if (!Directory.Exists(dictionaryPath))
            {
                _ = Directory.CreateDirectory(dictionaryPath);
            }

            return true;
        }

        /// <summary>
        /// Save assset to current asset's folder.
        /// </summary>
        /// <returns>Whether or not was saved successfully.</returns>
        /// <exception cref="Exception"></exception>
        public bool Save()
        {
            if (AssetPath == null)
            {
                throw new Exception("Attempt to save asset with no asset path");
            }

            return Save(AssetPath);
        }

        /// <summary>
        /// Import raw clip dictionary file as asset.
        /// </summary>
        /// <param name="dataRaw">Raw #CD file data</param>
        /// <param name="loadProgress">Load progress callback</param>
        /// <returns>Whether or not was imported successfully.</returns>
        /// <exception cref="Exception"></exception>
        public bool ImportRaw(byte[] dataRaw, Action<AssetImportStage, int, int> loadProgress)
        {
            YcdFile ycdFile = new YcdFile();
            RpfFile.LoadResourceFile(ycdFile, dataRaw, 46);

            if (string.IsNullOrEmpty(AssetPath))
            {
                throw new Exception("Attempt to save YCD file with no asset path");
            }

            string basePath = Path.Combine(AssetPath, Dictionary.Name);

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            int loadedAnimations = 0;
            int overallAnimations = ycdFile.AnimMap.Count;

            // Storing a list of original animation and onim file for clip loading.
            var animationOnims = new List<KeyValuePair<Animation, OnimFile>>();

            foreach (var animMap in ycdFile.AnimMap)
            {
                string onimPath = Path.Combine(basePath, animMap.Key.ToString() + ".onim");

                if (File.Exists(onimPath))
                {
                    File.Delete(onimPath);
                }

                OnimFile prevOnim = Dictionary.OnimFiles.Find(_ => _.Name == animMap.Key.ToString());

                if (prevOnim != null)
                {
                    _ = Dictionary.OnimFiles.Remove(prevOnim);
                }

                OnimFile onimFile = new OnimFile();
                onimFile.Load(animMap.Value.Animation, onimPath);

                Dictionary.OnimFiles.Add(onimFile);

                animationOnims.Add(new KeyValuePair<Animation, OnimFile>(animMap.Value.Animation, onimFile));

                loadProgress?.Invoke(AssetImportStage.Animations, ++loadedAnimations, overallAnimations);
            }

            int loadedClips = 0;
            int overallClips = ycdFile.ClipMap.Count;

            foreach (var clipMap in ycdFile.ClipMap)
            {
                string clipName = clipMap.Value.Clip.Name.Replace("pack:/", "").Replace(".clip", "");
                int attempts = 0;

                while (Clips.Find(_ => _.Name == clipName) != null && attempts++ < 50)
                {
                    clipName = $"{clipName}_{new Random().Next(1000, 10000)}";
                }

                // Failed to generate name somehow...
                if (attempts >= 50)
                {
                    continue;
                }

                AssetClip assetClip = new AssetClip
                {
                    Name = clipName,
                    Animations = new List<AssetClipAnimation>(),
                };

                try
                {
                    ClipAnimation clipAnimEntry = clipMap.Value.Clip as ClipAnimation;

                    if (clipAnimEntry?.Animation != null)
                    {
                        assetClip.Duration = clipAnimEntry.GetPlaybackTime(0.0f);

                        var clipOnimData = animationOnims.Find(_ => _.Key == clipAnimEntry.Animation);

                        AssetClipAnimation clipAnim = new AssetClipAnimation
                        {
                            Name = clipOnimData.Value.Name,
                            Rate = clipAnimEntry.Rate,
                            StartTime = clipAnimEntry.StartTime,
                            EndTime = clipAnimEntry.EndTime
                        };

                        assetClip.Animations.Add(clipAnim);

                        loadProgress?.Invoke(AssetImportStage.Animations, ++loadedClips, overallAnimations);

                        // Don't try to find clip animation list of this was a ClipAnimation
                        Clips.Add(assetClip);
                        continue;
                    }
                }
                catch { }

                try
                {
                    ClipAnimationList clipAnimListEntry = clipMap.Value.Clip as ClipAnimationList;

                    if (clipAnimListEntry?.Animations != null)
                    {
                        assetClip.Duration = clipAnimListEntry.GetPlaybackTime(0.0f);

                        foreach (var listItem in clipAnimListEntry.Animations)
                        {
                            if (listItem?.Animation != null)
                            {
                                var clipOnimData = animationOnims.Find(_ => _.Key == listItem.Animation);

                                AssetClipAnimation clipAnim = new AssetClipAnimation
                                {
                                    Name = clipOnimData.Value.Name,
                                    Rate = listItem.Rate,
                                    StartTime = listItem.StartTime,
                                    EndTime = listItem.EndTime
                                };

                                assetClip.Animations.Add(clipAnim);

                                loadProgress?.Invoke(AssetImportStage.Animations, ++loadedClips, overallClips);
                            }
                        }

                        Clips.Add(assetClip);
                        continue;
                    }
                }
                catch { }
            }

            // In the end - we're going to be saved.
            _ = Save();

            return true;
        }

        /// <summary>
        /// Import openFormats animation dictionary as asset.
        /// </summary>
        /// <param name="oadPath">Path to oad file</param>
        /// <returns>Whether or not was imported successfully.</returns>
        public bool ImportOpen(string oadPath)
        {
            if (!File.Exists(oadPath))
            {
                return false;
            }

            string basePath = Path.Combine(AssetPath, Dictionary.Name);

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            OadFile oadFile = new OadFile();
            oadFile.Load(oadPath, AssetLoadType.NoAnimations);

            foreach (string onimPath in oadFile.OnimPaths)
            {
                string targetPath = Path.Combine(oadFile.FileDirectory, onimPath);
                Dictionary.AddOnim(targetPath);
            }

            return true;
        }

        /// <summary>
        /// Export asset as raw clip dictionary file.
        /// </summary>
        /// <param name="outPath">Export path</param>
        /// <returns>Whether or not was exported successfully.</returns>
        public bool ExportRaw(string outPath)
        {
            try
            {
                if (File.Exists(outPath))
                {
                    File.Delete(outPath);
                }

                byte[] ycdRaw = Dictionary.BuildYcd(Clips);
                File.WriteAllBytes(outPath, ycdRaw);

                return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Export asset as openFormats animation dictionary.
        /// </summary>
        /// <param name="outPath">Export path</param>
        /// <returns>Whether or not was exported successfully.</returns>
        public bool ExportOpen(string outPath)
        {
            bool success = Save(outPath);

            if (!success)
            {
                return false;
            }

            string dictPath = Path.Combine(outPath, Dictionary.Name);

            if (!Directory.Exists(dictPath))
            {
                Directory.CreateDirectory(dictPath);
            }

            try
            {
                foreach (string onimPath in Dictionary.OnimPaths)
                {
                    string targetPath = Path.Combine(outPath, onimPath);
                    string sourcePath = Path.Combine(Dictionary.FileDirectory, onimPath);

                    File.Copy(sourcePath, targetPath);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Export single animation file as raw clip dictionary file.
        /// </summary>
        /// <param name="onimPath">Path to animation file</param>
        /// <param name="outPath">Export path</param>
        /// <returns>Whether or not was exported successfully.</returns>
        public bool ExportSingle(string onimPath, string outPath)
        {
            OnimFile onimFile = new OnimFile();
            onimFile.Load(onimPath, AssetLoadType.Full);
            return ExportSingle(onimFile, outPath);
        }

        /// <summary>
        /// Export single animation data as raw clip dictionary file.
        /// </summary>
        /// <param name="onimFile">Animation data</param>
        /// <param name="outPath">Export path</param>
        /// <returns>Whether or not was exported successfully.</returns>
        public bool ExportSingle(OnimFile onimFile, string outPath)
        {
            try
            {
                string fileName = $"{ExportName}@{onimFile.Name}.ycd";
                string exportPath = Path.Combine(outPath, fileName);

                if (File.Exists(exportPath))
                {
                    File.Delete(exportPath);
                }

                OadFile oadFile = new OadFile();
                oadFile.Version = new string[] { "1", "2" };
                oadFile.OnimFiles.Add(onimFile);

                byte[] ycdRaw = oadFile.BuildYcd();
                File.WriteAllBytes(exportPath, ycdRaw);

                return true;
            }
            catch { }

            return false;
        }
    }
}
