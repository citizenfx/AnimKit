using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for IconButton.xaml
    /// </summary>
    public partial class IconButton : UserControl
    {
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks on button")]
        public event MouseButtonEventHandler? Click;

        public string ImageSource { get; set; } = "";

        public bool Blocked
        {
            get => RawBlocked;
            set
            {
                RawBlocked = value;
                ButtonGrid.Opacity = value ? 0.4 : 1.0;
                Panel.SetZIndex(ButtonBlocker, value ? 1 : -1);
            }
        }

        private bool RawBlocked { get; set; }

        public IconButton()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ButtonRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Blocked)
            {
                Click?.Invoke(this, e);
            }
        }
    }
}
