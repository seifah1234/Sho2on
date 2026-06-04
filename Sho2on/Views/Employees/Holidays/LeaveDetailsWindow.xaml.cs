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
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·≈Ã«“…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «· ›«’Ì·: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void LoadLeaveDetails(Leave leave)
        {
            try
            {
                // —ﬁ„ «·ÿ·»
                txtRequestId.Text = leave.Id.ToString();

                // „⁄·Ê„«  «·„ÊŸ›
                txtEmployeeId.Text = leave.User?.Id.ToString() ?? "€Ì— „ Ê›—";
                txtEmployeeName.Text = leave.User?.FullName ?? "€Ì— „ Ê›—";
                txtDepartment.Text = leave.User?.Department?.Name ?? "€Ì— „ Ê›—";
                txtBranch.Text = leave.User?.Branch?.Name ?? "€Ì— „ Ê›—";
                txtHireDate.Text = leave.User?.HireDate.ToString("yyyy/MM/dd") ?? "€Ì— „ Ê›—";
                txtWeekend.Text = GetWeekendDaysSummary(leave.User?.WeekHoliday) ?? "€Ì— „ Ê›—";

                // „⁄·Ê„«  «·≈Ã«“…
                txtLeaveType.Text = leave.LeaveType?.Name ?? "€Ì— „ Ê›—";
                txtStartDate.Text = leave.StartDate.ToString("yyyy/MM/dd");
                txtEndDate.Text = leave.EndDate.ToString("yyyy/MM/dd");
                txtDuration.Text = $"{leave.Duration} ÌÊ„";
                txtRequestDate.Text = leave.RequestDate.ToString("yyyy/MM/dd HH:mm");
                txtBalanceEffect.Text = leave.LeaveType?.DeductFromBalance == true ? "ÌŒ’„ „‰ «·—’Ìœ" : "·« ÌŒ’„ „‰ «·—’Ìœ";
                txtReason.Text = leave.Reason ?? "·« ÌÊÃœ ”»»";

                // Õ«·… «·ÿ·»
                var statusInfo = GetStatusInfo(leave.Status);
                txtStatus.Text = statusInfo.Text;
                txtStatus.Foreground = new SolidColorBrush(statusInfo.Color);
                txtApprovalDate.Text = leave.ApprovalDate?.ToString("yyyy/MM/dd HH:mm") ?? "·„   „ «·„Ê«›ﬁ… »⁄œ";
                txtApprovedBy.Text = leave.Approver?.FullName ?? "·„   „ «·„Ê«›ﬁ… »⁄œ";
                txtRejectionReason.Text = leave.RejectionReason ?? "·« ÌÊÃœ";

                //  Õ„Ì· „⁄·Ê„«  «·—’Ìœ
                LoadBalanceInfo(leave.UserId, leave.LeaveTypeId);

                // „·«ÕŸ« 
                txtNotes.Text = leave.Notes ?? "·«  ÊÃœ „·«ÕŸ« ";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ⁄—÷ «· ›«’Ì·: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetWeekendDaysSummary(WeekHoliday weekHoliday)
        {
            if (weekHoliday == null) return "€Ì— „Õœœ";

            var days = new System.Collections.Generic.List<string>();

            if (weekHoliday.Day1) days.Add("«·”» ");
            if (weekHoliday.Day2) days.Add("«·√Õœ");
            if (weekHoliday.Day3) days.Add("«·≈À‰Ì‰");
            if (weekHoliday.Day4) days.Add("«·À·«À«¡");
            if (weekHoliday.Day5) days.Add("«·√—»⁄«¡");
            if (weekHoliday.Day6) days.Add("«·Œ„Ì”");
            if (weekHoliday.Day7) days.Add("«·Ã„⁄…");

            return days.Count > 0 ? string.Join("° ", days) : "·«  ÊÃœ √Ì«„ ≈Ã«“…";
        }

        private async void LoadBalanceInfo(int userId, int leaveTypeId)
        {
            try
            {
                // «·Õ’Ê· ⁄·Ï —’Ìœ «·≈Ã«“…
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);

                int totalBalance = leaveBalance?.TotalBalance ?? leaveType?.DefaultBalance ?? 0;

                // Õ”«» «·≈Ã«“«  «·„” Œœ„… («·„Ê«›ﬁ ⁄·ÌÂ« ›ﬁÿ)
                var usedLeaves = await _context.Leaves
                    .Where(l => l.UserId == userId &&
                               l.LeaveTypeId == leaveTypeId &&
                               l.Status == 2 && // «·„Ê«›ﬁ ⁄·ÌÂ«
                               !l.IsCancelled)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                int remainingBalance = totalBalance - usedLeaves;

                //  ÕœÌÀ «·⁄—÷
                txtTotalBalance.Text = $"{totalBalance} ÌÊ„";
                txtUsedBalance.Text = $"{usedLeaves} ÌÊ„";
                txtRemainingBalance.Text = $"{remainingBalance} ÌÊ„";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· „⁄·Ê„«  «·—’Ìœ: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string Text, Color Color) GetStatusInfo(int status)
        {
            return status switch
            {
                0 => ("„”Êœ…", Colors.Gray),
                1 => ("ﬁÌœ «·«‰ Ÿ«—", Colors.Orange),
                2 => ("„Ê«›ﬁ ⁄·ÌÂ", Colors.Green),
                3 => ("„—›Ê÷", Colors.Red),
                4 => ("„·€Ï", Colors.Purple),
                _ => ("€Ì— „⁄—Ê›", Colors.Gray)
            };
        }

        private async void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //  Õ„Ì· «·»Ì«‰«  «·ﬂ«„·… ··ÿ»«⁄…
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
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·≈Ã«“…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // «·Õ’Ê· ⁄·Ï „⁄·Ê„«  «·—’Ìœ
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

                // ⁄—÷ ŒÌ«—«  «·ÿ»«⁄…
                var printWindow = new LeavePrintOptionsWindow();
                printWindow.Owner = this;

                if (printWindow.ShowDialog() == true)
                {
                    // ≈‰‘«¡ ﬂ«∆‰ «·ÿ»«⁄…
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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·ÿ»«⁄…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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
