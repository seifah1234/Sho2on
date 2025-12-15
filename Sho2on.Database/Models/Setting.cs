using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class Setting
    {
        [Key]
        public int Id { get; set; }
        public int MaxMobileUsers { get; set; } = 0;

        public string? CentralDocumentStoragePath { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
