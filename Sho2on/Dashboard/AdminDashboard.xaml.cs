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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Dashboard
{
    // ─────────────────────────────────────────────────────────────
    // Branch card ViewModel
    // ─────────────────────────────────────────────────────────────
    public class SectionCardViewModel
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalEmployees { get; set; }
        public int TodayPresent { get; set; }
        public int TodayAbsent { get; set; }
        public int PendingLeaves { get; set; }

        // Attendance rate (0–1)
        public double AttendanceRate =>
            TotalEmployees > 0 ? (double)TodayPresent / TotalEmployees : 0;

        public string AttendanceRateText =>
            $"نسبة الحضور: {AttendanceRate:P0}";

        // Bar width in px (max 178 = card 210 – 2×16 padding)
        public double AttendanceBarWidth =>
            Math.Min(AttendanceRate * 178, 178);

        // Each card gets a colour from the palette
        public string GradientFrom { get; set; }
        public string GradientTo { get; set; }
        public Brush AccentBrush { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // Dashboard UserControl
    // ─────────────────────────────────────────────────────────────
    public partial class AdminDashboard : UserControl, INotifyPropertyChanged
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
            set { _departmentLabels = value; OnPropertyChanged(nameof(DepartmentLabels)); }
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; OnPropertyChanged(nameof(CurrentDateTime)); }
        }

        public string UserBranch
        {
            get => _userBranch;
            set { _userBranch = value; OnPropertyChanged(nameof(UserBranch)); }
        }

        public ObservableCollection<Alert> Alerts { get; set; }
        public ObservableCollection<Activity> RecentActivities { get; set; }

        // Branch card colour palette
        private static readonly (string From, string To, string Accent)[] _palette =
        {
            ("#003843", "#00838F", "#00BCD4"),
            ("#1B3A1C", "#2E7D32", "#66BB6A"),
            ("#3B1010", "#B71C1C", "#EF5350"),
            ("#3A1F00", "#E65100", "#FFA726"),
            ("#2A0A3A", "#6A1B9A", "#AB47BC"),
            ("#0D2137", "#0D47A1", "#42A5F5"),
            ("#1A2A00", "#33691E", "#8BC34A"),
            ("#2A1A00", "#F57F17", "#FFCA28"),
        };

        public AdminDashboard()
        {
            InitializeComponent();
            DataContext = this;
            _context = new AppDbContext(App.ConnectionString);

            InitializeDashboard();
            _ = LoadDashboardDataAsync();
            StartTimer();
        }

        // ── Init ──────────────────────────────────────────────────
        private void InitializeDashboard()
        {
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser.FullName}";

            Alerts = new ObservableCollection<Alert>();
            RecentActivities = new ObservableCollection<Activity>();

            AlertsList.ItemsSource = Alerts;

            AttendanceSeries = new SeriesCollection();
            DepartmentSeries = new SeriesCollection();
        }

        // ── Data loading ──────────────────────────────────────────
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var userBranch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.Id == App.CurrentUser.BranchId);
                UserBranch = userBranch?.Name ?? "غير محدد";

                await LoadStatistics();
                await LoadSectionCards();
                await LoadCharts();
                await LoadAlerts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStatistics()
        {
            var today = DateTime.Today;

            TotalEmployees.Text = (await _context.Users
                .Where(u => !u.IsArchived).CountAsync()).ToString();

            TodayAttendance.Text = (await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.CheckInTime.HasValue && !a.IsAbsence)
                .CountAsync()).ToString();

            TodayAbsence.Text = (await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today && a.IsAbsence)
                .CountAsync()).ToString();

            PendingSalaries.Text = (await _context.Users
                .Where(u => !u.IsArchived).CountAsync()).ToString();

            PendingLeaves.Text = (await _context.Leaves
                .Where(l => l.Status == 0).CountAsync()).ToString();
        }

        /// <summary>Load per-branch summary cards.</summary>
        private async Task LoadSectionCards()
        {
            var today = DateTime.Today;
            var sections = await _context.Degrees.OrderBy(b => b.Name).ToListAsync();

            BranchCountLabel.Text = sections.Count.ToString();

            var cards = new System.Collections.Generic.List<SectionCardViewModel>();

            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                var palette = _palette[i % _palette.Length];

                var totalEmp = await _context.Users
                    .Where(u => !u.IsArchived && u.DegreeId == section.Id)
                    .CountAsync();

                var present = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == today &&
                                a.CheckInTime.HasValue && !a.IsAbsence &&
                                a.User.DegreeId == section.Id)
                    .CountAsync();

                var absent = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == today &&
                                a.IsAbsence &&
                                a.User.DegreeId == section.Id)
                    .CountAsync();

                var pendingLeaves = await _context.Leaves
                    .Where(l => l.Status == 0 && l.User.DegreeId == section.Id)
                    .CountAsync();

                cards.Add(new SectionCardViewModel
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    TotalEmployees = totalEmp,
                    TodayPresent = present,
                    TodayAbsent = absent,
                    PendingLeaves = pendingLeaves,
                    GradientFrom = palette.From,
                    GradientTo = palette.To,
                    AccentBrush = new SolidColorBrush(
                                          (Color)ColorConverter.ConvertFromString(palette.Accent))
                });
            }

            SectionsPanel.ItemsSource = cards;
        }

        private async Task LoadCharts()
        {
            var today = DateTime.Today;

            var present = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.CheckInTime.HasValue && !a.IsAbsence).CountAsync();
            var absent = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today && a.IsAbsence).CountAsync();
            var late = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today && a.Late.HasValue).CountAsync();

            AttendanceSeries.Clear();
            AttendanceSeries.Add(new PieSeries
            {
                Title = "حاضر",
                Values = new ChartValues<int> { present },
                DataLabels = true,
                LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
            });
            AttendanceSeries.Add(new PieSeries
            {
                Title = "غائب",
                Values = new ChartValues<int> { absent },
                DataLabels = true,
                LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
            });
            AttendanceSeries.Add(new PieSeries
            {
                Title = "متأخر",
                Values = new ChartValues<int> { late },
                DataLabels = true,
                LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
            });

            var departments = await _context.Departments.ToListAsync();
            DepartmentLabels = new ObservableCollection<string>(
                departments.Select(d => d.Name).ToList());

            var deptCounts = new ChartValues<int>();
            foreach (var dept in departments)
                deptCounts.Add(await _context.Users
                    .Where(u => u.DepartmentId == dept.Id && !u.IsArchived).CountAsync());

            DepartmentSeries.Clear();
            DepartmentSeries.Add(new ColumnSeries
            {
                Title = "الموظفين",
                Values = deptCounts,
                DataLabels = true
            });
        }

        private async Task LoadAlerts()
        {
            Alerts.Clear();

            var expiredDocs = await _context.Users
                .Where(u => u.NationalIDExpiration.HasValue &&
                            u.NationalIDExpiration.Value <
                            DateOnly.FromDateTime(DateTime.Now.AddMonths(1)))
                .ToListAsync();

            foreach (var user in expiredDocs.Take(5))
                Alerts.Add(new Alert
                {
                    Icon = "⚠️",
                    Message = $"رقم قومي منتهي للموظف {user.FullName}"
                });

            var pendingLoans = await _context.Loans
                .Where(l => l.Status == "SentToManager").CountAsync();
            if (pendingLoans > 0)
                Alerts.Add(new Alert
                {
                    Icon = "💰",
                    Message = $"{pendingLoans} طلب سلفة بانتظار الموافقة"
                });

            var totalUsers = await _context.Users.CountAsync();
            var attendanceCount = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == DateTime.Today)
                .Select(a => a.CheckInTime.HasValue).CountAsync();

            if (attendanceCount < totalUsers * 0.8)
                Alerts.Add(new Alert
                {
                    Icon = "📊",
                    Message = "معدل الحضور اليومي منخفض"
                });
        }

        // ── Navigation: Master → Detail ───────────────────────────

        /// <summary>Clicked on a branch card → open detail view.</summary>
        private void SectionCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is SectionCardViewModel vm)
            {
                NavigateToSection(vm.SectionId, vm.SectionName);
            }
        }

        private void NavigateToSection(int sectionId, string sectionName)
        {
            SectionDetail.LoadSection(sectionId, sectionName);

            // Slide-in animation: fade MasterView out, DetailView in
            MasterView.Visibility = Visibility.Collapsed;
            DetailView.Visibility = Visibility.Visible;
        }

        /// <summary>Back button raised by SectionDetailView.</summary>
        private void SectionDetail_BackRequested(object sender, EventArgs e)
        {
            DetailView.Visibility = Visibility.Collapsed;
            MasterView.Visibility = Visibility.Visible;

            // Refresh branch cards in background
            _ = LoadSectionCards();
        }

        // ── Timer ─────────────────────────────────────────────────
        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) =>
                CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _timer.Start();
        }

        protected virtual void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Quick Action Handlers ─────────────────────────────────
        private void OpenSalaryReport(object sender, RoutedEventArgs e) => new SalaryReport().Show();
        private void OpenEmployeeManagement(object sender, RoutedEventArgs e) => new AddEmplo().Show();
        private void OpenMonthlyAttendance(object sender, RoutedEventArgs e) => new MonthlyData().Show();
        private void OpenSalaryPayment(object sender, RoutedEventArgs e) => new BulkSalaryPaymentWindow().Show();
        private void OpenPermissions(object sender, RoutedEventArgs e) => new Permissions().Show();
        private void OpenBackup(object sender, RoutedEventArgs e) => MainWindow.CreateBackup(App.ConnectionString);
    }

    // ─────────────────────────────────────────────────────────────
    // Shared model classes
    // ─────────────────────────────────────────────────────────────
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
