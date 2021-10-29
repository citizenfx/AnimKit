using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.ComponentModel;
using AnimKit.Core;
using System.Diagnostics;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AssetDictionaryPage.xaml
    /// </summary>
    public partial class AssetDictionaryPage : Page
    {
        /// <summary>
        /// Current asset getter.
        /// </summary>
        public Asset CurrentAsset => ParentWindow.CurrentAsset;

        /// <summary>
        /// Parent asset window.
        /// </summary>
        private AssetWindow ParentWindow { get; }

        /// <summary>
        /// Cancellation token.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; } = new();

        public AssetDictionaryPage(AssetWindow window)
        {
            ParentWindow = window;
            InitializeComponent();
        }

        private void AssetImportFromRawButton_Click(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.DefaultExt = ".ycd";
            dialog.Multiselect = false;
            dialog.Filter = "Win64 clip dictionary (*.ycd)|*.ycd";

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            ParentWindow.AssetImportFromRaw(dialog.FileName);
        }

        private void AssetImportFromOpenButton_Click(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.DefaultExt = ".oad";
            dialog.Multiselect = false;
            dialog.Filter = "openFormats animation dictionary (*.oad)|*.oad";

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            ParentWindow.AssetImportFromOpen(dialog.FileName);
        }

        private void AssetExportToRawButton_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            ParentWindow.AssetExportAsRaw(dialog.SelectedPath);
        }

        private void AssetExportToOpenButton_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            ParentWindow.AssetExportAsRaw(dialog.SelectedPath);
        }

        private void DictionaryNameInput_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            bool nameChanged = CurrentAsset.ExportName != DictionaryNameInput.Value;

            if (DictionaryNameHelp != null && DictionaryNameSave != null)
            {
                DictionaryNameHelp.Visibility = nameChanged ? Visibility.Hidden : Visibility.Visible;
                DictionaryNameSave.Visibility = nameChanged ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void DictionaryNameSave_Click(object sender, MouseButtonEventArgs e)
        {
            bool nameChanged = CurrentAsset.ExportName != DictionaryNameInput.Value;

            if (!nameChanged)
            {
                return;
            }

            string newName = DictionaryNameInput.Value.Trim().ToLowerInvariant();

            if (!Utils.IsNameValid(newName, 64, false))
            {
                _ = MessageBox.Show("Dictionary name is invalid!\n" +
                    "Must not be empty, contain forbidden symbols, whitespaces, and have less than 64 chars.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            CurrentAsset.ExportName = newName;
            CurrentAsset.Save();

            DictionaryNameInput.Value = newName;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DictionaryNameInput.Value = CurrentAsset.ExportName;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                CancellationToken.Cancel();
            }
        }

        private void AssetOpenLocation_Click(object sender, MouseButtonEventArgs e)
        {
            string assetFolder = CurrentAsset.AssetPath;

            _ = Process.Start(new ProcessStartInfo()
            {
                FileName = assetFolder + "\\",
                UseShellExecute = true
            });
        }

        private void AssetExportNames_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            try
            {
                string outPath = Path.Combine(dialog.SelectedPath, $"{CurrentAsset.ExportName}.nametable");
                CurrentAsset.Dictionary.ExportNames(outPath);
            }
            catch
            {
                _ = MessageBox.Show(
                    "Failed to export names. " +
                    "Filename might be already taken, or AnimKit lacks access to the folder?",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}