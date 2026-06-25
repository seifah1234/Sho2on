using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sho2on.Database.Models
{
    public class UserTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Description { get; set; }
        public int Type { get; set; } = (int)UserTaskType.Task;
        public int Status { get; set; } = (int)UserTaskStatus.Sent;

        [ForeignKey("AssignedToUser")]
        public int AssignedToUserId { get; set; }
        public virtual User AssignedToUser { get; set; }

        [ForeignKey("AssignedByUser")]
        public int AssignedByUserId { get; set; }
        public virtual User AssignedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public string StatusText 
        {
            get
            {
                switch (Status)
                {
                    case (int)UserTaskStatus.Sent:
                        return "ارسلت";
                    case (int)UserTaskStatus.Received:
                        return "استلمت";
                    case (int)UserTaskStatus.OnHold:
                        return "معلقة";
                    case (int)UserTaskStatus.InProgress:
                        return "قيد التنفيذ";
                    case (int)UserTaskStatus.Completed:
                        return "مكتملة";
                    default:
                        return "Unknown Status";
                }
            }
        }

        public string TypeText
        {
            get
            {
                switch (Type)
                {
                    case (int)UserTaskType.Task:
                        return "مهمة";
                    case (int)UserTaskType.Order:
                        return "طلب";
                    default:
                        return "غير معروف";
                }
            }
        }
    }

    public enum UserTaskStatus
    {
        Sent = 4,
        Received = 0,
        OnHold = 1,
        InProgress = 2,
        Completed = 3
    }

    public enum UserTaskType
    {
        Task = 1,
        Order = 2
    }

}
