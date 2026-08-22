// EmployeeEvaluation.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.API.Models
{
    public class EmployeeEvaluation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int EvaluatorId { get; set; }

        [Required]
        public DateTime EvaluationDate { get; set; } = DateTime.Now;

        [Required]
        public EvaluationStatus Status { get; set; } = EvaluationStatus.Draft;

        [Required]
        public decimal TotalScore { get; set; }

        [Required]
        public decimal MaxPossibleScore { get; set; }

        [Required]
        public decimal SuccessPercentage { get; set; }

        [Required]
        public EvaluationResult FinalResult { get; set; }

        public string GeneralNotes { get; set; }

        // العلاقات
        [ForeignKey(nameof(EmployeeId))]
        public virtual User Employee { get; set; }

        [ForeignKey(nameof(EvaluatorId))]
        public virtual User Evaluator { get; set; }

        public virtual ICollection<EvaluationCriteria> EvaluationCriterias { get; set; }
    }

    public class EvaluationCriteria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EvaluationId { get; set; }

        [Required]
        [StringLength(200)]
        public string CriteriaName { get; set; }

        [Required]
        public decimal Score { get; set; }

        [Required]
        public decimal MaxScore { get; set; }

        [Required]
        public bool IsSuccessful { get; set; }

        public string Notes { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        [ForeignKey(nameof(EvaluationId))]
        public virtual EmployeeEvaluation Evaluation { get; set; }
    }

    public enum EvaluationStatus
    {
        Draft = 0,
        Completed = 1,
        Archived = 2
    }

    public enum EvaluationResult
    {
        Successful = 1,
        Unsuccessful = 2,
        Conditional = 3
    }
}