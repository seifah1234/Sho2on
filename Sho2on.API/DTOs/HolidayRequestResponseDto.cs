namespace Sho2on.API.Dtos
{
    public class HolidayRequestResponseDto
    {
        public int RequestId { get; set; }
        public string RequestNumber { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public int? ApprovalManagerId { get; set; }
        public string ApprovalManagerName { get; set; }
    }
}
