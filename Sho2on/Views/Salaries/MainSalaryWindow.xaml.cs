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
            // „”Õ Ã„Ì⁄ «·ÕﬁÊ·

            ClearSalary();

            // „”Õ ÕﬁÊ· «·”«⁄…
            hour_box.Text = "";
            hour_price_box.Text = "";
            day_hour_box.Text = "";
            shift_hour_box.Text = "";
            shift_price_box.Text = "";
            day_shift_box.Text = "";

            // „”Õ „⁄·Ê„«  «·„ÊŸ›
            ClearEmployeeInfo();

            // ≈⁄«œ…  ⁄ÌÌ‰ ‰Ê⁄ «·—« »
            fixed_box.IsChecked = true;
            salaryType = 1;
            UpdateSalaryTypeVisibility();

            //  ÕœÌÀ «·⁄œ«œ
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

            employeeInfoText.Text = $"„⁄·Ê„«  «·„ÊŸ› - {user.FullName}";
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— „ÊŸ› √Ê·«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = await _context.Users
                    .Include(u => u.Shift)
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

                if (user == null)
                {
                    LocalizationManager.ShowMessage("«·„” Œœ„ €Ì— „ÊÃÊœ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //  ÕœÌÀ ”«⁄«  «·⁄„·
                if (user.WorkHours == TimeSpan.Zero && user.Shift != null)
                {
                    user.WorkHours = CalculateWorkHours(user.Shift);
                }

                //  ÕœÌÀ Õœ «·”·›
                user.LoanMaxAmount = Convert.ToDecimal(loanMax_box.Text);

                //  ÕœÌÀ «·—« » «·√”«”Ì »‰«¡ ⁄·Ï «·‰Ê⁄
                UpdateUserSalary(user);

                //  ÕœÌÀ √Ê ≈÷«›… «·—Ê« »
                await UpdateOrCreateSalaries(user);

                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage(" „ Õ›Ÿ «·„— » »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);

                //  ÕœÌÀ „⁄·Ê„«  «·„ÊŸ› »⁄œ «·Õ›Ÿ
                UpdateEmployeeInfo(user);
                CalculateTotalSalary();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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
                { 1, (salary_box.Text, 1) },      // —« » - ≈÷«›…
                { 2, (housing_box.Text, 1) },     // »œ· ”ﬂ‰ - ≈÷«›…
                { 3, (transmission_box.Text, 1) }, // »œ· «‰ ﬁ«· - ≈÷«›…
                { 4, (insurance_box.Text, 0) },   //  √„Ì‰«  - Œ’„
                { 5, (tax_box.Text, 0) },         // ÷—Ì»… - Œ’„
                { 6, (social_box.Text, 0) },      // √Œ—Ï - Œ’„
                { 12, (abcence_box.Text, 0) },    // €Ì«» - Œ’„
                { 13, (box_box.Text, 0) },        // ’‰œÊﬁ «·“„«·… - Œ’„
                { 14, (depart_box.Text, 1) },     // »œ· ≈œ«—… - ≈÷«›…
                { 15, (natural_box.Text, 1) },    // »œ· ÿ»Ì⁄… ⁄„· - ≈÷«›…
                { 16, (comp_insurance_box.Text, 0) } //  √„Ì‰«  «·‘—ﬂ… - Œ’„
            };

            foreach (var salaryTypeDic in salaryTypes)
            {
                var existingSalary = existingSalaries.FirstOrDefault(s => s.Type == salaryTypeDic.Key);

                if (existingSalary != null)
                {
                    //  ÕœÌÀ «·—« » «·„ÊÃÊœ
                    existingSalary.Amount = Convert.ToDecimal(salaryTypeDic.Value.TextBox);
                    existingSalary.SalaryType = salaryType;
                    existingSalary.EditedAt = DateTime.Now;
                }
                else
                {
                    // ≈÷«›… —« » ÃœÌœ
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

                //  ÿ»Ìﬁ ⁄Ê«„· «· ’›Ì…
                await ApplyFilters();

                if (_filteredUsers.Count > 0)
                {
                    _currentUserIndex = 0;
                    _currentUser = _filteredUsers[_currentUserIndex];
                    await LoadUserData(_currentUser);
                    UpdateNavigationCounter();

                    if (_filteredUsers.Count > 1)
                    {
                        LocalizationManager.ShowMessage($" „ «·⁄ÀÊ— ⁄·Ï {_filteredUsers.Count} „ÊŸ›", "„⁄·Ê„« ",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï √Ì „ÊŸ›", " Õ–Ì—",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·»ÕÀ: {ex.Message}", "Œÿ√",
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

            //  ÿ»Ìﬁ ⁄«„· «· ’›Ì… »«·ﬂÊœ
            if (!string.IsNullOrEmpty(code_box.Text))
            {
                query = query.Where(u => u.Code.Contains(code_box.Text));
            }


            //  ÿ»Ìﬁ ⁄«„· «· ’›Ì… »«·›—⁄
            if (branch_box.SelectedValue != null)
            {
                int branchId = (int)branch_box.SelectedValue;
                query = query.Where(u => u.BranchId == branchId);
            }

            // «· Õﬁﬁ „‰ ’·«ÕÌ«  «·„” Œœ„
            query = query.Where(u => App.userBranches.Contains(u.BranchId));

            _filteredUsers = await query.ToListAsync();
        }

        private async Task LoadUserData(User user)
        {
            try
            {
                ClearSalary();
                //  Õ„Ì· »Ì«‰«  «·—Ê« »
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

                    //  ÕœÌÀ „⁄·Ê„«  «·„ÊŸ›
                    UpdateEmployeeInfo(user);

                // Õ”«» «·≈Ã„«·Ì
                CalculateTotalSalary();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· »Ì«‰«  «·„ÊŸ›: {ex.Message}", "Œÿ√",
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

                // «·≈÷«›« 
                total += (string.IsNullOrEmpty(salary_box.Text)) ? 0 : Convert.ToDecimal(salary_box.Text);
                total += (string.IsNullOrEmpty(housing_box.Text)) ? 0 : Convert.ToDecimal(housing_box.Text);
                total += (string.IsNullOrEmpty(transmission_box.Text)) ? 0 : Convert.ToDecimal(transmission_box.Text);
                total += (string.IsNullOrEmpty(depart_box.Text)) ? 0 : Convert.ToDecimal(depart_box.Text);
                total += (string.IsNullOrEmpty(natural_box.Text)) ? 0 : Convert.ToDecimal(natural_box.Text);

                // «·Œ’Ê„« 
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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ”«» «·≈Ã„«·Ì: {ex.Message}", "Œÿ√",
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
            // Â‰« Ì„ﬂ‰ﬂ › Õ ‰«›–…  ﬁ—Ì— «·„— »« 
            LocalizationManager.ShowMessage("”Ì „ › Õ  ﬁ—Ì— «·„— »«  Â‰«", "„⁄·Ê„…",
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
                //  Ã«Â· «·√Œÿ«¡ ›Ì «·≈œŒ«·
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
                //  Ã«Â· «·√Œÿ«¡ ›Ì «·≈œŒ«·
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

                //  Õ„Ì· »Ì«‰«  «·„” Œœ„ «·Õ«·Ì ≈–« ﬂ«‰ Â‰«ﬂ „ÊŸ› „Õœœ
                if (_currentUser != null)
                {
                    await LoadUserData(_currentUser);
                }

                fixed_box.IsChecked = true;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  ÕœÌÀ «·≈Ã„«·Ì ⁄‰œ  €ÌÌ— √Ì Õﬁ·
            CalculateTotalSalary();
        }
    }
}
