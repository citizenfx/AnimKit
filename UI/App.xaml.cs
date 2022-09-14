using System.Globalization;
using System.Threading;
using System.Windows;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // If we're running "new" file, act like an updater.
            if (Updater.EnsureUpdater())
            {
                Current.Shutdown();
                return;
            }

            // Adding variables from startup arguments.
            Variables.Instance.AddRaw(e.Args);

            // Adding variables from commandline.txt file
            Variables.Instance.AddRawFromFile("commandline.txt");

            bool showSplash = !Variables.Instance.Has("nosplash");

            SplashWindow window = new();

            if (showSplash)
            {
                window.Show();
            }
        }
    }
}
