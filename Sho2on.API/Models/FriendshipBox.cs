// FriendshipBox.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class FriendshipBox
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "صندوق الزمالة المشترك";

        [Required]
        public decimal CurrentBalance { get; set; } = 0; // الرصيد الحالي للصندوق

        public decimal TotalDeposits { get; set; } = 0; // إجمالي الإيداعات
        public decimal TotalLoans { get; set; } = 0; // إجمالي السلف المسحوبة
        public decimal TotalRepayments { get; set; } = 0; // إجمالي السداد

        [Required]
        public decimal DeductionPercentage { get; set; } = 2.0m; // نسبة الخصم من الراتب (2% افتراضياً)

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}