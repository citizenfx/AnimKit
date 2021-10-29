using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for HeaderTitle.xaml
    /// </summary>
    public partial class HeaderTitle : UserControl
    {
        public string Title
        {
            get => TitleValue.Content?.ToString() ?? "";
            set => TitleValue.Content = value;
        }

        public HeaderTitle()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
