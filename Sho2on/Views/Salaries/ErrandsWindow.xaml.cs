using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static HR_Application.EmployeeData;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for ErrandsWindow.xaml
    /// </summary>
    public partial class ErrandsWindow : Window
    {
        public event Action<DateTime> FromDateChanged;
        public event Action<DateTime> ToDateChanged;

        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private string code, name;
        private DateTime fromdate;
        private DateTime todate;
        private int? branchId;
        private bool Early;
        private bool Late;
        private bool OT;
        private TimeSpan fromShift;
        private TimeSpan toShift;
        private List<User> _managers = new List<User>();
        private List<User> _employees = new List<User>();
        private User _selectedEmployee;
        private User _selectedApprover;
        private List<User> users = new List<User>();

        public ErrandsWindow()
        {
            InitializeComponent();
        }

        public ErrandsWindow(string _code, int _branchId, TimeSpan _todate, TimeSpan _fromdate, DateTime date)
        {
            code = _code;
            todate = date.Add(_todate);
            fromdate = date.Add(_fromdate);
            branchId = _branchId;
            InitializeComponent();
            from_dateTimePicker.Value = fromdate;
            to_dateTimePicker.Value = todate;
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                txtEmployeeCode.Text = user_box.SelectedValue.ToString();
                _selectedEmployee = selectedUser;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _employees = _context.Users.Include(e => e.Shift).Include(e => e.Manager).ToList();

                users.AddRange(_employees);
                user_box.ItemsSource = users;
                if (!string.IsNullOrEmpty(code) && branchId != null)
                {
                    txtEmployeeCode.Text = code;
                    var employee = _employees.FirstOrDefault(e => e.Code == code);
                    if (employee != null)
                        LoadEmployeeData(employee);
                }
                else
                {
                    txtEmployeeCode.Text = App.CurrentUser.Code;
                    var employee = _employees.FirstOrDefault(e => e.Code == App.CurrentUser.Code);
                    if (employee != null)
                        LoadEmployeeData(employee);
                }


            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
            }
        }

        private void LoadEmployeeData(User user)
        {
            try
            {

                if (user != null)
                {
                    _selectedEmployee = user;
                    user_box.SelectedValue = user.Code;
                    Early = user.ExemptEarlyLeave;
                    OT = user.ExemptOvertime;
                    Late = user.ExemptLate;

                    fromShift = user.Shift.StartTime;
                    toShift = user.Shift.EndTime;

                    if (user.Manager != null)
                    {
                        _selectedApprover = user.Manager;
                        txtApproverName.Text = _selectedApprover.FullName;
                    }

                    LoadApprovers();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
            }
        }

        private void SearchEmployee(string searchText)
        {
            var employeeSelectionWindow = new EmployeeSelectionWindow(
                _context.Users.ToList(),
                false,
                "«Œ — «·„ÊŸ› ·ÿ·» «·≈–‰",
                searchText);
            employeeSelectionWindow.Owner = this;

            if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
            {
                LoadEmployeeData(employeeSelectionWindow.SelectedUser);
            }
        }

        private void btnSelectEmployee_Click(object sender, RoutedEventArgs e)
        {
            SearchEmployee(txtEmployeeCode.Text);
        }

        private async void LoadApprovers()
        {
            try
            {
                _managers = await _context.Users
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                btnSelectApprover.IsEnabled = _managers.Count > 0;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„œÌ—Ì‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApproveMission(string code, int branchId)
        {

            int type = 1;

            try
                {
                    try
                    {
                        var user = _context.Users
                         .FirstOrDefault(u => u.Code.ToString() == code && branchId == u.BranchId);

                        // Calculate time differences
                        DateTime? clockIn = from_dateTimePicker.Value;
                        DateTime? clockOff = to_dateTimePicker.Value;

                        TimeSpan late = TimeSpan.Zero;
                        if (user.ExemptLate && clockIn != null && clockIn.Value.TimeOfDay > fromShift)
                        {
                            late = TimeSpan.FromMinutes((clockIn.Value.TimeOfDay - fromShift).TotalMinutes);
                        }

                        TimeSpan early = TimeSpan.Zero;
                        if (user.ExemptEarlyLeave && clockOff != null && clockOff.Value.TimeOfDay < toShift)
                        {
                            early = TimeSpan.FromMinutes((toShift - clockOff.Value.TimeOfDay).TotalMinutes);
                        }

                        TimeSpan INearly = TimeSpan.Zero;
                        if (user.ExemptEarlyEnter && clockIn != null && clockIn.Value.TimeOfDay < fromShift)
                        {
                            INearly = TimeSpan.FromMinutes((fromShift - clockIn.Value.TimeOfDay).TotalMinutes);
                        }

                        TimeSpan overtime = TimeSpan.Zero;
                        if (user.ExemptOvertime && clockOff != null && clockOff.Value.TimeOfDay > toShift)
                        {
                            overtime = TimeSpan.FromMinutes((clockOff.Value.TimeOfDay - toShift).TotalMinutes);
                        }

                        TimeSpan workTime = TimeSpan.FromMinutes((toShift - fromShift).TotalMinutes);

                        TimeSpan attendTime = TimeSpan.Zero;
                        if (clockIn != null && clockOff != null)
                        {
                            TimeSpan clockInTime = clockIn.Value.TimeOfDay;
                            TimeSpan clockOffTime = clockOff.Value.TimeOfDay;

                            if (clockOffTime < clockInTime)
                            {
                                clockOffTime += TimeSpan.FromDays(1);
                            }

                            attendTime = clockOffTime - clockInTime;
                        }

                        // Update or Insert Attendance
                        var dayDate = to_dateTimePicker.Value.Value.Date;
                        var attendance = _context.Attendances.Include(a => a.User)
                            .FirstOrDefault(a => a.User.Code.ToString() == code && a.User.BranchId == branchId && a.AttendanceDate == dayDate);

                        if (attendance == null)
                        {
                            attendance = new Attendance
                            {
                                UserId = user.Id,
                                AttendanceDate = dayDate,
                                ShiftId = user.ShiftId,
                                CheckInBranchId = branchId,
                                CheckOutBranchId = branchId
                            };
                            _context.Attendances.Add(attendance);
                        }

                        attendance.CheckInTime = clockIn;
                        attendance.CheckOutTime = clockOff;
                        attendance.Late = late;
                        attendance.EarlyLeave = early;
                        attendance.Overtime = overtime;
                        attendance.TotalWorkHours = attendTime;
                        attendance.EarlyEnter = INearly;
                        attendance.ExemptLate = Late;
                        attendance.ExemptEarlyEnter = user.ExemptEarlyEnter;
                        attendance.ExemptEarlyLeave = Early;
                        attendance.ExemptOvertime = OT;

                        // Insert Salary Operation if permission
                        if (type == 2 && !string.IsNullOrEmpty(value_box.Text))
                        {
                            var salaryOperation = new Salary
                            {
                                UserId = user.Id,
                                Notes = text_box.Text,
                                DayDate = to_dateTimePicker.Value.Value.Date,
                                EditedAt = DateTime.Now,
                                Type = 17,
                                Amount = decimal.Parse(value_box.Text),
                                Operation = 1
                            };
                            _context.Salaries.Add(salaryOperation);
                        }

                        _context.SaveChanges();

                        LocalizationManager.ShowMessage(" „ «÷«›… «·«Ã—«¡");
                    }
                    catch (Exception ex)
                    {
                        LocalizationManager.ShowMessage($"Error: {ex.Message}");
                    }

                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage(ex.Message);
                }
            
        }

        private void save_month_btn_Click(object sender, RoutedEventArgs e)
        {
            int type = 1;

            if (_selectedEmployee != null)
            {
                try
                {
                        try
                        {
                        var user = _context.Users
                         .FirstOrDefault(u => u.Code.ToString() == _selectedEmployee.Code && _selectedEmployee.BranchId == u.BranchId);

                        // Insert into Procedures
                        var procedure = new Procedure
                            {
                                UserId = _selectedEmployee.Id,
                                Notes = text_box.Text,
                                StartDate = from_dateTimePicker.Value.Value,
                                EndDate = to_dateTimePicker.Value.Value,
                                Type = type,
                                BranchId = _selectedEmployee.BreakId,
                                Status = ProcedureStatus.Pending,
                                ApprovedByUserId = _selectedApprover.Id,
                                CreatedAt = DateTime.Now
                            };
                            _context.Procedures.Add(procedure);


                            _context.SaveChanges();

                            LocalizationManager.ShowMessage(" „  ﬁœÌ„ «·ÿ·»");
                        }
                        catch (Exception ex)
                        {
                            LocalizationManager.ShowMessage($"Error: {ex.Message}");
                        }
                    
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage(ex.Message);
                }
            }
        }

        private void permission_check_Checked(object sender, RoutedEventArgs e)
        {
            value_box.Visibility = Visibility.Visible;
        }

        private void txtEmployeeCode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Enter)
            {
                user_box.SelectedValue = txtEmployeeCode.Text;
                if (user_box.SelectedItem is User selectedUser)
                    _selectedEmployee = selectedUser;
                //SearchEmployee(txtEmployeeCode.Text);
            }
        }

        private void permission_check_Unchecked(object sender, RoutedEventArgs e)
        {
            value_box.Visibility = Visibility.Collapsed;
        }

        private void from_dateTimePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DateTime? newDate = from_dateTimePicker.Value;
            if (newDate.HasValue)
            {
                FromDateChanged?.Invoke(newDate.Value);
            }
        }

        private void btnSelectApprover_Click(object sender, RoutedEventArgs e)
        {
            if (_managers.Count == 0)
            {
                LocalizationManager.ShowMessage("·« ÌÊÃœ „œÌ—Ì‰ „ «ÕÌ‰ ··«Œ Ì«—", "„⁄·Ê„…", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var managerSelectionWindow = new EmployeeSelectionWindow(_managers, true, "«Œ — «·„Ê«›ﬁ ⁄·Ï «·„√„Ê—Ì…");
            managerSelectionWindow.Owner = this;

            if (managerSelectionWindow.ShowDialog() == true && managerSelectionWindow.SelectedUser != null)
            {
                _selectedApprover = managerSelectionWindow.SelectedUser;
                txtApproverName.Text = _selectedApprover.FullName;
            }
        }


        private void to_dateTimePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DateTime? newDate = to_dateTimePicker.Value;
            if (newDate.HasValue)
            {
                ToDateChanged?.Invoke(newDate.Value);
            }
        }
    }
}
