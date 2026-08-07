using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class TaskListItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public UserTaskType Type { get; set; }
        public UserTaskStatus Status { get; set; }
        public string AssignedToName { get; set; } = "";
        public string AssignedByName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Now && Status != UserTaskStatus.Completed;
    }
}