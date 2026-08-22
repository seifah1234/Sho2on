// LoanPayment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class LoanPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int LoanId { get; set; }

        [Required]
        public decimal PaymentAmount { get; set; } // مبلغ الدفعة

        [Required]
        public DateTime PaymentDate { get; set; } // تاريخ الدفعة

        [Required]
        [MaxLength(20)]
        public string PaymentType { get; set; } // Monthly, Partial, Full

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey(nameof(LoanId))]
        public Loan? Loan { get; set; }
    }
}