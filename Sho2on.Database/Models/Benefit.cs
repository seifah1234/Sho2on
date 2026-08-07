// Sho2on.Database/Models/Benefit.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Benefit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BenefitTypeId { get; set; } // نوع الاستحقاق/الاستقطاع

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // العلاقات
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(BenefitTypeId))]
        public BenefitType? BenefitType { get; set; }
    }
}