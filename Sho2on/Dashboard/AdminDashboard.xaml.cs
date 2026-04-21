// AdminDashboard.xaml.cs
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using System;
using System.Collections.Generic;
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
    // Sector card ViewModel  (Level 1)
    // ─────────────────────────────────────────────────────────────
    public class SectorCardViewModel
    {
        public int SectorId { get; set; }
        public string SectorName { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalBranches { get; set; }

        public string GradientFrom { get; set; }
        public string GradientTo { get; set; }
        public Brush AccentBrush { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // AdminDashboard
    // ─────────────────────────────────────────────────────────────
    public partial class AdminDashboard : UserControl, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private DispatcherTimer _timer;
        private string _currentDateTime;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; OnPropertyChanged(nameof(CurrentDateTime)); }
        }

        public ObservableCollection<Alert> Alerts { get; set; }

        // Colour palette for sector cards
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

            Alerts = new ObservableCollection<Alert>();
            AlertsList.ItemsSource = Alerts;


            _ = LoadDashboardDataAsync();
            StartTimer();
        }

        // ── Data ──────────────────────────────────────────────────

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                await LoadSectorCards();
                await LoadAlerts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Load all sectors (Degrees) with employee + branch counts.</summary>
        private async Task LoadSectorCards()
        {
            // Degrees = القطاعات
            var sectors = await _context.Degrees.OrderBy(d => d.Name).ToListAsync();

            SectorCountLabel.Text = sectors.Count.ToString();

            var cards = new List<SectorCardViewModel>();

            for (int i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                var palette = _palette[i % _palette.Length];

                var empCount = await _context.Users
                    .Where(u => !u.IsArchived && u.DegreeId == sector.Id)
                    .CountAsync();

                // Count distinct branches that have at least one employee in this sector
                var branchCount = await _context.Users
                    .Where(u => !u.IsArchived && u.DegreeId == sector.Id && u.BranchId != null)
                    .Select(u => u.BranchId)
                    .Distinct()
                    .CountAsync();

                cards.Add(new SectorCardViewModel
                {
                    SectorId = sector.Id,
                    SectorName = sector.Name,
                    TotalEmployees = empCount,
                    TotalBranches = branchCount,
                    GradientFrom = palette.From,
                    GradientTo = palette.To,
                    AccentBrush = new SolidColorBrush(
                                         (Color)ColorConverter.ConvertFromString(palette.Accent))
                });
            }

            SectorsPanel.ItemsSource = cards;
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
                Alerts.Add(new Alert { Icon = "⚠️", Message = $"رقم قومي منتهي للموظف {user.FullName}" });

            var pendingLoans = await _context.Loans
                .Where(l => l.Status == "SentToManager").CountAsync();
            if (pendingLoans > 0)
                Alerts.Add(new Alert { Icon = "💰", Message = $"{pendingLoans} طلب سلفة بانتظار الموافقة" });

            var totalUsers = await _context.Users.CountAsync();
            var attendanceCount = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == DateTime.Today && a.CheckInTime.HasValue)
                .CountAsync();

            if (totalUsers > 0 && attendanceCount < totalUsers * 0.8)
                Alerts.Add(new Alert { Icon = "📊", Message = "معدل الحضور اليومي منخفض" });
        }

        // ── Navigation ────────────────────────────────────────────

        /// <summary>Level 1 → Level 2: open branches of selected sector.</summary>
        private void SectorCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is SectorCardViewModel vm)
            {
                BranchDetail.LoadSector(vm.SectorId, vm.SectorName);
                MasterView.Visibility = Visibility.Collapsed;
                BranchView.Visibility = Visibility.Visible;
            }
        }

        /// <summary>Back from Branch view → return to Master.</summary>
        private void BranchView_BackRequested(object sender, EventArgs e)
        {
            BranchView.Visibility = Visibility.Collapsed;
            MasterView.Visibility = Visibility.Visible;
            _ = LoadSectorCards();
        }

        /// <summary>Branch selected inside BranchListView → go to Department detail.</summary>
        private void BranchView_BranchSelected(object sender, BranchSelectedEventArgs e)
        {
            DepartmentDetail.LoadBranch(e.BranchId, e.BranchName, e.SectorId, e.SectorName);
            BranchView.Visibility = Visibility.Collapsed;
            DepartmentView.Visibility = Visibility.Visible;
        }

        /// <summary>Back from Department view → return to Branch list.</summary>
        private void DepartmentView_BackRequested(object sender, EventArgs e)
        {
            DepartmentView.Visibility = Visibility.Collapsed;
            BranchView.Visibility = Visibility.Visible;
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

        // ── Quick actions ─────────────────────────────────────────
        private void OpenSalaryReport(object sender, RoutedEventArgs e) => new SalaryReport().Show();
        private void OpenEmployeeManagement(object sender, RoutedEventArgs e) => new AddEmplo().Show();
        private void OpenMonthlyAttendance(object sender, RoutedEventArgs e) => new MonthlyData().Show();
        private void OpenSalaryPayment(object sender, RoutedEventArgs e) => new BulkSalaryPaymentWindow().Show();
        private void OpenPermissions(object sender, RoutedEventArgs e) => new Permissions().Show();
        private void OpenBackup(object sender, RoutedEventArgs e) => MainWindow.CreateBackup(App.ConnectionString);
    }

    // ── Shared models ─────────────────────────────────────────────
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

    /// <summary>Event args carrying the branch the user tapped.</summary>
    public class BranchSelectedEventArgs : EventArgs
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int SectorId { get; set; }
        public string SectorName { get; set; }
    }
}
