namespace Sho2on.Web.Models
{
    public class SalarySettings
    {
        // الضرايب
        public decimal TaxPercentage { get; set; } = 0; // 10%
        public decimal TaxThreshold { get; set; } = 0; // أول 5000 معفاة

        // التأمينات
        public decimal InsurancePercentage { get; set; } = 0; // 11%
        public decimal InsuranceMaxAmount { get; set; } = 0; // الحد الأقصى

        // خصم الغياب
        public decimal AbsenceDeductionRate { get; set; } = 0; // 1 = يوم كامل

        // الصرف الفوري
        public bool AllowOffCycle { get; set; } = true;
    }
}