namespace Sho2on.Web.Models
{
    public class BenefitTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Benefit";
        public decimal Percentage { get; set; }
        public string SalaryTarget { get; set; } = "Total";
        public string Frequency { get; set; } = "Monthly"; // Monthly, Once
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}