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
            return manager?.FullName ?? LocalizationManager.Translate("غير معروف");
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

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != LocalizationManager.Translate("جميع الحالات"))
                {
                    query = query.Where(l => l.Status == statusFilter);
                }

                var loans = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
                dgLoans.ItemsSource = loans;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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

            string details = $"الموظف: {_selectedLoan.User?.FullName}\n" +
                            $"الكود: {_selectedLoan.User?.Code}\n" +
                            $"مبلغ السلفة: {_selectedLoan.LoanAmount:N2}\n" +
                            $"المبلغ المتبقي: {_selectedLoan.RemainingAmount:N2}\n" +
                            $"عدد الأقساط: {_selectedLoan.InstallmentCount}\n" +
                            $"القسط الشهري: {_selectedLoan.MonthlyInstallment:N2}\n" +
                            $"تاريخ الطلب: {_selectedLoan.LoanDate:yyyy-MM-dd}\n" +
                            $"تاريخ السداد المتوقع: {_selectedLoan.ExpectedPaybackDate:yyyy-MM-dd}\n" +
                            $"السبب: {_selectedLoan.Reason}\n" +
                            $"الحالة: {_selectedLoan.Status}";

            txtSelectedLoanDetails.Text = details;

        }

        // في LoanApprovalWindow.cs - تعديل دالة BtnApprove_Click
        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("الرجاء اختيار سلفة للموافقة عليها", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                LocalizationManager.ShowMessage("لا يمكن الموافقة على سلفة غير معلقة", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = LocalizationManager.ShowMessage(
                $"هل أنت متأكد من الموافقة على سلفة الموظف {_selectedLoan.User?.FullName} بمبلغ {_selectedLoan.LoanAmount:N2}؟",
                LocalizationManager.Translate("تأكيد الموافقة"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // استخدام خدمة صندوق الزمالة
                    var friendshipBoxService = new FriendshipBoxService(_context);

                    // التحقق من رصيد الصندوق
                    if (!await friendshipBoxService.CanWithdrawAsync(_selectedLoan.LoanAmount))
                    {
                        var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                        LocalizationManager.ShowMessage($"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {balance:N2}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // خصم المبلغ من صندوق الزمالة المشترك
                    await friendshipBoxService.RecordWithdrawalAsync(
                        _selectedLoan.UserId,
                        _selectedLoan.LoanAmount,
                        _selectedLoan.Id,
                        _selectedLoan.Reason);

                    // تحديث حالة السلفة
                    _selectedLoan.Status = "Approved";
                    _selectedLoan.ApprovedDate = DateTime.Now;
                    _selectedLoan.ApprovedByUserId = App.CurrentUser.Id; // افترض أن App.CurrentUserId موجود
                    _selectedLoan.UpdatedAt = DateTime.Now;

                    // تحديث رصيد السلف للموظف
                    var user = await _context.Users.FindAsync(_selectedLoan.UserId);
                    if (user != null)
                    {
                        user.CurrentLoanBalance += _selectedLoan.LoanAmount;

                        // إذا وصل رصيد السلف للحد الأقصى، منع أخذ سلفات جديدة
                        var basicSalary = await _context.Salaries
                            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Type == 1);

                        if (basicSalary != null)
                        {
                            decimal maxLoan = basicSalary.Amount * 0.5m; // 50% من الراتب
                            if (user.CurrentLoanBalance >= maxLoan)
                            {
                                user.CanTakeLoan = false;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تمت الموافقة على السلفة بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("الرجاء اختيار سلفة لرفضها", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                LocalizationManager.ShowMessage("لا يمكن رفض سلفة غير معلقة", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = LocalizationManager.ShowMessage(
                $"هل أنت متأكد من رفض سلفة الموظف {_selectedLoan.User?.FullName}؟",
                LocalizationManager.Translate("تأكيد الرفض"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _selectedLoan.Status = "Rejected";
                    _selectedLoan.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم رفض السلفة بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                LocalizationManager.ShowMessage("الرجاء اختيار سلفة لعرض تفاصيلها", LocalizationManager.Translate("تنبيه"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
/*
            var detailsWindow = new LoanDetailsWindow(_selectedLoan.Id);
            detailsWindow.ShowDialog();*/
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            string status = (cmbStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            LoadLoans(status == LocalizationManager.Translate("جميع الحالات") ? null : status);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLoans();
        }
    }
}
