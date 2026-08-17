using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class OfficialService
    {
        private readonly AppDbContext _db;
        public OfficialService(AppDbContext db) => _db = db;

        public async Task<List<Offical>> GetAllAsync() =>
            await _db.Officials.Include(o => o.User).OrderBy(o => o.Name).ToListAsync();

        public async Task SaveAsync(int? id, string name, int userId)
        {
            Offical o;
            if (id.HasValue) o = await _db.Officials.FindAsync(id.Value) ?? throw new Exception("غير موجود");
            else { o = new Offical(); _db.Officials.Add(o); }

            o.Name = name;
            o.UserId = userId;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var o = await _db.Officials.FindAsync(id) ?? throw new Exception("غير موجود");
            _db.Officials.Remove(o);
            await _db.SaveChangesAsync();
        }
    }
}