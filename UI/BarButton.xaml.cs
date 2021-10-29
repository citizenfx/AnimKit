using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for BarButton.xaml
    /// </summary>
    public partial class BarButton : UserControl
    {
        public string ImageSource { get; set; } = "";

        public string Title { get; set; } = "";

        public BarButton()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
