// DepartmentDetailView.xaml.cs
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using HR_Application.Helpers;
using Sho2on.Database.Models;
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
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Dashboard
{
    // ─────────────────────────────────────────────────────────────
    // ViewModel: Area manager row (has an AreaName property)
    // ─────────────────────────────────────────────────────────────
    public class AreaManagerViewModel
    {
        // Forwarded from User
        public string FullName { get; set; }
        public string AreaName { get; set; }
        public Department Department { get; set; }
        public JobTitle JobTitle { get; set; }
        public decimal BasicSalary { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // DepartmentDetailView  — Level 3
    // Departments are FILTERS, not cards.
    // Area managers (BranchId == null, AreaId == branch.AreaId) are
    // shown in a dedicated section above the employees grid.
    // ─────────────────────────────────────────────────────────────
    public partial class DepartmentDetailView : UserControl
    {
        private readonly AppDbContext _context;

        private int _branchId;
        private string _branchName;
        private int _sectorId;
        private string _sectorName;
        private int? _areaId;        // null when branch has no Area

        // Currently selected department filter (null = جميع الإدارات)
        private int? _selectedDeptId = null;

        // Cached data (loaded once, filtered in-memory for speed)
        private List<User> _allEmployees = new();
        private List<Leave> _allLeaves = new();
        private List<(int DeptId, string DeptName)> _departments = new();

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

        /// <summary>Fired when user presses "Back".</summary>
        public event EventHandler BackRequested;

        public DepartmentDetailView()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }

        // ── Public API ────────────────────────────────────────────

        public void LoadBranch(int branchId, string branchName, int sectorId, string sectorName)
        {
            _branchId = branchId;
            _branchName = branchName;
            _sectorId = sectorId;
            _sectorName = sectorName;
            _selectedDeptId = null;   // reset filter on every navigation

            SectorBreadcrumb.Text = sectorName;
            BranchTitleText.Text = branchName;
            BranchSubtitleText.Text = $"إدارات الفرع — {DateTime.Today:yyyy/MM/dd}";

            _ = LoadDataAsync();
        }

        // ── Data ──────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            try
            {
                // 1. Detect Area for this branch
                await DetectArea();

                // 2. Cache all employees + leaves (single DB round-trip per entity)
                await CacheData();

                // 3. Build department filter chips
                BuildDeptChips();

                // 4. Load area managers (separate query — cross-branch)
                await LoadAreaManagers();

                // 5. Apply filter (with _selectedDeptId == null → show all)
                ApplyFilter();

                // 6. KPIs (always based on full branch+sector, not filtered)
                UpdateKpis();

                // 7. Chart (responds to filter)
                UpdateChart();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الإدارات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Read AreaId from the branch and show/hide the Area badge.</summary>
        private async Task DetectArea()
        {
            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == _branchId);

            // Assumes Branch has nullable AreaId and a navigation property Area
            _areaId = branch?.AreaId;

            if (_areaId.HasValue)
            {
                var area = await _context.Areas
                    .FirstOrDefaultAsync(a => a.Id == _areaId.Value);
                if (area != null)
                {
                    AreaNameText.Text = area.Name;
                    AreaBadge.Visibility = Visibility.Visible;
                    AreaManagersTitle.Text = $"مسؤولو منطقة {area.Name}";
                }
            }
            else
            {
                AreaBadge.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Cache employees and leaves for this branch+sector into memory lists.</summary>
        private async Task CacheData()
        {
            _allEmployees = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Where(u => !u.IsArchived &&
                            u.DegreeId == _sectorId &&
                            u.BranchId == _branchId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            _allLeaves = await _context.Leaves
                .Include(l => l.User).ThenInclude(u => u.Department)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == 0 &&
                            l.User.DegreeId == _sectorId &&
                            l.User.BranchId == _branchId)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            // Distinct departments present in this branch+sector
            _departments = _allEmployees
                .Where(u => u.Department != null)
                .Select(u => (u.DepartmentId, u.Department!.Name))
                .Distinct()
                .OrderBy(d => d.Name)
                .ToList();
        }

        // ── Department filter chips ───────────────────────────────

        private void BuildDeptChips()
        {
            DeptChipsPanel.Children.Clear();

            // "جميع الإدارات" chip
            DeptChipsPanel.Children.Add(
                BuildChip(null, "جميع الإدارات", isSelected: _selectedDeptId == null));

            foreach (var (id, name) in _departments)
                DeptChipsPanel.Children.Add(
                    BuildChip(id, name, isSelected: _selectedDeptId == id));
        }

        private Border BuildChip(int? deptId, string label, bool isSelected)
        {
            var chip = new Border
            {
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(4),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1.5),
                Tag = deptId   // null = "all"
            };

            if (isSelected)
            {
                // Highlighted chip
                chip.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0xBC, 0xD4));
                chip.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xBC, 0xD4));
                chip.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 0,
                    Color = Color.FromRgb(0x00, 0xBC, 0xD4),
                    Opacity = 0.45
                };
            }
            else
            {
                chip.Background = (Brush)TryFindResource("SurfaceLightColor")
                                    ?? new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
                chip.BorderBrush = (Brush)TryFindResource("BorderColor")
                                    ?? new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
            }

            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            if (isSelected && deptId != null)
            {
                // Show small filter icon when a specific dept is selected
                var icon = new MahApps.Metro.IconPacks.PackIconMaterial
                {
                    Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Check,
                    Width = 12,
                    Height = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xBC, 0xD4)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                sp.Children.Add(icon);
            }

            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0x00, 0xBC, 0xD4))
                    : (Brush)(TryFindResource("TextSecondaryColor")
                      ?? new SolidColorBrush(Colors.White)),
                VerticalAlignment = VerticalAlignment.Center
            });

            chip.Child = sp;
            chip.MouseLeftButtonUp += Chip_Click;
            return chip;
        }

        private void Chip_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border chip)
            {
                _selectedDeptId = chip.Tag as int?;
                BuildDeptChips();   // re-render chips with new selection
                ApplyFilter();
                UpdateChart();
                UpdateActiveFilterBadge();
            }
        }

        private void UpdateActiveFilterBadge()
        {
            if (_selectedDeptId.HasValue)
            {
                var name = _departments.FirstOrDefault(d => d.DeptId == _selectedDeptId.Value).DeptName;
                ActiveFilterText.Text = name;
                ActiveFilterBadge.Visibility = Visibility.Visible;

                EmployeesGridTitle.Text = $"موظفو {name}";
                LeavesGridTitle.Text = $"طلبات إجازة — {name}";
            }
            else
            {
                ActiveFilterBadge.Visibility = Visibility.Collapsed;
                EmployeesGridTitle.Text = "موظفو الفرع";
                LeavesGridTitle.Text = "طلبات الإجازة المعلقة";
            }
        }

        // ── Apply filter to grids ─────────────────────────────────

        private void ApplyFilter()
        {
            IEnumerable<User> filteredEmp = _allEmployees;
            IEnumerable<Leave> filteredLeaves = _allLeaves;
            int approvedNo = Convert.ToInt32(KpiApprovedLeaves.Text);

            if (_selectedDeptId.HasValue)
            {
                filteredEmp = filteredEmp.Where(u => u.DepartmentId == _selectedDeptId.Value);
                filteredLeaves = filteredLeaves.Where(l => l.User.DepartmentId == _selectedDeptId.Value);
                approvedNo = _context.Leaves
                .Count(l => l.Status == 2 &&
                                 l.User.DegreeId == _sectorId &&
                                 l.User.BranchId == _branchId);
            }

            var empList = filteredEmp.ToList();
            var leaveList = filteredLeaves.ToList();

            EmployeesGrid.ItemsSource = empList;
            LeavesGrid.ItemsSource = leaveList;
            EmployeeCountBadge.Text = $"{empList.Count} موظف";
            KpiTotalEmp.Text = filteredEmp.Count().ToString();
            KpiMale.Text = filteredEmp.Count(u => u.Gender.ToString().ToLower() == "m").ToString();
            KpiFemale.Text = filteredEmp.Count(u => u.Gender.ToString().ToLower() == "f").ToString();
            KpiPendingLeaves.Text = filteredLeaves.Count().ToString();
            KpiApprovedLeaves.Text = approvedNo.ToString();
            KpiSalaries.Text = filteredEmp.Count().ToString();

            UpdateActiveFilterBadge();
        }

        // ── KPIs (always full branch+sector, not filtered) ────────

        private void UpdateKpis()
        {
            KpiTotalEmp.Text = _allEmployees.Count.ToString();
            KpiMale.Text = _allEmployees.Count(u => u.Gender.ToString().ToLower() == "m").ToString();
            KpiFemale.Text = _allEmployees.Count(u => u.Gender.ToString().ToLower() == "f").ToString();
            KpiPendingLeaves.Text = _allLeaves.Count.ToString();
            KpiSalaries.Text = _allEmployees.Count.ToString();

            // Approved leaves need a separate query (cached leaves are pending only)
            _ = LoadApprovedLeavesKpi();
        }

        private async Task LoadApprovedLeavesKpi()
        {
            var approved = await _context.Leaves
                .CountAsync(l => l.Status == 2 &&
                                 l.User.DegreeId == _sectorId &&
                                 l.User.BranchId == _branchId);
            KpiApprovedLeaves.Text = approved.ToString();
        }

        // ── Area managers ─────────────────────────────────────────

        /// <summary>
        /// Area managers are employees whose BranchId is null (or whose
        /// job scope covers the whole area), linked to the same AreaId
        /// as the current branch.
        /// 
        /// Strategy: we look for Users where
        ///   - DegreeId == _sectorId
        ///   - BranchId == null  (area-level, not tied to a single branch)
        ///   - Branch.AreaId    == _areaId  (same area)
        /// 
        /// If your schema uses a different field (e.g. IsAreaManager flag
        /// or a dedicated AreaId FK on User), adjust the Where clause below.
        /// </summary>
        private async Task LoadAreaManagers()
        {
            if (!_areaId.HasValue)
            {
                AreaManagersCard.Visibility = Visibility.Collapsed;
                return;
            }

            // Option A: employees with no BranchId but belong to the same area via their
            //           job title or an explicit AreaId column on User
            //           (adjust to match your actual schema)
            List<User> areaManagers;

            try
            {
                // Try: User has an AreaId property
                areaManagers = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Where(u => !u.IsArchived &&
                                u.DegreeId == _sectorId &&
                                u.AreaId == _areaId.Value)   // ← adjust field name if needed
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch
            {
                // Fallback: User has no AreaId → find employees with BranchId == null in sector
                areaManagers = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Where(u => !u.IsArchived &&
                                u.DegreeId == _sectorId &&
                                u.BranchId == null)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            if (areaManagers.Count == 0)
            {
                AreaManagersCard.Visibility = Visibility.Collapsed;
                return;
            }

            // Fetch area name for the tag column
            var area = await _context.Areas
                .FirstOrDefaultAsync(a => a.Id == _areaId.Value);
            var areaName = area?.Name ?? "المنطقة";

            AreaManagersGrid.ItemsSource = areaManagers.Select(u => new AreaManagerViewModel
            {
                FullName = u.FullName,
                AreaName = areaName,
                Department = u.Department,
                JobTitle = u.JobTitle,
                BasicSalary = u.MainSalary ?? 0
            }).ToList();

            AreaManagersCard.Visibility = Visibility.Visible;
        }

        // ── Chart (responds to dept filter) ──────────────────────

        private void UpdateChart()
        {
            // If a specific department is selected → single-bar chart
            // If "all" → one bar per department
            var deptsToShow = _selectedDeptId.HasValue
                ? _departments.Where(d => d.DeptId == _selectedDeptId.Value).ToList()
                : _departments;

            var counts = deptsToShow.Select(d =>
                _selectedDeptId.HasValue
                    ? _allEmployees.Count(u => u.DepartmentId == d.DeptId)
                    : _allEmployees.Count(u => u.DepartmentId == d.DeptId)
            ).ToList();

            DeptAxisX.Labels = deptsToShow.Select(d => d.DeptName).ToList();
            DeptBarChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title      = "الموظفين",
                    Values     = new ChartValues<int>(counts),
                    DataLabels = true
                }
            };
        }

        // ── Navigation ────────────────────────────────────────────

        private void BackBtn_Click(object sender, RoutedEventArgs e) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) =>
            _ = LoadDataAsync();
    }
}

