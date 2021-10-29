using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for CreateWindow.xaml
    /// </summary>
    public partial class CreateWindow : Window
    {
        public CreateWindow()
        {
            InitializeComponent();
        }

        private void AssetPathButton_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            AssetPathInput.Value = dialog.SelectedPath;
        }

        private void CreateButton_Click(object sender, MouseButtonEventArgs e)
        {
            string nameInput = AssetNameInput.Value.Trim();

            if (!Utils.IsNameValid(nameInput))
            {
                _ = MessageBox.Show("Asset name is invalid!\n" +
                    "Should not be empty, should have less than 64 symbols, should not contain forbidden symbols",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            string pathInput = AssetPathInput.Value.Trim();

            if (pathInput.Length <= 0)
            {
                _ = MessageBox.Show("Asset path is not specified!", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool validPath = pathInput.IndexOfAny(Path.GetInvalidPathChars()) == -1;

            if (!validPath)
            {
                _ = MessageBox.Show("Asset path is invalid!", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!Directory.Exists(pathInput))
                {
                    _ = Directory.CreateDirectory(pathInput);
                }
                else
                {
                    // If folder have some files, also creating folder with asset name.
                    if (Directory.EnumerateFileSystemEntries(pathInput).Any())
                    {
                        pathInput = Path.Combine(pathInput, nameInput);
                        _ = Directory.CreateDirectory(pathInput);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Failed to create directory: {ex.Message}", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AssetFile assetFile = new()
            {
                AssetName = nameInput,
                DictionaryName = nameInput,
            };

            // Creating new asset.
            Asset asset = new();

            bool isLoaded = asset.Load(assetFile, pathInput);

            if (!isLoaded)
            {
                _ = MessageBox.Show($"Failed to load asset data!", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isSaved = asset.Save();

            if (!isSaved)
            {
                _ = MessageBox.Show($"Failed to save asset!", Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Adding this to known assets list.
            if (Settings.Instance != null)
            {
                Settings.Instance.Assets.Add(pathInput);
                _ = Settings.Instance.Save();
            }

            // Close window as we don't need it anymore.
            Close();
        }
    }
}
