// SalaryPayment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class SalaryPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int Month { get; set; } // الشهر (1-12)

        [Required]
        public int Year { get; set; } // السنة

        [Required]
        public decimal BasicSalary { get; set; } // الراتب الأساسي

        public decimal HousingAllowance { get; set; } = 0; // بدل سكن
        public decimal TransportationAllowance { get; set; } = 0; // بدل انتقال
        public decimal ManagementAllowance { get; set; } = 0; // بدل إدارة
        public decimal NatureAllowance { get; set; } = 0; // بدل طبيعة عمل
        public decimal OvertimeAmount { get; set; } = 0; // إضافي
        public decimal Rewards { get; set; } = 0; // مكافآت
        public decimal TargetCommission { get; set; } = 0; // عمولات تحقيق
        public decimal ExternalCommission { get; set; } = 0; // عمولات خارجية

        // الاستقطاعات
        public decimal AbsenceDeduction { get; set; } = 0; // خصم الغياب
        public decimal LateDeduction { get; set; } = 0; // خصم التأخير
        public decimal LoanDeduction { get; set; } = 0; // خصم السلف
        public decimal PenaltyDeduction { get; set; } = 0; // خصم جزاءات
        public decimal TaxDeduction { get; set; } = 0; // ضريبة كسب العمل
        public decimal InsuranceDeduction { get; set; } = 0; // تأمينات الموظف
        public decimal SocialParticipation { get; set; } = 0; // مشاركة اجتماعية

        // صندوق الزمالة
        public decimal FriendshipBoxDeduction { get; set; } = 0; // خصم صندوق الزمالة



        // الإجماليات
        [Required]
        public decimal TotalAdditions { get; set; } // إجمالي الإضافات

        [Required]
        public decimal TotalDeductions { get; set; } // إجمالي الاستقطاعات

        [Required]
        public decimal NetSalary { get; set; } // صافي الراتب

        [Required]
        public DateTime PaymentDate { get; set; } // تاريخ الصرف

        public bool IsPaid { get; set; } = false; // تم الصرف أم لا

        public DateTime? ActualPaymentDate { get; set; } // تاريخ الصرف الفعلي

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}