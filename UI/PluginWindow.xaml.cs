using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for PluginWindow.xaml
    /// </summary>
    public partial class PluginWindow : Window
    {
        /// <summary>
        /// Cancellation token for threads.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; } = new();

        public PluginWindow()
        {
            InitializeComponent();
        }

        private void ReloadWindowContent()
        {
            MaxPathInput.Value = Plugin.MaxPath;
            LoadingOverlay.Visibility = Visibility.Hidden;

            if (Plugin.MaxPath == string.Empty)
            {
                InstallButton.Blocked = true;
                InstallButton.Visibility = Visibility.Visible;
                UpdateButton.Blocked = true;
                UpdateButton.Visibility = Visibility.Hidden;
                RemoveButton.Blocked = true;

                PluginStatusText.Content = "UNKNOWN";
                PluginStatusCircle.Fill = new SolidColorBrush(Colors.DarkGray);

                return;
            }

            if (!Plugin.CheckFiles())
            {
                InstallButton.Blocked = false;
                InstallButton.Visibility = Visibility.Visible;
                UpdateButton.Blocked = true;
                UpdateButton.Visibility = Visibility.Hidden;
                RemoveButton.Blocked = true;

                PluginStatusText.Content = "NOT INSTALLED";
                PluginStatusCircle.Fill = new SolidColorBrush(Colors.Red);

                return;
            }

            RemoveButton.Blocked = false;
            InstallButton.Blocked = true;
            InstallButton.Visibility = Visibility.Hidden;

            bool isOutdated = Plugin.CurrentVersion != null && Plugin.ActualVersion > Plugin.CurrentVersion;

            UpdateButton.Blocked = !isOutdated;
            UpdateButton.Visibility = Visibility.Visible;
            PluginStatusText.Content = (isOutdated ? "OUTDATED" : "INSTALLED") + $" ({Plugin.CurrentVersion})";

            PluginStatusCircle.Fill = new SolidColorBrush(isOutdated ? Colors.Orange : Colors.Green);
        }


        private bool UninstallPlugin()
        {
            bool success = Plugin.UninstallPlugin();

            if (!success)
            {
                _ = MessageBox.Show("An error occurred while uninstalling plugin.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            return true;
        }

        private void ReinstallPlugin()
        {
            if (Plugin.MaxPath == string.Empty)
            {
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.SetProgress("Initializing", "Checking files...");

            // If files exist - we're updating.
            bool isUpdating = Plugin.CheckFiles();

            // If we're updating - uninstall plugin first.
            if (isUpdating && !UninstallPlugin())
            {
                return;
            }

            string titleText = (isUpdating ? "Updating" : "Installing") + " plugin";
            LoadingOverlay.SetProgress(titleText, "Starting download...");

            // Loading progress callback.
            Action<long, long> loadProgress = new((long current, long overall) =>
            {
                if (overall <= 0 || CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    double downloaded = current / 1024.0;
                    double fileSize = overall / 1024.0;
                    double percent = downloaded / fileSize * 100;
                    string status = $"Dowloading {downloaded:0.0}KB of {fileSize:0.0}KB ({percent:0.00}%)";

                    LoadingOverlay.SetProgress(titleText, status);
                });
            });

            // Creating new task for plugin installing.
            _ = Task.Run(async () =>
            {
                bool isLoaded = await Plugin.DownloadPlugin(loadProgress);

                if (!isLoaded)
                {
                    _ = MessageBox.Show("Failed to download plugin!",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                    Close();
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    LoadingOverlay.SetProgress(titleText, "Unpacking files...");
                });

                bool isUnpacked = Plugin.UnpackDownloaded();

                if (!isUnpacked)
                {
                    _ = MessageBox.Show("Failed to unpack downloaded plugin!",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                    Close();
                    return;
                }

                if (Plugin.CheckFiles())
                {
                    _ = Plugin.LoadVersion();
                }

                Dispatcher.Invoke(() =>
                {
                    ReloadWindowContent();

                    _ = MessageBox.Show("Plugin was successfully " + (isUpdating ? "updated" : "installed") + "!",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }, CancellationToken.Token);
        }

        private void MaxPathBrowseButton_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "3ds Max root installaion folder";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (!File.Exists(dialog.SelectedPath + "\\3dsmax.exe") || !Directory.Exists(dialog.SelectedPath + "\\scripts"))
            {
                _ = MessageBox.Show("This folder has no 3ds Max executable.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Plugin.MaxPath = dialog.SelectedPath;

            // If have installed plugin inside - reload version.
            if (Plugin.CheckFiles())
            {
                _ = Plugin.LoadVersion();
            }

            ReloadWindowContent();
        }

        private void RemoveButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (UninstallPlugin())
            {
                _ = MessageBox.Show("Plugin was sucessfully uninstalled.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);

                ReloadWindowContent();
            }
        }

        private void InstallButton_Click(object sender, MouseButtonEventArgs e)
        {
            ReinstallPlugin();
        }

        private void UpdateButton_Click(object sender, MouseButtonEventArgs e)
        {
            ReinstallPlugin();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            ReloadWindowContent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                CancellationToken.Cancel();
            }
        }
    }
}
