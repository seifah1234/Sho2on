// Loan.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Loan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public decimal LoanAmount { get; set; } // مبلغ السلفة

        [Required]
        public decimal RemainingAmount { get; set; } // المبلغ المتبقي للسداد

        [Required]
        public DateTime LoanDate { get; set; } // تاريخ أخذ السلفة

        public DateTime? ExpectedPaybackDate { get; set; } // التاريخ المتوقع للسداد

        public DateTime? ActualPaybackDate { get; set; } // تاريخ السداد الفعلي

        [Required]
        public int InstallmentCount { get; set; } // عدد الأقساط

        [Required]
        public decimal MonthlyInstallment { get; set; } // القسط الشهري

        public decimal AmountPaid { get; set; } = 0; // المبلغ المدفوع حتى الآن

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Paid, PartiallyPaid

        [MaxLength(500)]
        public string? Reason { get; set; } // سبب السلفة

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? ApprovedByUserId { get; set; } // المستخدم الذي وافق على السلفة

        public DateTime? ApprovedDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ApprovedByUserId))]
        public User? ApprovedByUser { get; set; }

        // قائمة دفعات السلف
        public virtual ICollection<LoanPayment>? LoanPayments { get; set; }
    }
}