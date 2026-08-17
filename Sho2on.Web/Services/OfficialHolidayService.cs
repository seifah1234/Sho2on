// Services/OfficialHolidayService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class OfficialHolidayService
    {
        private readonly AppDbContext _db;
        public OfficialHolidayService(AppDbContext db) => _db = db;

        public async Task<List<OfficialHoliday>> GetAllAsync() => await _db.OfficialHolidays.OrderBy(h => h.Date).ToListAsync();

        public async Task SaveAsync(int? id, string name, DateOnly date)
        {
            OfficialHoliday h;
            if (id.HasValue) h = await _db.OfficialHolidays.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { h = new OfficialHoliday(); _db.OfficialHolidays.Add(h); }
            h.Name = name; h.Date = date.ToDateTime(TimeOnly.MinValue);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var h = await _db.OfficialHolidays.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.OfficialHolidays.Remove(h);
            await _db.SaveChangesAsync();
        }
    }
}