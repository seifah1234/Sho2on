using HR_Application.Views.Employees.Holidays;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using HR_Application.Helpers;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Dashboard
{
    /// <summary>
    /// Interaction logic for ManagerDashboard.xaml
    /// </summary>
    public partial class ManagerDashboard : System.Windows.Controls.UserControl
    {
        private readonly AppDbContext _context;
        private readonly int _currentUserId;

        public ManagerDashboard()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _currentUserId = App.CurrentUser?.Id ?? 0;

            InitializeDashboard();
            LoadDashboardDataAsync();
        }

        private void InitializeDashboard()
        {
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser?.FullName ?? "المدير"}";
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                await LoadTeamStatistics();
                await LoadTeamData();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTeamStatistics()
        {
            // فريق العمل تحت إدارتي
            var teamCount = await _context.Users
                .Where(u => u.BranchId == App.CurrentUser.BranchId &&
                           u.DepartmentId == App.CurrentUser.DepartmentId &&
                           !u.IsArchived)
                .CountAsync();
            TeamCount.Text = teamCount.ToString();

            // حساب الإنتاجية
            var today = DateTime.Today;
            var teamAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.User.BranchId == App.CurrentUser.BranchId &&
                           a.User.DepartmentId == App.CurrentUser.DepartmentId &&
                           a.CheckInTime.HasValue)
                .CountAsync();

            var attendanceRate = teamCount > 0 ? (teamAttendance * 100.0 / teamCount) : 0;
            Productivity.Text = $"{attendanceRate:F0}%";

            // المهام المستحقة
            var pendingTasks = await GetPendingTasksCount();
            PendingTasks.Text = pendingTasks.ToString();
        }

        private async Task<int> GetPendingTasksCount()
        {
            // يمكن إضافة منطق خاص بالمهام هنا
            // هذا مثال بسيط يعتمد على تقارير الحضور المتأخرة
            var today = DateTime.Today;
            var lateEmployees = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.User.BranchId == App.CurrentUser.BranchId &&
                           a.User.DepartmentId == App.CurrentUser.DepartmentId &&
                           a.Late.HasValue &&
                           a.Late.Value.TotalMinutes > 30)
                .CountAsync();

            return lateEmployees;
        }

        private async Task LoadTeamData()
        {
            var teamMembers = await _context.Users
                .Include(u => u.JobTitle)
                .Where(u => u.BranchId == App.CurrentUser.BranchId &&
                           u.DepartmentId == App.CurrentUser.DepartmentId &&
                           !u.IsArchived)
                .ToListAsync();

            var teamData = new ObservableCollection<TeamMember>();
            var today = DateTime.Today;

            foreach (var member in teamMembers)
            {
                var todayAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == member.Id &&
                                              a.AttendanceDate.Date == today);

                var memberData = new TeamMember
                {
                    Name = member.FullName,
                    Job = member.JobTitle?.Name ?? LocalizationManager.Translate("غير محدد"),
                    TodayAttendance = todayAttendance != null ?
                        (todayAttendance.CheckInTime.HasValue ? LocalizationManager.Translate("حاضر") : LocalizationManager.Translate("غائب")) : LocalizationManager.Translate("لم يسجل"),
                    TaskStatus = LocalizationManager.Translate("مستوى الأداء: جيد") // يمكن جلب هذا من تقييمات الموظفين
                };

                teamData.Add(memberData);
            }

            TeamGrid.ItemsSource = teamData;
        }

        // Quick Action Handlers
        private void OpenAttendanceReport(object sender, RoutedEventArgs e)
        {
            var window = new EmployeeMonthReport();
            window.Show();
        }

        private void OpenProductivityReport(object sender, RoutedEventArgs e)
        {
            // يمكن إنشاء نافذة تقرير إنتاجية خاصة
            LocalizationManager.ShowMessage("سيتم تطوير تقرير الإنتاجية في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenTasksReport(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("سيتم تطوير تقرير المهام في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenTeamManagement(object sender, RoutedEventArgs e)
        {
            var window = new EmployeeData();
            window.Show();
        }

        private void OpenEvaluations(object sender, RoutedEventArgs e)
        {
            // يمكن إنشاء نافذة تقييمات الموظفين
            LocalizationManager.ShowMessage("سيتم تطوير تقييمات الموظفين في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenLeaveRequests(object sender, RoutedEventArgs e)
        {
            var window = new LeaveManagementWindow();
            window.Show();
        }
    }

    public class TeamMember
    {
        public string Name { get; set; }
        public string Job { get; set; }
        public string TodayAttendance { get; set; }
        public string TaskStatus { get; set; }
    }
}

