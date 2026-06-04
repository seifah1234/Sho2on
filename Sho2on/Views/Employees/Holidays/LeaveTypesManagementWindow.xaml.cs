using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class LeaveTypesManagementWindow : Window
    {
        private readonly AppDbContext _context;

        public LeaveTypesManagementWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadLeaveTypes();
        }

        private async void LoadLeaveTypes()
        {
            try
            {
                var leaveTypes = await _context.LeaveTypes
                    .OrderBy(lt => lt.Name)
                    .ToListAsync();

                dgLeaveTypes.ItemsSource = leaveTypes;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· √‰Ê«⁄ «·≈Ã«“« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new LeaveTypeEditWindow();
            editWindow.Owner = this;

            if (editWindow.ShowDialog() == true)
            {
                LoadLeaveTypes();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null && button.Tag is int leaveTypeId)
            {
                var editWindow = new LeaveTypeEditWindow(leaveTypeId);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    LoadLeaveTypes();
                }
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null && button.Tag is int leaveTypeId)
            {
                var result = LocalizationManager.ShowMessage("Â· √‰  „ √ﬂœ „‰ Õ–› Â–« «·‰Ê⁄ „‰ «·≈Ã«“« ø",
                    " √ﬂÌœ «·Õ–›", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                        if (leaveType != null)
                        {
                            // «· Õﬁﬁ ≈–« ﬂ«‰ «·‰Ê⁄ „” Œœ„ ›Ì ÿ·»«  ≈Ã«“…
                            var isUsed = await _context.Leaves.AnyAsync(l => l.LeaveTypeId == leaveTypeId);

                            if (isUsed)
                            {
                                LocalizationManager.ShowMessage("·« Ì„ﬂ‰ Õ–› Â–« «·‰Ê⁄ ·√‰Â „” Œœ„ ›Ì ÿ·»«  ≈Ã«“…",
                                    " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            _context.LeaveTypes.Remove(leaveType);
                            await _context.SaveChangesAsync();

                            LoadLeaveTypes();
                            LocalizationManager.ShowMessage(" „ Õ–› ‰Ê⁄ «·≈Ã«“… »‰Ã«Õ", "‰Ã«Õ",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·Õ–›: {ex.Message}", "Œÿ√",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
