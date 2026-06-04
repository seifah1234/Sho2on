using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class LoanRequestWindow : Window
    {
        private AppDbContext _context;
        private User _currentUser;
        private List<User> users = new List<User>();

        public LoanRequestWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            dpLoanDate.SelectedDate = DateTime.Now;
            dpExpectedPayback.SelectedDate = DateTime.Now.AddMonths(1);

            LoadManagers();
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                txtCode.Text = user_box.SelectedValue.ToString();
                _currentUser = selectedUser; 
            }

        }

        private void searchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            var textBox = (System.Windows.Controls.TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);

            textBox.TextChanged -= searchComboBox_TextChanged;
            textBox.TextChanged += searchComboBox_TextChanged;
        }

        private void searchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            var comboBox = FindParent<System.Windows.Controls.ComboBox>(textBox);
            var searchText = textBox.Text;

            var itemsList = comboBox.Tag as List<User>;

            switch (comboBox.Name)
            {
                case "user_box":
                    itemsList = users;
                    break;
            }

            if (itemsList == null)
                return;

            if (string.IsNullOrEmpty(searchText))
            {
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = itemsList;
            }
            else
            {
                var filteredItems = itemsList
                    .Where(item => item.FullName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = filteredItems;
            }

            comboBox.IsDropDownOpen = true;
            textBox.Text = searchText;
            textBox.CaretIndex = searchText.Length;
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            while (parentObject != null)
            {
                if (parentObject is T parent)
                {
                    return parent;
                }
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }


        private async void LoadManagers()
        {
            try
            {
                //  Õ„Ì· «·„œÌ—Ì‰ («·„” Œœ„Ì‰ «·–Ì‰ ·œÌÂ„ ’·«ÕÌ… „Ê«›ﬁ…)
                var managers = await _context.Users
                    .Include(u => u.JobTitle)
                    .Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value) // ‰› —÷ ÊÃÊœ Œ«’Ì… CanApproveLoans
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                cmbManagers.ItemsSource = managers;

                if (managers.Any())
                {
                    cmbManagers.SelectedIndex = 0;
                }

                var dbUsers = _context.Users.ToList();

                users.AddRange(dbUsers);
                user_box.ItemsSource = users;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„œÌ—Ì‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ﬂÊœ «·„ÊŸ›", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentUser = await _context.Users
                    .Include(u => u.Salaries)
                    .FirstOrDefaultAsync(u => u.Code == txtCode.Text);

                if (_currentUser == null)
                {
                    LocalizationManager.ShowMessage("«·„ÊŸ› €Ì— „ÊÃÊœ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ⁄—÷ „⁄·Ê„«  «·„ÊŸ›
                user_box.SelectedValue = _currentUser.Code;

                // «·Õ’Ê· ⁄·Ï «·—« » «·√”«”Ì
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                if (basicSalary != null)
                {
                    txtBasicSalary.Text = basicSalary.Amount.ToString("N2");

                    // Õ”«» «·Õœ «·√ﬁ’Ï ··”·›… (50% „‰ «·—« »)
                    var maxAllowed = _currentUser.LoanMaxAmount ?? 0;
                    txtMaxAllowed.Text = maxAllowed.ToString("N2");
                }

                // «·Õ’Ê· ⁄·Ï „»·€ ’‰œÊﬁ «·“„«·… ··„ÊŸ›
                var friendshipBoxService = new FriendshipBoxService(_context);

                var friendshipBoxAmount = await friendshipBoxService.GetCurrentBalanceAsync();
                txtFriendshipBoxAmount.Text = $"{friendshipBoxAmount:N2}";

                // «·”·›… «·„” Õﬁ…
                txtCurrentLoan.Text = _currentUser.CurrentLoanBalance.ToString("N2");

                // Õ«·… «·„ÊŸ›
                txtEmployeeStatus.Text = _currentUser.CanTakeLoan ? "„”„ÊÕ »«·”·›…" : "€Ì— „”„ÊÕ »«·”·›…";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TxtLoanAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLoanDetails();
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateLoanDetails();
        }

        private void CalculateLoanDetails()
        {
            try
            {
                if (_currentUser == null || string.IsNullOrWhiteSpace(txtLoanAmount.Text))
                    return;

                if (!decimal.TryParse(txtLoanAmount.Text, out decimal loanAmount))
                    return;

                // «· Õﬁﬁ „‰ «·Õœ «·√ﬁ’Ï
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                var maxAllowed = _currentUser.LoanMaxAmount ?? 0;

                if (loanAmount > maxAllowed)
                {
                    LocalizationManager.ShowMessage($"„»·€ «·”·›… Ì Ã«Ê“ «·Õœ «·„”„ÊÕ ({maxAllowed:N2})", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Õ”«» ⁄œœ «·√‘Â—
                int months = cmbInstallmentMonths.SelectedIndex + 1;

                // Õ”«» «·ﬁ”ÿ «·‘Â—Ì
                decimal monthlyInstallment = loanAmount / months;
                txtMonthlyInstallment.Text = monthlyInstallment.ToString("N2");

                // «· Õﬁﬁ „‰ √‰ «·ﬁ”ÿ «·‘Â—Ì ·« Ì Ã«Ê“ 30% „‰ «·—« »
                if (basicSalary != null)
                {
                    decimal maxMonthlyInstallment = basicSalary.Amount * 0.3m;
                    if (monthlyInstallment > maxMonthlyInstallment)
                    {
                        LocalizationManager.ShowMessage($"«·ﬁ”ÿ «·‘Â—Ì ({monthlyInstallment:N2}) Ì Ã«Ê“ 30% „‰ «·—« » ({maxMonthlyInstallment:N2})",
                            " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·Õ”«»: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ «·»ÕÀ ⁄‰ „ÊŸ› √Ê·«", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_currentUser.CanTakeLoan)
                {
                    LocalizationManager.ShowMessage("Â–« «·„ÊŸ› €Ì— „”„ÊÕ ·Â »√Œ– ”·›…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtLoanAmount.Text, out decimal loanAmount) || loanAmount <= 0)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· „»·€ ”·›… ’ÕÌÕ", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbManagers.SelectedItem == null)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ «Œ Ì«— „œÌ— ··„Ê«›ﬁ… ⁄·Ï «·”·›…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ”»» «·”·›…", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpLoanDate.SelectedDate == null || dpExpectedPayback.SelectedDate == null)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡  ÕœÌœ  «—ÌŒ «·ÿ·» Ê«· «—ÌŒ «·„ Êﬁ⁄ ··”œ«œ", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «· Õﬁﬁ „‰ «·Õœ «·√ﬁ’Ï
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                var maxAllowed = _currentUser.LoanMaxAmount ?? 0;

                if (loanAmount > maxAllowed)
                {
                    LocalizationManager.ShowMessage($"„»·€ «·”·›… Ì Ã«Ê“ «·Õœ «·„”„ÊÕ ({maxAllowed:N2})", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «· Õﬁﬁ „‰ —’Ìœ ’‰œÊﬁ «·“„«·… «·„‘ —ﬂ
                var friendshipBoxService = new FriendshipBoxService(_context);
                if (!await friendshipBoxService.CanWithdrawAsync(loanAmount))
                {
                    var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                    LocalizationManager.ShowMessage($"—’Ìœ ’‰œÊﬁ «·“„«·… €Ì— ﬂ«›Ì. «·—’Ìœ «·„ «Õ: {balance:N2}", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int months = cmbInstallmentMonths.SelectedIndex + 1;
                var selectedManager = cmbManagers.SelectedItem as User;

                // ≈‰‘«¡ ”Ã· «·”·›…
                var loan = new Loan
                {
                    UserId = _currentUser.Id,
                    LoanAmount = loanAmount,
                    RemainingAmount = loanAmount,
                    LoanDate = dpLoanDate.SelectedDate.Value,
                    ExpectedPaybackDate = dpExpectedPayback.SelectedDate.Value,
                    InstallmentCount = months,
                    MonthlyInstallment = loanAmount / months,
                    Status = "SentToManager",
                    ApprovedByUserId = selectedManager.Id,
                    Reason = txtReason.Text,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _context.Loans.AddAsync(loan);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage($" „ ≈—”«· ÿ·» «·”·›… »‰Ã«Õ ··„œÌ—: {selectedManager.FullName}\n»«‰ Ÿ«— «·„Ê«›ﬁ…",
                    "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmbInstallmentMonths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateLoanDetails();
        }

        private void cmbInstallmentMonths_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
