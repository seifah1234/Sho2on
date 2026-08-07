// Models/LoanDto.cs
namespace Sho2on.Web.Models
{
    public class LoanDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserCode { get; set; } = "";
        public decimal LoanAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ExpectedPaybackDate { get; set; }
        public DateTime? ActualPaybackDate { get; set; }
        public int InstallmentCount { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public string Status { get; set; } = "";
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string ApprovedByName { get; set; } = "";
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int PaymentCount { get; set; }
        public List<LoanPaymentDto> Payments { get; set; } = new();

        // للعرض
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    "Pending" => "قيد الانتظار",
                    "Approved" => "معتمدة",
                    "Rejected" => "مرفوضة",
                    "PartiallyPaid" => "مسددة جزئياً",
                    "Paid" => "مسددة بالكامل",
                    _ => Status
                };
            }
        }

        public string StatusClass
        {
            get
            {
                return Status switch
                {
                    "Pending" => "status-pending",
                    "Approved" => "status-approved",
                    "Rejected" => "status-rejected",
                    "PartiallyPaid" => "status-partial",
                    "Paid" => "status-paid",
                    _ => "status-pending"
                };
            }
        }
    }

    public class LoanRequestDto
    {
        public int UserId { get; set; }
        public decimal LoanAmount { get; set; }
        public int InstallmentCount { get; set; } // عدد الأقساط (1 = مرة واحدة)
        public DateTime? ExpectedPaybackDate { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class LoanApprovalDto
    {
        public int LoanId { get; set; }
        public int ApprovedByUserId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class LoanPaymentDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; } = "Monthly"; // Monthly, Partial, Full
        public string? Notes { get; set; }
    }

    public class LoanStatisticsDto
    {
        public int TotalLoans { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public int ActiveLoans { get; set; }
        public int PendingLoans { get; set; }
    }

    public class EmployeeDetailDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string FullName { get; set; } = "";
        public decimal? MainSalary { get; set; }

        public decimal? FixedSalary { get; set; }      // ⬅️ جديد
        public decimal? HourlyRate { get; set; }       // ⬅️ جديد (الراتب المتغير)
        public decimal? MaxLoanAmount { get; set; }
        public bool CanTakeLoan { get; set; }
    }

}