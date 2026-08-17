using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

public class AreaService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public AreaService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;
    public async Task<List<AreaDto>> GetAllAsync() { using var db = await _dbFactory.CreateDbContextAsync(); return await db.Areas.OrderBy(a => a.Name).Select(a => new AreaDto { Id = a.Id, Name = a.Name }).ToListAsync(); }
    public async Task SaveAsync(int? id, string name) {
        using var db = await _dbFactory.CreateDbContextAsync(); 
        var existing = await db.Areas.FirstOrDefaultAsync(a => a.Name == name);
        if (existing != null)
        {
            if (!id.HasValue || existing.Id != id.Value)
                throw new Exception("المنطقة موجودة مسبقاً");
        }
        Area entity; 
        if (id.HasValue) 
            entity = await db.Areas.FindAsync(id.Value) ?? throw new Exception("غير موجود"); 
        else { 
            entity = new Area(); 
            db.Areas.Add(entity); 
        } 
        entity.Name = name; 
        await db.SaveChangesAsync(); 
    }
    public async Task DeleteAsync(int id) { using var db = await _dbFactory.CreateDbContextAsync(); var entity = await db.Areas.FindAsync(id) ?? throw new Exception("غير موجود"); db.Areas.Remove(entity); await db.SaveChangesAsync(); }
}
public class AreaDto { public int Id { get; set; } public string Name { get; set; } = ""; }