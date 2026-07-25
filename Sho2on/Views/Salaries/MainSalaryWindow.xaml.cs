using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Services;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using static HR_Application.EmployeeData;
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
        private List<User> users = new List<User>();

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
                    LocalizationManager.ShowMessage("يرجى اختيار موظف أولاً", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = await _context.Users
                    .Include(u => u.Shift)
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

                if (user == null)
                {
                    LocalizationManager.ShowMessage("المستخدم غير موجود", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                LocalizationManager.ShowMessage("تم حفظ المرتب بنجاح", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);

                // تحديث معلومات الموظف بعد الحفظ
                UpdateEmployeeInfo(user);
                CalculateTotalSalary();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في حفظ البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                        LocalizationManager.ShowMessage($"تم العثور على {_filteredUsers.Count} موظف", LocalizationManager.Translate("معلومات"),
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LocalizationManager.ShowMessage("لم يتم العثور على أي موظف", LocalizationManager.Translate("تحذير"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في البحث: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                code_box.Text = user_box.SelectedValue.ToString();
                _currentUser = selectedUser;
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
                query = query.Where(u => u.Code == code_box.Text);
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
                LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الموظف: {ex.Message}", LocalizationManager.Translate("خطأ"),
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

                totalSalaryText.Text = total.ToString("F2");
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في حساب الإجمالي: {ex.Message}", LocalizationManager.Translate("خطأ"),
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
            LocalizationManager.ShowMessage("سيتم فتح تقرير المرتبات هنا", LocalizationManager.Translate("معلومة"),
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
            day_hour_box.Visibility = Visibility.Collapsed;
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
                    !string.IsNullOrEmpty(hour_price_box.Text))
                {
                    double hour = Convert.ToDouble(hour_box.Text);
                    double hourPrice = Convert.ToDouble(hour_price_box.Text);

                    
                        double salary = hour * hourPrice;
                        salary_box.Text = Math.Round(salary, 3).ToString();
                        CalculateTotalSalary();
                    
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

                var dbUsers = _context.Users.ToList();

                users.AddRange(dbUsers);
                user_box.ItemsSource = users;

                // تحميل بيانات المستخدم الحالي إذا كان هناك موظف محدد
                if (_currentUser != null)
                {
                    await LoadUserData(_currentUser);
                }

                fixed_box.IsChecked = true;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"),
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
