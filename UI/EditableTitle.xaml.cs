using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for EditableTitle.xaml
    /// </summary>
    public partial class EditableTitle : UserControl
    {
        public string Title
        {
            get => EditTitle.Content?.ToString() ?? "";
            set => EditTitle.Content = value;
        }

        public EditableTitle()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
