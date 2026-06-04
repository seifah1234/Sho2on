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
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                // تطبيق الفلاتر
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
                    EmployeeName = p.User?.FullName ?? LocalizationManager.Translate("غير معروف"),
                    PermissionType = p.PermissionType,
                    PermissionTypeName = GetPermissionTypeName(p.PermissionType),
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason,
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? LocalizationManager.Translate("غير معروف"),
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? LocalizationManager.Translate("غير معروف"),
                    ApprovedByName = p.ApprovedBy?.FullName ?? LocalizationManager.Translate("لم تتم الموافقة بعد"),
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
                    EmployeeName = p.User?.FullName ?? LocalizationManager.Translate("غير معروف"),
                    PermissionType = p.PermissionType,
                    PermissionTypeName = GetPermissionTypeName(p.PermissionType),
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason,
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? LocalizationManager.Translate("غير معروف"),
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? LocalizationManager.Translate("غير معروف"),
                    ApprovedByName = p.ApprovedBy?.FullName ?? LocalizationManager.Translate("لم تتم الموافقة بعد"),
                    ApprovedDate = p.ApprovedDate,
                    RejectionReason = p.RejectionReason
                }).ToList();

                dgOwnPermissions.ItemsSource = _ownPermissions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل طلبات الإذن: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetPermissionTypeName(string permissionType)
        {
            return permissionType switch
            {
                "EarlyLeave" => LocalizationManager.Translate("خروج مبكر"),
                "LateEntry" => LocalizationManager.Translate("دخول متأخر"),
                "PersonalLeave" => LocalizationManager.Translate("إذن شخصي"),
                "Emergency" => LocalizationManager.Translate("طارئ"),
                "Official" => LocalizationManager.Translate("رسمي"),
                "Other" => LocalizationManager.Translate("أخرى"),
                _ => permissionType
            };
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "UnderReview" => LocalizationManager.Translate("تحت المراجعة"),
                "Pending" => LocalizationManager.Translate("قيد الانتظار"),
                "Approved" => LocalizationManager.Translate("موافق عليه"),
                "Rejected" => LocalizationManager.Translate("مرفوض"),
                "Draft" => LocalizationManager.Translate("مسودة"),
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
                    LocalizationManager.ShowMessage("لم يتم العثور على طلب الإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isApprove)
                {
                    if (App.CurrentUser.Department != null && App.CurrentUser.Department.IsHR.HasValue && App.CurrentUser.Department.IsHR.Value)
                    {
                        permission.Status = PermissionStatus.Approved;
                        permission.ApprovedDate = DateTime.Now;

                        // تحديث سجل الحضور
                        await UpdateAttendanceForPermission(permission);
                    }
                    else
                    {
                        permission.Status = PermissionStatus.UnderReview;
                        permission.ApprovedByUserId = App.CurrentUser?.Id;
                    }
                    

                    LocalizationManager.ShowMessage("تم الموافقة على طلب الإذن", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
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

                        LocalizationManager.ShowMessage("تم رفض طلب الإذن", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                LocalizationManager.ShowMessage($"خطأ في معالجة الطلب: {ex.InnerException}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateAttendanceForPermission(EmployeePermission permission)
        {
            // البحث عن سجل الحضور لهذا اليوم
            var attendanceDate = permission.StartDateTime.Date;
            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == permission.UserId &&
                                         a.AttendanceDate == attendanceDate);

            if (attendance != null)
            {
                // تحديث سجل الحضور بناءً على نوع الإذن
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
                    // يمكنك إنشاء نافذة عرض التفاصيل هنا
                    var detailsMessage = $"تفاصيل الإذن:\n\n" +
                                         $"الموظف: {permission.User?.FullName}\n" +
                                         $"نوع الإذن: {GetPermissionTypeName(permission.PermissionType)}\n" +
                                         $"من: {permission.StartDateTime:yyyy/MM/dd HH:mm}\n" +
                                         $"إلى: {permission.EndDateTime:yyyy/MM/dd HH:mm}\n" +
                                         $"المدة: {permission.Duration} ساعة\n" +
                                         $"السبب: {permission.Reason}\n" +
                                         $"الحالة: {GetStatusText(permission.Status)}\n" +
                                         $"تاريخ الطلب: {permission.CreatedAt:yyyy/MM/dd}";

                    LocalizationManager.ShowMessage(detailsMessage, LocalizationManager.Translate("تفاصيل الإذن"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في عرض التفاصيل: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }



        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // السماح فقط بالأرقام
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
                    "Draft" => new SolidColorBrush(Colors.Gray), // مسودة
                    "Under Review" => new SolidColorBrush(Colors.Blue), // قيد الانتظار
                    "Pending" => new SolidColorBrush(Colors.Orange), // قيد الانتظار
                    "Approved" => new SolidColorBrush(Colors.Green), // موافق عليه
                    "Rejected" => new SolidColorBrush(Colors.Red), // مرفوض
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
                // استخدام مكتبة PersianDateTime أو كتابة منطق التحويل
                return dateTime.ToString("yyyy/MM/dd");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // أضف هذا الـ Converter في نهاية الملف
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
                // تحقق من صلاحيات المستخدم
                bool isManager = App.CurrentUser?.JobTitle?.IsManager ?? false;
                bool isHR = App.CurrentUser?.Department?.IsHR ?? false;

                // إظهار الزر للمدير إذا كانت الحالة Pending
                if (status == "Pending" && isManager)
                    return Visibility.Visible;

                // إظهار الزر للموارد البشرية إذا كانت الحالة Under Review
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
