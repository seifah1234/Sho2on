using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;
using Sho2on.Web.Models.Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class SalaryService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly SalaryAttendanceCalculationService _attendanceCalcSvc;

        public SalaryService(IDbContextFactory<AppDbContext> dbFactory, SalaryAttendanceCalculationService attendanceCalcSvc)
        {
            _dbFactory = dbFactory;
            _attendanceCalcSvc = attendanceCalcSvc;

        }

        public async Task<MonthSettings> GetMonthSettingsAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();
            return new MonthSettings
            {
                StartDay = settings?.StartOfMonth ?? 26,
                EndDay = settings?.EndOfMonth ?? 25
            };
        }

        public (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year, MonthSettings settings)
        {
            int startDay = settings.StartDay;
            int endDay = settings.EndDay;

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);

            if (endDay < startDay)
            {
                startDate = startDate.AddMonths(-1);
            }

            return (startDate, endDate);
        }

        /// <summary>
        /// جلب الرواتب الشهرية للموظفين
        /// </summary>
        public async Task<PagedResult<SalaryPaymentGroupDto>> GetSalariesPagedAsync(
            int? userId, int month, int year, string? status, int page, int pageSize)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, settings);

            var usersQuery = db.Users.Where(u => !u.IsArchived).AsQueryable();
            if (userId.HasValue) usersQuery = usersQuery.Where(u => u.Id == userId.Value);

            var users = await usersQuery.Include(u => u.Branch).OrderBy(u => u.FullName).ToListAsync();
            var allItems = new List<SalaryPaymentGroupDto>();

            foreach (var user in users)
            {
                // ═══ 1. حساب الحضور (إضافي + تأخير + غياب) ═══
                var attendanceSummary = await _attendanceCalcSvc.CalculateAsync(user.Id, month, year);
                decimal overtimeValue = attendanceSummary?.TotalOvertimeValue ?? 0;
                decimal lateValue = attendanceSummary?.TotalLateValue ?? 0;
                decimal absenceValue = attendanceSummary?.AbsenceDeductionValue ?? 0;

                // ═══ 2. الاستحقاقات والخصومات الشهرية من EmployeeBenefits ═══
                var monthlyBenefits = await db.EmployeeBenefits
                    .Include(eb => eb.BenefitType)
                    .Where(eb => eb.UserId == user.Id && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                    .ToListAsync();

                // ═══ 3. الاستحقاقات والخصومات Once من Benefits ═══
                var onceBenefits = await db.Benefits
                    .Include(b => b.BenefitType)
                    .Where(b => b.UserId == user.Id && b.Date >= startDate && b.Date <= endDate)
                    .ToListAsync();

                // ═══ 4. حساب الإجماليات ═══
                decimal fixedSalary = user.FixedSalary ?? 0;
                decimal variableSalary = user.HourlyRate ?? 0;
                decimal basicSalary = fixedSalary + variableSalary;

                // الاستحقاقات = استحقاقات شهرية + استحقاقات Once + إضافي
                decimal monthlyAdditions = 0;
                decimal monthlyDeductions = 0;

                foreach (var eb in monthlyBenefits)
                {
                    var bt = eb.BenefitType;
                    if (bt == null || !bt.IsActive) continue;
                    decimal baseSalary = bt.SalaryTarget switch
                    {
                        "Fixed" => fixedSalary,
                        "Variable" => variableSalary,
                        _ => basicSalary
                    };
                    decimal amount = baseSalary * (bt.Percentage / 100);
                    if (bt.Type == "Benefit") monthlyAdditions += amount;
                    else if (bt.Type == "Deduction") monthlyDeductions += amount;
                }

                decimal onceAdditions = onceBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount);
                decimal onceDeductions = onceBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount);

                // ═══ 5. إجمالي الاستحقاقات = شهرية + Once + إضافي ═══
                decimal totalAdditions = monthlyAdditions + onceAdditions + overtimeValue;

                // ═══ 6. إجمالي الخصومات = شهرية + Once + تأخير + غياب ═══
                decimal totalDeductions = monthlyDeductions + onceDeductions + lateValue + absenceValue;

                // ═══ 7. صافي الراتب ═══
                decimal netSalary = basicSalary + totalAdditions - totalDeductions;

                // التحقق من حالة الدفع
                var existingPayment = await db.SalaryPayments
                    .FirstOrDefaultAsync(sp => sp.UserId == user.Id && sp.Month == month && sp.Year == year);
                bool isPaid = existingPayment?.IsPaid ?? false;

                if (status == "Paid" && !isPaid) continue;
                if (status == "Unpaid" && isPaid) continue;

                allItems.Add(new SalaryPaymentGroupDto
                {
                    FirstSalaryId = existingPayment?.Id ?? 0,
                    User = new UserBasicInfo
                    {
                        Id = user.Id,
                        Code = user.Code ?? "",
                        FullName = user.FullName,
                        FixedSalary = fixedSalary,
                        VariableSalary = variableSalary
                    },
                    FixedSalary = fixedSalary,
                    VariableSalary = variableSalary,
                    BasicSalary = basicSalary,
                    OvertimeValue = overtimeValue,
                    LateValue = lateValue,
                    AbsenceValue = absenceValue,
                    TotalAdditions = totalAdditions,
                    TotalDeductions = totalDeductions,
                    NetSalary = netSalary,
                    IsPaid = isPaid
                });
            }

            var pagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<SalaryPaymentGroupDto>
            {
                Items = pagedItems,
                TotalCount = allItems.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// ملخص الرواتب
        /// </summary>
        public async Task<SalarySummaryDto> GetSalarySummaryAsync(int? userId, int? branchId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, settings);

            var usersQuery = db.Users.Where(u => !u.IsArchived).AsQueryable();
            if (userId.HasValue) usersQuery = usersQuery.Where(u => u.Id == userId.Value);
            if (branchId.HasValue) usersQuery = usersQuery.Where(u => u.BranchId == branchId.Value);

            var users = await usersQuery.ToListAsync();
            decimal totalAdditions = 0, totalDeductions = 0, totalNet = 0;
            int paidCount = 0, unpaidCount = 0;

            foreach (var user in users)
            {
                var attendanceSummary = await _attendanceCalcSvc.CalculateAsync(user.Id, month, year);
                decimal overtimeValue = attendanceSummary?.TotalOvertimeValue ?? 0;
                decimal lateValue = attendanceSummary?.TotalLateValue ?? 0;
                decimal absenceValue = attendanceSummary?.AbsenceDeductionValue ?? 0;

                decimal fixedSalary = user.FixedSalary ?? 0;
                decimal variableSalary = user.HourlyRate ?? 0;
                decimal basicSalary = fixedSalary + variableSalary;

                var monthlyBenefits = await db.EmployeeBenefits
                    .Include(eb => eb.BenefitType)
                    .Where(eb => eb.UserId == user.Id && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                    .ToListAsync();

                var onceBenefits = await db.Benefits
                    .Include(b => b.BenefitType)
                    .Where(b => b.UserId == user.Id && b.Date >= startDate && b.Date <= endDate)
                    .ToListAsync();

                decimal adds = 0, deds = 0;
                foreach (var eb in monthlyBenefits)
                {
                    var bt = eb.BenefitType; if (bt == null) continue;
                    decimal baseSalary = bt.SalaryTarget switch { "Fixed" => fixedSalary, "Variable" => variableSalary, _ => basicSalary };
                    decimal amount = baseSalary * (bt.Percentage / 100);
                    if (bt.Type == "Benefit") adds += amount; else deds += amount;
                }
                adds += onceBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount);
                deds += onceBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount);

                adds += overtimeValue;
                deds += lateValue + absenceValue;

                totalAdditions += adds;
                totalDeductions += deds;
                totalNet += basicSalary + adds - deds;

                var payment = await db.SalaryPayments.FirstOrDefaultAsync(sp => sp.UserId == user.Id && sp.Month == month && sp.Year == year);
                if (payment?.IsPaid == true) paidCount++; else unpaidCount++;
            }

            return new SalarySummaryDto
            {
                TotalEmployees = users.Count,
                TotalAdditions = totalAdditions,
                TotalDeductions = totalDeductions,
                TotalNetSalary = totalNet,
                PaidCount = paidCount,
                UnpaidCount = unpaidCount
            };
        }

        public async Task<(bool, string)> ProcessBulkPaymentAsync(int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, settings);

            var users = await db.Users.Where(u => !u.IsArchived).ToListAsync();
            int paidCount = 0;

            foreach (var user in users)
            {
                var existing = await db.SalaryPayments.FirstOrDefaultAsync(sp => sp.UserId == user.Id && sp.Month == month && sp.Year == year);
                if (existing?.IsPaid == true) continue;

                var attendanceSummary = await _attendanceCalcSvc.CalculateAsync(user.Id, month, year);
                decimal overtimeValue = attendanceSummary?.TotalOvertimeValue ?? 0;
                decimal lateValue = attendanceSummary?.TotalLateValue ?? 0;
                decimal absenceValue = attendanceSummary?.AbsenceDeductionValue ?? 0;

                decimal fixedSalary = user.FixedSalary ?? 0;
                decimal variableSalary = user.HourlyRate ?? 0;
                decimal basicSalary = fixedSalary + variableSalary;

                var monthlyBenefits = await db.EmployeeBenefits
                    .Include(eb => eb.BenefitType)
                    .Where(eb => eb.UserId == user.Id && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                    .ToListAsync();

                var onceBenefits = await db.Benefits
                    .Where(b => b.UserId == user.Id && b.Date >= startDate && b.Date <= endDate)
                    .ToListAsync();

                decimal adds = 0, deds = 0;
                foreach (var eb in monthlyBenefits)
                {
                    var bt = eb.BenefitType; if (bt == null) continue;
                    decimal baseSalary = bt.SalaryTarget switch { "Fixed" => fixedSalary, "Variable" => variableSalary, _ => basicSalary };
                    decimal amount = baseSalary * (bt.Percentage / 100);
                    if (bt.Type == "Benefit") adds += amount; else deds += amount;
                }
                adds += onceBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount);
                deds += onceBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount);

                adds += overtimeValue;
                deds += lateValue + absenceValue;

                decimal netSalary = basicSalary + adds - deds;

                if (existing != null)
                {
                    existing.IsPaid = true; existing.ActualPaymentDate = DateTime.Now;
                    existing.NetSalary = netSalary; existing.TotalAdditions = adds; existing.TotalDeductions = deds;
                }
                else
                {
                    db.SalaryPayments.Add(new SalaryPayment
                    {
                        UserId = user.Id,
                        Month = month,
                        Year = year,
                        BasicSalary = basicSalary,
                        TotalAdditions = adds,
                        TotalDeductions = deds,
                        NetSalary = netSalary,
                        IsPaid = true,
                        PaymentDate = DateTime.Now,
                        ActualPaymentDate = DateTime.Now,
                        CreatedAt = DateTime.Now
                    });
                }
                paidCount++;
            }

            await db.SaveChangesAsync();
            return (true, $"تم صرف رواتب {paidCount} موظف بنجاح");
        }

        public async Task<(bool, string)> PaySingleEmployeeAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, settings);

            var user = await db.Users.FindAsync(userId);
            if (user == null) return (false, "الموظف غير موجود");

            var attendanceSummary = await _attendanceCalcSvc.CalculateAsync(userId, month, year);
            decimal overtimeValue = attendanceSummary?.TotalOvertimeValue ?? 0;
            decimal lateValue = attendanceSummary?.TotalLateValue ?? 0;
            decimal absenceValue = attendanceSummary?.AbsenceDeductionValue ?? 0;

            decimal fixedSalary = user.FixedSalary ?? 0;
            decimal variableSalary = user.HourlyRate ?? 0;
            decimal basicSalary = fixedSalary + variableSalary;

            var monthlyBenefits = await db.EmployeeBenefits
                .Include(eb => eb.BenefitType)
                .Where(eb => eb.UserId == userId && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                .ToListAsync();

            var onceBenefits = await db.Benefits
                .Where(b => b.UserId == userId && b.Date >= startDate && b.Date <= endDate)
                .ToListAsync();

            decimal adds = 0, deds = 0;
            foreach (var eb in monthlyBenefits)
            {
                var bt = eb.BenefitType; if (bt == null) continue;
                decimal baseSalary = bt.SalaryTarget switch { "Fixed" => fixedSalary, "Variable" => variableSalary, _ => basicSalary };
                decimal amount = baseSalary * (bt.Percentage / 100);
                if (bt.Type == "Benefit") adds += amount; else deds += amount;
            }
            adds += onceBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount);
            deds += onceBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount);
            adds += overtimeValue;
            deds += lateValue + absenceValue;

            decimal netSalary = basicSalary + adds - deds;

            var existing = await db.SalaryPayments.FirstOrDefaultAsync(sp => sp.UserId == userId && sp.Month == month && sp.Year == year);
            if (existing != null)
            {
                existing.IsPaid = true; existing.ActualPaymentDate = DateTime.Now;
                existing.NetSalary = netSalary; existing.TotalAdditions = adds; existing.TotalDeductions = deds;
                existing.IsOffCycle = false; // تأكيد إنه صرف شهري عادي مش فوري
            }
            else
            {
                db.SalaryPayments.Add(new SalaryPayment
                {
                    UserId = userId,
                    Month = month,
                    Year = year,
                    BasicSalary = basicSalary,
                    TotalAdditions = adds,
                    TotalDeductions = deds,
                    NetSalary = netSalary,
                    IsPaid = true,
                    IsOffCycle = false,
                    PaymentDate = DateTime.Now,
                    ActualPaymentDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
            return (true, $"تم صرف راتب {user.FullName} بنجاح ({netSalary:N0} ج.م)");
        }

        public async Task<decimal> GetTotalOffCyclePaymentsAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.SalaryPayments
                .Where(s => s.UserId == userId && s.Month == month && s.Year == year && s.IsOffCycle)
                .SumAsync(s => s.NetSalary);
        }

        public async Task<SalaryDetailsDto?> GetSalaryDetailsAsync(int salaryPaymentId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.SalaryPayments
                .Include(s => s.User).ThenInclude(u => u.Branch)
                .Where(s => s.Id == salaryPaymentId)
                .Select(s => new SalaryDetailsDto
                {
                    Id = s.Id,
                    EmployeeName = s.User.FullName,
                    EmployeeCode = s.User.Code ?? "",
                    BranchName = s.User.Branch != null ? s.User.Branch.Name : "",
                    BasicSalary = s.BasicSalary,
                    TotalAdditions = s.TotalAdditions,
                    TotalDeductions = s.TotalDeductions,
                    NetSalary = s.NetSalary,
                    IsPaid = s.IsPaid,
                    IsOffCycle = s.IsOffCycle,
                    Month = s.Month,
                    Year = s.Year,
                    PaymentDate = s.PaymentDate,
                    ActualPaymentDate = s.ActualPaymentDate,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        // في SalaryService.cs — دالة جديدة
        public async Task<SalaryDetailsDto?> GetSalaryPreviewAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await GetMonthSettingsAsync();
            var (startDate, endDate) = GetCustomMonthDates(month, year, settings);

            var user = await db.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var attendanceSummary = await _attendanceCalcSvc.CalculateAsync(userId, month, year);
            decimal overtimeValue = attendanceSummary?.TotalOvertimeValue ?? 0;
            decimal lateValue = attendanceSummary?.TotalLateValue ?? 0;
            decimal absenceValue = attendanceSummary?.AbsenceDeductionValue ?? 0;

            decimal fixedSalary = user.FixedSalary ?? 0;
            decimal variableSalary = user.HourlyRate ?? 0;
            decimal basicSalary = fixedSalary + variableSalary;

            var monthlyBenefits = await db.EmployeeBenefits
                .Include(eb => eb.BenefitType)
                .Where(eb => eb.UserId == userId && eb.IsActive && eb.BenefitType.Frequency == "Monthly")
                .ToListAsync();

            var onceBenefits = await db.Benefits
                .Include(b => b.BenefitType)
                .Where(b => b.UserId == userId && b.Date >= startDate && b.Date <= endDate)
                .ToListAsync();

            decimal adds = 0, deds = 0;
            foreach (var eb in monthlyBenefits)
            {
                var bt = eb.BenefitType; if (bt == null) continue;
                decimal baseSalary = bt.SalaryTarget switch { "Fixed" => fixedSalary, "Variable" => variableSalary, _ => basicSalary };
                decimal amount = baseSalary * (bt.Percentage / 100);
                if (bt.Type == "Benefit") adds += amount; else deds += amount;
            }
            adds += onceBenefits.Where(b => b.BenefitType?.Type == "Benefit").Sum(b => b.Amount);
            deds += onceBenefits.Where(b => b.BenefitType?.Type == "Deduction").Sum(b => b.Amount);
            adds += overtimeValue;
            deds += lateValue + absenceValue;

            // لو موجود صرف فعلي بالفعل، هات بياناته الحقيقية (تاريخ الصرف...)، وإلا رجّع معاينة بس
            var existingPayment = await db.SalaryPayments
                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.Month == month && sp.Year == year);

            return new SalaryDetailsDto
            {
                Id = existingPayment?.Id ?? 0,
                UserId = user.Id,
                EmployeeName = user.FullName,
                EmployeeCode = user.Code ?? "",
                BranchName = user.Branch?.Name ?? "",
                BasicSalary = basicSalary,
                TotalAdditions = adds,
                TotalDeductions = deds,
                NetSalary = basicSalary + adds - deds,
                IsPaid = existingPayment?.IsPaid ?? false,
                IsOffCycle = existingPayment?.IsOffCycle ?? false,
                Month = month,
                Year = year,
                PaymentDate = existingPayment?.PaymentDate,
                ActualPaymentDate = existingPayment?.ActualPaymentDate,
                Notes = existingPayment?.Notes,
                CreatedAt = existingPayment?.CreatedAt ?? DateTime.Now
            };
        }

        public async Task<(bool, string, int)> ProcessOffCyclePaymentAsync(int userId, decimal amount, string? notes)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var payment = new SalaryPayment
            {
                UserId = userId,
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                NetSalary = amount,
                TotalAdditions = amount,
                IsPaid = true,
                IsOffCycle = true,
                PaymentDate = DateTime.Now,
                ActualPaymentDate = DateTime.Now,
                Notes = notes,
                CreatedAt = DateTime.Now
            };
            db.SalaryPayments.Add(payment);
            await db.SaveChangesAsync();
            return (true, "تم الصرف الفوري بنجاح", payment.Id);
        }
    }
}