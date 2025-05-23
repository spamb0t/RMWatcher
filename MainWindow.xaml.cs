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
        private const string AppName = "RMWatcher";
        private static string SettingsFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RMWatcher", "settings.json");

        private const string RunRegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        // == MONITORING STATE ==
        private const int MaxUrls = 5;
        private List<string> monitoredUrls = new List<string>();
        private Dictionary<string, string> lastContents = new Dictionary<string, string>();
        private Dictionary<string, string> lastFoundLinks = new Dictionary<string, string>();
        private bool isMonitoring = false;
        private readonly HttpClient http = new HttpClient();

        // == TRAY STATE ==
        private NotifyIcon trayIcon;
        private bool trulyClosing = false;

        public MainWindow()
        {
            InitializeComponent();

            // Load settings
            LoadSettings();

            // Tray icon setup
            SetupTrayIcon();

            // Parse command-line args for minimized start
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args.Contains("--minimized"))
            {
                Hide();
            }

            Log("Welcome! Add up to 5 Reddit post URLs to monitor for magnet or .torrent links.");
        }

        #region Tray/Minimize Logic

        private void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = new System.Drawing.Icon("RMWatcher.ico");
            trayIcon.Visible = true;
            trayIcon.Text = "RMWatcher";

            // Context menu
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => ShowFromTray());
            menu.Items.Add("Settings", null, (s, e) => ShowSettings());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            trayIcon.ContextMenuStrip = menu;

            trayIcon.DoubleClick += (s, e) => ShowFromTray();
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApp()
        {
            trulyClosing = true;
            trayIcon.Visible = false;
            trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!trulyClosing && closeToTray)
            {
                e.Cancel = true;
                this.Hide();
                LogUI("App minimized to tray.");
            }
            else
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                base.OnClosing(e);
            }
        }

        #endregion

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
            if (monitoredUrls.Contains(url))
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

            monitoredUrls.Add(url);

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

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            monitoredUrls.Clear();
            UrlList.Items.Clear();
            lastContents.Clear();
            lastFoundLinks.Clear();
            Log("Cleared all URLs and reset state.");
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
            var dlg = new SettingsDialog(preferredLinkType, pollIntervalMin, autoRun, closeToTray)
            {
                Owner = this
            };
            if (dlg.ShowDialog() == true)
            {
                preferredLinkType = dlg.PreferredLinkType;
                pollIntervalMin = dlg.PollingIntervalMinutes;
                autoRun = dlg.AutoRun;
                closeToTray = dlg.CloseToTray;
                SaveSettings();

                if (autoRun)
                    EnableAutoRun();
                else
                    DisableAutoRun();

                LogUI($"Settings updated: Link type = {preferredLinkType}, Interval = {pollIntervalMin} min, Auto-run = {autoRun}, Close to tray = {closeToTray}");
            }
        }

        // All four settings now included!
        private class SettingsData
        {
            public string PreferredLinkType { get; set; }
            public int PollIntervalMin { get; set; }
            public bool AutoRun { get; set; }
            public bool CloseToTray { get; set; }
        }

        private void SaveSettings()
        {
            var data = new SettingsData
            {
                PreferredLinkType = preferredLinkType,
                PollIntervalMin = pollIntervalMin,
                AutoRun = autoRun,
                CloseToTray = closeToTray
            };
            //Ensure the directory for the settings file exists
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(data));
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
                }
                catch
                {
                    // Defaults in case of settings file corruption
                    preferredLinkType = "magnet";
                    pollIntervalMin = 60;
                    autoRun = false;
                    closeToTray = false;
                }
            }
            else
            {
                // Defaults
                preferredLinkType = "magnet";
                pollIntervalMin = 60;
                autoRun = false;
                closeToTray = false;
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

        private async void StartMonitoringLoop()
        {
            await System.Threading.Tasks.Task.Run(async () =>
            {
                while (isMonitoring)
                {
                    foreach (var url in monitoredUrls)
                    {
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

                            if (!lastContents.TryGetValue(url, out var prevContent) || selftext != prevContent)
                            {
                                LogUI($"[{url}] Post updated, scanning for links...");
                                lastContents[url] = selftext;

                                string foundLink = null;
                                if (preferredLinkType == "magnet")
                                {
                                    foundLink = ExtractMagnetLink(selftext) ?? ExtractTorrentLink(selftext);
                                }
                                else
                                {
                                    foundLink = ExtractTorrentLink(selftext) ?? ExtractMagnetLink(selftext);
                                }

                                if (foundLink != null)
                                {
                                    if (!lastFoundLinks.TryGetValue(url, out var lastLink) || foundLink != lastLink)
                                    {
                                        LogUI($"[{url}] New link found: {foundLink}");
                                        lastFoundLinks[url] = foundLink;

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
                                        LogUI($"[{url}] Link unchanged.");
                                    }
                                }
                                else
                                {
                                    LogUI($"[{url}] No matching link found in post.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogUI($"Error polling {url}: {ex.Message}");
                        }
                    }
                    // Wait for the user-set interval (in minutes)
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
