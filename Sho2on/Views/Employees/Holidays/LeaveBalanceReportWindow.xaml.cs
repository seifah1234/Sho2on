using ClosedXML.Excel;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class LeaveBalanceReportWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private ObservableCollection<EmployeeLeaveBalanceReport> _reportData;

        public event PropertyChangedEventHandler PropertyChanged;

        public LeaveBalanceReportWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _reportData = new ObservableCollection<EmployeeLeaveBalanceReport>();


            LoadDepartments();
            LoadReport();
        }

        public ObservableCollection<EmployeeLeaveBalanceReport> ReportData
        {
            get => _reportData;
            set
            {
                _reportData = value;
                OnPropertyChanged(nameof(ReportData));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadDepartments()
        {
            try
            {
                var departments = _context.Departments
                    .OrderBy(d => d.Name)
                    .ToList();

                cmbDepartment.Items.Clear();
                cmbDepartment.Items.Add(new ComboBoxItem { Content = "جميع الإدارات", Tag = -1 });

                foreach (var department in departments)
                {
                    cmbDepartment.Items.Add(new ComboBoxItem
                    {
                        Content = department.Name,
                        Tag = department.Id
                    });
                }

                if (cmbDepartment.Items.Count > 0)
                    cmbDepartment.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الإدارات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadReport()
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.InDuty)
                    .AsQueryable();

                // تطبيق الفلاتر
                if (int.TryParse(txtEmployeeId.Text, out int employeeId) && employeeId > 0)
                {
                    query = query.Where(u => u.Id == employeeId);
                }

                if (!string.IsNullOrWhiteSpace(txtEmployeeName.Text))
                {
                    query = query.Where(u => u.FullName.Contains(txtEmployeeName.Text));
                }

                if (cmbDepartment.SelectedItem is ComboBoxItem selectedDept &&
                    selectedDept.Tag is int deptId && deptId > 0)
                {
                    query = query.Where(u => u.DepartmentId == deptId);
                }

                var users = query.ToList();
                var reportData = new ObservableCollection<EmployeeLeaveBalanceReport>();

                foreach (var user in users)
                {
                    var employeeReport = new EmployeeLeaveBalanceReport
                    {
                        EmployeeId = user.Id,
                        EmployeeName = user.FullName,
                        DepartmentName = user.Department?.Name ?? "غير معروف"
                    };

                    // الحصول على جميع أنواع الإجازات النشطة
                    var leaveTypes = _context.LeaveTypes
                        .Where(lt => lt.IsActive)
                        .OrderBy(lt => lt.Name)
                        .ToList();

                    foreach (var leaveType in leaveTypes)
                    {
                        var balanceInfo = await CalculateLeaveBalance(user.Id, leaveType.Id);
                        var leaveBalance = new LeaveBalanceDetail
                        {
                            LeaveTypeId = leaveType.Id,
                            LeaveTypeName = leaveType.Name,
                            LeaveTypeCode = leaveType.Code,
                            Total = balanceInfo.Total,
                            Used = balanceInfo.Used
                        };

                        employeeReport.LeaveBalances.Add(leaveBalance);
                    }

                    reportData.Add(employeeReport);
                }

                ReportData = reportData;
                itemsReport.ItemsSource = ReportData;

                // إظهار إحصائية
                ShowStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<LeaveBalanceInfo> CalculateLeaveBalance(int userId, int leaveTypeId)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

            if (leaveBalance == null)
            {
                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                return new LeaveBalanceInfo
                {
                    Total = leaveType?.DefaultBalance ?? 0,
                    Used = 0,
                    Remaining = leaveType?.DefaultBalance ?? 0
                };
            }

            var usedLeaves = await _context.Leaves
                .Where(l => l.UserId == userId &&
                           l.LeaveTypeId == leaveTypeId &&
                           l.Status == 2) // الموافق عليها
                .SumAsync(l => (int?)l.Duration) ?? 0;

            return new LeaveBalanceInfo
            {
                Total = leaveBalance.TotalBalance,
                Used = usedLeaves,
                Remaining = leaveBalance.TotalBalance - usedLeaves
            };
        }

        private void ShowStatistics()
        {
            if (ReportData.Any())
            {
                int totalEmployees = ReportData.Count;
                int totalLeaveTypes = ReportData.First().LeaveBalances.Count;
                int employeesWithZeroBalance = ReportData.Count(e => e.LeaveBalances.All(lb => lb.Remaining == 0));

            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void btnShowAll_Click(object sender, RoutedEventArgs e)
        {
            txtEmployeeId.Text = "";
            txtEmployeeName.Text = "";
            if (cmbDepartment.Items.Count > 0)
                cmbDepartment.SelectedIndex = 0;
            LoadReport();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ReportData.Any())
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // كود التصدير إلى Excel
                // يمكنك استخدام مكتبة مثل ClosedXML أو EPPlus
                ExportToExcel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


private void ExportToExcel()
    {
        try
        {
            if (!ReportData.Any())
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // إنشاء حوار لحفظ الملف
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ملفات Excel|*.xlsx",
                FileName = $"تقرير_رصيد_الإجازات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "حفظ ملف Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    // صفحة التقرير الرئيسية
                    var worksheet = workbook.Worksheets.Add("تقرير رصيد الإجازات");

                    // تنسيق النص من اليمين لليسار
                    worksheet.RightToLeft = true;

                    // العنوان الرئيسي
                    var titleRange = worksheet.Range(1, 1, 1, 8);
                    titleRange.Merge();
                    titleRange.Value = "تقرير رصيد الإجازات";
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Font.FontSize = 16;
                    titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    titleRange.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                    titleRange.Style.Font.FontColor = XLColor.White;
                    titleRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                    titleRange.Style.Border.OutsideBorderColor = XLColor.Black;
                    worksheet.Row(1).Height = 30;

                    // معلومات التقرير
                    worksheet.Cell(2, 1).Value = "تاريخ التصدير:";
                    worksheet.Cell(2, 2).Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
                    worksheet.Cell(3, 1).Value = "عدد الموظفين:";
                    worksheet.Cell(3, 2).Value = ReportData.Count;
                    worksheet.Cell(4, 1).Value = "أنواع الإجازات:";
                    worksheet.Cell(4, 2).Value = ReportData.FirstOrDefault()?.LeaveBalances.Count ?? 0;

                    // تنسيق معلومات التقرير
                    for (int i = 2; i <= 4; i++)
                    {
                        worksheet.Cell(i, 1).Style.Font.Bold = true;
                        worksheet.Cell(i, 1).Style.Font.FontColor = XLColor.DarkBlue;
                        worksheet.Cell(i, 2).Style.Font.FontColor = XLColor.DarkGreen;
                    }

                    // رأس الجدول - معلومات الموظفين الأساسية
                    int currentRow = 6;
                    worksheet.Cell(currentRow, 1).Value = "كود الموظف";
                    worksheet.Cell(currentRow, 2).Value = "اسم الموظف";
                    worksheet.Cell(currentRow, 3).Value = "الإدارة";

                    // رأس الجدول - أنواع الإجازات
                    int colIndex = 4;
                    var leaveTypes = ReportData.FirstOrDefault()?.LeaveBalances ?? new List<LeaveBalanceDetail>();

                    foreach (var leaveType in leaveTypes)
                    {
                        // عنوان نوع الإجازة
                        worksheet.Cell(currentRow, colIndex).Value = leaveType.LeaveTypeName;
                        worksheet.Cell(currentRow, colIndex).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, colIndex).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(currentRow, colIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(currentRow, colIndex).Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);

                        // رؤوس الأعمدة الفرعية
                        worksheet.Cell(currentRow + 1, colIndex).Value = "المجموع";
                        worksheet.Cell(currentRow + 1, colIndex + 1).Value = "المستخدم";
                        worksheet.Cell(currentRow + 1, colIndex + 2).Value = "المتبقي";
                        worksheet.Cell(currentRow + 1, colIndex + 3).Value = "النسبة %";

                        colIndex += 4;
                    }

                    // دمج خلايا عنوان نوع الإجازة
                    for (int i = 0; i < leaveTypes.Count; i++)
                    {
                        var mergeRange = worksheet.Range(currentRow, 4 + (i * 4), currentRow, 7 + (i * 4));
                        mergeRange.Merge();
                    }

                    // تنسيق رؤوس الأعمدة الفرعية
                    var subHeaderRange = worksheet.Range(currentRow + 1, 1, currentRow + 1, colIndex - 1);
                    subHeaderRange.Style.Font.Bold = true;
                    subHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    subHeaderRange.Style.Fill.BackgroundColor = XLColor.FromArgb(218, 238, 243);
                    subHeaderRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    // بيانات الموظفين
                    currentRow += 2;
                    int dataStartRow = currentRow;

                    foreach (var employee in ReportData)
                    {
                        worksheet.Cell(currentRow, 1).Value = employee.EmployeeId;
                        worksheet.Cell(currentRow, 2).Value = employee.EmployeeName;
                        worksheet.Cell(currentRow, 3).Value = employee.DepartmentName;

                        // تنسيق خلايا معلومات الموظف
                        for (int i = 1; i <= 3; i++)
                        {
                            worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                            worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 203, 173);
                            worksheet.Cell(currentRow, i).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        // بيانات رصيد الإجازات
                        colIndex = 4;
                        foreach (var balance in employee.LeaveBalances)
                        {
                            worksheet.Cell(currentRow, colIndex).Value = balance.Total;     // المجموع
                            worksheet.Cell(currentRow, colIndex + 1).Value = balance.Used;   // المستخدم
                            worksheet.Cell(currentRow, colIndex + 2).Value = balance.Remaining; // المتبقي

                            // حساب النسبة المئوية للمستخدم
                            double percentage = balance.Total > 0 ?
                                Math.Round((balance.Used / (double)balance.Total) * 100, 1) : 0;
                            worksheet.Cell(currentRow, colIndex + 3).Value = percentage;

                            // تلوين الخلايا حسب القيم
                            ColorCellBasedOnValue(worksheet.Cell(currentRow, colIndex), balance.Total, true); // المجموع - أخضر
                            ColorCellBasedOnValue(worksheet.Cell(currentRow, colIndex + 1), balance.Used, false); // المستخدم - برتقالي
                            ColorCellBasedOnValue(worksheet.Cell(currentRow, colIndex + 2), balance.Remaining, true); // المتبقي - أزرق

                            // تلوين خلية النسبة المئوية
                            ColorPercentageCell(worksheet.Cell(currentRow, colIndex + 3), percentage);

                            colIndex += 4;
                        }

                        currentRow++;
                    }

                    // تنسيق الأعمدة
                    worksheet.Column(1).Width = 10;  // كود الموظف
                    worksheet.Column(2).Width = 25;  // اسم الموظف
                    worksheet.Column(3).Width = 20;  // الإدارة

                    for (int i = 4; i <= colIndex; i++)
                    {
                        worksheet.Column(i).Width = 10;
                        worksheet.Column(i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // إضافة حدود للبيانات
                    var dataRange = worksheet.Range(dataStartRow, 1, currentRow - 1, colIndex - 1);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // إضافة تنسيق شرطي للمتبقي
                    var remainingRange = dataRange.Range(dataStartRow, 6, currentRow - 1, colIndex - 1);
                    for (int col = 6; col < colIndex; col += 4)
                    {
                        var columnRange = worksheet.Range(dataStartRow, col, currentRow - 1, col);
                        var cf = columnRange.AddConditionalFormat();
                        cf.WhenLessThan(0).Font.FontColor = XLColor.Red;
                    }

                    // إضافة ملخص في صفحة منفصلة
                    AddSummarySheet(workbook);

                    // إضافة صفحة للإحصائيات
                    AddStatisticsSheet(workbook);

                    // حفظ الملف
                    workbook.SaveAs(saveFileDialog.FileName);

                    MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{saveFileDialog.FileName}",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    // فتح الملف بعد التصدير (اختياري)
                    if (MessageBox.Show("هل تريد فتح ملف Excel الآن؟", "فتح الملف",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في التصدير: {ex.Message}\n\n{ex.InnerException?.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ColorCellBasedOnValue(IXLCell cell, int value, bool isPositive)
    {
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (value == 0)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242); // رمادي فاتح
            cell.Style.Font.FontColor = XLColor.Gray;
        }
        else if (isPositive)
        {
            if (value > 0)
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(226, 239, 218); // أخضر فاتح
                cell.Style.Font.FontColor = XLColor.DarkGreen;
                cell.Style.Font.Bold = true;
            }
            else
            {
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230); // أحمر فاتح
                cell.Style.Font.FontColor = XLColor.Red;
            }
        }
        else
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204); // أصفر/برتقالي فاتح
            cell.Style.Font.FontColor = XLColor.DarkOrange;
        }
    }

    private void ColorPercentageCell(IXLCell cell, double percentage)
    {
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.NumberFormat.Format = "0.0%";

        if (percentage == 0)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
            cell.Style.Font.FontColor = XLColor.Gray;
        }
        else if (percentage < 50)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(226, 239, 218); // أخضر فاتح
            cell.Style.Font.FontColor = XLColor.DarkGreen;
        }
        else if (percentage < 80)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204); // أصفر فاتح
            cell.Style.Font.FontColor = XLColor.DarkOrange;
            cell.Style.Font.Bold = true;
        }
        else
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230); // أحمر فاتح
            cell.Style.Font.FontColor = XLColor.Red;
            cell.Style.Font.Bold = true;
        }
    }

    private void AddSummarySheet(XLWorkbook workbook)
    {
        var summarySheet = workbook.Worksheets.Add("ملخص");

        // تنسيق النص من اليمين لليسار
        summarySheet.RightToLeft = true;

        // العنوان
        summarySheet.Cell(1, 1).Value = "ملخص رصيد الإجازات";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
        summarySheet.Range(1, 1, 1, 4).Merge();
        summarySheet.Row(1).Height = 25;

        // رأس الجدول
        summarySheet.Cell(3, 1).Value = "نوع الإجازة";
        summarySheet.Cell(3, 2).Value = "إجمالي الرصيد";
        summarySheet.Cell(3, 3).Value = "إجمالي المستخدم";
        summarySheet.Cell(3, 4).Value = "متوسط النسبة %";

        var headerRange = summarySheet.Range(3, 1, 3, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        int row = 4;
        var leaveTypes = ReportData.FirstOrDefault()?.LeaveBalances ?? new List<LeaveBalanceDetail>();

        foreach (var leaveType in leaveTypes)
        {
            int totalBalance = 0;
            int totalUsed = 0;
            int employeeCount = 0;

            foreach (var employee in ReportData)
            {
                var balance = employee.LeaveBalances.FirstOrDefault(l => l.LeaveTypeId == leaveType.LeaveTypeId);
                if (balance != null)
                {
                    totalBalance += balance.Total;
                    totalUsed += balance.Used;
                    employeeCount++;
                }
            }

            double avgPercentage = employeeCount > 0 ?
                Math.Round((totalUsed / (double)totalBalance) * 100, 1) : 0;

            summarySheet.Cell(row, 1).Value = leaveType.LeaveTypeName;
            summarySheet.Cell(row, 2).Value = totalBalance;
            summarySheet.Cell(row, 3).Value = totalUsed;
            summarySheet.Cell(row, 4).Value = avgPercentage / 100; // لتحويلها إلى تنسيق النسبة المئوية في Excel

            // تنسيق النسبة المئوية
            summarySheet.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            ColorPercentageCell(summarySheet.Cell(row, 4), avgPercentage);

            row++;
        }

        // تنسيق الأعمدة
        summarySheet.Column(1).Width = 25;
        summarySheet.Column(2).Width = 15;
        summarySheet.Column(3).Width = 15;
        summarySheet.Column(4).Width = 15;

    }

    private void AddStatisticsSheet(XLWorkbook workbook)
    {
        var statsSheet = workbook.Worksheets.Add("إحصائيات");

        // تنسيق النص من اليمين لليسار
        statsSheet.RightToLeft = true;

        // العنوان
        statsSheet.Cell(1, 1).Value = "إحصائيات التقرير";
        statsSheet.Cell(1, 1).Style.Font.Bold = true;
        statsSheet.Cell(1, 1).Style.Font.FontSize = 14;
        statsSheet.Range(1, 1, 1, 2).Merge();
        statsSheet.Row(1).Height = 25;

        // إحصائيات عامة
        int row = 3;
        statsSheet.Cell(row, 1).Value = "عدد الموظفين:";
        statsSheet.Cell(row, 2).Value = ReportData.Count;

        statsSheet.Cell(++row, 1).Value = "أنواع الإجازات:";
        statsSheet.Cell(row, 2).Value = ReportData.FirstOrDefault()?.LeaveBalances.Count ?? 0;

        statsSheet.Cell(++row, 1).Value = "تاريخ التصدير:";
        statsSheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        statsSheet.Cell(++row, 1).Value = "عدد الموظفين بدون رصيد:";
        int zeroBalanceCount = ReportData.Count(e => e.LeaveBalances.All(l => l.Remaining == 0));
        statsSheet.Cell(row, 2).Value = zeroBalanceCount;

        statsSheet.Cell(++row, 1).Value = "نسبة الموظفين بدون رصيد:";
        double zeroBalancePercentage = ReportData.Count > 0 ?
            Math.Round((zeroBalanceCount / (double)ReportData.Count) * 100, 1) : 0;
        statsSheet.Cell(row, 2).Value = zeroBalancePercentage / 100;
        statsSheet.Cell(row, 2).Style.NumberFormat.Format = "0.0%";

        // تنسيق الجدول
        for (int i = 3; i <= row; i++)
        {
            statsSheet.Cell(i, 1).Style.Font.Bold = true;
            statsSheet.Cell(i, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 203, 173);
            statsSheet.Cell(i, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(226, 239, 218);
            statsSheet.Cell(i, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            statsSheet.Cell(i, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // تنسيق الأعمدة
        statsSheet.Column(1).Width = 25;
        statsSheet.Column(2).Width = 20;
    }


    private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(itemsReport, "تقرير رصيد الإجازات");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // فئات البيانات
    public class EmployeeLeaveBalanceReport
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
        public List<LeaveBalanceDetail> LeaveBalances { get; set; } = new List<LeaveBalanceDetail>();
    }

    public class LeaveBalanceDetail : INotifyPropertyChanged
    {
        private int _total;
        private int _used;

        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveTypeCode { get; set; }

        public int Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(Remaining));
                OnPropertyChanged(nameof(UsedPercentage));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressColor));
            }
        }

        public int Used
        {
            get => _used;
            set
            {
                _used = value;
                OnPropertyChanged(nameof(Used));
                OnPropertyChanged(nameof(Remaining));
                OnPropertyChanged(nameof(UsedPercentage));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressColor));
            }
        }

        public int Remaining => Total - Used;

        public double UsedPercentage => Total > 0 ? Math.Round((Used / (double)Total) * 100, 1) : 0;

        public double ProgressPercentage => Total > 0 ? (Remaining / (double)Total) * 100 : 0;

        public Brush ProgressColor
        {
            get
            {
                if (Total == 0) return Brushes.Gray;

                double percentage = Used / (double)Total;
                if (percentage < 0.5) return Brushes.Green;
                if (percentage < 0.8) return Brushes.Orange;
                return Brushes.Red;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    // Converter لتحويل النسبة إلى ارتفاع
    public class ProgressPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                // ارتفاع الشريط كنسبة مئوية
                return new GridLength(percentage, GridUnitType.Star);
            }
            return new GridLength(0, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}