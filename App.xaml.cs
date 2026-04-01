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

        /// <summary>
        /// Invoked when Windows is logging off or shutting down.
        /// We only register the app for restart when Windows is shutting down/restarting
        /// and the user has enabled the Resume-after-reboot option.
        /// </summary>
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            if (Current.MainWindow is MainWindow mw)
            {
                if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown && mw.ResumeAfterReboot)
                {
                    mw.EnableResumeAfterReboot();
                }
                else
                {
                    mw.DisableResumeAfterReboot();
                }
            }

            base.OnSessionEnding(e);
        }
    }
}