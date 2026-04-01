using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static HR_Application.EmployeeData;
using static MaterialDesignThemes.Wpf.Theme;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using Application = System.Windows.Application;
using Colors = System.Windows.Media.Colors;
using DataGrid = System.Windows.Controls.DataGrid;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for DataEdit.xaml
    /// </summary>
    public partial class DataEdit : Window
    {
        List<AttendData> dataList = new List<AttendData>();
        string shift = "";
        string branch = "";
        int branchI = 0;
        TimeSpan shiftFrom = new TimeSpan();
        TimeSpan shiftTo = new TimeSpan();
        List<bool> weekHoli = new List<bool>();
        Dictionary<string, int> _dates = new Dictionary<string, int>();
        private List<User> users = new List<User>();

        private AppDbContext _context;

        public ICommand OpenMonthlyDataCommand { get; }

        public DataEdit()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            job_box.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
            job_box.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            InitializeDateSelections();
            OpenMonthlyDataCommand = new RelayCommand(OpenMonthlyData);
        }

        private async void InitializeDateSelections()
        {
            var context = new AppDbContext(App.ConnectionString);
            month_box.ItemsSource = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
            year_box.ItemsSource = Enumerable.Range(2010, 21).ToList();
            month_box.SelectedItem = DateTime.Now.ToString("MMMM", CultureInfo.CurrentCulture);
            year_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            year_box.SelectedItem = DateTime.Now.Year;
            var branches = await context.Branches.ToListAsync();
            branch_box.ItemsSource = branches;
            branch_box.DisplayMemberPath = "Name";
            branch_box.SelectedValuePath = "Id";

            var dbUsers = _context.Users.ToList();

            users.AddRange(dbUsers);
            user_box.ItemsSource = users;
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string ConvertDate(DateTime dateTime)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            string formattedDate = dateTime.ToString("dd/MM/yyyy", enUS);
            return formattedDate;
        }

        private string ConvertTime(DateTime dateTime)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            string formattedDate = dateTime.ToString("hh:mm:ss tt", enUS);
            return formattedDate;
        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        private (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year)
        {
            try
            {
                int startDay = Convert.ToInt16(Properties.Settings.Default.StartOfMonth);
                int endDay = (month == 2 && Convert.ToInt16(Properties.Settings.Default.EndOfMonth) > 29) ? 29 : Convert.ToInt16(Properties.Settings.Default.EndOfMonth);
                if (shiftFrom > shiftTo)
                    endDay += 1;

                DateTime startDate = new DateTime(year, month, startDay);
                DateTime endDate = new DateTime(year, month, endDay);

                if (15 < startDay) startDate = startDate.AddMonths(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب تواريخ الشهر المخصص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                code_box.Text = user_box.SelectedValue.ToString();
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


        private void DataGet()
        {
            try
            {
                _dates.Clear();
                if (!string.IsNullOrEmpty(code_box.Text) && branch_box.SelectedValue != null)
                {
                    int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                    int year = Convert.ToInt16(year_box.Text);

                    // Get user data with EF
                    var userData = _context.Users
                        .Include(u => u.JobTitle)
                        .Include(u => u.Branch)
                        .Include(u => u.Shift)
                        .Include(u => u.WeekHoliday)
                        .FirstOrDefault(u => u.BranchId.ToString() == branch_box.SelectedValue.ToString() && u.Code == code_box.Text && App.userBranches.Contains(u.BranchId));

                    if (userData != null)
                    {
                        branchI = userData.BranchId;
                        branch = userData.Branch.Name;
                        user_box.SelectedValue = userData.Id;
                        job_box.Text = userData.JobTitle.Name;
                        shift = userData.Shift.Name;
                        shiftFrom = userData.Shift.StartTime;
                        shiftTo = userData.Shift.EndTime;

                        weekHoli.Clear();
                        if (userData.WeekHoliday != null)
                        {
                            weekHoli.Add(userData.WeekHoliday.Day1);
                            weekHoli.Add(userData.WeekHoliday.Day2);
                            weekHoli.Add(userData.WeekHoliday.Day3);
                            weekHoli.Add(userData.WeekHoliday.Day4);
                            weekHoli.Add(userData.WeekHoliday.Day5);
                            weekHoli.Add(userData.WeekHoliday.Day6);
                            weekHoli.Add(userData.WeekHoliday.Day7);
                        }

                        dataList.Clear();

                        int rowNumber = 1;
                        DateTime startDate;
                        DateTime endDate;

                        if (from_picker.SelectedDate != null && to_picker.SelectedDate != null)
                        {
                            startDate = from_picker.SelectedDate.Value;
                            endDate = to_picker.SelectedDate.Value;
                        }
                        else
                        {
                            (startDate, endDate) = GetCustomMonthDates(monthNumber, year);
                        }

                        // Get fingerprint data with EF
                        var fingerprintData = _context.FingerPrints
                            .Where(f => f.UserId == userData.Id &&
                                       f.FingerPrintDate >= startDate &&
                                       f.FingerPrintDate <= endDate)
                            .OrderBy(f => f.FingerPrintDate)
                            .ToList();

                        foreach (var fingerprint in fingerprintData)
                        {
                            DateTime d = fingerprint.FingerPrintDate;
                            string day = fingerprint.FingerPrintDate.DayOfWeek.ToString();

                            dataList.Add(new AttendData(
                                rowNumber++,
                                fingerprint.Id,
                                userData.Id,
                                d.ToString(),
                                (fingerprint.Status == 1) ? "حضور" : "انصراف",
                                fingerprint.Status.ToString(),
                                "_",
                                branch,
                                day,
                                "_"
                            ));

                            string dateKey = d.Date.ToString();
                            if (!_dates.ContainsKey(dateKey))
                            {
                                _dates.Add(dateKey, 1);
                            }
                            else
                            {
                                _dates[dateKey] += 1;
                            }
                        }

                        list.ItemsSource = dataList;
                        list.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show("Access Denied or User Not Found");
                    }
                }
                else
                {
                    MessageBox.Show("ادخل كود الموظف و اختار الفرع");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            add_print_grid.Visibility = Visibility.Collapsed;
            list.Visibility = Visibility.Visible;
            DataGet();
            night_btn.IsEnabled = true;
        }

        public class AttendData : INotifyPropertyChanged
        {
            private string _statusNo;
            private string _status;
            private string _dateEdit;

            public int rowNumber { get; set; }
            public string date { get; set; }
            public string status
            {
                get { return _status; }
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged("status");
                    }
                }
            }

            public string statusNo
            {
                get { return _statusNo; }
                set
                {
                    if (_statusNo != value)
                    {
                        _statusNo = value;
                        OnPropertyChanged("statusNo");
                    }
                }
            }

            public string dateEdit
            {
                get { return _dateEdit; }
                set
                {
                    if (_dateEdit != value)
                    {
                        _dateEdit = value;
                        OnPropertyChanged("dateEdit");
                    }
                }
            }
            public string procedures { get; set; }
            public int ID { get; set; }
            public int UserID { get; set; }
            public string branch { get; set; }
            public string day { get; set; }
            public string user { get; set; }

            public AttendData(int num, int id, int userId, string dateTime, string s, string sNo, string p, string b, string d, string u)
            {
                rowNumber = num;
                ID = id;
                UserID = userId;
                date = dateTime;
                dateEdit = dateTime;
                status = s;
                statusNo = sNo;
                procedures = p;
                branch = b;
                day = d;
                user = u;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            foreach (AttendData attend in dataList)
            {
                attend.status = (attend.status == "انصراف") ? "حضور" : "انصراف";
                attend.statusNo = (attend.statusNo == "1") ? "0" : "1";
            }
            list.ItemsSource = dataList;
            list.Items.Refresh();
        }

        private void OpenMonthlyData()
        {
            MonthlyData monthlyData = new MonthlyData(code_box.Text, month_box.Text, year_box.Text, branch_box.SelectedValue.ToString());
            monthlyData.ShowDialog();
        }

        private void SaveData()
        {
            try
            {
               
                    try
                    {
                        foreach (AttendData attend in dataList)
                        {
                            var fingerprint = _context.FingerPrints.FirstOrDefault(f => f.Id == attend.ID);
                            if (fingerprint != null)
                            {
                                fingerprint.Status = int.Parse(attend.statusNo);
                                fingerprint.FingerPrintDate = Convert.ToDateTime(attend.dateEdit);
                            }
                        }

                        _context.SaveChanges();

                        list.ItemsSource = null;
                        list.ItemsSource = dataList;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating database: {ex.Message}");
                    }
                
                UpdateAttendanceDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating database: {ex.Message}");
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveData();
                MessageBox.Show("Database updated successfully!");
                //DataGet();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void UpdateAttendanceDatabase()
        {
            try
            {
                var context = new AppDbContext(App.ConnectionString);
                var clockInRecords = new List<dynamic>();
                var clockOffRecords = new List<dynamic>();

                int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                int year = Convert.ToInt16(year_box.Text);
                (DateTime startDate, DateTime endDate) = GetCustomMonthDates(monthNumber, year);

                var dayMapping = new Dictionary<string, int>
                {
                    { "السبت", 0 }, { "الأحد", 1 }, { "الاثنين", 2 }, { "الثلاثاء", 3 },
                    { "الأربعاء", 4 }, { "الخميس", 5 }, { "الجمعة", 6 }
                };

                var user = context.Users.FirstOrDefault(u => u.Code == code_box.Text && u.BranchId.ToString() == branch_box.SelectedValue.ToString());
                if (user == null) return;

                foreach (var data in dataList)
                {
                    TimeSpan timeOff = shiftTo;
                    TimeSpan timeOn = shiftFrom;

                    var record = new
                    {
                        Code = code_box.Text,
                        Name = user_box.Text,
                        DayDate = Convert.ToDateTime(data.dateEdit).Date,
                        Shift = shift,
                        OnDuty = timeOn,
                        OffDuty = timeOff,
                        ClockIn = (data.statusNo == "1") ? Convert.ToDateTime(data.dateEdit) : (DateTime?)null,
                        ClockOff = (data.statusNo == "0") ? Convert.ToDateTime(data.dateEdit) : (DateTime?)null,
                        BranchIn = branchI.ToString(),
                        BranchOff = branchI.ToString(),
                        AllowLate = user.ExemptLate,
                        AllowEndJob = user.ExemptEarlyLeave,
                        AllowOVA = user.ExemptOvertime,
                        AllowOVB = user.ExemptEarlyEnter
                    };

                    if (record.ClockIn != null)
                        clockInRecords.Add(record);
                    if (record.ClockOff != null)
                        clockOffRecords.Add(record);
                }

                // Combine records and process attendance data
                var combinedRecords = clockInRecords
                    .Join(
                        clockOffRecords,
                        ci => new { ci.Code, ci.DayDate },
                        co => new { co.Code, co.DayDate },
                        (ci, co) => new
                        {
                            ci.Code,
                            ci.Name,
                            co.DayDate,
                            ci.Shift,
                            ci.OnDuty,
                            ci.OffDuty,
                            ClockIn = ci.ClockIn,
                            ClockOff = co.ClockOff,
                            BranchCode = ci.BranchIn,
                            DBranchCode = co.BranchOff,
                            ci.AllowLate,
                            ci.AllowEndJob,
                            ci.AllowOVA,
                            ci.AllowOVB
                        })
                    .ToList();

                // Delete existing attendance records for the period
                var existingAttendances = context.Attendances
                    .Where(a => a.UserId == user.Id && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
                    .ToList();

                context.Attendances.RemoveRange(existingAttendances);
                await context.SaveChangesAsync();

                CultureInfo arabicCulture = new CultureInfo("ar-SA");

                // Process each day in the period
                for (DateTime current = startDate; current <= endDate; current = current.AddDays(1))
                {
                    var attendanceRecord = combinedRecords.FirstOrDefault(r => r.DayDate == current);
                    int dayIndex = dayMapping[arabicCulture.DateTimeFormat.DayNames[(int)current.DayOfWeek]];
                    bool isHoliday = (dayIndex < weekHoli.Count && weekHoli[dayIndex]);
                    bool isAbsence = false;

                    // تحديد إذا كان اليوم غياب أو إجازة
                    if (attendanceRecord == null || (attendanceRecord.ClockIn == null && attendanceRecord.ClockOff == null))
                    {
                        if (isHoliday)
                        {
                            // إجازة
                            isAbsence = false;
                        }
                        else
                        {
                            // غياب
                            isAbsence = true;
                        }
                    }

                    var attendance = new Attendance
                    {
                        UserId = user.Id,
                        AttendanceDate = current,
                        CheckInTime = attendanceRecord?.ClockIn,
                        CheckOutTime = attendanceRecord?.ClockOff,
                        CheckInBranchId = branchI,
                        CheckOutBranchId = branchI,
                        ExemptLate = false,
                        ExemptEarlyLeave = false,
                        ExemptOvertime = false,
                        ExemptEarlyEnter = false,
                        IsAbsence = isAbsence,
                        IsHoliday = isHoliday,
                        ShiftId = user.ShiftId
                    };

                    // Calculate time differences and work hours only if not absence or holiday
                    if (!isAbsence && !isHoliday && attendanceRecord != null && 
                        attendanceRecord.ClockIn != null && attendanceRecord.ClockOff != null)
                    {
                        // Calculate late
                        if (attendanceRecord.ClockIn.TimeOfDay > attendanceRecord.OnDuty)
                        {
                            attendance.Late = attendanceRecord.ClockIn.TimeOfDay - attendanceRecord.OnDuty;
                        }

                        // Calculate early leave
                        if (attendanceRecord.ClockOff.TimeOfDay < attendanceRecord.OffDuty)
                        {
                            attendance.EarlyLeave = attendanceRecord.OffDuty - attendanceRecord.ClockOff.TimeOfDay;
                        }

                        // Calculate early enter
                        if (attendanceRecord.ClockIn.TimeOfDay < attendanceRecord.OnDuty)
                        {
                            attendance.EarlyEnter = attendanceRecord.OnDuty - attendanceRecord.ClockIn.TimeOfDay;
                        }

                        // Calculate overtime
                        if (attendanceRecord.ClockOff.TimeOfDay > attendanceRecord.OffDuty)
                        {
                            attendance.Overtime = attendanceRecord.ClockOff.TimeOfDay - attendanceRecord.OffDuty;
                        }
                        if (attendanceRecord.ClockOff.TimeOfDay > attendanceRecord.ClockIn.TimeOfDay)
                            attendance.TotalWorkHours = attendanceRecord.ClockOff.TimeOfDay - attendanceRecord.ClockIn.TimeOfDay;
                        else
                            attendance.TotalWorkHours = TimeSpan.FromHours(24) - attendanceRecord.ClockIn.TimeOfDay + attendanceRecord.ClockOff.TimeOfDay;

                        // Calculate total work hours
                    }
                    else
                    {
                        // Reset all time calculations for absence or holiday
                        attendance.Late = null;
                        attendance.EarlyLeave = null;
                        attendance.EarlyEnter = null;
                        attendance.Overtime = null;
                        attendance.TotalWorkHours = null;
                    }

                    context.Attendances.Add(attendance);
                }
                
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                 using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance Data");

                // Add header row
                worksheet.Cell(1, 1).Value = "م";
                worksheet.Cell(1, 2).Value = "التاريخ";
                worksheet.Cell(1, 3).Value = "الحالة";
                worksheet.Cell(1, 4).Value = "الفرع";
                worksheet.Cell(1, 5).Value = "الاجراءات";
                worksheet.Cell(1, 6).Value = "اليوم";
                worksheet.Cell(1, 7).Value = "المستخدم";

                // Add data rows
                for (int i = 0; i < dataList.Count; i++)
                {
                    var data = dataList[i];
                    worksheet.Cell(i + 2, 1).Value = data.rowNumber;
                    worksheet.Cell(i + 2, 2).Value = data.dateEdit;
                    worksheet.Cell(i + 2, 3).Value = data.status;
                    worksheet.Cell(i + 2, 4).Value = data.branch;
                    worksheet.Cell(i + 2, 5).Value = data.procedures;
                    worksheet.Cell(i + 2, 6).Value = data.day;
                    worksheet.Cell(i + 2, 7).Value = data.user;
                }

                // Save the workbook to a file
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = "AttendanceData.xlsx";
                if (saveFileDialog.ShowDialog() == true)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                }
                MessageBox.Show("تم استخراج الاكسيل!");

            }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ");
            }
        }

        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = list.SelectedItems.Cast<AttendData>().ToList();
            if (selectedItems != null && selectedItems.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show(
                    "هل انت متأكد من حذف هذه البصمة ؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var selectedRow in selectedItems)
                    {
                        var fingerprint = _context.FingerPrints.FirstOrDefault(f => f.Id == selectedRow.ID);
                        if (fingerprint != null)
                        {
                            _context.FingerPrints.Remove(fingerprint);
                        }
                    }

                    _context.SaveChanges();
                    DataGet();
                }
            }
        }

        private void DeleteDuplicate()
        {
            try
            {
                DateTime current = DateTime.Now;
                foreach (AttendData attend in dataList)
                {
                    DateTime dateTime = Convert.ToDateTime(attend.date);

                    if (dateTime >= current && dateTime <= current.AddMinutes(30))
                    {
                        var fingerprint = _context.FingerPrints.FirstOrDefault(f => f.Id == attend.ID);
                        if (fingerprint != null)
                        {
                            _context.FingerPrints.Remove(fingerprint);
                        }
                    }
                    current = dateTime;
                }

                _context.SaveChanges();
                DataGet();
                MessageBox.Show("تم حذف المكرر");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UpdateRows(int rowNum)
        {
            for (int i = rowNum - 1; i < dataList.Count; ++i)
            {
                AttendData attend = dataList[i];
                attend.rowNumber = i + 1;
                attend.statusNo = attend.statusNo == "1" ? "0" : "1";
                attend.status = attend.status == "حضور" ? "انصراف" : "حضور";
            }
            list.ItemsSource = null;
            list.ItemsSource = dataList;
            list.Items.Refresh();
        }

        private void deleteDuplicate_btn_Click(object sender, RoutedEventArgs e)
        {
            DeleteDuplicate();
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var selectedI = list.SelectedItem;
                if (selectedI != null)
                {
                    var selectedRow = (AttendData)selectedI;
                    int rowNum = selectedRow.rowNumber;
                    string currentStatus = selectedRow.statusNo;
                    bool isDifferent = false;

                    foreach (AttendData attend in dataList)
                    {
                        if (attend.ID == selectedRow.ID)
                        {
                            attend.status = (attend.status == "حضور" ? "انصراف" : "حضور");
                            attend.statusNo = (attend.statusNo == "0") ? "1" : "0";
                            currentStatus = attend.statusNo;

                            // Update in database
                            var fingerprint = _context.FingerPrints.FirstOrDefault(f => f.Id == attend.ID);
                            if (fingerprint != null)
                            {
                                fingerprint.Status = int.Parse(attend.statusNo);
                            }
                        }
                        if (attend.rowNumber == (rowNum + 1) && currentStatus == attend.statusNo)
                        {
                            isDifferent = true;
                            break;
                        }
                    }

                    if (isDifferent)
                    {
                        UpdateRows(rowNum + 1);
                    }

                    _context.SaveChanges();
                    list.ItemsSource = dataList;
                    list.Items.Refresh();
                }
            }
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            list.Visibility = Visibility.Collapsed;
            add_print_grid.Visibility = Visibility.Visible;
        }

        private void add_print_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(code_box.Text))
                {
                    var user = _context.Users
                        .Include(u => u.Branch)
                        .FirstOrDefault(u => u.Code== code_box.Text && App.userBranches.Contains(u.BranchId));

                    if (user != null)
                    {
                        DateTime date = add_date_picker.SelectedDate.Value;
                        string statusNo = (att_radio.IsChecked.Value) ? "1" : "0";
                        string status = (statusNo == "1") ? "حضور" : "انصراف";
                        DateTime time = time_picker.SelectedTime.Value;
                        DateTime fullTime = date.Date + time.TimeOfDay;

                        var fingerprint = new FingerPrint
                        {
                            UserId = user.Id,
                            FingerPrintDate = fullTime,
                            Status = int.Parse(statusNo),
                            BranchId = user.BranchId,
                            MachineId = null // Manual entry
                        };

                        _context.FingerPrints.Add(fingerprint);
                        _context.SaveChanges();

                        MessageBox.Show("تم إضافة البصمة بنجاح");
                        list.Visibility = Visibility.Visible;
                        add_print_grid.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show("User not found or access denied");
                    }
                }
                else
                {
                    MessageBox.Show("ادخل كود الموظف");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                list.Visibility = Visibility.Visible;
                add_print_grid.Visibility = Visibility.Collapsed;
            }
        }

        private void night_btn_Click(object sender, RoutedEventArgs e)
        {
            ConfirmMessage msgBox = new ConfirmMessage("هل أنت متأكد من مراجعة البيانات قبل المعالجة ؟", "متأكد", "لا");
            msgBox.ShowDialog();
            bool result = msgBox.Result;

            if (result)
            {
                foreach (var data in dataList)
                {
                    if (data.statusNo == "0")
                    {
                        data.dateEdit = Convert.ToDateTime(data.date).AddDays(-1).ToString("MM/dd/yyyy HH:mm:ss");
                        data.date = Convert.ToDateTime(data.date).AddDays(-1).ToString("MM/dd/yyyy HH:mm:ss");

                        var fingerprint = _context.FingerPrints.FirstOrDefault(f => f.Id == data.ID);
                        if (fingerprint != null)
                        {
                            fingerprint.FingerPrintDate = Convert.ToDateTime(data.dateEdit);
                        }
                    }
                }

                _context.SaveChanges();
                list.Items.Refresh();
                night_btn.IsEnabled = false;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.S)
                {
                    MonthlyData monthlyData = new MonthlyData(code_box.Text, month_box.Text, year_box.Text, branch_box.SelectedValue.ToString());
                    monthlyData.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error");
            }
        }

        private void list_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.IsKeyDown(Key.F2) || e.Key == Key.F2)
                {
                    int selectedIndex = list.SelectedIndex;
                    if (selectedIndex >= 0)
                    {
                        int insertIndex = selectedIndex + 1;
                        var selectedData = dataList[selectedIndex];

                        var newFingerprint = new FingerPrint
                        {
                            UserId = _context.Users.First(u => u.Code == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString()).Id,
                            FingerPrintDate = Convert.ToDateTime(selectedData.date).Date,
                            Status = (selectedData.statusNo == "0") ? 1 : 0,
                            BranchId = branchI,
                            MachineId = null
                        };

                        _context.FingerPrints.Add(newFingerprint);
                        _context.SaveChanges();

                        dataList.Insert(insertIndex, new AttendData(
                            insertIndex + 1,
                            newFingerprint.Id,
                            Convert.ToInt32(code_box.Text),
                            newFingerprint.FingerPrintDate.ToString(),
                            (newFingerprint.Status == 1) ? "حضور" : "انصراف",
                            newFingerprint.Status.ToString(),
                            "_",
                            branch,
                            selectedData.day,
                            "_"
                        ));

                        SaveData();
                        list.Items.Refresh();
                        list.SelectedIndex = insertIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void list_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var selectedRow = e.Row.DataContext as AttendData;
            if (selectedRow != null)
            {
                if (_dates.ContainsKey(Convert.ToDateTime(selectedRow.date).Date.ToString()))
                {
                    if (_dates[Convert.ToDateTime(selectedRow.date).Date.ToString()] == 1)
                    {
                        e.Row.Background = new SolidColorBrush(Colors.LightBlue);
                    }
                    else if (_dates[Convert.ToDateTime(selectedRow.date).Date.ToString()] > 2)
                    {
                        e.Row.Background = new SolidColorBrush(Colors.LightGreen);
                    }
                    else
                    {
                        e.Row.Background = new SolidColorBrush(Colors.Transparent);
                    }

                    if ((Convert.ToDateTime(selectedRow.date).Hour >= 0 && Convert.ToDateTime(selectedRow.date).Hour <= 5) && selectedRow.statusNo == "0")
                    {
                        e.Row.Background = new SolidColorBrush(Colors.Yellow);
                    }

                    DataGrid dataGrid = sender as DataGrid;
                    foreach (var column in dataGrid.Columns)
                    {
                        if (column is DataGridTextColumn textColumn)
                        {
                            // Check if it's the column you want to color
                            if (textColumn.Binding is System.Windows.Data.Binding binding && binding.Path.Path == "status")
                            {
                                // Get the cell and modify its style
                                DataGridCell cell = GetCell(e.Row, column);
                                if (cell != null)
                                {
                                    if (selectedRow.statusNo == "0")
                                    {
                                        cell.Background = new SolidColorBrush(Colors.Red);
                                    }
                                    else
                                    {
                                        cell.Background = new SolidColorBrush(Colors.LightGreen);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private DataGridCell GetCell(DataGridRow row, DataGridColumn column)
        {
            if (column.GetCellContent(row) is FrameworkElement cellContent)
            {
                return cellContent.Parent as DataGridCell;
            }
            return null;
        }

        public async Task InsertAttendanceDataAsync()
        {
            try
            {
                var progressDialog = new ProgressDialog
                {
                    Owner = this
                };


                // متغير لتتبع الإلغاء
                bool isCancelled = false;

                // عرض نافذة التحميل
                progressDialog.Show();

                progressDialog.UpdateStatus("جاري سحب الحركات...");


                var context = new AppDbContext(App.ConnectionString);
                // الحصول على البيانات من الجدول المؤقت machineData
                var machineDataList = await context.MachineData
                    .Where(m => m.UserID.ToString() == code_box.Text && m.BranchCode.ToString() == branch_box.SelectedValue.ToString())
                    .ToListAsync();

                int recordsInserted = 0;

                var user = await context.Users.FirstOrDefaultAsync(u => u.Code == code_box.Text && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                foreach (var machineData in machineDataList)
                {
                    // التحقق من عدم وجود التسجيل مسبقاً في FingerPrint
                    var existingRecord = await context.FingerPrints
                        .Include(f => f.User)
                        .FirstOrDefaultAsync(fp =>
                            fp.User.Code == machineData.UserID.ToString() &&
                            fp.FingerPrintDate == machineData.TDate &&
                            fp.User.BranchId == machineData.BranchCode);

                    if (existingRecord == null)
                    {
                        // إنشاء سجل جديد في FingerPrint
                        var fingerPrint = new FingerPrint
                        {
                            UserId = user.Id,
                            FingerPrintDate = machineData.TDate,
                            Status = machineData.StatusNo,
                            BranchId = machineData.BranchCode,
                        };

                        await context.FingerPrints.AddAsync(fingerPrint);
                        recordsInserted++;
                    }
                }

                await context.SaveChangesAsync();

                progressDialog.Closing += (s, args) =>
                {
                    if (progressDialog.IsCancelled)
                    {
                        isCancelled = true;
                    }
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"تم إدخال {recordsInserted} سجل جديد", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                    progressDialog.Close();
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في إدخال البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task InsertData()
        {
            try
            {
                ConfirmMessage msgBox = new ConfirmMessage("هل تريد سحب البيانات ؟", "متأكد", "لا");
                msgBox.ShowDialog();
                bool result = msgBox.Result;

                if (result)
                {
                    DateTime startDate;
                    DateTime endDate;
                    int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                    int year = Convert.ToInt16(year_box.Text);

                    if (from_picker.SelectedDate != null && to_picker.SelectedDate != null)
                    {
                        startDate = from_picker.SelectedDate.Value;
                        endDate = to_picker.SelectedDate.Value;
                    }
                    else
                    {
                        (startDate, endDate) = GetCustomMonthDates(monthNumber, year);
                    }

                    var user = _context.Users.FirstOrDefault(u => u.Code == code_box.Text && u.BranchId.ToString() == branch_box.SelectedValue.ToString());
                    if (user != null)
                    {
                        await InsertAttendanceDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async void get_btn_Click(object sender, RoutedEventArgs e)
        {
            await InsertData();
        }
    }
}