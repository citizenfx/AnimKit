using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AssetAnimationsPage.xaml
    /// </summary>
    public partial class AssetAnimationsPage : Page
    {
        /// <summary>
        /// Current asset getter.
        /// </summary>
        public Asset CurrentAsset => ParentWindow.CurrentAsset;

        /// <summary>
        /// Selected animation.
        /// </summary>
        public OnimFile? SelectedOnim { get; set; }

        /// <summary>
        /// Parent asset window.
        /// </summary>
        private AssetWindow ParentWindow { get; }

        public AssetAnimationsPage(AssetWindow window)
        {
            ParentWindow = window;
            InitializeComponent();
        }

        /// <summary>
        /// Gets selected animation dictionary from animations list.
        /// </summary>
        /// <returns>Selected animation dictionary</returns>
        private OnimFile? GetSelectedAnimationFile()
        {
            int selectedIndex = AssetAnimationList.SelectedIndex;

            if (selectedIndex == -1)
            {
                return null;
            }

            OadFile? dict = CurrentAsset.Dictionary;

            return dict?.OnimFiles[selectedIndex];
        }

        /// <summary>
        /// Performs list animation selection.
        /// </summary>
        /// <param name="onim">Selected animation</param>
        private void SelectAssetAnimation(OnimFile onim)
        {
            SelectedOnim = onim;

            Variables.Instance.Set($"{CurrentAsset.Name}:animations:selection", onim.Name);

            UpdateBonesData();

            {
                AssetAnimationFlagsValue.Content = $"x{onim.Flags.Length}";
                AssetAnimationFlagsValue.ToolTip = string.Join(" ", onim.Flags);
            }

            {
                int clipsCount = CurrentAsset.Clips.Count(clip => clip.Animations.Any(anim => anim.Name == SelectedOnim.Name));
                AssetAnimationClipsValue.Content = clipsCount;
            }

            AssetAnimationSequencesValue.Content = onim.SequenceList.Count;
            AssetAnimationFramesValue.Content = onim.Frames;
            AssetAnimationDurationValue.Content = onim.Duration.ToString("0.000s", CultureInfo.InvariantCulture);
            AssetAnimationFPSValue.Content = (onim.Frames / onim.Duration).ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reloads animations list.
        /// </summary>
        private void ReloadAnimationsList()
        {
            AssetAnimationList.Items.Clear();
            AnimationListClearButton.Blocked = CurrentAsset.Dictionary.OnimFiles.Count == 0;

            string lastSelection = Variables.Instance.Get($"{CurrentAsset.Name}:animations:selection", string.Empty);

            foreach (OnimFile onim in CurrentAsset.Dictionary.OnimFiles)
            {
                int index = AssetAnimationList.Items.Add(onim.Name);

                if (lastSelection == onim.Name)
                {
                    AssetAnimationList.SelectedIndex = index;
                }
            }

            bool assetSelected = AssetAnimationList.SelectedIndex != -1;
            AssetAnimationSelected.Visibility = assetSelected ? Visibility.Visible : Visibility.Hidden;
            AssetAnimationNotSelected.Visibility = assetSelected ? Visibility.Hidden : Visibility.Visible;

            AssetAnimationsTitle.Content = $"ANIMATIONS ({AssetAnimationList.Items.Count})";
        }

        /// <summary>
        /// Updates bone data of selected animation.
        /// </summary>
        private void UpdateBonesData()
        {
            if (SelectedOnim == null)
            {
                return;
            }

            List<int> bones = new();

            SelectedOnim.SequenceList.ForEach(seq =>
            {
                if (seq != null && !bones.Contains(seq.BoneID))
                {
                    bones.Add(seq.BoneID);
                }
            });

            AssetAnimationBonesValue.Content = bones.Count;

            if (bones.Count == 0)
            {
                AssetAnimationSkeletonValue.Content = "NONE";
                AssetAnimationSkeletonValue.ToolTip = "Animation has no skeleton data.";
                return;
            }

            // This logic might look a bit weird but we're not really can 100% predict skeleton type
            // ONLY using bone indexes. The point of this code is just *try* to predict in  obvious cases.
            // For example, very doubtful that human animation would ever use wing bones.
            // Probably this logic might be improved a lot, but it will require more researchings.

            string predictedSkeleton = "UNK";

            List<int> spineBones = new()
            {
                // SKEL_Pelvis, SKEL_Spine_Root, SKEL_Spine(0-3)
                11816,
                57597,
                23553,
                24816,
                24817,
                24818,
            };

            List<int> fingerBones = new()
            {
                // SKEL_(L/R)_Finger**
                58866,
                64016,
                64017,
                58867,
                64096,
                64097,
                58868,
                64112,
                64113,
                58869,
                64064,
                64065,
                58870,
                64080,
                64081,
                26610,
                4089,
                4090,
                26611,
                4169,
                4170,
                26612,
                4185,
                4186,
                26613,
                4137,
                4138,
                26614,
                4153,
                4154,
            };

            List<int> whaleBones = new()
            {
                // SKEL_(L/R)_TailFin_(00/01), SKEL_Dorsal_(01/02)
                695,
                696,
                30438,
                30439,
                3974,
                3975,
            };

            List<int> wingBones = new()
            {
                // MH_(L/R)_ WingRotator, WingPusherInnerROOT, WingPusherInner
                32398,
                34726,
                54749,
                65263,
                36262,
                58439,
            };

            List<int> tailBones = new()
            {
                // SKEL_Tail_(0-5)
                30992,
                30993,
                30994,
                30995,
                30996,
                30997,
            };

            List<int> facialBones = new()
            {
                // taken from facials@gen_male@base
                840,
                1574,
                1608,
                10274,
                15406,
                17867,
                18040,
                19205,
                21981,
                22140,
                23631,
            };

            // wheel_(l/r)(f/r), chasis_dummy, steeringwheel, exhaust, bodyshell, bonnet, door_(d/p)side_(f/r), suspension_(r/l)(f/r)
            List<int> vehicleBones = new()
            {
                26398,
                26418,
                27902,
                27922,
                39433,
                20285,
                4944,
                60113,
                42990,
                35346,
                60963,
                35230,
                61071,
                5589,
                6517,
                6505,
                5577,
            };

            // If has a spine, most likely it's creature
            if (spineBones.Intersect(bones).Any())
            {
                if (whaleBones.Intersect(bones).Any())
                {
                    predictedSkeleton = "WHALE";
                }
                else if (wingBones.Intersect(bones).Any())
                {
                    predictedSkeleton = "BIRD";
                }
                else if (tailBones.Intersect(bones).Any())
                {
                    predictedSkeleton = "ANIMAL";
                }
                else
                {
                    predictedSkeleton = "HUMAN";
                }
            }
            else if (fingerBones.Intersect(bones).Any())
            {
                predictedSkeleton = "FINGERS";
            }
            else if (facialBones.Intersect(bones).Any())
            {
                predictedSkeleton = "FACIAL";
            }
            else if (vehicleBones.Intersect(bones).Any())
            {
                predictedSkeleton = "VEHICLE";
            }

            bool isUnknown = predictedSkeleton == "UNK";

            AssetAnimationSkeletonValue.Content = isUnknown ? "UNK" : $"{predictedSkeleton}?";
            AssetAnimationSkeletonValue.ToolTip = isUnknown ? "Could not identify skeleton" : "This prediction might be wrong.";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadAnimationsList();
        }

        private void AssetAnimationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = AssetAnimationList.SelectedIndex;
            bool assetSelected = selectedIndex != -1;

            AssetAnimationSelected.Visibility = assetSelected ? Visibility.Visible : Visibility.Hidden;
            AssetAnimationNotSelected.Visibility = assetSelected ? Visibility.Hidden : Visibility.Visible;

            AnimationListRemoveButton.Blocked = !assetSelected;
            AnimationListEditButton.Blocked = !assetSelected;

            if (!assetSelected)
            {
                SelectedOnim = null;
                return;
            }

            OnimFile? onim = CurrentAsset.Dictionary.OnimFiles?[selectedIndex];

            if (onim != null)
            {
                SelectAssetAnimation(onim);
            }
        }

        private void AnimationListAddButton_Click(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.DefaultExt = ".onim";
            dialog.Multiselect = true;
            dialog.Filter = "ONIM file (*.onim)|*.onim";

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            if (dialog.FileNames.Length <= 0)
            {
                return;
            }

            foreach (string fileName in dialog.FileNames)
            {
                if (fileName.EndsWith(".onim"))
                {
                    _ = CurrentAsset.Dictionary.AddOnim(fileName);
                }
            }

            _ = CurrentAsset.Save();

            ReloadAnimationsList();
        }

        private void AnimationListRemoveButton_Click(object sender, MouseButtonEventArgs e)
        {
            OnimFile? onim = GetSelectedAnimationFile();

            if (onim != null)
            {
                if (SelectedOnim == onim)
                {
                    SelectedOnim = null;
                }

                CurrentAsset.Dictionary.RemoveOnim(onim);
                _ = CurrentAsset.Save();
            }

            ReloadAnimationsList();
        }

        private void AnimationListEditButton_Click(object sender, MouseButtonEventArgs e)
        {
            OnimFile? onim = GetSelectedAnimationFile();

            if (onim == null)
            {
                return;
            }

            InputWindow inputWindow = new("Rename animation", onim.Name,
                "* Only A-z 0-9 ^ @ - + __ chars are allowed. Must be a valid file name.");

            inputWindow.InputApplied += delegate (object sender, InputWindowEventArgs args)
            {
                string newName = args.Value.Trim().ToLowerInvariant();
                string oldName = onim.Name;

                if (!Utils.IsNameValid(newName))
                {
                    _ = MessageBox.Show("Animation name is invalid!\n" +
                        "Must not be empty, have less than 64 chars and not contain any forbidden symbols.",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                bool success = CurrentAsset.Dictionary.RenameOnim(onim, newName);

                if (!success)
                {
                    _ = MessageBox.Show(
                        "An error occurred while renaming animation. " +
                        "Filename might be already taken, or AnimKit lacks access to the folder?",
                        Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                // Replace names in associated clips
                foreach (AssetClip assetClip in CurrentAsset.Clips)
                {
                    foreach (AssetClipAnimation clipAnim in assetClip.Animations)
                    {
                        if (clipAnim.Name == oldName)
                        {
                            clipAnim.Name = newName;
                        }
                    }
                }

                _ = CurrentAsset.Save();

                Variables.Instance.Set($"{CurrentAsset.Name}:animations:selection", onim.Name);

                ReloadAnimationsList();

                inputWindow.Close();
            };

            _ = inputWindow.ShowDialog();
        }

        private void AnimationListClearButton_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to remove all animations? Clips will be removed as well. " +
                "You won't be able to undo this. Continue?",
                Constants.BrandingName, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Remove all animations.
            CurrentAsset.Dictionary.RemoveAllOnims();

            // Remove clips as well.
            CurrentAsset.Clips.Clear();

            _ = CurrentAsset.Save();

            Variables.Instance.Clear($"{CurrentAsset.Name}:animations:selection");

            SelectedOnim = null;
            ReloadAnimationsList();
        }

        private void AssetAnimationBonesList_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            Dictionary<int, List<string>> boneEntries = new();

            foreach (OnimSequence seq in SelectedOnim.SequenceList)
            {
                if (seq == null)
                {
                    continue;
                }

                if (boneEntries.TryGetValue(seq.BoneID, out List<string>? types))
                {
                    types.Add(seq.Track);
                }
                else
                {
                    boneEntries.Add(seq.BoneID, new() { seq.Track });
                }
            }

            List<string> bonesList = new();

            foreach (KeyValuePair<int, List<string>> boneData in boneEntries)
            {
                string trackTypes = string.Join(", ", boneData.Value);
                bonesList.Add($"{boneData.Key} [{trackTypes}]");
            }

            ListWindow listWindow = new("Bones list", bonesList);
            _ = listWindow.ShowDialog();
        }

        private void AssetAnimationSave_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            bool success = CurrentAsset.Dictionary.ExportOnim(SelectedOnim, dialog.SelectedPath);

            if (!success)
            {
                _ = MessageBox.Show(
                    "Failed to export animation. " +
                    "Filename might be already taken, or AnimKit lacks access to the folder?",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AssetAnimationLoad_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.DefaultExt = ".onim";
            dialog.Multiselect = false;
            dialog.Filter = "ONIM file (*.onim)|*.onim";

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            bool success = CurrentAsset.Dictionary.ImportOnim(SelectedOnim, dialog.FileName);

            if (!success)
            {
                _ = MessageBox.Show(
                    "Failed to import animation. " +
                    "Filename might be already taken, or AnimKit lacks access to the folder?",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SelectedOnim = null;
            ReloadAnimationsList();
        }

        private void AssetAnimationExportSingle_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            bool success = CurrentAsset.ExportSingle(SelectedOnim, dialog.SelectedPath);

            if (!success)
            {
                _ = MessageBox.Show(
                    "Failed to export single animation. " +
                    "Filename might be already taken, or AnimKit lacks access to the folder?",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AssetAnimationClipsList_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            // Finding associated clips.
            List<AssetClip> onimClips = CurrentAsset.Clips.FindAll(clip => clip.Animations.Any(anim => anim.Name == SelectedOnim.Name));

            if (onimClips.Count == 0)
            {
                _ = MessageBox.Show("Selected animation has no associated clips.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            // Generating clip names list.
            List<string> namesList = onimClips.Select(clip => clip.Name).ToList();

            ListWindow listWindow = new("Clips list", namesList);

            listWindow.SelectionApply += delegate (object sender, ListWindowEventArgs args)
            {
                int selectedIndex = args.Value;

                if (selectedIndex != -1)
                {
                    string selectedName = namesList.ElementAt(selectedIndex);
                    AssetClip? selectedClip = onimClips.Find(_ => _.Name == selectedName);

                    if (selectedClip != null)
                    {
                        // Setting clip as selected.
                        Variables.Instance.Set($"{CurrentAsset.Name}:clips:selection", selectedClip.Name);

                        // Select clips asset menu page.
                        ParentWindow.SelectMenuPage(AssetMenuPage.Clips);
                    }
                }

                listWindow.Close();
            };

            _ = listWindow.ShowDialog();
        }

        private void AssetAnimationPlugin_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedOnim == null)
            {
                return;
            }

            string onimPath = Path.Combine(CurrentAsset.Dictionary.FileDirectory,
                CurrentAsset.Dictionary.Name, SelectedOnim.Name + ".onim");;

            if (!File.Exists(onimPath))
            {
                return;
            }

            bool success = Utils.WriteRoamingFileContent(".ANIMKIT_ANIMATION", onimPath);

            if (!success)
            {
                _ = MessageBox.Show("An error occurred while writing animation data.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            _ = MessageBox.Show("Animation is ready to be imported. Use \"Quick Import\" button in 3ds Max plugin.\n" +
                "When you're done editing, you can use \"Quick Export\" to export the animation from 3ds Max back to AnimKit asset.",
                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AssetAnimationList_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length == 0)
            {
                return;
            }

            List<string> importedList = new();

            foreach (string filePath in files)
            {
                if (!filePath.EndsWith(".onim"))
                {
                    continue;
                }

                bool success = CurrentAsset.Dictionary.AddOnim(filePath);

                if (success)
                {
                    importedList.Add(Path.GetFileNameWithoutExtension(filePath));
                }
            }

            if (importedList.Count == 0)
            {
                _ = MessageBox.Show("Nothing was imported, only .onim files are supported.",
                    Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);

                return;
            }

            _ = CurrentAsset.Save();

            ReloadAnimationsList();

            _ = MessageBox.Show($"Imported {importedList.Count} onim files:\n" +
                string.Join("\n", importedList),
                Constants.BrandingName, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
