using System.Linq;
using System.Windows;

namespace RMWatcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Static property to check for --minimized on launch
        public static bool LaunchMinimized { get; private set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Parse command line args before main window is shown
            LaunchMinimized = e.Args.Any(arg => arg.Equals("--minimized", System.StringComparison.OrdinalIgnoreCase));
            base.OnStartup(e);
        }
    }
}
