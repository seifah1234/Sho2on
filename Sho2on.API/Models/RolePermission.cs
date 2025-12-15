using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class RolePermission
    {
        public int RoleID { get; set; }
        [ForeignKey(nameof(RoleID))]
        public Role Role { get; set; }

        public int PermissionID { get; set; }
        [ForeignKey(nameof(PermissionID))]
        public Permission Permission { get; set; }
    }
}
