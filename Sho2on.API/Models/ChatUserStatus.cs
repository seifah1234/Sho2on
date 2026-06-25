// Sho2on.Database/Models/ChatUserStatus.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatUserStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int UnreadCount { get; set; } = 0;

        public DateTime? LastReadAt { get; set; }

        [ForeignKey("ChatId")]
        public virtual Chat Chat { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}