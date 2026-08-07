namespace Sho2on.Web.Models
{
    public class BranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }
}