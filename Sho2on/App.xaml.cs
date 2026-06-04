using HR_Application.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HR_Application.Helpers;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Media;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private bool _isDarkTheme = false;

        public static User CurrentUser { get; set; }
        public static IServiceProvider ServiceProvider { get; private set; }
        public static HubConnection SignalRConnection { get; set; }
        public static string ServerIP { get; private set; }
        public static int SignalRPort { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {


            base.OnStartup(e);
            ThemeManager.Initialize();
            LocalizationManager.Initialize();
            LocalizationManager.RegisterAutomaticLocalization();
            LoadServerSettings();
            LoginScreen login = new LoginScreen();
            login.ShowDialog();
            //LoadThemePreference();
        }
        public static string ConnectionString { get; set; }
        public static string SoftechConnectionString { get; set; }
        public static string SoftechSQLConnectionString { get; set; }
        public static List<string> userPermissions { get; set; }
        public static List<int> userBranches { get; set; }
        public bool IsDarkTheme => _isDarkTheme;

        private void LoadThemePreference()
        {
            // Simple implementation - could be expanded to use proper settings storage
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingsPath = System.IO.Path.Combine(appDataPath, "ModernTodoApp", "settings.txt");

                if (System.IO.File.Exists(settingsPath))
                {
                    var content = System.IO.File.ReadAllText(settingsPath);
                    if (bool.TryParse(content, out bool isDark))
                    {
                        ApplyTheme(isDark);
                    }
                }
            }
            catch
            {
                // If loading fails, use default light theme
                ApplyTheme(false);
            }
        }
        // إنشاء المجلدات المطلوبة إذا لم تكن موجودة
        private void CreateRequiredFolders()
        {
            var folders = new[] { "CompanyDocuments", "SignedDocuments" };

            foreach (var folder in folders)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), folder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        public void SwitchTheme()
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme(_isDarkTheme);
            SaveThemePreference();
        }

        

        private void SaveThemePreference()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = System.IO.Path.Combine(appDataPath, "ModernTodoApp");

                if (!System.IO.Directory.Exists(appFolder))
                {
                    System.IO.Directory.CreateDirectory(appFolder);
                }

                var settingsPath = System.IO.Path.Combine(appFolder, "settings.txt");
                System.IO.File.WriteAllText(settingsPath, _isDarkTheme.ToString());
            }
            catch
            {
                // Silently fail if saving theme preference fails
            }
        }

        public void ApplyTheme(bool isDarkTheme)
        {
            _isDarkTheme = isDarkTheme;

            // Clear existing theme resources
            var existingTheme = Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("Theme.xaml") == true);

            if (existingTheme != null)
            {
                Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Add new theme
            var themeUri = _isDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            var newTheme = new ResourceDictionary()
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            Resources.MergedDictionaries.Insert(0, newTheme);

            if (!_isDarkTheme)
            {
                // ✅ Light → طبق الألوان اللي المستخدم مختارها
                LoadColorsFromSettings();
            }
            else
            {
                // ✅ Dark → امسح أي تأثير من Settings وخليها اللي في XAML
                ResetColorsToThemeDefaults();
            }
        }

        public void LoadColorsFromSettings()
        {
            try
            {
                var primary = HR_Application.Properties.Settings.Default.PrimaryColor;
                var secondary = HR_Application.Properties.Settings.Default.SecondaryColor;
                var third = HR_Application.Properties.Settings.Default.ThirdColorBrush;

                Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(primary);
                Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(secondary);
                Application.Current.Resources["ThirdColorBrush"] = new SolidColorBrush(third);

                var primaryBackground = HR_Application.Properties.Settings.Default.PrimaryColorBackground;
                var mainMenuColor = HR_Application.Properties.Settings.Default.MainMenuColor;
                var primaryTextBrush = HR_Application.Properties.Settings.Default.PrimaryTextBrush;
                var secondaryTextBrush = HR_Application.Properties.Settings.Default.SecondaryTextBrush;

                Application.Current.Resources["InputBackground"] = new SolidColorBrush(primaryBackground);
                Application.Current.Resources["BorderColor"] = new SolidColorBrush(mainMenuColor);
                Application.Current.Resources["TextPrimaryColor"] = new SolidColorBrush(primaryTextBrush);
                Application.Current.Resources["TextSecondaryColor"] = new SolidColorBrush(secondaryTextBrush);
            }
            catch
            {
                // fallback default لو حصل مشكلة
                Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(Colors.Blue);
                Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(Colors.Gray);
                Application.Current.Resources["ThirdColorBrush"] = new SolidColorBrush(Colors.LightGray);
            }
        }

        private void ResetColorsToThemeDefaults()
        {
            try
            {
                Application.Current.Resources.Remove("PrimaryColor");
                Application.Current.Resources.Remove("SecondaryColor");
                Application.Current.Resources.Remove("ThirdColorBrush");
                Application.Current.Resources.Remove("InputBackground");
                Application.Current.Resources.Remove("BorderColor");
                Application.Current.Resources.Remove("TextPrimaryColor");
                Application.Current.Resources.Remove("TextSecondaryColor");
            }
            catch
            {
                // ignore
            }
        }

        private void LoadServerSettings()
        {
            // قراءة الإعدادات من Properties.Settings
            ServerIP = HR_Application.Properties.Settings.Default.LastIPDB;
            SignalRPort = HR_Application.Properties.Settings.Default.SignalRPort;

            // إذا كان الإعداد فارغاً، استخدم القيم الافتراضية
            if (string.IsNullOrEmpty(ServerIP))
            {
                ServerIP = "localhost";
                SignalRPort = 7001;
            }
        }

        public static void SaveServerSettings(string ip, int port)
        {
            HR_Application.Properties.Settings.Default.LastIPDB = ip;
            HR_Application.Properties.Settings.Default.SignalRPort = port;
            HR_Application.Properties.Settings.Default.Save();

            ServerIP = ip;
            SignalRPort = port;
        }

        public static async Task InitializeSignalRAfterLogin()
        {
            await SignalRManager.Instance.InitializeAsync(CurrentUser.Id);
        }

        public static async Task<bool> TestServerConnection(string ip, int port)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, 2000);
                    if (reply.Status != IPStatus.Success)
                        return false;
                }

                // اختبار منفذ SignalR
                using (var tcpClient = new TcpClient())
                {
                    var result = tcpClient.BeginConnect(ip, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

    }

}
