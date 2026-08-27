namespace Sho2on.API.Dtos
{
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsActive { get; set; }
        public bool DeductFromBalance { get; set; }
        public int? DefaultBalance { get; set; }
    }

}
