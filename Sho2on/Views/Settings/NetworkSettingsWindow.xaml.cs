using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;

namespace HR_Application.Views.Settings
{
    public partial class NetworkSettingsWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);

        public NetworkSettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private async void LoadCurrentSettings()
        {
            try
            {
                // تحميل الإعدادات الحالية من قاعدة البيانات
                var setting = await _context.Settings
                    .FirstOrDefaultAsync();

                if (setting != null)
                {
                    networkPathTextBox.Text = setting.CentralDocumentStoragePath;
                }
                else
                {
                    // استخدام المسار الافتراضي
                    networkPathTextBox.Text = AppDbContext.CentralStoragePath;
                }

                // تحديث الإحصائيات
                await UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الإعدادات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void testNetworkBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = networkPathTextBox.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("يرجى إدخال مسار صحيح", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                testNetworkBtn.IsEnabled = false;
                networkStatusText.Text = "جاري اختبار الاتصال...";

                bool isAccessible = await Task.Run(() => TestNetworkPath(path));

                if (isAccessible)
                {
                    networkStatusText.Text = "✓ الاتصال ناجح - المسار متاح";
                    networkStatusText.Foreground = System.Windows.Media.Brushes.Green;

                    // اختبار الكتابة
                    bool canWrite = await TestWriteAccess(path);
                    if (canWrite)
                    {
                        networkStatusText.Text += " - صلاحيات الكتابة متاحة";
                    }
                    else
                    {
                        networkStatusText.Text += " - تحذير: لا توجد صلاحيات كتابة";
                        networkStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                }
                else
                {
                    networkStatusText.Text = "✗ الاتصال فاشل - المسار غير متاح";
                    networkStatusText.Foreground = System.Windows.Media.Brushes.Red;

                    // اقتراحات
                    ShowConnectionSuggestions(path);
                }
            }
            catch (Exception ex)
            {
                networkStatusText.Text = $"✗ خطأ: {ex.Message}";
                networkStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                testNetworkBtn.IsEnabled = true;
            }
        }

        private bool TestNetworkPath(string path)
        {
            try
            {
                if (path.StartsWith(@"\\"))
                {
                    // استخراج اسم السيرفر
                    string serverName = path.Substring(2).Split('\\')[0];

                    // اختبار Ping
                    if (!PingServer(serverName))
                    {
                        return false;
                    }

                    // اختبار وجود المجلد
                    if (!Directory.Exists(path))
                    {
                        // محاولة إنشاء المجلد
                        try
                        {
                            Directory.CreateDirectory(path);
                            return true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // لا توجد صلاحيات للإنشاء، ولكن المجلد قد يكون موجوداً
                            return Directory.Exists(Path.GetDirectoryName(path));
                        }
                    }

                    return true;
                }
                else
                {
                    // مسار محلي
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool PingServer(string serverNameOrIP)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(serverNameOrIP, 3000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestWriteAccess(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string testFile = Path.Combine(path, "test_write.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private void ShowConnectionSuggestions(string failedPath)
        {
            testResultsBorder.Visibility = Visibility.Visible;

            string suggestions = "اقتراحات لحل المشكلة:\n\n";

            if (failedPath.StartsWith(@"\\"))
            {
                string server = failedPath.Substring(2).Split('\\')[0];

                suggestions += $"1. تأكد من تشغيل جهاز السيرفر ({server})\n";
                suggestions += $"2. تحقق من اتصال الشبكة بين الأجهزة\n";
                suggestions += $"3. تأكد من تفعيل مشاركة الملفات على السيرفر\n";
                suggestions += $"4. تحقق من إعدادات جدار الحماية على السيرفر\n";
                suggestions += $"5. جرب استخدام IP آخر أو اسم السيرفر\n\n";
                suggestions += $"IPs المتاحة في الشبكة:\n";

                try
                {
                    var localIPs = GetLocalNetworkIPs();
                    foreach (var ip in localIPs)
                    {
                        suggestions += $"   - \\\\{ip}\\HR_Documents\n";
                    }
                }
                catch
                {
                    suggestions += "   غير قادر على اكتشاف IPs الشبكة\n";
                }
            }

            suggestions += "\n6. يمكنك استخدام المسار المحلي مؤقتاً";

            testResultsText.Text = suggestions;
        }

        private string[] GetLocalNetworkIPs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();
        }

        private void btnLocalIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string localIP = GetLocalIPAddress();
                networkPathTextBox.Text = $@"\\{localIP}\HR_Documents";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحصول على IP المحلي: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("لا يوجد عنوان IP محلي");
        }

        private void btnServerName_Click(object sender, RoutedEventArgs e)
        {
            networkPathTextBox.Text = @"\\SERVER\HR_Documents";
        }

        private void btnLocalPath_Click(object sender, RoutedEventArgs e)
        {
            networkPathTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                "HR_Documents");
        }

        private async void pingTestBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = networkPathTextBox.Text;
            if (!path.StartsWith(@"\\")) return;

            string server = path.Substring(2).Split('\\')[0];

            try
            {
                pingTestBtn.IsEnabled = false;
                bool success = await Task.Run(() => PingServer(server));

                if (success)
                {
                    ShowTestResult($"✓ السيرفر {server} يستجيب لـ Ping", true);
                }
                else
                {
                    ShowTestResult($"✗ السيرفر {server} لا يستجيب لـ Ping", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"✗ خطأ في اختبار Ping: {ex.Message}", false);
            }
            finally
            {
                pingTestBtn.IsEnabled = true;
            }
        }

        private async void shareTestBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = networkPathTextBox.Text;

            try
            {
                shareTestBtn.IsEnabled = false;
                bool success = await Task.Run(() => TestNetworkPath(path));

                if (success)
                {
                    ShowTestResult($"✓ المجلد {path} متاح للوصول", true);
                }
                else
                {
                    ShowTestResult($"✗ لا يمكن الوصول إلى المجلد {path}", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"✗ خطأ في اختبار المجلد: {ex.Message}", false);
            }
            finally
            {
                shareTestBtn.IsEnabled = true;
            }
        }

        private async void permissionTestBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = networkPathTextBox.Text;

            try
            {
                permissionTestBtn.IsEnabled = false;
                bool canWrite = await TestWriteAccess(path);

                if (canWrite)
                {
                    ShowTestResult("✓ صلاحيات الكتابة متاحة", true);
                }
                else
                {
                    ShowTestResult("✗ لا توجد صلاحيات كتابة على المجلد", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"✗ خطأ في اختبار الصلاحيات: {ex.Message}", false);
            }
            finally
            {
                permissionTestBtn.IsEnabled = true;
            }
        }

        private void ShowTestResult(string message, bool isSuccess)
        {
            testResultsBorder.Visibility = Visibility.Visible;
            testResultsText.Text = message;
            testResultsBorder.Background = isSuccess ?
                System.Windows.Media.Brushes.LightGreen :
                System.Windows.Media.Brushes.LightPink;
        }

        private async Task UpdateStatistics()
        {
            try
            {
                string currentPath = AppDbContext.CentralStoragePath;
                currentPathText.Text = currentPath;

                // حالة الاتصال
                bool isConnected = TestNetworkPath(currentPath);
                connectionStatusText.Text = isConnected ? "متصل ✓" : "غير متصل ✗";
                connectionStatusText.Foreground = isConnected ?
                    System.Windows.Media.Brushes.Green :
                    System.Windows.Media.Brushes.Red;

                // المساحة المتاحة
                if (Directory.Exists(currentPath))
                {
                    var driveInfo = new DriveInfo(Path.GetPathRoot(currentPath));
                    long freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                    long totalSpaceGB = driveInfo.TotalSize / (1024 * 1024 * 1024);

                    freeSpaceText.Text = $"{freeSpaceGB} GB من أصل {totalSpaceGB} GB";
                }
                else
                {
                    freeSpaceText.Text = "غير متاح";
                }
            }
            catch
            {
                currentPathText.Text = "غير قادر على تحديد المسار";
                connectionStatusText.Text = "خطأ في الاتصال";
                freeSpaceText.Text = "غير متاح";
            }
        }

        private async void loadStatsBtn_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStatistics();
        }

        private void initFoldersBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string basePath = networkPathTextBox.Text;

                string[] folders = {
                    "CompanyDocuments",
                    "EmployeeDocuments",
                    "SignedDocuments",
                    "OtherDocuments",
                    "Backups",
                    "Temp"
                };

                foreach (string folder in folders)
                {
                    string fullPath = Path.Combine(basePath, folder);
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                }

                MessageBox.Show("تم إنشاء المجلدات الهيكلية بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء المجلدات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void migrateFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل تريد نقل الملفات الحالية إلى المسار الجديد؟",
                "تأكيد النقل", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                migrateFilesBtn.IsEnabled = false;

                // نقل ملفات الشركة
                await MigrateFolder("CompanyDocuments", DocumentType.Company);

                // نقل ملفات الموظفين
                await MigrateFolder("EmployeeDocuments", DocumentType.Employee);

                // نقل الملفات الموقعة
                await MigrateFolder("SignedDocuments", DocumentType.Signed);

                MessageBox.Show("تم نقل الملفات بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في نقل الملفات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                migrateFilesBtn.IsEnabled = true;
            }
        }

        private async Task MigrateFolder(string folderName, DocumentType docType)
        {
            string localPath = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            string newPath = Path.Combine(networkPathTextBox.Text, folderName);

            if (!Directory.Exists(localPath)) return;

            // إنشاء المجلد الجديد
            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            // نقل الملفات
            var files = Directory.GetFiles(localPath);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(newPath, fileName);

                File.Copy(file, destFile, true);

                // تحديث قاعدة البيانات
                await UpdateDocumentPath(fileName, docType, destFile);
            }
        }

        private async Task UpdateDocumentPath(string fileName, DocumentType docType, string newPath)
        {
            try
            {
                if (docType == DocumentType.Company)
                {
                    var document = await _context.CompanyDocuments
                        .FirstOrDefaultAsync(d => d.FileName == fileName);

                    if (document != null)
                    {
                        document.FullPath = newPath;
                        document.StorageType = "Central";
                    }
                }
                else if (docType == DocumentType.Employee || docType == DocumentType.Signed)
                {
                    var document = await _context.EmployeeDocuments
                        .FirstOrDefaultAsync(d => d.FileName == fileName);

                    if (document != null)
                    {
                        document.FullPath = newPath;
                        document.StorageType = "Central";
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                // تجاهل الأخطاء في التحديث
            }
        }

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPath = networkPathTextBox.Text.Trim();

                if (string.IsNullOrEmpty(newPath))
                {
                    MessageBox.Show("يرجى إدخال مسار صحيح", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // اختبار المسار
                if (!TestNetworkPath(newPath))
                {
                    MessageBox.Show("المسار غير متاح. يرجى التحقق من الاتصال.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // حفظ في قاعدة البيانات
                var setting = await _context.Settings
                    .FirstOrDefaultAsync();

                if (setting == null)
                {
                    setting = new Setting
                    {
                        CentralDocumentStoragePath = newPath,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Settings.Add(setting);
                }
                else
                {
                    setting.CentralDocumentStoragePath = newPath;
                    setting.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ الإعدادات بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}