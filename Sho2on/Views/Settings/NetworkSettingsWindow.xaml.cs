using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows; using HR_Application.Helpers;
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
                //  Õ„Ì· «·≈⁄œ«œ«  «·Õ«·Ì… „‰ ﬁ«⁄œ… «·»Ì«‰« 
                var setting = await _context.Settings
                    .FirstOrDefaultAsync();

                if (setting != null)
                {
                    networkPathTextBox.Text = setting.CentralDocumentStoragePath;
                }
                else
                {
                    // «” Œœ«„ «·„”«— «·«› —«÷Ì
                    networkPathTextBox.Text = AppDbContext.CentralStoragePath;
                }

                //  ÕœÌÀ «·≈Õ’«∆Ì« 
                await UpdateStatistics();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·≈⁄œ«œ« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void testNetworkBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = networkPathTextBox.Text.Trim();

            if (string.IsNullOrEmpty(path))
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· „”«— ’ÕÌÕ", " Õ–Ì—",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                testNetworkBtn.IsEnabled = false;
                networkStatusText.Text = "Ã«—Ì «Œ »«— «·« ’«·...";

                bool isAccessible = await Task.Run(() => TestNetworkPath(path));

                if (isAccessible)
                {
                    networkStatusText.Text = "? «·« ’«· ‰«ÃÕ - «·„”«— „ «Õ";
                    networkStatusText.Foreground = System.Windows.Media.Brushes.Green;

                    // «Œ »«— «·ﬂ «»…
                    bool canWrite = await TestWriteAccess(path);
                    if (canWrite)
                    {
                        networkStatusText.Text += " - ’·«ÕÌ«  «·ﬂ «»… „ «Õ…";
                    }
                    else
                    {
                        networkStatusText.Text += " -  Õ–Ì—: ·«  ÊÃœ ’·«ÕÌ«  ﬂ «»…";
                        networkStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                }
                else
                {
                    networkStatusText.Text = "? «·« ’«· ›«‘· - «·„”«— €Ì— „ «Õ";
                    networkStatusText.Foreground = System.Windows.Media.Brushes.Red;

                    // «ﬁ —«Õ« 
                    ShowConnectionSuggestions(path);
                }
            }
            catch (Exception ex)
            {
                networkStatusText.Text = $"? Œÿ√: {ex.Message}";
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
                    // «” Œ—«Ã «”„ «·”Ì—›—
                    string serverName = path.Substring(2).Split('\\')[0];

                    // «Œ »«— Ping
                    if (!PingServer(serverName))
                    {
                        return false;
                    }

                    // «Œ »«— ÊÃÊœ «·„Ã·œ
                    if (!Directory.Exists(path))
                    {
                        // „Õ«Ê·… ≈‰‘«¡ «·„Ã·œ
                        try
                        {
                            Directory.CreateDirectory(path);
                            return true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // ·«  ÊÃœ ’·«ÕÌ«  ··≈‰‘«¡° Ê·ﬂ‰ «·„Ã·œ ﬁœ ÌﬂÊ‰ „ÊÃÊœ«
                            return Directory.Exists(Path.GetDirectoryName(path));
                        }
                    }

                    return true;
                }
                else
                {
                    // „”«— „Õ·Ì
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

            string suggestions = "«ﬁ —«Õ«  ·Õ· «·„‘ﬂ·…:\n\n";

            if (failedPath.StartsWith(@"\\"))
            {
                string server = failedPath.Substring(2).Split('\\')[0];

                suggestions += $"1.  √ﬂœ „‰  ‘€Ì· ÃÂ«“ «·”Ì—›— ({server})\n";
                suggestions += $"2.  Õﬁﬁ „‰ « ’«· «·‘»ﬂ… »Ì‰ «·√ÃÂ“…\n";
                suggestions += $"3.  √ﬂœ „‰  ›⁄Ì· „‘«—ﬂ… «·„·›«  ⁄·Ï «·”Ì—›—\n";
                suggestions += $"4.  Õﬁﬁ „‰ ≈⁄œ«œ«  Ãœ«— «·Õ„«Ì… ⁄·Ï «·”Ì—›—\n";
                suggestions += $"5. Ã—» «” Œœ«„ IP ¬Œ— √Ê «”„ «·”Ì—›—\n\n";
                suggestions += $"IPs «·„ «Õ… ›Ì «·‘»ﬂ…:\n";

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
                    suggestions += "   €Ì— ﬁ«œ— ⁄·Ï «ﬂ ‘«› IPs «·‘»ﬂ…\n";
                }
            }

            suggestions += "\n6. Ì„ﬂ‰ﬂ «” Œœ«„ «·„”«— «·„Õ·Ì „ƒﬁ «";

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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·Õ’Ê· ⁄·Ï IP «·„Õ·Ì: {ex.Message}", "Œÿ√",
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
            throw new Exception("·« ÌÊÃœ ⁄‰Ê«‰ IP „Õ·Ì");
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
                    ShowTestResult($"? «·”Ì—›— {server} Ì” ÃÌ» ·‹ Ping", true);
                }
                else
                {
                    ShowTestResult($"? «·”Ì—›— {server} ·« Ì” ÃÌ» ·‹ Ping", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"? Œÿ√ ›Ì «Œ »«— Ping: {ex.Message}", false);
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
                    ShowTestResult($"? «·„Ã·œ {path} „ «Õ ··Ê’Ê·", true);
                }
                else
                {
                    ShowTestResult($"? ·« Ì„ﬂ‰ «·Ê’Ê· ≈·Ï «·„Ã·œ {path}", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"? Œÿ√ ›Ì «Œ »«— «·„Ã·œ: {ex.Message}", false);
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
                    ShowTestResult("? ’·«ÕÌ«  «·ﬂ «»… „ «Õ…", true);
                }
                else
                {
                    ShowTestResult("? ·«  ÊÃœ ’·«ÕÌ«  ﬂ «»… ⁄·Ï «·„Ã·œ", false);
                }
            }
            catch (Exception ex)
            {
                ShowTestResult($"? Œÿ√ ›Ì «Œ »«— «·’·«ÕÌ« : {ex.Message}", false);
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

                // Õ«·… «·« ’«·
                bool isConnected = TestNetworkPath(currentPath);
                connectionStatusText.Text = isConnected ? "„ ’· ?" : "€Ì— „ ’· ?";
                connectionStatusText.Foreground = isConnected ?
                    System.Windows.Media.Brushes.Green :
                    System.Windows.Media.Brushes.Red;

                // «·„”«Õ… «·„ «Õ…
                if (Directory.Exists(currentPath))
                {
                    var driveInfo = new DriveInfo(Path.GetPathRoot(currentPath));
                    long freeSpaceGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                    long totalSpaceGB = driveInfo.TotalSize / (1024 * 1024 * 1024);

                    freeSpaceText.Text = $"{freeSpaceGB} GB „‰ √’· {totalSpaceGB} GB";
                }
                else
                {
                    freeSpaceText.Text = "€Ì— „ «Õ";
                }
            }
            catch
            {
                currentPathText.Text = "€Ì— ﬁ«œ— ⁄·Ï  ÕœÌœ «·„”«—";
                connectionStatusText.Text = "Œÿ√ ›Ì «·« ’«·";
                freeSpaceText.Text = "€Ì— „ «Õ";
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

                LocalizationManager.ShowMessage(" „ ≈‰‘«¡ «·„Ã·œ«  «·ÂÌﬂ·Ì… »‰Ã«Õ", "‰Ã«Õ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ≈‰‘«¡ «·„Ã·œ« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void migrateFilesBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = LocalizationManager.ShowMessage("Â·  —Ìœ ‰ﬁ· «·„·›«  «·Õ«·Ì… ≈·Ï «·„”«— «·ÃœÌœø",
                " √ﬂÌœ «·‰ﬁ·", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                migrateFilesBtn.IsEnabled = false;

                // ‰ﬁ· „·›«  «·‘—ﬂ…
                await MigrateFolder("CompanyDocuments", DocumentType.Company);

                // ‰ﬁ· „·›«  «·„ÊŸ›Ì‰
                await MigrateFolder("EmployeeDocuments", DocumentType.Employee);

                // ‰ﬁ· «·„·›«  «·„Êﬁ⁄…
                await MigrateFolder("SignedDocuments", DocumentType.Signed);

                LocalizationManager.ShowMessage(" „ ‰ﬁ· «·„·›«  »‰Ã«Õ", "‰Ã«Õ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ‰ﬁ· «·„·›« : {ex.Message}", "Œÿ√",
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

            // ≈‰‘«¡ «·„Ã·œ «·ÃœÌœ
            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            // ‰ﬁ· «·„·›« 
            var files = Directory.GetFiles(localPath);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(newPath, fileName);

                File.Copy(file, destFile, true);

                //  ÕœÌÀ ﬁ«⁄œ… «·»Ì«‰« 
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
                //  Ã«Â· «·√Œÿ«¡ ›Ì «· ÕœÌÀ
            }
        }

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPath = networkPathTextBox.Text.Trim();

                if (string.IsNullOrEmpty(newPath))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· „”«— ’ÕÌÕ", " Õ–Ì—",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «Œ »«— «·„”«—
                if (!TestNetworkPath(newPath))
                {
                    LocalizationManager.ShowMessage("«·„”«— €Ì— „ «Õ. Ì—ÃÏ «· Õﬁﬁ „‰ «·« ’«·.", "Œÿ√",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Õ›Ÿ ›Ì ﬁ«⁄œ… «·»Ì«‰« 
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

                LocalizationManager.ShowMessage(" „ Õ›Ÿ «·≈⁄œ«œ«  »‰Ã«Õ", "‰Ã«Õ",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ «·≈⁄œ«œ« : {ex.Message}", "Œÿ√",
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
