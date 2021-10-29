using System.Windows.Controls;
using System.Windows.Input;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AssetHelpPage.xaml
    /// </summary>
    public partial class AssetHelpPage : Page
    {
        private AssetWindow ParentWindow { get; }

        public Asset CurrentAsset
        {
            get => ParentWindow.CurrentAsset;
        }

        public AssetHelpPage(AssetWindow window)
        {
            ParentWindow = window;
            InitializeComponent();
        }

        private void DictionaryTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ParentWindow.SelectMenuPage(AssetMenuPage.Dictionary);
        }

        private void AnimationsTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ParentWindow.SelectMenuPage(AssetMenuPage.Animations);
        }

        private void ClipsTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ParentWindow.SelectMenuPage(AssetMenuPage.Clips);
        }
    }
}
