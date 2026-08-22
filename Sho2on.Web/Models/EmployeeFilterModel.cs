namespace Sho2on.Web.Models
{
    public class EmployeeFilterModel
    {
        public string? Search { get; set; }
        public int? BranchId { get; set; }
        public bool IncludeArchived { get; set; }

        public int? DepartmentId { get; set; }
        public int? JobTitleId { get; set; }
        public char? Gender { get; set; }
        public int? MaritalId { get; set; }
        public int? InsuredId { get; set; }
        public int? RecidenceId { get; set; }
        public int? DegreeId { get; set; }
        public int? QualificationId { get; set; }
        public int? AreaId { get; set; }
        public bool? InDuty { get; set; }
        public bool? UnderTraining { get; set; }
        public bool? Blacklist { get; set; }
        public DateOnly? HireDateFrom { get; set; }
        public DateOnly? HireDateTo { get; set; }
        public string? PhoneNumber { get; set; }
    }
}