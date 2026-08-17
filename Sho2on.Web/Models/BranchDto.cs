namespace Sho2on.Web.Models
{
    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int? AreaId { get; set; }
        public string AreaName { get; set; } = "";

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int RadiusMeters { get; set; } = 100;
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }
}