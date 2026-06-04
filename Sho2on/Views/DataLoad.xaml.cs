using DocumentFormat.OpenXml.Drawing;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using HR_Application.Helpers;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for DataLoad.xaml
    /// </summary>
    public partial class DataLoad : Window
    {
        private readonly AppDbContext _context;
        private List<MachineViewModel> _machines = new List<MachineViewModel>();
        private MachineViewModel _selectedMachine;
        private List<FingerPrint> fingerPrints = new List<FingerPrint>();
        public DataLoad()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ÷»ÿ «· «—ÌŒ «·«› —«÷Ì ·»œ«Ì… «·”‰… «·Õ«·Ì…
            SetDefaultDates();

            await LoadDataAsync();
        }

        private void SetDefaultDates()
        {
            // ÷»ÿ  «—ÌŒ «·»œ¡ ·»œ«Ì… «·”‰… «·Õ«·Ì…
            var currentYear = DateTime.Now.Year;
            var startOfYear = new DateTime(currentYear, 1, 1);

            startDatePicker.SelectedDate = startOfYear;
            startDatePicker.DisplayDate = startOfYear;


        }


        private async Task LoadDataAsync()
        {
            try
            {
                ShowLoadingIndicator();

                _machines = await _context.Machines
                    .Include(m => m.Branch)
                    .Where(m => App.userBranches.Contains(m.BranchId))
                    .Select(m => new MachineViewModel
                    {
                        Id = m.Id,
                        Code = m.BranchId,
                        Branch = m.Branch.Name,
                        MIP = m.MIP,
                        SIP = m.SIP,
                    })
                    .ToListAsync();

                // Õ”«» „⁄·Ê„«  ≈÷«›Ì… ·ﬂ· ›—⁄
                foreach (var machine in _machines)
                {
                    // «·Õ’Ê· ⁄·Ï  «—ÌŒ ¬Œ—  Õ„Ì·
                    machine.LastLoadDate = await GetLastLoadDateAsync(machine.Code);

                    // «·Õ’Ê· ⁄·Ï ⁄œœ «·”Ã·« 
                    machine.RecordCount = await GetRecordCountAsync(machine.Code);
                }

                //  —ﬁÌ„ «·’›Ê›
                int rowNumber = 1;
                foreach (var machine in _machines)
                {
                    machine.RowNumber = rowNumber++;
                }

                list.ItemsSource = _machines;
                HideLoadingIndicator();
            }
            catch (Exception ex)
            {
                HideLoadingIndicator();
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<DateTime?> GetLastLoadDateAsync(int branchCode)
        {
            try
            {
                var lastLoad = await _context.MachineData
                    .Where(md => md.BranchCode == branchCode)
                    .OrderByDescending(md => md.TDate)
                    .Select(md => md.TDate)
                    .FirstOrDefaultAsync();

                return lastLoad;
            }
            catch
            {
                return null;
            }
        }

        private async Task<int> GetRecordCountAsync(int branchCode)
        {
            try
            {
                var count = await _context.MachineData
                    .CountAsync(md => md.BranchCode == branchCode);

                return count;
            }
            catch
            {
                return 0;
            }
        }

        public class MachineViewModel
        {
            public int Id { get; set; }
            public int RowNumber { get; set; }
            public int Code { get; set; }
            public string Branch { get; set; }
            public string MIP { get; set; }
            public string SIP { get; set; }
            public DateTime? LastLoadDate { get; set; }
            public int RecordCount { get; set; }
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is MachineViewModel selected)
            {
                _selectedMachine = selected;
                UpdateSelectionInfo(selected);
            }
        }

        private void UpdateSelectionInfo(MachineViewModel machine)
        {
            string info = $"«·›—⁄ «·„Œ «—: {machine.Branch} (ﬂÊœ: {machine.Code})\n";
            info += $"IP «·ÃÂ«“: {machine.MIP}\n";

            if (machine.LastLoadDate.HasValue)
            {
                info += $"¬Œ—  Õ„Ì·: {machine.LastLoadDate.Value:yyyy/MM/dd HH:mm}\n";
            }

            info += $"⁄œœ «·”Ã·« : {machine.RecordCount}";

        }

        private void exit_Clicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowLoadingIndicator()
        {
            loadingProgressBar.Visibility = Visibility.Visible;
            loadingProgressBar.Width = 50;
            loadingProgressBar.Height = 50;
            this.IsEnabled = false;
        }

        private void HideLoadingIndicator()
        {
            loadingProgressBar.Visibility = Visibility.Collapsed;
            loadingProgressBar.Width = 0;
            loadingProgressBar.Height = 0;
            this.IsEnabled = true;
        }

        private async void LoadBranch_Clicked(object sender, RoutedEventArgs e)
        {
            await LoadDataWithParameters(true);
        }


        private async Task LoadDataWithParameters(bool useDateRange)
        {
            try
            {
                // «· Õﬁﬁ „‰ «·›—⁄ «·„Œ «— ›Ì UI thread
                if (_selectedMachine == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— ›—⁄ √Ê·«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «· Õﬁﬁ „‰ ’Õ… «· Ê«—ÌŒ ›Ì UI thread
                if (useDateRange)
                {
                    if (!startDatePicker.SelectedDate.HasValue)
                    {
                        LocalizationManager.ShowMessage("Ì—ÃÏ  ÕœÌœ  «—ÌŒÌ «·»œ¡", " Õ–Ì—",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // ≈ŸÂ«— „ƒ‘— «· Õ„Ì· ›Ì UI thread
                ShowLoadingIndicator();

                // Õ›Ÿ «·ﬁÌ„ «·„Õ·Ì… ﬁ»· «·»œ¡ ›Ì Task
                var server = Properties.Settings.Default.LastIPDB;
                var ip_machine = _selectedMachine.MIP;
                var branch = _selectedMachine.Code.ToString();
                var username = "OR";
                var password = "OriginalIBS2025";
                string exePath = @"script.exe";
                DateTime? startDate = useDateRange ? startDatePicker.SelectedDate : null;

                string result = string.Empty;

                //  ‘€Ì· «·⁄„·Ì… ›Ì thread „‰›’·
                await Task.Run(() =>
                {
                    try
                    {
                        if (useDateRange && startDate.HasValue)
                        {
                            // ”Õ» «·»Ì«‰«  »‰ÿ«ﬁ “„‰Ì ›ﬁÿ
                            var startDateStr = startDate.Value.ToString("yyyy-MM-dd");
                            result = RunPythonExecutableWithDateRange(exePath, server, username, password,
                                branch, ip_machine, "4370", startDateStr);
                        }
                        else
                        {
                            // «·”Õ» «·⁄«œÌ „‰ ¬Œ—  «—ÌŒ
                            result = RunPythonExecutable(exePath, server, username, password,
                                branch, ip_machine, "4370");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = $"Œÿ√: {ex.Message}";
                    }
                });

                // ⁄—÷ «·‰ ÌÃ… ›Ì Ê«ÃÂ… «·„” Œœ„ »«” Œœ«„ Dispatcher
                await Dispatcher.InvokeAsync(() =>
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        LocalizationManager.ShowMessage(result, "‰ ÌÃ… «· Õ„Ì·",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    LocalizationManager.ShowMessage("«·—Ã«¡ «‰ Ÿ«— „⁄«·Ã… «·»Ì«‰« ...", "Ã«—Ì «·„⁄«·Ã…",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });

                // ≈œŒ«· »Ì«‰«  «·Õ÷Ê—
                await InsertAttendanceDataAsync(_selectedMachine.Code);

                //  ÕœÌÀ «·»Ì«‰«  »⁄œ «· Õ„Ì·
                await LoadDataAsync();

                // ≈Œ›«¡ „ƒ‘— «· Õ„Ì·
                HideLoadingIndicator();

                // ⁄—÷ —”«·… «·‰Ã«Õ
                await Dispatcher.InvokeAsync(() =>
                {
                    LocalizationManager.ShowMessage(" „ «·«‰ Â«¡ „‰ „⁄«·Ã… «·»Ì«‰« ", "‰Ã«Õ",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                // „⁄«·Ã… «·√Œÿ«¡ »«” Œœ«„ Dispatcher
                await Dispatcher.InvokeAsync(() =>
                {
                    HideLoadingIndicator();
                    LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public string RunPythonExecutable(string exePath, string server, string username, string password, string branch, string ipAddress, string port)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{server} {username} {password} {branch} {ipAddress} {port}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrEmpty(error) ? result : $"Œÿ√: {error}";
        }

        public string RunPythonExecutableWithDateRange(string exePath, string server, string username, string password, string branch, string ipAddress, string port, string startDate)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{server} {username} {password} {branch} {ipAddress} {port} {startDate}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrEmpty(error) ? result : $"Œÿ√: {error}";
        }

        public void ClearOldData(int branchCode)
        {
            try
            {
                // Õ–› «·»Ì«‰«  «·ﬁœÌ„… „‰ machineData ·Â–« «·›—⁄
                var oldRecords = _context.MachineData
                    .Where(md => md.BranchCode == branchCode)
                    .ToList();

                if (oldRecords.Any())
                {
                    _context.MachineData.RemoveRange(oldRecords);
                    _context.SaveChanges();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LocalizationManager.ShowMessage($" „ Õ–› {oldRecords.Count} ”Ã· ﬁœÌ„", "„⁄·Ê„…", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ–› «·»Ì«‰«  «·ﬁœÌ„…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task InsertAttendanceDataAsync(int branchCode)
        {
            try
            {
                fingerPrints.Clear();
                // «·Õ’Ê· ⁄·Ï «·»Ì«‰«  „‰ «·ÃœÊ· «·„ƒﬁ  machineData ··›—⁄ «·„Õœœ
                var machineDataList = await _context.MachineData
                    .Include(md => md.Branch)
                    .Where(md => md.BranchCode == branchCode && _context.Users.Any(u => u.Code == md.UserID.ToString()))
                    .ToListAsync();

                int recordsInserted = 0;
                int recordsSkipped = 0;

                foreach (var machineData in machineDataList)
                {
                    try
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Code == machineData.UserID.ToString() && u.BranchId == machineData.BranchCode);

                        if (user == null)
                        {
                            //  ŒÿÌ ≈–« ·„ Ì „ «·⁄ÀÊ— ⁄·Ï «·„” Œœ„
                            recordsSkipped++;
                            continue;
                        }
                        // «· Õﬁﬁ „‰ ⁄œ„ ÊÃÊœ «· ”ÃÌ· „”»ﬁ« ›Ì FingerPrint
                        var existingRecord = await _context.FingerPrints
                            .FirstOrDefaultAsync(fp =>
                                fp.UserId == user.Id &&
                                fp.FingerPrintDate == machineData.TDate &&
                                fp.BranchId == machineData.BranchCode &&
                                fp.Status == machineData.StatusNo);

                        if (existingRecord == null)
                        {
                            // ≈‰‘«¡ ”Ã· ÃœÌœ ›Ì FingerPrint
                            var fingerPrint = new FingerPrint
                            {
                                UserId = user.Id,
                                FingerPrintDate = machineData.TDate,
                                Status = machineData.StatusNo,
                                BranchId = machineData.BranchCode,
                                MachineId = await GetMachineIdByIPAsync(machineData.MIP),
                            };

                            await _context.FingerPrints.AddAsync(fingerPrint);
                            fingerPrints.Add(fingerPrint);
                            recordsInserted++;
                        }
                        else
                        {
                            recordsSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LocalizationManager.ShowMessage($"Œÿ√ ›Ì „⁄«·Ã… ”Ã· ··„” Œœ„ {machineData.UserID}: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        continue;
                    }
                }

                await _context.SaveChangesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string message = $" „ ≈œŒ«· {recordsInserted} ”Ã· ÃœÌœ\n";
                    if (recordsSkipped > 0)
                    {
                        message += $"{recordsSkipped} ”Ã·  „  ŒÿÌÂ („ÊÃÊœ „”»ﬁ«)";
                    }

                    LocalizationManager.ShowMessage(message, "‰ ÌÃ… «·≈œŒ«·", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                HideLoadingIndicator();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì ≈œŒ«· «·»Ì«‰« : {ex.InnerException.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task<int?> GetMachineIdByIPAsync(string mip)
        {
            var machine = await _context.Machines
                .FirstOrDefaultAsync(m => m.MIP == mip);

            return machine?.Id;
        }


    }
}
