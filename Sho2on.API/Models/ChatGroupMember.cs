using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatGroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public int UserId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public int UnreadCount { get; set; } = 0;

        [ForeignKey("GroupId")]
        public virtual ChatGroup Group { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}