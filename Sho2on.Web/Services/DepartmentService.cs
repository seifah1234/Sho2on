using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

public class DepartmentService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public DepartmentService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Departments.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto { Id = d.Id, Name = d.Name, IsHR = d.IsHR })
            .ToListAsync();
    }

    public async Task SaveAsync(int? id, string name, bool isHR)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Departments.FirstOrDefaultAsync(d => d.Name == name);
        if (existing != null)
        {
            if (!id.HasValue || existing.Id != id.Value) throw new Exception("اسم القسم موجود مسبقاً");
        }
        Department entity;
        if (id.HasValue) entity = await db.Departments.FindAsync(id.Value) ?? throw new Exception("غير موجود");
        else { entity = new Department(); db.Departments.Add(entity); }
        entity.Name = name; entity.IsHR = isHR; entity.EditedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Departments.FindAsync(id) ?? throw new Exception("غير موجود");
        db.Departments.Remove(entity); await db.SaveChangesAsync();
    }
}

public class DepartmentDto { public int Id { get; set; } public string Name { get; set; } = ""; public bool? IsHR { get; set; } }