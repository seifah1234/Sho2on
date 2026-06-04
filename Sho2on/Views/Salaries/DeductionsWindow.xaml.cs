using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using static HR_Application.EmployeeData;
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
        private List<User> users = new List<User>();

        public DeductionsWindow(int code, string name)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _employeeCode = code;

            code_box.Text = code.ToString();
            user_box.Text = name;
        }

        private async Task InitializeForm()
        {
            value_box.Text = "0";
            date_picker.SelectedDate = DateTime.Now;
            branch_box.ItemsSource = await _context.Branches
                .Where(b => App.userBranches.Contains(b.Id))
                .ToListAsync();
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


        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // «· Õﬁﬁ „‰ ’Õ… «·»Ì«‰«  «·„œŒ·…
                if (!ValidateInput())
                    return;

                // «· Õﬁﬁ „‰ ’·«ÕÌ… «·„” Œœ„ ··Ê’Ê· ≈·Ï Â–« «·„ÊŸ›
                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == _employeeCode && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                if (employee == null)
                {
                    LocalizationManager.ShowMessage("·Ì” ·œÌﬂ ’·«ÕÌ… «·Ê’Ê· ≈·Ï Â–« «·„ÊŸ›", "Œÿ√ ›Ì «·’·«ÕÌ…", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // «· Õﬁﬁ „‰ ⁄œ„  ﬂ—«— «·”Ã· ·‰›” «·‰Ê⁄ ›Ì ‰›” «·ÌÊ„
                bool recordExists = await _context.Salaries
                    .AnyAsync(so => so.UserId == _employeeCode &&
                                   so.Type == _type &&
                                   so.DayDate == date_picker.SelectedDate.Value.Date);

                if (recordExists)
                {
                    LocalizationManager.ShowMessage("Â‰«ﬂ ”Ã· „”»ﬁ ·‰›” «·‰Ê⁄ ›Ì Â–« «· «—ÌŒ", " ‰»ÌÂ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ≈‰‘«¡ ”Ã· ÃœÌœ
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

                LocalizationManager.ShowMessage(" „ ≈÷«›… «·»Ì«‰«  »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm();
            }
            catch (FormatException)
            {
                LocalizationManager.ShowMessage("«·ﬁÌ„… ÌÃ» √‰  ﬂÊ‰ —ﬁ„Ì… ’ÕÌÕ…", "Œÿ√ ›Ì «·≈œŒ«·", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «·Õ›Ÿ: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(text_box.Text))
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· Ê’› ··⁄„·Ì…", "Õﬁ· „ÿ·Ê»", MessageBoxButton.OK, MessageBoxImage.Warning);
                text_box.Focus();
                return false;
            }

            if (date_picker.SelectedDate == null)
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— «· «—ÌŒ", "Õﬁ· „ÿ·Ê»", MessageBoxButton.OK, MessageBoxImage.Warning);
                date_picker.Focus();
                return false;
            }

            if (date_picker.SelectedDate > DateTime.Now)
            {
                LocalizationManager.ShowMessage("·« Ì„ﬂ‰ «Œ Ì«—  «—ÌŒ „” ﬁ»·Ì", "Œÿ√ ›Ì «· «—ÌŒ", MessageBoxButton.OK, MessageBoxImage.Warning);
                date_picker.Focus();
                return false;
            }

            if (!decimal.TryParse(value_box.Text, out decimal value) || value < 0)
            {
                LocalizationManager.ShowMessage("«·ﬁÌ„… ÌÃ» √‰  ﬂÊ‰ —ﬁ„Ì… „ÊÃ»…", "Œÿ√ ›Ì «·ﬁÌ„…", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (branch_box.SelectedValue == null || string.IsNullOrWhiteSpace(code_box.Text) || !string.IsNullOrWhiteSpace(code_box.Text))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· ﬂÊœ „ÊŸ› ’ÕÌÕ Ê «Œ Ì«— «·›—⁄", "Œÿ√ ›Ì «·≈œŒ«·", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Code == code_box.Text && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                if (employee != null)
                {
                    user_box.Text = employee.FullName;
                    _employeeCode = int.Parse(employee.Code);
                }
                else
                {
                    LocalizationManager.ShowMessage("«·„ÊŸ› €Ì— „ÊÃÊœ √Ê ·Ì” ·œÌﬂ ’·«ÕÌ… «·Ê’Ê·", "Œÿ√ ›Ì «·Ê’Ê·", MessageBoxButton.OK, MessageBoxImage.Error);
                    code_box.Clear();
                    user_box.Text = "";
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // √Õœ«À √“—«— «·—«œÌÊ
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
