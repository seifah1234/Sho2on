namespace Sho2on.API.Dtos
{
    public class RecordDto
    {
        public int UserId { get; set; }
        public int Status { get; set; }
        public int BranchId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationName { get; set; }

        public DateTime? DeviceTime { get; set; }
    }

}
