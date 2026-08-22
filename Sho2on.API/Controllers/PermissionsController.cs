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
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Permissions/GetPermissionTypes
        [HttpGet("GetPermissionTypes")]
        public async Task<ActionResult<ApiResponse<List<PermissionTypeDto>>>> GetPermissionTypes()
        {
            try
            {
                var permissionTypes = new List<PermissionTypeDto>
                {
                    new PermissionTypeDto { Id = 1, Name = "مأمورية", Code = "ER", DeductFromSalary = false },
                    new PermissionTypeDto { Id = 2, Name = "إذن", Code = "PR", DeductFromSalary = true },
                    new PermissionTypeDto { Id = 3, Name = "إذن طبي", Code = "MD", DeductFromSalary = false },
                    new PermissionTypeDto { Id = 4, Name = "إذن عائلي", Code = "FM", DeductFromSalary = false },
                    new PermissionTypeDto { Id = 5, Name = "إذن طارئ", Code = "EM", DeductFromSalary = false },
                };

                return Ok(new ApiResponse<List<PermissionTypeDto>>
                {
                    Success = true,
                    Message = "تم تحميل أنواع الإذن بنجاح",
                    Data = permissionTypes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<PermissionTypeDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل أنواع الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Permissions/CheckTimeConflict
        [HttpGet("CheckTimeConflict")]
        public async Task<ActionResult<ApiResponse<TimeConflictDto>>> CheckTimeConflict(
            [FromQuery] int employeeId,
            [FromQuery] DateTime startDateTime,
            [FromQuery] DateTime endDateTime)
        {
            try
            {
                // التحقق من تعارض الوقت مع الإجازات
                var leaveConflicts = await _context.Leaves
                    .Where(l => l.UserId == employeeId &&
                               l.Status == 2 && // الموافق عليها
                               !l.IsCancelled &&
                               ((l.StartDate <= endDateTime && l.EndDate >= startDateTime)))
                    .Select(l => new TimeConflictItemDto
                    {
                        Type = "إجازة",
                        StartDateTime = l.StartDate,
                        EndDateTime = l.EndDate,
                        Reason = l.Reason
                    })
                    .ToListAsync();

                // التحقق من تعارض الوقت مع المأموريات/الإذن السابقة
                var permissionConflicts = await _context.EmployeePermissions
                    .Where(p => p.UserId == employeeId &&
                               p.Status == "Approved" &&
                               ((p.StartDateTime <= endDateTime && p.EndDateTime >= startDateTime)))
                    .Select(p => new TimeConflictItemDto
                    {
                        Type = p.PermissionType,
                        StartDateTime = p.StartDateTime,
                        EndDateTime = p.EndDateTime,
                        Reason = p.Reason
                    })
                    .ToListAsync();

                var allConflicts = leaveConflicts.Concat(permissionConflicts).ToList();

                var result = new TimeConflictDto
                {
                    StartDateTime = startDateTime,
                    EndDateTime = endDateTime,
                    HasConflicts = allConflicts.Any(),
                    Conflicts = allConflicts,
                    TotalConflicts = allConflicts.Count
                };

                return Ok(new ApiResponse<TimeConflictDto>
                {
                    Success = true,
                    Message = allConflicts.Any() ?
                        "هناك تعارض في المواعيد" : "لا يوجد تعارض في المواعيد",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<TimeConflictDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء التحقق من التعارض",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/Permissions/CalculateDeduction
        [HttpPost("CalculateDeduction")]
        public async Task<ActionResult<ApiResponse<DeductionCalculationDto>>> CalculateDeduction(
            [FromBody] DeductionCalculationRequest request)
        {
            try
            {
                var employee = await _context.Users
                    .Include(u => u.Salaries)
                    .FirstOrDefaultAsync(u => u.Id == request.EmployeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<DeductionCalculationDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                // حساب عدد الساعات
                TimeSpan duration = request.EndDateTime - request.StartDateTime;
                double totalHours = duration.TotalHours;

                // الحصول على الراتب اليومي
                var basicSalary = employee.Salaries.FirstOrDefault(s => s.Type == 1);
                decimal dailySalary = 0;

                if (basicSalary != null)
                {
                    // نفترض أن الشهر 30 يوم
                    dailySalary = basicSalary.Amount / 30;
                }

                // حساب المبلغ المقتطع (إذا كان نوع الإذن يقتطع من الراتب)
                decimal deductedAmount = 0;
                if (request.DeductFromSalary)
                {
                    // حساب بالساعة: الراتب اليومي / عدد ساعات العمل اليومية
                    decimal hourlyRate = dailySalary / ((decimal)employee.WorkHours.TotalMinutes / 60); // نفترض 8 ساعات عمل يومية
                    deductedAmount = (decimal)totalHours * hourlyRate;
                }

                var calculation = new DeductionCalculationDto
                {
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    TotalHours = totalHours,
                    DailySalary = dailySalary,
                    HourlyRate = dailySalary / 8,
                    DeductedAmount = deductedAmount,
                    DeductFromSalary = request.DeductFromSalary
                };

                return Ok(new ApiResponse<DeductionCalculationDto>
                {
                    Success = true,
                    Message = "تم حساب الخصم بنجاح",
                    Data = calculation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DeductionCalculationDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء الحساب",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Permissions/GetManagersForApproval
        [HttpGet("GetManagersForApproval")]
        public async Task<ActionResult<ApiResponse<List<ManagerDto>>>> GetManagersForApproval()
        {
            try
            {
                var managers = await _context.Users
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .Where(u => u.JobTitle != null &&
                               u.JobTitle.IsManager.HasValue &&
                               u.JobTitle.IsManager.Value)
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

        // POST: api/Permissions/SubmitRequest
        [HttpPost("SubmitRequest")]
        public async Task<ActionResult<ApiResponse<PermissionResponseDto>>> SubmitRequest(
            [FromBody] PermissionRequestDto request)
        {
            try
            {
                // التحقق من صحة البيانات
                var validationResult = await ValidatePermissionRequest(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = "بيانات الطلب غير صحيحة",
                        Errors = validationResult.Errors
                    });
                }

                var employee = await _context.Users
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == request.EmployeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                // حساب المدة
                TimeSpan duration = request.EndDateTime - request.StartDateTime;
                double totalHours = duration.TotalHours;

                // حساب المبلغ المقتطع إذا كان النوع يقتطع من الراتب
                decimal? deductedAmount = null;
                if (request.PermissionTypeId == 2) // إذن عادي
                {
                    var basicSalary = await _context.Salaries
                        .FirstOrDefaultAsync(s => s.UserId == request.EmployeeId && s.Type == 1);

                    if (basicSalary != null)
                    {
                        decimal dailySalary = basicSalary.Amount / 30;
                        decimal hourlyRate = dailySalary / 8;
                        deductedAmount = (decimal)totalHours * hourlyRate;
                    }
                }

                // إنشاء طلب الإذن
                var permission = new EmployeePermission
                {
                    UserId = request.EmployeeId,
                    PermissionType = GetPermissionTypeName(request.PermissionTypeId),
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    Duration = totalHours,
                    Reason = request.Reason,
                    Notes = request.Notes,
                    DeductedAmount = deductedAmount,
                    Status = "Pending",
                    ApprovedByUserId = request.ApprovingManagerId,
                    BranchId = employee.BranchId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _context.EmployeePermissions.AddAsync(permission);
                await _context.SaveChangesAsync();

                // إذا كان الإذن يقتطع من الراتب، إنشاء سجل في الراتب
                if (deductedAmount.HasValue && deductedAmount.Value > 0)
                {
                    var salaryOperation = new Salary
                    {
                        UserId = request.EmployeeId,
                        Notes = $"خصم إذن: {request.Reason}",
                        DayDate = request.StartDateTime.Date,
                        EditedAt = DateTime.Now,
                        Type = 17, // نوع خاص للإذن
                        Amount = deductedAmount.Value,
                        Operation = 1 // خصم
                    };
                    await _context.Salaries.AddAsync(salaryOperation);
                    await _context.SaveChangesAsync();
                }

                // تحديث سجلات الحضور إذا كان الوقت في نفس اليوم
                if (request.StartDateTime.Date == DateTime.Today)
                {
                    await UpdateAttendanceForPermission(permission, employee);
                }

                var response = new PermissionResponseDto
                {
                    PermissionId = permission.Id,
                    PermissionNumber = $"{permission.Id}",
                    StartDateTime = permission.StartDateTime,
                    EndDateTime = permission.EndDateTime,
                    Duration = permission.Duration,
                    Status = permission.Status,
                    Message = $"تم تقديم طلب الإذن بنجاح. رقم الطلب: {permission.Id}",  
                    DeductedAmount = deductedAmount
                };

                return Ok(new ApiResponse<PermissionResponseDto>
                {
                    Success = true,
                    Message = "تم تقديم طلب الإذن بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PermissionResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تقديم الطلب",
                    Errors = new List<string> { ex.InnerException?.Message ?? ex.Message }
                });
            }
        }

        // GET: api/Permissions/GetEmployeePermissions/{employeeId}
        [HttpGet("GetEmployeePermissions/{employeeId}")]
        public async Task<ActionResult<ApiResponse<List<PermissionHistoryDto>>>> GetEmployeePermissions(
            int employeeId,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? permissionType = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.EmployeePermissions
                    .Include(p => p.ApprovedByUser)
                    .Include(p => p.Branch)
                    .Where(p => p.UserId == employeeId);

                // تطبيق عوامل التصفية
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime <= toDate.Value);
                }

                if (!string.IsNullOrEmpty(permissionType))
                {
                    query = query.Where(p => p.PermissionType == permissionType);
                }

                var totalRecords = await query.CountAsync();
                var permissions = await query
                    .OrderByDescending(p => p.StartDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PermissionHistoryDto
                    {
                        Id = p.Id,
                        PermissionNumber = $"No. {p.Id}",
                        PermissionType = p.PermissionType,
                        StartDateTime = p.StartDateTime,
                        EndDateTime = p.EndDateTime,
                        Duration = p.Duration,
                        Reason = p.Reason ?? "",
                        Notes = p.Notes ?? "",
                        Status = p.Status,
                        StatusText = GetPermissionStatusText(p.Status),
                        DeductedAmount = p.DeductedAmount,
                        ApprovedByName = p.ApprovedByUser != null ? p.ApprovedByUser.FullName : null,
                        ApprovedDate = p.ApprovedDate,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<PermissionHistoryDto>>
                {
                    Success = true,
                    Message = "تم تحميل سجل الإذن بنجاح",
                    Data = permissions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<PermissionHistoryDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل سجل الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // في PermissionsController.cs - إضافة الدوال التالية:

        // GET: api/Permissions/GetPendingPermissionsForManager/{managerId}
        [HttpGet("GetPendingPermissionsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerPermissionDto>>>> GetPendingPermissionsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerPermissionsByStatus(managerId, "Pending", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerPermissionDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Permissions/GetApprovedPermissionsForManager/{managerId}
        [HttpGet("GetApprovedPermissionsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerPermissionDto>>>> GetApprovedPermissionsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerPermissionsByStatus(managerId, "Approved", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerPermissionDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Permissions/GetRejectedPermissionsForManager/{managerId}
        [HttpGet("GetRejectedPermissionsForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerPermissionDto>>>> GetRejectedPermissionsForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerPermissionsByStatus(managerId, "Rejected", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerPermissionDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private async Task<ActionResult<ApiResponse<List<ManagerPermissionDto>>>> GetManagerPermissionsByStatus(
            int managerId,
            string status,
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
                return NotFound(new ApiResponse<List<ManagerPermissionDto>>
                {
                    Success = false,
                    Message = "المستخدم غير موجود"
                });
            }

            if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
            {
                return BadRequest(new ApiResponse<List<ManagerPermissionDto>>
                {
                    Success = false,
                    Message = "المستخدم ليس لديه صلاحيات مدير"
                });
            }

            // الحصول على طلبات الإذن التي تحتاج موافقة هذا المدير
            var query = _context.EmployeePermissions
                .Include(p => p.User)
                    .ThenInclude(u => u.Department)
                .Include(p => p.User)
                    .ThenInclude(u => u.JobTitle)
                .Where(p => p.ApprovedByUserId == managerId && p.Status == status);

            // تطبيق عوامل التصفية
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p =>
                    p.User.FullName.Contains(searchTerm) ||
                    p.Reason.Contains(searchTerm) ||
                    (p.Id.ToString()).Contains(searchTerm));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.StartDateTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.StartDateTime <= toDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var permissions = await query
                .OrderByDescending(p => p.StartDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ManagerPermissionDto
                {
                    Id = p.Id,
                    PermissionNumber = $"{p.Id}",
                    EmployeeId = p.UserId,
                    EmployeeName = p.User.FullName,
                    EmployeeCode = p.User.Code,
                    DepartmentName = p.User.Department != null ? p.User.Department.Name : "غير محدد",
                    JobTitleName = p.User.JobTitle != null ? p.User.JobTitle.Name : "غير محدد",
                    PermissionType = p.PermissionType,
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Duration = p.Duration,
                    Reason = p.Reason ?? "",
                    Status = p.Status,
                    DeductedAmount = p.DeductedAmount,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ManagerPermissionDto>>
            {
                Success = true,
                Message = $"تم تحميل طلبات الإذن ({status}) بنجاح",
                Data = permissions,
                TotalRecords = totalRecords
            });
        }

        // POST: api/Permissions/ApprovePermission/{permissionId}
        [HttpPost("ApprovePermission/{permissionId}")]
        public async Task<ActionResult<ApiResponse<PermissionResponseDto>>> ApprovePermission(int permissionId)
        {
            try
            {
                var permission = await _context.EmployeePermissions
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == permissionId);

                if (permission == null)
                {
                    return NotFound(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = "طلب الإذن غير موجود"
                    });
                }

                if (permission.Status != "Pending")
                {
                    return BadRequest(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن الموافقة على إذن بحالة: {permission.Status}"
                    });
                }

                // تحديث حالة الإذن
                permission.Status = "Approved";
                permission.ApprovedDate = DateTime.Now;
                permission.UpdatedAt = DateTime.Now;

                // إذا كان الإذن يقتطع من الراتب، تنفيذ الخصم
                if (permission.DeductedAmount.HasValue && permission.DeductedAmount.Value > 0)
                {
                    var salaryOperation = new Salary
                    {
                        UserId = permission.UserId,
                        Notes = $"خصم إذن: {permission.Reason}",
                        DayDate = permission.StartDateTime.Date,
                        EditedAt = DateTime.Now,
                        Type = 17, // نوع خاص للإذن
                        Amount = permission.DeductedAmount.Value,
                        Operation = 1 // خصم
                    };
                    await _context.Salaries.AddAsync(salaryOperation);
                }

                await _context.SaveChangesAsync();

                var response = new PermissionResponseDto
                {
                    PermissionId = permission.Id,
                    PermissionNumber = $"PR-{permission.Id:000000}",
                    StartDateTime = permission.StartDateTime,
                    EndDateTime = permission.EndDateTime,
                    Duration = permission.Duration,
                    Status = permission.Status,
                    Message = $"{permission.Id}"
                };

                return Ok(new ApiResponse<PermissionResponseDto>
                {
                    Success = true,
                    Message = "تمت الموافقة على الإذن بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PermissionResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء الموافقة على الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/Permissions/RejectPermission/{permissionId}
        [HttpPost("RejectPermission/{permissionId}")]
        public async Task<ActionResult<ApiResponse<PermissionResponseDto>>> RejectPermission(
            int permissionId,
            [FromBody] RejectPermissionRequest request)
        {
            try
            {
                var permission = await _context.EmployeePermissions
                    .FirstOrDefaultAsync(p => p.Id == permissionId);

                if (permission == null)
                {
                    return NotFound(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = "طلب الإذن غير موجود"
                    });
                }

                if (permission.Status != "Pending")
                {
                    return BadRequest(new ApiResponse<PermissionResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن رفض إذن بحالة: {permission.Status}"
                    });
                }

                // تحديث حالة الإذن
                permission.Status = "Rejected";
                permission.ApprovedDate = DateTime.Now;
                permission.Notes = string.IsNullOrEmpty(permission.Notes) ?
                    $"سبب الرفض: {request.Reason}" :
                    $"{permission.Notes}\nسبب الرفض: {request.Reason}";
                permission.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var response = new PermissionResponseDto
                {
                    PermissionId = permission.Id,
                    PermissionNumber = $"{permission.Id}",
                    StartDateTime = permission.StartDateTime,
                    EndDateTime = permission.EndDateTime,
                    Duration = permission.Duration,
                    Status = permission.Status,
                    Message = $"{permission.Id}"
                };

                return Ok(new ApiResponse<PermissionResponseDto>
                {
                    Success = true,
                    Message = "تم رفض الإذن بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PermissionResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء رفض الإذن",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Permissions/GetManagerPermissionStats/{managerId}
        [HttpGet("GetManagerPermissionStats/{managerId}")]
        public async Task<ActionResult<ApiResponse<ManagerPermissionStatsDto>>> GetManagerPermissionStats(
            int managerId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.EmployeePermissions
                    .Where(p => p.ApprovedByUserId == managerId);

                if (fromDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(p => p.StartDateTime <= toDate.Value);
                }

                var permissions = await query.ToListAsync();

                var stats = new ManagerPermissionStatsDto
                {
                    TotalPending = permissions.Count(p => p.Status == "Pending"),
                    TotalApproved = permissions.Count(p => p.Status == "Approved"),
                    TotalRejected = permissions.Count(p => p.Status == "Rejected"),
                    TotalHoursPending = permissions.Where(p => p.Status == "Pending").Sum(p => p.Duration),
                    TotalHoursApproved = permissions.Where(p => p.Status == "Approved").Sum(p => p.Duration),
                    TotalHoursRejected = permissions.Where(p => p.Status == "Rejected").Sum(p => p.Duration),
                    TotalAmountDeducted = permissions.Where(p => p.DeductedAmount.HasValue).Sum(p => p.DeductedAmount.Value)
                };

                return Ok(new ApiResponse<ManagerPermissionStatsDto>
                {
                    Success = true,
                    Message = "تم تحميل إحصائيات الإذن بنجاح",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ManagerPermissionStatsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل الإحصائيات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // إضافة DTOs الجديدة في نهاية الملف:
        public class ManagerPermissionDto
        {
            public int Id { get; set; }
            public string PermissionNumber { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string DepartmentName { get; set; }
            public string JobTitleName { get; set; }
            public string PermissionType { get; set; }
            public DateTime StartDateTime { get; set; }
            public DateTime EndDateTime { get; set; }
            public double Duration { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public decimal? DeductedAmount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class ManagerPermissionStatsDto
        {
            public int TotalPending { get; set; }
            public int TotalApproved { get; set; }
            public int TotalRejected { get; set; }
            public double TotalHoursPending { get; set; }
            public double TotalHoursApproved { get; set; }
            public double TotalHoursRejected { get; set; }
            public decimal TotalAmountDeducted { get; set; }
        }

        public class RejectPermissionRequest
        {
            public string Reason { get; set; }
        }

        // GET: api/Permissions/GetPermissionDetails/{permissionId}
        [HttpGet("GetPermissionDetails/{permissionId}")]
        public async Task<ActionResult<ApiResponse<PermissionDetailsDto>>> GetPermissionDetails(int permissionId)
        {
            try
            {
                var permission = await _context.EmployeePermissions
                    .Include(p => p.User)
                    .Include(p => p.ApprovedByUser)
                    .Include(p => p.Branch)
                    .FirstOrDefaultAsync(p => p.Id == permissionId);

                if (permission == null)
                {
                    return NotFound(new ApiResponse<PermissionDetailsDto>
                    {
                        Success = false,
                        Message = "الإذن غير موجود"
                    });
                }

                var details = new PermissionDetailsDto
                {
                    Id = permission.Id,
                    PermissionNumber = $"{permission.Id}",
                    EmployeeName = permission.User?.FullName ?? "",
                    PermissionType = permission.PermissionType,
                    StartDateTime = permission.StartDateTime,
                    EndDateTime = permission.EndDateTime,
                    Duration = permission.Duration,
                    Reason = permission.Reason ?? "",
                    Notes = permission.Notes ?? "",
                    Status = permission.Status,
                    StatusText = GetPermissionStatusText(permission.Status),
                    DeductedAmount = permission.DeductedAmount,
                    ApprovedByName = permission.ApprovedByUser?.FullName,
                    ApprovedDate = permission.ApprovedDate,
                    BranchName = permission.Branch?.Name ?? "",
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt
                };

                return Ok(new ApiResponse<PermissionDetailsDto>
                {
                    Success = true,
                    Message = "تم تحميل تفاصيل الإذن بنجاح",
                    Data = details
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PermissionDetailsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل التفاصيل",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #region Helper Methods

        private async Task<ValidationResult> ValidatePermissionRequest(PermissionRequestDto request)
        {
            var result = new ValidationResult();

            if (request.EmployeeId <= 0)
            {
                result.Errors.Add("رقم الموظف غير صالح");
            }

            if (request.PermissionTypeId <= 0)
            {
                result.Errors.Add("نوع الإذن غير صالح");
            }

            if (request.StartDateTime < DateTime.Now)
            {
                result.Errors.Add("لا يمكن تقديم إذن بتاريخ قديم");
            }

            if (request.EndDateTime <= request.StartDateTime)
            {
                result.Errors.Add("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                result.Errors.Add("سبب الإذن مطلوب");
            }

            if (request.ApprovingManagerId < 0)
            {
                result.Errors.Add("الرجاء اختيار مدير للموافقة");
            }

            // التحقق من وجود الموظف
            var employeeExists = await _context.Users.AnyAsync(u => u.Id == request.EmployeeId);
            if (!employeeExists)
            {
                result.Errors.Add("الموظف غير موجود");
            }

            // التحقق من تعارض الوقت
            var timeConflict = await CheckTimeConflictHelper(request.EmployeeId, request.StartDateTime, request.EndDateTime);
            if (timeConflict.HasConflicts)
            {
                result.Errors.Add($"هناك تعارض في المواعيد مع {timeConflict.TotalConflicts} من الإجازات/الإذن السابقة");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        private async Task<TimeConflictDto> CheckTimeConflictHelper(int employeeId, DateTime startDateTime, DateTime endDateTime)
        {
            var leaveConflicts = await _context.Leaves
                .Where(l => l.UserId == employeeId &&
                           l.Status == 2 &&
                           !l.IsCancelled &&
                           ((l.StartDate <= endDateTime && l.EndDate >= startDateTime)))
                .Select(l => new TimeConflictItemDto
                {
                    Type = "إجازة",
                    StartDateTime = l.StartDate,
                    EndDateTime = l.EndDate,
                    Reason = l.Reason
                })
                .ToListAsync();

            var permissionConflicts = await _context.EmployeePermissions
                .Where(p => p.UserId == employeeId &&
                           p.Status == "Approved" &&
                           ((p.StartDateTime <= endDateTime && p.EndDateTime >= startDateTime)))
                .Select(p => new TimeConflictItemDto
                {
                    Type = p.PermissionType,
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Reason = p.Reason
                })
                .ToListAsync();

            var allConflicts = leaveConflicts.Concat(permissionConflicts).ToList();

            return new TimeConflictDto
            {
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                HasConflicts = allConflicts.Any(),
                Conflicts = allConflicts,
                TotalConflicts = allConflicts.Count
            };
        }

        private string GetPermissionTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "مأمورية",
                2 => "إذن",
                3 => "إذن طبي",
                4 => "إذن عائلي",
                5 => "إذن طارئ",
                _ => "إذن"
            };
        }

        private static string GetPermissionStatusText(string status)
        {
            return status switch
            {
                "Pending" => "قيد الانتظار",
                "Approved" => "موافق",
                "Rejected" => "مرفوض",
                _ => "غير معروف"
            };
        }

        private async Task UpdateAttendanceForPermission(EmployeePermission permission, User employee)
        {
            // هذا مثال بسيط، يمكن تطويره حسب نظام الحضور الخاص بك
            var attendanceDate = permission.StartDateTime.Date;

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.UserId == permission.UserId &&
                                        a.AttendanceDate == attendanceDate);

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    UserId = permission.UserId,
                    AttendanceDate = attendanceDate,
                    PermissionId = permission.Id,
                };
                _context.Attendances.Add(attendance);
            }
            else
            {
                attendance.PermissionId = permission.Id;
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }

    // DTO Classes
    public class PermissionRequestDto
    {
        public int EmployeeId { get; set; }
        public int PermissionTypeId { get; set; } // 1: مأمورية, 2: إذن, 3: إذن طبي, 4: إذن عائلي, 5: إذن طارئ
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public int ApprovingManagerId { get; set; }
    }

    public class PermissionResponseDto
    {
        public int PermissionId { get; set; }
        public string PermissionNumber { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public decimal? DeductedAmount { get; set; }
    }

    public class PermissionTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool DeductFromSalary { get; set; }
    }

    public class TimeConflictDto
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public bool HasConflicts { get; set; }
        public List<TimeConflictItemDto> Conflicts { get; set; }
        public int TotalConflicts { get; set; }
    }

    public class TimeConflictItemDto
    {
        public string Type { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Reason { get; set; }
    }

    public class DeductionCalculationRequest
    {
        public int EmployeeId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public bool DeductFromSalary { get; set; }
    }

    public class DeductionCalculationDto
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double TotalHours { get; set; }
        public decimal DailySalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal DeductedAmount { get; set; }
        public bool DeductFromSalary { get; set; }
    }

    public class PermissionHistoryDto
    {
        public int Id { get; set; }
        public string PermissionNumber { get; set; }
        public string PermissionType { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PermissionDetailsDto
    {
        public int Id { get; set; }
        public string PermissionNumber { get; set; }
        public string EmployeeName { get; set; }
        public string PermissionType { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public double Duration { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string BranchName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}