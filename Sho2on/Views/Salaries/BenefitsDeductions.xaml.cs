using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Office.Interop.Excel;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using static HR_Application.EmployeeData;
using Application = Microsoft.Office.Interop.Excel.Application;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Range = Microsoft.Office.Interop.Excel.Range;
using Window = System.Windows.Window;
using Workbook = Microsoft.Office.Interop.Excel.Workbook;
using Worksheet = Microsoft.Office.Interop.Excel.Worksheet;

namespace HR_Application.Views
{
    /// <summary>
    /// Interaction logic for BenefitsDeductions.xaml
    /// </summary>
    public partial class BenefitsDeductions : Window
    {
        private readonly AppDbContext _context;
        private ObservableCollection<SalaryOperationViewModel> _deductions = new ObservableCollection<SalaryOperationViewModel>();
        private Dictionary<int, string> _operations = new Dictionary<int, string>();
        private Dictionary<int, string> _types = new Dictionary<int, string>();
        private List<User> users = new List<User>();

        public BenefitsDeductions()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            
        }

        private async Task InitializeDictionaries()
        {
            _operations.Add(0, "«” ﬁÿ«⁄« ");
            _operations.Add(1, "«” Õﬁ«ﬁ« ");

            _types.Add(11, "„ﬂ«›√…");
            _types.Add(10, "Ã“«¡");
            _types.Add(9, "”·›…");
            _types.Add(16, "⁄Ã“");

            _types.Add(1, "«·„— »");
            _types.Add(2, "»œ· ”ﬂ‰");
            _types.Add(3, "»œ· «‰ ﬁ«·");
            _types.Add(4, " √„Ì‰«  «·„ÊŸ›");

            _types.Add(5, "÷. ﬂ”» ⁄„·");
            _types.Add(6, "„‘«—ﬂ… «Ã „«⁄Ì…");
            _types.Add(12, "€Ì«»");
            _types.Add(13, "’‰œÊﬁ «·“„«·…");

            _types.Add(14, "»œ· ≈œ«—…");
            _types.Add(15, "»œ· ÿ»Ì⁄… ⁄„·");

            _types.Add(18, "⁄„Ê·«   ÕﬁÌﬁ");
            _types.Add(19, "⁄„Ê·«  Œ«—ÃÌ…");
            _types.Add(20, "›« Ê—…  ·Ì›Ê‰");

            branch_box.ItemsSource = await _context.Branches
                .Where(b => App.userBranches.Contains(b.Id))
                .ToListAsync();

        }

