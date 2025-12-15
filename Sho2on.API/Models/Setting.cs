using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.API.Models
{
    public class Setting
    {
        [Key]
        public int Id { get; set; }
        public int MaxMobileUsers { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
