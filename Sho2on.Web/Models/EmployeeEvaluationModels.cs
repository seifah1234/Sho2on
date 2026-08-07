using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class EvaluationCriteriaModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool? IsSuccessful { get; set; }
        public decimal? Percentage { get; set; }
        public string Notes { get; set; } = "";
    }

    public class SaveEvaluationModel
    {
        public int EmployeeId { get; set; }
        public List<EvaluationCriteriaModel> AdministrativeCriteria { get; set; } = new();
        public List<EvaluationCriteriaModel> TechnicalCriteria { get; set; } = new();
        public EvaluationResult FinalResult { get; set; }
        public string GeneralNotes { get; set; } = "";
        public string AdministrativeNotes { get; set; } = "";
        public string TechnicalNotes { get; set; } = "";
        public decimal AdministrativeScore { get; set; }
        public decimal TechnicalScore { get; set; }
        public decimal TotalScore { get; set; }
    }

    public class EmployeeEvaluationDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime EvaluationDate { get; set; }
        public decimal TotalScore { get; set; }
        public EvaluationResult FinalResult { get; set; }
        public string GeneralNotes { get; set; } = "";
        public string AdministrativeNotes { get; set; } = "";
        public string TechnicalNotes { get; set; } = "";
        public decimal AdministrativeScore { get; set; }
        public decimal TechnicalScore { get; set; }
        public List<EvaluationCriteriaModel> AdministrativeCriteria { get; set; } = new();
        public List<EvaluationCriteriaModel> TechnicalCriteria { get; set; } = new();
    }
}