namespace Sho2on.Database.Models
{
    public class AnnouncementType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#000000";

        public List<Announcement> Announcements = new List<Announcement>();
    }
}