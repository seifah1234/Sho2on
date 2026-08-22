using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class ProfileService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly LocalizationService Lang;
        public ProfileService(IDbContextFactory<AppDbContext> dbFactory, LocalizationService lang)
        {
            _dbFactory = dbFactory;
            Lang = lang;
        }

        public async Task<MyProfileInfo?> GetMyProfileAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users
                .Include(u => u.JobTitle).Include(u => u.Department).Include(u => u.Branch).Include(u => u.Manager)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var balance = await db.LeaveBalances.FirstOrDefaultAsync(lb => lb.UserId == userId);

            return new MyProfileInfo
            {
                Id = user.Id,
                FullName = user.FullName,
                Code = user.Code,
                Username = user.Username ?? "",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                JobTitleName = user.JobTitle?.Name ?? "",
                DepartmentName = user.Department?.Name ?? "",
                BranchName = user.Branch?.Name ?? "",
                ManagerName = user.Manager?.FullName,
                HireDate = user.HireDate,
                BirthDate = user.BirthDate,
                LeaveBalance = (balance?.TotalBalance ?? 0) - (balance?.UsedBalance ?? 0),
                LeaveUsed = balance?.UsedBalance ?? 0
            };
        }

        public async Task<(bool Success, string Message)> UpdateUsernameAsync(int userId, string newUsername)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var exists = await db.Users.AnyAsync(u => u.Username == newUsername && u.Id != userId);
            if (exists) return (false, "اسم المستخدم مستخدم بالفعل");

            var user = await db.Users.FindAsync(userId);
            if (user == null) return (false, "المستخدم غير موجود");

            user.Username = newUsername;
            await db.SaveChangesAsync();
            return (true, "تم تحديث اسم المستخدم بنجاح");
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users.FindAsync(userId);
            if (user == null) return (false, "المستخدم غير موجود");

            bool currentValid = user.PasswordHash.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)
                : user.PasswordHash == currentPassword; // دعم الحسابات القديمة اللي لسه plain text

            if (!currentValid) return (false, "كلمة المرور الحالية غير صحيحة");

            if (newPassword.Length < 6) return (false, "كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await db.SaveChangesAsync();
            return (true, "تم تغيير كلمة المرور بنجاح");
        }

        public async Task<List<MyRequestItem>> GetMyRequestsAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var items = new List<MyRequestItem>();

            var leaves = await db.Leaves
                .Include(l => l.LeaveType)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.RequestDate)
                .Take(50)
                .ToListAsync();
            items.AddRange(leaves.Select(l => new MyRequestItem
            {
                Type = "إجازة",
                Icon = "bi-calendar-check",
                Description = $"{l.LeaveType?.Name} — {l.StartDate:dd/MM/yyyy} {Lang.T("إلى")} {l.EndDate:dd/MM/yyyy}",
                RequestDate = l.RequestDate,
                Status = LeaveStatusLabel(l.Status),
                StatusClass = LeaveStatusClass(l.Status)
            }));

            var permissions = await db.EmployeePermissions
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
            items.AddRange(permissions.Select(p => new MyRequestItem
            {
                Type = "إذن",
                Icon = "bi-clock",
                Description = $"{p.StartDateTime:dd/MM/yyyy} — {p.StartDateTime:hh\\:mm} {Lang.T("إلى")} {p.EndDateTime:hh\\:mm}",
                RequestDate = p.CreatedAt,
                Status = GenericStatusLabel(p.Status),
                StatusClass = GenericStatusClass(p.Status)
            }));

            var missions = await db.Procedures
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();
            items.AddRange(missions.Select(m => new MyRequestItem
            {
                Type = "مأمورية",
                Icon = "bi-briefcase",
                Description = $"{m.StartDate:dd/MM/yyyy} {Lang.T("إلى")} {m.EndDate:dd/MM/yyyy}",
                RequestDate = m.CreatedAt?? DateTime.Now,
                Status = GenericStatusLabel(m.Status),
                StatusClass = GenericStatusClass(m.Status)
            }));

            var loans = await db.Loans
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .ToListAsync();
            items.AddRange(loans.Select(l => new MyRequestItem
            {
                Type = "سلفة",
                Icon = "bi-piggy-bank",
                Description = $"{l.LoanAmount:N0} ج.م",
                RequestDate = l.CreatedAt,
                Status = l.Status,
                StatusClass = LoanStatusClass(l.Status)
            }));

            var transfers = await db.DepartmentTransferRequests
                .Include(t => t.FromDepartment).Include(t => t.ToDepartment)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.RequestDate)
                .Take(50)
                .ToListAsync();
            items.AddRange(transfers.Select(t => new MyRequestItem
            {
                Type = "نقل",
                Icon = "bi-arrow-left-right",
                Description = $"{t.FromDepartment?.Name} ← {t.ToDepartment?.Name}",
                RequestDate = t.RequestDate,
                Status = TransferStatusLabel(t.Status),
                StatusClass = TransferStatusClass(t.Status)
            }));

            return items.OrderByDescending(i => i.RequestDate).ToList();
        }

        string LeaveStatusLabel(int status) => status switch { 0 => "معلّقة", 1 => "معتمدة", 2 => "مرفوضة", _ => "غير معروف" };
        string LeaveStatusClass(int status) => status switch { 0 => "status-pending", 1 => "status-checkin", 2 => "status-checkout", _ => "" };

        string GenericStatusLabel(string? status) => status switch
        {
            "Approved" => "معتمد",
            "Rejected" => "مرفوض",
            "Pending" => "معلّق",
            _ => status ?? "غير معروف"
        };
        string GenericStatusClass(string? status) => status switch
        {
            "Approved" => "status-checkin",
            "Rejected" => "status-checkout",
            _ => "status-pending"
        };

        string LoanStatusClass(string status) => status switch
        {
            "Approved" or "Active" => "status-checkin",
            "Rejected" => "status-checkout",
            _ => "status-pending"
        };

        string TransferStatusLabel(TransferRequestStatus s) => s switch
        {
            TransferRequestStatus.PendingDirectManager => "قيد المراجعه من المدير المباشر",
            TransferRequestStatus.PendingSecondApprover => "قيد المراجعه من الموافق الثاني",
            TransferRequestStatus.Approved => "معتمد",
            TransferRequestStatus.RejectedByDirectManager or TransferRequestStatus.RejectedBySecondApprover => "مرفوض",
            _ => ""
        };
        string TransferStatusClass(TransferRequestStatus s) => s switch
        {
            TransferRequestStatus.Approved => "status-checkin",
            TransferRequestStatus.RejectedByDirectManager or TransferRequestStatus.RejectedBySecondApprover => "status-checkout",
            _ => "status-pending"
        };
    }
}