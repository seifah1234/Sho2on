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

        [ForeignKey("Area")]
        public int? AreaId { get; set; }

        public virtual Area Area { get; set; }

        public DateTime? EditedAt { get; set; } = DateTime.Now;

        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();

        public virtual ICollection<User> Users { get; set; } = new List<User>();

    }
}
