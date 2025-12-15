using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class UserRole
    {

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public int RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; }
    }
}
