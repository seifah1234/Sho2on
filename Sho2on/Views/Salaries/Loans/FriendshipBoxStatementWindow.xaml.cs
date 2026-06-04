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
    /// „ÕÊ· «·√·Ê«‰ ·‰Ê⁄ «·Õ—ﬂ…
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
    /// ›∆… ≈Õ’«∆Ì«  «·›—⁄
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
    /// ›∆… ≈Õ’«∆Ì«  «·‘Â—
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
    /// ›∆… ﬂ‘› Õ”«» «·„ÊŸ›
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
                cmbBranch.Items.Add("«·ﬂ·");
                foreach (var branch in branches)
                {
                    cmbBranch.Items.Add(branch.Name);
                }
                cmbBranch.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·›—Ê⁄: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡  ÕœÌœ  «—ÌŒ «·»œ«Ì… Ê«·‰Â«Ì…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = dpFromDate.SelectedDate.Value;
                DateTime toDate = dpToDate.SelectedDate.Value;

                await LoadStatement(fromDate, toDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ≈‰‘«¡ «· ﬁ—Ì—: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStatement(DateTime fromDate, DateTime toDate)
        {
            try
            {
                // ≈ŸÂ«— „ƒ‘— «· Õ„Ì·
                Mouse.OverrideCursor = Cursors.Wait;

                // «·Õ’Ê· ⁄·Ï ’‰œÊﬁ «·“„«·…
                _friendshipBox = await _friendshipBoxService.GetOrCreateFriendshipBoxAsync();

                // «·Õ’Ê· ⁄·Ï «·Õ—ﬂ«  ›Ì «·› —…
                _transactions = await GetFilteredTransactions(fromDate, toDate);

                //  ÕœÌÀ «·≈Õ’«∆Ì« 
                UpdateStatistics(fromDate, toDate);

                //  ÕœÌÀ «· ›«’Ì·
                UpdateTransactionDetails();

                //  ÕœÌÀ ≈Õ’«∆Ì«  «·›—Ê⁄
                await UpdateBranchStatistics(fromDate, toDate);

                //  ÕœÌÀ ≈Õ’«∆Ì«  «·‘ÂÊ—
                UpdateMonthlyStatistics(fromDate, toDate);

                //  ÕœÌÀ ﬂ‘› Õ”«» «·„ÊŸ›Ì‰
                await UpdateEmployeeStatement(fromDate, toDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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

            //  ’›Ì… Õ”» ‰Ê⁄ «·Õ—ﬂ…
            string selectedType = (cmbTransactionType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            if (selectedType != "«·ﬂ·")
            {
                string type = selectedType switch
                {
                    "≈Ìœ«⁄" => "Deposit",
                    "”Õ»" => "Withdrawal",
                    "”œ«œ" => "Repayment",
                    _ => selectedType
                };
                query = query.Where(t => t.TransactionType == type);
            }

            //  ’›Ì… Õ”» «·›—⁄
            string selectedBranch = cmbBranch.SelectedItem?.ToString();
            if (selectedBranch != null && selectedBranch != "«·ﬂ·")
            {
                query = query.Where(t => t.User.Branch.Name == selectedBranch);
            }

            return await query.ToListAsync();
        }

        private void UpdateStatistics(DateTime fromDate, DateTime toDate)
        {
            // Õ”«» «·—’Ìœ «·«›  «ÕÌ (ﬁ»·  «—ÌŒ «·»œ«Ì…)
            var openingBalance = CalculateOpeningBalance(fromDate);

            // Õ”«» «·≈Ã„«·Ì«  ›Ì «·› —…
            decimal totalDeposits = _transactions
                .Where(t => t.TransactionType == "Deposit" || t.TransactionType == "Repayment")
                .Sum(t => t.Amount);

            decimal totalWithdrawals = _transactions
                .Where(t => t.TransactionType == "Withdrawal")
                .Sum(t => Math.Abs(t.Amount));

            decimal totalRepayments = _transactions
                .Where(t => t.TransactionType == "Repayment")
                .Sum(t => t.Amount);

            // «·—’Ìœ «·Œ «„Ì
            decimal closingBalance = openingBalance + totalDeposits - totalWithdrawals;

            //  ÕœÌÀ «·⁄—÷
            txtOpeningBalance.Text = openingBalance.ToString("N2");
            txtTotalDeposits.Text = totalDeposits.ToString("N2");
            txtTotalWithdrawals.Text = totalWithdrawals.ToString("N2");
            txtTotalRepayments.Text = totalRepayments.ToString("N2");
            txtClosingBalance.Text = closingBalance.ToString("N2");
            txtTransactionCount.Text = _transactions.Count.ToString();

            //  ÕœÌÀ «·≈Ã„«·Ì«  ›Ì «·ÃœÊ·
            txtDepositsTotal.Text = totalDeposits.ToString("N2");
            txtWithdrawalsTotal.Text = totalWithdrawals.ToString("N2");
        }

        private decimal CalculateOpeningBalance(DateTime fromDate)
        {
            try
            {
                // «·Õ’Ê· ⁄·Ï «·—’Ìœ ﬁ»·  «—ÌŒ «·»œ«Ì…
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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  ÕœÌÀ ≈Õ’«∆Ì«  «·›—Ê⁄: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMonthlyStatistics(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var monthlyStats = new List<MonthlyStatistic>();

                //  Ã„Ì⁄ «·»Ì«‰«  Õ”» «·‘Â—
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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  ÕœÌÀ ≈Õ’«∆Ì«  «·‘ÂÊ—: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    // „»·€ ’‰œÊﬁ «·“„«·… ··„ÊŸ›
                    var friendshipBoxSalary = user.Salaries?.FirstOrDefault(s => s.Type == 13);
                    decimal friendshipBoxAmount = friendshipBoxSalary?.Amount ?? 0;

                    // «·”·› «·Õ«·Ì…
                    decimal currentLoans = user.Loans?
                        .Where(l => l.Status == "Approved" && l.RemainingAmount > 0)
                        .Sum(l => l.RemainingAmount) ?? 0;

                    employeeStats.Add(new EmployeeStatement
                    {
                        Code = user.Code,
                        Name = user.FullName,
                        Branch = user.Branch?.Name ?? "€Ì— „Õœœ",
                        TotalDeposits = totalDeposits,
                        TotalWithdrawals = totalWithdrawals,
                        NetContribution = netContribution,
                        MonthlyContribution = friendshipBoxAmount,
                        CurrentLoans = currentLoans,
                        Status = user.CanTakeLoan ? "‰‘ÿ" : "€Ì— ‰‘ÿ"
                    });
                }

                dgEmployeeStatement.ItemsSource = employeeStats;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  ÕœÌÀ ﬂ‘› Õ”«» «·„ÊŸ›Ì‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡  ÕœÌœ  «—ÌŒ «·»œ«Ì… Ê«·‰Â«Ì… √Ê·«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"ﬂ‘› Õ”«» ’‰œÊﬁ «·“„«·…_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = " ’œÌ— ≈·Ï Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await ExportToExcel(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «· ’œÌ—: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    // Ê—ﬁ… «·Õ—ﬂ«  «· ›’Ì·Ì…
                    var worksheet = workbook.Worksheets.Add("«·Õ—ﬂ«  «· ›’Ì·Ì…");

                    // «·⁄‰Ê«‰
                    worksheet.Cell(1, 1).Value = "ﬂ‘› Õ”«» ’‰œÊﬁ «·“„«·… «·„‘ —ﬂ";
                    worksheet.Range(1, 1, 1, 8).Merge();
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(2, 1).Value = $"«·› —… „‰ {fromDate:yyyy-MM-dd} ≈·Ï {toDate:yyyy-MM-dd}";
                    worksheet.Range(2, 1, 2, 8).Merge();
                    worksheet.Cell(2, 1).Style.Font.FontSize = 12;
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // —√” «·ÃœÊ·
                    string[] headers = { "«· «—ÌŒ", "«·‰Ê⁄", "«·„ÊŸ›", "«·ﬂÊœ", "«·›—⁄", "«·„»·€", "«·—’Ìœ ﬁ»·", "«·—’Ìœ »⁄œ", "«·Ê’›" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(4, i + 1).Value = headers[i];
                        worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                        worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // »Ì«‰«  «·ÃœÊ·
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

                    //  ‰”Ìﬁ «·√—ﬁ«„
                    worksheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Column(8).Style.NumberFormat.Format = "#,##0.00";

                    // ≈÷«›… Ê—ﬁ… ≈Õ’«∆Ì«  «·›—Ê⁄
                    if (dgBranchStatistics.ItemsSource is List<BranchStatistic> branchStats && branchStats.Any())
                    {
                        var branchWorksheet = workbook.Worksheets.Add("≈Õ’«∆Ì«  «·›—Ê⁄");
                        string[] branchHeaders = { "«·›—⁄", "⁄œœ «·„ÊŸ›Ì‰", "«·≈Ìœ«⁄« ", "«·”ÕÊ»« ", "’«›Ì «·„”«Â„…", "‰”»… «·„”«Â„…" };

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

                    // ≈÷«›… Ê—ﬁ… ≈Õ’«∆Ì«  «·‘ÂÊ—
                    if (dgMonthlyStatistics.ItemsSource is List<MonthlyStatistic> monthlyStats && monthlyStats.Any())
                    {
                        var monthlyWorksheet = workbook.Worksheets.Add("≈Õ’«∆Ì«  «·‘ÂÊ—");
                        string[] monthlyHeaders = { "«·‘Â—", "«·≈Ìœ«⁄« ", "«·”ÕÊ»« ", "«·”œ«œ", "«·’«›Ì", "«·—’Ìœ" };

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

                    // ÷»ÿ ⁄—÷ «·√⁄„œ…
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }

                LocalizationManager.ShowMessage(" „ «· ’œÌ— »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Œÿ√ ›Ì «· ’œÌ— ≈·Ï Excel: {ex.Message}", ex);
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
                    LocalizationManager.ShowMessage("«·—Ã«¡  ÕœÌœ  «—ÌŒ «·»œ«Ì… Ê«·‰Â«Ì… √Ê·«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = dpFromDate.SelectedDate.Value;
                DateTime toDate = dpToDate.SelectedDate.Value;

                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var document = CreatePrintDocument(fromDate, toDate);
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                        $"ﬂ‘› Õ”«» ’‰œÊﬁ «·“„«·… - {fromDate:yyyy-MM-dd} ≈·Ï {toDate:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·ÿ»«⁄…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // «·⁄‰Ê«‰ «·—∆Ì”Ì
            var title = new Paragraph(new Run("ﬂ‘› Õ”«» ’‰œÊﬁ «·“„«·… «·„‘ —ﬂ"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(title);

            // «·› —…
            var period = new Paragraph(new Run($"«·› —… „‰ {fromDate:yyyy-MM-dd} ≈·Ï {toDate:yyyy-MM-dd}"))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(period);

            //  «—ÌŒ «·ÿ»«⁄…
            var printDate = new Paragraph(new Run($" «—ÌŒ «·ÿ»«⁄…: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(printDate);

            // «·≈Õ’«∆Ì«  «·”—Ì⁄…
            var summaryTable = CreateSummaryTable(fromDate, toDate);
            doc.Blocks.Add(summaryTable);

            //  ›«’Ì· «·Õ—ﬂ« 
            var detailsHeader = new Paragraph(new Run(" ›«’Ì· «·Õ—ﬂ« :"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            doc.Blocks.Add(detailsHeader);

            var detailsTable = CreateDetailsTable();
            doc.Blocks.Add(detailsTable);

            // ≈Ã„«·Ì«  «·Õ—ﬂ« 
            var totals = new Paragraph();
            totals.Inlines.Add(new Run("≈Ã„«·Ì «·≈Ìœ«⁄« : ") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTotalDeposits.Text));
            totals.Inlines.Add(new Run("     ≈Ã„«·Ì «·”ÕÊ»« : ") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTotalWithdrawals.Text));
            totals.Inlines.Add(new Run("     ⁄œœ «·Õ—ﬂ« : ") { FontWeight = FontWeights.Bold });
            totals.Inlines.Add(new Run(txtTransactionCount.Text));

            totals.Margin = new Thickness(0, 20, 0, 0);
            doc.Blocks.Add(totals);

            // «· ÊﬁÌ⁄« 
            var signatures = new Paragraph();
            signatures.Inlines.Add(new Run("\n\n\n"));
            signatures.Inlines.Add(new Run("„⁄œ «· ﬁ—Ì—: ___________________"));
            signatures.Inlines.Add(new Run("                    „œﬁﬁ: ___________________"));
            signatures.Inlines.Add(new Run("                    „œÌ— «·„«·Ì…: ___________________"));

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

            // —√” «·ÃœÊ·
            var headerRow = new TableRow { Background = Brushes.LightGray };
            string[] headers = { "«·»‰œ", "«·„»·€", "«·»‰œ", "«·„»·€" };

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

            // »Ì«‰«  «·ÃœÊ·
            var data = new[]
            {
                new[] { "«·—’Ìœ «·«›  «ÕÌ", txtOpeningBalance.Text, "≈Ã„«·Ì «·≈Ìœ«⁄« ", txtTotalDeposits.Text },
                new[] { "≈Ã„«·Ì «·”ÕÊ»« ", txtTotalWithdrawals.Text, "≈Ã„«·Ì «·”œ«œ", txtTotalRepayments.Text },
                new[] { "«·—’Ìœ «·Œ «„Ì", txtClosingBalance.Text, "⁄œœ «·Õ—ﬂ« ", txtTransactionCount.Text }
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
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «· «—ÌŒ
            table.Columns.Add(new TableColumn { Width = new GridLength(60) }); // «·‰Ê⁄
            table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // «·„ÊŸ›
            table.Columns.Add(new TableColumn { Width = new GridLength(70) }); // «·ﬂÊœ
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·„»·€
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·—’Ìœ ﬁ»·
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·—’Ìœ »⁄œ
            table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // «·Ê’›

            // —√” «·ÃœÊ·
            var headerRow = new TableRow { Background = Brushes.LightGray };
            string[] headers = { "«· «—ÌŒ", "«·‰Ê⁄", "«·„ÊŸ›", "«·ﬂÊœ", "«·„»·€", "«·—’Ìœ ﬁ»·", "«·—’Ìœ »⁄œ", "«·Ê’›" };

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

            // »Ì«‰«  «·ÃœÊ· (√Ê· 50 Õ—ﬂ… ›ﬁÿ ··ÿ»«⁄…)
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
