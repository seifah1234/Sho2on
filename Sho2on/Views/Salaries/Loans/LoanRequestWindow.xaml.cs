using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Linq;
using System.Windows;
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
                // تحميل المديرين (المستخدمين الذين لديهم صلاحية موافقة)
                var managers = await _context.Users
                    .Include(u => u.JobTitle)
                    .Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value) // نفترض وجود خاصية CanApproveLoans
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
                MessageBox.Show($"خطأ في تحميل المديرين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("الرجاء إدخال كود الموظف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentUser = await _context.Users
                    .Include(u => u.Salaries)
                    .FirstOrDefaultAsync(u => u.Code == txtCode.Text);

                if (_currentUser == null)
                {
                    MessageBox.Show("الموظف غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // عرض معلومات الموظف
                user_box.SelectedValue = _currentUser.Code;

                // الحصول على الراتب الأساسي
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                if (basicSalary != null)
                {
                    txtBasicSalary.Text = basicSalary.Amount.ToString("N2");

                    // حساب الحد الأقصى للسلفة (50% من الراتب)
                    var maxAllowed = _currentUser.LoanMaxAmount ?? 0;
                    txtMaxAllowed.Text = maxAllowed.ToString("N2");
                }

                // الحصول على مبلغ صندوق الزمالة للموظف
                var friendshipBoxService = new FriendshipBoxService(_context);

                var friendshipBoxAmount = await friendshipBoxService.GetCurrentBalanceAsync();
                txtFriendshipBoxAmount.Text = $"{friendshipBoxAmount:N2}";

                // السلفة المستحقة
                txtCurrentLoan.Text = _currentUser.CurrentLoanBalance.ToString("N2");

                // حالة الموظف
                txtEmployeeStatus.Text = _currentUser.CanTakeLoan ? "مسموح بالسلفة" : "غير مسموح بالسلفة";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // التحقق من الحد الأقصى
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                var maxAllowed = _currentUser.LoanMaxAmount ?? 0;

                if (loanAmount > maxAllowed)
                {
                    MessageBox.Show($"مبلغ السلفة يتجاوز الحد المسموح ({maxAllowed:N2})", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // حساب عدد الأشهر
                int months = cmbInstallmentMonths.SelectedIndex + 1;

                // حساب القسط الشهري
                decimal monthlyInstallment = loanAmount / months;
                txtMonthlyInstallment.Text = monthlyInstallment.ToString("N2");

                // التحقق من أن القسط الشهري لا يتجاوز 30% من الراتب
                if (basicSalary != null)
                {
                    decimal maxMonthlyInstallment = basicSalary.Amount * 0.3m;
                    if (monthlyInstallment > maxMonthlyInstallment)
                    {
                        MessageBox.Show($"القسط الشهري ({monthlyInstallment:N2}) يتجاوز 30% من الراتب ({maxMonthlyInstallment:N2})",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRequest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    MessageBox.Show("الرجاء البحث عن موظف أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!_currentUser.CanTakeLoan)
                {
                    MessageBox.Show("هذا الموظف غير مسموح له بأخذ سلفة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtLoanAmount.Text, out decimal loanAmount) || loanAmount <= 0)
                {
                    MessageBox.Show("الرجاء إدخال مبلغ سلفة صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbManagers.SelectedItem == null)
                {
                    MessageBox.Show("الرجاء اختيار مدير للموافقة على السلفة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("الرجاء إدخال سبب السلفة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dpLoanDate.SelectedDate == null || dpExpectedPayback.SelectedDate == null)
                {
                    MessageBox.Show("الرجاء تحديد تاريخ الطلب والتاريخ المتوقع للسداد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الحد الأقصى
                var basicSalary = _currentUser.Salaries?.FirstOrDefault(s => s.Type == 1);
                var maxAllowed = _currentUser.LoanMaxAmount ?? 0;

                if (loanAmount > maxAllowed)
                {
                    MessageBox.Show($"مبلغ السلفة يتجاوز الحد المسموح ({maxAllowed:N2})", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من رصيد صندوق الزمالة المشترك
                var friendshipBoxService = new FriendshipBoxService(_context);
                if (!await friendshipBoxService.CanWithdrawAsync(loanAmount))
                {
                    var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                    MessageBox.Show($"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {balance:N2}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int months = cmbInstallmentMonths.SelectedIndex + 1;
                var selectedManager = cmbManagers.SelectedItem as User;

                // إنشاء سجل السلفة
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

                MessageBox.Show($"تم إرسال طلب السلفة بنجاح للمدير: {selectedManager.FullName}\nبانتظار الموافقة",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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