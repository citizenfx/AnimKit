using System.ComponentModel;
using System.Windows.Controls;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string RawValue { get; set; } = "";

        public string Value
        {
            get => RawValue;
            set
            {
                RawValue = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public InputBox()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
