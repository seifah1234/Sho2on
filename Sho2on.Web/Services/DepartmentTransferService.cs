using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class TransferRequestItem
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string FromDepartmentName { get; set; } = "";
        public string ToDepartmentName { get; set; } = "";
        public string DirectManagerName { get; set; } = "";
        public string SecondApproverName { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime RequestDate { get; set; }
        public TransferRequestStatus Status { get; set; }
    }

    public class DepartmentTransferService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly NotificationCenterService _notificationService;

        public DepartmentTransferService(IDbContextFactory<AppDbContext> dbFactory, NotificationCenterService notificationService)
        {
            _dbFactory = dbFactory;
            _notificationService = notificationService;
        }

        // كل المديرين الفعليين في النظام (أي حد ظاهر كـ ManagerId لموظف آخر)
        public async Task<List<(int Id, string Name)>> GetManagersListAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var managerIds = await db.Users.Where(u => u.ManagerId != null).Select(u => u.ManagerId!.Value).Distinct().ToListAsync();
            return await db.Users.Where(u => managerIds.Contains(u.Id) && !u.IsArchived)
                .Select(u => new ValueTuple<int, string>(u.Id, u.FullName)).ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateRequestAsync(int userId, int toDepartmentId, int secondApproverId, string? reason, int firstApproverId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var employee = await db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
            if (employee == null) return (false, "الموظف غير موجود");
            if (firstApproverId == 0) return (false, "لا يوجد مدير مباشر مسجّل لهذا الموظف، لا يمكن تقديم الطلب");
            if (employee.DepartmentId == toDepartmentId) return (false, "الموظف بالفعل في هذه الإدارة");

            var request = new DepartmentTransferRequest
            {
                UserId = userId,
                FromDepartmentId = employee.DepartmentId,
                ToDepartmentId = toDepartmentId,
                DirectManagerId = firstApproverId,
                SecondApproverId = secondApproverId,
                Reason = reason,
                Status = TransferRequestStatus.PendingDirectManager
            };
            db.DepartmentTransferRequests.Add(request);
            await db.SaveChangesAsync();

            var toDept = await db.Departments.FindAsync(toDepartmentId);

            await _notificationService.CreateAsync(
                firstApproverId,
                "طلب نقل قسم جديد",
                $"{employee.FullName} قدّم طلب نقل إلى إدارة {toDept?.Name} ويحتاج موافقتك",
                "bi-arrow-left-right",
                "/employee/transfer-requests/management"
                );

            return (true, "تم تقديم طلب النقل بنجاح");
        }

        public async Task<List<TransferRequestItem>> GetMyRequestsAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.DepartmentTransferRequests
                .Include(r => r.FromDepartment).Include(r => r.ToDepartment)
                .Include(r => r.DirectManager).Include(r => r.SecondApprover)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => MapToItem(r))
                .ToListAsync();
        }

        // الطلبات اللي محتاجة موافقة المستخدم الحالي (سواء كمدير مباشر أو كموافق تاني)
        public async Task<List<TransferRequestItem>> GetPendingForApproverAsync(int approverUserId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.DepartmentTransferRequests
                .Include(r => r.User).Include(r => r.FromDepartment).Include(r => r.ToDepartment)
                .Include(r => r.DirectManager).Include(r => r.SecondApprover)
                .Where(r =>
                    (r.Status == TransferRequestStatus.PendingDirectManager && r.DirectManagerId == approverUserId) ||
                    (r.Status == TransferRequestStatus.PendingSecondApprover && r.SecondApproverId == approverUserId))
                .OrderBy(r => r.RequestDate)
                .Select(r => MapToItem(r))
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> DirectManagerActionAsync(int requestId, int approverId, bool approve, string? note)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var req = await db.DepartmentTransferRequests.Include(r => r.User).Include(r => r.ToDepartment)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return (false, "الطلب غير موجود");
            if (req.DirectManagerId != approverId) return (false, "غير مصرح لك بالتعامل مع هذا الطلب");
            if (req.Status != TransferRequestStatus.PendingDirectManager) return (false, "تم التعامل مع هذا الطلب بالفعل");

            req.DirectManagerActionDate = DateTime.Now;
            req.DirectManagerNote = note;

            if (approve)
            {
                req.Status = TransferRequestStatus.PendingSecondApprover;
                await db.SaveChangesAsync();

                await _notificationService.CreateAsync(req.SecondApproverId,
                    "طلب نقل يحتاج موافقتك",
                    $"طلب نقل {req.User.FullName} إلى إدارة {req.ToDepartment.Name} تمت الموافقة عليه من المدير المباشر، وينتظر موافقتك",
                    "bi-arrow-left-right",
                    "/employee/transfer-requests/management");

                return (true, "تمت الموافقة، تم إرسال الطلب للموافق التالي");
            }
            else
            {
                req.Status = TransferRequestStatus.RejectedByDirectManager;
                await db.SaveChangesAsync();

                await _notificationService.CreateAsync(req.UserId,
                    "تم رفض طلب النقل",
                    $"رفض مديرك المباشر طلب نقلك إلى إدارة {req.ToDepartment.Name}",
                    "bi-x-circle",
                    "/employee/transfer-requests/management");  // أو أي صفحة عندك لعرض طلبات الموظف

                return (true, "تم رفض الطلب");
            }
        }

        public async Task<(bool Success, string Message)> SecondApproverActionAsync(int requestId, int approverId, bool approve, string? note)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var req = await db.DepartmentTransferRequests.Include(r => r.User).Include(r => r.ToDepartment)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return (false, "الطلب غير موجود");
            if (req.SecondApproverId != approverId) return (false, "غير مصرح لك بالتعامل مع هذا الطلب");
            if (req.Status != TransferRequestStatus.PendingSecondApprover) return (false, "تم التعامل مع هذا الطلب بالفعل أو لسه مستني المدير المباشر");

            req.SecondApproverActionDate = DateTime.Now;
            req.SecondApproverNote = note;

            if (approve)
            {
                req.Status = TransferRequestStatus.Approved;
                req.EffectiveDate = DateTime.Now;

                // تنفيذ النقل فعليًا
                var user = await db.Users.FindAsync(req.UserId);
                if (user != null) user.DepartmentId = req.ToDepartmentId;

                await db.SaveChangesAsync();

                await _notificationService.CreateAsync(req.UserId,
                    "تمت الموافقة على طلب النقل",
                    $"تم نقلك بنجاح إلى إدارة {req.ToDepartment.Name}",
                    "bi-check-circle",
                    "/employees");

                return (true, "تمت الموافقة النهائية، تم نقل الموظف");
            }
            else
            {
                req.Status = TransferRequestStatus.RejectedBySecondApprover;
                await db.SaveChangesAsync();

                await _notificationService.CreateAsync(req.UserId,
                    "تم رفض طلب النقل",
                    $"رفض الموافق الثاني طلب نقلك إلى إدارة {req.ToDepartment.Name}",
                    "bi-x-circle",
                    "/leaves");

                return (true, "تم رفض الطلب");
            }
        }

        private static TransferRequestItem MapToItem(DepartmentTransferRequest r) => new()
        {
            Id = r.Id,
            EmployeeName = r.User?.FullName ?? "",
            FromDepartmentName = r.FromDepartment?.Name ?? "",
            ToDepartmentName = r.ToDepartment?.Name ?? "",
            DirectManagerName = r.DirectManager?.FullName ?? "",
            SecondApproverName = r.SecondApprover?.FullName ?? "",
            Reason = r.Reason,
            RequestDate = r.RequestDate,
            Status = r.Status
        };
    }
}