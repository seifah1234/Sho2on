using ClosedXML.Excel;
using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Data;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Input;
using Cursors = System.Windows.Input.Cursors;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HR_Application.Views
{
    /// <summary>
    /// محول الألوان لنوع الحركة
    /// </summary>
    public class TransactionTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string transactionType)
            {
                return transactionType switch
                {
                    "Deposit" => Brushes.Green,
                    "Withdrawal" => Brushes.Red,
                    "Repayment" => Brushes.Blue,
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// فئة إحصائيات الفرع
    /// </summary>
    public class BranchStatistic
    {
        public string BranchName { get; set; }
        public int EmployeeCount { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal NetContribution { get; set; }
        public decimal ContributionPercentage { get; set; }
    }

    /// <summary>
    /// فئة إحصائيات الشهر
    /// </summary>
    public class MonthlyStatistic
    {
        public string Month { get; set; }
        public decimal Deposits { get; set; }
        public decimal Withdrawals { get; set; }
        public decimal Repayments { get; set; }
        public decimal NetAmount { get; set; }
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// فئة كشف حساب الموظف
    /// </summary>
    public class EmployeeStatement
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Branch { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal NetContribution { get; set; }
        public decimal MonthlyContribution { get; set; }
        public decimal CurrentLoans { get; set; }
        public string Status { get; set; }
    }

    public partial class FriendshipBoxStatementWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly FriendshipBoxService _friendshipBoxService;
        private List<FriendshipBoxTransaction> _transactions = new();
        private FriendshipBox _friendshipBox;

        public FriendshipBoxStatementWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _friendshipBoxService = new FriendshipBoxService(_context);

            InitializeDates();
            LoadBranches();
        }

        private void InitializeDates()
        {
            dpFromDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpToDate.SelectedDate = DateTime.Now;
        }

        private async void LoadBranches()
        {
            try
            {
                var branches = await _context.Branches.ToListAsync();
                cmbBranch.Items.Clear();
                cmbBranch.Items.Add(LocalizationManager.Translate("الكل"));
                foreach (var branch in branches)
                {
                    cmbBranch.Items.Add(branch.Name);
                }
                cmbBranch.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الفروع: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("الرجاء تحديد تاريخ البداية والنهاية", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = dpFromDate.SelectedDate.Value;
                DateTime toDate = dpToDate.SelectedDate.Value;

                await LoadStatement(fromDate, toDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في إنشاء التقرير: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStatement(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // إظهار مؤشر التحميل
                Mouse.OverrideCursor = Cursors.Wait;

                // الحصول على صندوق الزمالة
                _friendshipBox = await _friendshipBoxService.GetOrCreateFriendshipBoxAsync();

                // الحصول على الحركات في الفترة
                _transactions = await GetFilteredTransactions(fromDate, toDate);

                // تحديث الإحصائيات
                UpdateStatistics(fromDate, toDate);

                // تحديث التفاصيل
                UpdateTransactionDetails();

                // تحديث إحصائيات الفروع
                await UpdateBranchStatistics(fromDate, toDate);

                // تحديث إحصائيات الشهور
                UpdateMonthlyStatistics(fromDate, toDate);

                // تحديث كشف حساب الموظفين
                await UpdateEmployeeStatement(fromDate, toDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<List<FriendshipBoxTransaction>> GetFilteredTransactions(DateTime fromDate, DateTime toDate)
        {
            var query = _context.FriendshipBoxTransactions
                .Include(t => t.User)
                .ThenInclude(u => u.Branch)
                .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate.AddDays(1))
                .OrderBy(t => t.TransactionDate)
                .AsQueryable();

            // تصفية حسب نوع الحركة
            string selectedType = (cmbTransactionType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            if (selectedType != LocalizationManager.Translate("الكل"))
            {
                if (selectedType == LocalizationManager.Translate("الإيداع"))
                    selectedType = "Deposit";
                else if (selectedType == LocalizationManager.Translate("السحب"))
                    selectedType = "Withdrawal";
                else if (selectedType == LocalizationManager.Translate("السداد"))
                    selectedType = "Repayment";

                query = query.Where(t => t.TransactionType == selectedType);
            }

            // تصفية حسب الفرع
            string selectedBranch = cmbBranch.SelectedItem?.ToString();
            if (selectedBranch != null && selectedBranch != LocalizationManager.Translate("الكل"))
            {
                query = query.Where(t => t.User.Branch.Name == selectedBranch);
            }

            return await query.ToListAsync();
        }

        private void UpdateStatistics(DateTime fromDate, DateTime toDate)
        {
            // حساب الرصيد الافتتاحي (قبل تاريخ البداية)
            var openingBalance = CalculateOpeningBalance(fromDate);

            // حساب الإجماليات في الفترة
            decimal totalDeposits = _transactions
                .Where(t => t.TransactionType == "Deposit" || t.TransactionType == "Repayment")
                .Sum(t => t.Amount);

            decimal totalWithdrawals = _transactions
                .Where(t => t.TransactionType == "Withdrawal")
                .Sum(t => Math.Abs(t.Amount));

            decimal totalRepayments = _transactions
                .Where(t => t.TransactionType == "Repayment")
                .Sum(t => t.Amount);

            // الرصيد الختامي
            decimal closingBalance = openingBalance + totalDeposits - totalWithdrawals;

            // تحديث العرض
            txtOpeningBalance.Text = openingBalance.ToString("N2");
            txtTotalDeposits.Text = totalDeposits.ToString("N2");
            txtTotalWithdrawals.Text = totalWithdrawals.ToString("N2");
            txtTotalRepayments.Text = totalRepayments.ToString("N2");
            txtClosingBalance.Text = closingBalance.ToString("N2");
            txtTransactionCount.Text = _transactions.Count.ToString();

            // تحديث الإجماليات في الجدول
            txtDepositsTotal.Text = totalDeposits.ToString("N2");
            txtWithdrawalsTotal.Text = totalWithdrawals.ToString("N2");
        }

        private decimal CalculateOpeningBalance(DateTime fromDate)
        {
            try
            {
                // الحصول على الرصيد قبل تاريخ البداية
                var lastTransactionBefore = _context.FriendshipBoxTransactions
                    .Where(t => t.TransactionDate < fromDate)
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault();

                return lastTransactionBefore?.BalanceAfter ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateTransactionDetails()
        {
            dgTransactions.ItemsSource = _transactions;
        }

        private async Task UpdateBranchStatistics(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var branches = await _context.Branches.ToListAsync();
                var branchStats = new List<BranchStatistic>();

                foreach (var branch in branches)
                {
                    var branchUsers = await _context.Users
                        .Where(u => u.BranchId == branch.Id)
                        .ToListAsync();

                    var branchTransactions = _transactions
                        .Where(t => t.User?.BranchId == branch.Id)
                        .ToList();

                    decimal totalDeposits = branchTransactions
                        .Where(t => t.TransactionType == "Deposit" || t.TransactionType == "Repayment")
                        .Sum(t => t.Amount);

                    decimal totalWithdrawals = branchTransactions
                        .Where(t => t.TransactionType == "Withdrawal")
                        .Sum(t => Math.Abs(t.Amount));

                    decimal netContribution = totalDeposits - totalWithdrawals;
                    decimal totalTransactions = _transactions.Sum(t => Math.Abs(t.Amount));
                    decimal contributionPercentage = totalTransactions > 0 ?
                        (netContribution / totalTransactions) * 100 : 0;

                    branchStats.Add(new BranchStatistic
                    {
                        BranchName = branch.Name,
                        EmployeeCount = branchUsers.Count,
                        TotalDeposits = totalDeposits,
                        TotalWithdrawals = totalWithdrawals,
                        NetContribution = netContribution,
                        ContributionPercentage = Math.Round(contributionPercentage, 1)
                    });
                }

                dgBranchStatistics.ItemsSource = branchStats;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحديث إحصائيات الفروع: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMonthlyStatistics(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var monthlyStats = new List<MonthlyStatistic>();

                // تجميع البيانات حسب الشهر
                var currentDate = fromDate;
                decimal runningBalance = CalculateOpeningBalance(fromDate);

                while (currentDate <= toDate)
                {
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    if (monthEnd > toDate) monthEnd = toDate;

                    var monthTransactions = _transactions
                        .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                        .ToList();

                    decimal deposits = monthTransactions
                        .Where(t => t.TransactionType == "Deposit")
                        .Sum(t => t.Amount);

                    decimal withdrawals = monthTransactions
                        .Where(t => t.TransactionType == "Withdrawal")
                        .Sum(t => Math.Abs(t.Amount));

                    decimal repayments = monthTransactions
                        .Where(t => t.TransactionType == "Repayment")
                        .Sum(t => t.Amount);

                    decimal netAmount = deposits + repayments - withdrawals;
                    runningBalance += netAmount;

                    monthlyStats.Add(new MonthlyStatistic
                    {
                        Month = monthStart.ToString("MMMM yyyy"),
                        Deposits = deposits,
                        Withdrawals = withdrawals,
                        Repayments = repayments,
                        NetAmount = netAmount,
                        Balance = runningBalance
                    });

                    currentDate = monthStart.AddMonths(1);
                }

                dgMonthlyStatistics.ItemsSource = monthlyStats;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحديث إحصائيات الشهور: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateEmployeeStatement(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Salaries)
                    .Include(u => u.Loans)
                    .ToListAsync();

                var employeeStats = new List<EmployeeStatement>();

                foreach (var user in users)
                {
                    var userTransactions = _transactions
                        .Where(t => t.UserId == user.Id)
                        .ToList();

                    decimal totalDeposits = userTransactions
                        .Where(t => t.TransactionType == "Deposit")
                        .Sum(t => t.Amount);

                    decimal totalWithdrawals = userTransactions
                        .Where(t => t.TransactionType == "Withdrawal")
                        .Sum(t => Math.Abs(t.Amount));

                    decimal netContribution = totalDeposits - totalWithdrawals;

                    // مبلغ صندوق الزمالة للموظف
                    var friendshipBoxSalary = user.Salaries?.FirstOrDefault(s => s.Type == 13);
                    decimal friendshipBoxAmount = friendshipBoxSalary?.Amount ?? 0;

                    // السلف الحالية
                    decimal currentLoans = user.Loans?
                        .Where(l => l.Status == "Approved" && l.RemainingAmount > 0)
                        .Sum(l => l.RemainingAmount) ?? 0;

                    employeeStats.Add(new EmployeeStatement
                    {
                        Code = user.Code,
                        Name = user.FullName,
                        Branch = user.Branch?.Name ?? LocalizationManager.Translate("غير محدد"),
                        TotalDeposits = totalDeposits,
                        TotalWithdrawals = totalWithdrawals,
                        NetContribution = netContribution,
                        MonthlyContribution = friendshipBoxAmount,
                        CurrentLoans = currentLoans,
                        Status = user.CanTakeLoan ? LocalizationManager.Translate("نشط") : LocalizationManager.Translate("غير نشط")
                    });
                }

                dgEmployeeStatement.ItemsSource = employeeStats;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحديث كشف حساب الموظفين: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("الرجاء تحديد تاريخ البداية والنهاية أولاً", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"كشف حساب صندوق الزمالة_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = LocalizationManager.Translate("تصدير إلى Excel")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await ExportToExcel(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في التصدير: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToExcel(string filePath)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var workbook = new XLWorkbook())
                {
                    DateTime fromDate = dpFromDate.SelectedDate.Value;
                    DateTime toDate = dpToDate.SelectedDate.Value;

                    // ورقة الحركات التفصيلية
                    var worksheet = workbook.Worksheets.Add(LocalizationManager.Translate("الحركات التفصيلية"));

                    // العنوان
                    worksheet.Cell(1, 1).Value = LocalizationManager.Translate("كشف حساب صندوق الزمالة المشترك");
                    worksheet.Range(1, 1, 1, 8).Merge();
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(2, 1).Value = $"الفترة من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}";
                    worksheet.Range(2, 1, 2, 8).Merge();
                    worksheet.Cell(2, 1).Style.Font.FontSize = 12;
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // رأس الجدول
                    string[] headers = { LocalizationManager.Translate("التاريخ"), LocalizationManager.Translate("النوع"), LocalizationManager.Translate("الموظف"), LocalizationManager.Translate("الكود"), LocalizationManager.Translate("الفرع"), LocalizationManager.Translate("المبلغ"), LocalizationManager.Translate("الرصيد قبل"), LocalizationManager.Translate("الرصيد بعد"), LocalizationManager.Translate("الوصف") };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(4, i + 1).Value = headers[i];
                        worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // بيانات الجدول
                    int row = 5;
                    foreach (var transaction in _transactions)
                    {
                        worksheet.Cell(row, 1).Value = transaction.TransactionDate.ToString("yyyy-MM-dd");
                        worksheet.Cell(row, 2).Value = transaction.TransactionType;
                        worksheet.Cell(row, 3).Value = transaction.User?.FullName;
                        worksheet.Cell(row, 4).Value = transaction.User?.Code;
                        worksheet.Cell(row, 5).Value = transaction.User?.Branch?.Name;
                        worksheet.Cell(row, 6).Value = transaction.Amount;
                        worksheet.Cell(row, 7).Value = transaction.BalanceBefore;
                        worksheet.Cell(row, 8).Value = transaction.BalanceAfter;
                        worksheet.Cell(row, 9).Value = transaction.Description;
                        row++;
                    }

                    // تنسيق الأرقام
                    worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Column(8).Style.NumberFormat.Format = "#,##0.00";

                    // إضافة ورقة إحصائيات الفروع
                    if (dgBranchStatistics.ItemsSource is List<BranchStatistic> branchStats && branchStats.Any())
                    {
                        var branchWorksheet = workbook.Worksheets.Add(LocalizationManager.Translate("إحصائيات الفروع"));
                        string[] branchHeaders = { LocalizationManager.Translate("الفرع"), LocalizationManager.Translate("عدد الموظفين"), LocalizationManager.Translate("الإيداعات"), LocalizationManager.Translate("السحوبات"), LocalizationManager.Translate("صافي المساهمة"), LocalizationManager.Translate("نسبة المساهمة") };

                        for (int i = 0; i < branchHeaders.Length; i++)
                        {
                            branchWorksheet.Cell(1, i + 1).Value = branchHeaders[i];
                            branchWorksheet.Cell(1, i + 1).Style.Font.Bold = true;
                            branchWorksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                        }

                        row = 2;
                        foreach (var stat in branchStats)
                        {
                            branchWorksheet.Cell(row, 1).Value = stat.BranchName;
                            branchWorksheet.Cell(row, 2).Value = stat.EmployeeCount;
                            branchWorksheet.Cell(row, 3).Value = stat.TotalDeposits;
                            branchWorksheet.Cell(row, 4).Value = stat.TotalWithdrawals;
                            branchWorksheet.Cell(row, 5).Value = stat.NetContribution;
                            branchWorksheet.Cell(row, 6).Value = stat.ContributionPercentage;
                            row++;
                        }

                        branchWorksheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
                        branchWorksheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
                        branchWorksheet.Column(5).Style.NumberFormat.Format = "#,##0.00";
                        branchWorksheet.Column(6).Style.NumberFormat.Format = "0.0";
                    }

                    // إضافة ورقة إحصائيات الشهور
                    if (dgMonthlyStatistics.ItemsSource is List<MonthlyStatistic> monthlyStats && monthlyStats.Any())
                    {
                        var monthlyWorksheet = workbook.Worksheets.Add(LocalizationManager.Translate("إحصائيات الشهور"));
                        string[] monthlyHeaders = { LocalizationManager.Translate("الشهر"), LocalizationManager.Translate("الإيداعات"), LocalizationManager.Translate("السحوبات"), LocalizationManager.Translate("السداد"), LocalizationManager.Translate("الصافي"), LocalizationManager.Translate("الرصيد") };

                        for (int i = 0; i < monthlyHeaders.Length; i++)
                        {
                            monthlyWorksheet.Cell(1, i + 1).Value = monthlyHeaders[i];
                            monthlyWorksheet.Cell(1, i + 1).Style.Font.Bold = true;
                            monthlyWorksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                        }

                        row = 2;
                        foreach (var stat in monthlyStats)
                        {
                            monthlyWorksheet.Cell(row, 1).Value = stat.Month;
                            monthlyWorksheet.Cell(row, 2).Value = stat.Deposits;
                            monthlyWorksheet.Cell(row, 3).Value = stat.Withdrawals;
                            monthlyWorksheet.Cell(row, 4).Value = stat.Repayments;
                            monthlyWorksheet.Cell(row, 5).Value = stat.NetAmount;
                            monthlyWorksheet.Cell(row, 6).Value = stat.Balance;
                            row++;
                        }

                        for (int i = 2; i <= 6; i++)
                        {
                            monthlyWorksheet.Column(i).Style.NumberFormat.Format = "#,##0.00";
                        }
                    }

                    // ضبط عرض الأعمدة
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }

                LocalizationManager.ShowMessage("تم التصدير بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التصدير إلى Excel: {ex.Message}", ex);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("الرجاء تحديد تاريخ البداية والنهاية أولاً", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = dpFromDate.SelectedDate.Value;
                DateTime toDate = dpToDate.SelectedDate.Value;

                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = CreatePrintDocument(fromDate, toDate);
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                        $"كشف حساب صندوق الزمالة - {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في الطباعة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument(DateTime fromDate, DateTime toDate)
        {
            var doc = new FlowDocument
            {
                PageWidth = 794, // A4 width in points
                PageHeight = 1123, // A4 height in points
                PagePadding = new Thickness(50),
                ColumnWidth = 694,
                FontFamily = new FontFamily("Arial"),
                FontSize = 10
            };

            // العنوان الرئيسي
            var title = new Paragraph(new Run(LocalizationManager.Translate("كشف حساب صندوق الزمالة المشترك")))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(title);

            // الفترة
            var period = new Paragraph(new Run($"الفترة من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}"))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(period);

            // تاريخ الطباعة
            var printDate = new Paragraph(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(printDate);

            // الإحصائيات السريعة
            var summaryTable = CreateSummaryTable(fromDate, toDate);
            doc.Blocks.Add(summaryTable);

            // تفاصيل الحركات
            var detailsHeader = new Paragraph(new Run(LocalizationManager.Translate("تفاصيل الحركات:")))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            doc.Blocks.Add(detailsHeader);

            var detailsTable = CreateDetailsTable();
            doc.Blocks.Add(detailsTable);

            // إجماليات الحركات
            var totals = new Paragraph();
            totals.Inlines.Add(new Run(LocalizationManager.Translate("إجمالي الإيداعات: ")) { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTotalDeposits.Text));
            totals.Inlines.Add(new Run(LocalizationManager.Translate("     إجمالي السحوبات: ")) { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTotalWithdrawals.Text));
            totals.Inlines.Add(new Run(LocalizationManager.Translate("     عدد الحركات: ")) { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTransactionCount.Text));

            totals.Margin = new Thickness(0, 20, 0, 0);
            doc.Blocks.Add(totals);

            // التوقيعات
            var signatures = new Paragraph();
            signatures.Inlines.Add(new Run("\n\n\n"));
            signatures.Inlines.Add(new Run(LocalizationManager.Translate("معد التقرير: ___________________")));
            signatures.Inlines.Add(new Run(LocalizationManager.Translate("                    مدقق: ___________________")));
            signatures.Inlines.Add(new Run(LocalizationManager.Translate("                    مدير المالية: ___________________")));

            doc.Blocks.Add(signatures);

            return doc;
        }

        private Table CreateSummaryTable(DateTime fromDate, DateTime toDate)
        {
            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });

            // رأس الجدول
            var headerRow = new TableRow { Background = Brushes.LightGray };
            string[] headers = { LocalizationManager.Translate("البند"), LocalizationManager.Translate("المبلغ"), LocalizationManager.Translate("البند"), LocalizationManager.Translate("المبلغ") };

            foreach (var header in headers)
            {
                var cell = new TableCell(new Paragraph(new Run(header))
                {
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                });
                cell.BorderBrush = Brushes.Black;
                cell.BorderThickness = new Thickness(1);
                cell.Padding = new Thickness(5);
                headerRow.Cells.Add(cell);
            }
            table.RowGroups.Add(new TableRowGroup());
            table.RowGroups[0].Rows.Add(headerRow);

            // بيانات الجدول
            var data = new[]
            {
                new[] { LocalizationManager.Translate("الرصيد الافتتاحي"), txtOpeningBalance.Text, LocalizationManager.Translate("إجمالي الإيداعات"), txtTotalDeposits.Text },
                new[] { LocalizationManager.Translate("إجمالي السحوبات"), txtTotalWithdrawals.Text, LocalizationManager.Translate("إجمالي السداد"), txtTotalRepayments.Text },
                new[] { LocalizationManager.Translate("الرصيد الختامي"), txtClosingBalance.Text, LocalizationManager.Translate("عدد الحركات"), txtTransactionCount.Text }
            };

            foreach (var rowData in data)
            {
                var row = new TableRow();
                foreach (var cellData in rowData)
                {
                    var cell = new TableCell(new Paragraph(new Run(cellData))
                    {
                        TextAlignment = TextAlignment.Center
                    });
                    cell.BorderBrush = Brushes.Black;
                    cell.BorderThickness = new Thickness(0.5);
                    cell.Padding = new Thickness(3);
                    row.Cells.Add(cell);
                }
                table.RowGroups[0].Rows.Add(row);
            }

            return table;
        }

        private Table CreateDetailsTable()
        {
            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // التاريخ
            table.Columns.Add(new TableColumn { Width = new GridLength(60) }); // النوع
            table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // الموظف
            table.Columns.Add(new TableColumn { Width = new GridLength(70) }); // الكود
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // المبلغ
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الرصيد قبل
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الرصيد بعد
            table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // الوصف

            // رأس الجدول
            var headerRow = new TableRow { Background = Brushes.LightGray };
            string[] headers = { LocalizationManager.Translate("التاريخ"), LocalizationManager.Translate("النوع"), LocalizationManager.Translate("الموظف"), LocalizationManager.Translate("الكود"), LocalizationManager.Translate("المبلغ"), LocalizationManager.Translate("الرصيد قبل"), LocalizationManager.Translate("الرصيد بعد"), LocalizationManager.Translate("الوصف") };

            foreach (var header in headers)
            {
                var cell = new TableCell(new Paragraph(new Run(header))
                {
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                });
                cell.BorderBrush = Brushes.Black;
                cell.BorderThickness = new Thickness(1);
                cell.Padding = new Thickness(3);
                headerRow.Cells.Add(cell);
            }
            table.RowGroups.Add(new TableRowGroup());
            table.RowGroups[0].Rows.Add(headerRow);

            // بيانات الجدول (أول 50 حركة فقط للطباعة)
            var printTransactions = _transactions.Take(50).ToList();

            foreach (var transaction in printTransactions)
            {
                var row = new TableRow();

                string[] values =
                {
                    transaction.TransactionDate.ToString("yyyy-MM-dd"),
                    transaction.TransactionType,
                    transaction.User?.FullName ?? "",
                    transaction.User?.Code ?? "",
                    transaction.Amount.ToString("N2"),
                    transaction.BalanceBefore.ToString("N2"),
                    transaction.BalanceAfter.ToString("N2"),
                    transaction.Description ?? ""
                };

                foreach (var value in values)
                {
                    var cell = new TableCell(new Paragraph(new Run(value))
                    {
                        TextAlignment = TextAlignment.Center
                    });
                    cell.BorderBrush = Brushes.Black;
                    cell.BorderThickness = new Thickness(0.5);
                    cell.Padding = new Thickness(3);
                    row.Cells.Add(cell);
                }
                table.RowGroups[0].Rows.Add(row);
            }

            return table;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
