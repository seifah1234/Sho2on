using Sho2on.Database;
using System.Linq;
using System.Windows; 
using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HR_Application
{
    public partial class AddDepart : Window
    {
        private AppDbContext _context;
        private Department? _selectedDepartment;
        private List<Department> _departments;

        public AddDepart()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }

        private async Task LoadData()
        {
            _departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            list.ItemsSource = _departments;
            name_box.Clear();
            _selectedDepartment = null;
            editBtn.Visibility = Visibility.Collapsed;
            deleteBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }

        private async void save_Btn(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("أدخل اسم الإدارة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var department = new Department
                {
                    Name = name_box.Text.Trim(),
                    IsHR = isHR_box.IsChecked

                };

                if (_departments.FirstOrDefault(d => d.Name == name_box.Text.Trim()) != null)
                {
                    LocalizationManager.ShowMessage("هذه الادارة موجودة بالفعل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _context.Departments.AddAsync(department);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم إضافة الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, EventArgs e)
        {
            if (_selectedDepartment == null)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار الإدارة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _context.Departments.Remove(_selectedDepartment);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم حذف الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, EventArgs e)
        {
            if (_selectedDepartment == null)
            {
                LocalizationManager.ShowMessage("لم تختار أي إدارة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _selectedDepartment.Name = name_box.Text.Trim();
                _selectedDepartment.IsHR = isHR_box.IsChecked;
                _selectedDepartment.EditedAt = DateTime.Now;
                _context.Departments.Update(_selectedDepartment);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم تعديل الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Department selected)
            {
                _selectedDepartment = selected;
                name_box.Text = selected.Name;
                isHR_box.IsChecked = selected.IsHR;
                editBtn.Visibility = Visibility.Visible;
                deleteBtn.Visibility = Visibility.Visible;
                saveBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void exit_Btn(object sender, EventArgs e) => Close();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            _selectedDepartment = null;
            name_box.Clear();
            isHR_box.IsChecked = false;
            editBtn.Visibility = Visibility.Collapsed;
            deleteBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
            
        }
    }
}

