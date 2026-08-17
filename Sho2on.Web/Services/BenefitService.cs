using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class BenefitService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<BenefitService> _logger;

        public BenefitService(ILogger<BenefitService> logger, IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<PagedResult<BenefitDto>> GetPagedListAsync(
            int? userId = null,
            int? benefitTypeId = null,
            string? type = null, // Benefit أو Deduction
            string? searchTerm = null,
            int page = 1,
            int pageSize = 15)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Benefits
                .Include(b => b.User)
                .Include(b => b.BenefitType)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(b => b.UserId == userId.Value);

            if (benefitTypeId.HasValue)
                query = query.Where(b => b.BenefitTypeId == benefitTypeId.Value);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(b => b.BenefitType != null && b.BenefitType.Type == type);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(b =>
                    (b.User != null && b.User.FullName != null && b.User.FullName.Contains(term)) ||
                    (b.BenefitType != null && b.BenefitType.Name.Contains(term)) ||
                    (b.Notes != null && b.Notes.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.Date)
                .ThenByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BenefitDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    UserName = b.User != null ? b.User.FullName ?? "غير معروف" : "غير معروف",
                    UserCode = b.User != null ? b.User.Code ?? "" : "",
                    BenefitTypeId = b.BenefitTypeId,
                    BenefitTypeName = b.BenefitType != null ? b.BenefitType.Name : "",
                    BenefitType = b.BenefitType != null ? b.BenefitType.Type : "Benefit",
                    Amount = b.Amount,
                    Date = b.Date,
                    Notes = b.Notes,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return new PagedResult<BenefitDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<BenefitDto?> GetByIdAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var benefit = await _db.Benefits
                .Include(b => b.User)
                .Include(b => b.BenefitType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (benefit == null) return null;

            return new BenefitDto
            {
                Id = benefit.Id,
                UserId = benefit.UserId,
                UserName = benefit.User?.FullName ?? "غير معروف",
                UserCode = benefit.User?.Code ?? "",
                BenefitTypeId = benefit.BenefitTypeId,
                BenefitTypeName = benefit.BenefitType?.Name ?? "",
                BenefitType = benefit.BenefitType?.Type ?? "Benefit",
                Amount = benefit.Amount,
                Date = benefit.Date,
                Notes = benefit.Notes,
                CreatedAt = benefit.CreatedAt,
                UpdatedAt = benefit.UpdatedAt
            };
        }

        public async Task<(bool Success, string Message, int? Id)> CreateAsync(BenefitDto dto)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                // التحقق من وجود الموظف
                var user = await _db.Users.FindAsync(dto.UserId);
                if (user == null)
                    return (false, "الموظف غير موجود", null);

                // التحقق من وجود نوع الاستحقاق
                var benefitType = await _db.BenefitTypes.FindAsync(dto.BenefitTypeId);
                if (benefitType == null)
                    return (false, "نوع الاستحقاق غير موجود", null);

                var benefit = new Benefit
                {
                    UserId = dto.UserId,
                    BenefitTypeId = dto.BenefitTypeId,
                    Amount = dto.Amount,
                    Date = dto.Date,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.Now
                };

                _db.Benefits.Add(benefit);
                await _db.SaveChangesAsync();

                return (true, "تمت الإضافة بنجاح", benefit.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إضافة استحقاق/استقطاع");
                return (false, "حدث خطأ أثناء الإضافة", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(BenefitDto dto)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var benefit = await _db.Benefits.FindAsync(dto.Id);
                if (benefit == null)
                    return (false, "العنصر غير موجود");

                var benefitType = await _db.BenefitTypes.FindAsync(dto.BenefitTypeId);
                if (benefitType == null)
                    return (false, "نوع الاستحقاق غير موجود");

                benefit.BenefitTypeId = dto.BenefitTypeId;
                benefit.Amount = dto.Amount;
                benefit.Date = dto.Date;
                benefit.Notes = dto.Notes;
                benefit.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();
                return (true, "تم التحديث بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحديث استحقاق/استقطاع {Id}", dto.Id);
                return (false, "حدث خطأ أثناء التحديث");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
            using var _db = await _dbFactory.CreateDbContextAsync();
                var benefit = await _db.Benefits.FindAsync(id);
                if (benefit == null)
                    return (false, "العنصر غير موجود");

                _db.Benefits.Remove(benefit);
                await _db.SaveChangesAsync();
                return (true, "تم الحذف بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف استحقاق/استقطاع {Id}", id);
                return (false, "حدث خطأ أثناء الحذف");
            }
        }

        public async Task<List<BenefitTypeDto>> GetBenefitTypesAsync(string? type = null)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.BenefitTypes.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(bt => bt.Type == type);

            return await query
                .Where(bt => bt.IsActive)
                .OrderBy(bt => bt.Type)
                .ThenBy(bt => bt.Name)
                .Select(bt => new BenefitTypeDto
                {
                    Id = bt.Id,
                    Name = bt.Name,
                    Type = bt.Type,
                    Description = bt.Description
                })
                .ToListAsync();
        }

        /// <summary>
        /// جلب الاستحقاقات والخصومات للموظف في شهر معين
        /// - Monthly: من جدول EmployeeBenefits (المرتبطة بالموظف)
        /// - Once: من جدول Benefits (اللي اتعملت مرة واحدة في الشهر ده)
        /// </summary>
        public async Task<(List<BenefitDto> Benefits, List<BenefitDto> Deductions)> GetEmployeeBenefitsForMonthAsync(
            int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // جلب إعدادات الشهر
            var settings = await db.Settings.FirstOrDefaultAsync();
            int startDay = settings?.StartOfMonth ?? 26;
            int endDay = settings?.EndOfMonth ?? 25;

            // حساب تواريخ الشهر المخصص
            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);
            if (endDay < startDay) startDate = startDate.AddMonths(-1);

            var resultBenefits = new List<BenefitDto>();
            var resultDeductions = new List<BenefitDto>();

            // ═══ 1. جلب الاستحقاقات الشهرية من EmployeeBenefits ═══
            var employeeMonthlyBenefits = await db.EmployeeBenefits
                .Include(eb => eb.BenefitType)
                .Where(eb => eb.UserId == userId && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                .ToListAsync();

            // جلب بيانات الموظف لحساب المبالغ
            var user = await db.Users.FindAsync(userId);
            decimal fixedSalary = user?.FixedSalary ?? 0;
            decimal variableSalary = user?.HourlyRate ?? 0;
            decimal totalSalary = fixedSalary + variableSalary;

            foreach (var empBenefit in employeeMonthlyBenefits)
            {
                var benefitType = empBenefit.BenefitType;
                if (benefitType == null || !benefitType.IsActive) continue;

                // تحديد أساس الحساب
                decimal baseSalary = benefitType.SalaryTarget switch
                {
                    "Fixed" => fixedSalary,
                    "Variable" => variableSalary,
                    "Total" => totalSalary,
                    _ => totalSalary
                };

                // حساب المبلغ = الأساس × النسبة
                decimal amount = baseSalary * (benefitType.Percentage / 100);

                var dto = new BenefitDto
                {
                    Id = empBenefit.Id,
                    UserId = userId,
                    UserName = user?.FullName ?? "",
                    UserCode = user?.Code ?? "",
                    BenefitTypeId = benefitType.Id,
                    BenefitTypeName = benefitType.Name,
                    BenefitType = benefitType.Type,
                    Amount = amount,
                    Date = startDate,
                    Notes = $"شهري تلقائي - {benefitType.Name} ({benefitType.Percentage}% من {GetTargetName(benefitType.SalaryTarget)})",
                    Frequency = "Monthly",
                    CreatedAt = DateTime.Now
                };

                if (benefitType.Type == "Benefit")
                    resultBenefits.Add(dto);
                else if (benefitType.Type == "Deduction")
                    resultDeductions.Add(dto);
            }

            // ═══ 2. جلب الاستحقاقات والخصومات الـ Once من جدول Benefits ═══
            var onceBenefits = await db.Benefits
                .Include(b => b.BenefitType)
                .Include(b => b.User)
                .Where(b => b.UserId == userId &&
                           b.Date >= startDate &&
                           b.Date <= endDate &&
                           b.BenefitType != null &&
                           b.BenefitType.Frequency == "Once")
                .OrderByDescending(b => b.Date)
                .Select(b => new BenefitDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    UserName = b.User != null ? b.User.FullName ?? "" : "",
                    UserCode = b.User != null ? b.User.Code ?? "" : "",
                    BenefitTypeId = b.BenefitTypeId,
                    BenefitTypeName = b.BenefitType != null ? b.BenefitType.Name : "",
                    BenefitType = b.BenefitType != null ? b.BenefitType.Type : "",
                    Amount = b.Amount,
                    Date = b.Date,
                    Notes = b.Notes,
                    Frequency = "Once",
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            // توزيع الـ Once
            resultBenefits.AddRange(onceBenefits.Where(b => b.BenefitType == "Benefit"));
            resultDeductions.AddRange(onceBenefits.Where(b => b.BenefitType == "Deduction"));

            return (resultBenefits, resultDeductions);
        }

        /// <summary>
        /// الحصول على اسم الهدف بالعربي
        /// </summary>
        private string GetTargetName(string target) => target switch
        {
            "Fixed" => "الراتب الثابت",
            "Variable" => "الراتب المتغير",
            "Total" => "إجمالي الراتب",
            _ => "إجمالي الراتب"
        };

        public async Task<BenefitStatisticsDto?> GetEmployeeBenefitsStatisticsAsync(
            int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var settings = await db.Settings.FirstOrDefaultAsync();
            int startDay = settings?.StartOfMonth ?? 26;
            int endDay = settings?.EndOfMonth ?? 25;

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);
            if (endDay < startDay) startDate = startDate.AddMonths(-1);

            var allBenefits = await db.Benefits
                .Include(b => b.BenefitType)
                .Where(b => b.UserId == userId &&
                           b.Date >= startDate &&
                           b.Date <= endDate)
                .ToListAsync();

            if (!allBenefits.Any()) return null;

            return new BenefitStatisticsDto
            {
                TotalBenefits = allBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount),
                TotalDeductions = allBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount),
                NetAmount = allBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount)
                          - allBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount),
                TotalCount = allBenefits.Count,
                BenefitCount = allBenefits.Count(b => b.BenefitType?.Type == "Benefit"),
                DeductionCount = allBenefits.Count(b => b.BenefitType?.Type == "Deduction")
            };
        }
    

        public async Task<BenefitStatisticsDto> GetStatisticsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Benefits
                .Include(b => b.BenefitType)
                .Where(b => b.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(b => b.Date >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.Date <= toDate.Value);

            var benefits = await query.ToListAsync();

            return new BenefitStatisticsDto
            {
                TotalBenefits = benefits.Where(b => b.BenefitType != null && b.BenefitType.Type == "Benefit").Sum(b => b.Amount),
                TotalDeductions = benefits.Where(b => b.BenefitType != null && b.BenefitType.Type == "Deduction").Sum(b => b.Amount),
                NetAmount = benefits.Where(b => b.BenefitType != null && b.BenefitType.Type == "Benefit").Sum(b => b.Amount)
                            - benefits.Where(b => b.BenefitType != null && b.BenefitType.Type == "Deduction").Sum(b => b.Amount),
                TotalCount = benefits.Count,
                BenefitCount = benefits.Count(b => b.BenefitType != null && b.BenefitType.Type == "Benefit"),
                DeductionCount = benefits.Count(b => b.BenefitType != null && b.BenefitType.Type == "Deduction")
            };
        }
    }
}