using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddAttendRecord.xaml
    /// </summary>
    public partial class AddAttendRecord : Window
    {
        private User? _selectedUser;
        TimeSpan shiftFrom, shiftTo, WH, AH;
        int branchCode;
        TimeSpan? AttendanceTime;
        TimeSpan? DepartureTime;
        TimeSpan? DutyOn;
        private AppDbContext _context;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            branch_box.ItemsSource = _context.Branches
                .Where(b => App.userBranches.Contains(b.Id))
                .ToList();
        }

        TimeSpan? DutyOff;
       
        public AddAttendRecord()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            DispatcherTimer LiveTime = new DispatcherTimer();
            LiveTime.Interval = TimeSpan.FromSeconds(1);
            LiveTime.Tick += timer_Tick;
            LiveTime.Start();
            
            
        }
        void timer_Tick(object sender, EventArgs e)
        {
            Date_Now.Text = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
        }

        private void LoadEmployeeData()
        {
            if (branch_box.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار الفرع أولاً");
                return;
            }
            if (string.IsNullOrEmpty(code_box.Text))
            {
                MessageBox.Show("يرجى إدخال كود الموظف أولاً");
                return;
            }
            _selectedUser = _context.Users.Include(u => u.Shift).FirstOrDefault(u => u.Code.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString());
            if (_selectedUser == null)
            {
                MessageBox.Show("الموظف غير موجود في هذا الفرع");
                return;
            }
        }

        private async void save_record_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployeeData();
            try
            {
                // Add Attend record
                var existingAttendance = _context.Attendances
                    .FirstOrDefault(a => a.UserId == _selectedUser.Id && a.AttendanceDate.Date == DateTime.Now.Date);

                if (attend_check.IsChecked.HasValue && attend_check.IsChecked.Value)
                {
                    // Clock In
                    if (existingAttendance != null)
                    {
                        existingAttendance.CheckInTime = DateTime.Now;
                        existingAttendance.CheckInLocation = location_box.Text;
                    }
                    else
                    {
                        existingAttendance = new Attendance
                        {
                            UserId = _selectedUser.Id,
                            AttendanceDate = DateTime.Now.Date,
                            CheckInTime = DateTime.Now,
                            ShiftId = _selectedUser.ShiftId,
                            CheckInLocation = location_box.Text
                        };
                        _context.Attendances.Add(existingAttendance);
                        await _context.SaveChangesAsync();

                    }
                    AttendanceTime = DateTime.Now.TimeOfDay;
                }
                else
                {
                    // Clock Out
                    if (existingAttendance != null)
                    {
                        existingAttendance.CheckOutTime = DateTime.Now;
                        existingAttendance.CheckOutLocation = location_box.Text;
                    }
                    else
                    {
                        existingAttendance = new Attendance
                        {
                            UserId = _selectedUser.Id,
                            AttendanceDate = DateTime.Now,
                            CheckOutTime = DateTime.Now,
                            ShiftId = _selectedUser.ShiftId,
                            CheckOutLocation = location_box.Text
                        };
                        _context.Attendances.Add(existingAttendance);
                        await _context.SaveChangesAsync();

                    }
                    DepartureTime = DateTime.Now.TimeOfDay;
                }

                existingAttendance = _context.Attendances
                    .FirstOrDefault(a => a.UserId == _selectedUser.Id && a.AttendanceDate.Date == Convert.ToDateTime(Date_Now.Text).Date);


                // Calculate Late, Early Leave, Overtime, Work Hours, etc. here as needed

                TimeSpan? clockIn = AttendanceTime;
                TimeSpan? clockOff = DepartureTime;
                TimeSpan? onDuty = _selectedUser.Shift.StartTime;
                TimeSpan? offDuty = _selectedUser.Shift.EndTime;

                // Calculate Absence
                int absence = (clockIn == null && clockOff == null) ? 1 : 0;

                // Calculate Late (if ClockIn is after OnDuty)
                TimeSpan late = TimeSpan.Zero;
                if (clockIn != null && clockIn.Value > onDuty)
                {
                    late = TimeSpan.FromMinutes((clockIn.Value - onDuty.Value).TotalMinutes);
                }

                // Calculate Early (if ClockOff is before OffDuty)
                TimeSpan early = TimeSpan.Zero;
                if (clockOff != null && clockOff.Value < offDuty)
                {
                    early = TimeSpan.FromMinutes((offDuty.Value - clockOff.Value).TotalMinutes);
                }

                // Calculate Overtime (if ClockOff is after OffDuty)
                TimeSpan overtime = TimeSpan.Zero;
                if (clockOff != null && clockOff.Value > offDuty)
                {
                    overtime = TimeSpan.FromMinutes((clockOff.Value - offDuty.Value).TotalMinutes);
                }

                // Calculate WorkTime (time between OnDuty and OffDuty)
                TimeSpan workTime = TimeSpan.FromMinutes((offDuty.Value - onDuty.Value).TotalMinutes);

                // Calculate AttendTime (time between ClockIn and ClockOff)
                TimeSpan attendTime = TimeSpan.Zero;
                if (clockIn != null && clockOff != null)
                {
                    // Create TimeSpans for the times
                    TimeSpan clockInTime = clockIn.Value;
                    TimeSpan clockOffTime = clockOff.Value;

                    // Handle cases where ClockOff is on the next day
                    if (clockOffTime < clockInTime)
                    {
                        // Add a day to clockOffTime to handle crossing midnight
                        clockOffTime += TimeSpan.FromDays(1);
                    }

                    // Calculate the duration between ClockIn and ClockOff
                    attendTime = clockOffTime - clockInTime;
                }

                // Format TimeSpans to (HH:mm:ss)
                string formattedOnDuty = onDuty.Value.ToString(@"hh\:mm\:ss");
                string formattedOffDuty = offDuty.Value.ToString(@"hh\:mm\:ss");
                string formattedLate = late.ToString(@"hh\:mm\:ss");
                string formattedEarly = early.ToString(@"hh\:mm\:ss");
                string formattedOvertime = overtime.ToString(@"hh\:mm\:ss");
                string formattedWorkTime = workTime.ToString(@"hh\:mm\:ss");
                string formattedAttendTime = attendTime.ToString(@"hh\:mm\:ss");


                existingAttendance.Late = late;
                existingAttendance.EarlyLeave = early;
                existingAttendance.Overtime = overtime;
                existingAttendance.TotalWorkHours = attendTime;
                existingAttendance.ExemptLate = _selectedUser.ExemptLate;
                existingAttendance.ExemptEarlyLeave = _selectedUser.ExemptEarlyLeave;
                existingAttendance.ExemptOvertime = _selectedUser.ExemptOvertime;
                existingAttendance.IsAbsence = (absence == 1 && !_selectedUser.ExemptAbsence);
                existingAttendance.ExemptEarlyEnter = _selectedUser.ExemptEarlyEnter;
                existingAttendance.ShiftId = _selectedUser.ShiftId;
                existingAttendance.IsHoliday = false;
                existingAttendance.EarlyEnter = null;
                await _context.SaveChangesAsync();


                System.Windows.MessageBox.Show("تم اضافة الحركة");

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }


        
    }
    
}
