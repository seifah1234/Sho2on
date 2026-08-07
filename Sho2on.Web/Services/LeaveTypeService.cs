using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services;

public class LeaveTypeService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public LeaveTypeService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<LeaveTypeListItem>> GetListAsync(string? search = null)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        var query = _db.LeaveTypes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Code.Contains(search));
        }

        return await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new LeaveTypeListItem
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DefaultBalance = x.DefaultBalance,
                IsActive = x.IsActive,
                DeductFromBalance = x.DeductFromBalance,
                RequiresApproval = x.RequiresApproval,
                MaxConsecutiveDays = x.MaxConsecutiveDays
            })
            .ToListAsync();
    }

    public async Task<LeaveTypeFormModel?> GetByIdAsync(int id)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        return await _db.LeaveTypes
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new LeaveTypeFormModel
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DefaultBalance = x.DefaultBalance,
                IsActive = x.IsActive,
                DeductFromBalance = x.DeductFromBalance,
                RequiresApproval = x.RequiresApproval,
                MaxConsecutiveDays = x.MaxConsecutiveDays,
                Notes = x.Notes
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(LeaveTypeFormModel model)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        var name = model.Name.Trim();
        var code = model.Code.Trim().ToUpperInvariant();

        var duplicateExists = await _db.LeaveTypes.AnyAsync(x =>
            x.Id != model.Id &&
            (x.Name == name || x.Code == code));

        if (duplicateExists)
            throw new InvalidOperationException("يوجد نوع إجازة بنفس الاسم أو الكود");

        LeaveType entity;

        if (model.Id.HasValue)
        {
            entity = await _db.LeaveTypes.FindAsync(model.Id.Value)
                ?? throw new InvalidOperationException("نوع الإجازة غير موجود");
        }
        else
        {
            entity = new LeaveType
            {
                CreatedAt = DateTime.Now
            };

            _db.LeaveTypes.Add(entity);
        }

        entity.Name = name;
        entity.Code = code;
        entity.DefaultBalance = model.DefaultBalance;
        entity.IsActive = model.IsActive;
        entity.DeductFromBalance = model.DeductFromBalance;
        entity.RequiresApproval = model.RequiresApproval;
        entity.MaxConsecutiveDays = model.MaxConsecutiveDays;
        entity.Notes = string.IsNullOrWhiteSpace(model.Notes)
            ? null
            : model.Notes.Trim();
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
            using var _db = await _dbFactory.CreateDbContextAsync();
        var entity = await _db.LeaveTypes
            .Include(x => x.Leaves)
            .Include(x => x.LeaveBalances)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new InvalidOperationException("نوع الإجازة غير موجود");

        if (entity.Leaves.Any() || entity.LeaveBalances.Any())
        {
            throw new InvalidOperationException(
                "لا يمكن حذف نوع الإجازة لأنه مرتبط بطلبات أو أرصدة إجازات");
        }

        _db.LeaveTypes.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public class LeaveTypeListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int DefaultBalance { get; set; }
        public bool IsActive { get; set; }
        public bool DeductFromBalance { get; set; }
        public bool RequiresApproval { get; set; }
        public int? MaxConsecutiveDays { get; set; }
    }
}