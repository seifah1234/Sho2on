using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatGroupAttachment
    {
        [Key]
        public int Id { get; set; }

        public int MessageId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public byte[] FileData { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MessageId")]
        public virtual ChatGroupMessage Message { get; set; }
    }
}