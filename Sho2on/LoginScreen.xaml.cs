using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Sho2on.Database;
using Sho2on.Database.Models;
using Syncfusion.Windows.Shared;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class LoginScreen : Window
    {
        private AppDbContext _context;
        private readonly AsyncRetryPolicy _retryPolicy;

        public LoginScreen()
        {
            InitializeComponent();

            // إنشاء سياسة إعادة المحاولة
            _retryPolicy = Policy
                .Handle<SqlException>(ex => IsTransientError(ex))
                .Or<SocketException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        // تسجيل المحاولات (يمكنك استبدالها بـ Log)
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.Seconds} seconds due to: {exception.Message}");
                    });

            LoadGIFAsync();
            IPDB_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            username_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            IPDB_box.Text = Properties.Settings.Default.LastIPDB;
        }

        private bool IsTransientError(SqlException ex)
        {
            // أرقام الأخطاء العابرة في SQL Server
            int[] transientErrorNumbers = {
                4060, 10928, 10929, 40197, 40501,
                40613, 49918, 49919, 49920, 11001
            };

            return transientErrorNumbers.Contains(ex.Number);
        }

        private async void Full()
        {
            await LogInAsync();
        }

        private async Task LogInAsync()
        {
            if (string.IsNullOrWhiteSpace(IPDB_box.Text) ||
                string.IsNullOrWhiteSpace(username_box.Text) ||
                string.IsNullOrWhiteSpace(pass_box.Password))
            {
                MessageBox.Show("Please fill in all required fields.");
                return;
            }

            string username = username_box.Text;
            string password = pass_box.Password;
            string ip = IPDB_box.Text;

            // تحسين Connection String
            string connectionString = BuildConnectionString(ip);

            try
            {
                // اختبار الاتصال أولاً قبل إنشاء Context
                bool canConnect = await TestConnectionAsync(connectionString);

                if (!canConnect)
                {
                    MessageBox.Show("لا يمكن الاتصال بقاعدة البيانات. تأكد من إعدادات الخادم.");
                    return;
                }

                // استخدام Retry Policy
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    App.ConnectionString = connectionString;
                    App.ServerIP = ip;
                    _context = new AppDbContext(App.ConnectionString);

                    // اختبار الاتصال بالـ Context
                    await _context.Database.CanConnectAsync();
                });

                // استمرار عملية تسجيل الدخول
                await ProcessLoginAsync(username, password);
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"خطأ في قاعدة البيانات: {sqlEx.Message}\nرقم الخطأ: {sqlEx.Number}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private string BuildConnectionString(string ip)
        {
            return $"Server={ip},1433;Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";
            //return $"Server={ip},1433;Initial Catalog=Original;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;";
        }

        private async Task<string> TestMultipleConnections(string ip)
        {
            // إضافة خيارات إضافية لـ Windows Server 2016
            var connections = new[]
            {
        // بدون Port
        $"Server={ip};Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;",
        
        // مع Port 1433
        $"Server={ip},1433;Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;",
        
        // مع Instance Name (جرب أسماء الـ Instance المختلفة)
        $"Server={ip}\\MSSQLSERVER;Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;",
        $"Server={ip}\\SQLEXPRESS;Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;",
        
        // مع Network Library
        $"Data Source={ip};Network Library=DBMSSOCN;Initial Catalog=Sho2onDB;User ID=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Connection Timeout=60;",
        
        // خيارات متقدمة
        $"Server={ip};Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;Persist Security Info=True;Connection Timeout=60;",
        
        // للخوادم البعيدة (ممكن تحتاج MultiSubnetFailover)
        $"Server={ip};Database=Sho2onDB;User Id=OR;Password=OriginalIBS2025;TrustServerCertificate=True;MultiSubnetFailover=True;Connection Timeout=60;"
    };

            var successfulConnections = new List<string>();

            foreach (var connString in connections)
            {
                try
                {
                    using (var connection = new SqlConnection(connString))
                    {
                        await connection.OpenAsync();

                        // اختبار استعلام بسيط للتأكد من العملية
                        using (var cmd = new SqlCommand("SELECT @@VERSION", connection))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            Console.WriteLine($"SQL Server Version: {result}");
                        }

                        await connection.CloseAsync();
                        Console.WriteLine($"✓ نجاح مع: {connString}");
                        successfulConnections.Add(connString);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ فشل مع: {connString}");
                    Console.WriteLine($"  السبب: {ex.Message}");
                }
            }

            // اختيار أفضل Connection String
            if (successfulConnections.Any())
            {
                // تفضيل الاتصالات بدون Port (أكثر استقراراً عادة)
                var preferred = successfulConnections.FirstOrDefault(c =>
                    !c.Contains(",1433") && !c.Contains("\\MSSQLSERVER") && !c.Contains("\\SQLEXPRESS"));

                return preferred ?? successfulConnections.First();
            }

            return null;
        }
        private async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    await connection.CloseAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private async Task ProcessLoginAsync(string username, string password)
        {
            try
            {
                if (username == "admin" && password == "admin")
                {
                    await ProcessAdminLoginAsync();
                    return;
                }

                await ProcessUserLoginAsync(username, password);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في عملية تسجيل الدخول: {ex.Message}", ex);
            }
        }

        private async Task ProcessAdminLoginAsync()
        {
            // Open Main Window for admin
            App.userPermissions = new List<string>();
            var permissions = await _context.Permissions
                .Select(rp => rp.PermissionName)
                .ToListAsync();

            App.userPermissions.AddRange(permissions);

            Properties.Settings.Default.LastUsername = "admin";
            Properties.Settings.Default.LastPassword = "admin";
            Properties.Settings.Default.Save();

            OpenMainWindow();
        }

         public static string ComputeSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private async Task ProcessUserLoginAsync(string username, string password)
        {
            // Get user from DB
            var passwordHash = password;

            var user = await _context.Users
                .Include(u => u.JobTitle)
                .Include(u => u.Manager)
                .ThenInclude(m => m.JobTitle)
                .Include(u => u.Manager)
                .ThenInclude(m => m.Department)
                .Include(u => u.Department)
                .Where(u => u.Username == username && u.PasswordHash == passwordHash)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                MessageBox.Show("Invalid username or password.");
                return;
            }

            App.CurrentUser = user;

            // Load Roles & Permissions باستخدام Retry Policy
            await LoadUserPermissionsAndBranchesAsync(user.Id);

            // Save settings
            Properties.Settings.Default.LastUsername = username;
            Properties.Settings.Default.LastPassword = password;
            Properties.Settings.Default.LastIPDB = IPDB_box.Text;
            Properties.Settings.Default.Save();

            OpenMainWindow();
        }

        private async Task LoadUserPermissionsAndBranchesAsync(int userId)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    // Load Roles
                    var roles = await _context.UserRoles
                        .Where(ur => ur.UserId == userId)
                        .Select(ur => ur.Role)
                        .ToListAsync();

                    App.userPermissions = new List<string>();

                    // Load Branches
                    App.userBranches = await _context.UserBranches
                        .Where(ub => ub.UserID == userId)
                        .Select(ub => ub.BranchId)
                        .ToListAsync();

                    // Load Permissions لكل Role
                    foreach (var role in roles)
                    {
                        Properties.Settings.Default.UserRole = role.RoleName;

                        var permissions = await _context.RolePermissions
                            .Where(rp => rp.RoleID == role.RoleID)
                            .Select(rp => rp.Permission.PermissionName)
                            .ToListAsync();

                        App.userPermissions.AddRange(permissions);
                    }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحميل صلاحيات المستخدم: {ex.Message}", ex);
            }
        }

        private void OpenMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private async Task LoadGIFAsync()
        {
            await Task.Delay(500);

            await Dispatcher.InvokeAsync(() =>
            {
                var gifUri = new Uri("pack://application:,,,/assets/images/Background.gif");
                var gifImage = new BitmapImage(gifUri);
                ImageBehavior.SetAnimatedSource(GIFImage, gifImage);
            });
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Min_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private async void login_Clicked(object sender, RoutedEventArgs e) => await LogInAsync();

        private void btn_mouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            login_btn.FontSize = 16;
            btn_border.Width += 2;
            btn_border.Height += 2;
            btn_border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#598bff"));
            login_btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void btn_mouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            login_btn.FontSize = 14;
            btn_border.Width -= 2;
            btn_border.Height -= 2;
            btn_border.Background = new SolidColorBrush(Colors.White);
            login_btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#598bff"));
        }
    }
}