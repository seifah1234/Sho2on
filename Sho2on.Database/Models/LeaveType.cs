using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class LeaveType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; }

        public int DefaultBalance { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool DeductFromBalance { get; set; } = true;

        public bool RequiresApproval { get; set; } = true;

        public int? MaxConsecutiveDays { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Leave> Leaves { get; set; }
        public ICollection<LeaveBalance> LeaveBalances { get; set; }

        // خاصية محسوبة
        [NotMapped]
        public string StatusText => IsActive ? "نشط" : "غير نشط";

        [NotMapped]
        public string DeductText => DeductFromBalance ? "يخصم" : "لا يخصم";

        [NotMapped]
        public string ApprovalText => RequiresApproval ? "يتطلب موافقة" : "لا يتطلب موافقة";
    }
}