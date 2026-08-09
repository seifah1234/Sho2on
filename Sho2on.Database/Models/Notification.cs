namespace Sho2on.Database.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }           // مين هيستقبل الإشعار
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Icon { get; set; } = "bi-bell";
        public string? Url { get; set; }           // فين يودّيك لما تدوس عليه
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User User { get; set; }
    }
}