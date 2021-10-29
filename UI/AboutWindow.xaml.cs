using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void VisitButton_Click(object sender, MouseButtonEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo("cmd", $"/c start https://cfx.re") { CreateNoWindow = true });
        }

        private void Window_Initialized(object sender, System.EventArgs e)
        {
            VersionLabel.Content = $"Version {Updater.CurrentVersion}";
        }
    }
}
