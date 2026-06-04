// BranchListView.xaml.cs
using HR_Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Dashboard
{
    // ─────────────────────────────────────────────────────────────
    // Branch card ViewModel  (Level 2)
    // ─────────────────────────────────────────────────────────────
    public class BranchCardViewModel
    {
        public int    BranchId          { get; set; }
        public string BranchName        { get; set; }
        public int    SectorId          { get; set; }
        public string SectorName        { get; set; }
        public int    TotalEmployees    { get; set; }
        public int    TotalDepartments  { get; set; }
        public int    TodayPresent      { get; set; }

        public double AttendanceRate =>
            TotalEmployees > 0 ? (double)TodayPresent / TotalEmployees : 0;

        public string AttendanceRateText =>
            $"نسبة الحضور: {AttendanceRate:P0}";

        /// Bar width in px (max 198 = card 230 – 2×16 padding)
        public double AttendanceBarWidth =>
            Math.Min(AttendanceRate * 198, 198);

        public string GradientFrom { get; set; }
        public string GradientTo   { get; set; }
        public Brush  AccentBrush  { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // BranchListView
    // ─────────────────────────────────────────────────────────────
    public partial class BranchListView : UserControl
    {
        private readonly AppDbContext _context;
        private int    _sectorId;
        private string _sectorName;

        /// <summary>Fired when the user presses "Back".</summary>
        public event EventHandler BackRequested;

        /// <summary>Fired when the user taps a branch card.</summary>
        public event EventHandler<BranchSelectedEventArgs> BranchSelected;

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

        public BranchListView()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }

        // ── Public API ────────────────────────────────────────────

        /// <summary>Called by AdminDashboard to show branches for a sector.</summary>
        public void LoadSector(int sectorId, string sectorName)
        {
            _sectorId   = sectorId;
            _sectorName = sectorName;

            SectorTitleText.Text    = sectorName;
            SectorSubtitleText.Text = $"فروع القطاع — {DateTime.Today:yyyy/MM/dd}";

            _ = LoadDataAsync();
        }

        // ── Data ──────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadKpis();
                await LoadBranchCards();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الفروع: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Sector-level aggregate KPIs shown at the top.</summary>
        private async Task LoadKpis()
        {
            var today = DateTime.Today;

            var totalEmp = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectorId)
                .CountAsync();

            // Count distinct branches that have employees in this sector
            var branchCount = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectorId && u.BranchId != null)
                .Select(u => u.BranchId)
                .Distinct()
                .CountAsync();

            var pendingLeaves = await _context.Leaves
                .Where(l => l.Status == 0 && l.User.DegreeId == _sectorId)
                .CountAsync();

            var todayPresent = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.CheckInTime.HasValue && !a.IsAbsence &&
                            a.User.DegreeId == _sectorId)
                .CountAsync();

            KpiTotalEmp.Text      = totalEmp.ToString();
            KpiBranchCount.Text   = branchCount.ToString();
            KpiPendingLeaves.Text = pendingLeaves.ToString();
            KpiTodayPresent.Text  = todayPresent.ToString();
        }

        /// <summary>Build one card per branch that contains employees in this sector.</summary>
        private async Task LoadBranchCards()
        {
            var today = DateTime.Today;

            // Fetch all branches that have at least one user in this sector
            var branchIds = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectorId && u.BranchId != null)
                .Select(u => (int)u.BranchId)
                .Distinct()
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => branchIds.Contains(b.Id))
                .OrderBy(b => b.Name)
                .ToListAsync();

            BranchCountBadge.Text = branches.Count.ToString();

            var cards = new List<BranchCardViewModel>();

            for (int i = 0; i < branches.Count; i++)
            {
                var branch  = branches[i];
                var palette = _palette[i % _palette.Length];

                var empCount = await _context.Users
                    .Where(u => !u.IsArchived &&
                                u.DegreeId == _sectorId &&
                                u.BranchId == branch.Id)
                    .CountAsync();

                // Departments that have employees in this branch+sector combo
                var deptCount = await _context.Users
                    .Where(u => !u.IsArchived &&
                                u.DegreeId == _sectorId &&
                                u.BranchId == branch.Id &&
                                u.DepartmentId != null)
                    .Select(u => u.DepartmentId)
                    .Distinct()
                    .CountAsync();

                var present = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == today &&
                                a.CheckInTime.HasValue && !a.IsAbsence &&
                                a.User.DegreeId == _sectorId &&
                                a.User.BranchId == branch.Id)
                    .CountAsync();

                cards.Add(new BranchCardViewModel
                {
                    BranchId         = branch.Id,
                    BranchName       = branch.Name,
                    SectorId         = _sectorId,
                    SectorName       = _sectorName,
                    TotalEmployees   = empCount,
                    TotalDepartments = deptCount,
                    TodayPresent     = present,
                    GradientFrom     = palette.From,
                    GradientTo       = palette.To,
                    AccentBrush      = new SolidColorBrush(
                                           (Color)ColorConverter.ConvertFromString(palette.Accent))
                });
            }

            BranchesPanel.ItemsSource = cards;
        }

        // ── Navigation ────────────────────────────────────────────

        private void BranchCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is BranchCardViewModel vm)
            {
                BranchSelected?.Invoke(this, new BranchSelectedEventArgs
                {
                    BranchId   = vm.BranchId,
                    BranchName = vm.BranchName,
                    SectorId   = vm.SectorId,
                    SectorName = vm.SectorName
                });
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) =>
            _ = LoadDataAsync();
    }
}

