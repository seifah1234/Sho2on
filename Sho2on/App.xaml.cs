using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        public static User CurrentUser { get; set; }
        public static IServiceProvider ServiceProvider { get; private set; }
        public static HubConnection SignalRConnection { get; set; }
        public static string ServerIP { get; private set; }
        public static int SignalRPort { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {


            base.OnStartup(e);
            LoadServerSettings();
            InitializeSignalR();
            // Set the culture to English
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;



        }
        public static string ConnectionString { get; set; }
        public static string SoftechConnectionString { get; set; }
        public static string SoftechSQLConnectionString { get; set; }
        public static List<string> userPermissions { get; set; }
        public static List<int> userBranches { get; set; }

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

        private async void InitializeSignalR()
        {
            try
            {
                // بناء URL مع userId في Query String
                var url = $"http://{ServerIP}:7001/chatHub";

                if (CurrentUser != null && CurrentUser.Id > 0)
                {
                    url += $"?userId={CurrentUser.Id}";
                }


                SignalRConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();

                // تسجيل الـ listener قبل الاتصال
                SignalRConnection.On<int, int, string, DateTime>("ReceiveMessage",
                    (fromUserId, toUserId, message, timestamp) =>
                    {
                        // معالجة الرسالة...
                    });

                await SignalRConnection.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SignalR connection failed: {ex.Message}");
            }
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
