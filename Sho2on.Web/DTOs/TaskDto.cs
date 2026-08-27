namespace Sho2on.Web.DTOs
{
    public class TaskDto
    {
        public int AssignedByUserId { get; set; }
        public int AssignedToUserId { get; set; }
        public string Description { get; set; } = "";

        public string Type { get; set; } = "task";
    }
}
