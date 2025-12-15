using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public DataLoad()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ضبط التاريخ الافتراضي لبداية السنة الحالية
            SetDefaultDates();

            await LoadDataAsync();
        }

        private void SetDefaultDates()
        {
            // ضبط تاريخ البدء لبداية السنة الحالية
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

                // حساب معلومات إضافية لكل فرع
                foreach (var machine in _machines)
                {
                    // الحصول على تاريخ آخر تحميل
                    machine.LastLoadDate = await GetLastLoadDateAsync(machine.Code);

                    // الحصول على عدد السجلات
                    machine.RecordCount = await GetRecordCountAsync(machine.Code);
                }

                // ترقيم الصفوف
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
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
            string info = $"الفرع المختار: {machine.Branch} (كود: {machine.Code})\n";
            info += $"IP الجهاز: {machine.MIP}\n";

            if (machine.LastLoadDate.HasValue)
            {
                info += $"آخر تحميل: {machine.LastLoadDate.Value:yyyy/MM/dd HH:mm}\n";
            }

            info += $"عدد السجلات: {machine.RecordCount}";

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
                // التحقق من الفرع المختار في UI thread
                if (_selectedMachine == null)
                {
                    MessageBox.Show("يرجى اختيار فرع أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من صحة التواريخ في UI thread
                if (useDateRange)
                {
                    if (!startDatePicker.SelectedDate.HasValue)
                    {
                        MessageBox.Show("يرجى تحديد تاريخي البدء", "تحذير",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // إظهار مؤشر التحميل في UI thread
                ShowLoadingIndicator();

                // حفظ القيم المحلية قبل البدء في Task
                var server = Properties.Settings.Default.LastIPDB;
                var ip_machine = _selectedMachine.MIP;
                var branch = _selectedMachine.Code.ToString();
                var username = "OR";
                var password = "OriginalIBS2025";
                string exePath = @"script.exe";
                DateTime? startDate = useDateRange ? startDatePicker.SelectedDate : null;

                string result = string.Empty;

                // تشغيل العملية في thread منفصل
                await Task.Run(() =>
                {
                    try
                    {
                        if (useDateRange && startDate.HasValue)
                        {
                            // سحب البيانات بنطاق زمني فقط
                            var startDateStr = startDate.Value.ToString("yyyy-MM-dd");
                            result = RunPythonExecutableWithDateRange(exePath, server, username, password,
                                branch, ip_machine, "4370", startDateStr);
                        }
                        else
                        {
                            // السحب العادي من آخر تاريخ
                            result = RunPythonExecutable(exePath, server, username, password,
                                branch, ip_machine, "4370");
                        }
                    }
                    catch (Exception ex)
                    {
                        result = $"خطأ: {ex.Message}";
                    }
                });

                // عرض النتيجة في واجهة المستخدم باستخدام Dispatcher
                await Dispatcher.InvokeAsync(() =>
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        MessageBox.Show(result, "نتيجة التحميل",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    MessageBox.Show("الرجاء انتظار معالجة البيانات...", "جاري المعالجة",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });

                // إدخال بيانات الحضور
                await InsertAttendanceDataAsync(_selectedMachine.Code);

                // تحديث البيانات بعد التحميل
                await LoadDataAsync();

                // إخفاء مؤشر التحميل
                HideLoadingIndicator();

                // عرض رسالة النجاح
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("تم الانتهاء من معالجة البيانات", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                // معالجة الأخطاء باستخدام Dispatcher
                await Dispatcher.InvokeAsync(() =>
                {
                    HideLoadingIndicator();
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
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

            return string.IsNullOrEmpty(error) ? result : $"خطأ: {error}";
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

            return string.IsNullOrEmpty(error) ? result : $"خطأ: {error}";
        }

        public void ClearOldData(int branchCode)
        {
            try
            {
                // حذف البيانات القديمة من machineData لهذا الفرع
                var oldRecords = _context.MachineData
                    .Where(md => md.BranchCode == branchCode)
                    .ToList();

                if (oldRecords.Any())
                {
                    _context.MachineData.RemoveRange(oldRecords);
                    _context.SaveChanges();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"تم حذف {oldRecords.Count} سجل قديم", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في حذف البيانات القديمة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task InsertAttendanceDataAsync(int branchCode)
        {
            try
            {
                // الحصول على البيانات من الجدول المؤقت machineData للفرع المحدد
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
                            // تخطي إذا لم يتم العثور على المستخدم
                            recordsSkipped++;
                            continue;
                        }
                        // التحقق من عدم وجود التسجيل مسبقاً في FingerPrint
                        var existingRecord = await _context.FingerPrints
                            .FirstOrDefaultAsync(fp =>
                                fp.UserId == user.Id &&
                                fp.FingerPrintDate == machineData.TDate &&
                                fp.BranchId == machineData.BranchCode &&
                                fp.Status == machineData.StatusNo);

                        if (existingRecord == null)
                        {
                            // إنشاء سجل جديد في FingerPrint
                            var fingerPrint = new FingerPrint
                            {
                                UserId = user.Id,
                                FingerPrintDate = machineData.TDate,
                                Status = machineData.StatusNo,
                                BranchId = machineData.BranchCode,
                                MachineId = await GetMachineIdByIPAsync(machineData.MIP),
                            };

                            await _context.FingerPrints.AddAsync(fingerPrint);
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
                            MessageBox.Show($"خطأ في معالجة سجل للمستخدم {machineData.UserID}: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        continue;
                    }
                }

                await _context.SaveChangesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string message = $"تم إدخال {recordsInserted} سجل جديد\n";
                    if (recordsSkipped > 0)
                    {
                        message += $"{recordsSkipped} سجل تم تخطيه (موجود مسبقاً)";
                    }

                    MessageBox.Show(message, "نتيجة الإدخال", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                HideLoadingIndicator();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في إدخال البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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