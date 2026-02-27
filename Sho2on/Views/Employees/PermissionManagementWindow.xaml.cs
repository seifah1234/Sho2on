using HR_Application.ViewModels;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees
{
    public partial class PermissionManagementWindow : Window
    {
        private readonly AppDbContext _context;
        private List<PermissionViewModel> _permissions = new List<PermissionViewModel>();

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                               (p.ApprovedByUserId == null && App.CurrentUser.JobTitle.IsManager == true))
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
                    EmployeeName = p.User?.FullName ?? "غير معروف",
                    PermissionType = p.PermissionType,
                    PermissionTypeName = GetPermissionTypeName(p.PermissionType),
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason,
                    Status = GetStatusText(p.Status),
                    StatusEn = p.Status,
                    CreatedAt = p.CreatedAt,
                    EmployeeDepartment = p.User?.Department?.Name ?? "غير معروف",
                    EmployeeJobTitle = p.User?.JobTitle?.Name ?? "غير معروف",
                    ApprovedByName = p.ApprovedBy?.FullName ?? "لم تتم الموافقة بعد",
                    ApprovedDate = p.ApprovedDate,
                    RejectionReason = p.RejectionReason
                }).ToList();

                dgPermissions.ItemsSource = _permissions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل طلبات الإذن: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetPermissionTypeName(string permissionType)
        {
            return permissionType switch
            {
                "EarlyLeave" => "خروج مبكر",
                "LateEntry" => "دخول متأخر",
                "PersonalLeave" => "إذن شخصي",
                "Emergency" => "طارئ",
                "Official" => "رسمي",
                "Other" => "أخرى",
                _ => permissionType
            };
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "قيد الانتظار",
                "Approved" => "موافق عليه",
                "Rejected" => "مرفوض",
                "Draft" => "مسودة",
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
                    MessageBox.Show("لم يتم العثور على طلب الإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (isApprove)
                {
                    permission.Status = PermissionStatus.Approved;
                    permission.ApprovedDate = DateTime.Now;
                    permission.ApprovedByUserId = App.CurrentUser?.Id;

                    // تحديث سجل الحضور
                    await UpdateAttendanceForPermission(permission);

                    MessageBox.Show("تم الموافقة على طلب الإذن", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
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

                        MessageBox.Show("تم رفض طلب الإذن", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"خطأ في معالجة الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    MessageBox.Show(detailsMessage, "تفاصيل الإذن", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض التفاصيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // فتح نافذة اختيار الموظف
                var employeeSelectionWindow = new EmployeeSelectionWindow(allUsers, false, "اختر الموظف لطلب الإجازة");
                employeeSelectionWindow.Owner = this;

                if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
                {
                    var selectedEmployee = employeeSelectionWindow.SelectedUser;

                    txtEmployeeId.Text = selectedEmployee.Id.ToString();
                    employeeName_box.Text = selectedEmployee.FullName;
                }
            }
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
            if (value is string status && parameter is string paramStr)
            {
                return status == paramStr ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}