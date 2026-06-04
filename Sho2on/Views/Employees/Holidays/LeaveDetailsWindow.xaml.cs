using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Globalization;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class LeaveDetailsWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Leave _leave;
        private readonly int _leaveId;

        public LeaveDetailsWindow(int leaveId)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _leaveId = leaveId;
            Loaded += LeaveDetailsWindow_Loaded;
        }

        public LeaveDetailsWindow(Leave leave)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _leave = leave;
            _leaveId = leave.Id;
            LoadLeaveDetails(_leave);
        }

        private async void LeaveDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLeaveDetailsAsync();
        }

        private async System.Threading.Tasks.Task LoadLeaveDetailsAsync()
        {
            try
            {
                var leave = await _context.Leaves
                    .Include(l => l.User)
                    .ThenInclude(u => u.Department)
                    .Include(l => l.User)
                    .ThenInclude(u => u.Branch)
                    .Include(l => l.User)
                    .ThenInclude(u => u.WeekHoliday)
                    .Include(l => l.LeaveType)
                    .Include(l => l.Approver)
                    .FirstOrDefaultAsync(l => l.Id == _leaveId);

                if (leave != null)
                {
                    LoadLeaveDetails(leave);
                }
                else
                {
                    LocalizationManager.ShowMessage("لم يتم العثور على طلب الإجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل التفاصيل: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void LoadLeaveDetails(Leave leave)
        {
            try
            {
                // رقم الطلب
                txtRequestId.Text = leave.Id.ToString();

                // معلومات الموظف
                txtEmployeeId.Text = leave.User?.Id.ToString() ?? LocalizationManager.Translate("غير متوفر");
                txtEmployeeName.Text = leave.User?.FullName ?? LocalizationManager.Translate("غير متوفر");
                txtDepartment.Text = leave.User?.Department?.Name ?? LocalizationManager.Translate("غير متوفر");
                txtBranch.Text = leave.User?.Branch?.Name ?? LocalizationManager.Translate("غير متوفر");
                txtHireDate.Text = leave.User?.HireDate.ToString("yyyy/MM/dd") ?? LocalizationManager.Translate("غير متوفر");
                txtWeekend.Text = GetWeekendDaysSummary(leave.User?.WeekHoliday) ?? LocalizationManager.Translate("غير متوفر");

                // معلومات الإجازة
                txtLeaveType.Text = leave.LeaveType?.Name ?? LocalizationManager.Translate("غير متوفر");
                txtStartDate.Text = leave.StartDate.ToString("yyyy/MM/dd");
                txtEndDate.Text = leave.EndDate.ToString("yyyy/MM/dd");
                txtDuration.Text = $"{leave.Duration} يوم";
                txtRequestDate.Text = leave.RequestDate.ToString("yyyy/MM/dd HH:mm");
                txtBalanceEffect.Text = leave.LeaveType?.DeductFromBalance == true ? LocalizationManager.Translate("يخصم من الرصيد") : LocalizationManager.Translate("لا يخصم من الرصيد");
                txtReason.Text = leave.Reason ?? LocalizationManager.Translate("لا يوجد سبب");

                // حالة الطلب
                var statusInfo = GetStatusInfo(leave.Status);
                txtStatus.Text = statusInfo.Text;
                txtStatus.Foreground = new SolidColorBrush(statusInfo.Color);
                txtApprovalDate.Text = leave.ApprovalDate?.ToString("yyyy/MM/dd HH:mm") ?? LocalizationManager.Translate("لم تتم الموافقة بعد");
                txtApprovedBy.Text = leave.Approver?.FullName ?? LocalizationManager.Translate("لم تتم الموافقة بعد");
                txtRejectionReason.Text = leave.RejectionReason ?? LocalizationManager.Translate("لا يوجد");

                // تحميل معلومات الرصيد
                LoadBalanceInfo(leave.UserId, leave.LeaveTypeId);

                // ملاحظات
                txtNotes.Text = leave.Notes ?? LocalizationManager.Translate("لا توجد ملاحظات");
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في عرض التفاصيل: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetWeekendDaysSummary(WeekHoliday weekHoliday)
        {
            if (weekHoliday == null) return LocalizationManager.Translate("غير محدد");

            var days = new System.Collections.Generic.List<string>();

            if (weekHoliday.Day1) days.Add(LocalizationManager.Translate("السبت"));
            if (weekHoliday.Day2) days.Add(LocalizationManager.Translate("الأحد"));
            if (weekHoliday.Day3) days.Add(LocalizationManager.Translate("الإثنين"));
            if (weekHoliday.Day4) days.Add(LocalizationManager.Translate("الثلاثاء"));
            if (weekHoliday.Day5) days.Add(LocalizationManager.Translate("الأربعاء"));
            if (weekHoliday.Day6) days.Add(LocalizationManager.Translate("الخميس"));
            if (weekHoliday.Day7) days.Add(LocalizationManager.Translate("الجمعة"));

            return days.Count > 0 ? string.Join(LocalizationManager.Translate("، "), days) : LocalizationManager.Translate("لا توجد أيام إجازة");
        }

        private async void LoadBalanceInfo(int userId, int leaveTypeId)
        {
            try
            {
                // الحصول على رصيد الإجازة
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);

                int totalBalance = leaveBalance?.TotalBalance ?? leaveType?.DefaultBalance ?? 0;

                // حساب الإجازات المستخدمة (الموافق عليها فقط)
                var usedLeaves = await _context.Leaves
                    .Where(l => l.UserId == userId &&
                               l.LeaveTypeId == leaveTypeId &&
                               l.Status == 2 && // الموافق عليها
                               !l.IsCancelled)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                int remainingBalance = totalBalance - usedLeaves;

                // تحديث العرض
                txtTotalBalance.Text = $"{totalBalance} يوم";
                txtUsedBalance.Text = $"{usedLeaves} يوم";
                txtRemainingBalance.Text = $"{remainingBalance} يوم";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل معلومات الرصيد: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Text, Color Color) GetStatusInfo(int status)
        {
            return status switch
            {
                0 => (LocalizationManager.Translate("مسودة"), Colors.Gray),
                1 => (LocalizationManager.Translate("قيد الانتظار"), Colors.Orange),
                2 => (LocalizationManager.Translate("موافق عليه"), Colors.Green),
                3 => (LocalizationManager.Translate("مرفوض"), Colors.Red),
                4 => (LocalizationManager.Translate("ملغى"), Colors.Purple),
                _ => (LocalizationManager.Translate("غير معروف"), Colors.Gray)
            };
        }

        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحميل البيانات الكاملة للطباعة
                var leave = await _context.Leaves
                    .Include(l => l.User)
                    .ThenInclude(u => u.Department)
                    .Include(l => l.User)
                    .ThenInclude(u => u.Branch)
                    .Include(l => l.User)
                    .ThenInclude(u => u.JobTitle)
                    .Include(l => l.User)
                    .ThenInclude(u => u.Shift)
                    .Include(l => l.User)
                    .ThenInclude(u => u.JobType)
                    .Include(l => l.LeaveType)
                    .Include(l => l.Approver)
                    .Include(l => l.Canceller)
                    .FirstOrDefaultAsync(l => l.Id == _leaveId);

                if (leave == null)
                {
                    LocalizationManager.ShowMessage("لم يتم العثور على طلب الإجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // الحصول على معلومات الرصيد
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.UserId == leave.UserId && lb.LeaveTypeId == leave.LeaveTypeId);

                var leaveType = await _context.LeaveTypes.FindAsync(leave.LeaveTypeId);

                int totalBalance = leaveBalance?.TotalBalance ?? leaveType?.DefaultBalance ?? 0;

                var usedLeaves = await _context.Leaves
                    .Where(l => l.UserId == leave.UserId &&
                               l.LeaveTypeId == leave.LeaveTypeId &&
                               l.Status == 2 &&
                               !l.IsCancelled)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                int remainingBalance = totalBalance - usedLeaves;

                // عرض خيارات الطباعة
                var printWindow = new LeavePrintOptionsWindow();
                printWindow.Owner = this;

                if (printWindow.ShowDialog() == true)
                {
                    // إنشاء كائن الطباعة
                    var printHelper = new LeavePrintHelper(leave, leave.User, leaveType,
                        totalBalance, usedLeaves, remainingBalance);

                    switch (printWindow.SelectedOption)
                    {
                        case PrintOption.Print:
                            printHelper.Print();
                            break;


                        case PrintOption.Preview:
                            var previewWindow = new PrintPreviewWindow(printHelper.CreatePrintDocument());
                            previewWindow.Owner = this;
                            previewWindow.ShowDialog();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في الطباعة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }


}
