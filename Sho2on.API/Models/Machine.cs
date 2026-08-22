using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class Machine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MIP { get; set; }

        [Required]
        [MaxLength(50)]
        public string SIP { get; set; }

        public Branch? Branch { get; set; }
    }
}