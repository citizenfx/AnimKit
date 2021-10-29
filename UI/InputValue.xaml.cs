using System.ComponentModel;
using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for InputValue.xaml
    /// </summary>
    public partial class InputValue : UserControl
    {
        public event PropertyChangedEventHandler? ValueChanged;

        public string Value
        {
            get => ValueInput.Value;
            set
            {
                ValueInput.Value = value;
                ValueChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public string Title { get; set; } = "Title";

        public string Units { get; set; } = "units";

        public InputValue()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ValueInput_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }
    }
}
