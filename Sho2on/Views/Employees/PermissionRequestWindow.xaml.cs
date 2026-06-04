using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
        private List<User> _employees = new List<User>();


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
                    var employee = _employees
                        .FirstOrDefault(u => u.Code == employeeCode);

                    if (employee != null)
                    {
                        SetSelectedEmployee(employee);
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الموظف: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employeeSelectionWindow = new EmployeeSelectionWindow(
                _context.Users.ToList(),
                false,
                LocalizationManager.Translate("اختر الموظف لطلب الإذن"),
                user_box.Text);
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

            txtEmployeeCode.Text = employee.Code.ToString();
            user_box.SelectedValue = employee.Code;

            // عرض معلومات الموظف
            txtDepartment.Text = employee.Department?.Name ?? LocalizationManager.Translate("غير محدد");
            txtBranch.Text = employee.Branch?.Name ?? LocalizationManager.Translate("غير محدد");
            txtJobTitle.Text = employee.JobTitle?.Name ?? LocalizationManager.Translate("غير محدد");
            txtShift.Text = employee.Shift?.Name ?? LocalizationManager.Translate("غير محدد");

            panelEmployeeInfo.Visibility = Visibility.Visible;

            if (employee.Manager != null)
            {
                _selectedApprover = employee.Manager;
                txtApproverName.Text = _selectedApprover.FullName;
            }
            // تحميل المديرين
            LoadApprovers();
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
                    itemsList = _employees;
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
                LocalizationManager.ShowMessage($"خطأ في تحميل المديرين: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSelectApprover_Click(object sender, RoutedEventArgs e)
        {
            if (_managers.Count == 0)
            {
                LocalizationManager.ShowMessage("لا يوجد مديرين متاحين للاختيار", LocalizationManager.Translate("معلومة"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var managerSelectionWindow = new EmployeeSelectionWindow(_managers, true, LocalizationManager.Translate("اختر الموافق على الإذن"));
            managerSelectionWindow.Owner = this;

            if (managerSelectionWindow.ShowDialog() == true && managerSelectionWindow.SelectedUser != null)
            {
                _selectedApprover = managerSelectionWindow.SelectedUser;
                txtApproverName.Text = _selectedApprover.FullName;
            }
        }


        private void dpPermissionDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateDuration();
        }

        private void CalculateDuration()
        {
            if (!txtStartTime.SelectedTime.HasValue || !txtEndTime.SelectedTime.HasValue)
                return;

            var startTime = txtStartTime.SelectedTime.Value.TimeOfDay;
            var endTime = txtEndTime.SelectedTime.Value.TimeOfDay;

            if (endTime <= startTime)
            {
                txtDuration.Text = "0";
                LocalizationManager.ShowMessage("وقت النهاية يجب أن يكون بعد وقت البداية", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                        StartDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtStartTime.SelectedTime.Value.TimeOfDay),
                        EndDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtEndTime.SelectedTime.Value.TimeOfDay),
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

                    LocalizationManager.ShowMessage("تم تقديم طلب الإذن بنجاح وهو قيد انتظار الموافقة", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ في تقديم الطلب: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private DateTime CombineDateAndTime(DateTime date, TimeSpan time)
        {
            return date.Date + time;
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
                        StartDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtStartTime.SelectedTime.Value.TimeOfDay),
                        EndDateTime = CombineDateAndTime(dpPermissionDate.SelectedDate.Value, txtEndTime.SelectedTime.Value.TimeOfDay),
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

                    LocalizationManager.ShowMessage("تم حفظ الطلب كمسودة", LocalizationManager.Translate("حفظ مسودة"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ في حفظ المسودة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = LocalizationManager.ShowMessage("هل تريد إلغاء طلب الإذن؟", LocalizationManager.Translate("تأكيد الإلغاء"),
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
                LocalizationManager.ShowMessage("يرجى اختيار الموظف أولاً", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من نوع الإذن
            if (cmbPermissionType.SelectedItem == null)
            {
                LocalizationManager.ShowMessage("يرجى اختيار نوع الإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من التاريخ
            if (dpPermissionDate.SelectedDate == null)
            {
                LocalizationManager.ShowMessage("يرجى تحديد تاريخ الإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من الأوقات
            if (!txtStartTime.SelectedTime.HasValue || !txtEndTime.SelectedTime.HasValue)
            {
                LocalizationManager.ShowMessage("يرجى إدخال وقت صحيح (HH:mm)", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من المدة
            if (string.IsNullOrEmpty(txtDuration.Text) || double.Parse(txtDuration.Text) <= 0)
            {
                LocalizationManager.ShowMessage("يرجى التحقق من المدة الزمنية للإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من الموافق (إذا لم يكن مسودة)
            if (!skipManager && _selectedApprover == null)
            {
                LocalizationManager.ShowMessage("يرجى اختيار الموافق على الإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // التحقق من السبب
            if (string.IsNullOrEmpty(txtReason.Text))
            {
                LocalizationManager.ShowMessage("يرجى كتابة سبب الإذن", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void txtEmployeeCode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab || e.Key == System.Windows.Input.Key.Enter)
            {
                LoadEmployeeByCode(txtEmployeeCode.Text);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                _employees = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Shift)
                    .Include(u => u.Manager)
                    .ToListAsync();

                user_box.ItemsSource = _employees;

                LoadEmployeeByCode(App.CurrentUser.Code);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message);
            }
        }

        private void txtStartTime_SelectedTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            CalculateDuration();

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
