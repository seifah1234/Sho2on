// Sho2on.Database/Models/ChatGroupMessageRead.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatGroupMessageRead
    {
        [Key]
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.Now;

        [ForeignKey("MessageId")]
        public virtual ChatGroupMessage Message { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}