// FriendshipBoxTransaction.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class FriendshipBoxTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int FriendshipBoxId { get; set; }

        public int? UserId { get; set; } // المستخدم المرتبط بالعملية (للسلف أو الإيداع)

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } // Deposit, Withdrawal, Loan, Repayment

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public decimal BalanceBefore { get; set; } // الرصيد قبل العملية

        [Required]
        public decimal BalanceAfter { get; set; } // الرصيد بعد العملية

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? SalaryPaymentId { get; set; } // مرتبط بصرف راتب
        public int? LoanId { get; set; } // مرتبط بسلفة

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey(nameof(FriendshipBoxId))]
        public FriendshipBox? FriendshipBox { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(SalaryPaymentId))]
        public SalaryPayment? SalaryPayment { get; set; }

        [ForeignKey(nameof(LoanId))]
        public Loan? Loan { get; set; }
    }
}