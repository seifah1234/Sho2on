using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    [Table("machineData")]
    public class MachineData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserID { get; set; }

        public int BranchCode { get; set; }

        public DateTime TDate { get; set; }

        public DateTime DateOnly { get; set; }

        public string Status { get; set; }

        public int Punch { get; set; }

        public string MIP { get; set; }

        public int StatusNo { get; set; }

        [ForeignKey("BranchCode")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}
