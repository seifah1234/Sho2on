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
    public class HolidayRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HolidayRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/HolidayRequests/SearchEmployees
        [HttpGet("SearchEmployees")]
        public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> SearchEmployees(
            [FromQuery] string searchTerm,
            [FromQuery] int? departmentId,
            [FromQuery] int? jobTitleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Branch)
                    .Include(u => u.WeekHoliday)
                    .AsQueryable();

                // تطبيق عوامل التصفية
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u =>
                        u.FullName.Contains(searchTerm) ||
                        u.Id.ToString().Contains(searchTerm) ||
                        (u.Email != null && u.Email.Contains(searchTerm)));
                }

                if (departmentId.HasValue)
                {
                    query = query.Where(u => u.DepartmentId == departmentId.Value);
                }

                if (jobTitleId.HasValue)
                {
                    query = query.Where(u => u.JobTitleId == jobTitleId.Value);
                }

                // التصفح
                var totalRecords = await query.CountAsync();
                var employees = await query
                    .OrderBy(u => u.FullName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new EmployeeDto
                    {
                        Id = u.Id,
                        Code = u.Id.ToString(),
                        FullName = u.FullName,
                        DepartmentName = u.Department != null ? u.Department.Name : "غير محدد",
                        JobTitleName = u.JobTitle != null ? u.JobTitle.Name : "غير محدد",
                        BranchName = u.Branch != null ? u.Branch.Name : "غير محدد",
                        HireDate = u.HireDate,
                        WeekendDays = GetWeekendDays(u.WeekHoliday),
                        HasManagerRole = u.JobTitle != null &&
                                        u.JobTitle.IsManager.HasValue &&
                                        u.JobTitle.IsManager.Value
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<EmployeeDto>>
                {
                    Success = true,
                    Message = "تم تحميل الموظفين بنجاح",
                    Data = employees
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<EmployeeDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء البحث عن الموظفين",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetEmployee/{id}
        [HttpGet("GetEmployee/{id}")]
        public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Branch)
                    .Include(u => u.WeekHoliday)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<EmployeeDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                var employeeDto = new EmployeeDto
                {
                    Id = employee.Id,
                    Code = employee.Id.ToString(),
                    FullName = employee.FullName,
                    DepartmentName = employee.Department?.Name ?? "غير محدد",
                    JobTitleName = employee.JobTitle?.Name ?? "غير محدد",
                    BranchName = employee.Branch?.Name ?? "غير محدد",
                    HireDate = employee.HireDate,
                    WeekendDays = GetWeekendDays(employee.WeekHoliday),
                    HasManagerRole = employee.JobTitle?.IsManager.HasValue == true &&
                                     employee.JobTitle.IsManager.Value
                };

                return Ok(new ApiResponse<EmployeeDto>
                {
                    Success = true,
                    Message = "تم تحميل بيانات الموظف بنجاح",
                    Data = employeeDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<EmployeeDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل بيانات الموظف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetLeaveTypes
        [HttpGet("GetLeaveTypes")]
        public async Task<ActionResult<ApiResponse<List<LeaveTypeDto>>>> GetLeaveTypes()
        {
            try
            {
                var leaveTypes = await _context.LeaveTypes
                    .Where(lt => lt.IsActive)
                    .OrderBy(lt => lt.Name)
                    .Select(lt => new LeaveTypeDto
                    {
                        Id = lt.Id,
                        Name = lt.Name,
                        Description = lt.Notes ?? "",
                        MaxConsecutiveDays = lt.MaxConsecutiveDays,
                        RequiresApproval = lt.RequiresApproval,
                        IsActive = lt.IsActive,
                        DeductFromBalance = lt.DeductFromBalance,
                        DefaultBalance = lt.DefaultBalance
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<LeaveTypeDto>>
                {
                    Success = true,
                    Message = "تم تحميل أنواع الإجازات بنجاح",
                    Data = leaveTypes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<LeaveTypeDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل أنواع الإجازات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetLeaveBalance/{employeeId}/{leaveTypeId}
        [HttpGet("GetLeaveBalance/{employeeId}/{leaveTypeId}")]
        public async Task<ActionResult<ApiResponse<LeaveBalanceDto>>> GetLeaveBalance(int employeeId, int leaveTypeId)
        {
            try
            {
                // التحقق من وجود الموظف
                var employeeExists = await _context.Users.AnyAsync(u => u.Id == employeeId);
                if (!employeeExists)
                {
                    return NotFound(new ApiResponse<LeaveBalanceDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                // الحصول على رصيد الإجازة من قاعدة البيانات
                var leaveBalance = await _context.LeaveBalances
                    .Include(lb => lb.LeaveType)
                    .FirstOrDefaultAsync(lb => lb.UserId == employeeId && lb.LeaveTypeId == leaveTypeId);

                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                if (leaveType == null)
                {
                    return NotFound(new ApiResponse<LeaveBalanceDto>
                    {
                        Success = false,
                        Message = "نوع الإجازة غير موجود"
                    });
                }

                int totalBalance = leaveBalance?.TotalBalance ?? leaveType.DefaultBalance;

                // حساب الإجازات المستخدمة (الموافق عليها فقط)
                var usedLeaves = await _context.Leaves
                    .Where(l => l.UserId == employeeId &&
                               l.LeaveTypeId == leaveTypeId &&
                               l.Status == 2 && // الموافق عليها
                               !l.IsCancelled)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                int remainingBalance = totalBalance - usedLeaves;
                double percentageUsed = totalBalance > 0 ? (double)usedLeaves / totalBalance * 100 : 0;

                var balanceDto = new LeaveBalanceDto
                {
                    LeaveTypeId = leaveTypeId,
                    LeaveTypeName = leaveType.Name,
                    TotalBalance = totalBalance,
                    UsedBalance = usedLeaves,
                    RemainingBalance = remainingBalance,
                    PercentageUsed = percentageUsed
                };

                return Ok(new ApiResponse<LeaveBalanceDto>
                {
                    Success = true,
                    Message = "تم تحميل رصيد الإجازة بنجاح",
                    Data = balanceDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LeaveBalanceDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء حساب الرصيد",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetManagers
        [HttpGet("GetManagers")]
        public async Task<ActionResult<ApiResponse<List<ManagerDto>>>> GetManagers(
            [FromQuery] int? jobTitleId = null,
            [FromQuery] int? departmentId = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .Where(u => u.JobTitle != null &&
                               u.JobTitle.IsManager.HasValue &&
                               u.JobTitle.IsManager.Value);

                if (jobTitleId.HasValue && jobTitleId.Value > 0)
                {
                    query = query.Where(u => u.JobTitleId == jobTitleId.Value);
                }

                if (departmentId.HasValue && departmentId.Value > 0)
                {
                    query = query.Where(u => u.DepartmentId == departmentId.Value);
                }

                var managers = await query
                    .OrderBy(u => u.FullName)
                    .Select(u => new ManagerDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        DepartmentName = u.Department != null ? u.Department.Name : "غير محدد",
                        JobTitleName = u.JobTitle != null ? u.JobTitle.Name : "غير محدد",
                        Email = u.Email ?? "",
                        Phone = u.PhoneNumber ?? ""
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ManagerDto>>
                {
                    Success = true,
                    Message = "تم تحميل المديرين بنجاح",
                    Data = managers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل المديرين",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/HolidayRequests/CheckDateConflicts
        [HttpPost("CheckDateConflicts")]
        public async Task<ActionResult<ApiResponse<DateConflictCheckDto>>> CheckDateConflicts(
            [FromBody] DateConflictCheckRequest request)
        {
            try
            {
                var conflicts = await _context.Leaves
                    .Include(l => l.LeaveType)
                    .Where(l => l.UserId == request.EmployeeId &&
                               l.Status == 2 && // الموافق عليها فقط
                               !l.IsCancelled &&
                               ((l.StartDate <= request.EndDate && l.EndDate >= request.StartDate)))
                    .Select(l => new ConflictDto
                    {
                        ConflictStartDate = l.StartDate,
                        ConflictEndDate = l.EndDate,
                        LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "غير محدد",
                        Status = GetStatusName(l.Status)
                    })
                    .ToListAsync();

                var result = new DateConflictCheckDto
                {
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    HasConflicts = conflicts.Any(),
                    Conflicts = conflicts
                };

                return Ok(new ApiResponse<DateConflictCheckDto>
                {
                    Success = true,
                    Message = conflicts.Any() ?
                        "هناك تعارض في التواريخ" : "لا يوجد تعارض في التواريخ",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DateConflictCheckDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء التحقق من التعارض",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/HolidayRequests/SubmitRequest
        [HttpPost("SubmitRequest")]
        public async Task<ActionResult<ApiResponse<HolidayRequestResponseDto>>> SubmitRequest(
            [FromBody] HolidayRequestDto request)
        {
            try
            {
                // التحقق من صحة البيانات
                var validationResult = await ValidateHolidayRequest(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiResponse<HolidayRequestResponseDto>
                    {
                        Success = false,
                        Message = "بيانات الطلب غير صحيحة",
                        Errors = validationResult.Errors
                    });
                }

                // الحصول على نوع الإجازة
                var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
                bool requiresApproval = leaveType?.RequiresApproval ?? true;

                // التحقق من المدير إذا كانت الإجازة تتطلب موافقة
                if (requiresApproval && request.ApprovingManagerId.HasValue)
                {
                    var managerExists = await _context.Users.AnyAsync(u =>
                        u.Id == request.ApprovingManagerId.Value &&
                        u.JobTitle != null &&
                        u.JobTitle.IsManager.HasValue &&
                        u.JobTitle.IsManager.Value);

                    if (!managerExists)
                    {
                        return BadRequest(new ApiResponse<HolidayRequestResponseDto>
                        {
                            Success = false,
                            Message = "المدير المحدد غير صالح للموافقة"
                        });
                    }
                }

                // إنشاء طلب الإجازة
                var leaveRequest = new Leave
                {
                    UserId = request.EmployeeId,
                    LeaveTypeId = request.LeaveTypeId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Duration = request.Duration,
                    Reason = request.Reason,
                    RequestDate = DateTime.Now,
                    Status = request.SaveAsDraft ? 0 : (requiresApproval ? 1 : 2), // 0: Draft, 1: Pending, 2: Approved
                    ApprovedBy = request.ApprovingManagerId,
                };

                _context.Leaves.Add(leaveRequest);
                await _context.SaveChangesAsync();

                // إذا كانت الإجازة لا تتطلب موافقة أو تمت الموافقة تلقائياً
                if (leaveRequest.Status == 2 && leaveType?.DeductFromBalance == true)
                {
                    await DeductLeaveBalance(request.EmployeeId, request.LeaveTypeId, request.Duration);

                    // تحديث سجلات الحضور
                    var employee = await _context.Users
                        .Include(u => u.WeekHoliday)
                        .FirstOrDefaultAsync(u => u.Id == request.EmployeeId);

                    if (employee != null)
                    {
                        await UpdateAttendanceForLeave(leaveRequest, employee);
                    }
                }

                var response = new HolidayRequestResponseDto
                {
                    RequestId = leaveRequest.Id,
                    RequestNumber = $"HR-{leaveRequest.Id:000000}",
                    RequestDate = leaveRequest.RequestDate,
                    Status = GetStatusName(leaveRequest.Status),
                    StatusCode = leaveRequest.Status.ToString(),
                    Message = leaveRequest.Status == 2 ?
                        "تم تقديم طلب الإجازة بنجاح وتحديث سجلات الحضور" :
                        "تم تقديم طلب الإجازة بنجاح وهو قيد انتظار الموافقة",
                    ApprovalManagerId = request.ApprovingManagerId,
                    ApprovalManagerName = request.ApprovingManagerId.HasValue ?
                        await GetManagerName(request.ApprovingManagerId.Value) : null
                };

                return Ok(new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = true,
                    Message = "تم تقديم طلب الإجازة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تقديم الطلب",
                    Errors = new List<string> { ex.InnerException?.Message ?? ex.Message }
                });
            }
        }

        // في HolidayRequestsController.cs - إضافة الدوال التالية:

        // GET: api/HolidayRequests/GetPendingRequestsForManager/{managerId}
        [HttpGet("GetPendingRequestsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerHolidayDto>>>> GetPendingRequestsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerHolidaysByStatus(managerId, 1, searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerHolidayDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetApprovedRequestsForManager/{managerId}
        [HttpGet("GetApprovedRequestsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerHolidayDto>>>> GetApprovedRequestsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerHolidaysByStatus(managerId, 2, searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerHolidayDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetRejectedRequestsForManager/{managerId}
        [HttpGet("GetRejectedRequestsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerHolidayDto>>>> GetRejectedRequestsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerHolidaysByStatus(managerId, 3, searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerHolidayDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private async Task<ActionResult<ApiResponse<List<ManagerHolidayDto>>>> GetManagerHolidaysByStatus(
            int managerId,
            int status,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize)
        {
            // التحقق من أن المستخدم مدير
            var manager = await _context.Users
                .Include(u => u.JobTitle)
                .FirstOrDefaultAsync(u => u.Id == managerId);

            if (manager == null)
            {
                return NotFound(new ApiResponse<List<ManagerHolidayDto>>
                {
                    Success = false,
                    Message = "المستخدم غير موجود"
                });
            }

            if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
            {
                return BadRequest(new ApiResponse<List<ManagerHolidayDto>>
                {
                    Success = false,
                    Message = "المستخدم ليس لديه صلاحيات مدير"
                });
            }

            // الحصول على طلبات الإجازة التي تحتاج موافقة هذا المدير
            var query = _context.Leaves
                .Include(l => l.User)
                    .ThenInclude(u => u.Department)
                .Include(l => l.User)
                    .ThenInclude(u => u.JobTitle)
                .Include(l => l.LeaveType)
                .Where(l => l.ApprovedBy == managerId && l.Status == status);

            // تطبيق عوامل التصفية
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(l =>
                    l.User.FullName.Contains(searchTerm) ||
                    l.Reason.Contains(searchTerm) ||
                    (l.Id.ToString()).Contains(searchTerm));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.StartDate <= toDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var leaves = await query
                .OrderByDescending(l => l.RequestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ManagerHolidayDto
                {
                    Id = l.Id,
                    RequestNumber = $"{l.Id}",
                    EmployeeId = l.UserId,
                    EmployeeName = l.User.FullName,
                    EmployeeCode = l.User.Id.ToString(),
                    DepartmentName = l.User.Department != null ? l.User.Department.Name : "غير محدد",
                    JobTitleName = l.User.JobTitle != null ? l.User.JobTitle.Name : "غير محدد",
                    LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "غير محدد",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Duration = l.Duration,
                    Reason = l.Reason ?? "",
                    Status = GetStatusName(l.Status),
                    StatusCode = l.Status.ToString(),
                    RequestDate = l.RequestDate,
                    IsCancelled = l.IsCancelled
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ManagerHolidayDto>>
            {
                Success = true,
                Message = $"تم تحميل طلبات الإجازة ({GetStatusName(status)}) بنجاح",
                Data = leaves,
                TotalRecords = totalRecords
            });
        }

        // POST: api/HolidayRequests/ApproveHoliday/{requestId}
        [HttpPost("ApproveHoliday/{requestId}")]
        public async Task<ActionResult<ApiResponse<HolidayRequestResponseDto>>> ApproveHoliday(int requestId)
        {
            try
            {
                var leaveRequest = await _context.Leaves
                    .Include(l => l.User)
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == requestId);

                if (leaveRequest == null)
                {
                    return NotFound(new ApiResponse<HolidayRequestResponseDto>
                    {
                        Success = false,
                        Message = "طلب الإجازة غير موجود"
                    });
                }

                if (leaveRequest.Status != 1) // ليس قيد الانتظار
                {
                    return BadRequest(new ApiResponse<HolidayRequestResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن الموافقة على طلب إجازة بحالة: {GetStatusName(leaveRequest.Status)}"
                    });
                }

                // تحديث حالة طلب الإجازة
                leaveRequest.Status = 2; // موافق
                leaveRequest.ApprovalDate = DateTime.Now;

                // خصم من رصيد الإجازة إذا كان النوع يخصم من الرصيد
                if (leaveRequest.LeaveType?.DeductFromBalance == true)
                {
                    await DeductLeaveBalance(leaveRequest.UserId, leaveRequest.LeaveTypeId, leaveRequest.Duration);
                }

                // تحديث سجلات الحضور
                var employee = await _context.Users
                    .Include(u => u.WeekHoliday)
                    .FirstOrDefaultAsync(u => u.Id == leaveRequest.UserId);

                if (employee != null)
                {
                    await UpdateAttendanceForLeave(leaveRequest, employee);
                }

                await _context.SaveChangesAsync();

                var response = new HolidayRequestResponseDto
                {
                    RequestId = leaveRequest.Id,
                    RequestNumber = $"{leaveRequest.Id}",
                    RequestDate = leaveRequest.RequestDate,
                    Status = GetStatusName(leaveRequest.Status),
                    StatusCode = leaveRequest.Status.ToString(),
                    Message = $"{leaveRequest.Id}"
                };

                return Ok(new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = true,
                    Message = "تمت الموافقة على طلب الإجازة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء الموافقة على طلب الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/HolidayRequests/RejectHoliday/{requestId}
        [HttpPost("RejectHoliday/{requestId}")]
        public async Task<ActionResult<ApiResponse<HolidayRequestResponseDto>>> RejectHoliday(
            int requestId,
            [FromBody] RejectHolidayRequest request)
        {
            try
            {
                var leaveRequest = await _context.Leaves
                    .FirstOrDefaultAsync(l => l.Id == requestId);

                if (leaveRequest == null)
                {
                    return NotFound(new ApiResponse<HolidayRequestResponseDto>
                    {
                        Success = false,
                        Message = "طلب الإجازة غير موجود"
                    });
                }

                if (leaveRequest.Status != 1) // ليس قيد الانتظار
                {
                    return BadRequest(new ApiResponse<HolidayRequestResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن رفض طلب إجازة بحالة: {GetStatusName(leaveRequest.Status)}"
                    });
                }

                // تحديث حالة طلب الإجازة
                leaveRequest.Status = 3; // مرفوض
                leaveRequest.ApprovalDate = DateTime.Now;

                // إضافة ملاحظة الرفض
                if (!string.IsNullOrEmpty(request.Reason))
                {
                    leaveRequest.Reason = string.IsNullOrEmpty(leaveRequest.Reason) ?
                        $"سبب الرفض: {request.Reason}" :
                        $"{leaveRequest.Reason}\nسبب الرفض: {request.Reason}";
                }

                await _context.SaveChangesAsync();

                var response = new HolidayRequestResponseDto
                {
                    RequestId = leaveRequest.Id,
                    RequestNumber = $"{leaveRequest.Id}",
                    RequestDate = leaveRequest.RequestDate,
                    Status = GetStatusName(leaveRequest.Status),
                    StatusCode = leaveRequest.Status.ToString(),
                    Message = $"{leaveRequest.Id}"
                };

                return Ok(new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = true,
                    Message = "تم رفض طلب الإجازة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<HolidayRequestResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء رفض طلب الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/HolidayRequests/GetManagerHolidayStats/{managerId}
        [HttpGet("GetManagerHolidayStats/{managerId}")]
        public async Task<ActionResult<ApiResponse<ManagerHolidayStatsDto>>> GetManagerHolidayStats(
            int managerId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.Leaves
                    .Where(l => l.ApprovedBy == managerId);

                if (fromDate.HasValue)
                {
                    query = query.Where(l => l.StartDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(l => l.StartDate <= toDate.Value);
                }

                var leaves = await query.ToListAsync();

                var stats = new ManagerHolidayStatsDto
                {
                    TotalPending = leaves.Count(l => l.Status == 1),
                    TotalApproved = leaves.Count(l => l.Status == 2),
                    TotalRejected = leaves.Count(l => l.Status == 3),
                    TotalDaysPending = leaves.Where(l => l.Status == 1).Sum(l => l.Duration),
                    TotalDaysApproved = leaves.Where(l => l.Status == 2).Sum(l => l.Duration),
                    TotalDaysRejected = leaves.Where(l => l.Status == 3).Sum(l => l.Duration)
                };

                return Ok(new ApiResponse<ManagerHolidayStatsDto>
                {
                    Success = true,
                    Message = "تم تحميل إحصائيات الإجازات بنجاح",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ManagerHolidayStatsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل الإحصائيات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // إضافة DTOs الجديدة في نهاية الملف:
        public class ManagerHolidayDto
        {
            public int Id { get; set; }
            public string RequestNumber { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string DepartmentName { get; set; }
            public string JobTitleName { get; set; }
            public string LeaveTypeName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Duration { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public string StatusCode { get; set; }
            public DateTime RequestDate { get; set; }
            public bool IsCancelled { get; set; }
        }

        public class ManagerHolidayStatsDto
        {
            public int TotalPending { get; set; }
            public int TotalApproved { get; set; }
            public int TotalRejected { get; set; }
            public int TotalDaysPending { get; set; }
            public int TotalDaysApproved { get; set; }
            public int TotalDaysRejected { get; set; }
        }

        public class RejectHolidayRequest
        {
            public string Reason { get; set; }
        }

        // GET: api/HolidayRequests/GetEmployeeRequests/{employeeId}
        [HttpGet("GetEmployeeRequests/{employeeId}")]
        public async Task<ActionResult<ApiResponse<List<LeaveRequestHistoryDto>>>> GetEmployeeRequests(
            int employeeId,
            [FromQuery] int? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Leaves
                    .Include(l => l.LeaveType)
                    .Include(l => l.Approver)
                    .Where(l => l.UserId == employeeId);

                // تطبيق عوامل التصفية
                if (status.HasValue)
                {
                    query = query.Where(l => l.Status == status.Value);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(l => l.RequestDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(l => l.RequestDate <= toDate.Value);
                }

                var totalRecords = await query.CountAsync();
                var requests = await query
                    .OrderByDescending(l => l.RequestDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new LeaveRequestHistoryDto
                    {
                        Id = l.Id,
                        RequestNumber = $"{l.Id}",
                        LeaveTypeName = l.LeaveType != null ? l.LeaveType.Name : "غير محدد",
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        Duration = l.Duration,
                        Reason = l.Reason ?? "",
                        RequestDate = l.RequestDate,
                        Status = GetStatusName(l.Status),
                        StatusCode = l.Status.ToString(),
                        IsCancelled = l.IsCancelled,
                        ApprovedByName = l.Approver != null ? l.Approver.FullName : null,
                        ApprovedDate = l.ApprovalDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<LeaveRequestHistoryDto>>
                {
                    Success = true,
                    Message = "تم تحميل طلبات الإجازة بنجاح",
                    Data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<LeaveRequestHistoryDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإجازة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #region Helper Methods

        private List<string> GetWeekendDays(WeekHoliday weekHoliday)
        {
            if (weekHoliday == null) return new List<string>();

            var days = new List<string>();
            var dayNames = new[] { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };

            bool[] dayValues = {
                weekHoliday.Day1, weekHoliday.Day2, weekHoliday.Day3,
                weekHoliday.Day4, weekHoliday.Day5, weekHoliday.Day6, weekHoliday.Day7
            };

            for (int i = 0; i < dayValues.Length; i++)
            {
                if (dayValues[i])
                {
                    days.Add(dayNames[i]);
                }
            }

            return days;
        }

        private static string GetStatusName(int statusCode)
        {
            return statusCode switch
            {
                0 => "مسودة",
                1 => "قيد الانتظار",
                2 => "موافق",
                3 => "مرفوض",
                _ => "غير معروف"
            };
        }

        private async Task<ValidationResult> ValidateHolidayRequest(HolidayRequestDto request)
        {
            var result = new ValidationResult();

            if (request.EmployeeId <= 0)
            {
                result.Errors.Add("رقم الموظف غير صالح");
            }

            if (request.LeaveTypeId <= 0)
            {
                result.Errors.Add("نوع الإجازة غير صالح");
            }

            if (request.StartDate < DateTime.Today.Date)
            {
                result.Errors.Add("لا يمكن تقديم إجازة بتاريخ قديم");
            }

            if (request.EndDate < request.StartDate)
            {
                result.Errors.Add("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
            }

            if (request.Duration <= 0)
            {
                result.Errors.Add("مدة الإجازة يجب أن تكون أكبر من صفر");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                result.Errors.Add("سبب الإجازة مطلوب");
            }

            // التحقق من وجود الموظف
            var employeeExists = await _context.Users.AnyAsync(u => u.Id == request.EmployeeId);
            if (!employeeExists)
            {
                result.Errors.Add("الموظف غير موجود");
            }

            // التحقق من وجود نوع الإجازة
            var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
            if (leaveType == null || !leaveType.IsActive)
            {
                result.Errors.Add("نوع الإجازة غير متاح");
            }

            // التحقق من الرصيد إذا كان النوع يخصم من الرصيد
            if (leaveType?.DeductFromBalance == true)
            {
                var balance = await GetLeaveBalanceHelper(request.EmployeeId, request.LeaveTypeId);
                if (request.Duration > balance.RemainingBalance)
                {
                    result.Errors.Add($"الرصيد المتبقي غير كافي. المتبقي: {balance.RemainingBalance} يوم، المطلوب: {request.Duration} يوم");
                }
            }

            // التحقق من الحد الأقصى للأيام المتتالية
            if (leaveType?.MaxConsecutiveDays.HasValue == true &&
                request.Duration > leaveType.MaxConsecutiveDays.Value)
            {
                result.Errors.Add($"الحد الأقصى للإجازة من هذا النوع هو {leaveType.MaxConsecutiveDays.Value} يوم متتالية");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        private async Task<LeaveBalanceDto> GetLeaveBalanceHelper(int employeeId, int leaveTypeId)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == employeeId && lb.LeaveTypeId == leaveTypeId);

            var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);

            int totalBalance = leaveBalance?.TotalBalance ?? leaveType?.DefaultBalance ?? 0;

            var usedLeaves = await _context.Leaves
                .Where(l => l.UserId == employeeId &&
                           l.LeaveTypeId == leaveTypeId &&
                           l.Status == 2 &&
                           !l.IsCancelled)
                .SumAsync(l => (int?)l.Duration) ?? 0;

            return new LeaveBalanceDto
            {
                TotalBalance = totalBalance,
                UsedBalance = usedLeaves,
                RemainingBalance = totalBalance - usedLeaves
            };
        }

        private async Task DeductLeaveBalance(int userId, int leaveTypeId, int days)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.UserId == userId && lb.LeaveTypeId == leaveTypeId);

            if (leaveBalance == null)
            {
                var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
                leaveBalance = new LeaveBalance
                {
                    UserId = userId,
                    LeaveTypeId = leaveTypeId,
                    TotalBalance = leaveType?.DefaultBalance ?? 0,
                    UsedBalance = days,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.LeaveBalances.Add(leaveBalance);
            }
            else
            {
                leaveBalance.UsedBalance += days;
                leaveBalance.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateAttendanceForLeave(Leave leaveRequest, User employee)
        {
            DateTime currentDate = leaveRequest.StartDate;

            while (currentDate <= leaveRequest.EndDate)
            {
                // تخطي أيام العطلات الأسبوعية
                if (employee.WeekHoliday != null && IsWeekend(currentDate, employee.WeekHoliday))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                var attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == leaveRequest.UserId &&
                                             a.AttendanceDate == currentDate);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        UserId = leaveRequest.UserId,
                        AttendanceDate = currentDate,
                        CheckInTime = null,
                        CheckOutTime = null,
                        IsHoliday = true,
                        IsAbsence = false,
                        LeaveId = leaveRequest.Id,
                        ShiftId = employee.ShiftId,
                    };
                    _context.Attendances.Add(attendance);
                }
                else
                {
                    attendance.IsHoliday = true;
                    attendance.IsAbsence = false;
                    attendance.LeaveId = leaveRequest.Id;
                    attendance.CheckInTime = null;
                    attendance.CheckOutTime = null;
                    attendance.Late = null;
                    attendance.EarlyLeave = null;
                    attendance.Overtime = null;
                    attendance.TotalWorkHours = null;
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }

        private bool IsWeekend(DateTime date, WeekHoliday weekHoliday)
        {
            DayOfWeek dayOfWeek = date.DayOfWeek;

            return dayOfWeek switch
            {
                DayOfWeek.Saturday => weekHoliday.Day1,
                DayOfWeek.Sunday => weekHoliday.Day2,
                DayOfWeek.Monday => weekHoliday.Day3,
                DayOfWeek.Tuesday => weekHoliday.Day4,
                DayOfWeek.Wednesday => weekHoliday.Day5,
                DayOfWeek.Thursday => weekHoliday.Day6,
                DayOfWeek.Friday => weekHoliday.Day7,
                _ => false
            };
        }

        private async Task<string> GetManagerName(int managerId)
        {
            var manager = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == managerId);

            return manager?.FullName ?? "غير محدد";
        }

        #endregion
    }

    // Additional Models for API
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class DateConflictCheckRequest
    {
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class LeaveRequestHistoryDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration { get; set; }
        public string Reason { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string StatusCode { get; set; }
        public bool IsCancelled { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}