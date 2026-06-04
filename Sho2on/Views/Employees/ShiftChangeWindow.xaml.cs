using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for ShiftChangeWindow.xaml
    /// </summary>
    public partial class ShiftChangeWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        List<string> shifts = new List<string>();
        DateTime date;
        string code;
        public event Action<string> ShiftChanged;

        public ShiftChangeWindow(string currentShift, DateTime _date, string code)
        {
            InitializeComponent();
            LoadShifts();
            date = _date;
            shift_box.ItemsSource = shifts;
            shift_box.Text = currentShift;
            this.code = code;
        }

        private async void LoadShifts()
        {
            try
            {
                var dbShifts = await _context.Shifts
                    .Select(s => s.Name)
                    .ToListAsync();

                shifts.Clear();
                shifts.AddRange(dbShifts);

                // Refresh the ComboBox items source
                shift_box.ItemsSource = null;
                shift_box.ItemsSource = shifts;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·Ê—œÌ« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void shift_box_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    string newShift = shift_box.Text;
                    if (!string.IsNullOrEmpty(newShift))
                    {
                        // Check if the shift exists in the database
                        var shiftExists = await _context.Shifts
                            .AnyAsync(s => s.Name == newShift);

                        if (!shiftExists)
                        {
                            LocalizationManager.ShowMessage("Â–Â «·Ê—œÌ… €Ì— „ÊÃÊœ… ›Ì ﬁ«⁄œ… «·»Ì«‰« ", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Update the attendance record with the new shift
                        await UpdateAttendanceShift(newShift);

                        ShiftChanged?.Invoke(newShift); // Trigger event with new shift name
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task UpdateAttendanceShift(string newShift)
        {
            try
            {
                // Find the attendance record
                var attendance = await _context.Attendances
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.User.Id.ToString() == code && a.AttendanceDate == date.Date);

                if (attendance != null)
                {
                    // Get the new shift details
                    var shift = await _context.Shifts
                        .FirstOrDefaultAsync(s => s.Name == newShift);

                    if (shift != null)
                    {
                        // Update the shift information
                        attendance.ShiftId = shift.Id;

                        // You might want to update other shift-related fields if they exist in your Attendance model
                        // For example:
                        // attendance.ShiftFrom = shift.StartTime;
                        // attendance.ShiftTo = shift.EndTime;

                        await _context.SaveChangesAsync();
                        LocalizationManager.ShowMessage(" „  €ÌÌ— «·Ê—œÌ… »‰Ã«Õ", " „", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ”Ã· «·Õ÷Ê—", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  ÕœÌÀ «·Ê—œÌ…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newShift = shift_box.Text;
                if (!string.IsNullOrEmpty(newShift))
                {
                    // Check if the shift exists in the database
                    var shiftExists = await _context.Shifts
                        .AnyAsync(s => s.Name == newShift);

                    if (!shiftExists)
                    {
                        LocalizationManager.ShowMessage("Â–Â «·Ê—œÌ… €Ì— „ÊÃÊœ… ›Ì ﬁ«⁄œ… «·»Ì«‰« ", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Update the attendance record with the new shift
                    await UpdateAttendanceShift(newShift);

                    ShiftChanged?.Invoke(newShift); // Trigger event with new shift name
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}
