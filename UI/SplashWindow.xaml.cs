using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            SetStatusText("Statring...");

            // Making current window draggable.
            MouseDown += delegate { DragMove(); };

            // Start launching asynchronously.
            _ = Task.Run(async () =>
            {
                if (!await ProcessLaunching())
                {
                    ShutdownApp();
                }
            });
        }

        /// <summary>
        /// Doing all the required checks and action in order to initialize.
        /// </summary>
        private async Task<bool> ProcessLaunching()
        {
            SetStatusText("Fetching versions table...");

            // Fetch and update actual versions.
            if (!await Utils.UpdateActualVersion())
            {
                MessageBoxResult openMessage = MessageBox.Show($"Failed to fetch versions table.\n" +
                    "Consider checking manually if there are any updates.\n" +
                    "Do you want to open the download page?",
                    Constants.BrandingName, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (openMessage == MessageBoxResult.Yes)
                {
                    Utils.OpenRepoPage();
                    return false;
                }
            }

            SetStatusText("Checking for updates...");

            if (!await CheckVersion())
            {
                return false;
            }

            SetStatusText("Loading settings...");

            if (!await LoadSettings())
            {
                return false;
            }

            SetStatusText("Checking recent assets...");

            if (!await VerifyAssets())
            {
                return false;
            }

            SetStatusText("Checking plugin...");

            if (!await CheckPlugin())
            {
                return false;
            }

            SetStatusText("We're getting there.");

            // For debug purposes.
            if (Variables.Instance.Has("bannersleep"))
            {
                Thread.Sleep(2000);
            }

            // Finally we can show menu window!
            OpenMenuWindow();

            return true;
        }

        /// <summary>
        /// Checks the software version.
        /// </summary>
        /// <returns>Whether or nor version is updated and the software can be used</returns>
        private async Task<bool> CheckVersion()
        {
            // Delete "new" file.
            if (File.Exists("AnimKit.new.exe"))
            {
                File.Delete("AnimKit.new.exe");
            }

            UpdateAction action = Updater.CheckVersion();

            if (action == UpdateAction.Continue)
            {
                return true;
            }
            else if (action == UpdateAction.Shutdown)
            {
                return false;
            }
            else if (action == UpdateAction.Manual)
            {
                Utils.OpenRepoPage();
                return false;
            }

            // Loading progress callback.
            Action<long, long> loadProgress = new((long current, long overall) =>
            {
                double downloaded = current / 1024.0 / 1024.0;
                double fileSize = overall / 1024.0 / 1024.0;
                double percent = downloaded / fileSize * 100;

                SetStatusText($"Dowloading {downloaded:0.0}MB of {fileSize:0.0}MB ({percent:0.00}%)");
            });

            // Making user know that we're going to update.
            SetStatusText("Fetching update info...");

            // Downloading update!
            {
                bool success = await Updater.DownloadLatest(loadProgress);

                if (!success)
                {
                    _ = MessageBox.Show("Failed to download update!", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // Unpacking downloaded file.
            {
                bool success = Updater.UnpackLatest();

                if (!success)
                {
                    _ = MessageBox.Show("Failed to unpack update! Try to run AnimKit with administrator privileges.", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // Launching updater instance.
            _ = Process.Start("AnimKit.new.exe");

            // And shutting down the current.
            return false;
        }

        /// <summary>
        /// Loading settings.
        /// </summary>
        /// <returns>Whether or not settings was loaded</returns>
        private static async Task<bool> LoadSettings()
        {
            string roamingFolder = Path.Join(Utils.RoamingPath, Constants.RoamingFolderName);

            if (!Directory.Exists(roamingFolder))
            {
                _ = Directory.CreateDirectory(roamingFolder);
            }

            return await Settings.Load();
        }

        /// <summary>
        /// Validates plugin status.
        /// </summary>
        /// <returns>Whether or not plugin might be installed/used</returns>
        private static async Task<bool> VerifyAssets()
        {
            if (Variables.Instance.Has("noassetsverify"))
            {
                return true;
            }

            return await Task.Run(() =>
            {
                if (Settings.Instance == null)
                {
                    return false;
                }

                List<string> missedAssets = new();

                foreach (string assetPath in Settings.Instance.Assets)
                {
                    if (!Directory.Exists(assetPath) || !File.Exists(Path.Combine(assetPath, Constants.AssetManifestName)))
                    {
                        missedAssets.Add(assetPath);
                    }
                }

                if (missedAssets.Count > 0)
                {
                    missedAssets.ForEach(_ => Settings.Instance.Assets.Remove(_));

                    string messageText = $"We wasn't able to find some of your saved assets ({missedAssets.Count}). " +
                        "Here's list:\n" + string.Join("\n", missedAssets);

                    // Actually save
                    _ = Settings.Instance.Save();

                    MessageBox.Show(messageText, Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                return true;
            });
        }

        /// <summary>
        /// Validates plugin status.
        /// </summary>
        /// <returns>Whether or not plugin might be installed and used</returns>
        private static async Task<bool> CheckPlugin()
        {
            if (Settings.Instance == null)
            {
                return false;
            }

            // Update UI executable path for the plugin.
            string exePath = $"{AppDomain.CurrentDomain.BaseDirectory}{AppDomain.CurrentDomain.FriendlyName}.exe";
            bool pathWritten = await Utils.WriteRoamingFileContentAsync(".ANIMKIT_UI_PATH", exePath);

            if (!pathWritten)
            {
                return false;
            }

            if (Settings.Instance.MaxPath == "")
            {
                return true;
            }

            string startupFolder = Path.Combine(Settings.Instance.MaxPath, "scripts", "Startup");

            if (!Directory.Exists(startupFolder))
            {
                _ = MessageBox.Show("Your 3ds Max installation path is invalid. You can update it in Manage Plugin window.\n" +
                    $"Saved path: {Settings.Instance.MaxPath}", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                return true;
            }

            if (!Plugin.CheckFiles())
            {
                return true;
            }

            if (!Plugin.LoadVersion())
            {
                _ = MessageBox.Show("Failed to load plugin version. Please re-install plugin using Manage Plugin window.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                return true;
            }

            UpdateAction action = Plugin.CheckVersion();

            if (action == UpdateAction.Continue)
            {
                return true;
            }
            else if (action == UpdateAction.Shutdown)
            {
                return false;
            }
            else if (action == UpdateAction.Manual)
            {
                Utils.OpenRepoPage();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set splash screen status text.
        /// </summary>
        /// <param name="status">Status text</param>
        private void SetStatusText(string status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetStatusText(status));
                return;
            }

            ProgressStatus.Content = status;
        }

        /// <summary>
        /// Show menu window and close menu window.
        /// </summary>
        private void OpenMenuWindow()
        {
            Dispatcher.Invoke(() =>
            {
                new MenuWindow().Show();
                Close();
            });
        }

        /// <summary>
        /// Shutdown the application.
        /// </summary>
        private void ShutdownApp()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShutdownApp());
                return;
            }

            Application.Current.Shutdown();
        }
    }
}
