namespace Sho2on.Web.Models
{
    public class BenefitDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserCode { get; set; } = "";
        public int BenefitTypeId { get; set; }
        public string BenefitTypeName { get; set; } = "";
        public string BenefitType { get; set; } = "Benefit"; // Benefit أو Deduction

        public string Frequency { get; set; } = "Once";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // للعرض
        public string TypeDisplay => BenefitType == "Benefit" ? "استحقاق" : "استقطاع";
        public string TypeClass => BenefitType == "Benefit" ? "status-benefit" : "status-deduction";
        public string AmountDisplay => BenefitType == "Benefit" ? $"+{Amount:N0}" : $"-{Amount:N0}";
        public string AmountClass => BenefitType == "Benefit" ? "amount-positive" : "amount-negative";
    }

 
    public class BenefitStatisticsDto
    {
        public decimal TotalBenefits { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalCount { get; set; }
        public int BenefitCount { get; set; }
        public int DeductionCount { get; set; }
    }
}