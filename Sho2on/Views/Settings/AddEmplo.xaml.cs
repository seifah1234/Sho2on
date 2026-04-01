using HR_Application.Classes;
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static HR_Application.EmployeeData;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace HR_Application
{
    public partial class AddEmplo : Window
    {
        private readonly AppDbContext _context;
        private byte[] _profileImageData;
        private bool _isDriver = false;
        private User? _selectedUser = null;
        private Employee _selectedEmployee = null;
        private int _currentEmployee = 0;
        private List<User> users = new List<User>();

        public AddEmplo()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
        }
        public AddEmplo(Employee employee)
        {
            InitializeComponent();
            _selectedEmployee = employee;
            _context = new AppDbContext(App.ConnectionString);
        }

        // دالة جديدة لتعيين بيانات الموظف عند فتح النافذة
        public void SetEmployeeData(Employee employee)
        {
            _selectedEmployee = employee;
            emplo_code_box.Text = employee.Code.ToString();
            search_btn_Click(null, null);
        }

        private async Task LoadData()
        {
            try
            {
                // تحميل البيانات من قاعدة البيانات
                var branches = await _context.Branches.ToListAsync();
                branch_box.ItemsSource = branches;
                branch_box.DisplayMemberPath = "Name";
                branch_box.SelectedValuePath = "Id";

                var departments = await _context.Departments.ToListAsync();
                depart_box.ItemsSource = departments;
                depart_box.DisplayMemberPath = "Name";
                depart_box.SelectedValuePath = "Id";

                var jobs = await _context.JobTitles.ToListAsync();
                job_box.ItemsSource = jobs;
                job_box.DisplayMemberPath = "Name";
                job_box.SelectedValuePath = "Id";

                var degrees = await _context.Degrees.ToListAsync();
                degree_box.ItemsSource = degrees;
                degree_box.DisplayMemberPath = "Name";
                degree_box.SelectedValuePath = "Id";

                var shifts = await _context.Shifts.ToListAsync();
                shift_box.ItemsSource = shifts;
                shift_box.DisplayMemberPath = "Name";
                shift_box.SelectedValuePath = "Id";

                var recidences = Recidence.Recidences();
                recidenceBox.ItemsSource = recidences;

                var insures = Insurance.Insurances();
                insuredComboBox.ItemsSource = insures;

                var maritals = Marital.Maritals();
                maritalBox.ItemsSource = maritals;

                var managers = await _context.Users.Include(u => u.JobTitle).Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value).ToListAsync();
                manager_box.ItemsSource = managers;

                var weekHolidays = await _context.WeekHolidays.ToListAsync();
                week_holi_box.ItemsSource = weekHolidays;
                week_holi_box.DisplayMemberPath = "Name";
                week_holi_box.SelectedValuePath = "Id";

                var qualifications = await _context.Qualifications.ToListAsync();
                qualificationBox.ItemsSource = qualifications;
                qualificationBox.DisplayMemberPath = "Name";
                qualificationBox.SelectedValuePath = "Id";

                var jobTypes = await _context.JobTypes.ToListAsync();
                job_type_box.ItemsSource = jobTypes;
                job_type_box.DisplayMemberPath = "Name";
                job_type_box.SelectedValuePath = "Id";

                // تعيين القيم الافتراضية
                emplo_date_picker.SelectedDate = DateTime.Now;
                inDuty_check.IsChecked = true;

                // تحميل الصورة الافتراضية
                LoadDefaultImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDefaultImage()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/assets/images/avatar.jpg");
                var image = new BitmapImage(uri);
                EmployeeImage.Source = image;
                removeImage.Visibility = Visibility.Collapsed;
                addImage.Visibility = Visibility.Visible;
                _profileImageData = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الصورة الافتراضية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // حدث عند تغيير اختيار الوظيفة
        private async void job_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (job_box.SelectedItem is JobTitle selectedJob)
            {
                // التحقق إذا كانت الوظيفة تتطلب قيادة
                _isDriver = await IsDriverJob(selectedJob.Id);

                // إظهار أو إخفاء قسم معلومات القيادة
                driving_section.Visibility = _isDriver ? Visibility.Visible : Visibility.Collapsed;

                // إذا لم تكن الوظيفة سائق، مسح بيانات القيادة
                if (!_isDriver)
                {
                    ClearDrivingData();
                }
            }
        }

        private async Task<bool> IsDriverJob(int jobId)
        {
            try
            {
                var job = await _context.JobTitles.FindAsync(jobId);
                return job?.IsDriver == true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // حدث عند تفعيل البلاك ليست
        private void blacklist_check_Checked(object sender, RoutedEventArgs e)
        {
            blacklist_notes_box.Visibility = Visibility.Visible;
            blacklist_notes_box.Focus();
        }

        // حدث عند إلغاء البلاك ليست
        private void blacklist_check_Unchecked(object sender, RoutedEventArgs e)
        {
            blacklist_notes_box.Visibility = Visibility.Collapsed;
            blacklist_notes_box.Clear();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                // التحقق من عدم تكرار الكود
                if (_context.Users.Any(u => u.Code == emplo_code_box.Text && branch_box.SelectedValue.ToString() == u.BranchId.ToString()))
                {
                    MessageBox.Show("هذا الكود مستخدم بالفعل. الرجاء استخدام كود مختلف.", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var user = new User
                {
                    // المعلومات الشخصية
                    Code = emplo_code_box.Text,
                    NationalID = emplo_card_box.Text,
                    PhoneNumber = emplo_tele_box.Text,
                    FullName = emplo_name_box.Text,
                    Email = emplo_email_box.Text,
                    Address = emplo_address_box.Text,
                    HireDate = DateOnly.FromDateTime(emplo_date_picker.SelectedDate.Value),
                    BirthDate = DateOnly.FromDateTime(birth_date_picker.SelectedDate.Value),
                    FinishJob = end_date_picker.SelectedDate.HasValue ?
                        DateOnly.FromDateTime(end_date_picker.SelectedDate.Value) : null,
                    Gender = male_box.IsChecked == true ? 'M' : 'F',
                    MaritalId = (int)maritalBox.SelectedValue,
                    ManagerId = (int?)manager_box.SelectedValue,
                    QualificationId = (int?)qualificationBox.SelectedValue,

                    // المعلومات الوظيفية
                    BranchId = (int)branch_box.SelectedValue,
                    DepartmentId = (int)depart_box.SelectedValue,
                    JobTitleId = (int)job_box.SelectedValue,
                    DegreeId = (int)degree_box.SelectedValue,
                    ShiftId = (int)shift_box.SelectedValue,
                    WeekHolidayId = (int)week_holi_box.SelectedValue,
                    RecidenceId = (int?)recidenceBox.SelectedValue,
                    JobTypeId = (int?)job_type_box.SelectedValue,

                    // الإعفاءات
                    ExemptLate = late_box.IsChecked == true,
                    ExemptEarlyLeave = early_end.IsChecked == true,
                    ExemptOvertime = OV1_box.IsChecked == true,
                    ExemptEarlyEnter = OV_box.IsChecked == true,
                    ExemptAbsence = absent_box.IsChecked == true,

                    // المعلومات الإضافية
                    WorkHours = TimeSpan.TryParse(work_hours_box.Text, out var workHours) ? workHours : TimeSpan.Zero,
                    InDuty = inDuty_check.IsChecked == true,
                    InsuredId = (int?)insuredComboBox.SelectedValue,
                    HolidayBalance = int.TryParse(holiday_balance_box.Text, out int balance) ? balance : 0,

                    // البلاك ليست
                    IsArchived = archive_check.IsChecked == true,
                    Blacklist = blacklist_check.IsChecked == true,
                    BlacklistReason = blacklist_check.IsChecked == true ? blacklist_notes_box.Text : null,

                    // معلومات التدريب والتوظيف
                    UnderTraining = training_check.IsChecked == true,
                    UnderEmployment = employee_check.IsChecked == true,
                    IsUser = user_check.IsChecked == true,
                    IsMobileUser = userMobile_check.IsChecked == true,

                    // المستندات
                    NationalIDExpiration = national_id_expiration.SelectedDate.HasValue ?
                        DateOnly.FromDateTime(national_id_expiration.SelectedDate.Value) : null,
                    ArmyCertificateExpiration = army_certificate_expiration.SelectedDate.HasValue ?
                        DateOnly.FromDateTime(army_certificate_expiration.SelectedDate.Value) : null,
                    ArmyCertificateNumber = army_certificate_box.Text,
                    SSN = ssn_box.Text,
                    HealthInsuranceNumber = health_insurance_box.Text,

                    // معلومات القيادة (إذا كانت الوظيفة سائق)
                    DriverLicenseExpiration = _isDriver && driver_license_expiration.SelectedDate.HasValue ?
                        DateOnly.FromDateTime(driver_license_expiration.SelectedDate.Value) : null,
                    VehicleLicenseExpiration = _isDriver && vehicle_license_expiration.SelectedDate.HasValue ?
                        DateOnly.FromDateTime(vehicle_license_expiration.SelectedDate.Value) : null,

                    // الصورة
                    ProfileImageData = _profileImageData,

                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,

                    // معلومات المرتب
                    MainSalary = decimal.TryParse(holiday_balance_box.Text, out decimal salary) ? salary : 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ بيانات الموظف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearData();
                this.Close();
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show($"خطأ في قاعدة البيانات: {dbEx.InnerException?.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void search_btn_Click(object sender, RoutedEventArgs e)
        {


            try
            {
                _currentEmployee = 0;
                var context = new AppDbContext(App.ConnectionString);
                users = await context.Users
                    .Include(u => u.Branch)
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Degree)
                    .Include(u => u.Shift)
                    .Include(u => u.Qualification)
                    //.Include(u => u.Break)
                    .Include(u => u.WeekHoliday)
                    .Include(u => u.JobType)
                    .ToListAsync();

                if (!string.IsNullOrEmpty(emplo_code_box.Text))
                {
                    users = users.Where(u => u.Code == emplo_code_box.Text).ToList();
                }
                
                if (!string.IsNullOrEmpty(emplo_name_box.Text))
                {
                    users = users.Where(u => u.FullName.StartsWith(emplo_name_box.Text)).ToList();
                }

                if (branch_box.SelectedValue != null)
                {
                    int selectedBranchId = (int)branch_box.SelectedValue;
                    users = users.Where(u => u.BranchId == selectedBranchId).ToList();
                }

                if (job_box.SelectedValue != null)
                {
                    int selectedJobId = (int)job_box.SelectedValue;
                    users = users.Where(u => u.JobTitleId == selectedJobId).ToList();

                }

                if (users.Count > 0)
                {
                    FillFormWithUserData(users[_currentEmployee]);
                    _selectedUser = users[_currentEmployee];
                    editBtn.Visibility = Visibility.Visible;
                    saveBtn.Visibility = Visibility.Collapsed;
                    if (users.Count > 1)
                    {
                        employeesControlPanel.Visibility = Visibility.Visible;
                        currentIndexTxt.Text = $"{_currentEmployee + 1} / {users.Count}";
                    }
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على اي موظف", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ClearData();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillFormWithUserData(User user)
        {
            try
            {
                // المعلومات الشخصية
                emplo_code_box.Text = user.Code.ToString();
                emplo_name_box.Text = user.FullName ?? "";
                emplo_card_box.Text = user.NationalID ?? "";
                emplo_tele_box.Text = user.PhoneNumber ?? "";
                emplo_email_box.Text = user.Email ?? "";
                emplo_address_box.Text = user.Address ?? "";

                if (user.BirthDate != DateOnly.MinValue)
                    birth_date_picker.SelectedDate = user.BirthDate.ToDateTime(TimeOnly.MinValue);

                if (user.HireDate != DateOnly.MinValue)
                    emplo_date_picker.SelectedDate = user.HireDate.ToDateTime(TimeOnly.MinValue);

                if (user.FinishJob.HasValue)
                    end_date_picker.SelectedDate = user.FinishJob.Value.ToDateTime(TimeOnly.MinValue);

                // النوع
                male_box.IsChecked = user.Gender == 'M';
                female_box.IsChecked = user.Gender == 'F';

                // القوائم المنسدلة
                if (user.BranchId > 0)
                    branch_box.SelectedValue = user.BranchId;

                if (user.DepartmentId > 0)
                    depart_box.SelectedValue = user.DepartmentId;

                if (user.JobTitleId > 0)
                    job_box.SelectedValue = user.JobTitleId;

                if (user.DegreeId > 0)
                    degree_box.SelectedValue = user.DegreeId;

                if (user.ShiftId > 0)
                    shift_box.SelectedValue = user.ShiftId;

                if (user.MaritalId > 0)
                    maritalBox.SelectedValue = user.MaritalId;

                if (user.QualificationId > 0)
                    qualificationBox.SelectedValue = user.QualificationId;

                if (user.ManagerId.HasValue && user.ManagerId > 0)
                    manager_box.SelectedValue = user.ManagerId;

                //if (user.BreakId > 0)
                //    break_box.SelectedValue = user.BreakId;

                if (user.WeekHolidayId > 0)
                    week_holi_box.SelectedValue = user.WeekHolidayId;

                if (user.RecidenceId > 0)
                    recidenceBox.SelectedValue = user.RecidenceId;

                if (user.JobTypeId.HasValue)
                    job_type_box.SelectedValue = user.JobTypeId;

                // الإعفاءات
                late_box.IsChecked = user.ExemptLate;
                early_end.IsChecked = user.ExemptEarlyLeave;
                OV_box.IsChecked = user.ExemptEarlyEnter;
                OV1_box.IsChecked = user.ExemptOvertime;
                absent_box.IsChecked = user.ExemptAbsence;

                // الخيارات
                inDuty_check.IsChecked = user.InDuty;
                insuredComboBox.SelectedValue = user.InsuredId;
                user_check.IsChecked = user.IsUser;
                userMobile_check.IsChecked = user.IsMobileUser;
                employee_check.IsChecked = user.UnderEmployment;
                training_check.IsChecked = user.UnderTraining;

                // البلاك ليست
                blacklist_check.IsChecked = user.Blacklist;
                archive_check.IsChecked = user.IsArchived;
                if (user.Blacklist)
                {
                    blacklist_notes_box.Text = user.BlacklistReason;
                    blacklist_notes_box.Visibility = Visibility.Visible;
                }

                // المعلومات الإضافية
                holiday_balance_box.Text = user.HolidayBalance.ToString();
                work_hours_box.Text = user.WorkHours.ToString(@"hh\:mm") ?? "";
                ssn_box.Text = user.SSN ?? "";
                health_insurance_box.Text = user.HealthInsuranceNumber ?? "";
                army_certificate_box.Text = user.ArmyCertificateNumber ?? "";

                if (user.NationalIDExpiration.HasValue)
                    national_id_expiration.SelectedDate = user.NationalIDExpiration.Value.ToDateTime(TimeOnly.MinValue);

                if (user.ArmyCertificateExpiration.HasValue)
                    army_certificate_expiration.SelectedDate = user.ArmyCertificateExpiration.Value.ToDateTime(TimeOnly.MinValue);

                // معلومات القيادة
                if (user.DriverLicenseExpiration.HasValue)
                    driver_license_expiration.SelectedDate = user.DriverLicenseExpiration.Value.ToDateTime(TimeOnly.MinValue);

                if (user.VehicleLicenseExpiration.HasValue)
                    vehicle_license_expiration.SelectedDate = user.VehicleLicenseExpiration.Value.ToDateTime(TimeOnly.MinValue);

                // الصورة
                if (user.ProfileImageData != null && user.ProfileImageData.Length > 0)
                {
                    LoadUserImage(user.ProfileImageData);
                }
                else
                {
                    LoadDefaultImage();
                }

                // التحقق إذا كانت الوظيفة سائق وإظهار قسم القيادة
                if (user.JobTitleId > 0)
                {
                    CheckAndShowDrivingSection(user.JobTitleId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckAndShowDrivingSection(int jobTitleId)
        {
            try
            {
                _isDriver = await IsDriverJob(jobTitleId);
                driving_section.Visibility = _isDriver ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception)
            {
                driving_section.Visibility = Visibility.Collapsed;
            }
        }

        private async void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("يرجى البحث عن الموظف أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!ValidateInput())
                    return;

                // البحث عن المستخدم في قاعدة البيانات
                int code = int.Parse(emplo_code_box.Text);
                var user = await _context.Users.FindAsync(code);

                if (user != null)
                {
                    // تحديث البيانات
                    UpdateUserData(user);
                    user.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    MessageBox.Show("تم تعديل بيانات الموظف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                }
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show($"خطأ في قاعدة البيانات: {dbEx.InnerException?.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUserData(User user)
        {
            // تحديث جميع الحقول
            user.NationalID = emplo_card_box.Text;
            user.PhoneNumber = emplo_tele_box.Text;
            user.FullName = emplo_name_box.Text;
            user.Email = emplo_email_box.Text;
            user.Address = emplo_address_box.Text;

            if (emplo_date_picker.SelectedDate.HasValue)
                user.HireDate = DateOnly.FromDateTime(emplo_date_picker.SelectedDate.Value);

            if (birth_date_picker.SelectedDate.HasValue)
                user.BirthDate = DateOnly.FromDateTime(birth_date_picker.SelectedDate.Value);

            user.FinishJob = end_date_picker.SelectedDate.HasValue ?
                DateOnly.FromDateTime(end_date_picker.SelectedDate.Value) : null;

            user.Gender = male_box.IsChecked == true ? 'M' : 'F';

            user.BranchId = (int)branch_box.SelectedValue;
            user.DepartmentId = (int)depart_box.SelectedValue;
            user.ManagerId = (int?)manager_box.SelectedValue;
            user.JobTitleId = (int)job_box.SelectedValue;
            user.DegreeId = (int)degree_box.SelectedValue;
            user.ShiftId = (int)shift_box.SelectedValue;
            user.RecidenceId = (int?)recidenceBox.SelectedValue;
            user.WeekHolidayId = (int)week_holi_box.SelectedValue;
            user.JobTypeId = (int?)job_type_box.SelectedValue;
            user.InsuredId = (int?)insuredComboBox.SelectedValue;
            user.MaritalId = (int?)maritalBox.SelectedValue;
            user.QualificationId = (int?)qualificationBox.SelectedValue;

            user.ExemptLate = late_box.IsChecked == true;
            user.ExemptEarlyLeave = early_end.IsChecked == true;
            user.ExemptOvertime = OV1_box.IsChecked == true;
            user.ExemptEarlyEnter = OV_box.IsChecked == true;
            user.ExemptAbsence = absent_box.IsChecked == true;

            user.WorkHours = TimeSpan.Parse(work_hours_box.Text);

            user.InDuty = inDuty_check.IsChecked == true;
            user.HolidayBalance = int.TryParse(holiday_balance_box.Text, out int balance) ? balance : 0;

            user.Blacklist = blacklist_check.IsChecked == true;
            user.IsArchived = archive_check.IsChecked == true;
            user.BlacklistReason = blacklist_check.IsChecked == true ? blacklist_notes_box.Text : null;

            user.UnderTraining = training_check.IsChecked == true;
            user.UnderEmployment = employee_check.IsChecked == true;
            user.IsUser = user_check.IsChecked == true;
            user.IsMobileUser = userMobile_check.IsChecked == true;

            user.NationalIDExpiration = national_id_expiration.SelectedDate.HasValue ?
                DateOnly.FromDateTime(national_id_expiration.SelectedDate.Value) : null;
            user.ArmyCertificateExpiration = army_certificate_expiration.SelectedDate.HasValue ?
                DateOnly.FromDateTime(army_certificate_expiration.SelectedDate.Value) : null;
            user.ArmyCertificateNumber = army_certificate_box.Text;
            user.SSN = ssn_box.Text;
            user.HealthInsuranceNumber = health_insurance_box.Text;

            // تحديث معلومات القيادة إذا كانت الوظيفة سائق
            if (_isDriver)
            {
                user.DriverLicenseExpiration = driver_license_expiration.SelectedDate.HasValue ?
                    DateOnly.FromDateTime(driver_license_expiration.SelectedDate.Value) : null;
                user.VehicleLicenseExpiration = vehicle_license_expiration.SelectedDate.HasValue ?
                    DateOnly.FromDateTime(vehicle_license_expiration.SelectedDate.Value) : null;
            }
            else
            {
                user.DriverLicenseExpiration = null;
                user.VehicleLicenseExpiration = null;
            }

            if (_profileImageData != null)
                user.ProfileImageData = _profileImageData;
        }

        private void ClearDrivingData()
        {
            driver_license_expiration.SelectedDate = null;
            vehicle_license_expiration.SelectedDate = null;
        }

        private void ClearData()
        {
            try
            {
                users.Clear();
                _currentEmployee = 0;
                // مسح جميع الحقول
                emplo_code_box.Clear();
                emplo_name_box.Clear();
                emplo_card_box.Clear();
                emplo_tele_box.Clear();
                work_hours_box.Clear();
                emplo_email_box.Clear();
                emplo_address_box.Clear();
                holiday_balance_box.Clear();
                ssn_box.Clear();
                health_insurance_box.Clear();
                army_certificate_box.Clear();
                blacklist_notes_box.Clear();

                birth_date_picker.SelectedDate = null;
                emplo_date_picker.SelectedDate = DateTime.Now;
                end_date_picker.SelectedDate = null;
                national_id_expiration.SelectedDate = null;
                army_certificate_expiration.SelectedDate = null;

                branch_box.SelectedIndex = -1;
                depart_box.SelectedIndex = -1;
                job_box.SelectedIndex = -1;
                manager_box.SelectedIndex = -1;
                degree_box.SelectedIndex = -1;
                shift_box.SelectedIndex = -1;
                break_box.SelectedIndex = -1;
                week_holi_box.SelectedIndex = -1;
                job_type_box.SelectedIndex = -1;
                recidenceBox.SelectedIndex = -1;
                insuredComboBox.SelectedIndex = -1;
                maritalBox.SelectedIndex = -1;
                qualificationBox.SelectedIndex = -1;

                late_box.IsChecked = false;
                early_end.IsChecked = false;
                OV_box.IsChecked = false;
                OV1_box.IsChecked = false;
                absent_box.IsChecked = false;

                inDuty_check.IsChecked = true;
                user_check.IsChecked = false;
                userMobile_check.IsChecked = false;
                employee_check.IsChecked = false;
                blacklist_check.IsChecked = false;
                archive_check.IsChecked = false;
                training_check.IsChecked = false;

                male_box.IsChecked = true;
                female_box.IsChecked = false;

                blacklist_notes_box.Visibility = Visibility.Collapsed;
                driving_section.Visibility = Visibility.Collapsed;
                ClearDrivingData();

                LoadDefaultImage();

                editBtn.Visibility = Visibility.Collapsed;
                saveBtn.Visibility = Visibility.Visible;

                _selectedUser = null;
                _selectedEmployee = null;
                _isDriver = false;
                _profileImageData = null;

                employeesControlPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في مسح البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearData();
        }

        private void next_btn_Click(object sender, RoutedEventArgs e)
        {
           if (_currentEmployee < users.Count - 1)
            {
                _currentEmployee++;
                FillFormWithUserData(users[_currentEmployee]);
                _selectedUser = users[_currentEmployee];

                currentIndexTxt.Text = $"{_currentEmployee + 1} / {users.Count}";
            }
        }

        private void prev_btn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmployee > 0)
            {
                _currentEmployee--;
                FillFormWithUserData(users[_currentEmployee]);
                _selectedUser = users[_currentEmployee];
                currentIndexTxt.Text = $"{_currentEmployee + 1} / {users.Count}";
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void shift_changed(object sender, SelectionChangedEventArgs e)
        {
            if (shift_box.SelectedItem is Shift selectedShift)
            {
                if (selectedShift.StartTime < selectedShift.EndTime)
                    work_hours_box.Text = (selectedShift.EndTime - selectedShift.StartTime).ToString(@"hh\:mm");
                else
                    work_hours_box.Text = (new TimeSpan(24, 0, 0) - (selectedShift.StartTime - selectedShift.EndTime)).ToString(@"hh\:mm");
            }
        }

        private bool ValidateInput()
        {
            // التحقق من الحقول الإلزامية
            if (string.IsNullOrEmpty(emplo_name_box.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الموظف", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                emplo_name_box.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(emplo_card_box.Text))
            {
                MessageBox.Show("يرجى إدخال الرقم القومي", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                emplo_card_box.Focus();
                return false;
            }

            if (birth_date_picker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار تاريخ الميلاد", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                birth_date_picker.Focus();
                return false;
            }

            if (emplo_date_picker.SelectedDate == null)
            {
                MessageBox.Show("يرجى اختيار تاريخ التعيين", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                emplo_date_picker.Focus();
                return false;
            }

            if (branch_box.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار الفرع", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                branch_box.Focus();
                return false;
            }

            if (depart_box.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار الإدارة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                depart_box.Focus();
                return false;
            }

            if (job_box.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار الوظيفة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                job_box.Focus();
                return false;
            }

            if (recidenceBox.SelectedItem == null)
            {
                MessageBox.Show("يرجى اختيار الاقامة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                job_box.Focus();
                return false;
            }

            // التحقق من البلاك ليست
            if (blacklist_check.IsChecked == true && string.IsNullOrEmpty(blacklist_notes_box.Text))
            {
                MessageBox.Show("يرجى إدخال سبب الإضافة للقائمة السوداء", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                blacklist_notes_box.Focus();
                return false;
            }

            // التحقق من صحة البريد الإلكتروني (إذا تم إدخاله)
            if (!string.IsNullOrEmpty(emplo_email_box.Text))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(emplo_email_box.Text);
                    if (addr.Address != emplo_email_box.Text)
                    {
                        MessageBox.Show("يرجى إدخال بريد إلكتروني صحيح", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                        emplo_email_box.Focus();
                        return false;
                    }
                }
                catch
                {
                    MessageBox.Show("يرجى إدخال بريد إلكتروني صحيح", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    emplo_email_box.Focus();
                    return false;
                }
            }

            return true;
        }

        private void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser != null)
            {
                var archiveWindow = new EmployeeArchiveWindow(_selectedUser.Id);
                archiveWindow.Owner = this;
                archiveWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("يرجى اختيار موظف أولاً", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void addImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    Title = "اختر صورة الموظف"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFilePath = openFileDialog.FileName;

                    // التحقق من حجم الصورة (أقصى 5MB)
                    FileInfo fileInfo = new FileInfo(selectedFilePath);
                    if (fileInfo.Length > 5 * 1024 * 1024) // 5MB
                    {
                        MessageBox.Show("حجم الصورة كبير جداً. الرجاء اختيار صورة أقل من 5 ميجابايت.",
                            "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // تحميل الصورة وعرضها
                    LoadImageFromFile(selectedFilePath);

                    // تحويل الصورة إلى byte array وحفظها
                    _profileImageData = File.ReadAllBytes(selectedFilePath);

                    // تحديث واجهة المستخدم
                    removeImage.Visibility = Visibility.Visible;
                    addImage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الصورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void removeImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _profileImageData = null;
                LoadDefaultImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إزالة الصورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadImageFromFile(string filePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 200; // تحجيم الصورة لتحسين الأداء
                bitmap.EndInit();

                EmployeeImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض الصورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserImage(byte[] imageData)
        {
            try
            {
                if (imageData != null && imageData.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(imageData))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.DecodePixelWidth = 200; // تحجيم الصورة لتحسين الأداء
                        bitmap.EndInit();
                        bitmap.Freeze(); // مهم للعمليات متعددة الخيوط

                        EmployeeImage.Source = bitmap;
                    }

                    removeImage.Visibility = Visibility.Visible;
                    addImage.Visibility = Visibility.Collapsed;
                    _profileImageData = imageData;
                }
                else
                {
                    LoadDefaultImage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل صورة المستخدم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadDefaultImage();
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("يرجى البحث عن الموظف أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("هل أنت متأكد من حذف هذا الموظف؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                int code = int.Parse(emplo_code_box.Text);
                var user = await _context.Users.FindAsync(code);

                if (user != null)
                {
                    // حذف الموظف
                    _context.Users.Remove(user);

                    await _context.SaveChangesAsync();
                    MessageBox.Show("تم حذف الموظف بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // دالة جديدة لتحديد عنوان النافذة بناءً على الإجراء
        private void UpdateWindowTitle()
        {
            if (_selectedUser != null)
            {
                this.Title = $"تعديل بيانات الموظف - {_selectedUser.FullName}";
            }
            else
            {
                this.Title = "إضافة موظف جديد";
            }
        }

        // حدث عند تحميل النافذة
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
            UpdateWindowTitle();

            // إذا كانت هناك بيانات موظف محملة، املأ النموذج
            if (_selectedEmployee != null)
            {
                emplo_code_box.Text = _selectedEmployee.Code.ToString();
                search_btn_Click(null, null);
            }
        }

        // حدث عند إغلاق النافذة
        private void Window_Closed(object sender, EventArgs e)
        {
            _context?.Dispose();
        }

        private async void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            await LoadData();

        }
    }
}