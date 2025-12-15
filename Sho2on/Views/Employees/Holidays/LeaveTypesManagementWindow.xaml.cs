using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Linq;
using System.Windows;
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
                MessageBox.Show($"خطأ في تحميل أنواع الإجازات: {ex.Message}", "خطأ",
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
                var result = MessageBox.Show("هل أنت متأكد من حذف هذا النوع من الإجازات؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                        if (leaveType != null)
                        {
                            // التحقق إذا كان النوع مستخدم في طلبات إجازة
                            var isUsed = await _context.Leaves.AnyAsync(l => l.LeaveTypeId == leaveTypeId);

                            if (isUsed)
                            {
                                MessageBox.Show("لا يمكن حذف هذا النوع لأنه مستخدم في طلبات إجازة",
                                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            _context.LeaveTypes.Remove(leaveType);
                            await _context.SaveChangesAsync();

                            LoadLeaveTypes();
                            MessageBox.Show("تم حذف نوع الإجازة بنجاح", "نجاح",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ",
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