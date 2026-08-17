// Services/QualificationService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class QualificationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public QualificationService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<Qualification>> GetAllAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Qualifications.OrderBy(q => q.Name).ToListAsync();
        }

        public async Task SaveAsync(int? id, string name)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();

            var existing = await _db.Qualifications.FirstOrDefaultAsync(q => q.Name == name && q.Id != id);
            if (existing != null)
            {
                if (existing.Id == id)
                {
                    // The name is the same as the existing record, no need to update
                    return;
                }
                else
                {
                    throw new Exception("اسم المؤهل موجود مسبقاً");
                }
            }
            Qualification q;
            if (id.HasValue) q = await _db.Qualifications.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { q = new Qualification(); _db.Qualifications.Add(q); }
            q.Name = name;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var q = await _db.Qualifications.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.Qualifications.Remove(q);
            await _db.SaveChangesAsync();
        }
    }
}