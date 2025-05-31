using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms; // For NotifyIcon
using System.Drawing;       // For Icon
using Microsoft.Win32;

namespace RMWatcher
{
    public partial class MainWindow : Window
    {
        // == SETTINGS MANAGEMENT ==
        private string preferredLinkType = "magnet"; // or "torrent"
        private int pollIntervalMin = 60; // Default polling interval
        private bool autoRun = false;
        private bool closeToTray = false;
        private bool alwaysStartMinimized = false;
        private const string AppName = "RMWatcher";
        private static string SettingsFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RMWatcher", "settings.json");

        private const string RunRegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        // == MONITORING STATE ==
        private const int MaxUrls = 5;
        private List<MonitoredUrl> monitoredUrls = new List<MonitoredUrl>();
        private bool isMonitoring = false;
        private readonly HttpClient http = new HttpClient();

        // == TRAY STATE ==
        private NotifyIcon trayIcon;
        private bool trulyClosing = false;

        /// <summary>
        /// Represents a monitored URL and its last known content hash.
        /// You can expand this with per-URL settings later.
        /// </summary>
        public class MonitoredUrl
        {
            public string Url { get; set; }
            public string LastContentHash { get; set; } = "";
            // Future-proof: add interval, label, group, etc. here
        }
        public MainWindow()
        {
            InitializeComponent();

            // Load settings
            LoadSettings();
            UrlList.Items.Clear();
            foreach (var entry in monitoredUrls)
            {
                var item = new System.Windows.Controls.ListBoxItem
                {
                    Content = entry.Url,
                    ToolTip = entry.Url
                };
                UrlList.Items.Add(item);
            }
            UrlList.SelectionChanged += UrlList_SelectionChanged;

            // Tray icon setup
            SetupTrayIcon();

            if (App.LaunchMinimized || alwaysStartMinimized)
            {
                this.Hide();
            }

            this.StateChanged += MainWindow_StateChanged;
            Log("Welcome! Add up to 5 Reddit post URLs to monitor for magnet or .torrent links.");
        }

        #region Tray/Minimize/Close Logic

        // Flag to ensure tray balloon tip only appears once per session
        private bool trayTipShownThisSession = false;

        /// <summary>
        /// Sets up the tray icon and associated menu actions.
        /// </summary>
        private void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = new System.Drawing.Icon("Appicon.ico");
            trayIcon.Visible = true;
            trayIcon.Text = "RMWatcher";

            // Context menu for the tray icon
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => RestoreFromTray());
            menu.Items.Add("Settings", null, (s, e) => ShowSettings());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            trayIcon.ContextMenuStrip = menu;

            // Double-click tray icon restores window
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        /// <summary>
        /// Unified method for hiding the window and showing tray notification.
        /// Used for both minimize and "close to tray".
        /// Only shows balloon tip once per session.
        /// </summary>
        /// <param name="fromClose">True if hiding due to close event, false if due to minimize.</param>
        private void HideToTray(bool fromClose)
        {
            this.Hide(); // Removes from taskbar and hides window

            // Show a balloon tip the first time only, to avoid spamming user
            if (!trayTipShownThisSession)
            {
                trayIcon.BalloonTipTitle = "RMWatcher";
                trayIcon.BalloonTipText = fromClose
                    ? "App is still running in the system tray (closed to tray)."
                    : "App is still running in the system tray (minimized).";
                trayIcon.ShowBalloonTip(1000);
                trayTipShownThisSession = true;
            }

            LogUI(fromClose
                ? "App closed to tray."
                : "App minimized to tray.");
        }

        /// <summary>
        /// Restores the main window from the tray.
        /// </summary>
        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        /// <summary>
        /// Exits the application completely, disposing the tray icon.
        /// </summary>
        private void ExitApp()
        {
            trulyClosing = true;
            trayIcon.Visible = false;
            trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles minimize-to-tray logic: hides window and shows tray icon when minimized.
        /// </summary>
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Minimize always goes to tray, regardless of "close to tray" setting
            if (this.WindowState == WindowState.Minimized)
            {
                HideToTray(fromClose: false); // Not triggered by close
            }
        }

