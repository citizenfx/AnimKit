using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for MenuButton.xaml
    /// </summary>
    public partial class MenuButton : UserControl
    {
        public string ImageSource { get; set; } = "";

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public MenuButton()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
