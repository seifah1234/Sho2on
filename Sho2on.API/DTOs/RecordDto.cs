using System;

namespace Sho2on.API.DTOs
{
    public class RecordDto
    {
        public int UserId { get; set; }

        // 0 = CheckIn, 1 = CheckOut
        public int Status { get; set; }

        public int BranchId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string? LocationName { get; set; }

        // Optional: time from device
        public DateTime? DeviceTime { get; set; }
    }
}
