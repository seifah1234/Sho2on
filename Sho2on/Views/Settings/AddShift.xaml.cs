using Sho2on.Database.Models; // عدل المسار حسب مكان موديل Shift
using Sho2on.Database;
using System;
using System.Linq;
using System.Windows;
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
            FromTimePicker.SelectedDateTime = null;
            ToTimePicker.SelectedDateTime = null;
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromTimePicker.SelectedDateTime == null || ToTimePicker.SelectedDateTime == null)
                {
                    MessageBox.Show("من فضلك اختر الوقت من وإلى", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TimeSpan fromTime = FromTimePicker.SelectedDateTime.Value.TimeOfDay;
                TimeSpan toTime = ToTimePicker.SelectedDateTime.Value.TimeOfDay;

                var shift = new Shift
                {
                    Name = $"{fromTime:hh\\:mm} - {toTime:hh\\:mm}",
                    StartTime = fromTime,
                    EndTime = toTime,
                    EditedAt = DateTime.Now
                };

                await _context.Shifts.AddAsync(shift);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم إضافة الوردية", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                MessageBox.Show("لم تختار أي وردية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (FromTimePicker.SelectedDateTime == null || ToTimePicker.SelectedDateTime == null)
                {
                    MessageBox.Show("من فضلك اختر الوقت من وإلى", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedShift.StartTime = FromTimePicker.SelectedDateTime.Value.TimeOfDay;
                _selectedShift.EndTime = ToTimePicker.SelectedDateTime.Value.TimeOfDay;
                _selectedShift.Name = $"{_selectedShift.StartTime:hh\\:mm} - {_selectedShift.EndTime:hh\\:mm}";
                _selectedShift.EditedAt = DateTime.Now;

                _context.Shifts.Update(_selectedShift);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم تعديل الوردية", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                MessageBox.Show("لم يتم اختيار الوردية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _context.Shifts.Remove(_selectedShift);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حذف الوردية", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch
            {
                MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Shift selected)
            {
                _selectedShift = selected;
                FromTimePicker.SelectedDateTime = DateTime.Today.Add(selected.StartTime);
                ToTimePicker.SelectedDateTime = DateTime.Today.Add(selected.EndTime);
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
