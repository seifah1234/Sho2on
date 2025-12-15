using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    /// <summary>
    /// Interaction logic for SalaryReport.xaml
    /// </summary>
    public partial class SalaryReport : Window
    {
        private Dictionary<string, int> branches = new Dictionary<string, int>();
        private Dictionary<string, int> depatrs = new Dictionary<string, int>();
        private Dictionary<string, int> jobs = new Dictionary<string, int>();
        List<int> weekHoli = new List<int>();
        List<SalaryClass> employeeSalaryList = new List<SalaryClass>();
        private ObservableCollection<SalaryClass> employeeSalaryCollection;

        private MonthSettings monthSettings;
        int OTHours = 0;
        decimal LateValue = 0;
        int LateRepeat = 0;
        decimal OTValue = 0;
        private static int Abcense = 0;
        decimal minSalary = 0;
        int totalOvertimeHours = 0;
        int totalOvertimeMin = 0;
        int totalOvertimeSec = 0;
        int totalLateHours = 0;
        int totalLateMin = 0;
        int totalLateSec = 0;
        int totalWHHours = 0;
        int totalWHMin = 0;
        int totalWHSec = 0;
        private static TimeSpan late = TimeSpan.Zero;
        private static TimeSpan ot = TimeSpan.Zero;

        private AppDbContext _context = new AppDbContext(App.ConnectionString);

        public SalaryReport()
        {
            InitializeComponent();
            LoadData();
            InitializeDateSelections(); 
            employeeSalaryCollection = new ObservableCollection<SalaryClass>();
            list.ItemsSource = employeeSalaryCollection;
        }

        private void InitializeDateSelections()
        {
            monthComboBox.SelectedItem = DateTime.Now.ToString("MMMM", CultureInfo.CurrentCulture);
            yearComboBox.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            monthComboBox.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            yearComboBox.SelectedItem = DateTime.Now.Year;
        }

        private async void LoadData()
        {
            try
            {
                branches.Clear();
                depatrs.Clear();
                jobs.Clear();

                // Load branches
                var dbBranches = await _context.Branches.ToListAsync();
                branchComboBox.Items.Clear();
                foreach (var branch in dbBranches)
                {
                    branchComboBox.Items.Add(branch.Name);
                    branches.Add(branch.Name, branch.Id);
                }

                // Load departments
                var dbDepartments = await _context.Departments.ToListAsync();
                departComboBox.Items.Clear();
                foreach (var dept in dbDepartments)
                {
                    departComboBox.Items.Add(dept.Name);
                    depatrs.Add(dept.Name, dept.Id);
                }

                // Load jobs
                var dbJobs = await _context.JobTitles.ToListAsync();
                jobComboBox.Items.Clear();
                foreach (var job in dbJobs)
                {
                    jobComboBox.Items.Add(job.Name);
                    jobs.Add(job.Name, job.Id);
                }

                monthComboBox.ItemsSource = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
                yearComboBox.ItemsSource = Enumerable.Range(2010, 21).ToList();

                monthSettings = new MonthSettings
                {
                    StartDate = Properties.Settings.Default.StartOfMonth.ToString(),
                    EndDate = Properties.Settings.Default.EndOfMonth.ToString()
                };
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private async Task<List<SalaryData>> BuildQueryAsync(Dictionary<string, string> filters, DateTime start, DateTime end)
        {
            var query = _context.Users
                .Include(u => u.Branch)
                .Include(u => u.JobTitle)
                .Include(u => u.WeekHoliday)
                .Include(u => u.Salaries)
                .AsQueryable();

            // Apply filters
            if (filters.ContainsKey("code"))
            {
                query = query.Where(u => u.Id.ToString() == filters["code"]);
            }

            if (filters.ContainsKey("name"))
            {
                query = query.Where(u => u.FullName.StartsWith(filters["name"]));
            }

            if (filters.ContainsKey("depart"))
            {
                query = query.Where(u => u.DepartmentId.ToString() == filters["depart"]);
            }

            if (filters.ContainsKey("job"))
            {
                query = query.Where(u => u.JobTitleId.ToString() == filters["job"]);
            }

            if (filters.ContainsKey("branch"))
            {
                query = query.Where(u => u.BranchId.ToString() == filters["branch"]);
            }

            var users = await query.ToListAsync();
            var result = new List<SalaryData>();

            foreach (var user in users)
            {
                var salaries = user.Salaries ?? new List<Salary>();
                var mainSalary = salaries.FirstOrDefault(s => s.Type == 1);

                var salaryData = new SalaryData
                {
                    Code = user.Code.ToString(),
                    Name = user.FullName,
                    Branch = user.Branch?.Name,
                    MinSalary = user.MinSalary ?? 0,
                    SalaryType = mainSalary?.SalaryType ?? 1,
                    Job = user.JobTitle?.Name,
                    Day1 = user.WeekHoliday?.Day1 ?? false,
                    Day2 = user.WeekHoliday?.Day2 ?? false,
                    Day3 = user.WeekHoliday?.Day3 ?? false,
                    Day4 = user.WeekHoliday?.Day4 ?? false,
                    Day5 = user.WeekHoliday?.Day5 ?? false,
                    Day6 = user.WeekHoliday?.Day6 ?? false,
                    Day7 = user.WeekHoliday?.Day7 ?? false,
                    Salary = salaries.Where(s => s.Type == 1).Sum(s => s.Amount),
                    Houseing = salaries.Where(s => s.Type == 2).Sum(s => s.Amount),
                    Transmission = salaries.Where(s => s.Type == 3).Sum(s => s.Amount),
                    Reward = salaries.Where(s => s.Type == 11 && s.DayDate >= start && s.DayDate <= end).Sum(s => s.Amount),
                    Abcence = salaries.Where(s => s.Type == 12).Sum(s => s.Amount),
                    Ancestor = salaries.Where(s => s.Type == 9 && s.DayDate >= start && s.DayDate <= end).Sum(s => s.Amount),
                    Penalty = salaries.Where(s => s.Type == 10 && s.DayDate >= start && s.DayDate <= end).Sum(s => s.Amount),
                    PermissionValue = salaries.Where(s => s.Type == 17 && s.DayDate >= start && s.DayDate <= end).Sum(s => s.Amount),
                    Deficit = salaries.Where(s => s.Type == 16 && s.DayDate >= start && s.DayDate <= end).Sum(s => s.Amount),
                    Tax = salaries.Where(s => s.Type == 5).Sum(s => s.Amount),
                    Insurance = salaries.Where(s => s.Type == 4).Sum(s => s.Amount),
                    CompanyInsurance = salaries.Where(s => s.Type == 16).Sum(s => s.Amount),
                    Social = salaries.Where(s => s.Type == 6).Sum(s => s.Amount),
                    Box = salaries.Where(s => s.Type == 13).Sum(s => s.Amount),
                    Nature = salaries.Where(s => s.Type == 15).Sum(s => s.Amount),
                    Depart = salaries.Where(s => s.Type == 14).Sum(s => s.Amount),
                    TargetCommission = salaries.Where(s => s.Type == 18).Sum(s => s.Amount),
                    ExternalCommission = salaries.Where(s => s.Type == 19).Sum(s => s.Amount),
                    Phone = salaries.Where(s => s.Type == 20).Sum(s => s.Amount),
                    ExemptLate = user.ExemptLate,
                    ExemptEarlyLeave = user.ExemptEarlyLeave,
                    ExemptOvertime = user.ExemptOvertime,
                    ExemptAbsence = user.ExemptAbsence,
                    ExemptEarlyEnter = user.ExemptEarlyEnter
                };

                // Calculate totals
                salaryData.TotalEntitlements = salaryData.Salary + salaryData.Houseing + salaryData.Transmission +
                                            salaryData.Reward + salaryData.Nature + salaryData.Depart + salaryData.TargetCommission + salaryData.ExternalCommission;

                salaryData.TotalDeductions = salaryData.Tax + salaryData.Insurance + salaryData.Social +
                                           salaryData.Box + salaryData.Ancestor + salaryData.Penalty + salaryData.Phone +
                                           salaryData.Deficit + salaryData.CompanyInsurance;

                salaryData.Total = salaryData.TotalEntitlements - salaryData.TotalDeductions;

                result.Add(salaryData);
            }

            return result;
        }

        public static decimal TimeSpanToDecimal(TimeSpan timeSpan)
        {
            if (timeSpan.Hours > 0 || timeSpan.Minutes > 0)
            {
                decimal hours = timeSpan.Hours * 60;
                decimal minutes = timeSpan.Minutes;
                decimal totalHours = hours + minutes;
                return totalHours;
            }
            return 0;
        }

        private async Task LoadFromAttendance(int userId, bool exemptLate, bool exemptOvertime)
        {
            int lateType = Properties.Settings.Default.LateType;

            try
            {
                int monthNumber = DateTime.ParseExact(monthComboBox.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                int year = Convert.ToInt16(yearComboBox.Text);
                (DateTime startMonth, DateTime endMonth) = GetCustomMonthDates(monthNumber, year);

                var attendances = await _context.Attendances
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId &&
                               a.AttendanceDate >= startMonth &&
                               a.AttendanceDate <= endMonth)
                    .ToListAsync();

                // Get late and overtime rates
                var lateRates = await _context.LateOvertimes
                    .Where(l => l.Type == 0 && (lateType == 0 ? l.MoneyType == 0 : l.MoneyType == 1))
                    .ToListAsync();

                var overtimeRates = await _context.LateOvertimes
                    .Where(o => o.Type == 1 && (lateType == 0 ? o.MoneyType == 0 : o.MoneyType == 1))
                    .ToListAsync();

                foreach (var att in attendances)
                {
                    // Calculate late value
                    if (!exemptLate && att.Late.HasValue && att.Late.Value > TimeSpan.Zero)
                    {
                        var lateRate = lateRates.FirstOrDefault(l =>
                            att.Late.Value > l.StartTime && att.Late.Value < l.EndTime);

                        if (lateRate != null)
                        {
                            LateRepeat++;

                            if (lateType == 0)
                            {
                                LateValue += TimeSpanToDecimal(att.Late.Value) * lateRate.Value * minSalary;
                            }
                            else
                            {
                                LateValue += lateRate.Value;
                            }
                        }

                        totalLateHours += att.Late.Value.Hours;
                        totalLateMin += att.Late.Value.Minutes;
                        totalLateSec += att.Late.Value.Seconds;
                    }

                    // Calculate overtime value
                    if (!exemptOvertime && att.Overtime.HasValue && att.Overtime.Value > TimeSpan.Zero)
                    {
                        var otRate = overtimeRates.FirstOrDefault(o =>
                            att.Overtime.Value > o.StartTime && att.Overtime.Value < o.EndTime);

                        if (otRate != null)
                        {
                            if (lateType == 0)
                            {
                                OTValue += TimeSpanToDecimal(att.Overtime.Value) * otRate.Value * minSalary;
                            }
                            else
                            {
                                OTValue += otRate.Value;
                            }
                        }

                        totalOvertimeHours += att.Overtime.Value.Hours;
                        totalOvertimeMin += att.Overtime.Value.Minutes;
                        totalOvertimeSec += att.Overtime.Value.Seconds;
                    }

                    // Calculate work hours
                    if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                    {
                        TimeSpan totalWH = att.CheckOutTime.Value < att.CheckInTime.Value
                            ? att.CheckOutTime.Value.Add(new TimeSpan(24, 0, 0)) - att.CheckInTime.Value
                            : att.CheckOutTime.Value - att.CheckInTime.Value;

                        totalWHHours += totalWH.Hours;
                        totalWHMin += totalWH.Minutes;
                    }

                    // Count absences
                    if (att.IsAbsence)
                    {
                        Abcense++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year)
        {
            try
            {
                int startDay = Convert.ToInt16(Properties.Settings.Default.StartOfMonth);
                int endDay = (month == 2 && Convert.ToInt16(Properties.Settings.Default.EndOfMonth) > 29) ? 29 : Convert.ToInt16(Properties.Settings.Default.EndOfMonth);

                DateTime startDate = new DateTime(year, month, startDay);
                DateTime endDate = new DateTime(year, month, endDay);

                if (15 < startDay) startDate = startDate.AddMonths(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب تواريخ الشهر المخصص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }

        public class MonthSettings
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

        public class SalaryClass
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Branch { get; set; }
            public string? Job { get; set; }
            public string? Salary { get; set; }
            public string? Houseing { get; set; }
            public string? Depart { get; set; }
            public string? Nature { get; set; }
            public string? Transmission { get; set; }
            public string? Added { get; set; }
            public string? Reward { get; set; }
            public string? TargetCommission { get; set; }
            public string? ExternalCommission { get; set; }
            public string? TotalEntitlements { get; set; }
            public string? Abcence { get; set; }
            public string? Late { get; set; }
            public string? RepeatLate { get; set; }
            public string? Deficit { get; set; }
            public string? Ancestor { get; set; }
            public string? Permission { get; set; }
            public string? Penalty { get; set; }
            public string? Phone { get; set; }
            public string? Tax { get; set; }
            public string? Insurance { get; set; }
            public string? CompanyInsurance { get; set; }
            public string? Social { get; set; }
            public string? Box { get; set; }
            public string? TotalDeductions { get; set; }
            public string? Total { get; set; }

            public int LateH { get; set; }
            public int LateM { get; set; }
            public int LateS { get; set; }
            public int OTH { get; set; }
            public int OTM { get; set; }
            public int OTS { get; set; }

            public bool ExemptLate { get; set; }
            public bool ExemptEarlyLeave { get; set; }
            public bool ExemptOvertime { get; set; }
            public bool ExemptAbsence { get; set; }
            public bool ExemptEarlyEnter { get; set; }
        }

        private class SalaryData
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Branch { get; set; }
            public decimal MinSalary { get; set; }
            public int SalaryType { get; set; }
            public string Job { get; set; }
            public bool Day1 { get; set; }
            public bool Day2 { get; set; }
            public bool Day3 { get; set; }
            public bool Day4 { get; set; }
            public bool Day5 { get; set; }
            public bool Day6 { get; set; }
            public bool Day7 { get; set; }
            public decimal Salary { get; set; }
            public decimal Houseing { get; set; }
            public decimal Transmission { get; set; }
            public decimal Reward { get; set; }
            public decimal TargetCommission { get; set; }
            public decimal ExternalCommission { get; set; }
            public decimal TotalEntitlements { get; set; }
            public decimal Abcence { get; set; }
            public decimal RepeatLate { get; set; }
            public decimal Ancestor { get; set; }
            public decimal Penalty { get; set; }
            public decimal Phone { get; set; }
            public decimal PermissionValue { get; set; }
            public decimal Deficit { get; set; }
            public decimal Tax { get; set; }
            public decimal Insurance { get; set; }
            public decimal CompanyInsurance { get; set; }
            public decimal Social { get; set; }
            public decimal Box { get; set; }
            public decimal Nature { get; set; }
            public decimal Depart { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal Total { get; set; }

            public bool ExemptLate { get; set; }
            public bool ExemptEarlyLeave { get; set; }
            public bool ExemptOvertime { get; set; }
            public bool ExemptAbsence { get; set; }
            public bool ExemptEarlyEnter { get; set; }
        }

        private async Task GetDataSalary()
        {
            try
            {
                // مسح القائمة القديمة أولاً
                await Dispatcher.InvokeAsync(() =>
                {
                    employeeSalaryCollection.Clear();
                    employeeSalaryList.Clear();
                });


                var filters = new Dictionary<string, string>();

                // بناء الفلاتر كما في كودك
                string name = name_box.Text?.Trim();
                string code = code_box.Text?.Trim();
                string job = (jobComboBox.SelectedItem != null) ?
                    jobs[jobComboBox.SelectedItem.ToString()].ToString() : "";
                string branch = (branchComboBox.SelectedItem != null) ?
                    branches[branchComboBox.SelectedItem.ToString()].ToString() : "";
                string depart = (departComboBox.SelectedItem != null) ?
                    depatrs[departComboBox.SelectedItem.ToString()].ToString() : "";

                if (!string.IsNullOrEmpty(code))
                    filters.Add("code", code);
                if (!string.IsNullOrEmpty(name))
                    filters.Add("name", name);
                if (!string.IsNullOrEmpty(job))
                    filters.Add("job", job);
                if (!string.IsNullOrEmpty(branch))
                    filters.Add("branch", branch);
                if (!string.IsNullOrEmpty(depart))
                    filters.Add("depart", depart);

                // الحصول على تواريخ الشهر
                int monthNumber = DateTime.ParseExact(monthComboBox.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                int year = Convert.ToInt16(yearComboBox.Text);
                (DateTime startMonth, DateTime endMonth) = GetCustomMonthDates(monthNumber, year);

                // جلب البيانات
                var salaryData = await BuildQueryAsync(filters, startMonth, endMonth);

                // معالجة كل موظف بشكل منفصل
                foreach (var data in salaryData)
                {
                    // إعادة تعيين المتغيرات لكل موظف
                    ResetVariables();
                    weekHoli.Clear();
                    weekHoli.Add(data.Day1 ? 1 : 0);
                    weekHoli.Add(data.Day2 ? 1 : 0);
                    weekHoli.Add(data.Day3 ? 1 : 0);
                    weekHoli.Add(data.Day4 ? 1 : 0);
                    weekHoli.Add(data.Day5 ? 1 : 0);
                    weekHoli.Add(data.Day6 ? 1 : 0);
                    weekHoli.Add(data.Day7 ? 1 : 0);
                    minSalary = data.MinSalary;

                    await LoadFromAttendance(int.Parse(data.Code), data.ExemptLate, data.ExemptOvertime);

                    decimal absence = 0;
                    if (!data.ExemptAbsence)
                    {
                        absence = (decimal)data.Abcence * Abcense;
                    }
                    decimal lateRepeat = 0;
                    if (!data.ExemptLate)
                    {
                        lateRepeat = ((int)((decimal)LateRepeat) /
                            (Properties.Settings.Default.LateRepeat == 0 ? 1 :
                             Properties.Settings.Default.LateRepeat)) * Properties.Settings.Default.LateValue;
                    }
                    decimal deductions = (decimal)data.TotalDeductions + LateValue + absence + lateRepeat;
                    decimal entitlements = (decimal)data.TotalEntitlements + OTValue;
                    minSalary = (decimal)data.MinSalary;

                    bool isMonthlyHour = (data.SalaryType == 3);
                    double salary = isMonthlyHour ?
                        (double)data.Salary * (totalWHHours + (totalWHMin / 60.0)) :
                        (double)data.Salary;

                    employeeSalaryList.Add(new SalaryClass
                    {
                        Code = data.Code,
                        Name = data.Name,
                        Branch = data.Branch,
                        Job = data.Job,
                        Salary = salary.ToString("N2"),
                        Houseing = ((double)data.Houseing).ToString("N2"),
                        Depart = ((double)data.Depart).ToString("N2"),
                        Nature = ((double)data.Nature).ToString("N2"),
                        Transmission = ((double)data.Transmission).ToString("N2"),
                        Added = OTValue.ToString("N2"),
                        Reward = ((double)data.Reward).ToString("N2"),
                        TargetCommission = ((double)data.TargetCommission).ToString("N2"),
                        ExternalCommission = ((double)data.ExternalCommission).ToString("N2"),
                        TotalEntitlements = entitlements.ToString("N2"),
                        Abcence = absence.ToString("N2"),
                        Late = LateValue.ToString("N2"),
                        RepeatLate = lateRepeat.ToString("N2"),
                        Ancestor = ((double)data.Ancestor).ToString("N2"),
                        Permission = ((double)data.PermissionValue).ToString("N2"),
                        Penalty = ((double)data.Penalty).ToString("N2"),
                        Phone = ((double)data.Phone).ToString("N2"),
                        Deficit = ((double)data.Deficit).ToString("N2"),
                        Tax = ((double)data.Tax).ToString("N2"),
                        Insurance = ((double)data.Insurance).ToString("N2"),
                        CompanyInsurance = ((double)data.CompanyInsurance).ToString("N2"),
                        Social = ((double)data.Social).ToString("N2"),
                        Box = ((double)data.Box).ToString("N2"),
                        TotalDeductions = deductions.ToString("N2"),
                        OTH = totalOvertimeHours,
                        OTM = totalOvertimeMin,
                        OTS = totalOvertimeSec,
                        LateH = totalLateHours,
                        LateM = totalLateMin,
                        LateS = totalLateSec,
                        Total = (entitlements - deductions).ToString("N2"),
                        ExemptLate = data.ExemptLate,
                        ExemptOvertime = data.ExemptOvertime,
                        ExemptAbsence = data.ExemptAbsence
                    });
                }

                foreach (var item in employeeSalaryList)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        employeeSalaryCollection.Add(item);
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ResetVariables()
        {
            OTHours = 0;
            Abcense = 0;
            LateValue = 0;
            OTValue = 0;
            LateRepeat = 0;
            totalLateHours = 0;
            totalLateMin = 0;
            totalOvertimeHours = 0;
            totalOvertimeMin = 0;
            totalOvertimeSec = 0;
            totalLateSec = 0;
            totalWHHours = 0;
            totalWHMin = 0;
            totalWHSec = 0;
        }

        private async void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // إظهار مؤشر تحميل
                data_load_btn.IsEnabled = false;
                data_load_btn.Content = "جاري التحميل...";

                // تنفيذ العملية بشكل متزامن
                await GetDataSalary();


            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                data_load_btn.IsEnabled = true;
                data_load_btn.Content = "تحميل البيانات";
            }
        }

        public void ExportDataGridToExcel()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "ExportedData"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Sheet1");

                        var headers = new[]
                        {
                            "الكود", "الاسم", "الفرع", "الوظيفة", "الراتب", "بدل سكن", "بدل انتقال",
                            "بدل ادارة", "بدل طبيعة عمل", "اضافي", "مكافآت", "عمولات تحقيق", "عمولات خارجية", "اجمالي الاستحقاقات",
                            "الغياب", "التأخير", "سلف", "جزاء", "ضريبة", "تأمين", "صندوق الزمالة",
                            "مشاركة اجتماعي", "اجمالي الاستقطاعات", "الاجمالي"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Font.FontColor = XLColor.Black;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        }

                        for (int i = 0; i < employeeSalaryList.Count; i++)
                        {
                            var item = employeeSalaryList[i];
                            worksheet.Cell(i + 2, 1).Value = item.Code;
                            worksheet.Cell(i + 2, 2).Value = item.Name;
                            worksheet.Cell(i + 2, 3).Value = item.Branch;
                            worksheet.Cell(i + 2, 4).Value = item.Job;
                            worksheet.Cell(i + 2, 5).Value = item.Salary;
                            worksheet.Cell(i + 2, 6).Value = item.Houseing;
                            worksheet.Cell(i + 2, 7).Value = item.Transmission;
                            worksheet.Cell(i + 2, 8).Value = item.Depart;
                            worksheet.Cell(i + 2, 9).Value = item.Nature;
                            worksheet.Cell(i + 2, 10).Value = item.Added;
                            worksheet.Cell(i + 2, 11).Value = item.Reward;
                            worksheet.Cell(i + 2, 12).Value = item.TargetCommission;
                            worksheet.Cell(i + 2, 13).Value = item.ExternalCommission;
                            worksheet.Cell(i + 2, 14).Value = item.TotalEntitlements;
                            worksheet.Cell(i + 2, 15).Value = item.Abcence;
                            worksheet.Cell(i + 2, 16).Value = item.Late;
                            worksheet.Cell(i + 2, 17).Value = item.Ancestor;
                            worksheet.Cell(i + 2, 18).Value = item.Penalty;
                            worksheet.Cell(i + 2, 19).Value = item.Phone;
                            worksheet.Cell(i + 2, 20).Value = item.Tax;
                            worksheet.Cell(i + 2, 21).Value = item.Insurance;
                            worksheet.Cell(i + 2, 22).Value = item.Box;
                            worksheet.Cell(i + 2, 23).Value = item.Social;
                            worksheet.Cell(i + 2, 24).Value = item.TotalDeductions;
                            worksheet.Cell(i + 2, 25).Value = item.Total;
                        }

                        workbook.SaveAs(filePath);
                        MessageBox.Show("تم استخراج الاكسيل!");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "خطأ");
                }
            }
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGridToExcel();
        }

        public FlowDocument CreateDocument(List<SalaryClass> records, string year, string month)
        {
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(30);
            document.ColumnWidth = 500;
            document.FlowDirection = System.Windows.FlowDirection.RightToLeft;

            // Create a Table for the header with two columns: one for the image and one for text
            System.Windows.Documents.Table headerTable = new System.Windows.Documents.Table();
            headerTable.Columns.Add(new System.Windows.Documents.TableColumn());
            headerTable.Columns.Add(new System.Windows.Documents.TableColumn() { Width = new GridLength(15, GridUnitType.Star) });
            headerTable.CellSpacing = 0;
            headerTable.BorderThickness = new Thickness(0);
            var headerRowGroup1 = new TableRowGroup();
            System.Windows.Documents.TableRow headerRow1 = new System.Windows.Documents.TableRow();

            var cell6 = CreateCell($"صافي مرتبات شهر : {month} - {year}", false, true);
            cell6.FontSize = 14;
            cell6.FontWeight = FontWeights.Bold;
            cell6.BorderThickness = new Thickness(0);
            cell6.Background = System.Windows.Media.Brushes.White;

            headerRow1.Cells.Add(cell6);

            // Add Image to the first cell
            var image = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/Back.jfif", UriKind.RelativeOrAbsolute)),
                Width = 80,
                Height = 80,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
            };
            var imageContainer = new BlockUIContainer(image);
            var imageCell = new System.Windows.Documents.TableCell(imageContainer);
            imageCell.BorderThickness = new Thickness(0);
            headerRow1.Cells.Add(imageCell);

            headerRowGroup1.Rows.Add(headerRow1);
            headerTable.RowGroups.Add(headerRowGroup1);
            document.Blocks.Add(headerTable);

            var titleParagraph = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"تقرير مرتبات تفصيلي"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = System.Windows.Media.Brushes.Red,
                TextAlignment = System.Windows.TextAlignment.Center
            };

            document.Blocks.Add(titleParagraph);

            // Create Data Table
            Table table = new Table
            {
                CellSpacing = 0,
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black
            };

            // Define table columns
            table.Columns.Add(new TableColumn { Width = GridLength.Auto });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });

            // Table Header
            TableRowGroup headerRowGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Background = System.Windows.Media.Brushes.LightGray;
            headerRow.FontSize = 13;
            headerRow.FontWeight = FontWeights.Bold;

            headerRow.Cells.Add(CreateCell("الاسم", true, false));
            headerRow.Cells.Add(CreateCell("الفرع", true, false));
            headerRow.Cells.Add(CreateCell("الوظيفة", true, false));
            headerRow.Cells.Add(CreateCell("الراتب", true, false));
            headerRow.Cells.Add(CreateCell("اجمالي الاستحقاقات", true, false));
            headerRow.Cells.Add(CreateCell("اجمالي الاستقطاعات", true, false));
            headerRow.Cells.Add(CreateCell("صافي الراتب", true, false));

            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            // Populate table with data
            TableRowGroup dataRowGroup = new TableRowGroup();
            foreach (var record in records)
            {
                System.Windows.Documents.TableRow dataRow = new System.Windows.Documents.TableRow();

                dataRow.Cells.Add(CreateCell(record.Name, false, true));
                dataRow.Cells.Add(CreateCell(record.Branch, false, true));
                dataRow.Cells.Add(CreateCell(record.Job, false, true));
                dataRow.Cells.Add(CreateCell(record.Salary, false, false));
                dataRow.Cells.Add(CreateCell(record.TotalEntitlements, false, false));
                dataRow.Cells.Add(CreateCell(record.TotalDeductions, false, false));
                dataRow.Cells.Add(CreateCell(record.Total, false, false));

                dataRowGroup.Rows.Add(dataRow);
            }
            table.RowGroups.Add(dataRowGroup);

            // Add table to document
            document.Blocks.Add(table);

            return document;
        }

        public static FlowDocument CreateEmployeeDocument(List<SalaryClass> employees)
        {
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 559,
                PageHeight = 794,
                ColumnWidth = 500,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                PagePadding = new Thickness(10)
            };

            foreach (var employee in employees)
            {
                // Create a new section for each employee and ensure page break
                Section employeeSection = new Section
                {
                    BreakPageBefore = true
                };

                employeeSection.Blocks.Add(CreateEmployeeTable(employee));
                doc.Blocks.Add(employeeSection);
            }

            return doc;
        }

        private static string ForamttedTime(int hour, int min)
        {
            string hourS = hour.ToString();
            string minS = min.ToString();
            hourS = (hourS.Length < 2) ? "0" + hourS : hourS;
            minS = (minS.Length < 2) ? "0" + minS : minS;
            return hourS + ":" + minS;
        }

        private static Table CreateEmployeeTable(SalaryClass employee)
        {
            Table table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn());
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn());

            // Employee Name and Code
            table.RowGroups.Add(new TableRowGroup());
            var headerRow = new TableRow();
            headerRow.Cells.Add(CreateCell("الاسم:", true, 1));
            headerRow.Cells.Add(CreateCell(employee.Name, false, 3));
            table.RowGroups[0].Rows.Add(headerRow);

            headerRow = new TableRow();
            headerRow.Cells.Add(CreateCell("الكود:", true, 1));
            headerRow.Cells.Add(CreateCell(employee.Code, false, 3));
            table.RowGroups[0].Rows.Add(headerRow);

            // Section Headers
            var sectionHeaderRow = new TableRow();
            sectionHeaderRow.Cells.Add(CreateCell("الاستحقاقات", true, 2));
            sectionHeaderRow.Cells.Add(CreateCell("الاستقطاعات", true, 2));
            table.RowGroups[0].Rows.Add(sectionHeaderRow);

            int hours = employee.LateH;
            int min = employee.LateM;
            int sec = employee.LateS;

            while (sec >= 60)
            {
                min++;
                sec -= 60;
            }
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string AllLate = ForamttedTime(hours, min);

            hours = employee.OTH;
            min = employee.OTM;
            sec = employee.OTS;

            while (sec >= 60)
            {
                min++;
                sec -= 60;
            }
            while (min >= 60)
            {
                hours++;
                min -= 60;
            }
            string AllOT = ForamttedTime(hours, min);

            // Salary Details
            table.RowGroups[0].Rows.Add(CreateDataRow("المرتب:", employee.Salary, "ض كسب عمل:", employee.Tax));
            table.RowGroups[0].Rows.Add(CreateDataRow("بدل سكن:", employee.Houseing, "تأمينات الموظف:", employee.Insurance));
            table.RowGroups[0].Rows.Add(CreateDataRow("بدل ادارة:", employee.Depart, "مشاركة اجتماعية:", employee.Social));
            table.RowGroups[0].Rows.Add(CreateDataRow("بدل طبيعة عمل:", employee.Nature, "ص الزمالة:", employee.Box));
            table.RowGroups[0].Rows.Add(CreateDataRow("بدل انتقال:", employee.Transmission, "جزاء:", employee.Penalty));
            table.RowGroups[0].Rows.Add(CreateDataRow("اضافي:", AllOT, "سلف:", employee.Ancestor));
            table.RowGroups[0].Rows.Add(CreateDataRow("قيمة اضافي:", employee.Added, "عجز:", employee.Deficit));
            table.RowGroups[0].Rows.Add(CreateDataRow("مكافآت:", employee.Reward, "الغياب:", Abcense.ToString()));
            table.RowGroups[0].Rows.Add(CreateDataRow("عمولات تحقيق", employee.TargetCommission, "قيمة الغياب:", employee.Abcence));
            table.RowGroups[0].Rows.Add(CreateDataRow("عمولات خارجية", employee.ExternalCommission, "التأخير:", AllLate));
            table.RowGroups[0].Rows.Add(CreateDataRow("", "", "قيمة التأخير:", employee.Late));
            table.RowGroups[0].Rows.Add(CreateDataRow("", "", "تأمين الشركة:", employee.CompanyInsurance));
            table.RowGroups[0].Rows.Add(CreateDataRow("", "", "اذونات:", employee.Permission));
            table.RowGroups[0].Rows.Add(CreateDataRow("", "", "فاتورة تليقون:", employee.Phone));

            // Final Salary Row
            var totalRow = new TableRow();
            totalRow.Cells.Add(CreateCell("صافي الراتب:", true, 2));
            totalRow.Cells.Add(CreateCell(employee.Total, false, 2));
            table.RowGroups[0].Rows.Add(totalRow);

            return table;
        }

        private static TableRow CreateDataRow(string label1, string value1, string label2, string value2)
        {
            var row = new TableRow();
            row.Cells.Add(CreateCell(label1, true));
            row.Cells.Add(CreateCell(value1, false));
            row.Cells.Add(CreateCell(label2, true));
            row.Cells.Add(CreateCell(value2, false));
            return row;
        }

        private static TableCell CreateCell(string text, bool isHeader, int columnSpan = 1)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                ColumnSpan = columnSpan,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(2),
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black
            };
            if (isHeader)
            {
                cell.FontWeight = FontWeights.Bold;
            }
            return cell;
        }

        private string ConvertToArabicNumerals(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            string arabicNumerals = "٠١٢٣٤٥٦٧٨٩";
            string westernNumerals = "0123456789";

            return new string(input.Select(c =>
                westernNumerals.Contains(c) ? arabicNumerals[westernNumerals.IndexOf(c)] : c
            ).ToArray());
        }

        private System.Windows.Documents.TableCell CreateCell(string content, bool isHeader, bool isArabic)
        {
            var cell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(content)));
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.BorderThickness = new Thickness(1);
            cell.Padding = new Thickness(1);
            cell.FontSize = 12;
            if (isHeader)
            {
                cell.Background = System.Windows.Media.Brushes.Black;
                cell.Foreground = System.Windows.Media.Brushes.White;
            }
            if (isArabic)
            {
                cell.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                cell.TextAlignment = System.Windows.TextAlignment.Center;
            }
            return cell;
        }

        private void PrintDocument(FlowDocument document)
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Attendance Report");
            }
        }

        private void print_btn_Click(object sender, RoutedEventArgs e)
        {
            string year = yearComboBox.Text;
            string month = monthComboBox.Text;
            FlowDocument document = CreateEmployeeDocument(employeeSalaryList);
            MonthDataReport monthDataReport = new MonthDataReport(document);
            monthDataReport.Show();
        }
    }
}