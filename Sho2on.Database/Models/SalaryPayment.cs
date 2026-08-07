using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class SalaryPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// الموظف
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// الشهر
        /// </summary>
        [Required]
        public int Month { get; set; }

        /// <summary>
        /// السنة
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// إجمالي الراتب الأساسي (ثابت + متغير)
        /// </summary>
        public decimal BasicSalary { get; set; }

        /// <summary>
        /// إجمالي الاستحقاقات (Monthly + Once)
        /// </summary>
        public decimal TotalAdditions { get; set; }

        /// <summary>
        /// إجمالي الخصومات (Monthly + Once)
        /// </summary>
        public decimal TotalDeductions { get; set; }

        /// <summary>
        /// صافي الراتب
        /// </summary>
        public decimal NetSalary { get; set; }

        /// <summary>
        /// هل تم الدفع؟
        /// </summary>
        public bool IsPaid { get; set; }

        /// <summary>
        /// هل هو صرف فوري (Off-Cycle)؟
        /// </summary>
        public bool IsOffCycle { get; set; } = false;

        /// <summary>
        /// تاريخ الدفع
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// تاريخ الصرف الفعلي
        /// </summary>
        public DateTime? ActualPaymentDate { get; set; }

        /// <summary>
        /// ملاحظات
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// تاريخ الإنشاء
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// تاريخ التحديث
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ═══ العلاقات ═══
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}