using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for LoadingPanel.xaml
    /// </summary>
    public partial class LoadingPanel : UserControl
    {
        public string Title
        {
            get => ProgressTitle.Content?.ToString() ?? "";
            set => ProgressTitle.Content = value;
        }

        public string Status
        {
            get => ProgressStatus.Content?.ToString() ?? "";
            set => ProgressStatus.Content = value;
        }

        public LoadingPanel()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void SetProgress(string title, string status)
        {
            Title = title;
            Status = status;
        }
    }
}
