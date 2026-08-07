using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class EvaluationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EvaluationService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<EmployeeEvaluationDto?> GetLatestAsync(int employeeId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var evaluation = await db.EmployeeEvaluations
                .Include(e => e.EvaluationCriterias)
                .Where(e => e.EmployeeId == employeeId)
                .OrderByDescending(e => e.EvaluationDate)
                .FirstOrDefaultAsync();

            if (evaluation == null) return null;

            return new EmployeeEvaluationDto
            {
                Id = evaluation.Id,
                EmployeeId = evaluation.EmployeeId,
                EvaluationDate = evaluation.EvaluationDate,
                TotalScore = evaluation.TotalScore,
                FinalResult = evaluation.FinalResult,
                GeneralNotes = evaluation.GeneralNotes ?? "",
                AdministrativeNotes = evaluation.AdministrativeNotes ?? "",
                TechnicalNotes = evaluation.TechnicalNotes ?? "",
                AdministrativeScore = evaluation.AdministrativeScore,
                TechnicalScore = evaluation.TechnicalScore,
                AdministrativeCriteria = evaluation.EvaluationCriterias
                    .Where(c => c.EvaluationType == EvaluationType.Administrative)
                    .Select(c => new EvaluationCriteriaModel
                    {
                        Id = c.Id,
                        Name = c.CriteriaName,
                        IsSuccessful = c.IsSuccessful,
                        Percentage = c.Score,
                        Notes = c.Notes ?? ""
                    }).ToList(),
                TechnicalCriteria = evaluation.EvaluationCriterias
                    .Where(c => c.EvaluationType == EvaluationType.Technical)
                    .Select(c => new EvaluationCriteriaModel
                    {
                        Id = c.Id,
                        Name = c.CriteriaName,
                        IsSuccessful = c.IsSuccessful,
                        Percentage = c.Score,
                        Notes = c.Notes ?? ""
                    }).ToList()
            };
        }

        public async Task SaveAsync(SaveEvaluationModel model)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // حذف التقييمات القديمة
            var oldEvaluations = await db.EmployeeEvaluations
                .Where(e => e.EmployeeId == model.EmployeeId)
                .ToListAsync();

            db.EmployeeEvaluations.RemoveRange(oldEvaluations);
            await db.SaveChangesAsync();

            // إنشاء تقييم جديد
            var evaluation = new EmployeeEvaluation
            {
                EmployeeId = model.EmployeeId,
                EvaluatorId = 1, // TODO: استخدم ID المستخدم الحالي
                EvaluationDate = DateTime.Now,
                Status = EvaluationStatus.Completed,
                TotalScore = model.TotalScore,
                MaxPossibleScore = 100,
                SuccessPercentage = model.TotalScore,
                FinalResult = model.FinalResult,
                GeneralNotes = model.GeneralNotes,
                AdministrativeNotes = model.AdministrativeNotes,
                TechnicalNotes = model.TechnicalNotes,
                AdministrativeScore = model.AdministrativeScore,
                TechnicalScore = model.TechnicalScore,
                EvaluationCriterias = new List<EvaluationCriteria>()
            };

            int orderIndex = 1;

            // إضافة المعايير الإدارية
            foreach (var criteria in model.AdministrativeCriteria)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Name))
                {
                    evaluation.EvaluationCriterias.Add(new EvaluationCriteria
                    {
                        CriteriaName = criteria.Name,
                        Score = criteria.Percentage ?? 0,
                        MaxScore = 100,
                        IsSuccessful = criteria.IsSuccessful ?? false,
                        Notes = criteria.Notes,
                        OrderIndex = orderIndex++,
                        EvaluationType = EvaluationType.Administrative
                    });
                }
            }

            // إضافة المعايير الفنية
            foreach (var criteria in model.TechnicalCriteria)
            {
                if (!string.IsNullOrWhiteSpace(criteria.Name))
                {
                    evaluation.EvaluationCriterias.Add(new EvaluationCriteria
                    {
                        CriteriaName = criteria.Name,
                        Score = criteria.Percentage ?? 0,
                        MaxScore = 100,
                        IsSuccessful = criteria.IsSuccessful ?? false,
                        Notes = criteria.Notes,
                        OrderIndex = orderIndex++,
                        EvaluationType = EvaluationType.Technical
                    });
                }
            }

            db.EmployeeEvaluations.Add(evaluation);
            await db.SaveChangesAsync();
        }

        public async Task<List<EmployeeEvaluationDto>> GetEmployeeEvaluationsAsync(int employeeId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.EmployeeEvaluations
                .Where(e => e.EmployeeId == employeeId)
                .OrderByDescending(e => e.EvaluationDate)
                .Select(e => new EmployeeEvaluationDto
                {
                    Id = e.Id,
                    EmployeeId = e.EmployeeId,
                    EvaluationDate = e.EvaluationDate,
                    TotalScore = e.TotalScore,
                    FinalResult = e.FinalResult,
                    GeneralNotes = e.GeneralNotes ?? "",
                    AdministrativeScore = e.AdministrativeScore,
                    TechnicalScore = e.TechnicalScore
                })
                .ToListAsync();
        }
    }
}