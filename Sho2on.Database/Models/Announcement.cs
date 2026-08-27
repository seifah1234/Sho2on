using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public int CreatedById { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AnnouncementTypeId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ExpireDate { get; set; }

        public bool IsDeleted { get; set; }

        [ForeignKey("AnnouncementTypeId")]
        public AnnouncementType AnnouncementType { get; set; } = null!;
        
        [ForeignKey("CreatedById")]
        public User CreatedBy { get; set; } = null!;
    }
}
