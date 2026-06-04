using ClosedXML.Excel;
using DocumentFormat.OpenXml.Math;
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class MonthlySalaryData : System.Windows.Window, INotifyPropertyChanged
    {
        private ObservableCollection<AttendanceRecord> _monthDatas = new ObservableCollection<AttendanceRecord>();
        public ObservableCollection<AttendanceRecord> MonthDatas
        {
            get => _monthDatas;
            set
            {
                _monthDatas = value;
                OnPropertyChanged(nameof(MonthDatas));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<AttendanceRecord> attend = new ObservableCollection<AttendanceRecord>();
        private List<Employee> users = new List<Employee>();
        private List<DateTime> dates = new List<DateTime>();
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private MonthSettings monthSettings;
        private string shift;
        private string totalWHMain;
        private string totalLateMain;
        private string totalEarlyMain;
        private string totalINEarlyMain;
        private string totalOTMain;
        private string TotalWHEmployee;
        int totalWHHours = 0;
        TimeOnly totalHours = TimeOnly.MinValue;
        int totalWHMin = 0;
        int totalWHSec = 0;
        TimeOnly t_hours = TimeOnly.MinValue;
        int totalAbsences = 0;
        decimal minSalary = 0;
        decimal TotalOTValue = 0;
        decimal TotalLateValue = 0;

        int totalWeeklyRest = 0;
        int exemptLate = 0;
        int exemptEarly = 0;
        int exemptINEarly = 0;
        int exemptOT = 0;
        int totalLateHours = 0;
        int totalLateMin = 0;
        int totalOvertimeHours = 0;
        int totalOvertimeMin = 0;
        int totalOvertimeSec = 0;
        int totalEarlyHours = 0;
        int totalINEarlyHours = 0;
        int totalEarlyMin = 0;
        int totalINEarlyMin = 0;
        TimeSpan shiftFrom;
        TimeSpan shiftTo;
        string WH;
        List<bool> weekHoli = new List<bool>();
        private Dictionary<DateTime, string> allMonth = new Dictionary<DateTime, string>();
        private string branch, IP;
        private CultureInfo arabicCulture = new CultureInfo("ar-SA");
        private bool IsAccess = false;
        LoadingBar loadingBar;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MonthlySalaryData()
        {
            InitializeComponent();
            DataContext = this;
            dataGrid.ItemsSource = _monthDatas;
            dataGrid.LoadingRow += dataTable_LoadingRow;
            InitializeDateSelections();
            LoadData();
            loadingBar = new LoadingBar(loadingProgressBar);
        }

        public MonthlySalaryData(string code, string month, string year)
        {
            InitializeComponent();
            DataContext = this;
            dataGrid.ItemsSource = _monthDatas;
            dataGrid.LoadingRow += dataTable_LoadingRow;
            InitializeDateSelections();
            LoadData();
            loadingBar = new LoadingBar(loadingProgressBar);
            code_box.Text = code;
            month_box.SelectedItem = month;
            year_box.Text = year;
            DataGet();
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

            var itemsList = comboBox.Tag as List<Employee>;

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
                    .Where(item => item.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = filteredItems;
            }

            comboBox.IsDropDownOpen = true;
            textBox.Text = searchText;
            textBox.CaretIndex = searchText.Length;
        }

        private async System.Threading.Tasks.Task LoadAttendanceAndDepartureData()
        {
            totalAbsences = 0;
            totalWeeklyRest = 0;
            totalLateHours = 0;
            totalLateMin = 0;
            totalOvertimeHours = 0;
            TotalOTValue = 0;
            TotalLateValue = 0;
            totalOvertimeMin = 0;
            totalOvertimeSec = 0;
            totalEarlyHours = 0;
            totalINEarlyHours = 0;
            totalEarlyMin = 0;
            totalINEarlyMin = 0;
            totalWHHours = 0;
            totalHours = TimeOnly.MinValue;
            totalWHMin = 0;
            totalWHSec = 0;
            t_hours = TimeOnly.MinValue;

            var dayMapping = new Dictionary<string, int>
            {
                { "«·”» ", 0 }, { "«·√Õœ", 1 }, { "«·«À‰Ì‰", 2 }, { "«·À·«À«¡", 3 },
                { "«·√—»⁄«¡", 4 }, { "«·Œ„Ì”", 5 }, { "«·Ã„⁄…", 6 }
            };

            int lateType = Properties.Settings.Default.LateType;
            string lateCondition = lateType == 0 ? "AND Money = 0" : "AND Money = 1";

            var user = users.FirstOrDefault(u => u.Code == code_box.Text);
            int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
            int year = Convert.ToInt16(year_box.Text);
            (DateTime startMonth, DateTime endMonth) = GetCustomMonthDates(monthNumber, year);

            var attendances = await _context.Attendances
                .Include(a => a.Shift)
                .Include(a => a.CheckInBranch)
                .Include(a => a.CheckOutBranch)
                .Include(a => a.User)
                    .ThenInclude(u => u.Shift)
                .Include(a => a.User)
                    .ThenInclude(u => u.WeekHoliday)
                .Where(a => a.User.Code.ToString() == code_box.Text && a.User.BranchId.ToString() == branch_box.SelectedValue.ToString() &&
                           a.AttendanceDate >= startMonth &&
                           a.AttendanceDate <= endMonth)
                .ToListAsync();

            // Ã·» Ãœ«Ê· «· √ŒÌ— Ê«·√÷«›Ì
            var lateRates = await _context.LateOvertimes
                .Where(l => l.Type == 0 && (lateType == 0 ? l.MoneyType == 0 : l.MoneyType == 1))
                .ToListAsync();

            var overtimeRates = await _context.LateOvertimes
                .Where(o => o.Type == 1 && (lateType == 0 ? o.MoneyType == 0 : o.MoneyType == 1))
                .ToListAsync();

            var dataDict = new Dictionary<DateTime, AttendanceRecord>();

            foreach (var att in attendances)
            {
                // Õ”«» ﬁÌ„… «· √ŒÌ— Ê«·√÷«›Ì
                decimal lateValue = 0;
                decimal otValue = 0;

                if (att.Late.HasValue)
                {
                    var lateRate = lateRates.FirstOrDefault(l =>
                        att.Late.Value > l.StartTime && att.Late.Value < l.EndTime);

                    if (lateRate != null)
                    {
                        if (lateType == 0)
                        {
                            lateValue = TimeSpanToDecimal(att.Late.Value) * lateRate.Value * minSalary;
                        }
                        else
                        {
                            lateValue = lateRate.Value;
                        }
                    }
                }

                if (att.Overtime.HasValue)
                {
                    var otRate = overtimeRates.FirstOrDefault(o =>
                        att.Overtime.Value > o.StartTime && att.Overtime.Value < o.EndTime);

                    if (otRate != null)
                    {
                        if (lateType == 0)
                        {
                            otValue = TimeSpanToDecimal(att.Overtime.Value) * otRate.Value * minSalary;
                        }
                        else
                        {
                            otValue = otRate.Value;
                        }
                    }
                }

                TotalLateValue += lateValue;
                TotalOTValue += otValue;

                totalLateHours += att.Late?.Hours ?? 0;
                totalLateMin += att.Late?.Minutes ?? 0;
                totalOvertimeHours += att.Overtime?.Hours ?? 0;
                totalOvertimeMin += att.Overtime?.Minutes ?? 0;
                totalOvertimeSec += att.Overtime?.Seconds ?? 0;
                totalEarlyHours += att.EarlyLeave?.Hours ?? 0;
                totalINEarlyHours += att.EarlyEnter?.Hours ?? 0;
                totalEarlyMin += att.EarlyLeave?.Minutes ?? 0;
                totalINEarlyMin += att.EarlyEnter?.Minutes ?? 0;

                bool isAbsenceFromDB = att.IsAbsence;
                bool isHolidayFromDB = att.IsHoliday;

                var record = new AttendanceRecord
                {
                    Day = arabicCulture.DateTimeFormat.DayNames[(int)att.AttendanceDate.DayOfWeek],
                    Date = att.AttendanceDate,
                    AttendanceTime = att.CheckInTime,
                    DepartureTime = att.CheckOutTime,
                    DutyOn = att.Shift?.StartTime,
                    DutyOff = att.Shift?.EndTime,
                    Shift = att.Shift?.Name,
                    AttendBranch = (string.IsNullOrEmpty(att.CheckInLocation)) ? (att.CheckInBranch?.Name ?? "-") : att.CheckInLocation,
                    DepartBranch = (string.IsNullOrEmpty(att.CheckOutLocation)) ? (att.CheckOutBranch?.Name ?? "-") : (att.CheckOutLocation),
                    ExemptEarly = att.User.ExemptEarlyLeave,
                    ExemptINEarly = att.User.ExemptEarlyEnter,
                    ExemptLate = att.User.ExemptLate,
                    ExemptOT = att.User.ExemptOvertime,
                    LateValue = lateValue.ToString("N2"),
                    OTValue = otValue.ToString("N2"),
                    isAbsence = isAbsenceFromDB,
                    isHoliday = isHolidayFromDB,
                    WorkHours = att.TotalWorkHours,
                    Late = att.Late,
                    OverTime = att.Overtime
                };

                dataDict[att.AttendanceDate] = record;

                if (isHolidayFromDB)
                {
                    totalWeeklyRest++;
                }
                else if (isAbsenceFromDB)
                {
                    totalAbsences++;
                }
            }

            attend.Clear();
            for (DateTime current = startMonth; current <= endMonth; current = current.AddDays(1))
            {
                if (dataDict.TryGetValue(current, out var record))
                {
                    totalWHHours += record.WorkHours?.Hours ?? 0;
                    totalWHMin += record.WorkHours?.Minutes ?? 0;
                }
                else
                {
                    int dayIndex = dayMapping[arabicCulture.DateTimeFormat.DayNames[(int)current.DayOfWeek]];
                    bool holi = (dayIndex < weekHoli.Count && weekHoli[dayIndex]);

                    record = CreateDefaultRecord(current, holi);

                    if (holi)
                    {
                        totalWeeklyRest++;
                    }
                    else
                    {
                        totalAbsences++;
                    }
                }
                attend.Add(record);
            }
        }

        public static decimal TimeSpanToDecimal(TimeSpan timeSpan)
        {
            if (timeSpan.Hours > 0 || timeSpan.Minutes > 0)
            {
                decimal hours = timeSpan.Hours * 60;
                decimal minutes = timeSpan.Minutes;
                decimal totalHours = hours + minutes;
                return totalHours;
            }
            return 0;
        }

        private async Task UpdateDatabaseAsync(AttendanceRecord record)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var attendance = await _context.Attendances
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.User.Code.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == a.User.BranchId.ToString() && a.AttendanceDate == record.Date);

                    if (attendance == null)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == code_box.Text);
                        if (user != null)
                        {
                            attendance = new Attendance
                            {
                                UserId = user.Id,
                                AttendanceDate = record.Date,
                                CheckInBranchId = user.BranchId,
                                CheckOutBranchId = user.BranchId,
                                IsAbsence = record.isAbsence,
                                IsHoliday = record.isHoliday,
                                ShiftId = user.ShiftId
                            };
                            _context.Attendances.Add(attendance);
                        }
                    }

                    if (attendance != null)
                    {
                        attendance.CheckInTime = record.AttendanceTime;
                        attendance.CheckOutTime = record.DepartureTime;
                        attendance.IsAbsence = record.isAbsence;
                        attendance.IsHoliday = record.isHoliday;

                        if (!record.isAbsence && !record.isHoliday &&
                            record.AttendanceTime.HasValue && record.DepartureTime.HasValue)
                        {
                            await RecalculateAttendanceTimes(record, record.DutyOn.Value, record.DutyOff.Value);
                        }
                        else
                        {
                            attendance.Late = null;
                            attendance.EarlyLeave = null;
                            attendance.EarlyEnter = null;
                            attendance.Overtime = null;
                            attendance.TotalWorkHours = null;
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        await LoadAttendanceData();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    LocalizationManager.ShowMessage(ex.Message);
                }
            }
        }

        private async void UpdateData()
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var record in _monthDatas)
                        {
                            var user = await _context.Users.FirstOrDefaultAsync(u => u.Code.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString());
                            if (user == null) continue;

                            var attendance = await _context.Attendances
                                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AttendanceDate == record.Date);

                            if (attendance == null)
                            {
                                attendance = new Attendance
                                {
                                    UserId = user.Id,
                                    AttendanceDate = record.Date,
                                    CheckInBranchId = user.BranchId,
                                    CheckOutBranchId = user.BranchId
                                };
                                _context.Attendances.Add(attendance);
                            }

                            attendance.CheckInTime = record.AttendanceTime;
                            attendance.CheckOutTime = record.DepartureTime;
                            attendance.ExemptLate = record.ExemptLate;
                            attendance.ExemptEarlyLeave = record.ExemptEarly;
                            attendance.ExemptOvertime = record.ExemptOT;
                            attendance.ExemptEarlyEnter = record.ExemptINEarly;
                            attendance.IsAbsence = record.isAbsence;
                            attendance.IsHoliday = record.isHoliday;

                            if (!record.isAbsence && !record.isHoliday &&
                                record.AttendanceTime.HasValue && record.DepartureTime.HasValue &&
                                record.DutyOn.HasValue && record.DutyOff.HasValue)
                            {
                                await RecalculateAttendanceTimes(record, record.DutyOn.Value, record.DutyOff.Value);
                            }
                            else
                            {
                                attendance.Late = null;
                                attendance.EarlyLeave = null;
                                attendance.EarlyEnter = null;
                                attendance.Overtime = null;
                                attendance.TotalWorkHours = null;
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        await LoadAttendanceData();
                        LocalizationManager.ShowMessage(" „  ÕœÌÀ «·»Ì«‰«  »‰Ã«Õ");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        LocalizationManager.ShowMessage($"Œÿ√ ›Ì  ÕœÌÀ «·»Ì«‰« : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}");
            }
        }

        private void AttendanceDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                var attendanceRecord = e.Row.DataContext as AttendanceRecord;
                if (attendanceRecord != null)
                {
                    if (attendanceRecord.isHoliday)
                    {
                        e.Row.Background = new SolidColorBrush(Colors.Yellow);
                    }
                    else if (attendanceRecord.isAbsence)
                    {
                        e.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e85e4f"));
                    }
                    else
                    {
                        e.Row.Background = Brushes.Transparent;
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
            }
        }

        private async void MakeAbsence_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is AttendanceRecord selectedRow)
            {
                foreach (var item in dataGrid.Items)
                {
                    var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null && row.DataContext == selectedRow)
                    {
                        row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e85e4f"));
                        break;
                    }
                }

                selectedRow.AttendanceTime = null;
                selectedRow.DepartureTime = null;
                selectedRow.isAbsence = true;
                selectedRow.isHoliday = false;

                await UpdateDatabaseAsync(selectedRow);
                RefreshDataGrid();
            }
        }

        private async void MakeHoliday_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = dataGrid.SelectedItem as AttendanceRecord;
            if (selectedRow != null)
            {
                foreach (var item in dataGrid.Items)
                {
                    var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null && row.DataContext == selectedRow)
                    {
                        row.Background = new SolidColorBrush(Colors.Yellow);
                        break;
                    }
                }

                selectedRow.AttendanceTime = null;
                selectedRow.DepartureTime = null;
                selectedRow.isHoliday = true;
                selectedRow.isAbsence = false;

                await UpdateDatabaseAsync(selectedRow);
                RefreshDataGrid();
            }
        }

        private async void CancelAbsence_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is AttendanceRecord selectedRow)
            {
                selectedRow.AttendanceTime = selectedRow.Date.Date + shiftFrom;
                selectedRow.DepartureTime = selectedRow.Date.Date + shiftTo;
                selectedRow.isAbsence = false;
                selectedRow.isHoliday = false;
                selectedRow.DutyOn = shiftFrom;
                selectedRow.DutyOff = shiftTo;

                await UpdateDatabaseAsync(selectedRow);
                await LoadAttendanceData();
            }
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

        private void InitializeDateSelections()
        {
            month_box.SelectedItem = DateTime.Now.ToString("MMMM", CultureInfo.CurrentCulture);
            year_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            year_box.SelectedItem = DateTime.Now.Year;
        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
            if (e.ClickCount == 2) ToggleWindowState();
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private async void Start()
        {
            loadingBar.ShowLoadingIndicator();
            await InsertAttendanceData();
            loadingBar.HideLoadingIndicator();
        }

        private async void DataGet()
        {
            try
            {
                user_box.SelectedValue = code_box.Text;
                await LoadAttendanceData();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void data_show_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(code_box.Text) || branch_box.SelectedValue == null)
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ﬂ «»… ﬂÊœ «·„ÊŸ› Ê «Œ Ì«— «·›—⁄", "ŒŸ√", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DataGet();
        }

        private async System.Threading.Tasks.Task LoadAttendanceData()
        {
            ClearData();
            loadingBar.ShowLoadingIndicator();

            await LoadEmployeeData();
            if (IsAccess)
            {
                await LoadAttendanceAndDepartureData();
                loadingBar.HideLoadingIndicator();
                RefreshDataGrid();
            }
            else
            {
                LocalizationManager.ShowMessage("Access Denied");
            }

            _monthDatas = new ObservableCollection<AttendanceRecord>(attend);
        }

        private void MakeDeduction_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is AttendanceRecord selectedRow)
            {
                ErrandsWindow window = new ErrandsWindow(code_box.Text,  (int)branch_box.SelectedValue,
                    selectedRow.DepartureTime?.TimeOfDay ?? selectedRow.Date.TimeOfDay,
                    selectedRow.AttendanceTime?.TimeOfDay ?? selectedRow.Date.TimeOfDay,
                    selectedRow.Date);
                window.FromDateChanged += UpdateFromDateInDataGrid;
                window.ToDateChanged += UpdateToDateInDataGrid;
                window.ShowDialog();
            }
        }

        private void ChangeShift_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItem is AttendanceRecord selectedRow)
            {
                ShiftChangeWindow window = new ShiftChangeWindow(selectedRow.Shift, selectedRow.Date, code_box.Text);
                window.ShiftChanged += async (newShift) =>
                {
                    selectedRow.Shift = newShift;

                    var shift = await _context.Shifts.FirstOrDefaultAsync(s => s.Name == newShift);
                    if (shift != null)
                    {
                        var onDuty = shift.StartTime;
                        var offDuty = shift.EndTime;

                        await RecalculateAttendanceTimes(selectedRow, onDuty, offDuty);
                    }

                    await LoadAttendanceData();
                };
                window.ShowDialog();
            }
        }

        private async Task RecalculateAttendanceTimes(AttendanceRecord record, TimeSpan onDuty, TimeSpan offDuty)
        {
            if (record.AttendanceTime.HasValue && record.DepartureTime.HasValue)
            {
                var clockIn = record.AttendanceTime.Value.TimeOfDay;
                var clockOff = record.DepartureTime.Value.TimeOfDay;

                TimeSpan late = TimeSpan.Zero;
                if (clockIn > onDuty)
                {
                    late = clockIn - onDuty;
                }

                TimeSpan early = TimeSpan.Zero;
                if (clockOff < offDuty)
                {
                    early = offDuty - clockOff;
                }

                TimeSpan INearly = TimeSpan.Zero;
                if (clockIn < onDuty)
                {
                    INearly = onDuty - clockIn;
                }

                TimeSpan overtime = TimeSpan.Zero;
                if (clockOff > offDuty)
                {
                    overtime = clockOff - offDuty;
                }

                TimeSpan workTime = offDuty - onDuty;

                TimeSpan attendTime = TimeSpan.Zero;
                if (clockOff < clockIn)
                {
                    clockOff += TimeSpan.FromDays(1);
                }
                attendTime = clockOff - clockIn;

                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.User.BranchId.ToString() == branch_box.SelectedValue.ToString() && a.User.Code.ToString() == code_box.Text && a.AttendanceDate == record.Date);

                if (attendance != null)
                {
                    attendance.Late = late;
                    attendance.EarlyLeave = early;
                    attendance.EarlyEnter = INearly;
                    attendance.Overtime = overtime;
                    attendance.TotalWorkHours = attendTime;

                    await _context.SaveChangesAsync();
                }
            }
        }

        private void UpdateFromDateInDataGrid(DateTime newFromDate)
        {
            foreach (var item in _monthDatas)
            {
                if (item.Date.ToString("dd/MM/yyyy") == newFromDate.ToString("dd/MM/yyyy"))
                {
                    item.AttendanceTime = newFromDate;
                }
            }
            LoadAttendanceData();
        }

        private void UpdateToDateInDataGrid(DateTime newToDate)
        {
            foreach (var item in _monthDatas)
            {
                if (item.Date.ToString("dd/MM/yyyy") == newToDate.ToString("dd/MM/yyyy"))
                {
                    item.DepartureTime = newToDate;
                }
            }
            LoadAttendanceData();
        }

        private void ClearData()
        {
            _monthDatas.Clear();
            dates.Clear();
        }

        private async System.Threading.Tasks.Task LoadEmployeeData()
        {
            var user = await _context.Users
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Code.ToString() == code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString());

            if (user != null)
            {
                if (App.userBranches.Contains(user.BranchId))
                {
                    var employee = users.FirstOrDefault(u => u.Code == user.Id.ToString());
                    user_box.SelectedItem = employee;

                    shiftFrom = user.Shift?.StartTime ?? TimeSpan.MinValue;
                    shiftTo = user.Shift?.EndTime ?? TimeSpan.MinValue;
                    shift = user.Shift?.Name;

                    if (user.WeekHoliday != null)
                    {
                        weekHoli.Clear();
                        weekHoli.Add(user.WeekHoliday.Day1);
                        weekHoli.Add(user.WeekHoliday.Day2);
                        weekHoli.Add(user.WeekHoliday.Day3);
                        weekHoli.Add(user.WeekHoliday.Day4);
                        weekHoli.Add(user.WeekHoliday.Day5);
                        weekHoli.Add(user.WeekHoliday.Day6);
                        weekHoli.Add(user.WeekHoliday.Day7);
                    }

                    totalWHMain = user.WorkHours.ToString();
                    IsAccess = true;
                    exemptLate = user.ExemptLate ? 1 : 0;
                    exemptEarly = user.ExemptEarlyLeave ? 1 : 0;
                    exemptINEarly = user.ExemptEarlyEnter ? 1 : 0;
                    exemptOT = user.ExemptOvertime ? 1 : 0;
                    minSalary = user.MinSalary ?? 0;
                }
                else
                {
                    IsAccess = false;
                }
            }
        }

        private AttendanceRecord CreateDefaultRecord(DateTime date, bool holi)
        {
            return new AttendanceRecord
            {
                Day = arabicCulture.DateTimeFormat.DayNames[(int)date.DayOfWeek],
                Date = date,
                AttendanceTime = null,
                DepartureTime = null,
                DutyOn = shiftFrom,
                DutyOff = shiftTo,
                Shift = shift,
                isHoliday = holi,
                isAbsence = !holi,
                AttendBranch = "",
                DepartBranch = "",
                ExemptEarly = (exemptEarly == 1),
                ExemptINEarly = (exemptINEarly == 1),
                ExemptOT = (exemptOT == 1),
                ExemptLate = (exemptLate == 1),
                OverTime = null,
                Late = null,
                WorkHours = null,
                LateValue = "0.00",
                OTValue = "0.00"
            };
        }

        private (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year)
        {
            try
            {
                int startDay = Convert.ToInt16(Properties.Settings.Default.StartOfMonth);
                int endDay = (month == 2 && Convert.ToInt16(Properties.Settings.Default.EndOfMonth) > 29) ? 29 : Convert.ToInt16(Properties.Settings.Default.EndOfMonth);

                DateTime startDate = new DateTime(year, month, startDay);
                DateTime endDate = new DateTime(year, month, endDay);

                if (15 < startDay) startDate = startDate.AddMonths(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ”«»  Ê«—ÌŒ «·‘Â— «·„Œ’’: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }

        private async void LoadData()
        {
            try
            {
                month_box.ItemsSource = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
                year_box.ItemsSource = Enumerable.Range(2010, 21).ToList();

                var dbUsers = _context.Users.Select(u => new Employee
                {
                    Code = u.Id.ToString(),
                    Name = u.FullName,
                    Branch = u.BranchId.ToString()
                }).ToList();

                users.AddRange(dbUsers);
                user_box.ItemsSource = users;

                monthSettings = new MonthSettings
                {
                    StartDate = Properties.Settings.Default.StartOfMonth.ToString(),
                    EndDate = Properties.Settings.Default.EndOfMonth.ToString()
                };


                var branches = await _context.Branches.ToListAsync();
                branch_box.ItemsSource = branches;
                branch_box.DisplayMemberPath = "Name";
                branch_box.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (user_box.SelectedItem != null)
            {
                code_box.Text = user_box.SelectedValue.ToString();
            }
        }

        private void dataTable_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString(CultureInfo.InvariantCulture);
        }

        private async void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.DataContext as AttendanceRecord;
                if (item != null)
                {
                    await UpdateDatabaseAsync(item);
                }
            }
        }

        private void RefreshDataGrid()
        {
            _monthDatas = new ObservableCollection<AttendanceRecord>(attend);
            var collectionView = CollectionViewSource.GetDefaultView(_monthDatas);
            collectionView.Refresh();
            dataGrid.ItemsSource = _monthDatas;
        }

        public async System.Threading.Tasks.Task InsertAttendanceData()
        {
            try
            {
                var fingerPrints = await _context.FingerPrints
                    .Include(fp => fp.User)
                    .Include(fp => fp.User.Shift)
                    .GroupBy(fp => new { fp.UserId, fp.FingerPrintDate.Date })
                    .ToListAsync();

                foreach (var group in fingerPrints)
                {
                    var firstRecord = group.First();
                    var user = firstRecord.User;
                    var shift = user.Shift;

                    var clockIn = group.Where(fp => fp.Status == 1).Min(fp => (DateTime?)fp.FingerPrintDate);
                    var clockOff = group.Where(fp => fp.Status == 0).Max(fp => (DateTime?)fp.FingerPrintDate);

                    var attendance = new Attendance
                    {
                        UserId = user.Id,
                        AttendanceDate = group.Key.Date,
                        CheckInTime = clockIn,
                        CheckOutTime = clockOff,
                        CheckInBranchId = firstRecord.BranchId,
                        CheckOutBranchId = group.Where(fp => fp.Status == 0).Select(fp => fp.BranchId).FirstOrDefault(),
                        ExemptLate = user.ExemptLate,
                        ExemptEarlyLeave = user.ExemptEarlyLeave,
                        ExemptOvertime = user.ExemptOvertime,
                        ExemptEarlyEnter = false,
                        ShiftId = shift?.Id
                    };

                    if (clockIn != null && clockOff != null && shift != null)
                    {
                        DateTime onDuty = new DateTime(group.Key.Date.Year, group.Key.Date.Month, group.Key.Date.Day,
                                                 shift.StartTime.Hours, shift.StartTime.Minutes, shift.StartTime.Seconds);
                        DateTime offDuty = new DateTime(group.Key.Date.Year, group.Key.Date.Month, group.Key.Date.Day,
                                                  shift.EndTime.Hours, shift.StartTime.Minutes, shift.StartTime.Seconds);

                        if (!user.ExemptLate && clockIn > onDuty)
                        {
                            attendance.Late = clockIn - onDuty;
                        }

                        if (!user.ExemptEarlyLeave && clockOff < offDuty)
                        {
                            attendance.EarlyLeave = offDuty - clockOff;
                        }

                        if (!user.ExemptOvertime && clockOff > offDuty)
                        {
                            attendance.Overtime = clockOff - offDuty;
                        }

                        attendance.TotalWorkHours = clockOff - clockIn;
                    }

                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AttendanceDate == group.Key.Date);

                    if (existingAttendance == null)
                    {
                        _context.Attendances.Add(attendance);
                    }
                    else
                    {
                        existingAttendance.CheckInTime = clockIn;
                        existingAttendance.CheckOutTime = clockOff;
                        existingAttendance.Late = attendance.Late;
                        existingAttendance.EarlyLeave = attendance.EarlyLeave;
                        existingAttendance.Overtime = attendance.Overtime;
                        existingAttendance.TotalWorkHours = attendance.TotalWorkHours;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
            }
        }

        private void statistics_btn_Click(object sender, RoutedEventArgs e)
        {
            int tWHH = totalWHHours;
            int tWHM = totalWHMin;
            int hours = totalWHHours;
            int min = totalWHMin;
            int sec = totalWHSec;

            while (sec >= 60)
            {
                min++;
                sec -= 60;
            }
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string AlltotalWH = ForamttedTime(hours, min);

            hours = totalLateHours;
            min = totalLateMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH -= hours;
            tWHM -= min;
            string totalLate = ForamttedTime(hours, min);

            hours = totalEarlyHours;
            min = totalEarlyMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH -= hours;
            tWHM -= min;
            string totalEarly = ForamttedTime(hours, min);

            hours = totalINEarlyHours;
            min = totalINEarlyMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH += hours;
            tWHM += min;
            string totalINEarly = ForamttedTime(hours, min);

            hours = totalOvertimeHours;
            min = totalOvertimeMin;
            sec = totalOvertimeSec;
            while (sec >= 60)
            {
                min++;
                sec -= 60;
            }
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH += hours;
            tWHM += min;
            string totalOT = ForamttedTime(hours, min);

            hours = tWHH;
            min = tWHM;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string totalWH = ForamttedTime(hours, min);

            List<StatClass> stat = new List<StatClass>
            {
                new StatClass { name = "«·€Ì«»", value = totalAbsences },
                new StatClass { name = "«·—«Õ… «·«”»Ê⁄Ì…", value = totalWeeklyRest },
                new StatClass { name = "«· √ŒÌ—", value = totalLate },
                new StatClass { name = "«·«÷«›Ì", value = totalOT },
                new StatClass { name = "Œ—ÊÃ „»ﬂ—", value = totalEarly },
                new StatClass { name = "œŒÊ· „»ﬂ—", value = totalINEarly },
                new StatClass { name = "”«⁄«  «·Õ÷Ê—", value = AlltotalWH },
                new StatClass { name = "’«›Ì «·”«⁄« ", value = totalWH },
                new StatClass { name = "≈Ã„«·Ì ﬁÌ„… «· √ŒÌ—", value = TotalLateValue.ToString("N2") },
                new StatClass { name = "≈Ã„«·Ì ﬁÌ„… «·√÷«›Ì", value = TotalOTValue.ToString("N2") }
            };

            StatWindow window = new StatWindow(stat);
            window.Show();
        }

        public class AttendanceRecord : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private DateTime? _attendanceTime;
            public DateTime? AttendanceTime
            {
                get => _attendanceTime;
                set
                {
                    if (_attendanceTime != value)
                    {
                        _attendanceTime = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(OverTime));
                        OnPropertyChanged(nameof(Late));
                        OnPropertyChanged(nameof(WorkHours));
                    }
                }
            }

            private DateTime? _departureTime;
            public DateTime? DepartureTime
            {
                get => _departureTime;
                set
                {
                    if (_departureTime != value)
                    {
                        _departureTime = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(OverTime));
                        OnPropertyChanged(nameof(Late));
                        OnPropertyChanged(nameof(WorkHours));
                    }
                }
            }

            private TimeSpan? _dutyOn;
            public TimeSpan? DutyOn
            {
                get => _dutyOn;
                set
                {
                    if (_dutyOn != value)
                    {
                        _dutyOn = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(OverTime));
                        OnPropertyChanged(nameof(Late));
                        OnPropertyChanged(nameof(WorkHours));
                    }
                }
            }

            private TimeSpan? _dutyOff;
            public TimeSpan? DutyOff
            {
                get => _dutyOff;
                set
                {
                    if (_dutyOff != value)
                    {
                        _dutyOff = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(OverTime));
                        OnPropertyChanged(nameof(Late));
                        OnPropertyChanged(nameof(WorkHours));
                    }
                }
            }

            public string Day { get; set; }
            public string AttendBranch { get; set; }
            public string Text { get; set; }
            public string DepartBranch { get; set; }
            public DateTime Date { get; set; }
            public string Shift { get; set; }

            private bool _allowlate;
            public bool ExemptLate
            {
                get => _allowlate;
                set
                {
                    if (_allowlate != value)
                    {
                        _allowlate = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _allowearly;
            public bool ExemptEarly
            {
                get => _allowearly;
                set
                {
                    if (_allowearly != value)
                    {
                        _allowearly = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _allowinearly;
            public bool ExemptINEarly
            {
                get => _allowinearly;
                set
                {
                    if (_allowinearly != value)
                    {
                        _allowinearly = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _allowot;
            public bool ExemptOT
            {
                get => _allowot;
                set
                {
                    if (_allowot != value)
                    {
                        _allowot = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _isabsence;
            public bool isAbsence
            {
                get => _isabsence;
                set
                {
                    if (_isabsence != value)
                    {
                        _isabsence = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _isholiday;
            public bool isHoliday
            {
                get => _isholiday;
                set
                {
                    if (_isholiday != value)
                    {
                        _isholiday = value;
                        OnPropertyChanged();
                    }
                }
            }

            private TimeSpan? _WorkHours;
            public TimeSpan? WorkHours
            {
                get => _WorkHours;
                set
                {
                    if (_WorkHours != value)
                    {
                        _WorkHours = value;
                        OnPropertyChanged();
                    }
                }
            }

            private TimeSpan? _late;
            public TimeSpan? Late
            {
                get => _late;
                set
                {
                    if (_late != value)
                    {
                        _late = value;
                        OnPropertyChanged();
                    }
                }
            }

            private TimeSpan? _overTime;
            public TimeSpan? OverTime
            {
                get => _overTime;
                set
                {
                    if (_overTime != value)
                    {
                        _overTime = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string LateValue { get; set; }
            public string OTValue { get; set; }
        }

        public class MonthSettings
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

        class Employee
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Branch { get; set; }
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGridToExcel();
        }

        public void ExportDataGridToExcel()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "ExportedData"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Sheet1");

                        var headers = new[]
                        {
                    "«·ÌÊ„",
                    "«· «—ÌŒ",
                    "Õ÷Ê—",
                    "›—⁄ «·Õ÷Ê—",
                    "«‰’—«›",
                    "›—⁄ «·«‰’—«›",
                    "«·Ê—œÌ…",
                    "” «·⁄„·",
                    "ﬁÌ„… «· √ŒÌ—",
                    "ﬁÌ„… «·√÷«›Ì"
                };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Font.FontColor = XLColor.Black;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        }

                        for (int i = 0; i < _monthDatas.Count; i++)
                        {
                            var item = _monthDatas[i];

                            worksheet.Cell(i + 2, 1).Value = item.Day;
                            worksheet.Cell(i + 2, 2).Value = item.Date;
                            worksheet.Cell(i + 2, 3).Value = item.AttendanceTime;
                            worksheet.Cell(i + 2, 4).Value = item.AttendBranch;
                            worksheet.Cell(i + 2, 5).Value = item.DepartureTime;
                            worksheet.Cell(i + 2, 6).Value = item.DepartBranch;
                            worksheet.Cell(i + 2, 7).Value = item.Shift;
                            worksheet.Cell(i + 2, 8).Value = item.WorkHours;
                            worksheet.Cell(i + 2, 9).Value = item.LateValue;
                            worksheet.Cell(i + 2, 10).Value = item.OTValue;
                        }

                        workbook.SaveAs(filePath);
                        LocalizationManager.ShowMessage(" „ «” Œ—«Ã «·«ﬂ”Ì·!");
                    }
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage(ex.Message, "Œÿ√");
                }
            }
        }

        private void allLate_check_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptLate = true;
            }
            UpdateData();
        }

        private void allLate_check_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptLate = false;
            }
            UpdateData();
        }

        private void allEarly_check_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptEarly = true;
            }
            UpdateData();
        }

        private void allEarly_check_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptEarly = false;
            }
            UpdateData();
        }

        private void allINEarly_check_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptINEarly = true;
            }
            UpdateData();
        }

        private void allINEarly_check_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptINEarly = false;
            }
            UpdateData();
        }

        private void allOT_check_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptOT = true;
            }
            UpdateData();
        }

        private void allOT_check_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var record in _monthDatas)
            {
                record.ExemptOT = false;
            }
            UpdateData();
        }

        private void update_btn_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void print_btn_Click(object sender, RoutedEventArgs e)
        {
            string name = user_box.Text;
            string code = code_box.Text;
            string year = year_box.Text;
            string month = month_box.Text;

            int tWHH = totalWHHours;
            int tWHM = totalWHMin;
            int hours = totalWHHours;
            int min = totalWHMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string AlltotalWH = ForamttedTime(hours, min);

            hours = totalLateHours;
            min = totalLateMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH -= hours;
            tWHM -= min;
            string totalLate = ForamttedTime(hours, min);

            hours = totalEarlyHours;
            min = totalEarlyMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH -= hours;
            tWHM -= min;
            string totalEarly = ForamttedTime(hours, min);

            hours = totalINEarlyHours;
            min = totalINEarlyMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH += hours;
            tWHM += min;
            string totalINEarly = ForamttedTime(hours, min);

            hours = totalOvertimeHours;
            min = totalOvertimeMin;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            tWHH += hours;
            tWHM += min;
            string totalOT = ForamttedTime(hours, min);

            hours = tWHH;
            min = tWHM;
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string totalWH = ForamttedTime(hours, min);

            FlowDocument document = CreateDocument(_monthDatas, name, code, year, month, totalWH, totalAbsences, totalWeeklyRest, totalLate, totalOT, totalEarly, totalINEarly, AlltotalWH);
            MonthDataReport monthDataReport = new MonthDataReport(document);
            monthDataReport.Show();
        }

        // Helper Methods
        private string ForamttedTime(int hour, int min)
        {
            string hourS = hour.ToString().PadLeft(2, '0');
            string minS = min.ToString().PadLeft(2, '0');
            return hourS + ":" + minS;
        }

        private FlowDocument CreateDocument(
            ObservableCollection<AttendanceRecord> records,
            string name,
            string code,
            string year,
            string month,
            string totalHours,
            int totalAbsences,
            int totalWeeklyRest,
            string totalDelay,
            string totalOvertime,
            string totalEarly,
            string totalINEarly,
            string AlltotalWH)
        {
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(30);
            document.ColumnWidth = 500;
            document.FlowDirection = System.Windows.FlowDirection.RightToLeft;

            // Create header table
            System.Windows.Documents.Table headerTable = new System.Windows.Documents.Table();
            headerTable.Columns.Add(new System.Windows.Documents.TableColumn());
            headerTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new GridLength(15, GridUnitType.Star) });
            headerTable.CellSpacing = 0;
            headerTable.BorderThickness = new Thickness(0);

            var headerRowGroup1 = new TableRowGroup();
            System.Windows.Documents.TableRow headerRow1 = new System.Windows.Documents.TableRow();

            var cell6 = CreateCell($"«”„ «·„ÊŸ›: {name}\nﬂÊœ «·„ÊŸ›: {code}\n”«⁄«  ‘Â— : {month} - {year}", false, true);
            cell6.FontSize = 13;
            cell6.FontWeight = FontWeights.Bold;
            cell6.BorderThickness = new Thickness(0);
            cell6.Background = System.Windows.Media.Brushes.White;

            headerRow1.Cells.Add(cell6);

            // Add Image
            var image = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/Back.jfif", UriKind.RelativeOrAbsolute)),
                Width = 80,
                Height = 80,
                Stretch = System.Windows.Media.Stretch.Fill,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
            };
            var imageContainer = new BlockUIContainer(image);
            var imageCell = new System.Windows.Documents.TableCell(imageContainer);
            imageCell.BorderThickness = new Thickness(0);
            headerRow1.Cells.Add(imageCell);

            headerRowGroup1.Rows.Add(headerRow1);
            headerTable.RowGroups.Add(headerRowGroup1);
            document.Blocks.Add(headerTable);

            // Title
            var titleParagraph = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($" ﬁ—Ì— Õ÷Ê— Ê «‰’—«›  ›’Ì·Ì"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Red,
                TextAlignment = System.Windows.TextAlignment.Center
            };
            document.Blocks.Add(titleParagraph);

            // Create main table
            System.Windows.Documents.Table table = new System.Windows.Documents.Table();
            table.CellSpacing = 0;
            table.BorderThickness = new Thickness(1);
            table.BorderBrush = System.Windows.Media.Brushes.Black;

            // Define table columns
            for (int i = 0; i < 7; i++)
            {
                table.Columns.Add(new System.Windows.Documents.TableColumn());
            }

            // Create table header
            TableRowGroup headerRowGroup = new TableRowGroup();
            System.Windows.Documents.TableRow headerRow = new System.Windows.Documents.TableRow();
            headerRow.Background = System.Windows.Media.Brushes.LightGray;

            headerRow.Cells.Add(CreateCell("«·ÌÊ„", true, false));
            headerRow.Cells.Add(CreateCell("«· «—ÌŒ", true, false));
            headerRow.Cells.Add(CreateCell("Õ÷Ê—", true, false));
            headerRow.Cells.Add(CreateCell("«‰’—«›", true, false));
            headerRow.Cells.Add(CreateCell("” «·⁄„·", true, false));
            headerRow.Cells.Add(CreateCell("ﬁÌ„… «· √ŒÌ—", true, false));
            headerRow.Cells.Add(CreateCell("ﬁÌ„… «·√÷«›Ì", true, false));

            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var dayMapping = new Dictionary<string, int>
            {
                { "«·”» ", 0 }, { "«·√Õœ", 1 }, { "«·«À‰Ì‰", 2 }, { "«·À·«À«¡", 3 },
                { "«·√—»⁄«¡", 4 }, { "«·Œ„Ì”", 5 }, { "«·Ã„⁄…", 6 }
            };

            // Populate table with data
            TableRowGroup dataRowGroup = new TableRowGroup();
            foreach (var record in records)
            {
                string FormattedAttendanceTime = record.AttendanceTime.HasValue ?
                    ConvertToArabicNumerals(record.AttendanceTime.Value.ToString(@"hh\:mm", arabicCulture)) : string.Empty;
                string FormattedDepartureTime = record.DepartureTime.HasValue ?
                    ConvertToArabicNumerals(record.DepartureTime.Value.ToString(@"hh\:mm", arabicCulture)) : string.Empty;
                string FormattedWorkHours = record.WorkHours.HasValue ?
                    ConvertToArabicNumerals(record.WorkHours.Value.ToString(@"hh\:mm")) : string.Empty;
                string FormattedLateValue = ConvertToArabicNumerals(record.LateValue);
                string FormattedOTValue = ConvertToArabicNumerals(record.OTValue);
                string FormattedDate = ConvertToArabicNumerals(record.Date.ToString("yyyy/MM/dd"));

                System.Windows.Documents.TableRow dataRow = new System.Windows.Documents.TableRow();

                if (record.isHoliday)
                {
                    dataRow.Cells.Add(CreateColoredCell(record.Day, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedDate, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedAttendanceTime, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedDepartureTime, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedWorkHours, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedLateValue, Colors.Yellow, Colors.Black));
                    dataRow.Cells.Add(CreateColoredCell(FormattedOTValue, Colors.Yellow, Colors.Black));
                }
                else if (record.isAbsence)
                {
                    dataRow.Cells.Add(CreateColoredCell(record.Day, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedDate, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedAttendanceTime, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedDepartureTime, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedWorkHours, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedLateValue, Colors.Red, Colors.White));
                    dataRow.Cells.Add(CreateColoredCell(FormattedOTValue, Colors.Red, Colors.White));
                }
                else if (!string.IsNullOrEmpty(FormattedAttendanceTime) && !string.IsNullOrEmpty(FormattedDepartureTime))
                {
                    dataRow.Cells.Add(CreateCell(record.Day, false, true));
                    dataRow.Cells.Add(CreateCell(FormattedDate, false, true));
                    dataRow.Cells.Add(CreateCell(FormattedAttendanceTime, false, false));
                    dataRow.Cells.Add(CreateCell(FormattedDepartureTime, false, false));
                    dataRow.Cells.Add(CreateCell(FormattedWorkHours, false, false));
                    dataRow.Cells.Add(CreateCell(FormattedLateValue, false, false));
                    dataRow.Cells.Add(CreateCell(FormattedOTValue, false, false));
                }
                else
                {
                    int dayIndex = dayMapping[record.Day];
                    bool isWeeklyHoliday = (dayIndex < weekHoli.Count && weekHoli[dayIndex]);

                    if (isWeeklyHoliday)
                    {
                        dataRow.Cells.Add(CreateColoredCell(record.Day, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedDate, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedAttendanceTime, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedDepartureTime, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedWorkHours, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedLateValue, Colors.LightBlue, Colors.Black));
                        dataRow.Cells.Add(CreateColoredCell(FormattedOTValue, Colors.LightBlue, Colors.Black));
                    }
                    else
                    {
                        dataRow.Cells.Add(CreateColoredCell(record.Day, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedDate, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedAttendanceTime, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedDepartureTime, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedWorkHours, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedLateValue, Colors.Red, Colors.White));
                        dataRow.Cells.Add(CreateColoredCell(FormattedOTValue, Colors.Red, Colors.White));
                    }
                }

                dataRowGroup.Rows.Add(dataRow);
            }
            table.RowGroups.Add(dataRowGroup);
            document.Blocks.Add(table);

            // Create summary table
            System.Windows.Documents.Table summary_table = new System.Windows.Documents.Table();
            summary_table.CellSpacing = 0;
            summary_table.BorderBrush = System.Windows.Media.Brushes.Black;

            for (int i = 0; i < 5; i++)
            {
                summary_table.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new GridLength(1, GridUnitType.Auto) });
            }

            // Summary header
            TableRowGroup summary_headerRowGroup = new TableRowGroup();
            System.Windows.Documents.TableRow summary_headerRow = new System.Windows.Documents.TableRow();
            summary_headerRow.Background = System.Windows.Media.Brushes.LightGray;

            summary_headerRow.Cells.Add(CreateCell("»Ì«‰", true, false));
            summary_headerRow.Cells.Add(CreateCell("⁄ ”«⁄« ", true, false));
            summary_headerRow.Cells.Add(CreateCell("»Ì«‰", true, false));
            summary_headerRow.Cells.Add(CreateCell("⁄ «Ì«„", true, false));

            var empty = CreateCell("", false, false);
            empty.Background = System.Windows.Media.Brushes.White;
            empty.BorderThickness = new Thickness(0);
            summary_headerRow.Cells.Add(empty);

            summary_headerRowGroup.Rows.Add(summary_headerRow);
            summary_table.RowGroups.Add(summary_headerRowGroup);

            // Summary data
            TableRowGroup summary_dataRowGroup = new TableRowGroup();

            // Row 1
            System.Windows.Documents.TableRow summary_dataRow = new System.Windows.Documents.TableRow();
            summary_dataRow.Cells.Add(CreateCell("«· √ŒÌ—", false, true));
            summary_dataRow.Cells.Add(CreateCell(ConvertToArabicNumerals(totalDelay.ToString()), false, true));
            summary_dataRow.Cells.Add(CreateCell("€Ì«»", false, true));
            summary_dataRow.Cells.Add(CreateCell(ConvertToArabicNumerals(totalAbsences.ToString()), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow);

            // Row 2
            System.Windows.Documents.TableRow summary_dataRow1 = new System.Windows.Documents.TableRow();
            summary_dataRow1.Cells.Add(CreateCell("«÷«›Ì", false, true));
            summary_dataRow1.Cells.Add(CreateCell(ConvertToArabicNumerals(totalOvertime.ToString()), false, true));
            summary_dataRow1.Cells.Add(CreateCell("«·—«Õ… «·«”»Ê⁄Ì…", false, true));
            summary_dataRow1.Cells.Add(CreateCell(ConvertToArabicNumerals(totalWeeklyRest.ToString()), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow1);

            // Row 3
            System.Windows.Documents.TableRow summary_dataRow2 = new System.Windows.Documents.TableRow();
            summary_dataRow2.Cells.Add(CreateCell("œ „»ﬂ—", false, true));
            summary_dataRow2.Cells.Add(CreateCell(ConvertToArabicNumerals(totalINEarly.ToString()), false, true));
            summary_dataRow2.Cells.Add(CreateCell("«·«Ã«“« ", false, true));
            summary_dataRow2.Cells.Add(CreateCell(ConvertToArabicNumerals("0"), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow2);

            // Row 4
            System.Windows.Documents.TableRow summary_dataRow3 = new System.Windows.Documents.TableRow();
            summary_dataRow3.Cells.Add(CreateCell("”«⁄«  «·⁄„·", false, true));
            summary_dataRow3.Cells.Add(CreateCell(ConvertToArabicNumerals(AlltotalWH.ToString()), false, true));
            summary_dataRow3.Cells.Add(CreateCell("Œ „»ﬂ—", false, true));
            summary_dataRow3.Cells.Add(CreateCell(ConvertToArabicNumerals(totalEarly.ToString()), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow3);

            // Row 5
            System.Windows.Documents.TableRow summary_dataRow4 = new System.Windows.Documents.TableRow();
            summary_dataRow4.Cells.Add(CreateCell("≈Ã„«·Ì ﬁÌ„… «· √ŒÌ—", false, true));
            summary_dataRow4.Cells.Add(CreateCell(ConvertToArabicNumerals(TotalLateValue.ToString("N2")), false, true));
            summary_dataRow4.Cells.Add(CreateCell("≈Ã„«·Ì ﬁÌ„… «·√÷«›Ì", false, true));
            summary_dataRow4.Cells.Add(CreateCell(ConvertToArabicNumerals(TotalOTValue.ToString("N2")), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow4);

            // Row 6
            System.Windows.Documents.TableRow summary_dataRow5 = new System.Windows.Documents.TableRow();
            summary_dataRow5.Cells.Add(CreateCell("⁄ ”«⁄«  ›⁄·Ì…", false, true));
            summary_dataRow5.Cells.Add(CreateCell(ConvertToArabicNumerals(totalHours.ToString()), false, true));
            summary_dataRowGroup.Rows.Add(summary_dataRow5);

            summary_table.RowGroups.Add(summary_dataRowGroup);
            document.Blocks.Add(summary_table);

            return document;
        }

        private System.Windows.Documents.TableCell CreateColoredCell(string content, Color backgroundColor, Color foregroundColor)
        {
            var cell = CreateCell(content, false, true);
            cell.Background = new SolidColorBrush(backgroundColor);
            cell.Foreground = new SolidColorBrush(foregroundColor);
            return cell;
        }

        private string ConvertToArabicNumerals(string input)
        {
            string arabicNumerals = "0123456789";
            string westernNumerals = "0123456789";
            return new string(input.Select(c =>
                westernNumerals.Contains(c) ? arabicNumerals[westernNumerals.IndexOf(c)] : c
            ).ToArray());
        }

        private System.Windows.Documents.TableCell CreateCell(string content, bool isHeader, bool isArabic)
        {
            var cell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(content)));
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.BorderThickness = new Thickness(1);
            cell.Padding = new Thickness(0.5);
            if (isHeader)
            {
                cell.Background = System.Windows.Media.Brushes.Black;
                cell.Foreground = System.Windows.Media.Brushes.White;
            }
            if (isArabic)
            {
                cell.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                cell.TextAlignment = System.Windows.TextAlignment.Center;
            }
            return cell;
        }
    }
}
