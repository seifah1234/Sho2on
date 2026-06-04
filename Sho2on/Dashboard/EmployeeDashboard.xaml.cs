using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using System; using HR_Application.Helpers;
using HR_Application.Helpers;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for EmployeeDashboard.xaml
    /// </summary>
    public partial class EmployeeDashboard : System.Windows.Controls.UserControl
    {
        private readonly AppDbContext _context;
        private readonly int _currentUserId;
        private readonly DateTime _currentMonthStart;
        private readonly DateTime _currentMonthEnd;

        public EmployeeDashboard()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _currentUserId = App.CurrentUser?.Id ?? 0;

            var today = DateTime.Today;
            _currentMonthStart = new DateTime(today.Year, today.Month, 1);
            _currentMonthEnd = _currentMonthStart.AddMonths(1).AddDays(-1);

            InitializeDashboard();
            LoadDashboardDataAsync();
        }

        private void InitializeDashboard()
        {
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser?.FullName ?? "الموظف"}";
            LoadProfileImage();
            LoadEmployeeInfo();
        }

        private void LoadProfileImage()
        {
            try
            {
                if (App.CurrentUser?.ProfileImageData != null && App.CurrentUser.ProfileImageData.Length > 0)
                {
                    using (var stream = new MemoryStream(App.CurrentUser.ProfileImageData))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        ProfileImage.Source = image;
                    }
                }
                else
                {
                    // صورة افتراضية
                    ProfileImage.Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/default-avatar.png"));
                }
            }
            catch
            {
                // في حالة حدوث خطأ، عرض صورة افتراضية
                ProfileImage.Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/default-avatar.png"));
            }
        }

        private void LoadEmployeeInfo()
        {
            if (App.CurrentUser != null)
            {
                EmployeeName.Text = App.CurrentUser.FullName;
                EmployeeCode.Text = $"الكود: {App.CurrentUser.Code}";

                // تحميل معلومات الوظيفة والادارة
                LoadJobAndDepartmentInfo();
            }
        }

        private async Task LoadJobAndDepartmentInfo()
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == _currentUserId);

                if (user?.JobTitle != null)
                {
                    EmployeeJob.Text = user.JobTitle.Name;
                }

                if (user?.Department != null)
                {
                    EmployeeDepartment.Text = user.Department.Name;
                }
            }
            catch (Exception ex)
            {
                EmployeeJob.Text = LocalizationManager.Translate("غير محدد");
                EmployeeDepartment.Text = LocalizationManager.Translate("غير محدد");
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                await LoadPersonalStatistics();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPersonalStatistics()
        {
            // رصيد الإجازات
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == _currentUserId);

            if (leaveBalance != null)
            {
                LeaveBalance.Text = $"{leaveBalance.TotalBalance - leaveBalance.UsedBalance} يوم";
            }
            else
            {
                LeaveBalance.Text = LocalizationManager.Translate("0 يوم");
            }

            // الحضور الشهري
            var monthlyAttendance = await CalculateMonthlyAttendance();
            MonthlyAttendance.Text = $"{monthlyAttendance:F0}%";

            // الراتب الأخير
            await LoadLastSalary();

            // ساعات العمل الشهرية
            await LoadWorkHours();
        }

        private async Task<double> CalculateMonthlyAttendance()
        {
            var totalWorkDays = await GetTotalWorkDays();
            if (totalWorkDays == 0) return 0;

            var attendedDays = await _context.Attendances
                .Where(a => a.UserId == _currentUserId &&
                           a.AttendanceDate >= _currentMonthStart &&
                           a.AttendanceDate <= _currentMonthEnd &&
                           a.CheckInTime.HasValue &&
                           !a.IsAbsence)
                .CountAsync();

            return (attendedDays * 100.0 / totalWorkDays);
        }

        private async Task<int> GetTotalWorkDays()
        {
            // حساب أيام العمل الفعلية في الشهر (باستثناء الإجازات الأسبوعية)
            var user = await _context.Users
                .Include(u => u.WeekHoliday)
                .FirstOrDefaultAsync(u => u.Id == _currentUserId);

            if (user?.WeekHoliday == null) return 20; // قيمة افتراضية

            int totalWorkDays = 0;
            for (var date = _currentMonthStart; date <= _currentMonthEnd; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                bool isHoliday = dayOfWeek switch
                {
                    0 => user.WeekHoliday.Day7, // الأحد
                    1 => user.WeekHoliday.Day1, // الاثنين
                    2 => user.WeekHoliday.Day2,
                    3 => user.WeekHoliday.Day3,
                    4 => user.WeekHoliday.Day4,
                    5 => user.WeekHoliday.Day5,
                    6 => user.WeekHoliday.Day6,
                    _ => false
                };

                if (!isHoliday)
                {
                    totalWorkDays++;
                }
            }

            return totalWorkDays;
        }

        private async Task LoadLastSalary()
        {
            var lastSalary = await _context.SalaryPayments
                .Where(sp => sp.UserId == _currentUserId)
                .OrderByDescending(sp => sp.PaymentDate)
                .FirstOrDefaultAsync();

            if (lastSalary != null)
            {
                LastSalary.Text = $"{lastSalary.NetSalary:C2}";
            }
            else
            {
                LastSalary.Text = LocalizationManager.Translate("غير متوفر");
            }
        }

        private async Task LoadWorkHours()
        {
            var totalHours = await _context.Attendances
                .Where(a => a.UserId == _currentUserId &&
                           a.AttendanceDate >= _currentMonthStart &&
                           a.AttendanceDate <= _currentMonthEnd &&
                           a.TotalWorkHours.HasValue)
                .SumAsync(a => a.TotalWorkHours.Value.TotalHours);

            WorkHours.Text = $"{totalHours:F1} ساعة";
        }

        // Quick Action Handlers
        private void OpenLeaveRequest(object sender, RoutedEventArgs e)
        {
            var window = new HolidayRequestWindow();
            window.Show();
        }

        private void OpenLoanRequest(object sender, RoutedEventArgs e)
        {
            var window = new LoanRequestWindow();
            window.Show();
        }

        private void OpenMyProfile(object sender, RoutedEventArgs e)
        {
            var window = new AddEmplo();
            window.Show();
        }

        private void OpenMySalary(object sender, RoutedEventArgs e)
        {
            var window = new SalaryReport();
            // يمكن تمرير كود الموظف الحالي لعرض بياناته فقط
            window.Show();
        }

        private void OpenMyAttendance(object sender, RoutedEventArgs e)
        {
            var window = new MonthlyData(
                App.CurrentUser.Code.ToString(),
                DateTime.Now.ToString("MMMM"),
                DateTime.Now.Year.ToString(),
                App.CurrentUser.BranchId.ToString());
            window.Show();
        }

        private void OpenMyBenefits(object sender, RoutedEventArgs e)
        {
            // يمكن إنشاء نافذة تعرض مستحقات الموظف فقط
            var window = new BenefitsDeductions();
            window.Show();
        }

        private void OpenMyHistory(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("سيتم تطوير السجل الوظيفي في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenMyDocuments(object sender, RoutedEventArgs e)
        {
            LocalizationManager.ShowMessage("سيتم تطوير عرض المستندات في النسخة القادمة", LocalizationManager.Translate("قيد التطوير"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

