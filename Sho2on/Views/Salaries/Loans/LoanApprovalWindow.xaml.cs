using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Linq;
using System.Windows;
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
            return manager?.FullName ?? "غير معروف";
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

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "جميع الحالات")
                {
                    query = query.Where(l => l.Status == statusFilter);
                }

                var loans = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
                dgLoans.ItemsSource = loans;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("الرجاء اختيار سلفة للموافقة عليها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                MessageBox.Show("لا يمكن الموافقة على سلفة غير معلقة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من الموافقة على سلفة الموظف {_selectedLoan.User?.FullName} بمبلغ {_selectedLoan.LoanAmount:N2}؟",
                "تأكيد الموافقة",
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
                        MessageBox.Show($"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {balance:N2}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("تمت الموافقة على السلفة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                MessageBox.Show("الرجاء اختيار سلفة لرفضها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLoan.Status != "SentToManager")
            {
                MessageBox.Show("لا يمكن رفض سلفة غير معلقة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من رفض سلفة الموظف {_selectedLoan.User?.FullName}؟",
                "تأكيد الرفض",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _selectedLoan.Status = "Rejected";
                    _selectedLoan.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    MessageBox.Show("تم رفض السلفة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLoans();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLoan == null)
            {
                MessageBox.Show("الرجاء اختيار سلفة لعرض تفاصيلها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
/*
            var detailsWindow = new LoanDetailsWindow(_selectedLoan.Id);
            detailsWindow.ShowDialog();*/
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            string status = (cmbStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            LoadLoans(status == "جميع الحالات" ? null : status);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLoans();
        }
    }
}