// Services/ShiftService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class ShiftService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public ShiftService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public async Task<List<Shift>> GetAllAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Shifts.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task SaveAsync(int? id, string name, TimeSpan start, TimeSpan end)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var existingShift = await _db.Shifts.FirstOrDefaultAsync(s => s.Name == name && s.Id != id);
            if (existingShift != null)
            {
                if (existingShift.Id == id)
                {
                    return;
                }
                throw new Exception("اسم الوردية موجود مسبقاً");

            }
            Shift s;
            if (id.HasValue) s = await _db.Shifts.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { s = new Shift(); _db.Shifts.Add(s); }
            s.Name = name; s.StartTime = start; s.EndTime = end;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var s = await _db.Shifts.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.Shifts.Remove(s);
            await _db.SaveChangesAsync();
        }
    }
}