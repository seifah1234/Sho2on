using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

public class JobTitleService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public JobTitleService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;
    public async Task<List<JobTitleDto>> GetAllAsync() { using var db = await _dbFactory.CreateDbContextAsync(); return await db.JobTitles.OrderBy(j => j.Name).Select(j => new JobTitleDto { Id = j.Id, Name = j.Name, IsManager = j.IsManager, IsHR = j.IsHR }).ToListAsync(); }
    public async Task SaveAsync(int? id, string name, bool isManager, bool isHR) { 
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.JobTitles.FirstOrDefaultAsync(j => j.Name == name);
        if (existing != null)
        {
            if (!id.HasValue || existing.Id != id.Value)
                throw new Exception("المسمى الوظيفي موجود مسبقاً");
        }
        JobTitle entity;
        if (id.HasValue) 
            entity = await db.JobTitles.FindAsync(id.Value) ?? throw new Exception("غير موجود");
        else { 
            entity = new JobTitle();
            db.JobTitles.Add(entity);
        } 
        entity.Name = name;
        entity.IsManager = isManager;
        entity.IsHR = isHR;
        entity.EditedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }
    public async Task DeleteAsync(int id) { using var db = await _dbFactory.CreateDbContextAsync(); var entity = await db.JobTitles.FindAsync(id) ?? throw new Exception("غير موجود"); db.JobTitles.Remove(entity); await db.SaveChangesAsync(); }
}
public class JobTitleDto { public int Id { get; set; } public string Name { get; set; } = ""; public bool? IsManager { get; set; } public bool? IsHR { get; set; } }