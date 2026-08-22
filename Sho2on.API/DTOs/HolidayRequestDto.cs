namespace Sho2on.API.Dtos
{
    public class HolidayRequestDto
    {
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration { get; set; }
        public string Reason { get; set; }
        public int? ApprovingManagerId { get; set; }
        public bool SaveAsDraft { get; set; }
    }

}
