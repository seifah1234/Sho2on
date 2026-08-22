using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    [Table("UserBranches")]
    public class UserBranch
    {
        public int UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        public User User { get; set; }

        public int BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public Branch Branch { get; set; }
    }
}
