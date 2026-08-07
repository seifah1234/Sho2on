namespace Sho2on.Database.Models
{
    public class AbsenceTier
    {
        public int Id { get; set; }
        public int FromOccurrence { get; set; }      
        public int? ToOccurrence { get; set; }
        public decimal DeductionMultiplier { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}