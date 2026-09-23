using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class ReasonService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ReasonService(IDbContextFactory<AppDbContext> dbFactory)
            => _dbFactory = dbFactory;

        public async Task<List<ReasonDto>> GetAllAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Reasons
                .OrderBy(a => a.Name)
                .Select(a => new ReasonDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    ReasonType = a.ReasonType
                })
                .ToListAsync();
        }

        public async Task SaveAsync(int? id, string name, ReasonType reasonType)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // منع التكرار: نفس الاسم + نفس النوع
            var existing = await db.Reasons
                .FirstOrDefaultAsync(a => a.Name == name && a.ReasonType == reasonType);

            if (existing != null && (!id.HasValue || existing.Id != id.Value))
                throw new Exception("السبب موجود مسبقاً");

            Reason entity;
            if (id.HasValue)
                entity = await db.Reasons.FindAsync(id.Value)
                         ?? throw new Exception("غير موجود");
            else
            {
                entity = new Reason();
                db.Reasons.Add(entity);
            }

            entity.Name = name;
            entity.ReasonType = reasonType;
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var entity = await db.Reasons.FindAsync(id)
                         ?? throw new Exception("غير موجود");
            db.Reasons.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public class ReasonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ReasonType ReasonType { get; set; }
    }
}