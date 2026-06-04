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
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser?.FullName ?? "مسؤول شؤون الموظفين"}";
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
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadHRStatistics()
        {
            // الموظفين الجدد (خلال الشهر الحالي)
            var firstDayOfMonth = new DateTime(_currentYear, _currentMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var newEmployees = await _context.Users
                .Where(u => u.HireDate.ToDateTime(TimeOnly.MinValue) >= firstDayOfMonth &&
                           u.HireDate.ToDateTime(TimeOnly.MinValue) <= lastDayOfMonth &&
                           !u.IsArchived)
                .CountAsync();
            NewEmployees.Text = newEmployees.ToString();

            // المغادرين (خلال الشهر الحالي)
            var leavingEmployees = await _context.Users
                .Where(u => u.FinishJob.HasValue &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) >= firstDayOfMonth &&
                           u.FinishJob.Value.ToDateTime(TimeOnly.MinValue) <= lastDayOfMonth)
                .CountAsync();
            LeavingEmployees.Text = leavingEmployees.ToString();

            // طلبات الإجازة المعلقة
            var leaveRequests = await _context.Leaves
                .Where(l => l.Status == 0) // طلبات بانتظار الموافقة
                .CountAsync();
            LeaveRequests.Text = leaveRequests.ToString();

            // العقود المنتهية خلال 30 يوم
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

            // التغييرات الأخيرة في رواتب الموظفين
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
                    EmployeeName = salary.User?.FullName ?? LocalizationManager.Translate("غير معروف"),
                    ChangeType = GetSalaryTypeName(salary.Type),
                    Details = $"مبلغ: {salary.Amount:C2}"
                });
            }

            // التغييرات الأخيرة في بيانات الموظفين
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
                    ChangeType = LocalizationManager.Translate("تعديل بيانات"),
                    Details = LocalizationManager.Translate("تم تحديث المعلومات الشخصية")
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
                1 => LocalizationManager.Translate("راتب أساسي"),
                2 => LocalizationManager.Translate("بدل سكن"),
                3 => LocalizationManager.Translate("بدل انتقال"),
                4 => LocalizationManager.Translate("تأمين"),
                5 => LocalizationManager.Translate("ضريبة"),
                6 => LocalizationManager.Translate("مشاركة اجتماعية"),
                9 => LocalizationManager.Translate("سلفة"),
                10 => LocalizationManager.Translate("جزاء"),
                11 => LocalizationManager.Translate("مكافأة"),
                12 => LocalizationManager.Translate("غياب"),
                13 => LocalizationManager.Translate("صندوق الزمالة"),
                14 => LocalizationManager.Translate("بدل إدارة"),
                15 => LocalizationManager.Translate("بدل طبيعة عمل"),
                16 => LocalizationManager.Translate("عجز"),
                17 => LocalizationManager.Translate("إذن"),
                18 => LocalizationManager.Translate("عمولة تحقيق"),
                19 => LocalizationManager.Translate("عمولة خارجية"),
                20 => LocalizationManager.Translate("فاتورة تليفون"),
                _ => LocalizationManager.Translate("مستحقات أخرى")
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
            LocalizationManager.ShowMessage("سيتم تطوير نظام الترقيات والنقل في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenTerminations(object sender, RoutedEventArgs e)
        {
            // يمكن إنشاء نافذة خاصة بإنهاء الخدمات
            var result = LocalizationManager.ShowMessage("هل تريد فتح نافذة إنهاء الخدمات؟", LocalizationManager.Translate("إنهاء الخدمات"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LocalizationManager.ShowMessage("سيتم تطوير نظام إنهاء الخدمات في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
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

