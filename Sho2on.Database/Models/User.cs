using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class User
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Code { get; set; }

        // Personal Information
        [Required, StringLength(14)]
        public string NationalID { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required, StringLength(150)]
        public string FullName { get; set; }

        [StringLength(150)]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Address { get; set; }

        [Required]
        public DateOnly HireDate { get; set; }

        [Required]
        public DateOnly BirthDate { get; set; }

        public decimal? MainSalary { get; set; }
        public decimal? MinSalary { get; set; }
        public string? RegisteredDeviceId { get; set; }

        public decimal? LoanMaxAmount { get; set; } = 0;

        public char Gender { get; set; }  // M / F

        public decimal MaxLoanAmount { get; set; } = 0; // الحد الأقصى للسلفة المسموح بها
        public decimal CurrentLoanBalance { get; set; } = 0; // رصيد السلف الحالي
        public decimal? HousingAllowance { get; set; }      // بدل سكن
        public decimal? TransportationAllowance { get; set; } // بدل انتقال
        public decimal? ManagementAllowance { get; set; }    // بدل إدارة
        public decimal? NatureAllowance { get; set; }        // بدل طبيعة عمل
        public bool CanTakeLoan { get; set; } = true; // هل يمكن أخذ سلفة أم لا


        public SalaryTypeEnum? SalaryType { get; set; }

        // الراتب الثابت
        public decimal? FixedSalary { get; set; }

        // سعر الساعة
        public decimal? HourlyRate { get; set; }

        // ساعات العمل الشهرية
        public decimal? MonthlyWorkingHours { get; set; } = 208;

        // ساعات العمل اليومية
        public decimal? DailyWorkingHours { get; set; } = 8;

        // أيام العمل في الشهر
        public int? WorkingDaysPerMonth { get; set; } = 26;

        // ══ خاصية محسوبة للراتب الشهري ══
        public decimal MonthlySalary => SalaryType switch
        {
            SalaryTypeEnum.Fixed => FixedSalary ?? 0,
            SalaryTypeEnum.MonthlyHourly => (HourlyRate ?? 0) * (MonthlyWorkingHours ?? 0),
            SalaryTypeEnum.DailyHourly => (HourlyRate ?? 0) * (DailyWorkingHours ?? 0) * (WorkingDaysPerMonth ?? 0),
            _ => MainSalary ?? 0
        };

        // Work Information
        [ForeignKey(nameof(Area))]
        public int? AreaId { get; set; }
        [ForeignKey(nameof(Branch))]
        public int BranchId { get; set; }
        [ForeignKey(nameof(Manager))]
        public int? ManagerId { get; set; }

        [ForeignKey(nameof(Department))]
        public int DepartmentId { get; set; }

        [ForeignKey(nameof(JobTitle))]
        public int JobTitleId { get; set; }

        [ForeignKey(nameof(Degree))]
        public int DegreeId { get; set; }

        [ForeignKey(nameof(Shift))]
        public int ShiftId { get; set; }

        [ForeignKey(nameof(Break))]
        public int? BreakId { get; set; }

        [ForeignKey(nameof(WeekHoliday))]
        public int WeekHolidayId { get; set; }

        [ForeignKey(nameof(JobType))]
        public int? JobTypeId { get; set; }

        [ForeignKey(nameof(Qualification))]
        public int? QualificationId { get; set; }
        public int? RecidenceId { get; set; }
        public int? MaritalId { get; set; }

        public bool ExemptLate { get; set; }
        public bool ExemptEarlyLeave { get; set; }
        public bool ExemptOvertime { get; set; }
        public bool ExemptAbsence { get; set; }
        public bool ExemptEarlyEnter { get; set; }

        public TimeSpan WorkHours { get; set; }
        public bool InDuty { get; set; }

        public int? InsuredId { get; set; }
        public int HolidayBalance { get; set; }

        // Sensitive / Administrative
        public bool Blacklist { get; set; }
        public string? BlacklistReason { get; set; }

        public bool UnderTraining { get; set; }
        public bool UnderEmployment { get; set; }

        public bool IsArchived { get; set; }

        // Documents & Expirations
        public DateOnly? FinishJob { get; set; }
        public DateOnly? DriverLicenseExpiration { get; set; }
        public DateOnly? VehicleLicenseExpiration { get; set; }
        public DateOnly? NationalIDExpiration { get; set; }
        public DateOnly? ArmyCertificateExpiration { get; set; }

        public string? ArmyCertificateNumber { get; set; }
        public string? SSN { get; set; }
        public string? HealthInsuranceNumber { get; set; }

        // Auth
        public string? PasswordHash { get; set; }

        public bool IsUser { get; set; }
        public bool? IsMobileUser { get; set; }

        // Profile Image (stored as bytes only)
        public byte[]? ProfileImageData { get; set; }

        // Audit Columns
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public Area? Area { get; set; } = null!;
        public Branch? Branch { get; set; } = null!;
        public User? Manager { get; set; } = null!;
        public Department? Department { get; set; } = null!;
        public JobTitle? JobTitle { get; set; } = null!;
        public Degree? Degree { get; set; } = null!;
        public Shift? Shift { get; set; } = null!;
        public Break? Break { get; set; } = null!;
        public WeekHoliday? WeekHoliday { get; set; } = null!;
        public JobType? JobType { get; set; } = null!;
        public Qualification? Qualification { get; set; } = null!;
        public ICollection<UserBranch>? UserBranches { get; internal set; }
        public ICollection<UserRole>? UserRoles { get; internal set; }
        public ICollection<FingerPrint>? FingerPrints { get; internal set; }
        public ICollection<MachineData>? MachineData { get; internal set; }
        public ICollection<Attendance>? Attendances { get; internal set; }
        public ICollection<Salary>? Salaries { get; internal set; }
        public ICollection<LeaveBalance>? LeaveBalances { get; internal set; }
        public ICollection<EmployeeDocument>? EmployeeDocuments { get; internal set; }
        public ICollection<EmployeeEvaluation>?  EmployeeEvaluations { get; internal set; }
        public ICollection<Loan>? Loans { get; internal set; }
        public ICollection<Loan>? ApprovedLoans { get; internal set; }
        public ICollection<SalaryPayment>? SalaryPayments { get; internal set; }
        public ICollection<EmployeePermission>? EmployeePermissions { get; internal set; }

        public virtual ICollection<EmployeeBenefit>? EmployeeBenefits { get; set; }
        public ICollection<User>? MyEmployees { get; internal set; }

        public ICollection<UserTask>? AssignedByTasks { get; internal set; }
        public ICollection<UserTask>? AssignedToTasks { get; internal set; }

        public ICollection<Chat>? SenderChats { get; internal set; }
        public ICollection<Chat>? ReceiverChats { get; internal set; }
    }

    public enum SalaryTypeEnum
    {
        Fixed = 1,
        MonthlyHourly = 2,
        DailyHourly = 3
    }
}
