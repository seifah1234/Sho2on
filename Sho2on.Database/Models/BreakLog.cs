namespace Sho2on.Database.Models
{
    public class BreakLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BreakId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool ExceededLimit { get; set; }

        public User User { get; set; }
        public Break Break { get; set; }
    }
}