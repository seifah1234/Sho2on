using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services;

public class LeaveBalanceService
{
    private readonly AppDbContext _db;

    public LeaveBalanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeOption>> SearchEmployeesAsync(string? search)
    {
        var query = _db.Users
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Where(x => !x.IsArchived)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.FullName.Contains(search) ||
                x.Code.Contains(search));
        }

        return await query
            .OrderBy(x => x.FullName)
            .Take(30)
            .Select(x => new EmployeeOption
            {
                Id = x.Id,
                Code = x.Code,
                FullName = x.FullName,
                BranchName = x.Branch != null ? x.Branch.Name : "",
                DepartmentName = x.Department != null ? x.Department.Name : ""
            })
            .ToListAsync();
    }

    public async Task<List<LeaveBalanceItem>> GetBalancesAsync(int userId)
    {
        var leaveTypes = await _db.LeaveTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync();

        var existingBalances = await _db.LeaveBalances
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .ToDictionaryAsync(x => x.LeaveTypeId);

        // Status = 2 يعني طلب إجازة معتمد
        var usedBalances = await _db.Leaves
            .Where(x => x.UserId == userId && x.Status == 2 && !x.IsCancelled)
            .GroupBy(x => x.LeaveTypeId)
            .Select(x => new
            {
                LeaveTypeId = x.Key,
                UsedBalance = x.Sum(y => y.Duration)
            })
            .ToDictionaryAsync(x => x.LeaveTypeId, x => x.UsedBalance);

        return leaveTypes.Select(type =>
        {
            existingBalances.TryGetValue(type.Id, out var savedBalance);
            usedBalances.TryGetValue(type.Id, out var usedBalance);

            return new LeaveBalanceItem
            {
                LeaveTypeId = type.Id,
                LeaveTypeName = type.Name,
                LeaveTypeCode = type.Code,
                TotalBalance = savedBalance?.TotalBalance ?? type.DefaultBalance,
                UsedBalance = usedBalance
            };
        }).ToList();
    }

    public async Task SaveBalancesAsync(int userId, List<LeaveBalanceItem> items)
    {
        var activeTypeIds = await _db.LeaveTypes
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        var validItems = items
            .Where(x => activeTypeIds.Contains(x.LeaveTypeId))
            .ToList();

        if (validItems.Any(x => x.TotalBalance < 0))
            throw new InvalidOperationException("الرصيد الإجمالي لا يمكن أن يكون أقل من صفر");

        if (validItems.Any(x => x.TotalBalance < x.UsedBalance))
        {
            throw new InvalidOperationException(
                "لا يمكن أن يكون الرصيد الإجمالي أقل من الرصيد المستخدم");
        }

        var existingBalances = await _db.LeaveBalances
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.LeaveTypeId);

        foreach (var item in validItems)
        {
            if (existingBalances.TryGetValue(item.LeaveTypeId, out var balance))
            {
                balance.TotalBalance = item.TotalBalance;
                balance.UsedBalance = item.UsedBalance;
                balance.UpdatedAt = DateTime.Now;
            }
            else
            {
                _db.LeaveBalances.Add(new LeaveBalance
                {
                    UserId = userId,
                    LeaveTypeId = item.LeaveTypeId,
                    TotalBalance = item.TotalBalance,
                    UsedBalance = item.UsedBalance,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public class EmployeeOption
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string FullName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
    }
}