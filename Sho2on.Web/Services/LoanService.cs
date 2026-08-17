using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class LoanService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<LoanService> _logger;
        private readonly NotificationCenterService _notify;

        public LoanService(IDbContextFactory<AppDbContext> dbFactory, ILogger<LoanService> logger, NotificationCenterService notify)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _notify = notify;
        }

        /// <summary>
        /// جلب كل طلبات السلف مع فلترة
        /// </summary>
        public async Task<PagedResult<LoanDto>> GetPagedListAsync(
            int? userId = null,
            string? status = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 15)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Loans
                .Include(l => l.User)
                .Include(l => l.ApprovedByUser)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(l =>
                    l.User!.FullName!.Contains(term) ||
                    l.User!.Code!.Contains(term) ||
                    l.Reason!.Contains(term));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserName = l.User!.FullName ?? "غير معروف",
                    UserCode = l.User.Code ?? "",
                    LoanAmount = l.LoanAmount,
                    RemainingAmount = l.RemainingAmount,
                    AmountPaid = l.AmountPaid,
                    LoanDate = l.LoanDate,
                    ExpectedPaybackDate = l.ExpectedPaybackDate,
                    ActualPaybackDate = l.ActualPaybackDate,
                    InstallmentCount = l.InstallmentCount,
                    MonthlyInstallment = l.MonthlyInstallment,
                    Status = l.Status,
                    Reason = l.Reason,
                    Notes = l.Notes,
                    ApprovedByUserId = l.ApprovedByUserId,
                    ApprovedByName = l.ApprovedByUser != null ? l.ApprovedByUser.FullName : "",
                    ApprovedDate = l.ApprovedDate,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    PaymentCount = l.LoanPayments != null ? l.LoanPayments.Count : 0
                })
                .ToListAsync();

            return new PagedResult<LoanDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// جلب تفاصيل سلفة معينة
        /// </summary>
        public async Task<LoanDto?> GetByIdAsync(int loanId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var loan = await _db.Loans
                .Include(l => l.User)
                .Include(l => l.ApprovedByUser)
                .Include(l => l.LoanPayments)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null) return null;

            return new LoanDto
            {
                Id = loan.Id,
                UserId = loan.UserId,
                UserName = loan.User?.FullName ?? "غير معروف",
                UserCode = loan.User?.Code ?? "",
                LoanAmount = loan.LoanAmount,
                RemainingAmount = loan.RemainingAmount,
                AmountPaid = loan.AmountPaid,
                LoanDate = loan.LoanDate,
                ExpectedPaybackDate = loan.ExpectedPaybackDate,
                ActualPaybackDate = loan.ActualPaybackDate,
                InstallmentCount = loan.InstallmentCount,
                MonthlyInstallment = loan.MonthlyInstallment,
                Status = loan.Status,
                Reason = loan.Reason,
                Notes = loan.Notes,
                ApprovedByUserId = loan.ApprovedByUserId,
                ApprovedByName = loan.ApprovedByUser?.FullName ?? "",
                ApprovedDate = loan.ApprovedDate,
                CreatedAt = loan.CreatedAt,
                UpdatedAt = loan.UpdatedAt,
                PaymentCount = loan.LoanPayments?.Count ?? 0,
                Payments = loan.LoanPayments?.Select(p => new LoanPaymentDto
                {
                    Id = p.Id,
                    PaymentAmount = p.PaymentAmount,
                    PaymentDate = p.PaymentDate,
                    PaymentType = p.PaymentType,
                    Notes = p.Notes
                }).ToList() ?? new()
            };
        }

        /// <summary>
        /// تقديم طلب سلفة جديد
        /// </summary>
        public async Task<(bool Success, string Message, int? LoanId)> RequestLoanAsync(LoanRequestDto request)
        {
            try
            {
                // التحقق من وجود الموظف
            using var _db = await _dbFactory.CreateDbContextAsync();
                var user = await _db.Users.FindAsync(request.UserId);
                if (user == null)
                    return (false, "الموظف غير موجود", null);

                // التحقق من صلاحية أخذ سلفة
                if (!user.CanTakeLoan)
                    return (false, "هذا الموظف غير مسموح له بأخذ سلفة", null);

                // التحقق من الحد الأقصى للسلف
                var totalActiveLoans = await _db.Loans
                    .Where(l => l.UserId == request.UserId &&
                                (l.Status == "Pending" || l.Status == "Approved" || l.Status == "PartiallyPaid"))
                    .SumAsync(l => l.RemainingAmount);

                var maxLoanAmount = user.MaxLoanAmount;
                if (totalActiveLoans + request.LoanAmount > maxLoanAmount)
                    return (false, $"تجاوزت الحد الأقصى للسلف (الحد الأقصى: {maxLoanAmount:N0})", null);

                // حساب القسط الشهري
                decimal monthlyInstallment = request.InstallmentCount > 0
                    ? request.LoanAmount / request.InstallmentCount
                    : request.LoanAmount; // لو مرة واحدة

                var loan = new Loan
                {
                    UserId = request.UserId,
                    LoanAmount = request.LoanAmount,
                    RemainingAmount = request.LoanAmount,
                    LoanDate = DateTime.Now,
                    ExpectedPaybackDate = request.ExpectedPaybackDate,
                    InstallmentCount = request.InstallmentCount,
                    MonthlyInstallment = monthlyInstallment,
                    AmountPaid = 0,
                    Status = "Pending",
                    Reason = request.Reason,
                    Notes = request.Notes,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _db.Loans.Add(loan);
                var employee = await _db.Users.FindAsync(request.UserId);
                var managers = await _db.Users.Where(u => u.Id == employee!.ManagerId).Select(u => u.Id).ToListAsync();
                if (managers.Count > 0)
                {
                    await _notify.CreateForApproversAsync(managers,
                        "طلب سلفة جديد",
                        $"{employee!.FullName} طلب سلفة بقيمة {request.LoanAmount:N0} ج.م",
                        "bi-cash-stack",
                        "/loans");
                }
                await _db.SaveChangesAsync();

                return (true, "تم تقديم طلب السلفة بنجاح", loan.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تقديم طلب سلفة للموظف {UserId}", request.UserId);
                return (false, "حدث خطأ أثناء تقديم الطلب", null);
            }
        }

        /// <summary>
        /// الموافقة على سلفة
        /// </summary>
        public async Task<(bool Success, string Message)> ApproveLoanAsync(int loanId, int approvedByUserId)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var loan = await _db.Loans.FindAsync(loanId);
                if (loan == null)
                    return (false, "السلفة غير موجودة");

                if (loan.Status != "Pending")
                    return (false, $"لا يمكن الموافقة على سلفة بحالة {loan.Status}");

                loan.Status = "Approved";
                loan.ApprovedByUserId = approvedByUserId;
                loan.ApprovedDate = DateTime.Now;
                loan.UpdatedAt = DateTime.Now;

                var manager = await _db.Users.FindAsync(approvedByUserId);

                if (manager != null)
                {
                    await _notify.CreateAsync(loan.UserId,
                        "مراجعة طلب سلفة",
                        $"{manager.FullName} تم الموافقة على طلب السلفة من",
                        "bi-cash-stack",
                        "/loans");
                }

                await _db.SaveChangesAsync();
                return (true, "تمت الموافقة على السلفة بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء الموافقة على السلفة {LoanId}", loanId);
                return (false, "حدث خطأ أثناء الموافقة");
            }
        }

        /// <summary>
        /// رفض سلفة
        /// </summary>
        public async Task<(bool Success, string Message)> RejectLoanAsync(int loanId, string? rejectionReason = null)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var loan = await _db.Loans.FindAsync(loanId);
                if (loan == null)
                    return (false, "السلفة غير موجودة");

                if (loan.Status != "Pending")
                    return (false, $"لا يمكن رفض سلفة بحالة {loan.Status}");

                loan.Status = "Rejected";
                loan.Notes = rejectionReason ?? loan.Notes;
                loan.UpdatedAt = DateTime.Now;

                var manager = await _db.Users.FindAsync(loan.ApprovedByUserId);

                if (manager != null)
                {
                    await _notify.CreateAsync(loan.UserId,
                        "مراجعة طلب سلفة",
                        $"{manager.FullName} تم رفض طلب السلفة من",
                        "bi-cash-stack",
                        "/loans");
                }

                await _db.SaveChangesAsync();
                return (true, "تم رفض السلفة");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء رفض السلفة {LoanId}", loanId);
                return (false, "حدث خطأ أثناء الرفض");
            }
        }

        /// <summary>
        /// تسجيل دفعة قسط شهرية
        /// </summary>
        public async Task<(bool Success, string Message)> RecordPaymentAsync(LoanPaymentDto payment)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var loan = await _db.Loans.FindAsync(payment.LoanId);
                if (loan == null)
                    return (false, "السلفة غير موجودة");

                if (loan.Status != "Approved" && loan.Status != "PartiallyPaid")
                    return (false, $"لا يمكن تسجيل دفعة لسلفة بحالة {loan.Status}");

                if (payment.PaymentAmount > loan.RemainingAmount)
                    return (false, $"المبلغ المدفوع ({payment.PaymentAmount:N0}) أكبر من المبلغ المتبقي ({loan.RemainingAmount:N0})");

                // تسجيل الدفعة
                var loanPayment = new LoanPayment
                {
                    LoanId = loan.Id,
                    PaymentAmount = payment.PaymentAmount,
                    PaymentDate = payment.PaymentDate,
                    PaymentType = payment.PaymentType ?? "Monthly",
                    Notes = payment.Notes,
                    CreatedAt = DateTime.Now
                };

                _db.LoanPayments.Add(loanPayment);

                // تحديث السلفة
                loan.AmountPaid += payment.PaymentAmount;
                loan.RemainingAmount -= payment.PaymentAmount;
                loan.UpdatedAt = DateTime.Now;

                if (loan.RemainingAmount <= 0)
                {
                    loan.Status = "Paid";
                    loan.ActualPaybackDate = DateTime.Now;
                }
                else
                {
                    loan.Status = "PartiallyPaid";
                }

                await _db.SaveChangesAsync();
                return (true, "تم تسجيل الدفعة بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تسجيل دفعة للسلفة {LoanId}", payment.LoanId);
                return (false, "حدث خطأ أثناء تسجيل الدفعة");
            }
        }

        /// <summary>
        /// حذف سلفة (منطقي أو فعلي حسب الحالة)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteLoanAsync(int loanId)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var loan = await _db.Loans.FindAsync(loanId);
                if (loan == null)
                    return (false, "السلفة غير موجودة");

                // لا يمكن حذف سلفة مدفوعة أو معتمدة
                if (loan.Status == "Paid" || loan.Status == "Approved" || loan.Status == "PartiallyPaid")
                    return (false, "لا يمكن حذف سلفة تم الموافقة عليها أو سدادها");

                _db.Loans.Remove(loan);
                await _db.SaveChangesAsync();
                return (true, "تم حذف السلفة بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف السلفة {LoanId}", loanId);
                return (false, "حدث خطأ أثناء الحذف");
            }
        }

        /// <summary>
        /// جلب إحصائيات السلف لموظف
        /// </summary>
        public async Task<LoanStatisticsDto> GetStatisticsAsync(int userId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var loans = await _db.Loans
                .Where(l => l.UserId == userId)
                .ToListAsync();

            return new LoanStatisticsDto
            {
                TotalLoans = loans.Count,
                TotalAmount = loans.Sum(l => l.LoanAmount),
                TotalPaid = loans.Sum(l => l.AmountPaid),
                TotalRemaining = loans.Sum(l => l.RemainingAmount),
                ActiveLoans = loans.Count(l => l.Status == "Approved" || l.Status == "PartiallyPaid"),
                PendingLoans = loans.Count(l => l.Status == "Pending")
            };
        }
    }
}