using HR_Application.Helpers;
using HR_Application.Helpers;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Office.Interop.Excel;
using Sho2on.Database;
using Sho2on.Database.Models; // عدل المسار حسب مكان موديل Shift
using System; 
using System.Linq;
using System.Threading.Tasks;
using System.Windows; 
using System.Windows.Controls;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Window = System.Windows.Window;

namespace HR_Application
{
    public partial class AddShift : Window
    {
        private AppDbContext _context;
        private Shift _selectedShift;
        private List<Shift> _shifts;

        public AddShift()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }



        private async Task LoadData()
        {
            _selectedShift = null;
            _shifts = await _context.Shifts
                                       .OrderBy(s => s.StartTime)
                                       .ToListAsync();
            list.ItemsSource = _shifts;
            fromTimePicker.SelectedTime = null;
            toTimePicker.SelectedTime = null;
            editBtn.Visibility = Visibility.Collapsed;
            deleteBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fromTimePicker.SelectedTime == null || toTimePicker.SelectedTime == null)
                {
                    LocalizationManager.ShowMessage("من فضلك اختر الوقت من وإلى", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
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


                if (_shifts.FirstOrDefault(a => a.Name == shift.Name) != null)
                {
                    LocalizationManager.ShowMessage("هذه الوردية موجودة بالفعل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _context.Shifts.AddAsync(shift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم إضافة الوردية", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                LocalizationManager.ShowMessage("لم تختار أي وردية", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (fromTimePicker.SelectedTime == null || toTimePicker.SelectedTime == null)
                {
                    LocalizationManager.ShowMessage("من فضلك اختر الوقت من وإلى", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _selectedShift.StartTime = fromTimePicker.SelectedTime.Value.TimeOfDay;
                _selectedShift.EndTime = toTimePicker.SelectedTime.Value.TimeOfDay;
                _selectedShift.Name = $"{_selectedShift.StartTime:hh\\:mm} - {_selectedShift.EndTime:hh\\:mm}";
                _selectedShift.EditedAt = DateTime.Now;

                _context.Shifts.Update(_selectedShift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم تعديل الوردية", "", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedShift == null)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار الوردية", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _context.Shifts.Remove(_selectedShift);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم حذف الوردية", LocalizationManager.Translate("نجح"), MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Shift selected)
            {
                _selectedShift = selected;
                fromTimePicker.SelectedTime = DateTime.Today.Add(selected.StartTime);
                toTimePicker.SelectedTime = DateTime.Today.Add(selected.EndTime);
                editBtn.Visibility = Visibility.Visible;
                deleteBtn.Visibility = Visibility.Visible;
                saveBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void exit_Btn(object sender, RoutedEventArgs e) => Close();
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {

            fromTimePicker.SelectedTime = null;
            toTimePicker.SelectedTime = null;
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

