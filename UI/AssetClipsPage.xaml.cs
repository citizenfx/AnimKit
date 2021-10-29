using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AssetClipsPage.xaml
    /// </summary>
    public partial class AssetClipsPage : Page
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
        /// Selected clip.
        /// </summary>
        private AssetClip? SelectedClip { get; set; }

        /// <summary>
        /// Selected clip animation.
        /// </summary>
        private AssetClipAnimation? SelectedAnimation { get; set; }

        public AssetClipsPage(AssetWindow window)
        {
            ParentWindow = window;
            InitializeComponent();
        }

        /// <summary>
        /// Reloads clips list.
        /// </summary>
        private void ReloadClipsList()
        {
            AssetClipsList.Items.Clear();
            ClipListClearButton.Blocked = CurrentAsset.Clips.Count == 0;

            string lastSelection = Variables.Instance.Get($"{CurrentAsset.Name}:clips:selection", string.Empty);

            foreach (var clip in CurrentAsset.Clips)
            {
                int index = AssetClipsList.Items.Add(clip.Name);

                if (lastSelection == clip.Name)
                {
                    AssetClipsList.SelectedIndex = index;
                }
            }

            bool clipSelected = AssetClipsList.SelectedIndex != -1;
            AssetClipSelected.Visibility = clipSelected ? Visibility.Visible : Visibility.Hidden;
            AssetClipNotSelected.Visibility = clipSelected ? Visibility.Hidden : Visibility.Visible;

            AssetClipsTitle.Content = $"CLIPS ({AssetClipsList.Items.Count})";

            ReloadClipAnimationsList();
        }

        /// <summary>
        /// Performs list clip selection.
        /// </summary>
        /// <param name="clip">Selected clip</param>
        private void SelectAssetClip(AssetClip clip)
        {
            SelectedClip = clip;

            Variables.Instance.Set($"{CurrentAsset.Name}:clips:selection", SelectedClip.Name);

            ReloadClipAnimationsList();

            // Select first animation if list is not empty.
            if (AssetClipAnimationsList.Items.Count > 0)
            {
                AssetClipAnimationsList.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Performs list clip animation selection.
        /// </summary>
        /// <param name="clip">Selected clip</param>
        private void SelectAssetClipAnimation(AssetClipAnimation clipAnim)
        {
            if (SelectedClip == null)
            {
                return;
            }

            SelectedAnimation = clipAnim;

            ClipAnimationStartFromValue.Value = clipAnim.StartTime.ToString("0.000", CultureInfo.InvariantCulture);
            ClipAnimationEndsAtValue.Value = clipAnim.EndTime.ToString("0.000", CultureInfo.InvariantCulture);
            ClipAnimationRateValue.Value = clipAnim.Rate.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reloads clip animations list.
        /// </summary>
        private void ReloadClipAnimationsList()
        {
            if (SelectedClip == null)
            {
                AssetClipAnimationsTitle.Content = "USED ANIMATIONS";
                return;
            }

            AssetClipAnimationsList.Items.Clear();

            foreach (AssetClipAnimation clipAnim in SelectedClip.Animations)
            {
                OnimFile? clipOnim = CurrentAsset.Dictionary.OnimFiles.Find(_ => _.Name == clipAnim.Name);

                if (clipOnim == null)
                {
                    continue;
                }

                AssetClipAnimationsList.Items.Add(clipOnim.Name);
            }

            bool isSelected = AssetClipAnimationsList.SelectedIndex != -1;
            AssetClipData.Visibility = isSelected ? Visibility.Visible : Visibility.Hidden;
            AssetClipDataNotSelected.Visibility = isSelected ? Visibility.Hidden : Visibility.Visible;

            bool hasAnimations = AssetClipAnimationsList.Items.Count > 0;
            AssetClipAnimationsRemoveButton.Blocked = !hasAnimations;
            AssetClipAnimationsJumpButton.Blocked = !hasAnimations;

            AssetClipAnimationsTitle.Content = $"USED ANIMATIONS ({AssetClipAnimationsList.Items.Count})";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadClipsList();
        }

        private void AssetClipsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = AssetClipsList.SelectedIndex;
            bool clipSelected = selectedIndex != -1;

            AssetClipSelected.Visibility = clipSelected ? Visibility.Visible : Visibility.Hidden;
            AssetClipNotSelected.Visibility = clipSelected ? Visibility.Hidden : Visibility.Visible;

            ClipListRemoveButton.Blocked = !clipSelected;
            ClipListEditButton.Blocked = !clipSelected;

            if (!clipSelected)
            {
                SelectedClip = null;
                return;
            }

            AssetClip? clip = CurrentAsset.Clips[selectedIndex];

            if (clip != null)
            {
                SelectAssetClip(clip);
            }
        }

        private void AssetClipAnimationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedClip == null)
            {
                return;
            }

            int selectedIndex = AssetClipAnimationsList.SelectedIndex;
            bool isSelected = selectedIndex != -1;

            AssetClipData.Visibility = isSelected ? Visibility.Visible : Visibility.Hidden;
            AssetClipDataNotSelected.Visibility = isSelected ? Visibility.Hidden : Visibility.Visible;

            if (!isSelected)
            {
                SelectedAnimation = null;
                return;
            }

            AssetClipAnimation? clipAnim = SelectedClip.Animations[selectedIndex];

            if (clipAnim != null)
            {
                SelectAssetClipAnimation(clipAnim);
            }
        }

        private void ClipAnimationStartFromValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SelectedClip == null || SelectedAnimation == null)
            {
                return;
            }

            if (float.TryParse(ClipAnimationStartFromValue.Value, out float value))
            {
                if (SelectedAnimation.StartTime != value)
                {
                    SelectedAnimation.StartTime = value;
                }
            }
        }

        private void ClipAnimationEndsAtValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SelectedClip == null || SelectedAnimation == null)
            {
                return;
            }

            if (float.TryParse(ClipAnimationEndsAtValue.Value, out float value))
            {
                if (SelectedAnimation.EndTime != value)
                {
                    SelectedAnimation.EndTime = value;
                }
            }
        }

        private void ClipAnimationRateValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (SelectedClip == null || SelectedAnimation == null)
            {
                return;
            }

            if (float.TryParse(ClipAnimationRateValue.Value, out float value))
            {
                if (SelectedAnimation.Rate != value)
                {
                    SelectedAnimation.Rate = value;
                }
            }
        }

        private void ClipListAddButton_Click(object sender, MouseButtonEventArgs e)
        {
            InputWindow inputWindow = new("Clip name", "sample_clip",
                "* Only A-z 0-9 ^ @ - + __ chars are allowed.");

            inputWindow.InputApplied += delegate (object sender, InputWindowEventArgs args)
            {
                string clipName = args.Value.Trim().ToLowerInvariant();

                if (!Utils.IsNameValid(clipName, 64, false))
                {
                    _ = MessageBox.Show("Clip name is invalid!\n" +
                        "Must not be empty, contain forbidden symbols, whitespaces, and have less than 64 chars.",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                bool isNameTaken = CurrentAsset.Clips.Exists(_ => _.Name == clipName);

                if (isNameTaken)
                {
                    _ = MessageBox.Show($"Clip with name \"{clipName}\" already exist",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                AssetClip assetClip = new()
                {
                    Name = clipName,
                    Duration = 0.0f,
                    Animations = new(),
                };

                CurrentAsset.Clips.Add(assetClip);
                _ = CurrentAsset.Save();

                Variables.Instance.Set($"{CurrentAsset.Name}:clips:selection", clipName);

                ReloadClipsList();

                inputWindow.Close();
            };

            _ = inputWindow.ShowDialog();
        }

        private void ClipListRemoveButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedClip == null)
            {
                return;
            }

            bool removed = CurrentAsset.Clips.Remove(SelectedClip);

            if (!removed)
            {
                _ = MessageBox.Show($"An error occurred while removing clip \"{SelectedClip.Name}\"",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            SelectedClip = null;
            SelectedAnimation = null;

            _ = CurrentAsset.Save();

            Variables.Instance.Clear($"{CurrentAsset.Name}:clips:selection");

            ReloadClipsList();
        }

        private void ClipListEditButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedClip == null)
            {
                return;
            }

            InputWindow inputWindow = new("Rename clip", SelectedClip.Name,
                "* Only A-z 0-9 ^ @ - + __ chars are allowed.");

            inputWindow.InputApplied += delegate (object sender, InputWindowEventArgs args)
            {
                string clipName = args.Value.Trim().ToLowerInvariant();

                if (!Utils.IsNameValid(clipName, 64, false))
                {
                    _ = MessageBox.Show("Clip name is invalid!\n" +
                        "Must not be empty, contain forbidden symbols, whitespaces, and have less than 64 chars.",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                bool isNameTaken = CurrentAsset.Clips.Exists(_ => _.Name == clipName);

                if (isNameTaken)
                {
                    _ = MessageBox.Show($"Clip with name \"{clipName}\" already exist",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                SelectedClip.Name = clipName;
                _ = CurrentAsset.Save();

                Variables.Instance.Set($"{CurrentAsset.Name}:clips:selection", clipName);

                ReloadClipsList();

                inputWindow.Close();
            };

            _ = inputWindow.ShowDialog();
        }

        private void ClipListClearButton_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to remove all clips? " +
                "You won't be able to undo this. Continue?",
                Constants.BrandingName, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Remove clips as well.
            CurrentAsset.Clips.Clear();

            SelectedClip = null;
            SelectedAnimation = null;

            _ = CurrentAsset.Save();

            Variables.Instance.Clear($"{CurrentAsset.Name}:clips:selection");

            ReloadClipsList();
        }

        private void AssetClipAnimationsAddButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedClip == null)
            {
                return;
            }

            List<string> namesList = CurrentAsset.Dictionary.OnimFiles.Select(_ => _.Name).ToList();
            ListWindow listWindow = new("Animations list", namesList);

            listWindow.SelectionApply += delegate (object sender, ListWindowEventArgs args)
            {
                int selectedIndex = args.Value;

                if (selectedIndex != -1)
                {
                    string selectedName = namesList.ElementAt(selectedIndex);

                    bool animationUsed = SelectedClip.Animations.Exists(_ => _.Name == selectedName);

                    if (animationUsed)
                    {
                        _ = MessageBox.Show(
                            "This animation is already used in selected clip",
                            Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Warning);

                        return;
                    }

                    OnimFile? selectedOnim = CurrentAsset.Dictionary.OnimFiles.Find(_ => _.Name == selectedName);

                    if (selectedOnim != null)
                    {
                        // Specify some default data
                        AssetClipAnimation clipAnimation = new()
                        {
                            Name = selectedOnim.Name,
                            Rate = 1.0f,
                            StartTime = 0.0f,
                            EndTime = selectedOnim.Duration
                        };

                        SelectedClip.Animations.Add(clipAnimation);
                        _ = CurrentAsset.Save();

                        ReloadClipAnimationsList();
                    }
                }

                listWindow.Close();
            };

            _ = listWindow.ShowDialog();
        }

        private void AssetClipAnimationsRemoveButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedClip == null || SelectedAnimation == null)
            {
                return;
            }

            SelectedClip.Animations.Remove(SelectedAnimation);
            _ = CurrentAsset.Save();

            ReloadClipAnimationsList();
        }

        private void AssetClipAnimationsJumpButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedClip == null || SelectedAnimation == null)
            {
                return;
            }

            Variables.Instance.Set($"{CurrentAsset.Name}:animations:selection", SelectedAnimation.Name);

            ParentWindow.SelectMenuPage(AssetMenuPage.Animations);
        }
    }
}
