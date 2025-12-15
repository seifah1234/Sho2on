// AdminDashboard.xaml.cs
using HR_Application.Views;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HR_Application.Dashboard
{
    public partial class AdminDashboard : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private DispatcherTimer _timer;
        private string _currentDateTime;
        private string _userBranch;

        public event PropertyChangedEventHandler PropertyChanged;

        public SeriesCollection AttendanceSeries { get; set; }
        public SeriesCollection DepartmentSeries { get; set; }

        private ObservableCollection<string> _departmentLabels;
        public ObservableCollection<string> DepartmentLabels
        {
            get => _departmentLabels;
            set
            {
                _departmentLabels = value;
                OnPropertyChanged(nameof(DepartmentLabels));
            }
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                _currentDateTime = value;
                OnPropertyChanged(nameof(CurrentDateTime));
            }
        }

        public string UserBranch
        {
            get => _userBranch;
            set
            {
                _userBranch = value;
                OnPropertyChanged(nameof(UserBranch));
            }
        }

        public ObservableCollection<Alert> Alerts { get; set; }
        public ObservableCollection<Activity> RecentActivities { get; set; }

        public AdminDashboard()
        {
            InitializeComponent();
            DataContext = this;
            _context = new AppDbContext(App.ConnectionString);

            InitializeDashboard();
            LoadDashboardDataAsync();
            StartTimer();
        }

        private void InitializeDashboard()
        {
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser.FullName}";

            Alerts = new ObservableCollection<Alert>();
            RecentActivities = new ObservableCollection<Activity>();

            AlertsList.ItemsSource = Alerts;
            RecentActivityGrid.ItemsSource = RecentActivities;

            // Initialize charts
            AttendanceSeries = new SeriesCollection();
            DepartmentSeries = new SeriesCollection();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Load user branch
                var userBranch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.Id == App.CurrentUser.BranchId);
                UserBranch = userBranch?.Name ?? "غير محدد";

                // Load statistics
                await LoadStatistics();
                await LoadCharts();
                await LoadAlerts();
                await LoadRecentActivities();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStatistics()
        {
            // Total Employees
            var totalEmployees = await _context.Users
                .Where(u => !u.IsArchived)
                .CountAsync();
            TotalEmployees.Text = totalEmployees.ToString();

            // Today's Attendance
            var today = DateTime.Today;
            var todayAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.CheckInTime.HasValue &&
                           !a.IsAbsence)
                .CountAsync();
            TodayAttendance.Text = todayAttendance.ToString();

            // Today's Absence
            var todayAbsence = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.IsAbsence)
                .CountAsync();
            TodayAbsence.Text = todayAbsence.ToString();

            // Pending Salaries
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var pendingSalaries = await _context.Users
                .Where(u => !u.IsArchived)
                .CountAsync();
            PendingSalaries.Text = pendingSalaries.ToString();

            // Pending Leave Requests
            var pendingLeaves = await _context.Leaves
                .Where(l => l.Status == 0) // Pending status
                .CountAsync();
            PendingLeaves.Text = pendingLeaves.ToString();
        }

        private async Task LoadCharts()
        {
            // Attendance Pie Chart
            var today = DateTime.Today;
            var present = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.CheckInTime.HasValue &&
                           !a.IsAbsence)
                .CountAsync();
            var absent = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.IsAbsence)
                .CountAsync();
            var late = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                           a.Late.HasValue)
                .CountAsync();

            AttendanceSeries.Clear();
            AttendanceSeries.Add(new PieSeries
            {
                Title = "حاضر",
                Values = new ChartValues<int> { present },
                DataLabels = true,
                LabelPoint = point => $"{point.Y} ({point.Participation:P0})"
            });
            AttendanceSeries.Add(new PieSeries
            {
                Title = "غائب",
                Values = new ChartValues<int> { absent },
                DataLabels = true,
                LabelPoint = point => $"{point.Y} ({point.Participation:P0})"
            });
            AttendanceSeries.Add(new PieSeries
            {
                Title = "متأخر",
                Values = new ChartValues<int> { late },
                DataLabels = true,
                LabelPoint = point => $"{point.Y} ({point.Participation:P0})"
            });

            // Department Distribution Chart
            var departments = await _context.Departments.ToListAsync();
            DepartmentLabels = new ObservableCollection<string>(departments.Select(d => d.Name).ToList());

            var departmentCounts = new ChartValues<int>();
            foreach (var dept in departments)
            {
                var count = await _context.Users
                    .Where(u => u.DepartmentId == dept.Id && !u.IsArchived)
                    .CountAsync();
                departmentCounts.Add(count);
            }

            DepartmentSeries.Clear();
            DepartmentSeries.Add(new ColumnSeries
            {
                Title = "الموظفين",
                Values = departmentCounts,
                DataLabels = true
            });
        }

        private async Task LoadAlerts()
        {
            Alerts.Clear();

            // Check for expired documents
            var expiredDocs = await _context.Users
                .Where(u => u.NationalIDExpiration.HasValue &&
                           u.NationalIDExpiration.Value < DateOnly.FromDateTime(DateTime.Now.AddMonths(1)))
                .ToListAsync();

            foreach (var user in expiredDocs.Take(5))
            {
                Alerts.Add(new Alert
                {
                    Icon = "⚠️",
                    Message = $"رقم قومي منتهي للموظف {user.FullName}"
                });
            }

            // Check for pending approvals
            var pendingLoans = await _context.Loans
                .Where(l => l.Status == "SentToManager")
                .CountAsync();

            if (pendingLoans > 0)
            {
                Alerts.Add(new Alert
                {
                    Icon = "💰",
                    Message = $"{pendingLoans} طلب سلفة بانتظار الموافقة"
                });
            }

            // System alerts
            var totalUsers = await _context.Users.CountAsync();
            var today = DateTime.Today;
            var attendanceRate = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today)
                .Select(a => a.CheckInTime.HasValue)
                .CountAsync();

            if (attendanceRate < totalUsers * 0.8) // Less than 80% attendance
            {
                Alerts.Add(new Alert
                {
                    Icon = "📊",
                    Message = "معدل الحضور اليومي منخفض"
                });
            }
        }

        private async Task LoadRecentActivities()
        {
            RecentActivities.Clear();

            // Get recent salary payments
            var recentPayments = await _context.SalaryPayments
                .Include(sp => sp.User)
                .OrderByDescending(sp => sp.PaymentDate)
                .Take(10)
                .ToListAsync();

            foreach (var payment in recentPayments)
            {
                RecentActivities.Add(new Activity
                {
                    Date = payment.PaymentDate,
                    ActivityType = "صرف مرتب",
                    User = payment.User?.FullName ?? "غير معروف",
                    Details = $"مبلغ {payment.NetSalary} جنيه"
                });
            }

            // Get recent leaves
            var recentLeaves = await _context.Leaves
                .Include(l => l.User)
                .Include(l => l.LeaveType)
                .OrderByDescending(l => l.RequestDate)
                .Take(10)
                .ToListAsync();

            foreach (var leave in recentLeaves)
            {
                RecentActivities.Add(new Activity
                {
                    Date = leave.RequestDate,
                    ActivityType = "طلب إجازة",
                    User = leave.User?.FullName ?? "غير معروف",
                    Details = leave.LeaveType?.Name + (!string.IsNullOrEmpty(leave.Notes) ?" - " + leave.Notes : "")
                });
            }
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Quick Action Handlers
        private void OpenSalaryReport(object sender, RoutedEventArgs e)
        {
            var window = new SalaryReport();
            window.Show();
        }

        private void OpenEmployeeManagement(object sender, RoutedEventArgs e)
        {
            var window = new AddEmplo();
            window.Show();
        }

        private void OpenMonthlyAttendance(object sender, RoutedEventArgs e)
        {
            var window = new MonthlyData();
            window.Show();
        }

        private void OpenSalaryPayment(object sender, RoutedEventArgs e)
        {
            var window = new BulkSalaryPaymentWindow();
            window.Show();
        }

        private void OpenPermissions(object sender, RoutedEventArgs e)
        {
            var window = new Permissions();
            window.Show();
        }

        private void OpenBackup(object sender, RoutedEventArgs e)
        {
            MainWindow.CreateBackup(App.ConnectionString);
        }

    }

    public class Alert
    {
        public string Icon { get; set; }
        public string Message { get; set; }
    }

    public class Activity
    {
        public DateTime Date { get; set; }
        public string ActivityType { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
    }
}