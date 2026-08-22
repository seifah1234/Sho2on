using HR_Application.Services;
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
    public class LoansController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoansController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Loans/SearchEmployees
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
                    .Include(u => u.Salaries)
                    .AsQueryable();

                // تطبيق عوامل التصفية
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u =>
                        u.FullName.Contains(searchTerm) ||
                        u.Code.Contains(searchTerm) ||
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
                        Code = u.Code,
                        FullName = u.FullName,
                        DepartmentName = u.Department != null ? u.Department.Name : "غير محدد",
                        JobTitleName = u.JobTitle != null ? u.JobTitle.Name : "غير محدد",
                        BranchName = u.Branch != null ? u.Branch.Name : "غير محدد",
                        HireDate = u.HireDate,
                        MainSalary = u.Salaries.FirstOrDefault(s => s.Type == 1) != null ?
                                     u.Salaries.FirstOrDefault(s => s.Type == 1).Amount : 0,
                        HasManagerRole = u.JobTitle != null &&
                                        u.JobTitle.IsManager.HasValue &&
                                        u.JobTitle.IsManager.Value,
                        CanTakeLoan = u.CanTakeLoan,
                        CurrentLoanBalance = u.CurrentLoanBalance,
                        LoanMaxAmount = u.LoanMaxAmount ?? 0
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

        // GET: api/Loans/GetEmployee/{id}
        [HttpGet("GetEmployee/{id}")]
        public async Task<ActionResult<ApiResponse<EmployeeLoanDto>>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Branch)
                    .Include(u => u.Salaries)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<EmployeeLoanDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                // حساب الحد الأقصى للسلفة (50% من الراتب)
                var basicSalary = employee.Salaries.FirstOrDefault(s => s.Type == 1);
                decimal maxAllowed = 0;
                if (basicSalary != null)
                {
                    maxAllowed = basicSalary.Amount * 0.5m;
                }

                // السلفة المستحقة
                var currentLoans = await _context.Loans
                    .Where(l => l.UserId == id &&
                               (l.Status == "Approved" || l.Status == "PartiallyPaid"))
                    .SumAsync(l => l.RemainingAmount);

                // رصيد صندوق الزمالة المشترك
                var friendshipBoxService = new FriendshipBoxService(_context);
                var friendshipBoxAmount = await friendshipBoxService.GetCurrentBalanceAsync();

                var employeeDto = new EmployeeLoanDto
                {
                    Id = employee.Id,
                    Code = employee.Code,
                    FullName = employee.FullName,
                    DepartmentName = employee.Department?.Name ?? "غير محدد",
                    JobTitleName = employee.JobTitle?.Name ?? "غير محدد",
                    BranchName = employee.Branch?.Name ?? "غير محدد",
                    HireDate = employee.HireDate.ToDateTime(TimeOnly.MinValue),
                    BasicSalary = basicSalary?.Amount ?? 0,
                    MaxAllowedAmount = maxAllowed,
                    CurrentLoanBalance = currentLoans,
                    FriendshipBoxBalance = friendshipBoxAmount,
                    CanTakeLoan = employee.CanTakeLoan,
                    EmployeeStatus = employee.CanTakeLoan ? "مسموح بالسلفة" : "غير مسموح بالسلفة"
                };

                return Ok(new ApiResponse<EmployeeLoanDto>
                {
                    Success = true,
                    Message = "تم تحميل بيانات الموظف بنجاح",
                    Data = employeeDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<EmployeeLoanDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل بيانات الموظف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Loans/GetManagers
        [HttpGet("GetManagers")]
        public async Task<ActionResult<ApiResponse<List<ManagerDto>>>> GetManagers()
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

        // POST: api/Loans/CalculateInstallment
        [HttpPost("CalculateInstallment")]
        public async Task<ActionResult<ApiResponse<LoanCalculationDto>>> CalculateInstallment(
            [FromBody] LoanCalculationRequest request)
        {
            try
            {
                // التحقق من وجود الموظف
                var employee = await _context.Users
                    .Include(u => u.Salaries)
                    .FirstOrDefaultAsync(u => u.Id == request.EmployeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<LoanCalculationDto>
                    {
                        Success = false,
                        Message = "الموظف غير موجود"
                    });
                }

                // التحقق من صلاحية أخذ سلفة
                if (!employee.CanTakeLoan)
                {
                    return BadRequest(new ApiResponse<LoanCalculationDto>
                    {
                        Success = false,
                        Message = "هذا الموظف غير مسموح له بأخذ سلفة"
                    });
                }

                // حساب الحد الأقصى للسلفة
                var basicSalary = employee.Salaries.FirstOrDefault(s => s.Type == 1);
                decimal maxAllowed = 0;
                if (basicSalary != null)
                {
                    maxAllowed = basicSalary.Amount * 0.5m;
                }

                // التحقق من الحد الأقصى
                if (request.LoanAmount > maxAllowed)
                {
                    return BadRequest(new ApiResponse<LoanCalculationDto>
                    {
                        Success = false,
                        Message = $"مبلغ السلفة يتجاوز الحد المسموح ({maxAllowed:N2})"
                    });
                }

                // حساب القسط الشهري
                decimal monthlyInstallment = request.LoanAmount / request.InstallmentMonths;

                // التحقق من أن القسط الشهري لا يتجاوز 30% من الراتب
                if (basicSalary != null)
                {
                    decimal maxMonthlyInstallment = basicSalary.Amount * 0.3m;
                    if (monthlyInstallment > maxMonthlyInstallment)
                    {
                        return BadRequest(new ApiResponse<LoanCalculationDto>
                        {
                            Success = false,
                            Message = $"القسط الشهري يتجاوز 30% من الراتب. الحد الأقصى للقسط: {maxMonthlyInstallment:N2}"
                        });
                    }
                }

                var calculation = new LoanCalculationDto
                {
                    LoanAmount = request.LoanAmount,
                    InstallmentMonths = request.InstallmentMonths,
                    MonthlyInstallment = monthlyInstallment,
                    MaxAllowedAmount = maxAllowed,
                    CalculationDate = DateTime.Now
                };

                return Ok(new ApiResponse<LoanCalculationDto>
                {
                    Success = true,
                    Message = "تم حساب القسط بنجاح",
                    Data = calculation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LoanCalculationDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء الحساب",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/Loans/SubmitRequest
        [HttpPost("SubmitRequest")]
        public async Task<ActionResult<ApiResponse<LoanResponseDto>>> SubmitRequest(
            [FromBody] LoanRequestDto request)
        {
            try
            {
                // التحقق من صحة البيانات
                var validationResult = await ValidateLoanRequest(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = "بيانات الطلب غير صحيحة",
                        Errors = validationResult.Errors
                    });
                }

                var employee = await _context.Users.FindAsync(request.EmployeeId);
                var managerExists = await _context.Users.AnyAsync(u =>
                    u.Id == request.ApprovingManagerId &&
                    u.JobTitle != null &&
                    u.JobTitle.IsManager.HasValue &&
                    u.JobTitle.IsManager.Value);

                if (!managerExists)
                {
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = "المدير المحدد غير صالح للموافقة"
                    });
                }

                // التحقق من رصيد صندوق الزمالة
                var friendshipBoxService = new FriendshipBoxService(_context);
                if (!await friendshipBoxService.CanWithdrawAsync(request.LoanAmount))
                {
                    var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = $"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {balance:N2}"
                    });
                }

                // إنشاء سجل السلفة
                var loan = new Loan
                {
                    UserId = request.EmployeeId,
                    LoanAmount = request.LoanAmount,
                    RemainingAmount = request.LoanAmount,
                    LoanDate = request.LoanDate,
                    ExpectedPaybackDate = request.ExpectedPaybackDate,
                    InstallmentCount = request.InstallmentMonths,
                    MonthlyInstallment = request.LoanAmount / request.InstallmentMonths,
                    Status = "Pending",
                    Reason = request.Reason,
                    ApprovedByUserId = request.ApprovingManagerId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _context.Loans.AddAsync(loan);
                await _context.SaveChangesAsync();

                // إنشاء الأقساط
                await CreateInstallments(loan);

                var response = new LoanResponseDto
                {
                    LoanId = loan.Id,
                    LoanNumber = $"{loan.Id}",
                    LoanDate = loan.LoanDate,
                    Status = loan.Status,
                    Message = $"تم إرسال طلب السلفة بنجاح للمدير للاعتماد. رقم الطلب: {loan.Id}",
                    ApprovingManagerId = request.ApprovingManagerId
                };

                return Ok(new ApiResponse<LoanResponseDto>
                {
                    Success = true,
                    Message = "تم تقديم طلب السلفة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LoanResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تقديم الطلب",
                    Errors = new List<string> { ex.InnerException?.Message ?? ex.Message }
                });
            }
        }

        // GET: api/Loans/GetEmployeeLoans/{employeeId}
        [HttpGet("GetEmployeeLoans/{employeeId}")]
        public async Task<ActionResult<ApiResponse<List<LoanHistoryDto>>>> GetEmployeeLoans(
            int employeeId,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Loans
                    .Include(l => l.ApprovedByUser)
                    .Where(l => l.UserId == employeeId);

                // تطبيق عوامل التصفية
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(l => l.Status == status);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(l => l.LoanDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(l => l.LoanDate <= toDate.Value);
                }

                var totalRecords = await query.CountAsync();
                var loans = await query
                    .OrderByDescending(l => l.LoanDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new LoanHistoryDto
                    {
                        Id = l.Id,
                        LoanNumber = $"No. {l.Id}",
                        LoanAmount = l.LoanAmount,
                        RemainingAmount = l.RemainingAmount,
                        LoanDate = l.LoanDate,
                        ExpectedPaybackDate = l.ExpectedPaybackDate,
                        InstallmentCount = l.InstallmentCount,
                        MonthlyInstallment = l.MonthlyInstallment,
                        Status = l.Status,
                        StatusText = GetLoanStatusText(l.Status),
                        Reason = l.Reason ?? "",
                        ApprovedByName = l.ApprovedByUser != null ? l.ApprovedByUser.FullName : null,
                        ApprovedDate = l.ApprovedDate
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<LoanHistoryDto>>
                {
                    Success = true,
                    Message = "تم تحميل سجل السلف بنجاح",
                    Data = loans
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<LoanHistoryDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل سجل السلف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Loans/GetPendingLoansForManager/{managerId}
        [HttpGet("GetPendingLoansForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerLoanDto>>>> GetPendingLoansForManager(
    int managerId,
    [FromQuery] string? searchTerm = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerLoansByStatus(managerId, "Pending", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerLoanDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات السلف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Loans/GetApprovedLoansForManager/{managerId}
        [HttpGet("GetApprovedLoansForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerLoanDto>>>> GetApprovedLoansForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerLoansByStatus(managerId, "Approved", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerLoanDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات السلف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Loans/GetRejectedLoansForManager/{managerId}
        [HttpGet("GetRejectedLoansForManager/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<ManagerLoanDto>>>> GetRejectedLoansForManager(
            int managerId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return await GetManagerLoansByStatus(managerId, "Rejected", searchTerm, fromDate, toDate, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ManagerLoanDto>>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل طلبات السلف",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private async Task<ActionResult<ApiResponse<List<ManagerLoanDto>>>> GetManagerLoansByStatus(
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
                return NotFound(new ApiResponse<List<ManagerLoanDto>>
                {
                    Success = false,
                    Message = "المستخدم غير موجود"
                });
            }

            if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
            {
                return BadRequest(new ApiResponse<List<ManagerLoanDto>>
                {
                    Success = false,
                    Message = "المستخدم ليس لديه صلاحيات مدير"
                });
            }

            // الحصول على طلبات السلف التي تحتاج موافقة هذا المدير
            var query = _context.Loans
                .Include(l => l.User)
                    .ThenInclude(u => u.Department)
                .Include(l => l.User)
                    .ThenInclude(u => u.JobTitle)
                .Where(l => l.ApprovedByUserId == managerId && l.Status == status);

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
                query = query.Where(l => l.LoanDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.LoanDate <= toDate.Value);
            }

            var totalRecords = await query.CountAsync();
            var loans = await query
                .OrderByDescending(l => l.LoanDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ManagerLoanDto
                {
                    Id = l.Id,
                    LoanNumber = $"{l.Id}",
                    EmployeeId = l.UserId,
                    EmployeeName = l.User.FullName,
                    EmployeeCode = l.User.Code,
                    DepartmentName = l.User.Department != null ? l.User.Department.Name : "غير محدد",
                    JobTitleName = l.User.JobTitle != null ? l.User.JobTitle.Name : "غير محدد",
                    LoanAmount = l.LoanAmount,
                    RemainingAmount = l.RemainingAmount,
                    LoanDate = l.LoanDate,
                    ExpectedPaybackDate = l.ExpectedPaybackDate,
                    InstallmentMonths = l.InstallmentCount,
                    MonthlyInstallment = l.MonthlyInstallment,
                    Reason = l.Reason ?? "",
                    Status = l.Status,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<ManagerLoanDto>>
            {
                Success = true,
                Message = $"تم تحميل طلبات السلف ({status}) بنجاح",
                Data = loans,
                TotalRecords = totalRecords
            });
        }

        // GET: api/Loans/GetAllManagerLoans/{managerId}
        [HttpGet("GetAllManagerLoans/{managerId}")]
        public async Task<ActionResult<ApiResponse<ManagerLoanStatsDto>>> GetAllManagerLoans(
            int managerId,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.Loans
                    .Include(l => l.User)
                    .Where(l => l.ApprovedByUserId == managerId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(l => l.Status == status);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(l => l.LoanDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(l => l.LoanDate <= toDate.Value);
                }

                var loans = await query.ToListAsync();

                var stats = new ManagerLoanStatsDto
                {
                    TotalPending = loans.Count(l => l.Status == "Pending"),
                    TotalApproved = loans.Count(l => l.Status == "Approved"),
                    TotalRejected = loans.Count(l => l.Status == "Rejected"),
                    TotalAmountPending = loans.Where(l => l.Status == "Pending").Sum(l => l.LoanAmount),
                    TotalAmountApproved = loans.Where(l => l.Status == "Approved").Sum(l => l.LoanAmount),
                    TotalAmountRejected = loans.Where(l => l.Status == "Rejected").Sum(l => l.LoanAmount)
                };

                return Ok(new ApiResponse<ManagerLoanStatsDto>
                {
                    Success = true,
                    Message = "تم تحميل إحصائيات السلف بنجاح",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ManagerLoanStatsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل الإحصائيات",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/Loans/ApproveLoan/{loanId}
        [HttpPost("ApproveLoan/{loanId}")]
        public async Task<ActionResult<ApiResponse<LoanResponseDto>>> ApproveLoan(int loanId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Id == loanId);

                if (loan == null)
                {
                    return NotFound(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = "السلفة غير موجودة"
                    });
                }

                if (loan.Status != "Pending")
                {
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن الموافقة على سلفة بحالة: {loan.Status}"
                    });
                }

                // التحقق من رصيد صندوق الزمالة
                var friendshipBoxService = new FriendshipBoxService(_context);
                if (!await friendshipBoxService.CanWithdrawAsync(loan.LoanAmount))
                {
                    var balance = await friendshipBoxService.GetCurrentBalanceAsync();
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = $"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {balance:N2}"
                    });
                }

                // تحديث حالة السلفة
                loan.Status = "Approved";
                loan.ApprovedDate = DateTime.Now;
                loan.UpdatedAt = DateTime.Now;

                // خصم المبلغ من صندوق الزمالة
                await friendshipBoxService.RecordWithdrawalAsync(loan.UserId, loan.LoanAmount,loanId, $"موافقة على سلفة للموظف {loan.User.FullName} - رقم السلفة: {loan.Id}");

                // تحديث رصيد السلفة للموظف
                loan.User.CurrentLoanBalance += loan.LoanAmount;

                await _context.SaveChangesAsync();

                var response = new LoanResponseDto
                {
                    LoanId = loan.Id,
                    LoanNumber = $"{loan.Id}",
                    LoanDate = loan.LoanDate,
                    Status = loan.Status,
                    Message = $"{loan.Id}"
                };

                return Ok(new ApiResponse<LoanResponseDto>
                {
                    Success = true,
                    Message = "تمت الموافقة على السلفة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LoanResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء الموافقة على السلفة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/Loans/RejectLoan/{loanId}
        [HttpPost("RejectLoan/{loanId}")]
        public async Task<ActionResult<ApiResponse<LoanResponseDto>>> RejectLoan(
            int loanId,
            [FromBody] RejectLoanRequest request)
        {
            try
            {
                var loan = await _context.Loans
                    .FirstOrDefaultAsync(l => l.Id == loanId);

                if (loan == null)
                {
                    return NotFound(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = "السلفة غير موجودة"
                    });
                }

                if (loan.Status != "Pending")
                {
                    return BadRequest(new ApiResponse<LoanResponseDto>
                    {
                        Success = false,
                        Message = $"لا يمكن رفض سلفة بحالة: {loan.Status}"
                    });
                }

                // تحديث حالة السلفة
                loan.Status = "Rejected";
                loan.ApprovedDate = DateTime.Now;
                loan.Notes = $"سبب الرفض: {request.Reason}";
                loan.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var response = new LoanResponseDto
                {
                    LoanId = loan.Id,
                    LoanNumber = $"{loan.Id}",
                    LoanDate = loan.LoanDate,
                    Status = loan.Status,
                    Message = $"{loan.Id}"
                };

                return Ok(new ApiResponse<LoanResponseDto>
                {
                    Success = true,
                    Message = "تم رفض السلفة بنجاح",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LoanResponseDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء رفض السلفة",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/Loans/GetLoanDetails/{loanId}
        [HttpGet("GetLoanDetails/{loanId}")]
        public async Task<ActionResult<ApiResponse<LoanDetailsDto>>> GetLoanDetails(int loanId)
        {
            try
            {
                var loan = await _context.Loans
                    .Include(l => l.User)
                    .Include(l => l.ApprovedByUser)
                    .Include(l => l.LoanPayments)
                    .FirstOrDefaultAsync(l => l.Id == loanId);

                if (loan == null)
                {
                    return NotFound(new ApiResponse<LoanDetailsDto>
                    {
                        Success = false,
                        Message = "السلفة غير موجودة"
                    });
                }

                var payments = loan.LoanPayments?
                    .OrderBy(p => p.PaymentDate)
                    .Select(p => new LoanPaymentDto
                    {
                        Id = p.Id,
                        PaymentAmount = p.PaymentAmount,
                        PaymentDate = p.PaymentDate,
                        PaymentType = p.PaymentType,
                        Notes = p.Notes
                    })
                    .ToList() ?? new List<LoanPaymentDto>();

                var details = new LoanDetailsDto
                {
                    Id = loan.Id,
                    LoanNumber = $"{loan.Id}",
                    EmployeeName = loan.User?.FullName ?? "",
                    LoanAmount = loan.LoanAmount,
                    RemainingAmount = loan.RemainingAmount,
                    AmountPaid = loan.AmountPaid,
                    LoanDate = loan.LoanDate,
                    ExpectedPaybackDate = loan.ExpectedPaybackDate,
                    InstallmentCount = loan.InstallmentCount,
                    MonthlyInstallment = loan.MonthlyInstallment,
                    Status = loan.Status,
                    StatusText = GetLoanStatusText(loan.Status),
                    Reason = loan.Reason ?? "",
                    Notes = loan.Notes ?? "",
                    ApprovedByName = loan.ApprovedByUser?.FullName,
                    ApprovedDate = loan.ApprovedDate,
                    Payments = payments
                };

                return Ok(new ApiResponse<LoanDetailsDto>
                {
                    Success = true,
                    Message = "تم تحميل تفاصيل السلفة بنجاح",
                    Data = details
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<LoanDetailsDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل التفاصيل",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        #region Helper Methods

        private async Task<ValidationResult> ValidateLoanRequest(LoanRequestDto request)
        {
            var result = new ValidationResult();

            if (request.EmployeeId <= 0)
            {
                result.Errors.Add("رقم الموظف غير صالح");
            }

            if (request.LoanAmount <= 0)
            {
                result.Errors.Add("مبلغ السلفة يجب أن يكون أكبر من صفر");
            }

            if (request.InstallmentMonths <= 0)
            {
                result.Errors.Add("عدد الأشهر يجب أن يكون أكبر من صفر");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                result.Errors.Add("سبب السلفة مطلوب");
            }

            if (request.ApprovingManagerId < 0)
            {
                result.Errors.Add("الرجاء اختيار مدير للموافقة");
            }

            // التحقق من وجود الموظف
            var employee = await _context.Users
                .Include(u => u.Salaries)
                .FirstOrDefaultAsync(u => u.Id == request.EmployeeId);

            if (employee == null)
            {
                result.Errors.Add("الموظف غير موجود");
            }
            else if (!employee.CanTakeLoan)
            {
                result.Errors.Add("هذا الموظف غير مسموح له بأخذ سلفة");
            }
            else
            {
                // التحقق من الحد الأقصى
                var basicSalary = employee.Salaries.FirstOrDefault(s => s.Type == 1);
                if (basicSalary != null)
                {
                    decimal maxAllowed = basicSalary.Amount * 0.5m;
                    if (request.LoanAmount > maxAllowed)
                    {
                        result.Errors.Add($"مبلغ السلفة يتجاوز الحد المسموح ({maxAllowed:N2})");
                    }

                    // التحقق من القسط الشهري
                    decimal monthlyInstallment = request.LoanAmount / request.InstallmentMonths;
                    decimal maxMonthlyInstallment = basicSalary.Amount * 0.3m;
                    if (monthlyInstallment > maxMonthlyInstallment)
                    {
                        result.Errors.Add($"القسط الشهري ({monthlyInstallment:N2}) يتجاوز 30% من الراتب ({maxMonthlyInstallment:N2})");
                    }
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        private async Task CreateInstallments(Loan loan)
        {
            var installments = new List<LoanPayment>();
            decimal installmentAmount = loan.MonthlyInstallment;

            for (int i = 1; i <= loan.InstallmentCount; i++)
            {
                var installment = new LoanPayment
                {
                    LoanId = loan.Id,
                    PaymentAmount = installmentAmount,
                    PaymentDate = loan.LoanDate.AddMonths(i),
                    PaymentType = "Monthly",
                    CreatedAt = DateTime.Now
                };

                installments.Add(installment);
            }

            await _context.LoanPayments.AddRangeAsync(installments);
            await _context.SaveChangesAsync();
        }

        private static string GetLoanStatusText(string status)
        {
            return status switch
            {
                "Pending" => "قيد الانتظار",
                "Approved" => "موافق",
                "Rejected" => "مرفوض",
                "Paid" => "مسدد بالكامل",
                "PartiallyPaid" => "مسدد جزئياً",
                _ => "غير معروف"
            };
        }

        #endregion
    }

    // DTO Classes
    public class LoanRequestDto
    {
        public int EmployeeId { get; set; }
        public decimal LoanAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime ExpectedPaybackDate { get; set; }
        public int InstallmentMonths { get; set; }
        public string Reason { get; set; }
        public int ApprovingManagerId { get; set; }
    }

    public class LoanResponseDto
    {
        public int LoanId { get; set; }
        public string LoanNumber { get; set; }
        public DateTime LoanDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public int ApprovingManagerId { get; set; }
    }

    public class LoanCalculationRequest
    {
        public int EmployeeId { get; set; }
        public decimal LoanAmount { get; set; }
        public int InstallmentMonths { get; set; }
    }

    public class LoanCalculationDto
    {
        public decimal LoanAmount { get; set; }
        public int InstallmentMonths { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public decimal MaxAllowedAmount { get; set; }
        public DateTime CalculationDate { get; set; }
    }

    public class EmployeeLoanDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string DepartmentName { get; set; }
        public string JobTitleName { get; set; }
        public string BranchName { get; set; }
        public DateTime HireDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal MaxAllowedAmount { get; set; }
        public decimal CurrentLoanBalance { get; set; }
        public decimal FriendshipBoxBalance { get; set; }
        public bool CanTakeLoan { get; set; }
        public string EmployeeStatus { get; set; }
    }

    public class LoanHistoryDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ExpectedPaybackDate { get; set; }
        public int InstallmentCount { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string Reason { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }

    public class LoanDetailsDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; }
        public string EmployeeName { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ExpectedPaybackDate { get; set; }
        public int InstallmentCount { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public List<LoanPaymentDto> Payments { get; set; }
    }

    public class ManagerLoanDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string DepartmentName { get; set; }
        public string JobTitleName { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ExpectedPaybackDate { get; set; }
        public int InstallmentMonths { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ManagerLoanStatsDto
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public decimal TotalAmountPending { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public decimal TotalAmountRejected { get; set; }
    }

    public class RejectLoanRequest
    {
        public string Reason { get; set; }
    }

    public class LoanPaymentDto
    {
        public int Id { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; }
        public string Notes { get; set; }
    }
}