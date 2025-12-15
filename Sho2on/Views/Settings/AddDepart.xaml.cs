using Sho2on.Database;
using System.Linq;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class AddDepart : Window
    {
        private AppDbContext _context;
        private Department _selectedDepartment;

        public AddDepart()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private void LoadData()
        {
            list.ItemsSource = _context.Departments.OrderBy(d => d.Name).ToList();
            name_box.Clear();
            _selectedDepartment = null;
        }

        private async void save_Btn(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    MessageBox.Show("أدخل اسم الإدارة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var department = new Department
                {
                    Name = name_box.Text.Trim()
                };

                await _context.Departments.AddAsync(department);
                await _context.SaveChangesAsync();
                MessageBox.Show("تم إضافة الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, EventArgs e)
        {
            if (_selectedDepartment == null)
            {
                MessageBox.Show("لم يتم اختيار الإدارة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _context.Departments.Remove(_selectedDepartment);
                await _context.SaveChangesAsync();
                MessageBox.Show("تم حذف الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, EventArgs e)
        {
            if (_selectedDepartment == null)
            {
                MessageBox.Show("لم تختار أي إدارة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _selectedDepartment.Name = name_box.Text.Trim();
                _selectedDepartment.EditedAt = DateTime.Now;
                _context.Departments.Update(_selectedDepartment);
                await _context.SaveChangesAsync();
                MessageBox.Show("تم تعديل الإدارة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Department selected)
            {
                _selectedDepartment = selected;
                name_box.Text = selected.Name;
            }
        }

        private void exit_Btn(object sender, EventArgs e) => Close();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
