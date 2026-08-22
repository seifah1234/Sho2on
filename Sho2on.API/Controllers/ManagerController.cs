using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.API.Data;
using Sho2on.API.Dtos;
using Sho2on.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ManagerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Manager/GetTeamStats/{managerId}
        [HttpGet("GetTeamStats/{managerId}")]
        public async Task<ActionResult<ApiResponse<ManagerTeamStatsDto>>> GetTeamStats(
            int managerId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;

                // التحقق من أن المستخدم مدير
                var manager = await _context.Users
                    .Include(u => u.JobTitle)
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return NotFound(new ApiResponse<ManagerTeamStatsDto>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
                {
                    return BadRequest(new ApiResponse<ManagerTeamStatsDto>
                    {
                        Success = false,
                        Message = "المستخدم ليس لديه صلاحيات مدير"
                    });
                }

                // الحصول على الموظفين تحت إشراف المدير
                var teamEmployees = await _context.Users
                    .Where(u => u.ManagerId == managerId)
                    .ToListAsync();

                // الحصول على حالات الحضور اليوم
                var todayAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == targetDate.Date &&
                                teamEmployees.Select(t => t.Id).Contains(a.UserId))
                    .ToListAsync();

                // الحصول على الإجازات اليوم
                var todayLeaves = await _context.Leaves
                    .Include(l => l.User)
                    .Where(l => l.StartDate.Date <= targetDate.Date &&
                                l.EndDate.Date >= targetDate.Date &&
                                l.Status == 2 && // الموافق عليها
                                !l.IsCancelled &&
                                teamEmployees.Select(t => t.Id).Contains(l.UserId))
                    .ToListAsync();

                // الحصول على طلبات السلف المعلقة
                var pendingLoans = await _context.Loans
                    .CountAsync(l => l.ApprovedByUserId == managerId && l.Status == "Pending");

                // الحصول على طلبات الإجازة المعلقة
                var pendingLeaves = await _context.Leaves
                    .CountAsync(l => l.ApprovedBy == managerId && l.Status == 1); // قيد الانتظار

                // الحصول على طلبات الإذن المعلقة
                var pendingPermissions = await _context.EmployeePermissions
                    .CountAsync(p => p.ApprovedByUserId == managerId && p.Status == "Pending");

                // حساب عدد الموظفين الذين عملوا CheckIn
                var checkedInToday = todayAttendance
                    .Where(a => a.CheckInTime != null)
                    .Select(a => a.UserId)
                    .Distinct()
                    .Count();

                // حساب المتأخرين
                var lateToday = todayAttendance
                    .Where(a => a.Late.HasValue && a.Late.Value.TotalMinutes > 0)
                    .Select(a => a.UserId)
                    .Distinct()
                    .Count();

                // حساب الغياب
                var absentToday = teamEmployees.Count -
                                 (checkedInToday + todayLeaves.Count);

                if (absentToday < 0) absentToday = 0;

                var stats = new ManagerTeamStatsDto
                {
                    TotalEmployees = teamEmployees.Count,
                    PresentToday = checkedInToday,
                    OnLeaveToday = todayLeaves.Count,
                    LateToday = lateToday,
                    AbsentToday = absentToday,
                    PendingLoanApprovals = pendingLoans,
                    PendingLeaveApprovals = pendingLeaves,
                    PendingPermissionApprovals = pendingPermissions,
                    TotalPendingApprovals = pendingLoans + pendingLeaves + pendingPermissions
                };

                return Ok(new ApiResponse<ManagerTeamStatsDto>
                {
                    Success = true,
                    Message = "تم تحميل إحصائيات الفريق بنجاح",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ManagerTeamStatsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل الإحصائيات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Manager/GetTeamMembers/{managerId}
        [HttpGet("GetTeamMembers/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<TeamMemberDto>>>> GetTeamMembers(
            int managerId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;

                // التحقق من أن المستخدم مدير
                var manager = await _context.Users
                    .Include(u => u.JobTitle)
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return NotFound(new ApiResponse<List<TeamMemberDto>>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
                {
                    return BadRequest(new ApiResponse<List<TeamMemberDto>>
                    {
                        Success = false,
                        Message = "المستخدم ليس لديه صلاحيات مدير"
                    });
                }

                // الحصول على الموظفين تحت إشراف المدير
                var teamMembers = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Shift)
                    .Where(u => u.ManagerId == managerId)
                    .OrderBy(u => u.FullName)
                    .Select(u => new TeamMemberDto
                    {
                        Id = u.Id,
                        Code = u.Code,
                        FullName = u.FullName,
                        DepartmentName = u.Department != null ? u.Department.Name : "غير محدد",
                        JobTitleName = u.JobTitle != null ? u.JobTitle.Name : "غير محدد",
                        ShiftName = u.Shift != null ? u.Shift.Name : "غير محدد",
                        Email = u.Email ?? "",
                        Phone = u.PhoneNumber ?? "",
                        ProfileImage = u.ProfileImageData != null ? Convert.ToBase64String(u.ProfileImageData) : null
                    })
                    .ToListAsync();

                // الحصول على حالات الحضور اليوم لكل موظف
                var todayAttendance = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == targetDate.Date &&
                                teamMembers.Select(t => t.Id).Contains(a.UserId))
                    .ToListAsync();

                // الحصول على الإجازات اليوم
                var todayLeaves = await _context.Leaves
                    .Where(l => l.StartDate.Date <= targetDate.Date &&
                                l.EndDate.Date >= targetDate.Date &&
                                l.Status == 2 &&
                                !l.IsCancelled &&
                                teamMembers.Select(t => t.Id).Contains(l.UserId))
                    .ToListAsync();

                // تحديث حالة كل موظف
                foreach (var member in teamMembers)
                {
                    var attendance = todayAttendance.FirstOrDefault(a => a.UserId == member.Id);
                    var isOnLeave = todayLeaves.Any(l => l.UserId == member.Id);

                    if (isOnLeave)
                    {
                        member.Status = "إجازة";
                        member.StatusColor = "#FF9800"; // لون برتقالي
                        member.StatusIcon = "beach_access";
                    }
                    else if (attendance != null)
                    {
                        if (attendance.CheckInTime.HasValue)
                        {
                            if (attendance.Late.HasValue && attendance.Late.Value.TotalMinutes > 0)
                            {
                                member.Status = "متأخر";
                                member.StatusColor = "#F44336"; // لون أحمر
                                member.StatusIcon = "schedule";
                                member.LateMinutes = (int)attendance.Late.Value.TotalMinutes;
                            }
                            else
                            {
                                member.Status = "حاضر";
                                member.StatusColor = "#4CAF50"; // لون أخضر
                                member.StatusIcon = "check_circle";
                            }

                            member.CheckInTime = attendance.CheckInTime.Value;
                            if (attendance.CheckOutTime.HasValue)
                            {
                                member.CheckOutTime = attendance.CheckOutTime.Value;
                            }
                        }
                        else
                        {
                            member.Status = "لم يحضر";
                            member.StatusColor = "#9E9E9E"; // لون رمادي
                            member.StatusIcon = "help";
                        }
                    }
                    else
                    {
                        member.Status = "لم يحضر";
                        member.StatusColor = "#9E9E9E";
                        member.StatusIcon = "help";
                    }
                }

                return Ok(new ApiResponse<List<TeamMemberDto>>
                {
                    Success = true,
                    Message = "تم تحميل أعضاء الفريق بنجاح",
                    Data = teamMembers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<TeamMemberDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل أعضاء الفريق",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Manager/GetTodayCheckIns/{managerId}
        [HttpGet("GetTodayCheckIns/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<TodayCheckInDto>>>> GetTodayCheckIns(int managerId)
        {
            try
            {
                var today = DateTime.Today;

                // التحقق من أن المستخدم مدير
                var manager = await _context.Users
                    .Include(u => u.JobTitle)
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return NotFound(new ApiResponse<List<TodayCheckInDto>>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
                {
                    return BadRequest(new ApiResponse<List<TodayCheckInDto>>
                    {
                        Success = false,
                        Message = "المستخدم ليس لديه صلاحيات مدير"
                    });
                }

                // الحصول على الموظفين تحت إشراف المدير الذين عملوا CheckIn اليوم
                var todayCheckIns = await _context.Attendances
                    .Include(a => a.User)
                        .ThenInclude(u => u.Department)
                    .Include(a => a.User)
                        .ThenInclude(u => u.JobTitle)
                    .Where(a => a.AttendanceDate.Date == today &&
                                a.User.ManagerId == managerId &&
                                a.CheckInTime.HasValue)
                    .OrderByDescending(a => a.CheckInTime)
                    .Select(a => new TodayCheckInDto
                    {
                        EmployeeId = a.UserId,
                        EmployeeName = a.User.FullName ?? "",
                        EmployeeCode = a.User.Code,
                        DepartmentName = a.User.Department != null ? a.User.Department.Name : "غير محدد",
                        JobTitleName = a.User.JobTitle != null ? a.User.JobTitle.Name : "غير محدد",
                        CheckInTime = a.CheckInTime,
                        CheckOutTime = a.CheckOutTime,
                        LateMinutes = a.Late.HasValue ? (int)a.Late.Value.TotalMinutes : 0,
                        EarlyLeaveMinutes = a.EarlyLeave.HasValue ? (int)a.EarlyLeave.Value.TotalMinutes : 0,
                        TotalWorkHours = a.TotalWorkHours.HasValue ? ((int)a.TotalWorkHours.Value.TotalHours) : 0,
                        Status = a.Late.HasValue && a.Late.Value.TotalMinutes > 0 ? "متأخر" : "حاضر"
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<TodayCheckInDto>>
                {
                    Success = true,
                    Message = "تم تحميل بيانات الحضور اليوم بنجاح",
                    Data = todayCheckIns
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<TodayCheckInDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل بيانات الحضور",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Manager/GetPendingApprovals/{managerId}
        [HttpGet("GetPendingApprovals/{managerId}")]
        public async Task<ActionResult<ApiResponse<PendingApprovalsDto>>> GetPendingApprovals(int managerId)
        {
            try
            {
                // التحقق من أن المستخدم مدير
                var manager = await _context.Users
                    .Include(u => u.JobTitle)
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return NotFound(new ApiResponse<PendingApprovalsDto>
                    {
                        Success = false,
                        Message = "المستخدم غير موجود"
                    });
                }

                if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
                {
                    return BadRequest(new ApiResponse<PendingApprovalsDto>
                    {
                        Success = false,
                        Message = "المستخدم ليس لديه صلاحيات مدير"
                    });
                }

                // الحصول على طلبات السلف المعلقة
                var pendingLoans = await _context.Loans
                    .Include(l => l.User)
                    .Where(l => l.ApprovedByUserId == managerId && l.Status == "Pending")
                    .Select(l => new PendingApprovalItemDto
                    {
                        Id = l.Id,
                        Type = "سلفة",
                        EmployeeName = l.User.FullName,
                        Amount = l.LoanAmount,
                        RequestDate = l.CreatedAt,
                        Details = $"سلفة بقيمة {l.LoanAmount:N2} جنيه"
                    })
                    .ToListAsync();

                // الحصول على طلبات الإجازة المعلقة
                var pendingLeaves = await _context.Leaves
                    .Include(l => l.User)
                    .Include(l => l.LeaveType)
                    .Where(l => l.ApprovedBy == managerId && l.Status == 1) // قيد الانتظار
                    .Select(l => new PendingApprovalItemDto
                    {
                        Id = l.Id,
                        Type = "إجازة",
                        EmployeeName = l.User.FullName,
                        Amount = 0,
                        RequestDate = l.RequestDate,
                        Details = $"{l.LeaveType.Name} لمدة {l.Duration} يوم"
                    })
                    .ToListAsync();

                // الحصول على طلبات الإذن المعلقة
                var pendingPermissions = await _context.EmployeePermissions
                    .Include(p => p.User)
                    .Where(p => p.ApprovedByUserId == managerId && p.Status == "Pending")
                    .Select(p => new PendingApprovalItemDto
                    {
                        Id = p.Id,
                        Type = "إذن",
                        EmployeeName = p.User.FullName,
                        Amount = p.DeductedAmount ?? 0,
                        RequestDate = p.CreatedAt,
                        Details = $"{p.PermissionType} لمدة {p.Duration:N1} ساعة"
                    })
                    .ToListAsync();

                var allPending = pendingLoans
                    .Concat(pendingLeaves)
                    .Concat(pendingPermissions)
                    .OrderByDescending(p => p.RequestDate)
                    .ToList();

                var result = new PendingApprovalsDto
                {
                    TotalPending = allPending.Count,
                    PendingLoans = pendingLoans.Count,
                    PendingLeaves = pendingLeaves.Count,
                    PendingPermissions = pendingPermissions.Count,
                    Items = allPending
                };

                return Ok(new ApiResponse<PendingApprovalsDto>
                {
                    Success = true,
                    Message = "تم تحميل الطلبات المعلقة بنجاح",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PendingApprovalsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل الطلبات المعلقة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

    // DTO Classes
    public class ManagerTeamStatsDto
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int OnLeaveToday { get; set; }
        public int LateToday { get; set; }
        public int AbsentToday { get; set; }
        public int PendingLoanApprovals { get; set; }
        public int PendingLeaveApprovals { get; set; }
        public int PendingPermissionApprovals { get; set; }
        public int TotalPendingApprovals { get; set; }
    }

    public class TeamMemberDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
        public string JobTitleName { get; set; }
        public string ShiftName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string ProfileImage { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string StatusIcon { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? LateMinutes { get; set; }
    }

    public class TodayCheckInDto
    {
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobTitleName { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyLeaveMinutes { get; set; }
        public double? TotalWorkHours { get; set; }
        public string? Status { get; set; }
    }

    public class PendingApprovalsDto
    {
        public int TotalPending { get; set; }
        public int PendingLoans { get; set; }
        public int PendingLeaves { get; set; }
        public int PendingPermissions { get; set; }
        public List<PendingApprovalItemDto> Items { get; set; }
    }

    public class PendingApprovalItemDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string EmployeeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestDate { get; set; }
        public string Details { get; set; }
    }
}