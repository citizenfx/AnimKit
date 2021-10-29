using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Asset menu page enum.
    /// </summary>
    public enum AssetMenuPage
    {
        /// <summary>
        /// Help page, active by default.
        /// </summary>
        Help = -1,

        /// <summary>
        /// Dictionary page.
        /// </summary>
        Dictionary,

        /// <summary>
        /// Animations page.
        /// </summary>
        Animations,

        /// <summary>
        /// Clips page.
        /// </summary>
        Clips,
    }

    /// <summary>
    /// Interaction logic for AssetWindow.xaml
    /// </summary>
    public partial class AssetWindow : Window
    {
        /// <summary>
        /// Asset instance in use.
        /// </summary>
        public Asset CurrentAsset { get; }

        /// <summary>
        /// Asset updated event handler.
        /// </summary>
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when asset getting updated")]
        public event EventHandler? AssetUpdated;

        /// <summary>
        /// Current page index.
        /// </summary>
        private AssetMenuPage CurrentPageItem { get; set; } = AssetMenuPage.Help;

        /// <summary>
        /// List of initialized pages.
        /// </summary>
        private List<KeyValuePair<AssetMenuPage, Page>> Pages { get; } = new();

        /// <summary>
        /// Cancellation token for threads.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; } = new();

        /// <summary>
        /// File system watcher for asset.
        /// </summary>
        private FileSystemWatcher Watcher { get; } = new();

        /// <summary>
        /// List of files that currently being processed.
        /// </summary>
        private List<string> WatcherLocks = new();

        /// <summary>
        /// Creates asset window
        /// </summary>
        /// <param name="assetPath"></param>
        public AssetWindow(string assetPath)
        {
            InitializeComponent();

            // Show loading overlay.
            ShowLoadingOverlay("Loading manifest", assetPath);

            // Creating asset intance.
            CurrentAsset = new Asset();

            // Loading progress callback.
            Action<int, int> loadProgress = new((int current, int overall) =>
            {
                if (overall <= 0 || CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    string displayPercent = (current / (float)overall * 100).ToString("0.00", CultureInfo.InvariantCulture);
                    string title = string.IsNullOrEmpty(CurrentAsset.Name) ? "Loading manifest" : CurrentAsset.Name;
                    string status = $"Loaded {current} of {overall} ({displayPercent}%) animations...";

                    ShowLoadingOverlay(title, status);
                });
            });

            // Creating new task for asset loading.
            _ = Task.Run(() =>
            {
                bool isLoaded = CurrentAsset.Load(assetPath, AssetLoadType.Full, loadProgress);

                if (!isLoaded)
                {
                    _ = MessageBox.Show("Failed to load asset", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    HideLoadingOverlay();
                    InitializeWindowPages();
                    InitializeFileWatcher();
                });
            }, CancellationToken.Token);
        }

        /// <summary>
        /// Shows loading overlay with specified title and status.
        /// </summary>
        /// <param name="title">Loading title</param>
        /// <param name="status">Loading status</param>
        public void ShowLoadingOverlay(string title, string status)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.SetProgress(title, status);
        }

        /// <summary>
        /// Hides loading overlay.
        /// </summary>
        public void HideLoadingOverlay()
        {
            LoadingOverlay.Visibility = Visibility.Hidden;
            LoadingOverlay.SetProgress(string.Empty, string.Empty);
        }

        /// <summary>
        /// Select menu page.
        /// </summary>
        /// <param name="page">Page index</param>
        public void SelectMenuPage(AssetMenuPage page)
        {
            // Is user selected the same page as currently in use - show help page.
            AssetMenuPage targetPage = (page == CurrentPageItem) ? AssetMenuPage.Help : page;

            // Find page in initialized pages list.
            var pageEntry = Pages.Find(_ => _.Key == targetPage);

            // Don't show selector when HELP page is active.
            if (targetPage == AssetMenuPage.Help)
            {
                AssetMenuSelector.Opacity = 0.0;
            }
            else
            {
                // Setting grid column index for selector.
                AssetMenuSelector.SetValue(Grid.ColumnProperty, (int)targetPage);
                AssetMenuSelector.Opacity = 1.0;
            }

            // Navigating frame to selected page.
            _ = AssetMenuFrame.Navigate(pageEntry.Value);

            // Saving selected page as current.
            Variables.Instance.Set($"{CurrentAsset.Name}:window:page", targetPage);

            CurrentPageItem = targetPage;
        }

        /// <summary>
        /// Initialize asset window menu pages.
        /// </summary>
        private void InitializeWindowPages()
        {
            Pages.Add(new KeyValuePair<AssetMenuPage, Page>(
                AssetMenuPage.Help, new AssetHelpPage(this)));
            Pages.Add(new KeyValuePair<AssetMenuPage, Page>(
                AssetMenuPage.Dictionary, new AssetDictionaryPage(this)));
            Pages.Add(new KeyValuePair<AssetMenuPage, Page>(
                AssetMenuPage.Animations, new AssetAnimationsPage(this)));
            Pages.Add(new KeyValuePair<AssetMenuPage, Page>(
                AssetMenuPage.Clips, new AssetClipsPage(this)));

            Title = $"{Constants.BrandingName} / {CurrentAsset.Name}";
            AssetEditableTitle.Title = CurrentAsset.Name;

            HandleScheduledActions();

            AssetMenuPage lastPage = Variables.Instance.Get($"{CurrentAsset.Name}:window:page", AssetMenuPage.Help);
            SelectMenuPage(lastPage);
        }

        /// <summary>
        /// Initialize asset file watcher.
        /// </summary>
        private void InitializeFileWatcher()
        {
            string dictionaryPath = Path.Combine(CurrentAsset.Dictionary.FileDirectory, CurrentAsset.Dictionary.Name);

            if (!Directory.Exists(dictionaryPath))
            {
                _ = Directory.CreateDirectory(dictionaryPath);
            }

            Watcher.Path = dictionaryPath;
            Watcher.NotifyFilter = NotifyFilters.LastWrite;
            Watcher.Filter = "*.onim";
            Watcher.IncludeSubdirectories = false;
            Watcher.EnableRaisingEvents = true;
            Watcher.InternalBufferSize = 1024 * 48; // 48KB of buffer
            Watcher.Changed += FileWatcherChanged;
        }

        private void FileWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (WatcherLocks.Contains(e.FullPath))
            {
                return;
            }

            WatcherLocks.Add(e.FullPath);
            Watcher.EnableRaisingEvents = false;

            bool waitForFile = true;
            string onimName = Path.GetFileNameWithoutExtension(e.FullPath);

            // Wait until we will be able to read file.
            while (waitForFile)
            {
                try
                {
                    if (!File.Exists(e.FullPath))
                    {
                        waitForFile = false;
                        break;
                    }

                    FileStream inputStream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                    inputStream.Close();
                    waitForFile = false;
                }
                catch { }
            }

            try
            {
                OnimFile? onimFile = CurrentAsset.Dictionary.OnimFiles.Find(_ => _.Name == onimName);

                if (onimFile != null)
                {
                    CurrentAsset.Dictionary.ReloadOnim(onimFile);

                    Dispatcher.Invoke(() =>
                    {
                        if (CurrentPageItem == AssetMenuPage.Animations)
                        {
                            SelectMenuPage(AssetMenuPage.Animations);
                        }
                    });
                }
            }
            finally
            {
                _ = WatcherLocks.Remove(e.FullPath);
                Watcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Import asset from raw clip dictionary (#CD) file.
        /// </summary>
        /// <param name="filePath">Path to clip dictionary.</param>
        public void AssetImportFromRaw(string filePath)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.Title = "Importing raw";
            LoadingOverlay.Status = filePath;

            var loadProgress = new Action<AssetImportStage, int, int>((AssetImportStage stage, int current, int overall) =>
            {
                if (CancellationToken.IsCancellationRequested || overall <= 0)
                {
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    LoadingOverlay.Title = stage == AssetImportStage.Animations ?
                    "Importing animations" : stage == AssetImportStage.Clips ?
                    "Importing clips" : "Loading meta";

                    if (stage == AssetImportStage.Meta)
                    {
                        return;
                    }

                    string displayPercent = (current / (float)overall * 100).ToString("0.00", CultureInfo.InvariantCulture);
                    LoadingOverlay.Status = $"Imported {current} of {overall} ({displayPercent}%) items...";
                });
            });

            _ = Task.Run(() =>
            {
                byte[] ycdRaw = File.ReadAllBytes(filePath);
                bool success = CurrentAsset.ImportRaw(ycdRaw, loadProgress);

                Dispatcher.Invoke(() =>
                {
                    if (!success)
                    {
                        _ = MessageBox.Show("Failed to import raw clip dictionary!",
                            Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    LoadingOverlay.Visibility = Visibility.Hidden;
                    AssetUpdated?.Invoke(this, new());
                });
            }, CancellationToken.Token);
        }

        /// <summary>
        /// Import asset from openFormats animation dictionary (OCD) file.
        /// </summary>
        /// <param name="filePath">Path to animation dictionary.</param>
        public void AssetImportFromOpen(string filePath)
        {
            ShowLoadingOverlay("Importing openFormats", filePath);

            _ = Task.Run(() =>
            {
                bool success = CurrentAsset.ImportOpen(filePath);

                if (!success)
                {
                    _ = MessageBox.Show("Failed to import openFormats animation dictionary!",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _ = CurrentAsset.Save();

                Dispatcher.Invoke(() =>
                {
                    HideLoadingOverlay();
                    AssetUpdated?.Invoke(this, new());
                });
            });
        }

        /// <summary>
        /// Export asset to raw clip dictionary (#CD) file.
        /// </summary>
        /// <param name="filePath">Path to export folder</param>
        public void AssetExportAsRaw(string filePath)
        {
            string ycdName = CurrentAsset.ExportName + ".ycd";
            string outFile = Path.Combine(filePath, ycdName);

            if (File.Exists(outFile))
            {
                MessageBoxResult questionResult = MessageBox.Show(
                    $"File with name {ycdName} is already exist in this directory. Do you want to replace it?",
                    Constants.BrandingName, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (questionResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.Title = "Exporting asset";
            LoadingOverlay.Status = CurrentAsset.ExportName;

            _ = Task.Run(() =>
            {
                bool success = CurrentAsset.ExportRaw(outFile);

                if (!success)
                {
                    _ = MessageBox.Show("An error occurred while exporting clip dictionary!",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Dispatcher.Invoke(() =>
                {
                    LoadingOverlay.Visibility = Visibility.Hidden;
                });
            });
        }

        /// <summary>
        /// Export asset to openFormats animation dictionary (OCD) file.
        /// </summary>
        /// <param name="filePath">Path to export folder</param>
        public void AssetExportAsOpen(string filePath)
        {
            bool success = CurrentAsset.ExportOpen(filePath);

            if (!success)
            {
                _ = MessageBox.Show("An error occurred while exporting animation dictionary!",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleScheduledActions()
        {
            if (Variables.Instance.Has($"{CurrentAsset.Name}:raw:import"))
            {
                string filePath = Variables.Instance.Get($"{CurrentAsset.Name}:raw:import", string.Empty);

                if (!string.IsNullOrEmpty(filePath))
                {
                    AssetImportFromRaw(filePath);
                }

                Variables.Instance.Clear($"{CurrentAsset.Name}:raw:import");
            }

            if (Variables.Instance.Has($"{CurrentAsset.Name}:open:import"))
            {
                string filePath = Variables.Instance.Get($"{CurrentAsset.Name}:open:import", string.Empty);

                if (!string.IsNullOrEmpty(filePath))
                {
                    AssetImportFromOpen(filePath);
                }

                Variables.Instance.Clear($"{CurrentAsset.Name}:open:import");
            }
        }

        private void AssetEditableTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            InputWindow inputWindow = new("Rename asset", CurrentAsset.Name,
                "* Should be a valid file name. Not tied to game data.");

            inputWindow.InputApplied += delegate (object sender, InputWindowEventArgs args)
            {
                string newName = args.Value;

                if (!Utils.IsNameValid(newName))
                {
                    _ = MessageBox.Show("Asset name is invalid!\n" + 
                        "Should not be empty, should have less than 64 symbols, should not contain forbidden symbols",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                AssetEditableTitle.EditTitle.Content = newName;

                CurrentAsset.Name = newName;
                _ = CurrentAsset.Save();

                inputWindow.Close();
            };

            _ = inputWindow.ShowDialog();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                CancellationToken.Cancel();
            }

            Watcher.Dispose();
        }

        private void BackdropRect_Loaded(object sender, RoutedEventArgs e)
        {
            // Make random viewport offset.
            double offsetX = Utils.Randomizer.NextDouble() * 100f;
            double offsetY = Utils.Randomizer.NextDouble() * 100f;

            BackdropImage.Viewport = new Rect(offsetX, offsetY, 300f, 300f);
        }

        private void AssetMenuDictionaryRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectMenuPage(AssetMenuPage.Dictionary);
        }

        private void AssetMenuAnimationsRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectMenuPage(AssetMenuPage.Animations);
        }

        private void AssetMenuClipsRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectMenuPage(AssetMenuPage.Clips);
        }

        private void SaveAssetButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (CurrentAsset == null)
            {
                return;
            }

            bool success = CurrentAsset.Save();

            _ = success
                ? MessageBox.Show("Asset was successfully saved!\n",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information)
                : MessageBox.Show("Asset saving failed!\n",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ExportAssetButton_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (CurrentAsset.LastExportPath != string.Empty)
            {
                if (Directory.Exists(CurrentAsset.LastExportPath))
                {
                    dialog.InitialDirectory = CurrentAsset.LastExportPath;
                }
                else
                {
                    CurrentAsset.LastExportPath = string.Empty;
                }
            }

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            CurrentAsset.LastExportPath = dialog.SelectedPath;

            _ = CurrentAsset.Save();

            AssetExportAsRaw(dialog.SelectedPath);
        }
    }
}
