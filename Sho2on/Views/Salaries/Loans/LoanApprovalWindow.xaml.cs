using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class LoanApprovalWindow : Window
    {
        private AppDbContext _context;
        private Loan _selectedLoan;
        public LoanApprovalWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadLoans();
        }

        private string GetManagerName(int managerId)
        {
            var manager = _context.Users.Find(managerId);
            return manager?.FullName ?? "€Ì— „⁄—Ê›";
        }

        private async void LoadLoans(string statusFilter = null)
        {
            try
            {
                var query = _context.Loans
                   .Include(l => l.User)
                   .Include(l => l.ApprovedByUser)
                   .Where(l => l.ApprovedByUserId == App.CurrentUser.Id) 
                   .AsQueryable();

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Ã„Ì⁄ «·Õ«·« ")
                {
                    query = query.Where(l => l.Status == statusFilter);
                }

                var loans = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
                dgLoans.ItemsSource = loans;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgLoans_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedLoan = dgLoans.SelectedItem as Loan;
            if (_selectedLoan != null)
            {
                UpdateLoanDetails();

                btnApprove.IsEnabled = _selectedLoan.Status == "SentToManager";
                btnReject.IsEnabled = _selectedLoan.Status == "SentToManager";
                btnViewDetails.IsEnabled = true;
            }
        }

        private void UpdateLoanDetails()
        {
            if (_selectedLoan == null) return;

            string details = $"«·„ÊŸ›: {_selectedLoan.User?.FullName}\n" +
                            $"«·ﬂÊœ: {_selectedLoan.User?.Code}\n" +
                            $"„»·€ «·”·›…: {_selectedLoan.LoanAmount:N2}\n" +
                            $"«·„»·€ «·„ »ﬁÌ: {_selectedLoan.RemainingAmount:N2}\n" +
                            $"⁄œœ «·√ﬁ”«ÿ: {_selectedLoan.InstallmentCount}\n" +
                            $"«·ﬁ”ÿ «·‘Â—Ì: {_selectedLoan.MonthlyInstallment:N2}\n" +
                            $" «—ÌŒ «·ÿ·»: {_selectedLoan.LoanDate:yyyy-MM-dd}\n" +
                            $" «—ÌŒ «·”œ«œ «·„ Êﬁ⁄: {_selectedLoan.ExpectedPaybackDate:yyyy-MM-dd}\n" +
                            $"«·”»»: {_selectedLoan.Reason}\n" +
                            $"«·Õ«·…: {_selectedLoan.Status}";

            txtSelectedLoanDetails.Text = details;

        }

        // ›Ì LoanApprovalWindow.cs -  ⁄œÌ· œ«·… BtnApprove_Click
        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ «Œ Ì«— ”·›… ··„Ê«›ﬁ… ⁄·ÌÂ«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                LocalizationManager.ShowMessage("·« Ì„ﬂ‰ «·„Ê«›ﬁ… ⁄·Ï ”·›… €Ì— „⁄·ﬁ…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = LocalizationManager.ShowMessage(
                $"Â· √‰  „ √ﬂœ „‰ «·„Ê«›ﬁ… ⁄·Ï ”·›… «·„ÊŸ› {_selectedLoan.User?.FullName} »„»·€ {_selectedLoan.LoanAmount:N2}ø",
                " √ﬂÌœ «·„Ê«›ﬁ…",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // «” Œœ«„ Œœ„… ’‰œÊﬁ «·“„«·…
                    var friendshipBoxService = new FriendshipBoxService(_context);

                    // «· Õﬁﬁ „‰ —’Ìœ «·’‰œÊﬁ
                    if (!await friendshipBoxService.CanWithdrawAsync(_selectedLoan.LoanAmount))
                    {
                        var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                        LocalizationManager.ShowMessage($"—’Ìœ ’‰œÊﬁ «·“„«·… €Ì— ﬂ«›Ì. «·—’Ìœ «·„ «Õ: {balance:N2}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Œ’„ «·„»·€ „‰ ’‰œÊﬁ «·“„«·… «·„‘ —ﬂ
                    await friendshipBoxService.RecordWithdrawalAsync(
                        _selectedLoan.UserId,
                        _selectedLoan.LoanAmount,
                        _selectedLoan.Id,
                        _selectedLoan.Reason);

                    //  ÕœÌÀ Õ«·… «·”·›…
                    _selectedLoan.Status = "Approved";
                    _selectedLoan.ApprovedDate = DateTime.Now;
                    _selectedLoan.ApprovedByUserId = App.CurrentUser.Id; // «› —÷ √‰ App.CurrentUserId „ÊÃÊœ
                    _selectedLoan.UpdatedAt = DateTime.Now;

                    //  ÕœÌÀ —’Ìœ «·”·› ··„ÊŸ›
                    var user = await _context.Users.FindAsync(_selectedLoan.UserId);
                    if (user != null)
                    {
                        user.CurrentLoanBalance += _selectedLoan.LoanAmount;

                        // ≈–« Ê’· —’Ìœ «·”·› ··Õœ «·√ﬁ’Ï° „‰⁄ √Œ– ”·›«  ÃœÌœ…
                        var basicSalary = await _context.Salaries
                            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Type == 1);

                        if (basicSalary != null)
                        {
                            decimal maxLoan = basicSalary.Amount * 0.5m; // 50% „‰ «·—« »
                            if (user.CurrentLoanBalance >= maxLoan)
                            {
                                user.CanTakeLoan = false;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage(" „  «·„Ê«›ﬁ… ⁄·Ï «·”·›… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ «Œ Ì«— ”·›… ·—›÷Â«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                LocalizationManager.ShowMessage("·« Ì„ﬂ‰ —›÷ ”·›… €Ì— „⁄·ﬁ…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = LocalizationManager.ShowMessage(
                $"Â· √‰  „ √ﬂœ „‰ —›÷ ”·›… «·„ÊŸ› {_selectedLoan.User?.FullName}ø",
                " √ﬂÌœ «·—›÷",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _selectedLoan.Status = "Rejected";
                    _selectedLoan.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage(" „ —›÷ «·”·›… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ «Œ Ì«— ”·›… ·⁄—÷  ›«’Ì·Â«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
/*
            var detailsWindow = new LoanDetailsWindow(_selectedLoan.Id);
            detailsWindow.ShowDialog();*/
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            string status = (cmbStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            LoadLoans(status == "Ã„Ì⁄ «·Õ«·« " ? null : status);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLoans();
        }
    }
}
