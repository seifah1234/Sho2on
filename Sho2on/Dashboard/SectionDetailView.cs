// BranchDetailView.xaml.cs
using LiveCharts;
using LiveCharts.Wpf;
using HR_Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Dashboard
{
    // ?????????????????????????????????????????????????????????????
    // Attendance row ViewModel (for today's grid)
    // ?????????????????????????????????????????????????????????????
    public class AttendanceRowViewModel
    {
        private readonly Attendance _att;

        public AttendanceRowViewModel(Attendance att) => _att = att;

        public User User => _att.User;
        public DateTime? CheckInTime => _att.CheckInTime;

        public string StatusText =>
            _att.IsAbsence ? "€«∆»" :
            _att.Late.HasValue ? "„ √Œ—" :
                                     "Õ«÷—";

        public Brush StatusColor =>
            _att.IsAbsence ? new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C)) :
            _att.Late.HasValue ? new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00)) :
                                 new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
    }

    // ?????????????????????????????????????????????????????????????
    // BranchDetailView
    // ?????????????????????????????????????????????????????????????
    public partial class SectionDetailView : UserControl
    {
        private readonly AppDbContext _context;

        private int _sectionId;
        private string _sectionName;

        /// <summary>Raised when the user presses the back button.</summary>
        public event EventHandler BackRequested;

        public SectionDetailView()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }

        // ?? Public API ????????????????????????????????????????????

        /// <summary>Called by AdminDashboard to set which section to show.</summary>
        public void LoadSection(int sectionId, string sectionName)
        {
            _sectionId = sectionId;
            _sectionName = sectionName;

            SectionTitleText.Text = sectionName;
            SectionSubtitleText.Text = $" ›«’Ì· «·ﬁÿ«⁄ ó {DateTime.Today:yyyy/MM/dd}";

            _ = LoadDataAsync();
        }

        // ?? Data loading ??????????????????????????????????????????

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadKpis();
                await LoadCharts();
                await LoadEmployees();
                await LoadTodayAttendance();
                await LoadPendingLeaves();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· »Ì«‰«  «·ﬁÿ«⁄: {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadKpis()
        {
            var today = DateTime.Today;

            var totalEmp = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectionId)
                .CountAsync();

            var totalMaleEmp = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectionId && u.Gender.ToString().ToLower() == "m")
                .CountAsync();

            var totalFemaleEmp = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectionId && u.Gender.ToString().ToLower() == "f")
                .CountAsync();

            var present = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.CheckInTime.HasValue && !a.IsAbsence &&
                            a.User.DegreeId == _sectionId)
                .CountAsync();

            var absent = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.IsAbsence && a.User.DegreeId == _sectionId)
                .CountAsync();

            var leaves = await _context.Leaves
                .Where(l => l.Status == 0 && l.User.DegreeId == _sectionId)
                .CountAsync();

            var approvedLeaves = await _context.Leaves
                .Where(l => l.Status == 2 && l.User.DegreeId == _sectionId)
                .CountAsync();

            var salaries = await _context.Users
                .Where(u => !u.IsArchived && u.DegreeId == _sectionId)
                .CountAsync();

            KpiEmployees.Text = totalEmp.ToString();
            KpiMaleEmployees.Text = totalMaleEmp.ToString();
            KpiFemaleEmployees.Text = totalFemaleEmp.ToString();
            KpiPresent.Text = present.ToString();
            KpiAbsent.Text = absent.ToString();
            KpiLeaves.Text = leaves.ToString();
            KpiSalaries.Text = salaries.ToString();
            KpiApprovedLeaves.Text = approvedLeaves.ToString();
        }

        private async Task LoadCharts()
        {
            var today = DateTime.Today;

            // ?? Pie: Õ÷Ê— «·›—⁄ «·ÌÊ„ ??
            var present = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.CheckInTime.HasValue && !a.IsAbsence &&
                            a.User.DegreeId == _sectionId).CountAsync();

            var absent = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.IsAbsence && a.User.DegreeId == _sectionId).CountAsync();

            var late = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today &&
                            a.Late.HasValue && a.User.DegreeId == _sectionId).CountAsync();

            var pieSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Õ«÷—", Values = new ChartValues<int> { present },
                    DataLabels = true,
                    LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
                },
                new PieSeries
                {
                    Title = "€«∆»", Values = new ChartValues<int> { absent },
                    DataLabels = true,
                    LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
                },
                new PieSeries
                {
                    Title = "„ √Œ—", Values = new ChartValues<int> { late },
                    DataLabels = true,
                    LabelPoint = p => $"{p.Y} ({p.Participation:P0})"
                }
            };
            SectionAttendancePie.Series = pieSeries;

            // ?? Bar:  Ê“Ì⁄ «·„ÊŸ›Ì‰ »«·≈œ«—«  ??
            var depts = await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();

            var deptCounts = new ChartValues<int>();
            foreach (var dept in depts)
                deptCounts.Add(await _context.Users
                    .Where(u => u.DepartmentId == dept.Id && !u.IsArchived).CountAsync());

            SectionDeptAxisX.Labels = depts.Select(d => d.Name).ToList();
            SectionDeptBar.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "«·„ÊŸ›Ì‰", Values = deptCounts, DataLabels = true
                }
            };
        }

        private async Task LoadEmployees()
        {
            var employees = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Where(u => !u.IsArchived && u.DegreeId == _sectionId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            EmployeesGrid.ItemsSource = employees;
        }

        private async Task LoadTodayAttendance()
        {
            var today = DateTime.Today;

            var attendances = await _context.Attendances
                .Include(a => a.User)
                .Where(a => a.AttendanceDate.Date == today &&
                            a.User.DegreeId == _sectionId)
                .OrderBy(a => a.User.FullName)
                .ToListAsync();

            TodayAttendanceGrid.ItemsSource =
                attendances.Select(a => new AttendanceRowViewModel(a)).ToList();
        }

        private async Task LoadPendingLeaves()
        {
            var leaves = await _context.Leaves
                .Include(l => l.User)
                .Include(l => l.LeaveType)
                .Where(l => l.Status == 0 && l.User.DegreeId == _sectionId)
                .OrderByDescending(l => l.RequestDate)
                .ToListAsync();

            LeavesGrid.ItemsSource = leaves;
        }

        // ?? Buttons ???????????????????????????????????????????????

        private void BackBtn_Click(object sender, RoutedEventArgs e) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) =>
            _ = LoadDataAsync();
    }
}

