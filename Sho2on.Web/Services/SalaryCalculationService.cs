using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;
using Sho2on.Web.Models.Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class SalaryCalculationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly LateOvertimeService _lateOvertimeSvc;

        public SalaryCalculationService(IDbContextFactory<AppDbContext> dbFactory, LateOvertimeService lateOvertimeSvc)
        {
            _dbFactory = dbFactory;
            _lateOvertimeSvc = lateOvertimeSvc; 
        }

        public async Task<SalaryDetailsDto?> GetSalaryDetailsForMonthAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // جلب بيانات الموظف
            var user = await db.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            // جلب إعدادات الشهر
            var settings = await db.Settings.FirstOrDefaultAsync();
            int startDay = settings?.StartOfMonth ?? 26;
            int endDay = settings?.EndOfMonth ?? 25;

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);
            if (endDay < startDay) endDate = endDate.AddMonths(1);

            // جلب Benefits (الاستحقاقات) من جدول Benefits
            var benefits = await db.Benefits
                .Include(b => b.BenefitType)
                .Where(b => b.UserId == userId &&
                           b.BenefitType != null &&
                           b.BenefitType.Type == "Benefit" &&
                           b.Date >= startDate && b.Date <= endDate)
                .ToListAsync();

            // جلب Deductions (الخصومات) من جدول Benefits
            var deductions = await db.Benefits
                .Include(b => b.BenefitType)
                .Where(b => b.UserId == userId &&
                           b.BenefitType != null &&
                           b.BenefitType.Type == "Deduction" &&
                           b.Date >= startDate && b.Date <= endDate)
                .ToListAsync();

            // حساب الراتب الأساسي
            decimal basicSalary = CalculateBasicSalary(user);

            // تجميع الاستحقاقات حسب النوع
            decimal housingAllowance = GetBenefitAmount(benefits, "بدل سكن");
            decimal transportationAllowance = GetBenefitAmount(benefits, "بدل انتقال");
            decimal managementAllowance = GetBenefitAmount(benefits, "بدل إدارة");
            decimal natureAllowance = GetBenefitAmount(benefits, "بدل طبيعة عمل");
            decimal overtimeAmount = GetBenefitAmount(benefits, "إضافي");
            decimal rewards = GetBenefitAmount(benefits, "مكافآت");
            decimal targetCommission = GetBenefitAmount(benefits, "عمولات تحقيق");
            decimal externalCommission = GetBenefitAmount(benefits, "عمولات خارجية");

            decimal totalAdditions = benefits.Sum(b => b.Amount);

            // تجميع الخصومات حسب النوع
            decimal taxDeduction = GetBenefitAmount(deductions, "ضريبة");
            decimal insuranceDeduction = GetBenefitAmount(deductions, "تأمينات");
            decimal socialParticipation = GetBenefitAmount(deductions, "مشاركة اجتماعية");
            decimal friendshipBoxDeduction = GetBenefitAmount(deductions, "صندوق الزمالة");
            decimal absenceDeduction = GetBenefitAmount(deductions, "غياب");
            decimal lateDeduction = GetBenefitAmount(deductions, "تأخير");
            decimal loanDeduction = GetBenefitAmount(deductions, "سلفة");
            decimal penaltyDeduction = GetBenefitAmount(deductions, "جزاءات");

            decimal totalDeductions = deductions.Sum(b => b.Amount);

            // جلب صافي الراتب من SalaryPayments لو موجود
            var payment = await db.SalaryPayments
                .Where(s => s.UserId == userId && s.Month == month && s.Year == year)
                .FirstOrDefaultAsync();

            return new SalaryDetailsDto
            {
                Id = payment?.Id ?? 0,
                EmployeeName = user.FullName,
                EmployeeCode = user.Code ?? "",
                BranchName = user.Branch?.Name ?? "",
                DepartmentName = user.Department?.Name ?? "",
                JobTitleName = user.JobTitle?.Name ?? "",

                BasicSalary = basicSalary,

                // الاستحقاقات
                TotalAdditions = totalAdditions,

                // الخصومات
                TotalDeductions = totalDeductions,

                // الصافي
                NetSalary = payment?.NetSalary ?? (basicSalary + totalAdditions - totalDeductions),
                IsPaid = payment?.IsPaid ?? false,
                Month = month,
                Year = year,
                Notes = payment?.Notes
            };
        }


        public async Task<SalaryCalculationResult> CalculateEmployeeSalaryAsync(int userId, int month, int year)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users
                .Include(u => u.Salaries)
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new Exception("الموظف غير موجود");

            // استخدام نفس إعدادات الشهر من AttendanceService
            var settings = await db.Settings.FirstOrDefaultAsync();
            int startDay = settings?.StartOfMonth ?? 26;
            int endDay = settings?.EndOfMonth ?? 25;

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);
            if (endDay < startDay) endDate = endDate.AddMonths(1);

            var attendances = await db.Attendances
                .Where(a => a.UserId == userId && a.AttendanceDate >= startDate && a.AttendanceDate <= endDate)
                .ToListAsync();

            // حساب الراتب الأساسي
            decimal basicSalary = CalculateBasicSalary(user);

            // حساب الإضافات
            decimal additions = CalculateAdditions(user);

            // حساب الاستقطاعات
            decimal deductions = CalculateDeductions(user);

            // حساب الحضور
            var (overtimeValue, lateValue, absenceValue) = await CalculateAttendanceValues(user, attendances);

            decimal netSalary = basicSalary + additions + overtimeValue - deductions - lateValue - absenceValue;

            return new SalaryCalculationResult
            {
                BasicSalary = basicSalary,
                Additions = additions,
                Deductions = deductions,
                OvertimeValue = overtimeValue,
                LateValue = lateValue,
                AbsenceValue = absenceValue,
                NetSalary = netSalary
            };
        }

        private decimal CalculateBasicSalary(User user)
        {
            if (user.SalaryType == SalaryTypeEnum.Fixed)
                return user.FixedSalary ?? user.MainSalary ?? 0;
            if (user.SalaryType == SalaryTypeEnum.MonthlyHourly)
                return (user.HourlyRate ?? 0) * (user.MonthlyWorkingHours ?? 208);
            if (user.SalaryType == SalaryTypeEnum.DailyHourly)
                return (user.HourlyRate ?? 0) * (user.DailyWorkingHours ?? 8) * (user.WorkingDaysPerMonth ?? 26);
            return user.MainSalary ?? 0;
        }

        private decimal GetBenefitAmount(List<Benefit> benefits, string typeName)
        {
            return benefits
                .Where(b => b.BenefitType != null &&
                           b.BenefitType.Name.Contains(typeName))
                .Sum(b => b.Amount);
        }

        private decimal CalculateAdditions(User user)
        {
            decimal additions = 0;
            var salaries = user.Salaries ?? new List<Salary>();

            // بدل سكن (2)
            additions += salaries.Where(s => s.Type == 2).Sum(s => s.Amount);
            // بدل انتقال (3)
            additions += salaries.Where(s => s.Type == 3).Sum(s => s.Amount);
            // بدل إدارة (14)
            additions += salaries.Where(s => s.Type == 14).Sum(s => s.Amount);
            // بدل طبيعة عمل (15)
            additions += salaries.Where(s => s.Type == 15).Sum(s => s.Amount);
            // مكافآت (11)
            additions += salaries.Where(s => s.Type == 11).Sum(s => s.Amount);

            return additions;
        }

        private decimal CalculateDeductions(User user)
        {
            decimal deductions = 0;
            var salaries = user.Salaries ?? new List<Salary>();

            // ضريبة (5)
            deductions += salaries.Where(s => s.Type == 5).Sum(s => s.Amount);
            // تأمينات (4)
            deductions += salaries.Where(s => s.Type == 4).Sum(s => s.Amount);
            // مشاركة اجتماعية (6)
            deductions += salaries.Where(s => s.Type == 6).Sum(s => s.Amount);
            // صندوق الزمالة (13)
            deductions += salaries.Where(s => s.Type == 13).Sum(s => s.Amount);

            return deductions;
        }


        private async Task<(decimal OvertimeValue, decimal LateValue, decimal AbsenceValue)> CalculateAttendanceValues(
            User user, List<Attendance> attendances)
        {
            decimal overtimeValue = 0;
            decimal lateValue = 0;
            decimal absenceValue = 0;

            // حساب سعر الدقيقة
            decimal monthlySalary = CalculateBasicSalary(user);
            decimal minuteRate = _lateOvertimeSvc.CalculateMinuteRate(monthlySalary, user.MonthlyWorkingHours ?? 208);

            // جلب إعدادات التأخير
            var (lateType, lateValueSetting, lateRepeat) = await _lateOvertimeSvc.GetLateSettingsAsync();

            int lateCount = 0; // عداد التأخيرات

            foreach (var att in attendances)
            {
                // ═══ حساب الإضافي ═══
                if (!user.ExemptOvertime && att.Overtime.HasValue && att.Overtime.Value > TimeSpan.Zero)
                {
                    if (lateType == 0) // نظام الدقائق
                    {
                        overtimeValue += await _lateOvertimeSvc.CalculateOvertimeValueAsync(
                            att.Overtime.Value, minuteRate, 0);
                    }
                    else // نظام المالية
                    {
                        overtimeValue += await _lateOvertimeSvc.CalculateOvertimeValueAsync(
                            att.Overtime.Value, minuteRate, 1);
                    }
                }

                // ═══ حساب التأخير ═══
                if (!user.ExemptLate && att.Late.HasValue && att.Late.Value > TimeSpan.Zero)
                {
                    lateCount++;

                    if (lateType == 0) // نظام الدقائق
                    {
                        lateValue += await _lateOvertimeSvc.CalculateLateValueAsync(
                            att.Late.Value, minuteRate, 0);
                    }
                    else // نظام المالية
                    {
                        lateValue += await _lateOvertimeSvc.CalculateLateValueAsync(
                            att.Late.Value, minuteRate, 1);
                    }
                }

                // ═══ حساب الغياب ═══
                if (!user.ExemptAbsence && att.IsAbsence)
                {
                    decimal dailySalary = monthlySalary / 30;
                    absenceValue += dailySalary;
                }
            }

            // ═══ حساب التأخير المتكرر ═══
            if (lateRepeat > 0 && lateCount >= lateRepeat)
            {
                lateValue += lateValueSetting * (lateCount / lateRepeat);
            }

            return (overtimeValue, lateValue, absenceValue);
        }
    }

    public class SalaryCalculationResult
    {
        public decimal BasicSalary { get; set; }
        public decimal Additions { get; set; }
        public decimal Deductions { get; set; }
        public decimal OvertimeValue { get; set; }
        public decimal LateValue { get; set; }
        public decimal AbsenceValue { get; set; }
        public decimal NetSalary { get; set; }
    }
}