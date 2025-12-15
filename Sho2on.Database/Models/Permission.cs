using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Permission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PermissionID { get; set; }

        [Required, StringLength(50)]
        public string PermissionName { get; set; }

        // Navigation
        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}
