using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public DateTime? EditedAt { get; set; } = DateTime.Now;

        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    }
}
