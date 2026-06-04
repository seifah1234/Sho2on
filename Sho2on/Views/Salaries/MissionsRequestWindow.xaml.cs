using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.ViewModels;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using Colors = System.Windows.Media.Colors;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Salaries
{
    /// <summary>
    /// Interaction logic for MissionsRequestWindow.xaml
    /// </summary>
    public partial class MissionsRequestWindow : Window
    {
        private readonly AppDbContext _context;
        private List<MissionViewModel> _missions = new List<MissionViewModel>();
        private List<MissionViewModel> _ownMissions = new List<MissionViewModel>();
        private List<User> users = new List<User>();

        public MissionsRequestWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                await LoadMissions();

                if ((!App.CurrentUser.Department.IsHR.HasValue || !App.CurrentUser.Department.IsHR.Value) && 
                    (!App.CurrentUser.JobTitle.IsManager.HasValue || !App.CurrentUser.JobTitle.IsManager.Value))
                {
                    employeesTab.Visibility = Visibility.Collapsed;
                }
                var _employees = _context.Users.Include(e => e.Shift).Include(e => e.Manager).ToList();

                users.AddRange(_employees);
                user_box.ItemsSource = users;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadMissions()
        {
            try
            {
                var query = await _context.Procedures
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Where(p => p.ApprovedByUserId == App.CurrentUser.Id ||
                               (p.ApprovedByUserId == null && App.CurrentUser.JobTitle.IsManager == true) ||
                               (p.Status == ProcedureStatus.UnderReview && App.CurrentUser.Department.IsHR == true))
                    .ToListAsync();

                //  ÿ»Ìﬁ «·›·« —
                if (int.TryParse(txtEmployeeId.Text, out int employeeId) && employeeId > 0)
                {
                    query = query.Where(p => p.UserId == employeeId).ToList();
                }


                if (dpFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.StartDate >= dpFromDate.SelectedDate.Value).ToList();
                }

                if (dpToDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.EndDate <= dpToDate.SelectedDate.Value).ToList();
                }

                if (cmbStatus.SelectedItem is ComboBoxItem selectedStatus &&
                    selectedStatus.Tag is string status && !string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status).ToList();
                }


                _missions = query.Select(p => new MissionViewModel
                {
                    Id = p.Id,
                    EmployeeId = p.UserId,
                    EmployeeName = p.User?.FullName ?? "€Ì— „⁄—Ê›",
                    StartDateTime = p.StartDate,
                    EndDateTime = p.EndDate,
                    Duration = Math.Round((p.EndDate - p.StartDate).Value.TotalHours, 2),
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? "€Ì— „⁄—Ê›",
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? "€Ì— „⁄—Ê›",
                    ApprovedByName = p.ApprovedBy?.FullName ?? "·„   „ «·„Ê«›ﬁ… »⁄œ",
                    ApprovedDate = p.ApprovedDate
                }).ToList();

                dgMissions.ItemsSource = _missions;
                
                query = await _context.Procedures
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Where(p => p.UserId == App.CurrentUser.Id)
                    .ToListAsync();



                if (dpOwnFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.StartDate >= dpOwnFromDate.SelectedDate.Value).ToList();
                }

                if (dpOwnToDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.EndDate <= dpOwnToDate.SelectedDate.Value).ToList();
                }

                if (cmbOwnStatus.SelectedItem is ComboBoxItem selectedOwnStatus &&
                    selectedOwnStatus.Tag is string ownStatus && !string.IsNullOrEmpty(ownStatus))
                {
                    query = query.Where(p => p.Status == ownStatus).ToList();
                }


                _ownMissions = query.Select(p => new MissionViewModel
                {
                    Id = p.Id,
                    EmployeeId = p.UserId,
                    EmployeeName = p.User?.FullName ?? "€Ì— „⁄—Ê›",
                    StartDateTime = p.StartDate,
                    EndDateTime = p.EndDate,
                    Duration = Math.Round((p.EndDate - p.StartDate).Value.TotalHours, 2),
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? "€Ì— „⁄—Ê›",
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? "€Ì— „⁄—Ê›",
                    ApprovedByName = p.ApprovedBy?.FullName ?? "·„   „ «·„Ê«›ﬁ… »⁄œ",
                    ApprovedDate = p.ApprovedDate
                }).ToList();

                dgOwnMissions.ItemsSource = _ownMissions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· ÿ·»«  «·„√„Ê—Ì« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                txtEmployeeId.Text = user_box.SelectedValue.ToString();
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

        private string GetStatusText(string status)
        {
            return status switch
            {
                "UnderReview" => " Õ  «·„—«Ã⁄…",
                "Pending" => "ﬁÌœ «·«‰ Ÿ«—",
                "Approved" => "„Ê«›ﬁ ⁄·ÌÂ",
                "Rejected" => "„—›Ê÷",
                "Draft" => "„”Êœ…",
                _ => status
            };
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await LoadMissions();
        }


        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtEmployeeId.Text = string.Empty;
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            cmbStatus.SelectedIndex = 0;
        }

        private async void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int missionId)
            {
                await ProcessMissionApproval(missionId, true);
            }
        }

        private async void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int missionId)
            {
                await ProcessMissionApproval(missionId, false);
            }
        }

        private async System.Threading.Tasks.Task ProcessMissionApproval(int missionId, bool isApprove)
        {
            try
            {
                var mission = await _context.Procedures
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == missionId);

                if (mission == null)
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·„√„Ê—Ì…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isApprove)
                {
                    if (App.CurrentUser.Department != null && App.CurrentUser.Department.IsHR.HasValue && App.CurrentUser.Department.IsHR.Value)
                    {
                        mission.Status = PermissionStatus.Approved;
                    }
                    else
                    {
                        mission.Status = PermissionStatus.UnderReview;
                    }
                    mission.ApprovedDate = DateTime.Now;
                    mission.ApprovedByUserId = App.CurrentUser?.Id;

                    //  ÕœÌÀ ”Ã· «·Õ÷Ê—
                    await UpdateAttendanceForMission(mission);

                    LocalizationManager.ShowMessage(" „ «·„Ê«›ﬁ… ⁄·Ï ÿ·» «·„√„Ê—Ì…", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                        mission.Status = PermissionStatus.Rejected;
                        mission.ApprovedDate = DateTime.Now;
                        mission.ApprovedByUserId = App.CurrentUser?.Id;

                        LocalizationManager.ShowMessage(" „ —›÷ ÿ·» «·„√„Ê—Ì…", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                   
                }

                await _context.SaveChangesAsync();
                await LoadMissions();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì „⁄«·Ã… «·ÿ·»: {ex.InnerException}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateAttendanceForMission(Procedure mission)
        {
            try
            {
                try
                {
                    var user = await _context.Users
                        .Include(u => u.Shift)
                     .FirstOrDefaultAsync(u => u.Id == mission.UserId);

                    // Calculate time differences
                    DateTime? clockIn = mission.StartDate;
                    DateTime? clockOff = mission.EndDate;

                    TimeSpan late = TimeSpan.Zero;
                    if (user.ExemptLate && clockIn != null && clockIn.Value.TimeOfDay > user.Shift.StartTime)
                    {
                        late = TimeSpan.FromMinutes((clockIn.Value.TimeOfDay - user.Shift.StartTime).TotalMinutes);
                    }

                    TimeSpan early = TimeSpan.Zero;
                    if (user.ExemptEarlyLeave && clockOff != null && clockOff.Value.TimeOfDay < user.Shift.EndTime)
                    {
                        early = TimeSpan.FromMinutes((user.Shift.EndTime - clockOff.Value.TimeOfDay).TotalMinutes);
                    }

                    TimeSpan INearly = TimeSpan.Zero;
                    if (user.ExemptEarlyEnter && clockIn != null && clockIn.Value.TimeOfDay < user.Shift.StartTime)
                    {
                        INearly = TimeSpan.FromMinutes((user.Shift.StartTime - clockIn.Value.TimeOfDay).TotalMinutes);
                    }

                    TimeSpan overtime = TimeSpan.Zero;
                    if (user.ExemptOvertime && clockOff != null && clockOff.Value.TimeOfDay > user.Shift.EndTime)
                    {
                        overtime = TimeSpan.FromMinutes((clockOff.Value.TimeOfDay - user.Shift.EndTime).TotalMinutes);
                    }

                    TimeSpan workTime = TimeSpan.FromMinutes((user.Shift.EndTime - user.Shift.StartTime).TotalMinutes);

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
                    var dayDate = mission.EndDate.Value.Date;
                    var attendance = await _context.Attendances.Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AttendanceDate == dayDate);

                    if (attendance == null)
                    {
                        attendance = new Attendance
                        {
                            UserId = user.Id,
                            AttendanceDate = dayDate,
                            ShiftId = user.ShiftId,
                            CheckInBranchId = user.BranchId,
                            CheckOutBranchId = user.BranchId
                        };
                        await _context.Attendances.AddAsync(attendance);
                    }

                    attendance.CheckInTime = clockIn;
                    attendance.CheckOutTime = clockOff;
                    attendance.Late = late;
                    attendance.EarlyLeave = early;
                    attendance.Overtime = overtime;
                    attendance.TotalWorkHours = attendTime;
                    attendance.EarlyEnter = INearly;
                    attendance.ExemptLate = user.ExemptLate;
                    attendance.ExemptEarlyEnter = user.ExemptEarlyEnter;
                    attendance.ExemptEarlyLeave = user.ExemptEarlyLeave;
                    attendance.ExemptOvertime = user.ExemptOvertime;


                    await _context.SaveChangesAsync();

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

        private async void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int missionId)
            {
                await ShowMissionDetails(missionId);
            }
        }

        private async System.Threading.Tasks.Task ShowMissionDetails(int missionId)
        {
            try
            {
                var mission = await _context.Procedures
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Branch)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Include(p => p.Branch)
                    .FirstOrDefaultAsync(p => p.Id == missionId);

                if (mission != null)
                {
                    // Ì„ﬂ‰ﬂ ≈‰‘«¡ ‰«›–… ⁄—÷ «· ›«’Ì· Â‰«
                    var detailsMessage = $" ›«’Ì· «·≈–‰:\n\n" +
                                         $"«·„ÊŸ›: {mission.User?.FullName}\n" +
                                         $"„‰: {mission.StartDate:yyyy/MM/dd HH:mm}\n" +
                                         $"≈·Ï: {mission.EndDate:yyyy/MM/dd HH:mm}\n" +
                                         $"«·„œ…: {Math.Round((mission.EndDate - mission.StartDate).Value.TotalHours, 2)} ”«⁄…\n" +
                                         $"«·Õ«·…: {GetStatusText(mission.Status)}\n" +
                                         $" «—ÌŒ «·ÿ·»: {mission.CreatedAt:yyyy/MM/dd}";

                    LocalizationManager.ShowMessage(detailsMessage, " ›«’Ì· «·„√„Ê—Ì…", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ⁄—÷ «· ›«’Ì·: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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


        private async void btnOwnSearch_Click(object sender, RoutedEventArgs e)
        {
            await LoadMissions();
        }

        private void btnOwnClearFilters_Click(object sender, RoutedEventArgs e)
        {

            dpOwnFromDate.SelectedDate = null;
            dpOwnToDate.SelectedDate = null;
            cmbOwnStatus.SelectedIndex = 0;
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Draft" => new SolidColorBrush(Colors.Gray), // „”Êœ…
                    "Under Review" => new SolidColorBrush(Colors.Blue), // ﬁÌœ «·«‰ Ÿ«—
                    "Pending" => new SolidColorBrush(Colors.Orange), // ﬁÌœ «·«‰ Ÿ«—
                    "Approved" => new SolidColorBrush(Colors.Green), // „Ê«›ﬁ ⁄·ÌÂ
                    "Rejected" => new SolidColorBrush(Colors.Red), // „—›Ê÷
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
            if (value is string status)
            {
                //  Õﬁﬁ „‰ ’·«ÕÌ«  «·„” Œœ„
                bool isManager = App.CurrentUser?.JobTitle?.IsManager ?? false;
                bool isHR = App.CurrentUser?.Department?.IsHR ?? false;

                // ≈ŸÂ«— «·“— ··„œÌ— ≈–« ﬂ«‰  «·Õ«·… Pending
                if (status == "Pending" && isManager)
                    return Visibility.Visible;

                // ≈ŸÂ«— «·“— ··„Ê«—œ «·»‘—Ì… ≈–« ﬂ«‰  «·Õ«·… Under Review
                if (status == "Under Review" && isHR)
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

