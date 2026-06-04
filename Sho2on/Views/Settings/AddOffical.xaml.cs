using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class AddOffical : Window
    {
        private AppDbContext _context;
        private Offical _selectedOffical;
        private List<User> users = new List<User>();

        public AddOffical()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private async Task LoadData()
        {
            list.ItemsSource = await _context.Officals.Include(o => o.User).OrderBy(d => d.Name).ToListAsync();
            name_box.Clear();
            user_box.SelectedIndex = -1;
            code_box.Clear();
            _selectedOffical = null;


            var dbUsers = await _context.Users.ToListAsync();

            users.AddRange(dbUsers);
            user_box.ItemsSource = users;
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (user_box.SelectedItem != null)
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


        private async void save_Btn(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("ÃÏÎá ÇáãÓãì", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Code == code_box.Text);
                if (user == null)
                {
                    LocalizationManager.ShowMessage("ÇáÑÌÇÁ ÇÎÊíÇÑ ÇáãæÙÝ");
                    return;
                }

                var offical = new Offical
                {
                    Name = name_box.Text.Trim(),
                    UserId = user.Id,

                };

                await _context.Officals.AddAsync(offical);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÅÖÇÝÉ ÇáãÓÄæá", "Êã", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, EventArgs e)
        {
            if (_selectedOffical == null)
            {
                LocalizationManager.ShowMessage("áã íÊã ÇÎÊíÇÑ ÇáãÓÄæá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }



            try
            {
                _context.Officals.Remove(_selectedOffical);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÍÐÝ ÇáãÓÄæá", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, EventArgs e)
        {
            if (_selectedOffical == null)
            {
                LocalizationManager.ShowMessage("áã ÊÎÊÇÑ Ãí ãÓÄæá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (user_box.SelectedItem is not User user)
            {
                LocalizationManager.ShowMessage("ÇáÑÌÇÁ ÇÎÊíÇÑ ÇáãæÙÝ");
                return;
            }

            try
            {
                _selectedOffical.Name = name_box.Text.Trim();
                _selectedOffical.UserId = user.Id;

                _context.Officals.Update(_selectedOffical);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÊÚÏíá ÇáãÓÄæá", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Offical selected)
            {
                _selectedOffical = selected;
                name_box.Text = selected.Name;
                user_box.SelectedValue = selected.User.Code;
                code_box.Text = selected.User.Code;
            }
        }

        private void exit_Btn(object sender, EventArgs e) => Close();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void code_box_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                e.Handled = true;
                user_box.SelectedValue = code_box.Text;
            }
        }
    }
}

