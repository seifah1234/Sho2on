using Sho2on.Database;
using Sho2on.Database.Models;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class AddQualification : Window
    {
        private AppDbContext _context;
        private Qualification _selectedQualification;

        public AddQualification()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private void LoadData()
        {
            list.ItemsSource = _context.Qualifications.OrderBy(d => d.Name).ToList();
            name_box.Clear();
            _selectedQualification = null;
        }

        private async void save_Btn(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("ÃÏÎá ÇÓã ÇáãÄåá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var qualification = new Qualification
                {
                    Name = name_box.Text.Trim(),

                };

                await _context.Qualifications.AddAsync(qualification);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÅÖÇÝÉ ÇáãÄåá", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, EventArgs e)
        {
            if (_selectedQualification == null)
            {
                LocalizationManager.ShowMessage("áã íÊã ÇÎÊíÇÑ ÇáãÄåá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _context.Qualifications.Remove(_selectedQualification);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÍÐÝ ÇáãÄåá", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, EventArgs e)
        {
            if (_selectedQualification == null)
            {
                LocalizationManager.ShowMessage("áã ÊÎÊÇÑ Ãí ãÄåá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _selectedQualification.Name = name_box.Text.Trim();
                _context.Qualifications.Update(_selectedQualification);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÊÚÏíá ÇáãÄåá", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Qualification selected)
            {
                _selectedQualification = selected;
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

