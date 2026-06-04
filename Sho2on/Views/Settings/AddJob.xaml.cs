using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class AddJob : Window
    {
        private AppDbContext _context;

        public AddJob()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                name_box.Clear();
                var jobs = await _context.JobTitles
                                         .OrderBy(j => j.Id)
                                         .ToListAsync();
                list.ItemsSource = jobs;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage(ex.Message, "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("ÃÏÎá ÇÓã ÇáæÙíÝÉ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var job = new JobTitle
                {
                    Name = name_box.Text.Trim(),
                    IsDriver = isDriver_box.IsChecked,
                    IsManager = isManager_box.IsChecked,
                    IsHR = isHR_box.IsChecked,
                };

                await _context.JobTitles.AddAsync(job);
                await _context.SaveChangesAsync();


                LocalizationManager.ShowMessage("Êã ÅÖÇÝÉ ÇáæÙíÝÉ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ ÃËäÇÁ ÇáÍÝÙ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem is not JobTitle selectedJob)
            {
                LocalizationManager.ShowMessage("áã íÊã ÇÎÊíÇÑ ÇáæÙíÝÉ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _context.JobTitles.Remove(selectedJob);
                await _context.SaveChangesAsync();
                LocalizationManager.ShowMessage("Êã ÍÐÝ ÇáæÙíÝÉ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ ÃËäÇÁ ÇáÍÐÝ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem is not JobTitle selectedJob)
            {
                LocalizationManager.ShowMessage("áã ÊÎÊÇÑ Ãí æÙíÝÉ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                selectedJob.Name = name_box.Text.Trim();
                selectedJob.IsDriver = isDriver_box.IsChecked;
                selectedJob.IsManager = isManager_box.IsChecked;
                selectedJob.IsHR = isHR_box.IsChecked;
                selectedJob.EditedAt = DateTime.Now;
                _context.JobTitles.Update(selectedJob);
                await _context.SaveChangesAsync();


                LocalizationManager.ShowMessage("Êã ÊÚÏíá ÇáæÙíÝÉ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ ÃËäÇÁ ÇáÊÚÏíá", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is JobTitle selectedJob)
            {
                name_box.Text = selectedJob.Name;
                isDriver_box.IsChecked = selectedJob.IsDriver;
                isManager_box.IsChecked = selectedJob.IsManager;
                isHR_box.IsChecked = selectedJob.IsHR;
                editBtn.Visibility = Visibility.Visible;
                saveBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void exit_Btn(object sender, RoutedEventArgs e) => Close();

        private void Min_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            list.SelectedIndex = -1;
            name_box.Clear();
            isDriver_box.IsChecked = false;
            isManager_box.IsChecked = false;
            isHR_box.IsChecked = false;
            editBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }
    }
}

