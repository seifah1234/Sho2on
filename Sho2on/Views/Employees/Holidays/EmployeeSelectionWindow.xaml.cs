using Sho2on.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class EmployeeSelectionWindow : Window
    {
        public User SelectedUser { get; private set; }
        public string WindowTitle { get; set; } = "اختر موظف";
        public string SelectButtonText { get; set; } = "اختيار";

        public string? _searchCode;

        public EmployeeSelectionWindow(List<User> users, bool showManagersOnly = false, string title = "اختر موظف", string? searchCode = null)
        {
            InitializeComponent();

            WindowTitle = title;
            Title = title;
            txtTitle.Text = title;
            btnSelect.Content = SelectButtonText;
            _searchCode = searchCode;

            

            // فلترة المستخدمين إذا طُلب المديرين فقط
            if (showManagersOnly)
            {
                users = users.Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value).ToList();
            }

            dgEmployees.ItemsSource = users;

            // إضافة عمود بحث سريع
            SetupSearchFilter();

            if (users.Any())
            {
                dgEmployees.SelectedIndex = 0;
            }
            else
            {
                btnSelect.IsEnabled = false;
                txtNoResults.Visibility = Visibility.Visible;
            }
        }

        private void SetupSearchFilter()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(dgEmployees.ItemsSource);
            view.Filter = UserFilter;
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(txtSearch.Text))
                return true;

            var user = item as User;
            if (user == null)
                return false;

            string searchText = txtSearch.Text.ToLower();

            return user.FullName.ToLower().Contains(searchText) ||
                   user.Code.ToString().StartsWith(searchText) ||
                   (user.JobTitle?.Name ?? "").ToLower().Contains(searchText) ||
                   (user.Department?.Name ?? "").ToLower().Contains(searchText);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(dgEmployees.ItemsSource).Refresh();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            SelectedUser = dgEmployees.SelectedItem as User;
            if (SelectedUser != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار موظف من القائمة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void dgEmployees_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgEmployees.SelectedItem != null)
            {
                btnSelect_Click(sender, e);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_searchCode != null)
            {
                txtSearch.Text = _searchCode;
            }
        }
    }

    public class BoolToActiveStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "نشط" : "غير نشط";
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}