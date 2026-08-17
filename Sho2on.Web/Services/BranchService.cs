using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

public class BranchService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public BranchService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<List<BranchDto>> GetAllAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Branches
            .Include(b => b.Area)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                AreaId = b.AreaId,
                AreaName = b.Area != null ? b.Area.Name : "",

                Latitude = b.Latitude,
                Longitude = b.Longitude,
                RadiusMeters = b.RadiusMeters
            })
            .ToListAsync();
    }

    public async Task SaveAsync(
    int? id,
    string name,
    int? areaId,
    double? latitude,
    double? longitude,
    int radiusMeters)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Branches
            .FirstOrDefaultAsync(b => b.Name == name);

        if (existing != null)
        {
            if (!id.HasValue || existing.Id != id.Value)
                throw new Exception("الفرع موجود مسبقاً");
        }

        Branch entity;

        if (id.HasValue)
        {
            entity = await db.Branches.FindAsync(id.Value)
                ?? throw new Exception("غير موجود");
        }
        else
        {
            entity = new Branch();
            db.Branches.Add(entity);
        }

        entity.Name = name;
        entity.AreaId = areaId;

        entity.Latitude = latitude;
        entity.Longitude = longitude;
        entity.RadiusMeters = radiusMeters;

        entity.EditedAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Branches.FindAsync(id) ?? throw new Exception("غير موجود");
        db.Branches.Remove(entity); await db.SaveChangesAsync();
    }
}