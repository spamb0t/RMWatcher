using System.Windows;

namespace RMWatcher
{
    public partial class SettingsDialog : Window
    {
        public string PreferredLinkType { get; set; }
        public int PollingIntervalMinutes { get; set; }
        public bool AutoRun { get; set; }
        public bool CloseToTray { get; set; }
        public bool AlwaysStartMinimized { get; set; }

        public SettingsDialog(string preferredType, int interval, bool autoRun, bool closeToTray, bool alwaysStartMinimized)
        {
            InitializeComponent();

            // Pre-fill controls with existing values
            if (preferredType == "magnet")
                MagnetRadio.IsChecked = true;
            else
                TorrentRadio.IsChecked = true;

            IntervalBox.Text = interval.ToString();
            AutoRunBox.IsChecked = autoRun;
            CloseTrayBox.IsChecked = closeToTray;
            AlwaysMinBox.IsChecked = alwaysStartMinimized; // New!
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            PreferredLinkType = MagnetRadio.IsChecked == true ? "magnet" : "torrent";
            if (!int.TryParse(IntervalBox.Text.Trim(), out int interval) || interval < 1)
            {
                MessageBox.Show("Polling interval must be a positive number.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Warn user if polling interval is less than 60 (Reddit rate-limiting risk)
            if (interval < 60)
            {
                var result = MessageBox.Show(
                    "Polling less often than every 60 minutes is not recommended and may result in Reddit temporarily blocking or rate-limiting your requests. Are you sure you want to proceed?",
                    "Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            PollingIntervalMinutes = interval;
            AutoRun = AutoRunBox.IsChecked == true;
            CloseToTray = CloseTrayBox.IsChecked == true;
            AlwaysStartMinimized = AlwaysMinBox.IsChecked == true;
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
