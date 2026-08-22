namespace Sho2on.API.Dtos
{
    public class DateConflictCheckDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool HasConflicts { get; set; }
        public List<ConflictDto> Conflicts { get; set; } = new List<ConflictDto>();
    }
}
