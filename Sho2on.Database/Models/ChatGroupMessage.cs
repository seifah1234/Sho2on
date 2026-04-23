using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatGroupMessage
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;

        [ForeignKey("GroupId")]
        public virtual ChatGroup Group { get; set; }

        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        public virtual ICollection<ChatGroupAttachment> Attachments { get; set; }
    }
}