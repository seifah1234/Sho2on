using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class BenefitType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "Benefit"; // Benefit, Deduction

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // ═══ نسبة مئوية من الراتب ═══
        public decimal Percentage { get; set; } = 0;

        // ═══ يطبق على أي جزء من الراتب ═══
        [MaxLength(20)]
        public string SalaryTarget { get; set; } = "Total"; // Fixed, Variable, Total

        // ═══ نوع التكرار ═══
        [MaxLength(20)]
        public string Frequency { get; set; } = "Monthly"; // Monthly, Once

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // العلاقات
        public virtual ICollection<Benefit>? Benefits { get; set; }
    }
}