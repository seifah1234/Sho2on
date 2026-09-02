// AttendanceReportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.API.Dtos;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceReportController : ControllerBase
    {
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AttendanceReportController(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        private async Task<(int StartDay, int EndDay)> GetMonthSettingsAsync()
        {
        using var _context = await _dbFactory.CreateDbContextAsync(); 
            var settings = await _context.Settings.FirstOrDefaultAsync();
            return (
                settings?.StartOfMonth ?? 26,
                settings?.EndOfMonth ?? 25
            );
        }


        private async Task<(DateOnly Start, DateOnly End)> GetMonthRange(int month, int year)
        {
        using var _context = await _dbFactory.CreateDbContextAsync(); 
            var (startDay, endDay) = await GetMonthSettingsAsync();

            DateTime startDate = new DateTime(year, month, startDay);
            DateTime endDate = new DateTime(year, month, endDay);

            // لو بداية الشهر أكبر من 15 (يعني الشهر بيبدأ في الشهر اللي قبله)
            if (startDay > endDay)
            {

                startDate = startDate.AddMonths(-1);
            }


            return (DateOnly.FromDateTime(startDate), DateOnly.FromDateTime(endDate));
        }

        // GET: api/AttendanceReport/Monthly/{userId}/{year}/{month}
        [HttpGet("Monthly/{userId}/{year}/{month}")]
        public async Task<ActionResult<ApiResponse<MonthlyReportDto>>> GetMonthlyReport(
            int userId, int year, int month)
        {
        using var _context = await _dbFactory.CreateDbContextAsync(); 
            try
            {
                // تحديد تاريخ البداية والنهاية للشهر

                var (startDate, endDate) = await GetMonthRange(month, year);
                var startDt = startDate.ToDateTime(TimeOnly.MinValue);
                var endDt = endDate.ToDateTime(TimeOnly.MaxValue);

                // جلب بيانات الحضور للشهر
                var attendances = await _context.Attendances
                    .Include(a => a.Shift)
                    .Include(a => a.CheckInFingerPrint)
                    .Include(a => a.CheckOutFingerPrint)
                    .Where(a => a.UserId == userId &&
                               a.AttendanceDate >= startDt &&
                               a.AttendanceDate <= endDt)
                    .OrderBy(a => a.AttendanceDate)
                    .ToListAsync();

                // جلب أيام الإجازة للشهر
                var leaves = await _context.Leaves
                    .Include(l => l.LeaveType)
                    .Where(l => l.UserId == userId &&
                               l.Status == 2 && // الموافق عليها فقط
                               !l.IsCancelled &&
                               ((l.StartDate <= endDt && l.EndDate >= startDt)))
                    .ToListAsync();

                // إنشاء بيانات التقرير
                var dailyReports = new List<DailyReportDto>();
                var summaryStats = new MonthlySummaryDto();

                DateTime currentDate = startDt;

                while (currentDate <= endDt)
                {
                    // تخطي أيام نهاية الأسبوع (السبت والجمعة)
                    if (currentDate.DayOfWeek != DayOfWeek.Friday &&
                        currentDate.DayOfWeek != DayOfWeek.Saturday)
                    {
                        var attendance = attendances.FirstOrDefault(a => a.AttendanceDate.Date == currentDate.Date);
                        var leave = leaves.FirstOrDefault(l => currentDate.Date >= l.StartDate.Date &&
                                                              currentDate.Date <= l.EndDate.Date);

                        var dailyReport = CreateDailyReport(currentDate, attendance, leave);
                        dailyReports.Add(dailyReport);

                        // تحديث الإحصائيات
                        UpdateSummaryStats(summaryStats, dailyReport);
                    }

                    currentDate = currentDate.AddDays(1);
                }

                var reportDto = new MonthlyReportDto
                {
                    Month = startDt,
                    DailyReports = dailyReports,
                    Summary = summaryStats
                };

                return Ok(new ApiResponse<MonthlyReportDto>
                {
                    Success = true,
                    Message = "تم تحميل التقرير الشهري بنجاح",
                    Data = reportDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<MonthlyReportDto>
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحميل التقرير",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        private DailyReportDto CreateDailyReport(DateTime date, Attendance attendance, Leave leave)
        {
            var report = new DailyReportDto
            {
                Date = date,
                DayOfWeek = GetArabicDayName(date.DayOfWeek),
                Status = "غائب", // القيمة الافتراضية
                CheckIn = null,
                CheckOut = null,
                LateMinutes = 0,
                EarlyLeaveMinutes = 0,
                OvertimeMinutes = 0,
                WorkHours = 0.0,
                Notes = ""
            };

            // التحقق من الإجازة
            if (leave != null)
            {
                report.Status = $"إجازة ({leave.LeaveType?.Name})";
                report.Notes = leave.Reason;
                return report;
            }

            // إذا لم يكن هناك حضور
            if (attendance == null || (!attendance.CheckInTime.HasValue && !attendance.CheckOutTime.HasValue))
            {
                report.Status = "غائب";
                return report;
            }

            // بيانات الحضور
            report.Status = "حاضر";
            report.CheckIn = attendance.CheckInTime?.ToString("HH:mm");
            report.CheckOut = attendance.CheckOutTime?.ToString("HH:mm");
            report.Notes = "";

            // حساب التأخير
            if (attendance.Late.HasValue)
            {
                report.LateMinutes = (int)attendance.Late.Value.TotalMinutes;
            }

            // حساب الخروج المبكر
            if (attendance.EarlyLeave.HasValue)
            {
                report.EarlyLeaveMinutes = (int)attendance.EarlyLeave.Value.TotalMinutes;
            }

            // حساب العمل الإضافي
            if (attendance.Overtime.HasValue)
            {
                report.OvertimeMinutes = (int)attendance.Overtime.Value.TotalMinutes;
            }

            // حساب ساعات العمل
            if (attendance.TotalWorkHours.HasValue)
            {
                report.WorkHours = attendance.TotalWorkHours.Value.TotalHours;
            }

            // إضافة ملاحظات خاصة
            if (attendance.CheckInLocation != null)
            {
                report.Notes += $" (من: {attendance.CheckInLocation})";
            }

            return report;
        }

        private string GetArabicDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Saturday => "السبت",
                DayOfWeek.Sunday => "الأحد",
                DayOfWeek.Monday => "الإثنين",
                DayOfWeek.Tuesday => "الثلاثاء",
                DayOfWeek.Wednesday => "الأربعاء",
                DayOfWeek.Thursday => "الخميس",
                DayOfWeek.Friday => "الجمعة",
                _ => ""
            };
        }

        private void UpdateSummaryStats(MonthlySummaryDto summary, DailyReportDto dailyReport)
        {
            summary.TotalDays++;

            switch (dailyReport.Status)
            {
                case string s when s.Contains("حاضر"):
                    summary.PresentDays++;
                    break;
                case string s when s.Contains("غائب"):
                    summary.AbsentDays++;
                    break;
                case string s when s.Contains("إجازة"):
                    summary.LeaveDays++;
                    break;
            }

            if (dailyReport.LateMinutes > 0)
            {
                summary.LateDays++;
                summary.TotalLateMinutes += dailyReport.LateMinutes;
            }

            if (dailyReport.EarlyLeaveMinutes > 0)
            {
                summary.EarlyLeaveDays++;
                summary.TotalEarlyLeaveMinutes += dailyReport.EarlyLeaveMinutes;
            }

            summary.TotalOvertimeMinutes += dailyReport.OvertimeMinutes;
            summary.TotalWorkHours += dailyReport.WorkHours;
        }
    }

    // DTOs للتقرير
    public class MonthlyReportDto
    {
        public DateTime Month { get; set; }
        public List<DailyReportDto> DailyReports { get; set; } = new List<DailyReportDto>();
        public MonthlySummaryDto Summary { get; set; } = new MonthlySummaryDto();
    }

    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = "";
        public string Status { get; set; } = "";
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public double WorkHours { get; set; }
        public string Notes { get; set; } = "";
    }

    public class MonthlySummaryDto
    {
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int LateDays { get; set; }
        public int EarlyLeaveDays { get; set; }
        public int TotalLateMinutes { get; set; }
        public int TotalEarlyLeaveMinutes { get; set; }
        public int TotalOvertimeMinutes { get; set; }
        public double TotalWorkHours { get; set; }
    }
}