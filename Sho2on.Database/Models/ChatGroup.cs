using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public byte[] GroupImageData { get; set; }

        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; }

        public virtual ICollection<ChatGroupMember> Members { get; set; }
        public virtual ICollection<ChatGroupMessage> Messages { get; set; }
    }
}