        /// <summary>
        /// Handles "close to tray" logic: if enabled, hides window instead of exiting.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!trulyClosing && closeToTray)
            {
                e.Cancel = true;           // Cancel the actual close event
                HideToTray(fromClose: true); // Hide window, notify user
            }
            else
            {
                trayIcon.Visible = false;  // Fully exit, remove tray icon
                trayIcon.Dispose();
                base.OnClosing(e);
            }
        }

        #endregion


        // Add this line to the MainWindow constructor (after SetupTrayIcon):
        // this.StateChanged += MainWindow_StateChanged;


        #region UI Event Handlers

        // ==== UPDATED: Fetches Reddit post title from JSON API, shows in ListBox, URL on hover ====
        private async void AddUrl_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                Log("Please enter a Reddit post URL.");
                return;
            }
            if (monitoredUrls.Count >= MaxUrls)
            {
                Log($"You can only monitor up to {MaxUrls} URLs.");
                return;
            }
            if (monitoredUrls.Any(x => x.Url == url))
            {
                Log("This URL is already being monitored.");
                return;
            }
            if (!IsValidRedditPostUrl(url))
            {
                Log("Invalid Reddit post URL. Example: https://www.reddit.com/r/sub/comments/abc123/title/");
                return;
            }

            // --- Fetch the real Reddit post title using the JSON API ---
            string title = url;
            try
            {
                // Reddit requires a User-Agent header or you'll get 429/403 errors
                http.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) RedditMagnetWatcher/1.0"
                );
                string jsonUrl = url.TrimEnd('/') + "/.json";
                string json = await http.GetStringAsync(jsonUrl);

                using var doc = JsonDocument.Parse(json);
                // Path: [0].data.children[0].data.title
                title = doc.RootElement[0]
                            .GetProperty("data")
                            .GetProperty("children")[0]
                            .GetProperty("data")
                            .GetProperty("title")
                            .GetString() ?? "(No Title)";
            }
            catch (Exception ex)
            {
                title = "(Failed to load title)";
                Log($"Could not fetch post title: {ex.Message}");
            }

            monitoredUrls.Add(new MonitoredUrl { Url = url, LastContentHash = "" });
            SaveSettings();

            // --- Add ListBoxItem with title as Content, URL as ToolTip ---
            var item = new System.Windows.Controls.ListBoxItem
            {
                Content = title,
                ToolTip = url
            };
            UrlList.Items.Add(item);

            UrlInput.Text = "";
            Log($"Added: {title}");
        }

        // === Future feature: Remove a single URL from monitoring ===
        // This method will remove the first matching URL from monitoredUrls, update the settings, and refresh the UI.
        // To use: uncomment, then call from your "Remove" button handler (when you add one).
        /*
        private void RemoveUrl(string urlToRemove)
        {
            var toRemove = monitoredUrls.FirstOrDefault(x => x.Url == urlToRemove);
            if (toRemove != null)
            {
                monitoredUrls.Remove(toRemove);
                SaveSettings();

                // Refresh the UI ListBox (removes only the deleted entry)
                foreach (var item in UrlList.Items.OfType<ListBoxItem>().ToList())
                {
                    if ((string)item.Content == urlToRemove)
                        UrlList.Items.Remove(item);
                }

                Log($"Removed URL: {urlToRemove}");
            }
        }
        */

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            monitoredUrls.Clear();
            UrlList.Items.Clear();
            SaveSettings();
            Log("Cleared all URLs and reset state.");
        }
        
        private void UrlList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearSelectedBtn.IsEnabled = UrlList.SelectedItems.Count > 0;
        }
        private void ClearSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            // Make a copy to avoid collection modification issues
            var itemsToRemove = UrlList.SelectedItems.Cast<System.Windows.Controls.ListBoxItem>().ToList();
            if (itemsToRemove.Count == 0)
                return;

            int removedCount = 0;

            foreach (var item in itemsToRemove)
            {
                string urlToRemove = item.ToolTip as string;
                var found = monitoredUrls.FirstOrDefault(x => x.Url == urlToRemove);
                if (found != null)
                {
                    monitoredUrls.Remove(found);
                    removedCount++;
                }
                UrlList.Items.Remove(item);
            }

            if (removedCount > 0)
            {
                SaveSettings();
                LogUI($"Cleared {removedCount} URL{(removedCount > 1 ? "s" : "")} from monitoring.");
            }
        }


        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (monitoredUrls.Count == 0)
            {
                Log("Add at least one URL before starting.");
                return;
            }
            isMonitoring = true;
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            Log("Monitoring started...");
            StartMonitoringLoop();
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            isMonitoring = false;
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            Log("Monitoring stopped.");
        }

        #endregion

        #region Settings Dialog & Persistence

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            var dlg = new SettingsDialog(preferredLinkType, pollIntervalMin, autoRun, closeToTray, alwaysStartMinimized)
            {
                Owner = this
            };
            if (dlg.ShowDialog() == true)
            {
                preferredLinkType = dlg.PreferredLinkType;
                pollIntervalMin = dlg.PollingIntervalMinutes;
                autoRun = dlg.AutoRun;
                closeToTray = dlg.CloseToTray;
                alwaysStartMinimized = dlg.AlwaysStartMinimized; // New!
                SaveSettings();

                if (autoRun)
                    EnableAutoRun();
                else
                    DisableAutoRun();

                LogUI($"Settings updated: Link type = {preferredLinkType}, Interval = {pollIntervalMin} min, Auto-run = {autoRun}, Close to tray = {closeToTray}, Always start minimized = {alwaysStartMinimized}");
            }
        }

        private class SettingsData
        {
            public string PreferredLinkType { get; set; }
            public int PollIntervalMin { get; set; }
            public bool AutoRun { get; set; }
            public bool CloseToTray { get; set; }
            public bool AlwaysStartMinimized { get; set; }
            public List<MonitoredUrl> MonitoredUrls { get; set; } = new List<MonitoredUrl>(); // Saves all monitored URL in the user settings
        }


        private void SaveSettings()
        {
            var data = new SettingsData
            {
                PreferredLinkType = preferredLinkType,
                PollIntervalMin = pollIntervalMin,
                AutoRun = autoRun,
                CloseToTray = closeToTray,
                AlwaysStartMinimized = alwaysStartMinimized,
                MonitoredUrls = monitoredUrls
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                LogUI($"Failed to save settings: {ex.GetType().Name}: {ex.Message}");
                // Show a one-time error message popup
                Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Settings could not be saved. See log for details.", "RMWatcher - Error", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }


        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    var text = File.ReadAllText(SettingsFile);
                    var data = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(text);
                    preferredLinkType = data.PreferredLinkType ?? "magnet";
                    pollIntervalMin = Math.Max(data.PollIntervalMin, 60);
                    autoRun = data.AutoRun;
                    closeToTray = data.CloseToTray;
                    alwaysStartMinimized = data.AlwaysStartMinimized;
                    monitoredUrls = data.MonitoredUrls ?? new List<MonitoredUrl>();
                }
                catch (Exception ex)
                {
                    LogUI("Settings file is missing or corrupted. Loading defaults.");
                    LogUI($"Settings error: {ex.GetType().Name}: {ex.Message}");
                    // Show a one-time error message popup
                    Dispatcher.Invoke(() => System.Windows.MessageBox.Show("Settings file could not be read and defaults were loaded. See log for details.", "RMWatcher - Error", MessageBoxButton.OK, MessageBoxImage.Warning));
                    preferredLinkType = "magnet";
                    pollIntervalMin = 60;
                    autoRun = false;
                    closeToTray = false;
                    alwaysStartMinimized = false;
                    monitoredUrls = new List<MonitoredUrl>();
                }

            }
            else
            {
                // Defaults
                preferredLinkType = "magnet";
                pollIntervalMin = 60;
                autoRun = false;
                closeToTray = false;
                alwaysStartMinimized = false;
                monitoredUrls = new List<MonitoredUrl>();
            }
        }

        #endregion

        #region Auto-run at Startup (Registry)

        private void EnableAutoRun()
        {
            string exePath = $"\"{System.Reflection.Assembly.GetExecutingAssembly().Location}\" --minimized";
            using (var key = Registry.CurrentUser.CreateSubKey(RunRegKey))
            {
                key.SetValue(AppName, exePath);
            }
        }

        private void DisableAutoRun()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunRegKey))
            {
                if (key.GetValue(AppName) != null)
                    key.DeleteValue(AppName);
            }
        }

        #endregion

        #region Polling & Link Detection

        private bool IsValidRedditPostUrl(string url)
        {
            return url.StartsWith("https://www.reddit.com/r/")
                && url.Contains("/comments/")
                && !url.EndsWith("/comments/");
        }

        /// <summary>
        /// Starts the main monitoring loop. For each monitored URL, fetches content,
        /// computes a hash for change detection, and launches new links if found.
        /// Persists URL hashes after each scan.
        /// </summary>
        private async void StartMonitoringLoop()
        {
            await System.Threading.Tasks.Task.Run(async () =>
            {
                while (isMonitoring)
                {
                    bool settingsChanged = false; // Track if we need to save settings
                    
                    foreach (var entry in monitoredUrls) // Loop over each monitored URL object
                    {
                        string url = entry.Url; // Use the stored URL

                        try
                        {
                            string jsonUrl = url.TrimEnd('/') + "/.json";
                            http.DefaultRequestHeaders.UserAgent.ParseAdd("RMWatcher/0.2 (by spamb0t)");

                            var resp = await http.GetAsync(jsonUrl);
                            if (!resp.IsSuccessStatusCode)
                            {
                                LogUI($"Failed to fetch {url}: {resp.StatusCode}");
                                continue;
                            }
                            var text = await resp.Content.ReadAsStringAsync();

                            using var doc = JsonDocument.Parse(text);
                            var root = doc.RootElement;
                            string selftext = "";
                            try
                            {
                                selftext = root[0].GetProperty("data")
                                                .GetProperty("children")[0]
                                                .GetProperty("data")
                                                .GetProperty("selftext")
                                                .GetString() ?? "";
                            }
                            catch
                            {
                                LogUI($"Failed to parse selftext for {url}");
                                continue;
                            }

                            // --- NEW: Hash-based change detection ---
                            string currentHash = ComputeHash(selftext); // Compute the current content hash

                            if (entry.LastContentHash != currentHash)
                            {
                                LogUI($"[{url}] Post updated, scanning for links...");
                                entry.LastContentHash = currentHash; // Save the new hash

                                string foundLink = null;
                                if (preferredLinkType == "magnet")
                                {
                                    foundLink = ExtractMagnetLink(selftext) ?? ExtractTorrentLink(selftext);
                                }
                                else
                                {
                                    foundLink = ExtractTorrentLink(selftext) ?? ExtractMagnetLink(selftext);
                                }

                                if (!string.IsNullOrEmpty(foundLink))
                                {
                                    LogUI($"[{url}] New link found: {foundLink}");

                                    try
                                    {
                                        // Open with system default app
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = foundLink,
                                            UseShellExecute = true
                                        });
                                        LogUI($"Launched link via system.");
                                    }
                                    catch (Exception ex)
                                    {
                                        LogUI($"Failed to launch link: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    LogUI($"[{url}] No matching link found in post.");
                                }

                                // Persist the updated hash after each change
                                settingsChanged = true;
                            }
                            else
                            {
                                // No change detected; do nothing
                                // Optionally, you can log if desired:
                                // LogUI($"[{url}] No content change detected.");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogUI($"Error polling {url}: {ex.Message}");
                        }
                    }

                    if (settingsChanged)
                    {
                        SaveSettings(); // Save settings if any URL hash changed
                        LogUI("Settings saved after content change detection.");
                    }

                    // Wait for the user-set interval (in minutes), but allow early stop
                        for (int i = 0; i < pollIntervalMin * 60; i++)
                        {
                            if (!isMonitoring) break;
                            await System.Threading.Tasks.Task.Delay(1000);
                        }
                }
            });
        }

        private string? ExtractMagnetLink(string selftext)
        {
            var m = Regex.Match(selftext, @"magnet:\?[^ \r\n]+");
            return m.Success ? m.Value : null;
        }

        private string? ExtractTorrentLink(string selftext)
        {
            // Simple .torrent URL: must start with http(s) and end with .torrent (common in trackers)
            var m = Regex.Match(selftext, @"https?://\S+\.torrent");
            return m.Success ? m.Value : null;
        }
        /// <summary>
        /// Computes a SHA256 hash for change detection.
        /// </summary>
        public static string ComputeHash(string content)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(content ?? "");
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        #endregion

        #region Logging

        private void Log(string message)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            LogBox.ScrollToEnd();
        }

        private void LogUI(string message)
        {
            Dispatcher.Invoke(() => Log(message));
        }

        #endregion
    }
}
