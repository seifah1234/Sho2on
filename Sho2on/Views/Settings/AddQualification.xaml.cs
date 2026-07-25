using Sho2on.Database;
using Sho2on.Database.Models;
using System.Linq;
using System.Windows; 
using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HR_Application
{
    public partial class AddQualification : Window
    {
        private AppDbContext _context;
        private Qualification? _selectedQualification;
        private List<Qualification> _qualifications;

        public AddQualification()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }

        private async Task LoadData()
        {
            try
            {

                _qualifications = await _context.Qualifications.OrderBy(d => d.Name).ToListAsync();
                list.ItemsSource = _qualifications;
                name_box.Clear();
                _selectedQualification = null;
                editBtn.Visibility = Visibility.Collapsed;
                deleteBtn.Visibility = Visibility.Collapsed;
                saveBtn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void save_Btn(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("أدخل اسم المؤهل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var qualification = new Qualification
                {
                    Name = name_box.Text.Trim(),

                };

                if (_qualifications.FirstOrDefault(q => q.Name == qualification.Name) == null)
                {

                    await _context.Qualifications.AddAsync(qualification);
                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم إضافة المؤهل", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
                }else
                {
                    LocalizationManager.ShowMessage("هذا المؤهل موجود بالفعل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, EventArgs e)
        {
            if (_selectedQualification == null)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار المؤهل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _context.Qualifications.Remove(_selectedQualification);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم حذف المؤهل", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, EventArgs e)
        {
            if (_selectedQualification == null)
            {
                LocalizationManager.ShowMessage("لم تختار أي مؤهل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _selectedQualification.Name = name_box.Text.Trim();
                _context.Qualifications.Update(_selectedQualification);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("تم تعديل المؤهل", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Qualification selected)
            {
                _selectedQualification = selected;
                name_box.Text = selected.Name;
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
            _selectedQualification = null;
            name_box.Clear();
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

