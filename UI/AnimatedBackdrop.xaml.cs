using System;
using System.Windows;
using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AnimatedBackdrop.xaml
    /// </summary>
    public partial class AnimatedBackdrop : UserControl
    {
        public AnimatedBackdrop()
        {
            InitializeComponent();
        }

        private void Grid_Initialized(object sender, EventArgs e)
        {
            bool direction = Utils.RandomBool();

            float offsetX = 3f * (!direction ? Utils.Randomizer.NextSingle() : (Utils.RandomBool() ? 1.0f : -1.0f));
            float offsetY = 3f * (direction ? Utils.Randomizer.NextSingle() : (Utils.RandomBool() ? 1.0f : -1.0f));

            BackdropAnimation.To = new Rect(offsetX, offsetY, 1, 1);
        }
    }
}
