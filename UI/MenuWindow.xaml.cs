using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for MenuWindow.xaml
    /// </summary>
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens asset window by path.
        /// </summary>
        /// <param name="assetPath">Path to asset folder</param>
        private void OpenAssetWindow(string assetPath)
        {
            if (Settings.Instance == null)
            {
                return;
            }

            AssetWindow? assetWindow = Utils.GetOpenedAssetWindow(assetPath);

            // If this asset is already in use, do beep and focus window.
            if (assetWindow != null)
            {
                SystemSounds.Beep.Play();
                _ = assetWindow.Focus();
                return;
            }

            // Find if the openning asset exist in recent list.
            bool inRecents = Settings.Instance.Assets.Exists(_ => _ == assetPath);

            if (!inRecents)
            {
                // Add to recents if it wasn't added yet.
                Settings.Instance.Assets.Add(assetPath);
                _ = Settings.Instance.Save();

                // Update list since now we have one more entry.
                UpdateRecentAssetsList();
            }

            // Creating new asset window and showing it.
            assetWindow = new(assetPath);
            assetWindow.Closed += AssetWindow_Closed;
            assetWindow.AssetUpdated += AssetWindow_AssetUpdated;
            assetWindow.Show();
        }

        /// <summary>
        /// Updates recent assets list.
        /// </summary>
        private void UpdateRecentAssetsList()
        {
            if (Settings.Instance == null)
            {
                return;
            }

            RecentAssetsList.RecentStackPanel.Children.Clear();

            List<RecentItem> recentItems = new();

            foreach (string assetPath in Settings.Instance.Assets)
            {
                try
                {
                    Asset tempAsset = new();
                    bool loaded = tempAsset.Load(assetPath, AssetLoadType.NoAnimations);

                    if (!loaded)
                    {
                        continue;
                    }

                    RecentItem item = new();
                    item.Title = tempAsset.Name;
                    item.AssetPath = assetPath;
                    item.Description = $"Anims: {tempAsset.Dictionary.OnimPaths.Count} / Clips: {tempAsset.Clips.Count}";
                    item.DateTimestamp = tempAsset.LastUpdated;

                    item.ItemSelected += RecentItem_ItemSelected;
                    item.ItemRemoved += RecentItem_ItemRemoved;

                    recentItems.Add(item);
                }
                catch { }
            }

            if (recentItems.Count > 0)
            {
                // Sort recent list by latest usage.
                recentItems.Sort((x, y) => DateTime.Compare(y.DateTimestamp, x.DateTimestamp));
                recentItems.ForEach(_ => RecentAssetsList.RecentStackPanel.Children.Add(_));

                RecentAssetsEmptyLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                RecentAssetsEmptyLabel.Visibility = Visibility.Visible;
            }
        }

        private void QuickActionHandleOnim(QuickActionType type, string path)
        {
            switch (type)
            {
                case QuickActionType.CreateAsset:
                    {
                        System.Windows.Forms.FolderBrowserDialog dialog = new()
                        {
                            Description = $"Select asset folder"
                        };

                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                        if (result != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        string assetName = $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                        string assetPath = dialog.SelectedPath;

                        // If folder have some files, also creating folder with asset name.
                        if (Directory.EnumerateFileSystemEntries(assetPath).Any())
                        {
                            assetPath = Path.Combine(assetPath, assetName);
                            _ = Directory.CreateDirectory(assetPath);
                        }

                        AssetFile assetFile = new()
                        {
                            AssetName = assetName,
                            DictionaryName = "dictionary",
                        };

                        // Creating new asset.
                        Asset asset = new();

                        if (!asset.Load(assetFile, assetPath))
                        {
                            _ = MessageBox.Show("Failed to create asset from onim: loading asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!asset.Dictionary.AddOnim(path))
                        {
                            _ = MessageBox.Show("Failed to create asset from onim: importing onim.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!asset.Save())
                        {
                            _ = MessageBox.Show("Failed to create asset from onim: saving asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (Settings.Instance != null)
                        {
                            Settings.Instance.Assets.Add(assetPath);
                            UpdateRecentAssetsList();
                        }

                        break;
                    }
                case QuickActionType.Convert:
                    {
                        var dialog = new System.Windows.Forms.FolderBrowserDialog();

                        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        Asset tempAsset = new()
                        {
                            ExportName = "export",
                        };

                        bool success = tempAsset.ExportSingle(path, dialog.SelectedPath);

                        if (!success)
                        {
                            _ = MessageBox.Show("Failed to export onim to raw.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        break;
                    }
            }
        }

        private void QuickActionHandleRaw(QuickActionType type, string path)
        {
            switch (type)
            {
                case QuickActionType.CreateAsset:
                    {
                        System.Windows.Forms.FolderBrowserDialog dialog = new()
                        {
                            Description = $"Select asset folder"
                        };

                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                        if (result != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        string assetName = $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                        string assetPath = dialog.SelectedPath;

                        // If folder have some files, also creating folder with asset name.
                        if (Directory.EnumerateFileSystemEntries(assetPath).Any())
                        {
                            assetPath = Path.Combine(assetPath, assetName);
                            _ = Directory.CreateDirectory(assetPath);
                        }

                        AssetFile assetFile = new()
                        {
                            AssetName = assetName,
                            DictionaryName = "dictionary",
                        };

                        // Creating new asset.
                        Asset asset = new();

                        if (!asset.Load(assetFile, assetPath))
                        {
                            _ = MessageBox.Show("Failed to create asset from raw: loading asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!asset.Save())
                        {
                            _ = MessageBox.Show("Failed to create asset from raw: saving asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        // Adding asset to recents and updating list.
                        if (Settings.Instance != null)
                        {
                            Settings.Instance.Assets.Add(assetPath);
                            UpdateRecentAssetsList();
                        }

                        // Scheduling raw import for this asset.
                        Variables.Instance.Set($"{asset.Name}:raw:import", path);

                        // Opening asset window to scheduled import will start.
                        OpenAssetWindow(assetPath);

                        break;
                    }
                case QuickActionType.Convert:
                    {
                        // TODO: raw -> open convertion.
                        _ = MessageBox.Show("Not implemented.",
                            Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                        break;
                    }
            }
        }

        private void QuickActionHandleOpen(QuickActionType type, string path)
        {
            switch (type)
            {
                case QuickActionType.CreateAsset:
                    {
                        System.Windows.Forms.FolderBrowserDialog dialog = new()
                        {
                            Description = $"Select asset folder"
                        };

                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                        if (result != System.Windows.Forms.DialogResult.OK)
                        {
                            return;
                        }

                        string assetName = $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                        string assetPath = dialog.SelectedPath;

                        // If folder have some files, also creating folder with asset name.
                        if (Directory.EnumerateFileSystemEntries(assetPath).Any())
                        {
                            assetPath = Path.Combine(assetPath, assetName);
                            _ = Directory.CreateDirectory(assetPath);
                        }

                        AssetFile assetFile = new()
                        {
                            AssetName = assetName,
                            DictionaryName = "dictionary",
                        };

                        // Creating new asset.
                        Asset asset = new();

                        if (!asset.Load(assetFile, assetPath))
                        {
                            _ = MessageBox.Show("Failed to create asset from raw: loading asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        if (!asset.Save())
                        {
                            _ = MessageBox.Show("Failed to create asset from raw: saving asset.",
                                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                            return;
                        }

                        // Adding asset to recents and updating list.
                        if (Settings.Instance != null)
                        {
                            Settings.Instance.Assets.Add(assetPath);
                            UpdateRecentAssetsList();
                        }

                        // Scheduling raw import for this asset.
                        Variables.Instance.Set($"{asset.Name}:open:import", path);

                        // Opening asset window to scheduled import will start.
                        OpenAssetWindow(assetPath);

                        break;
                    }
                case QuickActionType.Convert:
                    {
                        // TODO: open -> raw convertion.
                        _ = MessageBox.Show("Not implemented.",
                            Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                        break;
                    }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // If quick actions panel already visible - skip.
            if (QuickActionsPanel.Visibility == Visibility.Visible)
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length != 1)
            {
                // TODO: support multiple .onim files!
                return;
            }

            string filePath = files[0];

            QuickActionsData data = new("UNKNOWN FILE", filePath);

            if (filePath.EndsWith(".onim"))
            {
                data.ActionsTitle = "ANIMATION FILE";
                data.Left = new QuickActionEntry(QuickActionType.CreateAsset,
                    "CREATE ASSET", "Create asset and import animation from .onim file");
                data.Right = new QuickActionEntry(QuickActionType.Convert,
                    "CONVERT TO #CD", "Create a clip dictionary for in-game use from .onim file");
            }
            else if (filePath.EndsWith(".ycd"))
            {
                data.ActionsTitle = "CLIP DICTIONARY";
                data.Left = new QuickActionEntry(QuickActionType.CreateAsset,
                    "CREATE ASSET", "Create asset and import animations and clips from clip dictionary.");
                data.Right = new QuickActionEntry(QuickActionType.Convert,
                    "CONVERT TO OAD", "Convert clip dictionary to openFormats animation dictionary.");
            }
            else if (filePath.EndsWith(".oad"))
            {
                data.ActionsTitle = "ANIMATION DICTIONARY";
                data.Left = new QuickActionEntry(QuickActionType.CreateAsset,
                    "CREATE ASSET", "Create asset and import animations from animation dictionary.");
                data.Right = new QuickActionEntry(QuickActionType.Convert,
                    "CONVERT TO #CD", "Convert openFormats animation dictionary to clip dictionary.");
            }

            QuickActionsPanel.SetQuickActionsData(data);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            AssetWindow[] assetWindows = Utils.GetOpenedWindowsByType<AssetWindow>();

            if (assetWindows.Length == 0)
            {
                return;
            }

            if (Variables.Instance.Has("noexitwarn"))
            {
                return;
            }

            // Warn user if there's any opened asset windows.
            MessageBoxResult result = MessageBox.Show($"You have some assets in use ({assetWindows.Length}). " +
                "Are you sure you want to exit?", Constants.BrandingName,
                MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            Application.Current.Shutdown();
        }

        private void QuickActionsPanel_ActionDrop(object sender, QuickActionEventArgs e)
        {
            string filePath = e.Path;
            QuickActionType actionType = e.Type;

            if (actionType == QuickActionType.Invalid)
            {
                return;
            }

            if (filePath.EndsWith(".onim"))
            {
                QuickActionHandleOnim(actionType, filePath);
            }
            else if (filePath.EndsWith(".ycd"))
            {
                QuickActionHandleRaw(actionType, filePath);
            }
            else if (filePath.EndsWith(".oad"))
            {
                QuickActionHandleOpen(actionType, filePath);
            }
        }

        private void CreateButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CreateWindow? createWindow = Utils.GetOpenedWindowByType<CreateWindow>();

            if (createWindow != null)
            {
                SystemSounds.Beep.Play();
                _ = createWindow.Focus();
                return;
            }

            createWindow = new CreateWindow();
            createWindow.Closed += CreateWindow_Closed;
            createWindow.Show();
        }

        private void PluginButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PluginWindow? pluginWindow = Utils.GetOpenedWindowByType<PluginWindow>();

            if (pluginWindow != null)
            {
                SystemSounds.Beep.Play();
                _ = pluginWindow.Focus();
                return;
            }

            pluginWindow = new PluginWindow();
            pluginWindow.Show();
        }

        private void ContributeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.OpenRepoPage();
        }

        private void AboutButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AboutWindow aboutWindow = new();
            _ = aboutWindow.ShowDialog();
        }

        private void CreateWindow_Closed(object? sender, EventArgs e)
        {
            UpdateRecentAssetsList();
        }

        private void OpenButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new()
            {
                Description = $"Select asset folder (should include {Constants.AssetManifestName} file)"
            };

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            OpenAssetWindow(dialog.SelectedPath);
        }

        private void AssetWindow_Closed(object? sender, EventArgs e)
        {
            UpdateRecentAssetsList();
        }

        private void AssetWindow_AssetUpdated(object? sender, EventArgs e)
        {
            UpdateRecentAssetsList();
        }

        private void RecentItem_ItemSelected(object sender, MouseEventArgs e)
        {
            RecentItem item = (RecentItem)sender;
            OpenAssetWindow(item.AssetPath);
        }

        private void RecentItem_ItemRemoved(object sender, MouseEventArgs e)
        {
            if (Settings.Instance == null)
            {
                return;
            }

            RecentItem item = (RecentItem)sender;

            int index = Settings.Instance.Assets.FindIndex(_ => _ == item.AssetPath);

            if (index != -1)
            {
                Settings.Instance.Assets.RemoveAt(index);
                _ = Settings.Instance.Save();

                UpdateRecentAssetsList();
            }
        }

        private void RecentAssetsList_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateRecentAssetsList();
        }
    }
}
