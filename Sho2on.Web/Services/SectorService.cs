using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

public class SectorService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public SectorService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;
    public async Task<List<SectorDto>> GetAllAsync() { using var db = await _dbFactory.CreateDbContextAsync(); return await db.Degrees.OrderBy(a => a.Name).Select(a => new SectorDto { Id = a.Id, Name = a.Name }).ToListAsync(); }
    public async Task SaveAsync(int? id, string name) {
        using var db = await _dbFactory.CreateDbContextAsync(); 
        var existing = await db.Degrees.FirstOrDefaultAsync(a => a.Name == name);
        if (existing != null)
        {
            if (!id.HasValue || existing.Id != id.Value)
                throw new Exception("القطاع موجودة مسبقاً");
        }
        Degree entity; 
        if (id.HasValue) 
            entity = await db.Degrees.FindAsync(id.Value) ?? throw new Exception("غير موجود"); 
        else { 
            entity = new Degree(); 
            db.Degrees.Add(entity); 
        } 
        entity.Name = name; 
        await db.SaveChangesAsync(); 
    }
    public async Task DeleteAsync(int id) { using var db = await _dbFactory.CreateDbContextAsync(); var entity = await db.Degrees.FindAsync(id) ?? throw new Exception("غير موجود"); db.Degrees.Remove(entity); await db.SaveChangesAsync(); }
}
public class SectorDto { public int Id { get; set; } public string Name { get; set; } = ""; }