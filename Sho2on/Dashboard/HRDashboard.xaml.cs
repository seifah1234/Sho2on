using HR_Application.Views;
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
    /// Interaction logic for HRDashboard.xaml
    /// </summary>
    public partial class HRDashboard : System.Windows.Controls.UserControl
    {
        private readonly AppDbContext _context;
        private readonly int _currentMonth;
        private readonly int _currentYear;

        public HRDashboard()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _currentMonth = DateTime.Now.Month;
            _currentYear = DateTime.Now.Year;

            InitializeDashboard();
            LoadDashboardDataAsync();
        }

        private void InitializeDashboard()
        {
            WelcomeText.Text = $"„—Õ»« »ﬂ° {App.CurrentUser?.FullName ?? "„”ƒÊ· ‘ƒÊ‰ «·„ÊŸ›Ì‰"}";
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                await LoadHRStatistics();
                await LoadEmployeeChanges();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadHRStatistics()
        {
            // «·„ÊŸ›Ì‰ «·Ãœœ (Œ·«· «·‘Â— «·Õ«·Ì)
            var firstDayOfMonth = new DateTime(_currentYear, _currentMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var newEmployees = await _context.Users
                .Where(u => u.HireDate.ToDateTime(TimeOnly.MinValue) >= firstDayOfMonth &&
                           u.HireDate.ToDateTime(TimeOnly.MinValue) <= lastDayOfMonth &&
                           !u.IsArchived)
                .CountAsync();
            NewEmployees.Text = newEmployees.ToString();

            // «·„€«œ—Ì‰ (Œ·«· «·‘Â— «·Õ«·Ì)
            var leavingEmployees = await _context.Users
                .Where(u => u.FinishJob.HasValue &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) >= firstDayOfMonth &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) <= lastDayOfMonth)
                .CountAsync();
            LeavingEmployees.Text = leavingEmployees.ToString();

            // ÿ·»«  «·≈Ã«“… «·„⁄·ﬁ…
            var leaveRequests = await _context.Leaves
                .Where(l => l.Status == 0) // ÿ·»«  »«‰ Ÿ«— «·„Ê«›ﬁ…
                .CountAsync();
            LeaveRequests.Text = leaveRequests.ToString();

            // «·⁄ﬁÊœ «·„‰ ÂÌ… Œ·«· 30 ÌÊ„
            var expiringDate = DateTime.Now.AddDays(30);
            var expiringContracts = await _context.Users
                .Where(u => u.FinishJob.HasValue &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) <= expiringDate &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) > DateTime.Now &&
                           !u.IsArchived)
                .CountAsync();
            ExpiringContracts.Text = expiringContracts.ToString();
        }

        private async Task LoadEmployeeChanges()
        {
            var changes = new ObservableCollection<EmployeeChange>();

            // «· €ÌÌ—«  «·√ŒÌ—… ›Ì —Ê« » «·„ÊŸ›Ì‰
            var recentSalaryChanges = await _context.Salaries
                .Include(s => s.User)
                .Where(s => s.CreatedAt >= DateTime.Now.AddDays(-30))
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var salary in recentSalaryChanges)
            {
                changes.Add(new EmployeeChange
                {
                    Date = salary.CreatedAt,
                    EmployeeName = salary.User?.FullName ?? "€Ì— „⁄—Ê›",
                    ChangeType = GetSalaryTypeName(salary.Type),
                    Details = $"„»·€: {salary.Amount:C2}"
                });
            }

            // «· €ÌÌ—«  «·√ŒÌ—… ›Ì »Ì«‰«  «·„ÊŸ›Ì‰
            var recentUserChanges = await _context.Users
                .Where(u => u.UpdatedAt >= DateTime.Now.AddDays(-30) &&
                           u.UpdatedAt != u.CreatedAt)
                .OrderByDescending(u => u.UpdatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var user in recentUserChanges)
            {
                changes.Add(new EmployeeChange
                {
                    Date = user.UpdatedAt,
                    EmployeeName = user.FullName,
                    ChangeType = " ⁄œÌ· »Ì«‰« ",
                    Details = " „  ÕœÌÀ «·„⁄·Ê„«  «·‘Œ’Ì…"
                });
            }

            EmployeeChangesGrid.ItemsSource = changes
                .OrderByDescending(c => c.Date)
                .Take(10);
        }

        private string GetSalaryTypeName(int type)
        {
            return type switch
            {
                1 => "—« » √”«”Ì",
                2 => "»œ· ”ﬂ‰",
                3 => "»œ· «‰ ﬁ«·",
                4 => " √„Ì‰",
                5 => "÷—Ì»…",
                6 => "„‘«—ﬂ… «Ã „«⁄Ì…",
                9 => "”·›…",
                10 => "Ã“«¡",
                11 => "„ﬂ«›√…",
                12 => "€Ì«»",
                13 => "’‰œÊﬁ «·“„«·…",
                14 => "»œ· ≈œ«—…",
                15 => "»œ· ÿ»Ì⁄… ⁄„·",
                16 => "⁄Ã“",
                17 => "≈–‰",
                18 => "⁄„Ê·…  ÕﬁÌﬁ",
                19 => "⁄„Ê·… Œ«—ÃÌ…",
                20 => "›« Ê—…  ·Ì›Ê‰",
                _ => "„” Õﬁ«  √Œ—Ï"
            };
        }

        // Quick Action Handlers
        private void OpenAddEmployee(object sender, RoutedEventArgs e)
        {
            var window = new AddEmplo();
            window.Show();
        }

        private void OpenEditEmployees(object sender, RoutedEventArgs e)
        {
            var window = new EmployeeData();
            window.Show();
        }

        private void OpenPromotions(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("”Ì „  ÿÊÌ— ‰Ÿ«„ «· —ﬁÌ«  Ê«·‰ﬁ· ›Ì «·‰”Œ… «·ﬁ«œ„…", "ﬁÌœ «· ÿÊÌ—",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenTerminations(object sender, RoutedEventArgs e)
        {
            // Ì„ﬂ‰ ≈‰‘«¡ ‰«›–… Œ«’… »≈‰Â«¡ «·Œœ„« 
            var result = LocalizationManager.ShowMessage("Â·  —Ìœ › Õ ‰«›–… ≈‰Â«¡ «·Œœ„« ø", "≈‰Â«¡ «·Œœ„« ",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LocalizationManager.ShowMessage("”Ì „  ÿÊÌ— ‰Ÿ«„ ≈‰Â«¡ «·Œœ„«  ›Ì «·‰”Œ… «·ﬁ«œ„…", "ﬁÌœ «· ÿÊÌ—",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenAddSalary(object sender, RoutedEventArgs e)
        {
            var window = new MainSalaryWindow();
            window.Show();
        }

        private void OpenBenefitsDeductions(object sender, RoutedEventArgs e)
        {
            var window = new BenefitsDeductions();
            window.Show();
        }

        private void OpenSalaryReport(object sender, RoutedEventArgs e)
        {
            var window = new SalaryReport();
            window.Show();
        }

        private void OpenLoans(object sender, RoutedEventArgs e)
        {
            var window = new LoanApprovalWindow();
            window.Show();
        }
    }

    public class EmployeeChange
    {
        public DateTime Date { get; set; }
        public string EmployeeName { get; set; }
        public string ChangeType { get; set; }
        public string Details { get; set; }
    }
}

