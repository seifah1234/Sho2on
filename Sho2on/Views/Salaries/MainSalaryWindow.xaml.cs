using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class MainSalaryWindow : Window
    {
        private int salaryType = 1;
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private FriendshipBoxService _friendshipBoxService;

        private List<User> _filteredUsers = new List<User>();
        private int _currentUserIndex = -1;
        private User _currentUser = null;

        public MainSalaryWindow()
        {
            InitializeComponent();
            _friendshipBoxService = new FriendshipBoxService(_context);
            Clear();
        }

        private void ClearSalary()
        {
            salary_box.Text = "0";
            transmission_box.Text = "0";
            housing_box.Text = "0";
            insurance_box.Text = "0";
            tax_box.Text = "0";
            social_box.Text = "0";
            abcence_box.Text = "0";
            box_box.Text = "0";
            loanMax_box.Text = "0";
            depart_box.Text = "0";
            natural_box.Text = "0";
            comp_insurance_box.Text = "0";
        }

        private void Clear()
        {
            // مسح جميع الحقول

            ClearSalary();

            // مسح حقول الساعة
            hour_box.Text = "";
            hour_price_box.Text = "";
            day_hour_box.Text = "";
            shift_hour_box.Text = "";
            shift_price_box.Text = "";
            day_shift_box.Text = "";

            // مسح معلومات الموظف
            ClearEmployeeInfo();

            // إعادة تعيين نوع الراتب
            fixed_box.IsChecked = true;
            salaryType = 1;
            UpdateSalaryTypeVisibility();

            // تحديث العداد
            UpdateNavigationCounter();
        }

        private void ClearEmployeeInfo()
        {
            employeeNameText.Text = "-";
            employeeCodeText.Text = "-";
            employeeBranchText.Text = "-";
            employeeDepartmentText.Text = "-";
            employeeJobText.Text = "-";
            employeeWorkTypeText.Text = "-";
            employeeHireDateText.Text = "-";
            employeeSalaryText.Text = "-";
            employeeLoanLimitText.Text = "-";
            totalSalaryText.Text = "0.00";
        }

        private void UpdateNavigationCounter()
        {
            currentIndexTxt.Text = $"{_currentUserIndex + 1} / {_filteredUsers.Count}";
        }

        private void UpdateEmployeeInfo(User user)
        {
            if (user == null) return;

            employeeNameText.Text = user.FullName ?? "-";
            employeeCodeText.Text = user.Code?.ToString() ?? "-";
            employeeBranchText.Text = user.Branch?.Name ?? "-";
            employeeDepartmentText.Text = user.Department?.Name ?? "-";
            employeeJobText.Text = user.JobTitle?.Name ?? "-";
            employeeWorkTypeText.Text = user.JobType?.Name ?? "-";
            employeeHireDateText.Text = user.HireDate.ToString("yyyy-MM-dd");
            employeeSalaryText.Text = (user.MainSalary ?? 0).ToString("N2");
            employeeLoanLimitText.Text = (user.LoanMaxAmount ?? 0).ToString("N2");
            loanMax_box.Text = (user.LoanMaxAmount ?? 0).ToString("N2");

            employeeInfoText.Text = $"معلومات الموظف - {user.FullName}";
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    MessageBox.Show("يرجى اختيار موظف أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = await _context.Users
                    .Include(u => u.Shift)
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

                if (user == null)
                {
                    MessageBox.Show("المستخدم غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // تحديث ساعات العمل
                if (user.WorkHours == TimeSpan.Zero && user.Shift != null)
                {
                    user.WorkHours = CalculateWorkHours(user.Shift);
                }

                // تحديث حد السلف
                user.LoanMaxAmount = Convert.ToDecimal(loanMax_box.Text);

                // تحديث الراتب الأساسي بناءً على النوع
                UpdateUserSalary(user);

                // تحديث أو إضافة الرواتب
                await UpdateOrCreateSalaries(user);

                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ المرتب بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // تحديث معلومات الموظف بعد الحفظ
                UpdateEmployeeInfo(user);
                CalculateTotalSalary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TimeSpan CalculateWorkHours(Shift shift)
        {
            if (shift.StartTime > shift.EndTime)
            {
                return shift.EndTime.Add(new TimeSpan(1, 0, 0, 0)) - shift.StartTime;
            }
            return shift.EndTime - shift.StartTime;
        }

        private void UpdateUserSalary(User user)
        {
            switch (salaryType)
            {
                case 2: // shift
                    user.MainSalary = Convert.ToDecimal(salary_box.Text);
                    user.MinSalary = (user.MainSalary / 30m) / ((decimal)user.WorkHours.TotalHours) / 60m;
                    break;
                case 3: // hour
                    user.MainSalary = Convert.ToDecimal(salary_box.Text);
                    user.MinSalary = user.MainSalary / 60m;
                    break;
                default: // fixed
                    user.MainSalary = Convert.ToDecimal(salary_box.Text);
                    user.MinSalary = (user.MainSalary / 30m) / ((decimal)user.WorkHours.TotalHours) / 60m;
                    break;
            }
        }

        private async Task UpdateOrCreateSalaries(User user)
        {
            var existingSalaries = await _context.Salaries
                .Where(s => s.UserId == user.Id)
                .ToListAsync();

            var salaryTypes = new Dictionary<int, (string TextBox, int Operation)>
            {
                { 1, (salary_box.Text, 1) },      // راتب - إضافة
                { 2, (housing_box.Text, 1) },     // بدل سكن - إضافة
                { 3, (transmission_box.Text, 1) }, // بدل انتقال - إضافة
                { 4, (insurance_box.Text, 0) },   // تأمينات - خصم
                { 5, (tax_box.Text, 0) },         // ضريبة - خصم
                { 6, (social_box.Text, 0) },      // أخرى - خصم
                { 12, (abcence_box.Text, 0) },    // غياب - خصم
                { 13, (box_box.Text, 0) },        // صندوق الزمالة - خصم
                { 14, (depart_box.Text, 1) },     // بدل إدارة - إضافة
                { 15, (natural_box.Text, 1) },    // بدل طبيعة عمل - إضافة
                { 16, (comp_insurance_box.Text, 0) } // تأمينات الشركة - خصم
            };

            foreach (var salaryTypeDic in salaryTypes)
            {
                var existingSalary = existingSalaries.FirstOrDefault(s => s.Type == salaryTypeDic.Key);

                if (existingSalary != null)
                {
                    // تحديث الراتب الموجود
                    existingSalary.Amount = Convert.ToDecimal(salaryTypeDic.Value.TextBox);
                    existingSalary.SalaryType = salaryType;
                    existingSalary.EditedAt = DateTime.Now;
                }
                else
                {
                    // إضافة راتب جديد
                    _context.Salaries.Add(new Salary
                    {
                        UserId = user.Id,
                        Amount = Convert.ToDecimal(salaryTypeDic.Value.TextBox),
                        Type = salaryTypeDic.Key,
                        SalaryType = salaryType,
                        Operation = salaryTypeDic.Value.Operation,
                        DayDate = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        EditedAt = DateTime.Now
                    });
                }
            }
        }

        private async void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear();

                // تطبيق عوامل التصفية
                await ApplyFilters();

                if (_filteredUsers.Count > 0)
                {
                    _currentUserIndex = 0;
                    _currentUser = _filteredUsers[_currentUserIndex];
                    await LoadUserData(_currentUser);
                    UpdateNavigationCounter();

                    if (_filteredUsers.Count > 1)
                    {
                        MessageBox.Show($"تم العثور على {_filteredUsers.Count} موظف", "معلومات",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على أي موظف", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApplyFilters()
        {
            var query = _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Include(u => u.JobType)
                .AsQueryable();

            // تطبيق عامل التصفية بالكود
            if (!string.IsNullOrEmpty(code_box.Text))
            {
                query = query.Where(u => u.Code.Contains(code_box.Text));
            }

            // تطبيق عامل التصفية بالاسم
            if (!string.IsNullOrEmpty(name_box.Text))
            {
                query = query.Where(u => u.FullName.Contains(name_box.Text));
            }

            // تطبيق عامل التصفية بالفرع
            if (branch_box.SelectedValue != null)
            {
                int branchId = (int)branch_box.SelectedValue;
                query = query.Where(u => u.BranchId == branchId);
            }

            // التحقق من صلاحيات المستخدم
            query = query.Where(u => App.userBranches.Contains(u.BranchId));

            _filteredUsers = await query.ToListAsync();
        }

        private async Task LoadUserData(User user)
        {
            try
            {
                ClearSalary();
                // تحميل بيانات الرواتب
                var salaries = await _context.Salaries
                    .Where(s => s.UserId == user.Id)
                    .ToListAsync();

                if (salaries.Any())
                {
                    foreach (var salary in salaries)
                    {
                        switch (salary.Type)
                        {
                            case 1:
                                salary_box.Text = salary.Amount.ToString();
                                salaryType = salary.SalaryType;
                                UpdateSalaryTypeVisibility();
                                break;
                            case 2:
                                housing_box.Text = salary.Amount.ToString();
                                break;
                            case 3:
                                transmission_box.Text = salary.Amount.ToString();
                                break;
                            case 4:
                                insurance_box.Text = salary.Amount.ToString();
                                break;
                            case 5:
                                tax_box.Text = salary.Amount.ToString();
                                break;
                            case 6:
                                social_box.Text = salary.Amount.ToString();
                                break;
                            case 12:
                                abcence_box.Text = salary.Amount.ToString();
                                break;
                            case 13:
                                box_box.Text = salary.Amount.ToString();
                                break;
                            case 14:
                                depart_box.Text = salary.Amount.ToString();
                                break;
                            case 15:
                                natural_box.Text = salary.Amount.ToString();
                                break;
                            case 16:
                                comp_insurance_box.Text = salary.Amount.ToString();
                                break;
                        }
                    }
                }

                    // تحديث معلومات الموظف
                    UpdateEmployeeInfo(user);

                // حساب الإجمالي
                CalculateTotalSalary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الموظف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void next_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredUsers.Count == 0) return;

            if (_currentUserIndex < _filteredUsers.Count - 1)
            {
                _currentUserIndex++;
                _currentUser = _filteredUsers[_currentUserIndex];
                await LoadUserData(_currentUser);
                UpdateNavigationCounter();
            }
        }

        private async void prev_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredUsers.Count == 0) return;

            if (_currentUserIndex > 0)
            {
                _currentUserIndex--;
                _currentUser = _filteredUsers[_currentUserIndex];
                await LoadUserData(_currentUser);
                UpdateNavigationCounter();
            }
        }

        private void CalculateTotalSalary()
        {
            try
            {
                decimal total = 0;

                // الإضافات
                total += (string.IsNullOrEmpty(salary_box.Text)) ? 0 : Convert.ToDecimal(salary_box.Text);
                total += (string.IsNullOrEmpty(housing_box.Text)) ? 0 : Convert.ToDecimal(housing_box.Text);
                total += (string.IsNullOrEmpty(transmission_box.Text)) ? 0 : Convert.ToDecimal(transmission_box.Text);
                total += (string.IsNullOrEmpty(depart_box.Text)) ? 0 : Convert.ToDecimal(depart_box.Text);
                total += (string.IsNullOrEmpty(natural_box.Text)) ? 0 : Convert.ToDecimal(natural_box.Text);

                // الخصومات
                total -= (string.IsNullOrEmpty(insurance_box.Text)) ? 0 : Convert.ToDecimal(insurance_box.Text);
                total -= (string.IsNullOrEmpty(tax_box.Text)) ? 0 : Convert.ToDecimal(tax_box.Text);
                total -= (string.IsNullOrEmpty(social_box.Text)) ? 0 : Convert.ToDecimal(social_box.Text);
                total -= (string.IsNullOrEmpty(abcence_box.Text)) ? 0 : Convert.ToDecimal(abcence_box.Text);
                total -= (string.IsNullOrEmpty(box_box.Text)) ? 0 : Convert.ToDecimal(box_box.Text);
                total -= (string.IsNullOrEmpty(comp_insurance_box.Text)) ? 0 : Convert.ToDecimal(comp_insurance_box.Text);

                totalSalaryText.Text = total.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب الإجمالي: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateTotal_Click(object sender, RoutedEventArgs e)
        {
            CalculateTotalSalary();
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            code_box.Clear();
            Clear();
        }

        private void OpenSalaryReport_Click(object sender, RoutedEventArgs e)
        {
            // هنا يمكنك فتح نافذة تقرير المرتبات
            MessageBox.Show("سيتم فتح تقرير المرتبات هنا", "معلومة",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpdateSalaryTypeVisibility()
        {
            switch (salaryType)
            {
                case 1: // fixed
                    fixed_box.IsChecked = true;
                    HourSalary.Visibility = Visibility.Collapsed;
                    ShiftSalary.Visibility = Visibility.Collapsed;
                    break;
                case 2: // shift
                    shift.IsChecked = true;
                    HourSalary.Visibility = Visibility.Collapsed;
                    ShiftSalary.Visibility = Visibility.Visible;
                    break;
                case 3: // hour
                    hour.IsChecked = true;
                    HourSalary.Visibility = Visibility.Visible;
                    ShiftSalary.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void fixed_Checked(object sender, RoutedEventArgs e)
        {
            HourSalary.Visibility = Visibility.Collapsed;
            ShiftSalary.Visibility = Visibility.Collapsed;
            salaryType = 1;
            salary_box.IsEnabled = true;
        }

        private void shift_Checked(object sender, RoutedEventArgs e)
        {
            HourSalary.Visibility = Visibility.Collapsed;
            ShiftSalary.Visibility = Visibility.Visible;
            salary_box.IsEnabled = false;
            salaryType = 2;
        }

        private void hour_Checked(object sender, RoutedEventArgs e)
        {
            HourSalary.Visibility = Visibility.Visible;
            ShiftSalary.Visibility = Visibility.Collapsed;
            salary_box.IsEnabled = false;
            salaryType = 3;
        }

        private void hour_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateHourlySalary();
        }

        private void hour_price_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateHourlySalary();
        }

        private void day_hour_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateHourlySalary();
        }

        private void shift_hour_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateShiftSalary();
        }

        private void shift_price_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateShiftSalary();
        }

        private void day_shift_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateShiftSalary();
        }

        private void CalculateHourlySalary()
        {
            try
            {
                if (!string.IsNullOrEmpty(hour_box.Text) &&
                    !string.IsNullOrEmpty(hour_price_box.Text) &&
                    !string.IsNullOrEmpty(day_hour_box.Text))
                {
                    double hour = Convert.ToDouble(hour_box.Text);
                    double hourPrice = Convert.ToDouble(hour_price_box.Text);
                    double days = Convert.ToDouble(day_hour_box.Text);

                    if (days > 0)
                    {
                        double salary = hour * hourPrice / days;
                        salary_box.Text = Math.Round(salary, 3).ToString();
                        CalculateTotalSalary();
                    }
                }
            }
            catch (Exception)
            {
                // تجاهل الأخطاء في الإدخال
            }
        }

        private void CalculateShiftSalary()
        {
            try
            {
                if (!string.IsNullOrEmpty(shift_hour_box.Text) &&
                    !string.IsNullOrEmpty(shift_price_box.Text) &&
                    !string.IsNullOrEmpty(day_shift_box.Text))
                {
                    double hour = Convert.ToDouble(shift_hour_box.Text);
                    double hourPrice = Convert.ToDouble(shift_price_box.Text);
                    double days = Convert.ToDouble(day_shift_box.Text);

                    double salary = hour * hourPrice * days;
                    salary_box.Text = Math.Round(salary, 3).ToString();
                    CalculateTotalSalary();
                }
            }
            catch (Exception)
            {
                // تجاهل الأخطاء في الإدخال
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var branches = await _context.Branches.ToListAsync();
                branch_box.ItemsSource = branches;
                branch_box.DisplayMemberPath = "Name";
                branch_box.SelectedValuePath = "Id";

                // تحميل بيانات المستخدم الحالي إذا كان هناك موظف محدد
                if (_currentUser != null)
                {
                    await LoadUserData(_currentUser);
                }

                fixed_box.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // تحديث الإجمالي عند تغيير أي حقل
            CalculateTotalSalary();
        }
    }
}