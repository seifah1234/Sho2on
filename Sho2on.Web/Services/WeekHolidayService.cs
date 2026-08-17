// Services/WeekHolidayService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class WeekHolidayService
    {
        private readonly AppDbContext _db;
        public WeekHolidayService(AppDbContext db) => _db = db;

        public async Task<List<WeekHoliday>> GetAllAsync() => await _db.WeekHolidays.OrderBy(w => w.Name).ToListAsync();

        public async Task SaveAsync(int? id, string name, bool[] days)
        {
            WeekHoliday w;
            if (id.HasValue) w = await _db.WeekHolidays.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { w = new WeekHoliday(); _db.WeekHolidays.Add(w); }

            w.Name = name;
            w.Day1 = days[0]; w.Day2 = days[1]; w.Day3 = days[2]; w.Day4 = days[3];
            w.Day5 = days[4]; w.Day6 = days[5]; w.Day7 = days[6];

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var w = await _db.WeekHolidays.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.WeekHolidays.Remove(w);
            await _db.SaveChangesAsync();
        }
    }
}