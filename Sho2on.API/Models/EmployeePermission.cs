using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.API.Models
{
    public class EmployeePermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PermissionType { get; set; } // Errand, Leave, Emergency, Medical, etc.

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        public double Duration { get; set; } // بالساعات

        [MaxLength(500)]
        public string Reason { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public decimal? DeductedAmount { get; set; } // المبلغ المقتطع

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ApprovedByUserId))]
        public User? ApprovedByUser { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}
