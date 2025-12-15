using ClosedXML.Excel;
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    public partial class EmployeeData : Window
    {
        private ObservableCollection<Employee> employeeList = new ObservableCollection<Employee>();
        private Dictionary<string, int> branches = new Dictionary<string, int>();
        private Dictionary<string, int> _insured = new Dictionary<string, int>();
        private Dictionary<string, bool> _inDuties = new Dictionary<string, bool>();
        private Dictionary<string, int> jobs = new Dictionary<string, int>();
        private Dictionary<string, int> departments = new Dictionary<string, int>();
        private Dictionary<string, int> degrees = new Dictionary<string, int>();
        private Dictionary<string, int> shifts = new Dictionary<string, int>();
        private Dictionary<string, int> breaks = new Dictionary<string, int>();
        private Dictionary<string, int> weekHolidays = new Dictionary<string, int>();
        private Dictionary<string, int> jobTypes = new Dictionary<string, int>();

        private AppDbContext _context;

        public EmployeeData()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            LoadData();
        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        private void LoadData()
        {
            try
            {
                branches.Clear();
                _inDuties.Clear();
                jobs.Clear();
                _insured.Clear();
                departments.Clear();
                degrees.Clear();
                shifts.Clear();
                breaks.Clear();
                weekHolidays.Clear();
                jobTypes.Clear();

                // Load branches
                branchComboBox.Items.Clear();
                var branchesFromDb = _context.Branches.ToList();
                foreach (var branch in branchesFromDb)
                {
                    branchComboBox.Items.Add(branch.Name);
                    branches.Add(branch.Name, branch.Id);
                }

                // Load departments
                var departmentsFromDb = _context.Departments.ToList();
                foreach (var dept in departmentsFromDb)
                {
                    departments.Add(dept.Name, dept.Id);
                }

                // Load jobs
                jobComboBox.Items.Clear();
                var jobsFromDb = _context.JobTitles.ToList();
                foreach (var job in jobsFromDb)
                {
                    jobComboBox.Items.Add(job.Name);
                    jobs.Add(job.Name, job.Id);
                }

                // Load degrees
                var degreesFromDb = _context.Degrees.ToList();
                foreach (var degree in degreesFromDb)
                {
                    degrees.Add(degree.Name, degree.Id);
                }

                // Load shifts
                var shiftsFromDb = _context.Shifts.ToList();
                foreach (var shift in shiftsFromDb)
                {
                    shifts.Add(shift.Name, shift.Id);
                }

                // Load breaks
                var breaksFromDb = _context.Breaks.ToList();
                foreach (var brk in breaksFromDb)
                {
                    breaks.Add(brk.Name, brk.Id);
                }

                // Load week holidays
                var weekHolidaysFromDb = _context.WeekHolidays.ToList();
                foreach (var holiday in weekHolidaysFromDb)
                {
                    weekHolidays.Add(holiday.Name, holiday.Id);
                }

                // Load job types
                var jobTypesFromDb = _context.JobTypes.ToList();
                foreach (var jobType in jobTypesFromDb)
                {
                    jobTypes.Add(jobType.Name, jobType.Id);
                }

                // Load insured options
                insuredComboBox.Items.Clear();
                insuredComboBox.Items.Add("نعم");
                insuredComboBox.Items.Add("لا");
                _insured.Add("نعم", 1);
                _insured.Add("لا", 0);

                // Load inDuty options
                inDutyComboBox.Items.Clear();
                inDutyComboBox.Items.Add("نعم");
                inDutyComboBox.Items.Add("لا");
                _inDuties.Add("نعم", true);
                _inDuties.Add("لا", false);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void ExportDataGridToExcel()
        {
            if (employeeList.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"بيانات_العاملين_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("بيانات العاملين");

                        // Create headers
                        var headers = new[]
                        {
                            "م", "الكود", "الاسم", "الوظيفة", "الفرع", "الإدارة",
                            "الرقم القومي", "النوع", "تاريخ الميلاد", "العمر",
                            "تاريخ التعيين", "تاريخ انتهاء العمل", "العنوان",
                            "المرتب", "رصيد الإجازات", "مؤمن عليه", "الهاتف",
                            "البريد الإلكتروني", "الرقم التأميني", "التأمين الصحي",
                            "في الخدمة", "تحت التدريب", "تحت التوظيف", "القائمة السوداء"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Font.FontColor = XLColor.Black;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        // Populate data
                        for (int i = 0; i < employeeList.Count; i++)
                        {
                            var item = employeeList[i];

                            worksheet.Cell(i + 2, 1).Value = item.RowNumber;
                            worksheet.Cell(i + 2, 2).Value = item.Code;
                            worksheet.Cell(i + 2, 3).Value = item.Name;
                            worksheet.Cell(i + 2, 4).Value = item.Job;
                            worksheet.Cell(i + 2, 5).Value = item.Br;
                            worksheet.Cell(i + 2, 6).Value = item.Department;
                            worksheet.Cell(i + 2, 7).Value = item.CardNo;
                            worksheet.Cell(i + 2, 8).Value = item.Gender;
                            worksheet.Cell(i + 2, 9).Value = item.BirthD?.ToString("dd/MM/yyyy") ?? "";
                            worksheet.Cell(i + 2, 10).Value = item.Age;
                            worksheet.Cell(i + 2, 11).Value = item.DateT.ToString("dd/MM/yyyy");
                            worksheet.Cell(i + 2, 12).Value = item.EndDate?.ToString("dd/MM/yyyy") ?? "";
                            worksheet.Cell(i + 2, 13).Value = item.Address;
                            worksheet.Cell(i + 2, 14).Value = item.Salary;
                            worksheet.Cell(i + 2, 15).Value = item.HolidayBalance;
                            worksheet.Cell(i + 2, 16).Value = item.Insured;
                            worksheet.Cell(i + 2, 17).Value = item.Phone;
                            worksheet.Cell(i + 2, 18).Value = item.Email;
                            worksheet.Cell(i + 2, 19).Value = item.SSN;
                            worksheet.Cell(i + 2, 20).Value = item.HealthInsurance;
                            worksheet.Cell(i + 2, 21).Value = item.InDuty;
                            worksheet.Cell(i + 2, 22).Value = item.UnderTraining;
                            worksheet.Cell(i + 2, 23).Value = item.UnderEmployment;
                            worksheet.Cell(i + 2, 24).Value = item.Blacklist;

                            // Format salary column
                            if (item.Salary > 0)
                            {
                                worksheet.Cell(i + 2, 14).Style.NumberFormat.Format = "#,##0";
                            }
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        // Add totals row
                        var totalRow = employeeList.Count + 2;
                        worksheet.Cell(totalRow, 1).Value = "الإجمالي:";
                        worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        // Calculate total salary
                        var totalSalary = employeeList.Sum(e => e.Salary);
                        if (totalSalary > 0)
                        {
                            worksheet.Cell(totalRow, 14).Value = totalSalary;
                            worksheet.Cell(totalRow, 14).Style.Font.Bold = true;
                            worksheet.Cell(totalRow, 14).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(totalRow, 14).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }

                        worksheet.Cell(totalRow, 2).Value = employeeList.Count;
                        worksheet.Cell(totalRow, 2).Style.Font.Bold = true;

                        // Save the workbook
                        workbook.SaveAs(filePath);
                        MessageBox.Show($"تم تصدير {employeeList.Count} سجل بنجاح إلى: {filePath}", "نجاح",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"خطأ في التصدير: {e.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void dateEnabled(object sender, RoutedEventArgs e)
        {
            from_picker.IsEnabled = true;
            to_picker.IsEnabled = true;
        }

        private void dateUnenabled(object sender, RoutedEventArgs e)
        {
            from_picker.IsEnabled = false;
            to_picker.IsEnabled = false;
            from_picker.SelectedDate = null;
            to_picker.SelectedDate = null;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private IQueryable<User> BuildQuery(Dictionary<string, string> filters)
        {
            var query = _context.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Include(u => u.Degree)
                .Include(u => u.Shift)
                .Include(u => u.Break)
                .Include(u => u.WeekHoliday)
                .Include(u => u.JobType)
                .AsQueryable();

            if (filters.ContainsKey("code"))
            {
                query = query.Where(u => u.Id.ToString() == filters["code"]);
            }

            if (filters.ContainsKey("cardNo"))
            {
                query = query.Where(u => u.NationalID.Contains(filters["cardNo"]));
            }

            if (filters.ContainsKey("name"))
            {
                query = query.Where(u => u.FullName.Contains(filters["name"]));
            }

            if (filters.ContainsKey("insured"))
            {
                query = query.Where(u => u.IsInsured == (filters["insured"] == "1"));
            }

            if (filters.ContainsKey("inDuty"))
            {
                query = query.Where(u => u.InDuty == _inDuties[filters["inDuty"]]);
            }

            if (filters.ContainsKey("job"))
            {
                int jobCode = int.Parse(filters["job"]);
                query = query.Where(u => u.JobTitleId == jobCode);
            }

            if (filters.ContainsKey("holidayBalance"))
            {
                int holidayBalance = int.Parse(filters["holidayBalance"]);
                query = query.Where(u => u.HolidayBalance == holidayBalance);
            }

            if (filters.ContainsKey("branch"))
            {
                int branchCode = int.Parse(filters["branch"]);
                query = query.Where(u => u.BranchId == branchCode);
            }

            if (filters.ContainsKey("fromDate"))
            {
                DateOnly fromDate = DateOnly.Parse(filters["fromDate"]);
                query = query.Where(u => u.HireDate >= fromDate);
            }

            if (filters.ContainsKey("toDate"))
            {
                DateOnly toDate = DateOnly.Parse(filters["toDate"]);
                query = query.Where(u => u.HireDate <= toDate);
            }

            if (filters.ContainsKey("endFromDate"))
            {
                DateOnly endFromDate = DateOnly.Parse(filters["endFromDate"]);
                query = query.Where(u => u.FinishJob >= endFromDate);
            }

            if (filters.ContainsKey("endToDate"))
            {
                DateOnly endToDate = DateOnly.Parse(filters["endToDate"]);
                query = query.Where(u => u.FinishJob <= endToDate);
            }

            if (filters.ContainsKey("birthDate"))
            {
                DateOnly birthDate = DateOnly.Parse(filters["birthDate"]);
                query = query.Where(u => u.BirthDate == birthDate);
            }

            // إضافة فلاتر إضافية
            if (filters.ContainsKey("phone"))
            {
                query = query.Where(u => u.PhoneNumber.Contains(filters["phone"]));
            }

            if (filters.ContainsKey("email"))
            {
                query = query.Where(u => u.Email.Contains(filters["email"]));
            }

            if (filters.ContainsKey("ssn"))
            {
                query = query.Where(u => u.SSN.Contains(filters["ssn"]));
            }

            if (filters.ContainsKey("healthInsurance"))
            {
                query = query.Where(u => u.HealthInsuranceNumber.Contains(filters["healthInsurance"]));
            }

            return query.OrderBy(u => u.Id);
        }

        private void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            var filters = new Dictionary<string, string>();
            employeeList.Clear();

            string name = name_box.Text;
            string code = code_box.Text;
            string cardNo = cardId_box.Text;
            string holidayBalance = holiday_balance_box.Text;
            string job = (jobComboBox.SelectedItem != null) ? jobs[jobComboBox.SelectedItem.ToString()].ToString() : "";
            string branch = (branchComboBox.SelectedItem != null) ? branches[branchComboBox.SelectedItem.ToString()].ToString() : "";
            string insured = (insuredComboBox.SelectedItem != null) ? _insured[insuredComboBox.SelectedItem.ToString()].ToString() : "";
            string inDuty = (inDutyComboBox.SelectedItem != null) ? inDutyComboBox.SelectedItem.ToString() : "";

            if (!string.IsNullOrEmpty(code))
            {
                filters.Add("code", code);
            }
            if (!string.IsNullOrEmpty(cardNo))
            {
                filters.Add("cardNo", cardNo);
            }
            if (!string.IsNullOrEmpty(holidayBalance))
            {
                filters.Add("holidayBalance", holidayBalance);
            }
            if (!string.IsNullOrEmpty(name))
            {
                filters.Add("name", name);
            }
            if (!string.IsNullOrEmpty(job))
            {
                filters.Add("job", job);
            }
            if (!string.IsNullOrEmpty(branch))
            {
                filters.Add("branch", branch);
            }
            if (!string.IsNullOrEmpty(insured))
            {
                filters.Add("insured", insured);
            }
            if (!string.IsNullOrEmpty(inDuty))
            {
                filters.Add("inDuty", inDuty);
            }
            if (from_picker.SelectedDate.HasValue && to_picker.SelectedDate.HasValue)
            {
                filters.Add("fromDate", from_picker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                filters.Add("toDate", to_picker.SelectedDate.Value.ToString("yyyy-MM-dd"));
            }
            if (end_from_picker.SelectedDate.HasValue && end_to_picker.SelectedDate.HasValue)
            {
                filters.Add("endFromDate", end_from_picker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                filters.Add("endToDate", end_to_picker.SelectedDate.Value.ToString("yyyy-MM-dd"));
            }
            if (birth_picker.SelectedDate.HasValue)
            {
                filters.Add("birthDate", birth_picker.SelectedDate.Value.ToString("yyyy-MM-dd"));
            }

            try
            {
                var query = BuildQuery(filters);
                var users = query.ToList();

                int rowNumber = 1;

                foreach (var user in users)
                {
                    employeeList.Add(new Employee
                    {
                        RowNumber = rowNumber++,
                        Name = user.FullName,
                        Address = user.Address,
                        Code = user.Id,
                        HolidayBalance = user.HolidayBalance,
                        CardNo = user.NationalID,
                        Job = GetKeyByValue(jobs, user.JobTitleId),
                        Department = GetKeyByValue(departments, user.DepartmentId),
                        Degree = GetKeyByValue(degrees, user.DegreeId),
                        Salary = user.MainSalary,
                        Gender = (user.Gender == 'M') ? "ذكر" : "انثى",
                        Insured = user.IsInsured ? "نعم" : "لا",
                        DateT = user.HireDate,
                        EndDate = user.FinishJob,
                        BirthD = user.BirthDate,
                        Br = GetKeyByValue(branches, user.BranchId),
                        Phone = user.PhoneNumber,
                        Email = user.Email,
                        SSN = user.SSN,
                        HealthInsurance = user.HealthInsuranceNumber,
                        InDuty = user.InDuty ? "نعم" : "لا",
                        UnderTraining = user.UnderTraining ? "نعم" : "لا",
                        UnderEmployment = user.UnderEmployment ? "نعم" : "لا",
                        Blacklist = user.Blacklist ? "نعم" : "لا",
                        Shift = GetKeyByValue(shifts, user.ShiftId),
                        Break = GetKeyByValue(breaks, user.BreakId),
                        WeekHoliday = GetKeyByValue(weekHolidays, user.WeekHolidayId),
                        JobType = GetKeyByValue(jobTypes, user.JobTypeId),
                        Age = CalculateAge(user.BirthDate),
                        WorkHours = user.WorkHours.ToString(@"hh\:mm") ?? "",
                        IsArchived = user.IsArchived,
                        IsUser = user.IsUser,
                        IsMobileUser = user.IsMobileUser ?? false,
                        ExemptLate = user.ExemptLate,
                        ExemptEarlyLeave = user.ExemptEarlyLeave,
                        ExemptOvertime = user.ExemptOvertime,
                        ExemptAbsence = user.ExemptAbsence,
                        BlacklistReason = user.BlacklistReason,
                        ArmyCertificateNumber = user.ArmyCertificateNumber,
                        ArmyCertificateExpiration = user.ArmyCertificateExpiration,
                        NationalIDExpiration = user.NationalIDExpiration,
                        DriverLicenseExpiration = user.DriverLicenseExpiration,
                        VehicleLicenseExpiration = user.VehicleLicenseExpiration,
                        UserId = user.Id // لحفظ المعرف الأساسي
                    });
                }

                list.ItemsSource = employeeList;
                txtTotalCount.Text = employeeList.Count.ToString();

                if (employeeList.Count == 0)
                {
                    MessageBox.Show("لا توجد نتائج للبحث", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CalculateAge(DateOnly birthDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            return age;
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGridToExcel();
        }

        private string GetKeyByValue(Dictionary<string, int> dictionary, int? value)
        {
            if (value == null) return "";

            foreach (var kvp in dictionary)
            {
                if (kvp.Value == value)
                {
                    return kvp.Key;
                }
            }
            return "";
        }

        private string GetKeyByValue(Dictionary<string, bool> dictionary, bool? value)
        {
            if (value == null) return "";

            foreach (var kvp in dictionary)
            {
                if (kvp.Value == value)
                {
                    return kvp.Key;
                }
            }
            return "";
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearFilters();
        }

        private void ClearFilters()
        {
            code_box.Clear();
            name_box.Clear();
            cardId_box.Clear();
            holiday_balance_box.Clear();
            jobComboBox.SelectedItem = null;
            branchComboBox.SelectedItem = null;
            inDutyComboBox.SelectedItem = null;
            insuredComboBox.SelectedItem = null;
            birth_picker.SelectedDate = null;
            from_picker.SelectedDate = null;
            end_to_picker.SelectedDate = null;
            end_from_picker.SelectedDate = null;
            to_picker.SelectedDate = null;
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenEmployeeManagementWindow(null);
        }

        private void list_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && dataGrid.SelectedItem is Employee selectedEmployee)
            {
                OpenEmployeeManagementWindow(selectedEmployee);
            }
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق إضافي عند اختيار صف
        }

        private void OpenEmployeeManagementWindow(Employee employee)
        {

            if (employee != null)
            {
                var employeeWindow = new AddEmplo(employee);
                employeeWindow.Owner = this;
                employeeWindow.ShowDialog();

                // تحديث البيانات بعد إغلاق نافذة التعديل
                LoadEmployeeData();
            }

            
        }

        public class Employee
        {
            public int RowNumber { get; set; }
            public string Name { get; set; }
            public string CardNo { get; set; }
            public string Insured { get; set; }
            public string Address { get; set; }
            public string Gender { get; set; }
            public int Code { get; set; }
            public int UserId { get; set; } // لحفظ معرف المستخدم
            public int? HolidayBalance { get; set; }
            public int? Age { get; set; }
            public decimal? Salary { get; set; }
            public string Job { get; set; }
            public string Department { get; set; }
            public string Degree { get; set; }
            public string Shift { get; set; }
            public string Break { get; set; }
            public string WeekHoliday { get; set; }
            public string JobType { get; set; }
            public DateOnly DateT { get; set; }
            public DateOnly? BirthD { get; set; }
            public DateOnly? EndDate { get; set; }
            public string Br { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string SSN { get; set; }
            public string HealthInsurance { get; set; }
            public string InDuty { get; set; }
            public string UnderTraining { get; set; }
            public string UnderEmployment { get; set; }
            public string Blacklist { get; set; }
            public string WorkHours { get; set; }
            public bool IsArchived { get; set; }
            public bool IsUser { get; set; }
            public bool IsMobileUser { get; set; }
            public bool ExemptLate { get; set; }
            public bool ExemptEarlyLeave { get; set; }
            public bool ExemptOvertime { get; set; }
            public bool ExemptAbsence { get; set; }
            public string BlacklistReason { get; set; }
            public string ArmyCertificateNumber { get; set; }
            public DateOnly? ArmyCertificateExpiration { get; set; }
            public DateOnly? NationalIDExpiration { get; set; }
            public DateOnly? DriverLicenseExpiration { get; set; }
            public DateOnly? VehicleLicenseExpiration { get; set; }
        }
    }
}