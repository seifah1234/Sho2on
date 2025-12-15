using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Windows;
using System.Windows.Input;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                branch_box.ItemsSource = _context.Branches
                    .Where(b => App.userBranches.Contains(b.Id))
                    .ToList();
                if (branchId != null)
                {
                    branch_box.SelectedValue = branchId;
                }

                if (!string.IsNullOrEmpty(code) && branchId != null)
                {
                    code_box.Text = code;
                    name_box.Text = name;
                    LoadEmployeeData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadEmployeeData()
        {
            try
            {
                var user = _context.Users
                    .Include(u => u.Shift)
                    .Include(u => u.Branch)
                    .FirstOrDefault(u => u.Id.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString());

                if (user != null)
                {
                    name_box.Text = user.FullName;
                    Early = user.ExemptEarlyLeave;
                    OT = user.ExemptOvertime;
                    Late = user.ExemptLate;

                    fromShift = user.Shift.StartTime;
                    toShift = user.Shift.EndTime;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadEmployeeData();
                e.Handled = true;
            }
        }

        private void save_month_btn_Click(object sender, RoutedEventArgs e)
        {
            int type = 0;
            if (errand_check.IsChecked == true)
            {
                type = 1;
            }
            else if (permission_check.IsChecked == true)
            {
                type = 2;
            }

            if (!string.IsNullOrEmpty(code_box.Text))
            {
                try
                {
                   
                        try
                        {
                        var user = _context.Users
                         .FirstOrDefault(u => u.Code.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString());

                        // Insert into Procedures
                        var procedure = new Procedure
                            {
                                UserId = int.Parse(code_box.Text),
                                Notes = text_box.Text,
                                StartDate = from_dateTimePicker.Value.Value,
                                EndDate = to_dateTimePicker.Value.Value,
                                Type = type,
                                BranchId = (int)branch_box.SelectedValue
                            };
                            _context.Procedures.Add(procedure);

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
                                .FirstOrDefault(a => a.User.Code.ToString() ==  code_box.Text && a.User.BranchId.ToString() == branch_box.SelectedValue.ToString() && a.AttendanceDate == dayDate);

                        if (attendance == null)
                            {
                                attendance = new Attendance
                                {
                                    UserId = int.Parse(code_box.Text),
                                    AttendanceDate = dayDate,
                                    ShiftId = user.ShiftId,
                                    CheckInBranchId = (int)branch_box.SelectedValue,
                                    CheckOutBranchId = (int)branch_box.SelectedValue
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
                                    UserId = int.Parse(code_box.Text),
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

                            MessageBox.Show("تم اضافة الاجراء");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error: {ex.Message}");
                        }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void permission_check_Checked(object sender, RoutedEventArgs e)
        {
            value_box.Visibility = Visibility.Visible;
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

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {

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