using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees
{
    public partial class PermissionRequestWindow : Window
    {
        private readonly AppDbContext _context;
        private int _employeeId;
        private User _selectedEmployee;
        private User _selectedApprover;
        private List<User> _managers = new List<User>();

        public PermissionRequestWindow(string employeeCode = null)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);

            // تعيين التاريخ الحالي

            if (!string.IsNullOrEmpty(employeeCode))
            {
                txtEmployeeCode.Text = employeeCode;
                LoadEmployeeByCode(employeeCode);
            }
        }

        private async void LoadEmployeeByCode(string employeeCode)
        {
            try
            {
                if (!string.IsNullOrEmpty(employeeCode))
                {
                    var employee = await _context.Users
                        .Include(u => u.Department)
                        .Include(u => u.Branch)
                        .Include(u => u.JobTitle)
                        .Include(u => u.Shift)
                        .FirstOrDefaultAsync(u => u.Code == employeeCode);

                    if (employee != null)
                    {
                        SetSelectedEmployee(employee);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employeeSelectionWindow = new EmployeeSelectionWindow(
                _context.Users.ToList(),
                false,
                "اختر الموظف لطلب الإذن");
            employeeSelectionWindow.Owner = this;

            if (employeeSelectionWindow.ShowDialog() == true && employeeSelectionWindow.SelectedUser != null)
            {
                SetSelectedEmployee(employeeSelectionWindow.SelectedUser);
            }
        }

        private void SetSelectedEmployee(User employee)
        {
            _selectedEmployee = employee;
            _employeeId = employee.Id;

            txtEmployeeCode.Text = employee.Id.ToString();
            txtEmployeeName.Text = employee.FullName;

            // عرض معلومات الموظف
            txtDepartment.Text = employee.Department?.Name ?? "غير محدد";
            txtBranch.Text = employee.Branch?.Name ?? "غير محدد";
            txtJobTitle.Text = employee.JobTitle?.Name ?? "غير محدد";
            txtShift.Text = employee.Shift?.Name ?? "غير محدد";

            panelEmployeeInfo.Visibility = Visibility.Visible;

            // تحميل المديرين
            LoadApprovers();
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
                MessageBox.Show($"خطأ في تحميل المديرين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectApprover_Click(object sender, RoutedEventArgs e)
        {
            if (_managers.Count == 0)
            {
                MessageBox.Show("لا يوجد مديرين متاحين للاختيار", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var managerSelectionWindow = new EmployeeSelectionWindow(_managers, true, "اختر الموافق على الإذن");
            managerSelectionWindow.Owner = this;

            if (managerSelectionWindow.ShowDialog() == true && managerSelectionWindow.SelectedUser != null)
            {
                _selectedApprover = managerSelectionWindow.SelectedUser;
                txtApproverName.Text = _selectedApprover.FullName;
            }
        }

        private void Time_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateDuration();
        }

        private void dpPermissionDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateDuration();
        }

        private void CalculateDuration()
        {
            if (string.IsNullOrEmpty(txtStartTime.Text) || string.IsNullOrEmpty(txtEndTime.Text))
                return;

            if (!IsValidTime(txtStartTime.Text) || !IsValidTime(txtEndTime.Text))
                return;

            var startTime = TimeSpan.Parse(txtStartTime.Text);
            var endTime = TimeSpan.Parse(txtEndTime.Text);

            if (endTime <= startTime)
            {
                txtDuration.Text = "0";
                MessageBox.Show("وقت النهاية يجب أن يكون بعد وقت البداية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var duration = (endTime - startTime).TotalHours;
            txtDuration.Text = duration.ToString("F2");
        }

        private bool IsValidTime(string time)
        {
            return Regex.IsMatch(time, @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$");
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    var permission = new EmployeePermission
                    {
                        UserId = _employeeId,
                        PermissionType = (cmbPermissionType.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                        StartDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtStartTime.Text),
                        EndDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtEndTime.Text),
                        Duration = double.Parse(txtDuration.Text),
                        Reason = txtReason.Text,
                        Notes = txtNotes.Text,
                        Status = PermissionStatus.Pending,
                        ApprovedByUserId = _selectedApprover?.Id,
                        BranchId = _selectedEmployee.BranchId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.EmployeePermissions.Add(permission);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم تقديم طلب الإذن بنجاح وهو قيد انتظار الموافقة", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تقديم الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private DateTime CombineDateAndTime(DateTime date, string time)
        {
            var timeSpan = TimeSpan.Parse(time);
            return date.Date + timeSpan;
        }

        private async void btnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm(skipManager: true))
            {
                try
                {
                    var permission = new EmployeePermission
                    {
                        UserId = _employeeId,
                        PermissionType = (cmbPermissionType.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                        StartDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtStartTime.Text),
                        EndDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtEndTime.Text),
                        Duration = double.Parse(txtDuration.Text),
                        Reason = txtReason.Text,
                        Notes = txtNotes.Text,
                        Status = "Draft",
                        BranchId = _selectedEmployee.BranchId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.EmployeePermissions.Add(permission);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم حفظ الطلب كمسودة", "حفظ مسودة", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حفظ المسودة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل تريد إلغاء طلب الإذن؟", "تأكيد الإلغاء",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private bool ValidateForm(bool skipManager = false)
        {
            // التحقق من الموظف
            if (_selectedEmployee == null)
            {
                MessageBox.Show("يرجى اختيار الموظف أولاً", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من نوع الإذن
            if (cmbPermissionType.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار نوع الإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من التاريخ
            if (dpPermissionDate.SelectedDate == null)
            {
                MessageBox.Show("يرجى تحديد تاريخ الإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من الأوقات
            if (!IsValidTime(txtStartTime.Text) || !IsValidTime(txtEndTime.Text))
            {
                MessageBox.Show("يرجى إدخال وقت صحيح (HH:mm)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من المدة
            if (string.IsNullOrEmpty(txtDuration.Text) || double.Parse(txtDuration.Text) <= 0)
            {
                MessageBox.Show("يرجى التحقق من المدة الزمنية للإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من الموافق (إذا لم يكن مسودة)
            if (!skipManager && _selectedApprover == null)
            {
                MessageBox.Show("يرجى اختيار الموافق على الإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من السبب
            if (string.IsNullOrEmpty(txtReason.Text))
            {
                MessageBox.Show("يرجى كتابة سبب الإذن", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                txtReason.Focus();
                return false;
            }

            return true;
        }

        private void cmbPermissionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق إضافي هنا بناءً على نوع الإذن
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }

    // Converter لعرض المدة
    public class DurationToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double duration)
            {
                int hours = (int)duration;
                int minutes = (int)((duration - hours) * 60);

                if (minutes > 0)
                    return $"{hours} ساعة و {minutes} دقيقة";
                else
                    return $"{hours} ساعة";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}