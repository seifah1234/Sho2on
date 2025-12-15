using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    /// <summary>
    /// Interaction logic for DeductionsWindow.xaml
    /// </summary>
    public partial class DeductionsWindow : Window
    {
        private readonly AppDbContext _context;
        private int _employeeCode;
        private int _type = 7;
        private int _operation = 1;

        public DeductionsWindow(int code, string name)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _employeeCode = code;

            code_box.Text = code.ToString();
            name_box.Text = name;
        }

        private async Task InitializeForm()
        {
            value_box.Text = "0";
            date_picker.SelectedDate = DateTime.Now;
            branch_box.ItemsSource = await _context.Branches
                .Where(b => App.userBranches.Contains(b.Id))
                .ToListAsync();
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من صحة البيانات المدخلة
                if (!ValidateInput())
                    return;

                // التحقق من صلاحية المستخدم للوصول إلى هذا الموظف
                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == _employeeCode && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                if (employee == null)
                {
                    MessageBox.Show("ليس لديك صلاحية الوصول إلى هذا الموظف", "خطأ في الصلاحية", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التحقق من عدم تكرار السجل لنفس النوع في نفس اليوم
                bool recordExists = await _context.Salaries
                    .AnyAsync(so => so.UserId == _employeeCode &&
                                   so.Type == _type &&
                                   so.DayDate == date_picker.SelectedDate.Value.Date);

                if (recordExists)
                {
                    MessageBox.Show("هناك سجل مسبق لنفس النوع في هذا التاريخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // إنشاء سجل جديد
                var salaryOperation = new Salary
                {
                    UserId = _employeeCode,
                    Amount = decimal.Parse(value_box.Text),
                    Notes = text_box.Text.Trim(),
                    Type = _type,
                    Operation = _operation,
                    DayDate = date_picker.SelectedDate.Value.Date,
                    CreatedAt = DateTime.Now,
                    EditedAt = DateTime.Now
                };

                await _context.Salaries.AddAsync(salaryOperation);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم إضافة البيانات بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm();
            }
            catch (FormatException)
            {
                MessageBox.Show("القيمة يجب أن تكون رقمية صحيحة", "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(text_box.Text))
            {
                MessageBox.Show("يرجى إدخال وصف للعملية", "حقل مطلوب", MessageBoxButton.OK, MessageBoxImage.Warning);
                text_box.Focus();
                return false;
            }

            if (date_picker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار التاريخ", "حقل مطلوب", MessageBoxButton.OK, MessageBoxImage.Warning);
                date_picker.Focus();
                return false;
            }

            if (date_picker.SelectedDate > DateTime.Now)
            {
                MessageBox.Show("لا يمكن اختيار تاريخ مستقبلي", "خطأ في التاريخ", MessageBoxButton.OK, MessageBoxImage.Warning);
                date_picker.Focus();
                return false;
            }

            if (!decimal.TryParse(value_box.Text, out decimal value) || value < 0)
            {
                MessageBox.Show("القيمة يجب أن تكون رقمية موجبة", "خطأ في القيمة", MessageBoxButton.OK, MessageBoxImage.Warning);
                value_box.Focus();
                return false;
            }

            return true;
        }

        private void ResetForm()
        {
            text_box.Clear();
            value_box.Text = "0";
            date_picker.SelectedDate = DateTime.Now;
            text_box.Focus();
        }

        private async void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (branch_box.SelectedValue == null || string.IsNullOrWhiteSpace(code_box.Text) || !int.TryParse(code_box.Text, out int code))
                {
                    MessageBox.Show("يرجى إدخال كود موظف صحيح و اختيار الفرع", "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == code && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                if (employee != null)
                {
                    name_box.Text = employee.FullName;
                    _employeeCode = code;
                }
                else
                {
                    MessageBox.Show("الموظف غير موجود أو ليس لديك صلاحية الوصول", "خطأ في الوصول", MessageBoxButton.OK, MessageBoxImage.Error);
                    code_box.Clear();
                    name_box.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GetValue()
        {
            if (text_box != null && value_box != null)
            {
                text_box.Text = string.Empty;
                value_box.Text = "0";
                text_box.Focus();
            }
        }

        // أحداث أزرار الراديو
        private void addes_Checked(object sender, RoutedEventArgs e)
        {
            _type = 7;
            _operation = 1;
            GetValue();
        }

        private void ancestor_Checked(object sender, RoutedEventArgs e)
        {
            _type = 9;
            _operation = 0;
            GetValue();
        }

        private void penalty_Checked(object sender, RoutedEventArgs e)
        {
            _type = 10;
            _operation = 0;
            GetValue();
        }

        private void reward_Checked(object sender, RoutedEventArgs e)
        {
            _type = 11;
            _operation = 1;
            GetValue();
        }

        private void deficit_Checked(object sender, RoutedEventArgs e)
        {
            _type = 16;
            _operation = 0;
            GetValue();
        }


        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void phone_check_Checked(object sender, RoutedEventArgs e)
        {
            _type = 20;
            _operation = 0;
            GetValue();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeForm();

        }
    }
}