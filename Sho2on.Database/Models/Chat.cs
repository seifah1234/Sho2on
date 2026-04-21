using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Chat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("FirstUser")]
        public int FirstUserId { get; set; }
        [ForeignKey("SecondUser")]
        public int SecondUserId { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<ChatMessage> Messages { get; set; }
        public virtual User FirstUser { get; set; }

        public virtual User SecondUser { get; set; }
    }
}