        private void InitializeDateSelections()
        {
            month_box.ItemsSource = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
            year_box.ItemsSource = Enumerable.Range(2010, 21).ToList();
            month_box.SelectedItem = DateTime.Now.ToString("MMMM", CultureInfo.CurrentCulture);
            year_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            year_box.SelectedItem = DateTime.Now.Year;

            var dbUsers = _context.Users.ToList();

            users.AddRange(dbUsers);
            user_box.ItemsSource = users;
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                code_box.Text = user_box.SelectedValue.ToString();
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


        private (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year)
        {
            try
            {
                int startDay = Convert.ToInt16(Properties.Settings.Default.StartOfMonth);
                int endDay = (month == 2 && Convert.ToInt16(Properties.Settings.Default.EndOfMonth) > 29) ? 29 : Convert.ToInt16(Properties.Settings.Default.EndOfMonth);

                DateTime startDate = new DateTime(year, month, startDay);
                DateTime endDate = new DateTime(year, month, endDay);

                if (15 < startDay) startDate = startDate.AddMonths(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ”«»  Ê«—ÌŒ «·‘Â— «·„Œ’’: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }

        public class SalaryOperationViewModel
        {
            public int ID { get; set; }
            public int RowNumber { get; set; }
            public string Value { get; set; }
            public string Text { get; set; }
            public string Operation { get; set; }
            public string Type { get; set; }
            public string Date { get; set; }
        }

        private async void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var deduction = button?.CommandParameter as SalaryOperationViewModel;
            if (deduction == null) return;

            try
            {
                MessageBoxResult result = LocalizationManager.ShowMessage("Â·  —Ìœ Õ–› Â–« «·”Ã·ø", " √ﬂÌœ «·Õ–›",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var salaryOperation = await _context.Salaries.FindAsync(deduction.ID);
                    if (salaryOperation != null)
                    {
                        _context.Salaries.Remove(salaryOperation);
                        await _context.SaveChangesAsync();

                        LocalizationManager.ShowMessage(" „ Õ–› «·”Ã· »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                        await GetDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·Õ–›: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            // Ì„ﬂ‰ ≈÷«›… ÊŸÌ›… «·Õ›Ÿ ≈–« ﬂ«‰  „ÿ·Ê»…
        }

        private async Task GetDataAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code_box.Text) || branch_box.SelectedValue == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· ﬂÊœ «·„ÊŸ› Ê «Œ Ì«— «·›—⁄", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(code_box.Text, out int employeeCode))
                {
                    LocalizationManager.ShowMessage("ﬂÊœ «·„ÊŸ› ÌÃ» √‰ ÌﬂÊ‰ —ﬁ„«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //  Õ„Ì· »Ì«‰«  «·„ÊŸ›
                var employee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == employeeCode && u.BranchId.ToString() == branch_box.SelectedValue.ToString());

                if (employee == null)
                {
                    LocalizationManager.ShowMessage("«·„ÊŸ› €Ì— „ÊÃÊœ √Ê ·Ì” ·œÌﬂ ’·«ÕÌ… «·Ê’Ê·", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    user_box.SelectedIndex = -1;
                    _deductions.Clear();
                    list.ItemsSource = _deductions;
                    return;
                }

                user_box.SelectedValue = employee.Code;

                //  ÕœÌœ «· Ê«—ÌŒ
                int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                int year = Convert.ToInt16(year_box.Text);

                DateTime startDate, endDate;

                if (from_picker.SelectedDate != null && to_picker.SelectedDate != null)
                {
                    startDate = from_picker.SelectedDate.Value;
                    endDate = to_picker.SelectedDate.Value;
                }
                else
                {
                    (startDate, endDate) = GetCustomMonthDates(monthNumber, year);
                }

                //  Õ„Ì· »Ì«‰«  «·«” ﬁÿ«⁄«  Ê«·«” Õﬁ«ﬁ« 
                var salaryOperations = await _context.Salaries
                    .Where(so => so.UserId == employeeCode &&
                                so.DayDate >= startDate &&
                                so.DayDate <= endDate)
                    .OrderBy(so => so.DayDate)
                    .ToListAsync();

                _deductions.Clear();
                int rowNumber = 1;

                foreach (var operation in salaryOperations)
                {
                    var deduction = new SalaryOperationViewModel
                    {
                        ID = operation.Id,
                        RowNumber = rowNumber++,
                        Value = operation.Amount.ToString(),
                        Text = operation.Notes,
                        Operation = _operations.ContainsKey(operation.Operation) ? _operations[operation.Operation] : "-",
                        Type = _types.ContainsKey(operation.Type) ? _types[operation.Type] : "-",
                        Date = operation.DayDate.ToShortDateString()
                    };
                    _deductions.Add(deduction);
                }

                list.ItemsSource = _deductions;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            await GetDataAsync();
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_deductions.Count == 0)
                {
                    LocalizationManager.ShowMessage("·«  ÊÃœ »Ì«‰«  · ’œÌ—Â«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // «Œ Ì«— „”«— «·„·›
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    DefaultExt = "xlsx",
                    AddExtension = true,
                    FileName = $"Benefits_Deductions_{DateTime.Now:yyyyMMdd_HHmmss}",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveFileDialog.ShowDialog() != true) return;
                string filePath = saveFileDialog.FileName;

                // ≈‰‘«¡ „” ‰œ Excel ÃœÌœ
                Application excelApp = new Application();
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;
                Workbook workbook = excelApp.Workbooks.Add();
                Worksheet worksheet = (Worksheet)workbook.Sheets[1];
                worksheet.Name = "«·«” ﬁÿ«⁄«  Ê«·«” Õﬁ«ﬁ« ";

                // ≈÷«›… «·⁄‰«ÊÌ‰ ≈·Ï „” ‰œ Excel
                worksheet.Cells[1, 1] = "—ﬁ„ «·”Ã·";
                worksheet.Cells[1, 2] = "«·ﬁÌ„…";
                worksheet.Cells[1, 3] = "«·‰’";
                worksheet.Cells[1, 4] = "«·⁄„·Ì…";
                worksheet.Cells[1, 5] = "«·‰Ê⁄";
                worksheet.Cells[1, 6] = "«· «—ÌŒ";

                // ≈÷«›… «·»Ì«‰«  ≈·Ï „” ‰œ Excel
                for (int i = 0; i < _deductions.Count; i++)
                {
                    var deduction = _deductions[i];
                    worksheet.Cells[i + 2, 1] = deduction.RowNumber;
                    worksheet.Cells[i + 2, 2] = deduction.Value;
                    worksheet.Cells[i + 2, 3] = deduction.Text;
                    worksheet.Cells[i + 2, 4] = deduction.Operation;
                    worksheet.Cells[i + 2, 5] = deduction.Type;
                    worksheet.Cells[i + 2, 6] = deduction.Date;
                }

                //  ‰”Ìﬁ «·ÃœÊ·
                Range range = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[_deductions.Count + 1, 6]];
                range.Borders.LineStyle = XlLineStyle.xlContinuous;
                range.Borders.Weight = XlBorderWeight.xlThin;

                //  ‰”Ìﬁ «·⁄‰«ÊÌ‰
                Range headerRange = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 6]];
                headerRange.Font.Bold = true;
                headerRange.Interior.Color = System.Drawing.Color.LightGray.ToArgb();

                // Õ›Ÿ „” ‰œ Excel
                workbook.SaveAs(filePath);
                workbook.Close();
                excelApp.Quit();

                //  Õ—Ì— «·„Ê«—œ
                System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

                LocalizationManager.ShowMessage($" „  ’œÌ— «·»Ì«‰«  »‰Ã«Õ ≈·Ï: {filePath}", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «· ’œÌ—: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(code_box.Text))
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· ﬂÊœ «·„ÊŸ› √Ê·«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(code_box.Text, out int employeeCode))
            {
                LocalizationManager.ShowMessage("ﬂÊœ «·„ÊŸ› ÌÃ» √‰ ÌﬂÊ‰ —ﬁ„«", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DeductionsWindow deductionsWindow = new DeductionsWindow(employeeCode, user_box.Text);
            deductionsWindow.Closed += async (s, args) => await GetDataAsync();
            deductionsWindow.ShowDialog();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeDictionaries();
            InitializeDateSelections();
        }
    }
}
