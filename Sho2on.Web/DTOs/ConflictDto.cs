namespace Sho2on.API.Dtos
{
    public class ConflictDto
    {
        public DateTime ConflictStartDate { get; set; }
        public DateTime ConflictEndDate { get; set; }
        public string LeaveTypeName { get; set; }
        public string Status { get; set; }
    }
}