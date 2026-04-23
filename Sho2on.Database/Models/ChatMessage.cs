using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ChatId { get; set; }

        [ForeignKey("Sender")]
        public int SenderId { get; set; }
        [ForeignKey("Receiver")]
        public int ReceiverId { get; set; }

        public string Message { get; set; }

        public bool? IsDelivered { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public virtual Chat Chat { get; set; }

        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}
