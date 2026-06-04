using MaterialDesignThemes.Wpf;
using Sho2on.Database;
using Sho2on.Database.Models; // ÚÏá ÇáãÓÇÑ ÍÓÈ ãßÇä ãæÏíá Shift
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class AddShift : Window
    {
        private AppDbContext _context;
        private Shift _selectedShift;

        public AddShift()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }



        private void LoadData()
        {
            _selectedShift = null;
            list.ItemsSource = _context.Shifts
                                       .OrderBy(s => s.StartTime)
                                       .ToList();
            fromTimePicker.SelectedTime = null;
            toTimePicker.SelectedTime = null;
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fromTimePicker.SelectedTime == null || toTimePicker.SelectedTime == null)
                {
                    LocalizationManager.ShowMessage("ãä ÝÖáß ÇÎÊÑ ÇáæÞÊ ãä æÅáì", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TimeSpan fromTime = fromTimePicker.SelectedTime.Value.TimeOfDay;
                TimeSpan toTime = toTimePicker.SelectedTime.Value.TimeOfDay;

                var shift = new Shift
                {
                    Name = $"{fromTime:hh\\:mm} - {toTime:hh\\:mm}",
                    StartTime = fromTime,
                    EndTime = toTime,
                    EditedAt = DateTime.Now
                };

                await _context.Shifts.AddAsync(shift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("Êã ÅÖÇÝÉ ÇáæÑÏíÉ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                LocalizationManager.ShowMessage("áã ÊÎÊÇÑ Ãí æÑÏíÉ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (fromTimePicker.SelectedTime == null || toTimePicker.SelectedTime == null)
                {
                    LocalizationManager.ShowMessage("ãä ÝÖáß ÇÎÊÑ ÇáæÞÊ ãä æÅáì", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedShift.StartTime = fromTimePicker.SelectedTime.Value.TimeOfDay;
                _selectedShift.EndTime = toTimePicker.SelectedTime.Value.TimeOfDay;
                _selectedShift.Name = $"{_selectedShift.StartTime:hh\\:mm} - {_selectedShift.EndTime:hh\\:mm}";
                _selectedShift.EditedAt = DateTime.Now;

                _context.Shifts.Update(_selectedShift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("Êã ÊÚÏíá ÇáæÑÏíÉ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                LocalizationManager.ShowMessage("áã íÊã ÇÎÊíÇÑ ÇáæÑÏíÉ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _context.Shifts.Remove(_selectedShift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("Êã ÍÐÝ ÇáæÑÏíÉ", "äÌÍ", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("ÍÏË ÎØÃ", "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Shift selected)
            {
                _selectedShift = selected;
                fromTimePicker.SelectedTime = DateTime.Today.Add(selected.StartTime);
                toTimePicker.SelectedTime = DateTime.Today.Add(selected.EndTime);
            }
        }

        private void exit_Btn(object sender, RoutedEventArgs e) => Close();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

    }
}

