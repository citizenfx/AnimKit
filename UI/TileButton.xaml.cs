using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for TileButton.xaml
    /// </summary>
    public partial class TileButton : UserControl
    {
        public string ImageSource { get; set; } = "";

        public string Title { get; set; } = "";

        public TileButton()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
