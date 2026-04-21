using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class ChatAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        public long FileSize { get; set; }

        [Required]
        public byte[] FileData { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MessageId")]
        public virtual ChatMessage Message { get; set; }
    }
}