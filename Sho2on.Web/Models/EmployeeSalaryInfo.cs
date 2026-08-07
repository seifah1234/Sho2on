using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class EmployeeSalaryInfo
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public SalaryTypeEnum? SalaryType { get; set; }
        public decimal? FixedSalary { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? MonthlyWorkingHours { get; set; }
        public decimal? DailyWorkingHours { get; set; }
        public int? WorkingDaysPerMonth { get; set; }
        public decimal? MainSalary { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxLoanAmount { get; set; }
        public bool CanTakeLoan { get; set; }

        // حسابات
        public decimal MonthlySalary => SalaryType switch
        {
            SalaryTypeEnum.Fixed => FixedSalary ?? 0,
            SalaryTypeEnum.MonthlyHourly => (HourlyRate ?? 0) * (MonthlyWorkingHours ?? 0),
            SalaryTypeEnum.DailyHourly => (HourlyRate ?? 0) * (DailyWorkingHours ?? 0) * (WorkingDaysPerMonth ?? 0),
            _ => MainSalary ?? 0
        };

        public decimal DailySalary => SalaryType == SalaryTypeEnum.DailyHourly
            ? (HourlyRate ?? 0) * (DailyWorkingHours ?? 0)
            : 0;

        public string SalaryTypeDisplay => SalaryType switch
        {
            SalaryTypeEnum.Fixed => "راتب ثابت",
            SalaryTypeEnum.MonthlyHourly => "ساعة شهرية",
            SalaryTypeEnum.DailyHourly => "ساعة يومية",
            _ => "غير محدد"
        };
    }
}