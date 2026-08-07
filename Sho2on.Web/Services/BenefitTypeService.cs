using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class BenefitTypeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public BenefitTypeService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<BenefitTypeDto>> GetAllAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.BenefitTypes
                .OrderBy(b => b.Type)
                .ThenBy(b => b.Name)
                .Select(b => new BenefitTypeDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Type = b.Type,
                    Percentage = b.Percentage,
                    SalaryTarget = b.SalaryTarget,
                    Frequency = b.Frequency ?? "Monthly",
                    Description = b.Description,
                    IsActive = b.IsActive
                })
                .ToListAsync();
        }

        public async Task SaveAsync(int? id, string name, string type, decimal percentage, string salaryTarget, string frequency, string? description, bool isActive)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            BenefitType entity;

            if (id.HasValue)
                entity = await db.BenefitTypes.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else
            {
                entity = new BenefitType { CreatedAt = DateTime.Now };
                db.BenefitTypes.Add(entity);
            }

            entity.Name = name;
            entity.Type = type;
            entity.Percentage = percentage;
            entity.SalaryTarget = salaryTarget;
            entity.Frequency = frequency;
            entity.Description = description;
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.BenefitTypes.FindAsync(id) ?? throw new Exception("غير موجود");
            db.BenefitTypes.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}