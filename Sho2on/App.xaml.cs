using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {

        public static User CurrentUser { get; set; }
        public static IServiceProvider ServiceProvider { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {


            base.OnStartup(e);

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

    }

}
