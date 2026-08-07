namespace Sho2on.Web.Models
{
    public class SalarySettings
    {
        // الضرايب
        public decimal TaxPercentage { get; set; } = 10; // 10%
        public decimal TaxThreshold { get; set; } = 5000; // أول 5000 معفاة

        // التأمينات
        public decimal InsurancePercentage { get; set; } = 11; // 11%
        public decimal InsuranceMaxAmount { get; set; } = 50000; // الحد الأقصى

        // خصم الغياب
        public decimal AbsenceDeductionRate { get; set; } = 1; // 1 = يوم كامل

        // الصرف الفوري
        public bool AllowOffCycle { get; set; } = true;
    }
}