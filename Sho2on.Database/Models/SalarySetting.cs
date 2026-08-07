using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class SalarySetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // الضرايب
        public decimal TaxPercentage { get; set; } = 10; // 10%
        public decimal TaxThreshold { get; set; } = 5000; // أول 5000 معفاة

        // التأمينات
        public decimal InsurancePercentage { get; set; } = 11; // 11%
        public decimal InsuranceMaxAmount { get; set; } = 50000; // الحد الأقصى

        // صندوق الزمالة
        public decimal FriendshipBoxPercentage { get; set; } = 5; // 5%

        // المشاركة الاجتماعية
        public decimal SocialParticipationAmount { get; set; } = 0;

        // الصرف الفوري
        public bool AllowOffCycle { get; set; } = true;

        // خصم الغياب
        public decimal AbsenceDeductionRate { get; set; } = 1; // 1 = يوم كامل

        // العملة
        [MaxLength(10)]
        public string Currency { get; set; } = "ج.م";

        // إعدادات عامة
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}