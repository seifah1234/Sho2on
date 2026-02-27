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

        public bool CanTakeLoan { get; set; } = true; // هل يمكن أخذ سلفة أم لا

        // Work Information
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

        public bool ExemptLate { get; set; }
        public bool ExemptEarlyLeave { get; set; }
        public bool ExemptOvertime { get; set; }
        public bool ExemptAbsence { get; set; }
        public bool ExemptEarlyEnter { get; set; }

        public TimeSpan WorkHours { get; set; }
        public bool InDuty { get; set; }

        public bool IsInsured { get; set; }
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
        public Branch? Branch { get; set; } = null!;
        public User? Manager { get; set; } = null!;
        public Department? Department { get; set; } = null!;
        public JobTitle? JobTitle { get; set; } = null!;
        public Degree? Degree { get; set; } = null!;
        public Shift? Shift { get; set; } = null!;
        public Break? Break { get; set; } = null!;
        public WeekHoliday? WeekHoliday { get; set; } = null!;
        public JobType? JobType { get; set; } = null!;
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

        public ICollection<User>? MyEmployees { get; internal set; }
    }
}
