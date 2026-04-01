using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace HR_Application.Views
{
    public partial class HolidayRequestWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private int _selectedLeaveTypeId;
        private User _user; 
        private User _selectedManager;
        private User _selectedReplaceEmployee;
        private List<LeaveType> _leaveTypes = new List<LeaveType>();
        private List<JobTitle> _jobTitles = new List<JobTitle>();
        private List<User> _managers = new List<User>();
        private List<User> _users = new List<User>();

        public HolidayRequestWindow(string employeeCode = null)
        {
            InitializeComponent();

            if (employeeCode != null && !string.IsNullOrEmpty(employeeCode))
            {
                employeeCode_box.Text = employeeCode;
                SearchEmployee();
            }
                
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                ReplaceEmployeeCode_box.Text = user_box.SelectedValue.ToString();
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
                    itemsList = _users;
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

        private async Task LoadData()
        {
            try
            {
                using (var dbContext = new AppDbContext(App.ConnectionString))
                {
                    _users = await dbContext.Users
                        .Include(u => u.Department)
                        .Include(u => u.Branch)
                        .Include(u => u.JobTitle)
                        .Include(u => u.Manager)
                        .ToListAsync();

                    user_box.ItemsSource = _users;


                    _jobTitles = await dbContext.JobTitles
                        .OrderBy(j => j.Name)
                        .ToListAsync();

                    cmbFilterByJobTitle.ItemsSource = _jobTitles;


                    _leaveTypes = await dbContext.LeaveTypes
                        .Where(lt => lt.IsActive)
                        .OrderBy(lt => lt.Name)
                        .ToListAsync();

                    // تحديث RadioButtons بناءً على أنواع الإجازات
                    UpdateLeaveTypeRadioButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public HolidayRequestWindow()
        {
            InitializeComponent();
        }

        private async Task LoadManagers(int? jobTitleId = null)
        {
            try
            {
                using (var dbContext = new AppDbContext(App.ConnectionString))
                {
                    var query = dbContext.Users
                        .Include(u => u.JobTitle)
                        .Include(u => u.Department)
                        .Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value);

                    if (jobTitleId.HasValue && jobTitleId.Value > 0)
                    {
                        query = query.Where(u => u.JobTitleId == jobTitleId.Value);
                    }

                    _managers = await query
                        .OrderBy(u => u.FullName)
                        .ToListAsync();

                    // الآن بدلاً من ComboBox، سنستخدم زر لفتح نافذة الاختيار
                    if (_managers.Count == 0 && _selectedManager == null)
                    {
                        btnSelectManager.Content = "لا يوجد مديرين متاحين";

                    }
                    btnSelectManager.IsEnabled = _managers.Count > 0;

                    // تحديث قسم الموافقة
                    UpdateApprovalSectionVisibility();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المديرين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btnSelectManager_Click(object sender, RoutedEventArgs e)
        {
            if (_managers.Count == 0)
            {
                MessageBox.Show("لا يوجد مديرين متاحين للاختيار", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // فتح نافذة اختيار المدير
            var managerSelectionWindow = new EmployeeSelectionWindow(_managers, true, "اختر الموافق على الإجازة");

            if (managerSelectionWindow.ShowDialog() == true && managerSelectionWindow.SelectedUser != null)
            {
                var selectedManager = managerSelectionWindow.SelectedUser;

                // حفظ المدير المختار
                _selectedManager = selectedManager;

                // تحديث الواجهة
                UpdateSelectedManagerDisplay(selectedManager);
            }
        }

        private void UpdateSelectedManagerDisplay(User manager)
        {
            // إظهار بيانات المدير المختار
            txtSelectedManagerName.Text = manager.FullName;
            txtSelectedManagerJobTitle.Text = manager.JobTitle?.Name ?? "غير محدد";
            txtSelectedManagerDepartment.Text = manager.Department?.Name ?? "غير محدد";

            panelSelectedManager.Visibility = Visibility.Visible;
            btnSelectManager.Content = "تغيير المدير";
        }

        private void UpdateLeaveTypeRadioButtons()
        {
            // مسح RadioButtons الموجودة
            var radioButtonPanel = this.FindName("leaveTypePanel") as StackPanel;
            if (radioButtonPanel == null) return;

            radioButtonPanel.Children.Clear();

            // إضافة RadioButton لكل نوع إجازة
            foreach (var leaveType in _leaveTypes)
            {
                var radioButton = new RadioButton
                {
                    Style = (Style)FindResource("RadioButtonStyle"),
                    Content = leaveType.Name,
                    Tag = leaveType.Id,
                    Margin = new Thickness(15, 0, 15, 0),
                    GroupName = "LeaveType"
                };
                radioButton.Checked += LeaveTypeRadioButton_Checked;

                radioButtonPanel.Children.Add(radioButton);
            }

            // تحديد أول RadioButton
            if (radioButtonPanel.Children.Count > 0)
            {
                var firstRadio = radioButtonPanel.Children[0] as RadioButton;
                firstRadio.IsChecked = true;
                _selectedLeaveTypeId = (int)firstRadio.Tag;
            }
        }

        private void InitializeEvents()
        {
            startDate_picker.SelectedDateChanged += UpdateLeaveDuration;
            endDate_picker.SelectedDateChanged += UpdateLeaveDuration;
        }

        private async void LeaveTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is int leaveTypeId)
            {
                _selectedLeaveTypeId = leaveTypeId;
                await LoadManagers();
                if (_employeeId > 0)
                {
                    await UpdateLeaveBalanceDisplay(_employeeId, leaveTypeId);
                }
                UpdateUIForLeaveType(leaveTypeId);
            }
        }

        private async void UpdateUIForLeaveType(int leaveTypeId)
        {
            var leaveType = _leaveTypes.FirstOrDefault(lt => lt.Id == leaveTypeId);
            if (leaveType == null) return;

            // تحديث عرض معلومات نوع الإجازة
            txtLeaveTypeInfo.Text = leaveType.Name;
            txtMaxConsecutiveDays.Text = leaveType.MaxConsecutiveDays?.ToString() ?? "لا يوجد حد";
            txtRequiresApproval.Text = leaveType.RequiresApproval ? "نعم" : "لا";

            // عرض/إخفاء قسم الموافقة
            UpdateApprovalSectionVisibility();

            // إذا كان نوع الإجازة يتطلب موافقة، تحميل المديرين
            if (leaveType.RequiresApproval)
            {
                await LoadManagers();
            }
        }

        private void UpdateApprovalSectionVisibility()
        {
            var leaveType = _leaveTypes.FirstOrDefault(lt => lt.Id == _selectedLeaveTypeId);

            // البحث عن GroupBox الخاص بالموافقة في XAML
            var approvalGroupBox = this.FindName("approvalGroupBox") as System.Windows.Controls.GroupBox;
            if (approvalGroupBox != null)
            {
                approvalGroupBox.Visibility = (leaveType?.RequiresApproval == true) ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void cmbFilterByJobTitle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterByJobTitle.SelectedValue != null)
            {
                int? jobTitleId = null;

                if (cmbFilterByJobTitle.SelectedValue is int selectedId && selectedId > 0)
                {
                    jobTitleId = selectedId;
                }

                await LoadManagers(jobTitleId);

                // مسح اختيار المدير السابق عند تغيير الفلتر
                ClearManagerSelection();
            }
        }

        private void ClearManagerSelection()
        {
            _selectedManager = null;
            panelSelectedManager.Visibility = Visibility.Collapsed;
            btnSelectManager.Content = "اختر الموافق على الإجازة";
        }

        private async void UpdateLeaveDuration(object sender, SelectionChangedEventArgs e)
        {
            if (startDate_picker.SelectedDate != null && endDate_picker.SelectedDate != null)
            {
                DateTime startDate = startDate_picker.SelectedDate.Value;
                DateTime endDate = endDate_picker.SelectedDate.Value;

                if (endDate >= startDate)
                {
                    TimeSpan duration = endDate - startDate;
                    int totalDays = (int)duration.TotalDays + 1;
                    duration_box.Text = totalDays.ToString();

                    // التحقق من الرصيد المتبقي
                    await CheckLeaveBalance();
                }
                else
                {
                    duration_box.Text = "0";
                    MessageBox.Show("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
                }
            }
        }

        private async Task LoadEmployeeData(User selectedEmployee)
        {

            _employeeId = selectedEmployee.Id;
            _user = selectedEmployee;
            employeeCode_box.Text = selectedEmployee.Code;
            employeeName_box.Text = selectedEmployee.FullName;

            if (_user.Manager != null)
            {
                _selectedManager = _user.Manager;

                UpdateSelectedManagerDisplay(_selectedManager);
            }

            DisplayEmployeeInfo(selectedEmployee);

            // تحميل المديرين
            await LoadManagers();

            await UpdateLeaveBalanceDisplay(_employeeId, _selectedLeaveTypeId);

            leaveBalanceSection.Visibility = Visibility.Visible;
        }

        private async void SearchEmployee()
        {
            try
            {
                // تحميل جميع الموظفين
                var allUsers = _users.Where(u => u.FullName.StartsWith(employeeName_box.Text) || u.Code == employeeCode_box.Text)
                    .ToList();

                // فتح نافذة اختيار الموظف
                var employeeSelectionWindow = new EmployeeSelectionWindow(allUsers, false, "اختر الموظف لطلب الإجازة", employeeCode_box.Text);

                if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
                {
                    var selectedEmployee = employeeSelectionWindow.SelectedUser;
                    LoadEmployeeData(selectedEmployee);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayEmployeeInfo(User employee)
        {
            // يمكنك هنا عرض معلومات إضافية عن الموظف إذا رغبت
            txtEmployeeDepartment.Text = employee.Department?.Name ?? "غير محدد";
            txtEmployeeBranch.Text = employee.Branch?.Name ?? "غير محدد";
            txtEmployeeHireDate.Text = employee.HireDate.ToString("yyyy/MM/dd");

            // عرض أيام الإجازة الأسبوعية
            if (employee.WeekHoliday != null)
            {
                txtEmployeeWeekend.Text = GetWeekendDaysSummary(employee.WeekHoliday);
            }
            else
            {
                txtEmployeeWeekend.Text = "غير محدد";
            }
        }

        private void ResetEmployeeInfo()
        {
            _employeeId = 0;
            _user = null;
            employeeName_box.Text = string.Empty;
            txtEmployeeDepartment.Text = string.Empty;
            txtEmployeeBranch.Text = string.Empty;
            txtEmployeeHireDate.Text = string.Empty;
            txtEmployeeWeekend.Text = string.Empty; // إضافة هذا السطر
            leaveBalanceSection.Visibility = Visibility.Collapsed;

            // إعادة تعيين رصيد الإجازات
            annualBalance_text.Text = "0 يوم";
            remainingBalance_text.Text = "0 يوم";
            usedBalance_text.Text = "0 يوم";

            ClearManagerSelection();
        }

        private async System.Threading.Tasks.Task UpdateLeaveBalanceDisplay(int userId, int leaveTypeId)
        {
            try
            {
               
                    var balanceInfo = await CalculateLeaveBalance(userId, leaveTypeId);

                    // تحديث عرض الرصيد
                    annualBalance_text.Text = $"{balanceInfo.Total} يوم";
                    remainingBalance_text.Text = $"{balanceInfo.Remaining} يوم";
                    usedBalance_text.Text = $"{balanceInfo.Used} يوم";

                    // تحديث لون الرصيد المتبقي
                    UpdateRemainingBalanceColor(balanceInfo.Remaining, balanceInfo.Total);

                    // عرض معلومات نوع الإجازة
                    var leaveType = _leaveTypes.FirstOrDefault(lt => lt.Id == leaveTypeId);
                    if (leaveType != null)
                    {
                        txtLeaveTypeInfo.Text = leaveType.Name;
                        txtMaxConsecutiveDays.Text = leaveType.MaxConsecutiveDays?.ToString() ?? "لا يوجد حد";
                        txtRequiresApproval.Text = leaveType.RequiresApproval ? "نعم" : "لا";
                    }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب الرصيد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRemainingBalanceColor(int remaining, int total)
        {
            if (total == 0) return;

            double percentage = (double)remaining / total;

            if (percentage >= 0.5)
                remainingBalance_text.Foreground = System.Windows.Media.Brushes.Green;
            else if (percentage >= 0.25)
                remainingBalance_text.Foreground = System.Windows.Media.Brushes.Orange;
            else
                remainingBalance_text.Foreground = System.Windows.Media.Brushes.Red;
        }

        private async System.Threading.Tasks.Task<LeaveBalanceInfo> CalculateLeaveBalance(int userId, int leaveTypeId)
        {
            using (var dbContext = new AppDbContext(App.ConnectionString))
            {
                // الحصول على رصيد الإجازة من قاعدة البيانات
                var leaveBalance = await dbContext.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

                var leaveType = await dbContext.LeaveTypes.FindAsync(leaveTypeId);

                int totalBalance = leaveBalance?.TotalBalance ?? leaveType?.DefaultBalance ?? 0;

                // حساب الإجازات المستخدمة (الموافق عليها فقط)
                var usedLeaves = await dbContext.Leaves
                    .Where(l => l.UserId == userId &&
                               l.LeaveTypeId == leaveTypeId &&
                               l.Status == 2 && // الموافق عليها
                               !l.IsCancelled)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                return new LeaveBalanceInfo
                {
                    Total = totalBalance,
                    Used = usedLeaves,
                    Remaining = totalBalance - usedLeaves
                };
            }

        }

        private async System.Threading.Tasks.Task CheckLeaveBalance()
        {
            if (_employeeId == 0 || string.IsNullOrEmpty(duration_box.Text)) return;

            int requestedDays;
            if (!int.TryParse(duration_box.Text, out requestedDays) || requestedDays <= 0)
                return;

            var balanceInfo = await CalculateLeaveBalance(_employeeId, _selectedLeaveTypeId);
            var leaveType = _leaveTypes.FirstOrDefault(lt => lt.Id == _selectedLeaveTypeId);

            // التحقق إذا كان نوع الإجازة يخصم من الرصيد
            if (leaveType?.DeductFromBalance == true && requestedDays > balanceInfo.Remaining)
            {
                MessageBox.Show($"الرصيد المتبقي غير كافي. المتبقي: {balanceInfo.Remaining} يوم، المطلوب: {requestedDays} يوم",
                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // التحقق من الحد الأقصى للأيام المتتالية
            if (leaveType?.MaxConsecutiveDays.HasValue == true && requestedDays > leaveType.MaxConsecutiveDays.Value)
            {
                MessageBox.Show($"الحد الأقصى للإجازة من هذا النوع هو {leaveType.MaxConsecutiveDays.Value} يوم متتالية",
                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // التحقق من تعارض التواريخ
            await CheckDateConflicts(requestedDays);
        }

        private async System.Threading.Tasks.Task CheckDateConflicts(int requestedDays)
        {
            if (startDate_picker.SelectedDate == null || endDate_picker.SelectedDate == null)
                return;

            DateTime startDate = startDate_picker.SelectedDate.Value;
            DateTime endDate = endDate_picker.SelectedDate.Value;

            // التحقق من وجود إجازات متعارضة
            var conflictingLeaves = await _context.Leaves
                .Where(l => l.UserId == _employeeId &&
                           l.Status == 2 && // الموافق عليها فقط
                           !l.IsCancelled &&
                           ((l.StartDate <= endDate && l.EndDate >= startDate)))
                .ToListAsync();

            if (conflictingLeaves.Any())
            {
                var conflictMessage = "هناك إجازات متعارضة في الفترة المحددة:\n";
                foreach (var leave in conflictingLeaves)
                {
                    conflictMessage += $"- من {leave.StartDate:yyyy/MM/dd} إلى {leave.EndDate:yyyy/MM/dd}\n";
                }

                MessageBox.Show(conflictMessage, "تعارض في التواريخ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void submit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    // الحصول على نوع الإجازة
                    var leaveType = await _context.LeaveTypes.FindAsync(_selectedLeaveTypeId);
                    bool requiresApproval = leaveType?.RequiresApproval ?? true;

                    int? approvingManagerId = null;
                    if (requiresApproval && _selectedManager != null)
                    {
                        approvingManagerId = _selectedManager.Id;
                    }

                    // 1. إنشاء طلب الإجازة
                    var leaveRequest = new Leave
                    {
                        UserId = _employeeId,
                        LeaveTypeId = _selectedLeaveTypeId,
                        StartDate = startDate_picker.SelectedDate.Value,
                        EndDate = endDate_picker.SelectedDate.Value,
                        Duration = int.Parse(duration_box.Text),
                        Reason = reason_box.Text,
                        RequestDate = DateTime.Now,
                        ReplacementUserId = (_selectedReplaceEmployee != null) ? _selectedReplaceEmployee.Id : null,
                        Status = requiresApproval ? 1 : 2, // 1: Pending, 2: Approved
                        ApprovedBy = approvingManagerId
                    };

                    _context.Leaves.Add(leaveRequest);
                    await _context.SaveChangesAsync();

                    // 2. إذا كانت الإجازة لا تتطلب موافقة أو تمت الموافقة تلقائياً
                    if (leaveRequest.Status == 2)
                    {
                        // خصم من الرصيد إذا كان النوع يخصم
                        if (leaveType?.DeductFromBalance == true)
                        {
                            await DeductLeaveBalance(_employeeId, _selectedLeaveTypeId, leaveRequest.Duration);
                        }

                        // تحديث سجلات الحضور
                        await UpdateAttendanceForLeave(leaveRequest);
                    }

                    string message = leaveRequest.Status == 2
                        ? "تم تقديم طلب الإجازة بنجاح وتحديث سجلات الحضور"
                        : "تم تقديم طلب الإجازة بنجاح وهو قيد انتظار الموافقة";

                    MessageBox.Show(message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تقديم الطلب: {ex.InnerException?.Message ?? ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        private async System.Threading.Tasks.Task UpdateAttendanceForLeave(Leave leaveRequest)
        {
            DateTime currentDate = leaveRequest.StartDate;

            while (currentDate <= leaveRequest.EndDate)
            {
                // تخطي أيام العطلات الأسبوعية
                if (_user.WeekHoliday != null && IsWeekend(currentDate, _user.WeekHoliday))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == _employeeId && a.AttendanceDate == currentDate);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        UserId = _employeeId,
                        AttendanceDate = currentDate,
                        CheckInTime = null,
                        CheckOutTime = null,
                        IsHoliday = true,
                        IsAbsence = false,
                        LeaveId = leaveRequest.Id,
                        ShiftId = _user.ShiftId,
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    attendance.IsHoliday = true;
                    attendance.IsAbsence = false;
                    attendance.LeaveId = leaveRequest.Id;
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
        private bool IsWeekend(DateTime date, WeekHoliday weekHoliday)
        {
            DayOfWeek dayOfWeek = date.DayOfWeek;

            // تحويل DayOfWeek إلى الرقم المناسب لهيكل WeekHoliday
            // Day1 = السبت, Day2 = الأحد, Day3 = الإثنين, Day4 = الثلاثاء, Day5 = الأربعاء, Day6 = الخميس, Day7 = الجمعة
            switch (dayOfWeek)
            {
                case DayOfWeek.Saturday:
                    return weekHoliday.Day1;
                case DayOfWeek.Sunday:
                    return weekHoliday.Day2;
                case DayOfWeek.Monday:
                    return weekHoliday.Day3;
                case DayOfWeek.Tuesday:
                    return weekHoliday.Day4;
                case DayOfWeek.Wednesday:
                    return weekHoliday.Day5;
                case DayOfWeek.Thursday:
                    return weekHoliday.Day6;
                case DayOfWeek.Friday:
                    return weekHoliday.Day7;
                default:
                    return false;
            }
        }

        private string GetWeekendDaysSummary(WeekHoliday weekHoliday)
        {
            if (weekHoliday == null) return "غير محدد";

            var days = new List<string>();

            if (weekHoliday.Day1) days.Add("السبت");
            if (weekHoliday.Day2) days.Add("الأحد");
            if (weekHoliday.Day3) days.Add("الإثنين");
            if (weekHoliday.Day4) days.Add("الثلاثاء");
            if (weekHoliday.Day5) days.Add("الأربعاء");
            if (weekHoliday.Day6) days.Add("الخميس");
            if (weekHoliday.Day7) days.Add("الجمعة");

            return days.Count > 0 ? string.Join("، ", days) : "لا توجد أيام إجازة";
        }


        private async void saveDraft_btn_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    var leaveRequest = new Leave
                    {
                        UserId = _employeeId,
                        LeaveTypeId = _selectedLeaveTypeId,
                        StartDate = startDate_picker.SelectedDate.Value,
                        EndDate = endDate_picker.SelectedDate.Value,
                        Duration = int.Parse(duration_box.Text),
                        Reason = reason_box.Text,
                        RequestDate = DateTime.Now,
                        Status = 0, // Draft
                    };

                    _context.Leaves.Add(leaveRequest);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم حفظ الطلب كمسودة", "حفظ مسودة", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حفظ المسودة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل تريد إلغاء طلب الإجازة؟", "تأكيد الإلغاء",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void search_btn_Click(object sender, RoutedEventArgs e)
        {
            SearchEmployee();
        }

        private bool ValidateForm()
        {
            // التحقق من كود الموظف
            if (string.IsNullOrEmpty(employeeCode_box.Text))
            {
                MessageBox.Show("يرجى إدخال كود الموظف", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                employeeCode_box.Focus();
                return false;
            }

            if (_employeeId == 0)
            {
                MessageBox.Show("يرجى البحث عن الموظف أولاً", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من نوع الإجازة
            if (_selectedLeaveTypeId == 0)
            {
                MessageBox.Show("يرجى اختيار نوع الإجازة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var leaveType = _leaveTypes.FirstOrDefault(lt => lt.Id == _selectedLeaveTypeId);
            if (leaveType?.RequiresApproval == true && _selectedManager == null)
            {
                MessageBox.Show("يرجى اختيار الموافق على الإجازة", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnSelectManager.Focus();
                return false;
            }

            // التحقق من التواريخ
            if (startDate_picker.SelectedDate == null || endDate_picker.SelectedDate == null)
            {
                MessageBox.Show("يرجى تحديد تاريخي بداية ونهاية الإجازة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (startDate_picker.SelectedDate.Value < DateTime.Today)
            {
                MessageBox.Show("لا يمكن تقديم إجازة بتاريخ قديم", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                startDate_picker.Focus();
                return false;
            }

            // التحقق من المدة
            if (string.IsNullOrEmpty(duration_box.Text) || int.Parse(duration_box.Text) <= 0)
            {
                MessageBox.Show("يرجى إدخال مدة إجازة صحيحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من السبب
            if (string.IsNullOrEmpty(reason_box.Text))
            {
                MessageBox.Show("يرجى كتابة سبب الإجازة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                reason_box.Focus();
                return false;
            }

            return true;
        }

        // إضافة زر لإدارة رصيد الإجازات في نفس النافذة
        private void btnManageBalance_Click(object sender, RoutedEventArgs e)
        {
            if (_employeeId > 0)
            {
                var manageBalanceWindow = new ManageLeaveBalanceWindow();
                manageBalanceWindow.Owner = this;

                // تمرير كود الموظف للبحث عنه تلقائياً
                manageBalanceWindow.txtSearchEmployeeId.Text = _employeeId.ToString();
                manageBalanceWindow.btnSearch_Click(sender, e);

                manageBalanceWindow.ShowDialog();

                // تحديث عرض الرصيد بعد التعديل
                if (_employeeId > 0 && _selectedLeaveTypeId > 0)
                {
                    UpdateLeaveBalanceDisplay(_employeeId, _selectedLeaveTypeId);
                }
            }
            else
            {
                MessageBox.Show("الرجاء البحث عن موظف أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private async void employeeCode_box_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Enter)
            {
                string code = employeeCode_box.Text;
                if (!string.IsNullOrEmpty(code))
                {
                    var selectedEmployee = _users.FirstOrDefault(u => u.Code == code);
                    if (selectedEmployee != null)
                    {
                        _employeeId = selectedEmployee.Id;
                        _user = selectedEmployee;
                        employeeCode_box.Text = selectedEmployee.Id.ToString();
                        employeeName_box.Text = selectedEmployee.FullName;
                        if (_user.Manager != null)
                        {

                            // حفظ المدير المختار
                            _selectedManager = _user.Manager;

                            // تحديث الواجهة
                            UpdateSelectedManagerDisplay(_selectedManager);
                        }

                        DisplayEmployeeInfo(selectedEmployee);

                        // تحميل المديرين
                        await LoadManagers();

                        await UpdateLeaveBalanceDisplay(_employeeId, _selectedLeaveTypeId);

                        leaveBalanceSection.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("لم يتم العثور ع الموظف", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void searchReplace_btn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                // تحميل جميع الموظفين
                var allUsers = _users.Where(u => u.Code == ReplaceEmployeeCode_box.Text)
                    .ToList();

                // فتح نافذة اختيار الموظف
                var employeeSelectionWindow = new EmployeeSelectionWindow(allUsers, false, "اختر الموظف القائم عن العمل", ReplaceEmployeeCode_box.Text);
                employeeSelectionWindow.Owner = this;

                if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
                {
                    _selectedReplaceEmployee = employeeSelectionWindow.SelectedUser;

                    ReplaceEmployeeCode_box.Text = _selectedReplaceEmployee.Code;
                    user_box.SelectedValue = _selectedReplaceEmployee.Code;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            InitializeEvents();
            await LoadData();
            employeeCode_box.Text = App.CurrentUser.Code;
            await LoadEmployeeData(App.CurrentUser);
        }

        private void ReplaceEmployeeCode_box_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            user_box.SelectedValue = ReplaceEmployeeCode_box.Text;
        }
    }

    public class LeaveBalanceInfo
    {
        public int Total { get; set; }
        public int Used { get; set; }
        public int Remaining { get; set; }
    }

    // إضافة Converter جديد
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}