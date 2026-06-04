using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.ViewModels;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static HR_Application.EmployeeData;
using Button = System.Windows.Controls.Button;
using Colors = System.Windows.Media.Colors;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees
{
    public partial class PermissionManagementWindow : Window
    {
        private readonly AppDbContext _context;
        private List<PermissionViewModel> _permissions = new List<PermissionViewModel>();
        private List<PermissionViewModel> _ownPermissions = new List<PermissionViewModel>();
        private List<User> users = new List<User>();

        public PermissionManagementWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                await LoadPermissions();

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


        private async System.Threading.Tasks.Task LoadPermissions()
        {
            try
            {
                var query = await _context.EmployeePermissions
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Where(p => p.ApprovedByUserId == App.CurrentUser.Id ||
                               (p.ApprovedByUserId == null && App.CurrentUser.JobTitle.IsManager == true) ||
                               (p.Status == PermissionStatus.UnderReview && App.CurrentUser.Department.IsHR == true))
                    .ToListAsync();

                //  ÿ»Ìﬁ «·›·« —
                if (int.TryParse(txtEmployeeId.Text, out int employeeId) && employeeId > 0)
                {
                    query = query.Where(p => p.UserId == employeeId).ToList();
                }


                if (dpFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime >= dpFromDate.SelectedDate.Value).ToList();
                }

                if (dpToDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.EndDateTime <= dpToDate.SelectedDate.Value).ToList();
                }

                if (cmbStatus.SelectedItem is ComboBoxItem selectedStatus &&
                    selectedStatus.Tag is string status && !string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status).ToList();
                }


                _permissions = query.Select(p => new PermissionViewModel
                {
                    Id = p.Id,
                    EmployeeId = p.UserId,
                    EmployeeName = p.User?.FullName ?? "€Ì— „⁄—Ê›",
                    PermissionType = p.PermissionType,
                    PermissionTypeName = GetPermissionTypeName(p.PermissionType),
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason,
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? "€Ì— „⁄—Ê›",
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? "€Ì— „⁄—Ê›",
                    ApprovedByName = p.ApprovedBy?.FullName ?? "·„   „ «·„Ê«›ﬁ… »⁄œ",
                    ApprovedDate = p.ApprovedDate,
                    RejectionReason = p.RejectionReason
                }).ToList();

                dgPermissions.ItemsSource = _permissions;
                query = await _context.EmployeePermissions
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Where(p => p.UserId == App.CurrentUser.Id)
                    .ToListAsync();


                if (dpOwnFromDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime >= dpOwnFromDate.SelectedDate.Value).ToList();
                }

                if (dpOwnToDate.SelectedDate.HasValue)
                {
                    query = query.Where(p => p.EndDateTime <= dpOwnToDate.SelectedDate.Value).ToList();
                }

                if (cmbOwnStatus.SelectedItem is ComboBoxItem selectedOwnStatus &&
                    selectedOwnStatus.Tag is string OwnStatus && !string.IsNullOrEmpty(OwnStatus))
                {
                    query = query.Where(p => p.Status == OwnStatus).ToList();
                }


                _ownPermissions = query.Select(p => new PermissionViewModel
                {
                    Id = p.Id,
                    EmployeeId = p.UserId,
                    EmployeeName = p.User?.FullName ?? "€Ì— „⁄—Ê›",
                    PermissionType = p.PermissionType,
                    PermissionTypeName = GetPermissionTypeName(p.PermissionType),
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason,
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? "€Ì— „⁄—Ê›",
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? "€Ì— „⁄—Ê›",
                    ApprovedByName = p.ApprovedBy?.FullName ?? "·„   „ «·„Ê«›ﬁ… »⁄œ",
                    ApprovedDate = p.ApprovedDate,
                    RejectionReason = p.RejectionReason
                }).ToList();

                dgOwnPermissions.ItemsSource = _ownPermissions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· ÿ·»«  «·≈–‰: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetPermissionTypeName(string permissionType)
        {
            return permissionType switch
            {
                "EarlyLeave" => "Œ—ÊÃ „»ﬂ—",
                "LateEntry" => "œŒÊ· „ √Œ—",
                "PersonalLeave" => "≈–‰ ‘Œ’Ì",
                "Emergency" => "ÿ«—∆",
                "Official" => "—”„Ì",
                "Other" => "√Œ—Ï",
                _ => permissionType
            };
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
            await LoadPermissions();
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
            if (sender is Button button && button.Tag is int permissionId)
            {
                await ProcessPermissionApproval(permissionId, true);
            }
        }

        private async void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int permissionId)
            {
                await ProcessPermissionApproval(permissionId, false);
            }
        }

        private async System.Threading.Tasks.Task ProcessPermissionApproval(int permissionId, bool isApprove)
        {
            try
            {
                var permission = await _context.EmployeePermissions
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == permissionId);

                if (permission == null)
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ÿ·» «·≈–‰", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isApprove)
                {
                    if (App.CurrentUser.Department != null && App.CurrentUser.Department.IsHR.HasValue && App.CurrentUser.Department.IsHR.Value)
                    {
                        permission.Status = PermissionStatus.Approved;
                        permission.ApprovedDate = DateTime.Now;

                        //  ÕœÌÀ ”Ã· «·Õ÷Ê—
                        await UpdateAttendanceForPermission(permission);
                    }
                    else
                    {
                        permission.Status = PermissionStatus.UnderReview;
                        permission.ApprovedByUserId = App.CurrentUser?.Id;
                    }
                    

                    LocalizationManager.ShowMessage(" „ «·„Ê«›ﬁ… ⁄·Ï ÿ·» «·≈–‰", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var reasonWindow = new RejectionReasonWindow();
                    reasonWindow.Owner = this;

                    if (reasonWindow.ShowDialog() == true)
                    {
                        permission.Status = PermissionStatus.Rejected;
                        permission.RejectionReason = reasonWindow.RejectionReason;
                        permission.ApprovedDate = DateTime.Now;
                        permission.ApprovedByUserId = App.CurrentUser?.Id;

                        LocalizationManager.ShowMessage(" „ —›÷ ÿ·» «·≈–‰", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        return;
                    }
                }

                await _context.SaveChangesAsync();
                await LoadPermissions();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì „⁄«·Ã… «·ÿ·»: {ex.InnerException}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateAttendanceForPermission(EmployeePermission permission)
        {
            // «·»ÕÀ ⁄‰ ”Ã· «·Õ÷Ê— ·Â–« «·ÌÊ„
            var attendanceDate = permission.StartDateTime.Date;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == permission.UserId &&
                                         a.AttendanceDate == attendanceDate);

            if (attendance != null)
            {
                //  ÕœÌÀ ”Ã· «·Õ÷Ê— »‰«¡ ⁄·Ï ‰Ê⁄ «·≈–‰
                switch (permission.PermissionType)
                {
                    case "EarlyLeave":
                        attendance.EarlyLeave = TimeSpan.FromHours(permission.Duration);
                        break;
                    case "LateEntry":
                        attendance.Late = TimeSpan.FromHours(permission.Duration);
                        break;
                }

            }
        }

        private async void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int permissionId)
            {
                await ShowPermissionDetails(permissionId);
            }
        }

        private async System.Threading.Tasks.Task ShowPermissionDetails(int permissionId)
        {
            try
            {
                var permission = await _context.EmployeePermissions
                    .Include(p => p.User)
                        .ThenInclude(u => u.Department)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Branch)
                    .Include(p => p.User)
                        .ThenInclude(u => u.JobTitle)
                    .Include(p => p.ApprovedBy)
                    .Include(p => p.Branch)
                    .FirstOrDefaultAsync(p => p.Id == permissionId);

                if (permission != null)
                {
                    // Ì„ﬂ‰ﬂ ≈‰‘«¡ ‰«›–… ⁄—÷ «· ›«’Ì· Â‰«
                    var detailsMessage = $" ›«’Ì· «·≈–‰:\n\n" +
                                         $"«·„ÊŸ›: {permission.User?.FullName}\n" +
                                         $"‰Ê⁄ «·≈–‰: {GetPermissionTypeName(permission.PermissionType)}\n" +
                                         $"„‰: {permission.StartDateTime:yyyy/MM/dd HH:mm}\n" +
                                         $"≈·Ï: {permission.EndDateTime:yyyy/MM/dd HH:mm}\n" +
                                         $"«·„œ…: {permission.Duration} ”«⁄…\n" +
                                         $"«·”»»: {permission.Reason}\n" +
                                         $"«·Õ«·…: {GetStatusText(permission.Status)}\n" +
                                         $" «—ÌŒ «·ÿ·»: {permission.CreatedAt:yyyy/MM/dd}";

                    LocalizationManager.ShowMessage(detailsMessage, " ›«’Ì· «·≈–‰", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void btnOwnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            await LoadPermissions();

        }

        private void btnOwnSearch_Click(object sender, RoutedEventArgs e)
        {
            dpOwnFromDate.SelectedDate = null;
            dpOwnToDate.SelectedDate = null;
            cmbOwnStatus.SelectedIndex = 0;
        }
    }

    // Converters
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
