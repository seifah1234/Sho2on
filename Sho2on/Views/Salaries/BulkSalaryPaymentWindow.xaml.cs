using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class BulkSalaryPaymentWindow : Window
    {
        private AppDbContext _context;
        private ObservableCollection<EmployeeSalaryViewModel> _employees;
        private int _currentMonth;
        private int _currentYear;

        public BulkSalaryPaymentWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _employees = new ObservableCollection<EmployeeSalaryViewModel>();
            dgEmployees.ItemsSource = _employees;

            InitializeDateControls();
        }

        private void InitializeDateControls()
        {
            // شهور السنة
            for (int i = 1; i <= 12; i++)
            {
                cmbMonth.Items.Add(new System.Windows.Controls.ComboBoxItem
                {
                    Content = new DateTime(2024, i, 1).ToString("MMMM")
                });
            }
            cmbMonth.SelectedIndex = DateTime.Now.Month - 1;

            // سنوات
            int currentYear = DateTime.Now.Year;
            for (int i = currentYear - 5; i <= currentYear + 1; i++)
            {
                cmbYear.Items.Add(i);
            }
            cmbYear.SelectedItem = currentYear;
        }

        private async void BtnLoadEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMonth = cmbMonth.SelectedIndex + 1;
                _currentYear = (int)cmbYear.SelectedItem;

                _employees.Clear();

                // تحميل الموظفين مع بياناتهم
                var users = await _context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Salaries)
                    .Include(u => u.Loans)
                    .Where(u => App.userBranches.Contains(u.BranchId))
                    .ToListAsync();

                foreach (var user in users)
                {
                    var viewModel = new EmployeeSalaryViewModel
                    {
                        Id = user.Id,
                        Code = user.Code,
                        Name = user.FullName,
                        Branch = user.Branch?.Name,
                        IsSelected = false
                    };

                    // حساب الراتب
                    await CalculateEmployeeSalary(viewModel, user);

                    _employees.Add(viewModel);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CalculateEmployeeSalary(EmployeeSalaryViewModel viewModel, User user)
        {
            try
            {
                // الراتب الأساسي
                var basicSalary = user.Salaries?.FirstOrDefault(s => s.Type == 1);
                viewModel.BasicSalary = basicSalary?.Amount ?? 0;

                // الإضافات
                viewModel.Additions = CalculateAdditions(user);

                // الاستقطاعات (بدون صندوق الزمالة)
                viewModel.Deductions = CalculateDeductions(user);

                // صندوق الزمالة
                var friendshipBoxSalary = user.Salaries?.FirstOrDefault(s => s.Type == 13);
                viewModel.FriendshipBoxAmount = friendshipBoxSalary?.Amount ?? 0;


                // السلف المستحقة
                viewModel.LoanDeduction = await CalculateLoanDeduction(user);

                // صافي الراتب
                viewModel.NetSalary = (viewModel.BasicSalary + viewModel.Additions) -
                                      (viewModel.Deductions + viewModel.FriendshipBoxAmount + viewModel.LoanDeduction);

                // التحقق إذا تم صرف الراتب لهذا الشهر
                var existingPayment = await _context.SalaryPayments
                    .FirstOrDefaultAsync(sp => sp.UserId == user.Id &&
                                              sp.Month == _currentMonth &&
                                              sp.Year == _currentYear);

                viewModel.PaymentStatus = existingPayment?.IsPaid == true ? "تم الصرف" : "لم يصرف";
                viewModel.IsAlreadyPaid = existingPayment?.IsPaid == true;

                if (viewModel.IsAlreadyPaid)
                {
                    viewModel.IsSelected = false; // لا نختار الموظفين الذين تم صرف رواتبهم
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب راتب الموظف {user.FullName}: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateAdditions(User user)
        {
            decimal additions = 0;
            var salaries = user.Salaries;

            if (salaries != null)
            {
                // بدل سكن
                additions += salaries.Where(s => s.Type == 2).Sum(s => s.Amount);
                // بدل انتقال
                additions += salaries.Where(s => s.Type == 3).Sum(s => s.Amount);
                // بدل إدارة
                additions += salaries.Where(s => s.Type == 14).Sum(s => s.Amount);
                // بدل طبيعة عمل
                additions += salaries.Where(s => s.Type == 15).Sum(s => s.Amount);
                // مكافآت
                additions += salaries.Where(s => s.Type == 11).Sum(s => s.Amount);
                // عمولات تحقيق
                additions += salaries.Where(s => s.Type == 18).Sum(s => s.Amount);
                // عمولات خارجية
                additions += salaries.Where(s => s.Type == 19).Sum(s => s.Amount);
            }

            return additions;
        }

        private decimal CalculateDeductions(User user)
        {
            decimal deductions = 0;
            var salaries = user.Salaries;

            if (salaries != null)
            {
                // ضريبة كسب العمل
                deductions += salaries.Where(s => s.Type == 5).Sum(s => s.Amount);
                // تأمينات الموظف
                deductions += salaries.Where(s => s.Type == 4).Sum(s => s.Amount);
                // تأمينات الشركة
                deductions += salaries.Where(s => s.Type == 16).Sum(s => s.Amount);
                // مشاركة اجتماعية
                deductions += salaries.Where(s => s.Type == 6).Sum(s => s.Amount);
                // جزاءات
                deductions += salaries.Where(s => s.Type == 10).Sum(s => s.Amount);
                // فاتورة تليفون
                deductions += salaries.Where(s => s.Type == 20).Sum(s => s.Amount);
                // عجز
                deductions += salaries.Where(s => s.Type == 16).Sum(s => s.Amount);
            }

            return deductions;
        }

        private async Task<decimal> CalculateLoanDeduction(User user)
        {
            decimal loanDeduction = 0;

            // الحصول على السلف النشطة
            var activeLoans = await _context.Loans
                .Where(l => l.UserId == user.Id &&
                           l.Status == "Approved" &&
                           l.RemainingAmount > 0)
                .ToListAsync();

            foreach (var loan in activeLoans)
            {
                loanDeduction += loan.MonthlyInstallment;
            }

            return loanDeduction;
        }

        private void UpdateSummary()
        {
            int selectedCount = _employees.Count(e => e.IsSelected && !e.IsAlreadyPaid);
            int totalCount = _employees.Count(e => !e.IsAlreadyPaid);
            decimal totalNetSalary = _employees.Where(e => e.IsSelected && !e.IsAlreadyPaid).Sum(e => e.NetSalary);
            decimal totalFriendshipBox = _employees.Where(e => e.IsSelected && !e.IsAlreadyPaid).Sum(e => e.FriendshipBoxAmount);
            decimal totalLoanDeduction = _employees.Where(e => e.IsSelected && !e.IsAlreadyPaid).Sum(e => e.LoanDeduction);

            txtSummary.Text = $"عدد المحددين: {selectedCount} من {totalCount} | إجمالي الصافي: {totalNetSalary:N2} | إجمالي السلف: {totalLoanDeduction:N2}";
            txtFriendshipBoxSummary.Text = $"إجمالي صندوق الزمالة: {totalFriendshipBox:N2}";
        }

        private async void BtnPaySelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployees = _employees.Where(emp => emp.IsSelected && !emp.IsAlreadyPaid).ToList();

            if (!selectedEmployees.Any())
            {
                MessageBox.Show("الرجاء تحديد موظفين لصرف رواتبهم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من صرف رواتب {selectedEmployees.Count} موظف؟\n" +
                $"إجمالي المبلغ: {selectedEmployees.Sum(e => e.NetSalary):N2}",
                "تأكيد الصرف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var emp in selectedEmployees)
                    {
                        await ProcessSalaryPayment(emp);
                    }

                    await _context.SaveChangesAsync();

                    MessageBox.Show($"تم صرف رواتب {selectedEmployees.Count} موظف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    // تحديث البيانات
                    await RefreshEmployeeData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في صرف الرواتب: {ex.InnerException}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // في BulkSalaryPaymentWindow.cs - تعديل دالة ProcessSalaryPayment
        private async Task ProcessSalaryPayment(EmployeeSalaryViewModel emp)
        {
            var user = await _context.Users.FindAsync(emp.Id);
            if (user == null) return;


            // إنشاء سجل صرف الراتب
            var salaryPayment = new SalaryPayment
            {
                UserId = user.Id,
                Month = _currentMonth,
                Year = _currentYear,
                BasicSalary = emp.BasicSalary,
                HousingAllowance = GetAllowanceAmount(user, 2),
                TransportationAllowance = GetAllowanceAmount(user, 3),
                ManagementAllowance = GetAllowanceAmount(user, 14),
                NatureAllowance = GetAllowanceAmount(user, 15),
                OvertimeAmount = 0, // يمكن إضافته من بيانات الحضور
                Rewards = GetAllowanceAmount(user, 11),
                TargetCommission = GetAllowanceAmount(user, 18),
                ExternalCommission = GetAllowanceAmount(user, 19),
                AbsenceDeduction = GetDeductionAmount(user, 12),
                LateDeduction = 0, // يمكن إضافته من بيانات الحضور
                LoanDeduction = emp.LoanDeduction,
                PenaltyDeduction = GetDeductionAmount(user, 10),
                TaxDeduction = GetDeductionAmount(user, 5),
                InsuranceDeduction = GetDeductionAmount(user, 4),
                SocialParticipation = GetDeductionAmount(user, 6),
                FriendshipBoxDeduction = emp.FriendshipBoxAmount, // هذا فقط للتسجيل
                TotalAdditions = emp.Additions,
                TotalDeductions = emp.Deductions + emp.FriendshipBoxAmount + emp.LoanDeduction,
                NetSalary = emp.NetSalary,
                PaymentDate = DateTime.Now,
                IsPaid = true,
                ActualPaymentDate = DateTime.Now,
                Notes = "صرف جماعي",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _context.SalaryPayments.AddAsync(salaryPayment);
            await _context.SaveChangesAsync();
            salaryPayment = _context.SalaryPayments.FirstOrDefault(sp => sp.Month == _currentMonth && sp.Year == _currentYear && sp.UserId == user.Id);

            // استخدام خدمة صندوق الزمالة
            var friendshipBoxService = new FriendshipBoxService(_context);

            // تسجيل الإيداع في صندوق الزمالة المشترك
            await friendshipBoxService.RecordDepositAsync(
                user.Id,
                emp.FriendshipBoxAmount,
                salaryPayment.Id, // سيتم تحديثه عند إنشاء سجل صرف الراتب
                $"خصم صندوق زمالة من راتب {_currentMonth}/{_currentYear}"
            );

            // تحديث رصيد السلف
            var activeLoans = await _context.Loans
                .Where(l => l.UserId == user.Id && l.Status == "Approved" && l.RemainingAmount > 0)
                .ToListAsync();

            foreach (var loan in activeLoans)
            {
                decimal paymentAmount = Math.Min(loan.RemainingAmount, loan.MonthlyInstallment);
                loan.RemainingAmount -= paymentAmount;
                loan.AmountPaid += paymentAmount;

                // إذا تم سداد كامل السلفة
                if (loan.RemainingAmount <= 0)
                {
                    loan.Status = "Paid";
                    loan.ActualPaybackDate = DateTime.Now;

                    // تسجيل السداد في صندوق الزمالة
                    await friendshipBoxService.RecordRepaymentAsync(
                        user.Id,
                        paymentAmount,
                        salaryPayment.Id, // سيتم تحديثه عند إنشاء سجل سداد السلف
                        $"سداد سلفة كاملة - قرض رقم {loan.Id}"
                    );
                }
                else
                {
                    loan.Status = "PartiallyPaid";

                    // تسجيل السداد الجزئي في صندوق الزمالة
                    await friendshipBoxService.RecordRepaymentAsync(
                        user.Id,
                        paymentAmount,
                        salaryPayment.Id, // سيتم تحديثه عند إنشاء سجل سداد السلف
                        $"سداد قسط سلفة - قرض رقم {loan.Id}"
                    );
                }

                loan.UpdatedAt = DateTime.Now;

                // تسجيل دفعة السلف
                var loanPayment = new LoanPayment
                {
                    LoanId = loan.Id,
                    PaymentAmount = paymentAmount,
                    PaymentDate = DateTime.Now,
                    PaymentType = "Monthly",
                    Notes = $"دفعة شهرية من الراتب - {_currentMonth}/{_currentYear}",
                    CreatedAt = DateTime.Now
                };

                await _context.LoanPayments.AddAsync(loanPayment);
            }

            // تحديث رصيد السلف للموظف
            user.CurrentLoanBalance = activeLoans.Sum(l => l.RemainingAmount);

            // إذا انخفض رصيد السلف عن الحد الأقصى، السماح بأخذ سلفات جديدة
            var basicSalary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Type == 1);

            if (basicSalary != null)
            {
                decimal maxLoan = basicSalary.Amount * 0.5m;
                if (user.CurrentLoanBalance < maxLoan)
                {
                    user.CanTakeLoan = true;
                }
            }

            await _context.SaveChangesAsync(); // حفظ أولي للحصول على ID

            // تحديث حركة صندوق الزمالة برقم صرف الراتب
            var depositTransaction = await _context.FriendshipBoxTransactions
                .FirstOrDefaultAsync(t => t.UserId == user.Id &&
                                         t.TransactionType == "Deposit" &&
                                         t.Description.Contains($"خصم صندوق زمالة من راتب {_currentMonth}/{_currentYear}"));

            if (depositTransaction != null)
            {
                depositTransaction.SalaryPaymentId = salaryPayment.Id;
                await _context.SaveChangesAsync();
            }

            // تحديث حالة الموظف في العرض
            emp.PaymentStatus = "تم الصرف";
            emp.IsAlreadyPaid = true;
        }

        private decimal GetAllowanceAmount(User user, int type)
        {
            return user.Salaries?.Where(s => s.Type == type).Sum(s => s.Amount) ?? 0;
        }

        private decimal GetDeductionAmount(User user, int type)
        {
            return user.Salaries?.Where(s => s.Type == type).Sum(s => s.Amount) ?? 0;
        }

        private async Task RefreshEmployeeData()
        {
            foreach (var emp in _employees)
            {
                var user = await _context.Users
                    .Include(u => u.Salaries)
                    .Include(u => u.Loans)
                    .FirstOrDefaultAsync(u => u.Id == emp.Id);

                if (user != null)
                {
                    await CalculateEmployeeSalary(emp, user);
                }
            }

            UpdateSummary();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var emp in _employees.Where(e => !e.IsAlreadyPaid))
            {
                emp.IsSelected = true;
            }
            UpdateSummary();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var emp in _employees)
            {
                emp.IsSelected = false;
            }
            UpdateSummary();
        }

        private void BtnCalculateAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var emp in _employees)
            {
                // إعادة حساب صندوق الزمالة للنسبة الجديدة
                decimal friendshipBoxPercentage = 0.02m;
                if (decimal.TryParse(txtFriendshipBoxPercentage.Text, out decimal percentage))
                {
                    friendshipBoxPercentage = percentage / 100m;
                }

                emp.FriendshipBoxAmount = (emp.BasicSalary + emp.Additions) * friendshipBoxPercentage;
                emp.NetSalary = (emp.BasicSalary + emp.Additions) -
                               (emp.Deductions + emp.FriendshipBoxAmount + emp.LoanDeduction);
            }

            UpdateSummary();
        }

        private void BtnApplyPercentage_Click(object sender, RoutedEventArgs e)
        {
            BtnCalculateAll_Click(sender, e);
        }

        private void BtnPayAll_Click(object sender, RoutedEventArgs e)
        {
            // تحديد جميع الموظفين الذين لم يصرفوا رواتبهم
            foreach (var emp in _employees.Where(e => !e.IsAlreadyPaid))
            {
                emp.IsSelected = true;
            }

            BtnPaySelected_Click(sender, e);
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployees = _employees.Where(emp => emp.IsSelected && !emp.IsAlreadyPaid).ToList();

            if (!selectedEmployees.Any())
            {
                MessageBox.Show("الرجاء تحديد موظفين للمعاينة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewWindow = new SalaryPaymentPreviewWindow(selectedEmployees, _currentMonth, _currentYear);
            previewWindow.ShowDialog();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // تصدير البيانات إلى Excel
            var exportWindow = new SalaryExportWindow(_employees.ToList(), _currentMonth, _currentYear);
            exportWindow.ShowDialog();
        }
    }

    public class EmployeeSalaryViewModel : BaseViewModel
    {
        private bool _isSelected;
        private decimal _friendshipBoxAmount;
        private decimal _netSalary;

        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Branch { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Additions { get; set; }
        public decimal Deductions { get; set; }
        public decimal LoanDeduction { get; set; }

        public decimal FriendshipBoxAmount
        {
            get => _friendshipBoxAmount;
            set
            {
                _friendshipBoxAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal NetSalary
        {
            get => _netSalary;
            set
            {
                _netSalary = value;
                OnPropertyChanged();
            }
        }

        public string PaymentStatus { get; set; }
        public bool IsAlreadyPaid { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public class BaseViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}