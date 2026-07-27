using System.ComponentModel.DataAnnotations;

namespace Sho2on.Web.Models
{
    public class EmployeeFormModel
    {
        public int? Id { get; set; }

        // ── بيانات شخصية ──
        [Required(ErrorMessage = "الاسم مطلوب")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "الكود مطلوب")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        public string NationalID { get; set; } = "";

        public string PhoneNumber { get; set; } = "";
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateOnly BirthDate { get; set; }
        public char Gender { get; set; } = 'M';

        // ── بيانات الوظيفة ──
        [Required] public DateOnly HireDate { get; set; }
        [Required] public int BranchId { get; set; }
        [Required] public int DepartmentId { get; set; }
        [Required] public int JobTitleId { get; set; }
        [Required] public int DegreeId { get; set; }
        public int? ManagerId { get; set; }
        [Required] public int ShiftId { get; set; }
        public int? BreakId { get; set; }
        [Required] public int WeekHolidayId { get; set; }
        public int? JobTypeId { get; set; }
        public int? QualificationId { get; set; }
        public int? AreaId { get; set; }
        public TimeSpan WorkHours { get; set; } = TimeSpan.FromHours(8);
        public bool UnderTraining { get; set; }
        public bool UnderEmployment { get; set; } = true;
        public bool InDuty { get; set; } = true;
        public DateOnly? FinishJob { get; set; }

        // ── الراتب والسلف ──
        public decimal? MainSalary { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal MaxLoanAmount { get; set; }
        public bool CanTakeLoan { get; set; } = true;

        // ── الحضور والإعفاءات ──
        public int HolidayBalance { get; set; }
        public bool ExemptLate { get; set; }
        public bool ExemptEarlyLeave { get; set; }
        public bool ExemptOvertime { get; set; }
        public bool ExemptAbsence { get; set; }
        public bool ExemptEarlyEnter { get; set; }

        // ── المستندات ──
        public DateOnly? NationalIDExpiration { get; set; }
        public DateOnly? DriverLicenseExpiration { get; set; }
        public DateOnly? VehicleLicenseExpiration { get; set; }
        public DateOnly? ArmyCertificateExpiration { get; set; }
        public string? ArmyCertificateNumber { get; set; }
        public string? SSN { get; set; }
        public string? HealthInsuranceNumber { get; set; }

        // ── إعدادات الحساب ──
        public string? Username { get; set; }
        public bool IsUser { get; set; }
        public bool IsMobileUser { get; set; }
        public bool Blacklist { get; set; }
        public string? BlacklistReason { get; set; }

        public int? MaritalId { get; set; }
        public int? RecidenceId { get; set; }
        public int InsuredId { get; set; } = 0;
    }

    public class EmployeeLookups
    {
        public List<(int Id, string Name)> Branches { get; set; } = new();
        public List<(int Id, string Name)> Departments { get; set; } = new();
        public List<(int Id, string Name)> JobTitles { get; set; } = new();
        public List<(int Id, string Name)> Degrees { get; set; } = new();
        public List<(int Id, string Name)> Shifts { get; set; } = new();
        public List<(int Id, string Name)> Breaks { get; set; } = new();
        public List<(int Id, string Name)> WeekHolidays { get; set; } = new();
        public List<(int Id, string Name)> JobTypes { get; set; } = new();
        public List<(int Id, string Name)> Qualifications { get; set; } = new();
        public List<(int Id, string Name)> Areas { get; set; } = new();
        public List<(int Id, string Name)> Managers { get; set; } = new();
    }
}