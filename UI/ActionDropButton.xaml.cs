using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for ActionDropButton.xaml
    /// </summary>
    public partial class ActionDropButton : UserControl
    {
        public string Title
        {
            get => (string)(ActionTitle?.Content ?? "");
            set => ActionTitle.Content = value;
        }

        public string Description
        {
            get => ActionDescription?.Text ?? "";
            set => ActionDescription.Text = value;
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user drop files on action")]
        public event DragEventHandler? ActionDrop;

        public ActionDropButton()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ActionRegion_Drop(object sender, DragEventArgs e)
        {
            ActionDrop?.Invoke(this, e);
        }
    }
}
