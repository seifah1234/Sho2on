using Microsoft.EntityFrameworkCore;
using HR_Application.ViewModels;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class LeaveManagementWindow : Window
    {
        private readonly AppDbContext _context;
        private List<LeaveViewModel> _leaves = new List<LeaveViewModel>();
        private List<LeaveViewModel> _ownLeaves = new List<LeaveViewModel>();
        private List<LeaveType> _leaveTypes = new List<LeaveType>();

        public LeaveManagementWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                //  Õ„Ì· √‰Ê«⁄ «·≈Ã«“« 
                _leaveTypes = await _context.LeaveTypes
                    .Where(lt => lt.IsActive)
                    .OrderBy(lt => lt.Name)
                    .ToListAsync();

                cmbLeaveType.Items.Clear();
                cmbOwnLeaveType.Items.Clear();
                cmbLeaveType.Items.Add(new ComboBoxItem { Content = "Ã„Ì⁄ «·√‰Ê«⁄", Tag = -1 });
                cmbOwnLeaveType.Items.Add(new ComboBoxItem { Content = "Ã„Ì⁄ «·√‰Ê«⁄", Tag = -1 });

                foreach (var leaveType in _leaveTypes)
                {
                    cmbLeaveType.Items.Add(new ComboBoxItem
                    {
                        Content = leaveType.Name,
                        Tag = leaveType.Id
                    });
                    cmbOwnLeaveType.Items.Add(new ComboBoxItem
                    {
                        Content = leaveType.Name,
                        Tag = leaveType.Id
                    });
                }

                if (cmbLeaveType.Items.Count > 0)
                    cmbLeaveType.SelectedIndex = 0;

                if (cmbOwnLeaveType.Items.Count > 0)
                    cmbOwnLeaveType.SelectedIndex = 0;

                List<StatusType> statuses = new List<StatusType>
                {
                    new StatusType{Name = "Ã„Ì⁄ «·Õ«·« " , Code = -1},
                    new StatusType{Name = "„”Êœ…" , Code = 0},
                    new StatusType{Name = "ﬁÌœ «·«‰ Ÿ«—" , Code = 1},
                    new StatusType{Name = "„Ê«›ﬁ ⁄·ÌÂ" , Code = 2},
                    new StatusType{Name = "„—›Ê÷" , Code = 3},
                    new StatusType{Name = "„·€Ï" , Code = 4},
                };

                cmbStatus.ItemsSource = statuses;
                cmbOwnStatus.ItemsSource = statuses;

                await LoadLeaves();

                if ((!App.CurrentUser.Department.IsHR.HasValue || !App.CurrentUser.Department.IsHR.Value) &&
                   (!App.CurrentUser.JobTitle.IsManager.HasValue || !App.CurrentUser.JobTitle.IsManager.Value))
                {
                    employeesTab.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class StatusType
        {
            public string Name { get; set; }
            public int Code { get; set; }
        }

        private async System.Threading.Tasks.Task LoadLeaves()
        {
            try
            {
                var query = _context.Leaves
                    .Include(l => l.User)
                    .Include(l => l.ReplacementUser)
                    .Include(l => l.LeaveType)
                    .Where(l => l.ApprovedBy == App.CurrentUser.Id ||
                               (l.Status == 5 && App.CurrentUser.Department.IsHR == true))
                    .AsQueryable();

                //  ÿ»Ìﬁ «·›·« —
                if (!string.IsNullOrEmpty(txtEmployeeId.Text))
                {
                    query = query.Where(l => l.User.Code == txtEmployeeId.Text);
                }

                if (cmbLeaveType.SelectedItem is ComboBoxItem selectedLeaveType &&
                    selectedLeaveType.Tag is int leaveTypeId && leaveTypeId > 0)
                {
                    query = query.Where(l => l.LeaveTypeId == leaveTypeId);
                }

                if (dpFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(l => l.StartDate.Date >= dpFromDate.SelectedDate.Value);
                }

                if (dpToDate.SelectedDate.HasValue)
                {
                    query = query.Where(l => l.EndDate.Date <= dpToDate.SelectedDate.Value);
                }

                if (cmbStatus.SelectedItem is StatusType selectedStatus && selectedStatus.Code >= 0)
                {
                    query = query.Where(l => l.Status == selectedStatus.Code);
                }

                //  ‰›Ì– «·«” ⁄·«„
                var leaves = await query
                    .OrderByDescending(l => l.RequestDate)
                    .ToListAsync();

                //  ÕÊÌ· ≈·Ï ViewModel
                _leaves = leaves.Select(l => new LeaveViewModel
                {
                    Id = l.Id,
                    EmployeeId = l.UserId,
                    EmployeeName = l.User?.FullName ?? "€Ì— „⁄—Ê›",
                    ReplacementUserName = l.ReplacementUser?.FullName ?? "·« ÌÊÃœ",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType?.Name ?? "€Ì— „⁄—Ê›",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Duration = l.Duration,
                    Reason = l.Reason,
                    StatusId = l.Status,
                    Status = GetStatusText(l.Status),
                    RequestDate = l.RequestDate
                }).ToList();

                dgLeaves.ItemsSource = _leaves;

                query = _context.Leaves
                    .Include(l => l.User)
                    .Include(l => l.ReplacementUser)
                    .Include(l => l.LeaveType)
                    .Where(l => l.UserId == App.CurrentUser.Id)
                    .AsQueryable();


                if (cmbOwnLeaveType.SelectedItem is ComboBoxItem selectedOwnLeaveType &&
                    selectedOwnLeaveType.Tag is int leaveTypeOwnId && leaveTypeOwnId > 0)
                {
                    query = query.Where(l => l.LeaveTypeId == leaveTypeOwnId);
                }

                if (dpOwnFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(l => l.StartDate.Date >= dpOwnFromDate.SelectedDate.Value);
                }

                if (dpOwnToDate.SelectedDate.HasValue)
                {
                    query = query.Where(l => l.EndDate.Date <= dpOwnToDate.SelectedDate.Value);
                }

                if (cmbOwnStatus.SelectedItem is StatusType selectedOwnStatus && selectedOwnStatus.Code >= 0)
                {
                    query = query.Where(l => l.Status == selectedOwnStatus.Code);
                }

                //  ‰›Ì– «·«” ⁄·«„
                leaves = await query
                    .OrderByDescending(l => l.RequestDate)
                    .ToListAsync();

                //  ÕÊÌ· ≈·Ï ViewModel
                _ownLeaves = leaves.Select(l => new LeaveViewModel
                {
                    Id = l.Id,
                    EmployeeId = l.UserId,
                    EmployeeName = l.User?.FullName ?? "€Ì— „⁄—Ê›",
                    ReplacementUserName = l.ReplacementUser?.FullName ?? "·« ÌÊÃœ",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType?.Name ?? "€Ì— „⁄—Ê›",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Duration = l.Duration,
                    Reason = l.Reason,
                    StatusId = l.Status,
                    Status = GetStatusText(l.Status),
                    RequestDate = l.RequestDate
                }).ToList();

                dgOwnLeaves.ItemsSource = _ownLeaves;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· ÿ·»«  «·≈Ã«“…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "„”Êœ…",
                1 => "ﬁÌœ «·«‰ Ÿ«—",
                2 => "„Ê«›ﬁ ⁄·ÌÂ",
                3 => "„—›Ê÷",
                4 => "„·€Ï",
                5 => " Õ  «·„—«Ã⁄…",
                _ => "€Ì— „⁄—Ê›"
            };
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await LoadLeaves();
        }


        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtEmployeeId.Text = string.Empty;
            cmbLeaveType.SelectedIndex = 0;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            cmbStatus.SelectedIndex = 0;
        }

        private async void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int leaveId)
            {
                await ProcessLeaveApproval(leaveId, true);
            }
        }

        private async void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int leaveId)
            {
                await ProcessLeaveApproval(leaveId, false);
            }
        }

        private async System.Threading.Tasks.Task ProcessLeaveApproval(int leaveId, bool isApprove)
        {
            try
            {
                var leave = await _context.Leaves
                    .Include(l => l.User)
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == leaveId);

                if (leave == null)
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·≈Ã«“…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isApprove)
                {
                    if (App.CurrentUser.Department != null && App.CurrentUser.Department.IsHR.HasValue && App.CurrentUser.Department.IsHR.Value)
                    {
                        leave.Status = 2;
                        // «· Õﬁﬁ „‰ «·—’Ìœ «·„ »ﬁÌ
                        var balance = await GetLeaveBalance(leave.UserId, leave.LeaveTypeId);
                        if (balance.Remaining < leave.Duration && leave.LeaveType.DeductFromBalance)
                        {
                            LocalizationManager.ShowMessage($"«·—’Ìœ «·„ »ﬁÌ €Ì— ﬂ«›Ì. «·„ »ﬁÌ: {balance.Remaining} ÌÊ„",
                                " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        //  ÕœÌÀ «·Õ«·…
                        leave.Status = 2; // Approved
                        leave.ApprovalDate = DateTime.Now;

                        // Œ’„ „‰ «·—’Ìœ ≈–« ﬂ«‰ «·‰Ê⁄ ÌŒ’„
                        if (leave.LeaveType.DeductFromBalance)
                        {
                            await DeductLeaveBalance(leave.UserId, leave.LeaveTypeId, leave.Duration);
                        }

                        //  ÕœÌÀ ”Ã·«  «·Õ÷Ê—
                        await UpdateAttendanceForLeave(leave);
                    }
                    else
                    {
                        leave.Status = 5;
                        leave.ApprovedBy = App.CurrentUser?.Id; // «Õ’· ⁄·Ï «·„” Œœ„ «·Õ«·Ì
                    }

                    LocalizationManager.ShowMessage(" „ «·„Ê«›ﬁ… ⁄·Ï ÿ·» «·≈Ã«“…", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ÿ·» ”»» «·—›÷
                    var reasonWindow = new RejectionReasonWindow();
                    reasonWindow.Owner = this;

                    if (reasonWindow.ShowDialog() == true)
                    {
                        leave.Status = 3; // Rejected
                        leave.RejectionReason = reasonWindow.RejectionReason;
                        leave.ApprovalDate = DateTime.Now;
                        leave.ApprovedBy = App.CurrentUser?.Id;

                        LocalizationManager.ShowMessage(" „ —›÷ ÿ·» «·≈Ã«“…", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        return;
                    }
                }

                await _context.SaveChangesAsync();
                await LoadLeaves();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì „⁄«·Ã… «·ÿ·»: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int leaveId)
            {
                await CancelLeaveRequest(leaveId);
            }
        }

        private async System.Threading.Tasks.Task CancelLeaveRequest(int leaveId)
        {
            try
            {
                var leave = await _context.Leaves
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == leaveId);

                if (leave == null)
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·≈Ã«“…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (LocalizationManager.ShowMessage("Â·  —Ìœ ≈·€«¡ Â–Â «·≈Ã«“…ø", " √ﬂÌœ «·≈·€«¡",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                // ≈—Ã«⁄ «·—’Ìœ ≈–« ﬂ«‰  „Ê«›ﬁ ⁄·ÌÂ« Ê Œ’„ „‰ «·—’Ìœ
                if (leave.Status == 2 && leave.LeaveType.DeductFromBalance)
                {
                    await ReturnLeaveBalance(leave.UserId, leave.LeaveTypeId, leave.Duration);
                }

                //  ÕœÌÀ «·Õ«·…
                leave.Status = 4; // Cancelled
                leave.IsCancelled = true;
                leave.CancelledDate = DateTime.Now;
                leave.CancelledBy = App.CurrentUser?.Id;

                // ≈⁄«œ…  ÕœÌÀ ”Ã·«  «·Õ÷Ê—
                await ResetAttendanceForLeave(leave);

                await _context.SaveChangesAsync();
                await LoadLeaves();

                LocalizationManager.ShowMessage(" „ ≈·€«¡ «·≈Ã«“… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ≈·€«¡ «·≈Ã«“…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                if (button.Tag is Tuple<int, int> data)
                {
                    var leaveId = data.Item1;
                    await ShowLeaveDetails(leaveId);
                }
                else if (button.Tag is Tuple<object, object> dataObj)
                {
                    // Handle as objects and try to convert
                    if (dataObj.Item1 is int leaveId)
                    {
                        await ShowLeaveDetails(leaveId);
                    }
                    else if (int.TryParse(dataObj.Item1?.ToString(), out leaveId))
                    {
                        await ShowLeaveDetails(leaveId);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task ShowLeaveDetails(int leaveId)
        {
            try
            {
                var leave = await _context.Leaves
                    .Include(l => l.User)
                    .ThenInclude(l => l.Branch)
                    .Include(l => l.User)
                    .ThenInclude(l => l.Department)
                    .Include(l => l.User)
                    .ThenInclude(l => l.WeekHoliday)
                    .Include(l => l.LeaveType)
                    .Include(l => l.Approver)
                    .FirstOrDefaultAsync(l => l.Id == leaveId);

                if (leave != null)
                {
                    var detailsWindow = new LeaveDetailsWindow(leave);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ⁄—÷ «· ›«’Ì·: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<LeaveBalanceInfo> GetLeaveBalance(int userId, int leaveTypeId)
        {
            // «Õ’· ⁄·Ï —’Ìœ «·≈Ã«“… ·Â–« «·„ÊŸ› ÊÂ–« «·‰Ê⁄
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

            if (leaveBalance == null)
            {
                // ≈‰‘«¡ —’Ìœ ÃœÌœ ≈–« ·„ Ìﬂ‰ „ÊÃÊœ«
                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                return new LeaveBalanceInfo
                {
                    Total = leaveType?.DefaultBalance ?? 0,
                    Used = 0,
                    Remaining = leaveType?.DefaultBalance ?? 0
                };
            }

            // Õ”«» «·≈Ã«“«  «·„” Œœ„…
            var usedLeaves = await _context.Leaves
                .Where(l => l.UserId == userId &&
                           l.LeaveTypeId == leaveTypeId &&
                           l.Status == 2) // «·„Ê«›ﬁ ⁄·ÌÂ« ›ﬁÿ
                .SumAsync(l => l.Duration);

            return new LeaveBalanceInfo
            {
                Total = leaveBalance.TotalBalance,
                Used = usedLeaves,
                Remaining = leaveBalance.TotalBalance - usedLeaves
            };
        }

        private async System.Threading.Tasks.Task DeductLeaveBalance(int userId, int leaveTypeId, int days)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

            if (leaveBalance == null)
            {
                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                leaveBalance = new LeaveBalance
                {
                    UserId = userId,
                    LeaveTypeId = leaveTypeId,
                    TotalBalance = leaveType?.DefaultBalance ?? 0,
                    UsedBalance = days,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.LeaveBalances.Add(leaveBalance);
            }
            else
            {
                leaveBalance.UsedBalance += days;
                leaveBalance.UpdatedAt = DateTime.Now;

            }
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task ReturnLeaveBalance(int userId, int leaveTypeId, int days)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

            if (leaveBalance != null)
            {
                leaveBalance.UsedBalance -= days;
                leaveBalance.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        private async System.Threading.Tasks.Task UpdateAttendanceForLeave(Leave leave)
        {
            DateTime currentDate = leave.StartDate;

            while (currentDate <= leave.EndDate)
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == leave.UserId &&
                                             a.AttendanceDate == currentDate);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        UserId = leave.UserId,
                        AttendanceDate = currentDate,
                        CheckInTime = null,
                        CheckOutTime = null,
                        IsHoliday = true,
                        IsAbsence = false,
                        LeaveId = leave.Id,
                        ShiftId = leave.User.ShiftId
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    attendance.IsHoliday = true;
                    attendance.IsAbsence = false;
                    attendance.LeaveId = leave.Id;
                    attendance.CheckInTime = null;
                    attendance.CheckOutTime = null;
                    attendance.Late = null;
                    attendance.EarlyLeave = null;
                    attendance.Overtime = null;
                    attendance.TotalWorkHours = null;
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task ResetAttendanceForLeave(Leave leave)
        {
            DateTime currentDate = leave.StartDate;

            while (currentDate <= leave.EndDate)
            {
                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == leave.UserId &&
                                             a.AttendanceDate == currentDate);

                if (attendance != null && attendance.LeaveId == leave.Id)
                {
                    attendance.IsHoliday = false;
                    attendance.IsAbsence = true;
                    attendance.LeaveId = null;
                    // Ì„ﬂ‰ﬂ Â‰« ≈⁄«œ…  ⁄ÌÌ‰ «·Õ÷Ê— ≈–« ﬂ«‰ Â‰«ﬂ ‰Ÿ«„ Õ÷Ê— «› —«÷Ì
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // «·”„«Õ ›ﬁÿ »«·√—ﬁ«„
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private async void employeeName_box_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                var allUsers = await _context.Users
                   .Include(u => u.Department)
                   .Include(u => u.Branch)
                   .Include(u => u.JobTitle)
                   .OrderBy(u => u.FullName)
                   .Where(u => u.FullName.StartsWith(employeeName_box.Text))
                   .ToListAsync();

                // › Õ ‰«›–… «Œ Ì«— «·„ÊŸ›
                var employeeSelectionWindow = new EmployeeSelectionWindow(allUsers, false, "«Œ — «·„ÊŸ› ·ÿ·» «·≈Ã«“…");
                employeeSelectionWindow.Owner = this;

                if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
                {
                    var selectedEmployee = employeeSelectionWindow.SelectedUser;

                    txtEmployeeId.Text = selectedEmployee.Id.ToString();
                    employeeName_box.Text = selectedEmployee.FullName;
                }
            }
        }

        private async void btnOwnSearch_Click(object sender, RoutedEventArgs e)
        {
            await LoadLeaves();

        }

        private void btnOwnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            cmbOwnLeaveType.SelectedIndex = 0;
            dpOwnFromDate.SelectedDate = null;
            dpOwnToDate.SelectedDate = null;
            cmbOwnStatus.SelectedIndex = 0;
        }

    }

    // ›∆«  „”«⁄œ…
    public class LeaveBalanceInfo
    {
        public int Total { get; set; }
        public int Used { get; set; }
        public int Remaining { get; set; }
    }

    // Converters
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    0 => new SolidColorBrush(Colors.Gray), // „”Êœ…
                    1 => new SolidColorBrush(Colors.Orange), // ﬁÌœ «·«‰ Ÿ«—
                    2 => new SolidColorBrush(Colors.Green), // „Ê«›ﬁ ⁄·ÌÂ
                    3 => new SolidColorBrush(Colors.Red), // „—›Ê÷
                    4 => new SolidColorBrush(Colors.Purple), // „·€Ï
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeToPersianConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // «” Œœ«„ „ﬂ »… PersianDateTime √Ê ﬂ «»… „‰ÿﬁ «· ÕÊÌ·
                return dateTime.ToString("yyyy/MM/dd");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // √÷› Â–« «·‹ Converter ›Ì ‰Â«Ì… «·„·›
    public class TupleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                return System.Tuple.Create(values[0], values[1]);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int status)
            {

                //  Õﬁﬁ „‰ ’·«ÕÌ«  «·„” Œœ„
                bool isManager = App.CurrentUser?.JobTitle?.IsManager ?? false;
                bool isHR = App.CurrentUser?.Department?.IsHR ?? false;

                // ≈ŸÂ«— «·“— ··„œÌ— ≈–« ﬂ«‰  «·Õ«·… Pending
                if (status == 1 && isManager)
                    return Visibility.Visible;

                // ≈ŸÂ«— «·“— ··„Ê«—œ «·»‘—Ì… ≈–« ﬂ«‰  «·Õ«·… Under Review
                if (status == 5 && isHR)
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
    }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
