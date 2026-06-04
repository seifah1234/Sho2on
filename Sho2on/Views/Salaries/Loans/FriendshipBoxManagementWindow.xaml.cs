// FriendshipBoxManagementWindow.xaml.cs
using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class FriendshipBoxManagementWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly FriendshipBoxService _friendshipBoxService;

        public FriendshipBoxManagementWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _friendshipBoxService = new FriendshipBoxService(_context);

            Loaded += async (s, e) => await LoadDataAsync();
            InitializeDatePickers();
        }

        private void InitializeDatePickers()
        {
            dpFromDate.SelectedDate = DateTime.Now.AddMonths(-1);
            dpToDate.SelectedDate = DateTime.Now;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadBoxStatistics();
                await LoadTransactions();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBoxStatistics()
        {
            var box = await _friendshipBoxService.GetOrCreateFriendshipBoxAsync();
            var stats = await _friendshipBoxService.GetStatisticsAsync();

            //  ÕœÌÀ ≈Õ’«∆Ì«  «·’‰œÊﬁ
            txtCurrentBalance.Text = box.CurrentBalance.ToString("N2");
            txtTotalDeposits.Text = box.TotalDeposits.ToString("N2");
            txtTotalLoans.Text = box.TotalLoans.ToString("N2");
            txtTotalRepayments.Text = box.TotalRepayments.ToString("N2");

            //  ÕœÌÀ «·≈Õ’«∆Ì«  «·‘Â—Ì…
            txtMonthlyDeposits.Text = stats.MonthlyDeposits.ToString("N2");
            txtMonthlyLoans.Text = stats.MonthlyLoans.ToString("N2");
            txtMonthlyNet.Text = (stats.MonthlyDeposits - stats.MonthlyLoans).ToString("N2");

            //  ÕœÌÀ ‰”»… «·Œ’„
            txtDeductionPercentage.Text = box.DeductionPercentage.ToString("N1");

            // Õ”«» ⁄œœ «·Õ—ﬂ«  «·‘Â—Ì…
            var monthlyTransactions = await _friendshipBoxService.GetTransactionsAsync(
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                DateTime.Now
            );
            txtMonthlyTransactions.Text = monthlyTransactions.Count.ToString();
        }

        private async Task LoadTransactions()
        {
            try
            {
                var transactions = await _friendshipBoxService.GetTransactionsAsync();
                dgTransactions.ItemsSource = transactions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·Õ—ﬂ« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnUpdatePercentage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(txtDeductionPercentage.Text, out decimal percentage))
                {
                    await _friendshipBoxService.UpdateDeductionPercentageAsync(percentage);
                    LocalizationManager.ShowMessage(" „  ÕœÌÀ ‰”»… «·Œ’„ »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDataAsync();
                }
                else
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ‰”»… ’ÕÌÕ…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void BtnAddManualDeposit_Click(object sender, RoutedEventArgs e)
        {
            var window = new ManualTransactionWindow("Deposit");
            window.TransactionCompleted += async (s, args) => await LoadDataAsync();
            window.ShowDialog();
        }

        private void BtnAddManualWithdrawal_Click(object sender, RoutedEventArgs e)
        {
            var window = new ManualTransactionWindow("Withdrawal");
            window.TransactionCompleted += async (s, args) => await LoadDataAsync();
            window.ShowDialog();
        }

        private void BtnViewAllTransactions_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void BtnFilterTransactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? fromDate = dpFromDate.SelectedDate;
                DateTime? toDate = dpToDate.SelectedDate;
                string typeFilter = (cmbTransactionType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

                var transactions = await _friendshipBoxService.GetTransactionsAsync(fromDate, toDate);

                if (typeFilter != "«·ﬂ·")
                {
                    transactions = transactions.Where(t => t.TransactionType == typeFilter).ToList();
                }

                dgTransactions.ItemsSource = transactions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «· ’›Ì…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGenerateMonthlyReport_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("”Ì „ ≈‰‘«¡  ﬁ—Ì— ‘Â—Ì", " ﬁ—Ì—", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGenerateYearlyReport_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("”Ì „ ≈‰‘«¡  ﬁ—Ì— ”‰ÊÌ", " ﬁ—Ì—", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("”Ì „  ’œÌ— «·»Ì«‰«  ≈·Ï Excel", " ’œÌ—", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPrintBalanceSheet_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("”Ì „ ÿ»«⁄… ﬂ‘› «·—’Ìœ", "ÿ»«⁄…", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
