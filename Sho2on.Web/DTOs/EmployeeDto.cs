namespace Sho2on.API.Dtos
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
        public string JobTitleName { get; set; }
        public string BranchName { get; set; }
        public DateOnly HireDate { get; set; }
        public List<string> WeekendDays { get; set; } = new List<string>();
        public bool HasManagerRole { get; set; }
        public decimal? MainSalary { get; set; }

        public decimal MaxLoanAmount { get; set; } = 0; // الحد الأقصى للسلفة المسموح بها
        public decimal CurrentLoanBalance { get; set; } = 0; // رصيد السلف الحالي

        public bool CanTakeLoan { get; set; } = true; // هل يمكن أخذ سلفة أم لا
        public decimal? LoanMaxAmount { get; set; } = 0;

    }

}
