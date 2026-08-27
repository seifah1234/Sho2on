namespace Sho2on.API.Dtos
{
    public class EmployeeSearchRequest
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string SearchTerm { get; set; }
        public int? DepartmentId { get; set; }
        public int? JobTitleId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
