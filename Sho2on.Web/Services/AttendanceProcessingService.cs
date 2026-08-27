using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class AttendanceProcessingService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public AttendanceProcessingService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// إعادة تعيين بصمات موظف: حذف البصمات القديمة من FingerPrints وسحبها من MachineData
        /// </summary>
        public async Task<(bool Success, string Message)> ResetAndPullScansFromMachineAsync(
            int userId, DateOnly startDate, DateOnly endDate)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .Include(u => u.Shift)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return (false, "الموظف غير موجود");

            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);

            // ═══ 1. حذف البصمات القديمة من FingerPrints ═══
            var oldScans = await db.FingerPrints
                .Where(f => f.UserId == userId && f.FingerPrintDate >= start && f.FingerPrintDate <= end)
                .ToListAsync();

            db.FingerPrints.RemoveRange(oldScans);

            // ═══ 2. حذف سجلات الحضور القديمة ═══
            var oldAttendances = await db.Attendances
                .Where(a => a.UserId == userId && a.AttendanceDate >= start && a.AttendanceDate <= end)
                .ToListAsync();

            db.Attendances.RemoveRange(oldAttendances);

            // ═══ 3. سحب البصمات من MachineData ═══
            var machineData = await db.MachineData
                .Where(m => m.UserID.ToString() == user.Code && m.TDate >= start && m.TDate <= end)
                .OrderBy(m => m.TDate)
                .ToListAsync();

            if (machineData.Count == 0)
            {
                await db.SaveChangesAsync();
                return (true, "تم حذف البصمات القديمة ولكن لا توجد بصمات جديدة في جهاز البصمة");
            }

            // ═══ 4. تحويل بيانات الجهاز إلى FingerPrints ═══
            int scansAdded = 0;
            foreach (var md in machineData)
            {
                // تجنب إضافة بصمات مكررة من نفس الثانية
                var exists = await db.FingerPrints.AnyAsync(f =>
                    f.UserId == userId &&
                    f.FingerPrintDate == md.TDate);

                if (exists) continue;

                var fingerPrint = new FingerPrint
                {
                    UserId = userId,
                    FingerPrintDate = md.TDate,
                    Status = md.Status == "حضور" ? 1 : 0, // 0 = حضور، 1 = انصراف (أو العكس حسب جهازك)
                    BranchId = user.BranchId,
                    IsManualEntry = false
                };

                db.FingerPrints.Add(fingerPrint);
                scansAdded++;
            }

            await db.SaveChangesAsync();

            // ═══ 5. معالجة البصمات الجديدة وتحويلها لحضور ═══
            var (pullSuccess, pullMessage) = await PullEmployeeScansFromFingerPrintsAsync(userId, startDate, endDate);

            return (true, $"تم حذف {oldScans.Count} بصمة قديمة وسحب {scansAdded} بصمة من جهاز البصمة. {pullMessage}");
        }

        /// <summary>
/// ✅ معالجة البصمات وحساب الحضور بشكل صحيح
/// </summary>
private void ProcessDayScans(Attendance attendance, List<FingerPrint> dayScans, Shift shift, DateOnly day)
{
    if (dayScans == null || dayScans.Count == 0) return;

    if (dayScans.Count == 1)
    {
        // ═══ ✅ بصمة واحدة: نحدد هل هي حضور ولا انصراف ═══
        var scan = dayScans[0];
        var scanTime = scan.FingerPrintDate.TimeOfDay;

        // ✅ نحسب المسافة الدائرية لبداية ونهاية الشيفت
        var distToStart = CircularDistance(scanTime, shift.StartTime);
        var distToEnd = CircularDistance(scanTime, shift.EndTime);

        if (distToStart <= distToEnd)
        {
            // ✅ الأقرب لبداية الشيفت => حضور
            attendance.CheckInTime = scan.FingerPrintDate;
            attendance.IsCheckInAutoFilled = false;
            
            // ✅ نكمل الانصراف تلقائياً حسب الشيفت
            FillMissingPunch(attendance, day, shift);
        }
        else
        {
            // ✅ الأقرب لنهاية الشيفت => انصراف
            attendance.CheckOutTime = scan.FingerPrintDate;
            attendance.IsCheckOutAutoFilled = false;
            
            // ✅ نكمل الحضور تلقائياً حسب الشيفت
            FillMissingPunch(attendance, day, shift);
        }
    }
    else
    {
        // ═══ ✅ بصمتين أو أكثر: أول بصمة حضور، آخر بصمة انصراف ═══
        attendance.CheckInTime = dayScans.First().FingerPrintDate;
        attendance.CheckOutTime = dayScans.Last().FingerPrintDate;
    }

    // ✅ لو لسه في نقص، نكمله
    if (attendance.CheckInTime == null || attendance.CheckOutTime == null)
    {
        FillMissingPunch(attendance, day, shift);
    }
}

/// <summary>
/// ✅ حساب التأخير/الحضور المبكر/الانصراف المبكر/الإضافي/إجمالي ساعات العمل
/// ✅ الأساس دائمًا هو "يوم الشيفت المنطقي" (day) مش تاريخ بصمة الحضور نفسها
///    لأن في شيفتات زي (12ص - 12م) ممكن الموظف يبصم قبل منتصف الليل بشوية
///    فتاريخ البصمة يبقى "أمس" رغم إنها بصمة حضور ليوم الشيفت "النهارده"
/// </summary>
private const int MaxDiffHours = 12; // أي فرق أكبر من كده يعتبر بيانات غير منطقية ويتجاهل

private void CalculateTimes(Attendance attendance, DateTime checkIn, DateTime checkOut, Shift shift, DateOnly day)
{
    // ═══ إعادة تصفير القيم قبل الحساب (عشان الدالة تبقى صالحة لإعادة الحساب كمان) ═══
    attendance.Late = null;
    attendance.EarlyEnter = null;
    attendance.EarlyLeave = null;
    attendance.Overtime = null;

    // ═══ ✅ حساب بداية ونهاية الشيفت المتوقعة بالاعتماد على يوم الشيفت الصحيح ═══
    var shiftDate = day.ToDateTime(TimeOnly.MinValue);
    var expectedStart = shiftDate.Add(shift.StartTime);
    var expectedEnd = shiftDate.Add(shift.EndTime);

    // ✅ لو الشيفت عابر لمنتصف الليل (مثلاً 8 م - 6 ص) => نهاية الشيفت في اليوم التالي
    if (shift.EndTime < shift.StartTime)
    {
        expectedEnd = expectedEnd.AddDays(1);
    }

    // ═══ ✅ التأخير / الحضور المبكر ═══
    if (checkIn > expectedStart)
    {
        var late = checkIn - expectedStart;
        if (late.TotalHours <= MaxDiffHours)
            attendance.Late = ClampTimeSpan(late);
    }
    else if (checkIn < expectedStart)
    {
        var earlyEnter = expectedStart - checkIn;
        if (earlyEnter.TotalHours <= MaxDiffHours)
            attendance.EarlyEnter = ClampTimeSpan(earlyEnter);
    }

    // ═══ ✅ الانصراف المبكر / الإضافي ═══
    if (checkOut < expectedEnd)
    {
        var earlyLeave = expectedEnd - checkOut;
        if (earlyLeave.TotalHours <= MaxDiffHours)
            attendance.EarlyLeave = ClampTimeSpan(earlyLeave);
    }
    else if (checkOut > expectedEnd)
    {
        var overtime = checkOut - expectedEnd;
        if (overtime.TotalHours <= MaxDiffHours)
            attendance.Overtime = ClampTimeSpan(overtime);
    }

    // ═══ ✅ إجمالي ساعات العمل الفعلية (فرق حقيقي بين تاريخين كاملين، مفيش لبس هنا) ═══
    var totalWork = checkOut - checkIn;
    attendance.TotalWorkHours = totalWork > TimeSpan.Zero ? ClampTimeSpan(totalWork) : TimeSpan.Zero;
}

/// <summary>
/// ✅ تكملة البصمة الناقصة
/// لو في حضور بس => نكمل الانصراف بميعاد نهاية الشيفت
/// لو في انصراف بس => نكمل الحضور بميعاد بداية الشيفت
/// </summary>
private bool FillMissingPunch(Attendance attendance, DateOnly day, Shift shift)
{
    // ✅ لو الاتنين موجودين - مفيش حاجة نعملها
    if (attendance.CheckInTime != null && attendance.CheckOutTime != null)
        return false;

    // ✅ لو مفيش ولا بصمة - مفيش حاجة نكملها
    if (attendance.CheckInTime == null && attendance.CheckOutTime == null)
        return false;

    // ═══ ✅ لو في حضور بس - نكمل الانصراف ═══
    if (attendance.CheckInTime != null && attendance.CheckOutTime == null)
    {
        var checkOutTime = day.ToDateTime(TimeOnly.MinValue).Add(shift.EndTime);

        // ✅ لو الشيفت عابر لمنتصف الليل
        if (shift.EndTime < shift.StartTime)
        {
            checkOutTime = checkOutTime.AddDays(1);
        }

        attendance.CheckOutTime = checkOutTime;
        attendance.IsCheckOutAutoFilled = true;
        return true;
    }

    // ═══ ✅ لو في انصراف بس - نكمل الحضور ═══
    if (attendance.CheckInTime == null && attendance.CheckOutTime != null)
    {
        var checkInTime = day.ToDateTime(TimeOnly.MinValue).Add(shift.StartTime);

        attendance.CheckInTime = checkInTime;
        attendance.IsCheckInAutoFilled = true;
        return true;
    }

    return false;
}

/// <summary>
/// ✅ تحديد TimeSpan ليكون بين 0 و 23:59:59
/// </summary>
private TimeSpan ClampTimeSpan(TimeSpan value)
{
    if (value < TimeSpan.Zero)
        return TimeSpan.Zero;

    if (value.TotalHours >= 24)
        return TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(59);

    return value;
}

/// <summary>
/// ✅ المسافة الدائرية بين وقتين (لحساب الأقرب)
/// </summary>
private TimeSpan CircularDistance(TimeSpan a, TimeSpan b)
{
    var diff = (a - b).Duration();
    var wrap = TimeSpan.FromHours(24) - diff;
    return diff < wrap ? diff : wrap;
}

        /// <summary>
        /// سحب بصمات موظف من FingerPrints إلى Attendances (مع إزالة المكررة)
        /// </summary>
        private async Task<(bool Success, string Message)> PullEmployeeScansFromFingerPrintsAsync(
            int userId, DateOnly startDate, DateOnly endDate)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .FirstOrDefaultAsync(u => u.Id == userId);
            var officialHolidays = await GetOfficialHolidayDatesAsync(db, startDate, endDate); // ← جديد
            if (user == null || user.Shift == null)
                return (false, "الموظف أو الوردية غير موجودة");

            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);

            // جلب البصمات
            var scans = await db.FingerPrints
                .Where(f => f.UserId == userId && f.FingerPrintDate >= start && f.FingerPrintDate <= end)
                .OrderBy(f => f.FingerPrintDate)
                .ToListAsync();

            if (scans.Count == 0)
                return (false, "لا توجد بصمات للمعالجة");

            // إزالة المكررة
            var deduplicatedScans = RemoveDuplicateScans(scans);

            var scansByDay = GroupScansByWorkDay(deduplicatedScans, user.Shift);

            var weekHoliDays = GetWeekHolidayFlags(user.WeekHoliday);

            int daysProcessed = 0;

            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                var dayIndex = (int)day.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                bool isHoliday = weekHoliDays[dayIndex] || officialHolidays.Contains(day);
                scansByDay.TryGetValue(day, out var dayScans);

                // حذف سجل الحضور القديم لنفس اليوم
                var existingAtt = await db.Attendances
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == day.ToDateTime(TimeOnly.MinValue));
                if (existingAtt != null)
                    db.Attendances.Remove(existingAtt);

                var attendance = new Attendance
                {
                    UserId = userId,
                    AttendanceDate = day.ToDateTime(TimeOnly.MinValue),
                    ShiftId = user.ShiftId,
                    CheckInBranchId = user.BranchId,
                    CheckOutBranchId = user.BranchId,
                    IsHoliday = isHoliday
                };

                if (dayScans != null && dayScans.Count > 0)
                {
                    if (dayScans.Count == 1)
                    {
                        var scan = dayScans[0];
                        var distToIn = CircularDistance(scan.FingerPrintDate.TimeOfDay, user.Shift.StartTime);
                        var distToOut = CircularDistance(scan.FingerPrintDate.TimeOfDay, user.Shift.EndTime);

                        if (distToIn <= distToOut)
                            attendance.CheckInTime = scan.FingerPrintDate;
                        else
                            attendance.CheckOutTime = scan.FingerPrintDate;
                    }
                    else
                    {
                        attendance.CheckInTime = dayScans.First().FingerPrintDate;
                        attendance.CheckOutTime = dayScans.Last().FingerPrintDate;
                    }

                }

                bool isAbsence = attendance.CheckInTime == null && attendance.CheckOutTime == null && !isHoliday;

                if (!isAbsence)
                    FillMissingPunch(attendance, day, user.Shift);

                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                    CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, user.Shift, day);

                if (attendance.CheckInTime == null && attendance.CheckOutTime == null && !isHoliday)
                    attendance.IsAbsence = true;

                db.Attendances.Add(attendance);
                daysProcessed++;
            }

            await db.SaveChangesAsync();

            return (true, $"تمت معالجة {daysProcessed} يوم بنجاح (من أصل {deduplicatedScans.Count} بصمة)");
        }

        /// <summary>
        /// سحب بصمات مجموعة من الموظفين في فترة محددة
        /// </summary>
        public async Task<AttendanceProcessingResult> PullEmployeesScansAsync(
            int? branchId, int? userId, DateOnly startDate, DateOnly endDate)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var result = new AttendanceProcessingResult();
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);

            var usersQuery = db.Users
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .Where(u => !u.IsArchived);
            var officialHolidays = await GetOfficialHolidayDatesAsync(db, startDate, endDate);

            if (userId.HasValue)
                usersQuery = usersQuery.Where(u => u.Id == userId.Value);
            else if (branchId.HasValue)
                usersQuery = usersQuery.Where(u => u.BranchId == branchId.Value);

            var users = await usersQuery.ToListAsync();

            foreach (var user in users)
            {
                if (user.Shift == null) continue;

                // جلب البصمات من FingerPrints
                var scans = await db.FingerPrints
                    .Where(f => f.UserId == user.Id &&
                               f.FingerPrintDate >= start &&
                               f.FingerPrintDate <= end)
                    .OrderBy(f => f.FingerPrintDate)
                    .ToListAsync();

                var scansByDay = GroupScansByWorkDay(scans, user.Shift);

                // حذف سجلات الحضور القديمة
                var existingAttendances = await db.Attendances
                    .Where(a => a.UserId == user.Id &&
                               a.AttendanceDate >= start &&
                               a.AttendanceDate <= end)
                    .ToListAsync();
                db.Attendances.RemoveRange(existingAttendances);

                var weekHoliDays = GetWeekHolidayFlags(user.WeekHoliday);

                for (var day = startDate; day <= endDate; day = day.AddDays(1))
                {
                    var dayIndex = (int)day.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                    bool isHoliday = weekHoliDays[dayIndex] || officialHolidays.Contains(day);
                    scansByDay.TryGetValue(day, out var dayScans);

                    var attendance = new Attendance
                    {
                        UserId = user.Id,
                        AttendanceDate = day.ToDateTime(TimeOnly.MinValue),
                        ShiftId = user.ShiftId,
                        CheckInBranchId = user.BranchId,
                        CheckOutBranchId = user.BranchId,
                        IsHoliday = isHoliday
                    };

                    if (dayScans != null && dayScans.Count > 0)
                    {
                        if (dayScans.Count == 1)
                        {
                            var scan = dayScans[0];
                            var distToIn = CircularDistance(scan.FingerPrintDate.TimeOfDay, user.Shift.StartTime);
                            var distToOut = CircularDistance(scan.FingerPrintDate.TimeOfDay, user.Shift.EndTime);

                            if (distToIn <= distToOut)
                                attendance.CheckInTime = scan.FingerPrintDate;
                            else
                                attendance.CheckOutTime = scan.FingerPrintDate;
                        }
                        else
                        {
                            attendance.CheckInTime = dayScans.First().FingerPrintDate;
                            attendance.CheckOutTime = dayScans.Last().FingerPrintDate;
                        }

                    }

                    bool isAbsence = attendance.CheckInTime == null && attendance.CheckOutTime == null && !isHoliday;
                    attendance.IsAbsence = isAbsence;

                    if (!isAbsence)
                    {
                        bool wasFilled = FillMissingPunch(attendance, day, user.Shift);
                        if (wasFilled) result.MissingPunchesAutoFilled++;
                    }

                    if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                        CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, user.Shift, day);
                    db.Attendances.Add(attendance);
                    result.DaysAutoResolved++;
                }

                result.EmployeesProcessed++;
            }

            await db.SaveChangesAsync();
            return result;
        }

     
        // دوال مساعدة
        private bool IsWeeklyRestDay(DayOfWeek day, WeekHoliday? wh)
        {
            if (wh == null) return false;
            return day switch
            {
                DayOfWeek.Sunday => wh.Day2,
                DayOfWeek.Monday => wh.Day3,
                DayOfWeek.Tuesday => wh.Day4,
                DayOfWeek.Wednesday => wh.Day5,
                DayOfWeek.Thursday => wh.Day6,
                DayOfWeek.Friday => wh.Day7,
                DayOfWeek.Saturday => wh.Day1,
                _ => false
            };
        }

        /// <summary>
        /// تطبيع تلقائي كامل: إزالة التكرار + فرض نمط التبديل (حضور/انصراف) + حسم يوم البصمة الواحدة
        /// بيتنفذ قبل أي حساب، ويحدّث Status الحقيقي في FingerPrints عشان الجدول يعرضها صح من غير تدخل يدوي
        /// </summary>
        private async Task<(List<FingerPrint> CleanScans, int DuplicatesRemoved, int StatusesCorrected, int SingleScanResolved)>
            NormalizeUserScansAsync(AppDbContext db, List<FingerPrint> allScans, Shift shift)
        {
            int duplicatesRemoved = 0;
            int statusesCorrected = 0;
            int singleScanResolved = 0;

            // ═══ 1. إزالة التكرار (نفس اللوجيك الموجود، بس معزول هنا) ═══
            var deduped = RemoveDuplicateScans(allScans);
            duplicatesRemoved = allScans.Count - deduped.Count;

            // ═══ 2. تجميع حسب اليوم وفرض النمط ═══
            var byDay = GroupScansByWorkDay(deduped, shift);

            foreach (var (day, dayScans) in byDay)
            {
                if (dayScans.Count == 1)
                {
                    // بصمة واحدة: قرار نهائي بالأقرب لوقت الوردية، وبنكتبه في Status نفسه
                    var scan = dayScans[0];
                    var distToIn = CircularDistance(scan.FingerPrintDate.TimeOfDay, shift.StartTime);
                    var distToOut = CircularDistance(scan.FingerPrintDate.TimeOfDay, shift.EndTime);
                    bool shouldBeCheckIn = distToIn <= distToOut;

                    int expectedStatus = shouldBeCheckIn ? 1 : 0;
                    if (scan.Status != expectedStatus)
                    {
                        scan.Status = expectedStatus;
                        statusesCorrected++;
                    }
                    singleScanResolved++;
                }
                else
                {
                    // بصمتين أو أكتر: فرض التبديل بالترتيب الزمني (فردي = حضور، زوجي = انصراف)
                    for (int i = 0; i < dayScans.Count; i++)
                    {
                        int expectedStatus = (i % 2 == 0) ? 1 : 0; // أول واحدة حضور، اللي بعدها انصراف...
                        if (dayScans[i].Status != expectedStatus)
                        {
                            dayScans[i].Status = expectedStatus;
                            statusesCorrected++;
                        }
                    }
                }
            }

            return (deduped, duplicatesRemoved, statusesCorrected, singleScanResolved);
        }

        /// <summary>
/// ✅ المعالجة: تحديد CheckIn و CheckOut من البصمات
/// ✅ الحساب: يتم بعدين على مستوى الـ Attendance
/// </summary>
public async Task<AttendanceProcessingResult> ProcessAsync(int? branchId, int? userId, DateOnly startDate, DateOnly endDate)
{
    using var db = await _dbFactory.CreateDbContextAsync();
    var result = new AttendanceProcessingResult();
    var start = startDate.ToDateTime(TimeOnly.MinValue);
    var end = endDate.ToDateTime(TimeOnly.MaxValue);

    var usersQuery = db.Users.Include(u => u.Shift).Include(u => u.WeekHoliday).Where(u => !u.IsArchived);
    if (userId.HasValue) usersQuery = usersQuery.Where(u => u.Id == userId.Value);
    else if (branchId.HasValue) usersQuery = usersQuery.Where(u => u.BranchId == branchId.Value);

    var users = await usersQuery.ToListAsync();
    var officialHolidays = await GetOfficialHolidayDatesAsync(db, startDate, endDate);

    foreach (var user in users)
    {
        if (user.Shift == null) continue;

        var allScans = await db.FingerPrints
            .Where(f => f.UserId == user.Id && f.FingerPrintDate >= start && f.FingerPrintDate <= end)
            .OrderBy(f => f.FingerPrintDate)
            .ToListAsync();

        if (allScans.Count == 0) continue;

        // ═══ 1. التطبيع: إزالة التكرار + فرض النمط ═══
        var (cleanScans, dupCount, correctedCount, singleCount) = await NormalizeUserScansAsync(db, allScans, user.Shift);

        var duplicates = allScans.Except(cleanScans).ToList();
        if (duplicates.Count > 0) db.FingerPrints.RemoveRange(duplicates);

        result.DuplicateScansRemoved += dupCount;
        result.StatusesAutoCorrected += correctedCount;
        result.SingleScanDaysAutoResolved += singleCount;

        // ═══ 2. تجميع البصمات حسب يوم العمل ═══
        var scansByDay = GroupScansByWorkDay(cleanScans, user.Shift);

        // ═══ 3. حذف الحضور القديم ═══
        var existingAttendances = await db.Attendances
            .Where(a => a.UserId == user.Id && a.AttendanceDate >= start && a.AttendanceDate <= end)
            .ToListAsync();
        if (existingAttendances.Count > 0) db.Attendances.RemoveRange(existingAttendances);

        var weekHoliDays = GetWeekHolidayFlags(user.WeekHoliday);

        // ═══ 4. إنشاء سجلات الحضور (بدون حساب التأخير والإضافي) ═══
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var dayIndex = (int)day.ToDateTime(TimeOnly.MinValue).DayOfWeek;
            bool isHoliday = weekHoliDays[dayIndex] || officialHolidays.Contains(day);
            scansByDay.TryGetValue(day, out var dayScans);

            var attendance = new Attendance
            {
                UserId = user.Id,
                AttendanceDate = day.ToDateTime(TimeOnly.MinValue),
                CheckInBranchId = user.BranchId,
                CheckOutBranchId = user.BranchId,
                IsHoliday = isHoliday,
                ShiftId = user.ShiftId
            };

            // ✅ تحديد CheckIn و CheckOut فقط (بدون حساب)
            DetermineCheckInAndCheckOut(attendance, dayScans, day, user.Shift);

            // ✅ تحديد الغياب
            attendance.IsAbsence = attendance.CheckInTime == null && 
                                   attendance.CheckOutTime == null && 
                                   !isHoliday;

            db.Attendances.Add(attendance);

            if (dayScans != null && dayScans.Count > 0) 
                result.DaysAutoResolved++;
        }

        result.EmployeesProcessed++;
    }

    await db.SaveChangesAsync();

    // ═══ 5. ✅ حساب التأخير والإضافي بعد الحفظ ═══
    await RecalculateAllTimesAsync(db, start, end, users.Select(u => u.Id).ToList());

    return result;
}

/// <summary>
/// ✅ تحديد CheckIn و CheckOut فقط (بدون حساب التأخير والإضافي)
/// </summary>
private void DetermineCheckInAndCheckOut(Attendance attendance, List<FingerPrint>? dayScans, DateOnly day, Shift shift)
{
    if (dayScans == null || dayScans.Count == 0) return;

    if (dayScans.Count == 1)
    {
        // ✅ بصمة واحدة: نحدد الأقرب (حضور ولا انصراف)
        var scan = dayScans[0];
        var scanTime = scan.FingerPrintDate.TimeOfDay;

        var distToStart = CircularDistance(scanTime, shift.StartTime);
        var distToEnd = CircularDistance(scanTime, shift.EndTime);

        if (distToStart <= distToEnd)
        {
            // ✅ الأقرب لبداية الشيفت => حضور
            attendance.CheckInTime = scan.FingerPrintDate;
        }
        else
        {
            // ✅ الأقرب لنهاية الشيفت => انصراف
            attendance.CheckOutTime = scan.FingerPrintDate;
        }
    }
    else
    {
        // ✅ بصمتين أو أكثر: أول بصمة حضور، آخر بصمة انصراف
        attendance.CheckInTime = dayScans.First().FingerPrintDate;
        attendance.CheckOutTime = dayScans.Last().FingerPrintDate;
    }

    // ✅ تكملة الناقص
    FillMissingPunch(attendance, day, shift);
}

/// <summary>
/// ✅ إعادة حساب التأخير والإضافي لكل سجلات الحضور
/// دي بتتنفذ بعد المعالجة على مستوى الـ Attendance
/// </summary>
private async Task RecalculateAllTimesAsync(AppDbContext db, DateTime start, DateTime end, List<int> userIds)
{
    var attendances = await db.Attendances
        .Where(a => userIds.Contains(a.UserId) &&
                    a.AttendanceDate >= start &&
                    a.AttendanceDate <= end)
        .ToListAsync();

    var shifts = await db.Shifts.ToDictionaryAsync(s => s.Id);

    foreach (var attendance in attendances)
    {
        attendance.Late = null;
        attendance.EarlyEnter = null;
        attendance.Overtime = null;
        attendance.EarlyLeave = null;
        attendance.TotalWorkHours = null;

        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            var shift = shifts.GetValueOrDefault(attendance.ShiftId ?? 0);
            if (shift != null)
            {
                CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value,
                    shift, DateOnly.FromDateTime(attendance.AttendanceDate));
            }
        }
    }

    await db.SaveChangesAsync();
}

/// <summary>
/// ✅ حساب التأخير والإضافي من بيانات الـ Attendance
/// </summary>
/// <summary>
/// ✅ حساب التأخير والإضافي بشكل صحيح مع دعم كل أنواع الشيفتات
/// </summary>
private void CalculateTimesFromAttendance(Attendance attendance, DateTime checkIn, DateTime checkOut, Shift shift)
{
    // ═══ ✅ 1. تحديد "يوم الشيفت" الصحيح ═══
    // ✅ نستخدم وقت الحضور كمرجع أساسي لتحديد يوم الشيفت
    var checkInTime = checkIn.TimeOfDay;
    var checkOutTime = checkOut.TimeOfDay;

    // ═══ ✅ 2. حساب بداية ونهاية الشيفت كـ TimeSpan ═══
    var shiftStart = shift.StartTime;
    var shiftEnd = shift.EndTime;

    // ═══ ✅ 3. حساب التأخير والإضافي باستخدام Circular Distance ═══
    
    // ✅ التأخير: كم اتأخر عن بداية الشيفت
    var lateMinutes = CalculateLate(checkInTime, shiftStart);
    if (lateMinutes > 0 && lateMinutes <= 12 * 60) // ✅ أقصى تأخير 12 ساعة
    {
        attendance.Late = TimeSpan.FromMinutes(lateMinutes);
    }
    else if (lateMinutes > 0)
    {
        // ✅ لو "التأخير" أكتر من 12 ساعة، ده معناه إن البصمة قريبة من نهاية الشيفت
        // ✅ يعني ممكن تكون انصراف مش حضور - لكن المعالجة المفروض تكون حددت صح
        attendance.Late = null;
    }

    // ✅ الحضور المبكر: كم جه بدري عن بداية الشيفت
    var earlyEnterMinutes = CalculateEarlyEnter(checkInTime, shiftStart);
    if (earlyEnterMinutes > 0 && earlyEnterMinutes <= 12 * 60)
    {
        attendance.EarlyEnter = TimeSpan.FromMinutes(earlyEnterMinutes);
    }

    // ✅ الانصراف المبكر: كم انصرف بدري عن نهاية الشيفت
    var earlyLeaveMinutes = CalculateEarlyLeave(checkOutTime, shiftEnd);
    if (earlyLeaveMinutes > 0 && earlyLeaveMinutes <= 12 * 60)
    {
        attendance.EarlyLeave = TimeSpan.FromMinutes(earlyLeaveMinutes);
    }

    // ✅ الإضافي: كم اشتغل زيادة عن نهاية الشيفت
    var overtimeMinutes = CalculateOvertime(checkOutTime, shiftEnd);
    if (overtimeMinutes > 0 && overtimeMinutes <= 12 * 60) // ✅ أقصى إضافي 12 ساعة
    {
        attendance.Overtime = TimeSpan.FromMinutes(overtimeMinutes);
    }

    // ═══ ✅ 4. حساب ساعات العمل الفعلية ═══
    var workMinutes = CalculateWorkMinutes(checkIn, checkOut);
    if (workMinutes > 0 && workMinutes <= 24 * 60)
    {
        attendance.TotalWorkHours = TimeSpan.FromMinutes(workMinutes);
    }
}

/// <summary>
/// ✅ حساب التأخير بالدقائق
/// لو الحضور بعد بداية الشيفت => تأخير
/// </summary>
private int CalculateLate(TimeSpan checkIn, TimeSpan shiftStart)
{
    // ✅ الفرق المباشر
    var diff = (checkIn - shiftStart).TotalMinutes;
    
    // ✅ لو الفرق موجب => اتأخر
    if (diff > 0 && diff <= 12 * 60) // ✅ أقصى 12 ساعة
        return (int)diff;
    
    // ✅ لو الفرق سالب => جه بدري (مش تأخير)
    return 0;
}

/// <summary>
/// ✅ حساب الحضور المبكر بالدقائق
/// لو الحضور قبل بداية الشيفت => حضور مبكر
/// </summary>
private int CalculateEarlyEnter(TimeSpan checkIn, TimeSpan shiftStart)
{
    var diff = (shiftStart - checkIn).TotalMinutes;
    
    if (diff > 0 && diff <= 12 * 60)
        return (int)diff;
    
    return 0;
}

/// <summary>
/// ✅ حساب الانصراف المبكر بالدقائق
/// لو الانصراف قبل نهاية الشيفت => انصراف مبكر
/// </summary>
private int CalculateEarlyLeave(TimeSpan checkOut, TimeSpan shiftEnd)
{
    var diff = (shiftEnd - checkOut).TotalMinutes;
    
    if (diff > 0 && diff <= 12 * 60)
        return (int)diff;
    
    return 0;
}

/// <summary>
/// ✅ حساب الإضافي بالدقائق
/// لو الانصراف بعد نهاية الشيفت => إضافي
/// </summary>
private int CalculateOvertime(TimeSpan checkOut, TimeSpan shiftEnd)
{
    var diff = (checkOut - shiftEnd).TotalMinutes;
    
    if (diff > 0 && diff <= 12 * 60)
        return (int)diff;
    
    return 0;
}

/// <summary>
/// ✅ حساب ساعات العمل الفعلية
/// </summary>
private int CalculateWorkMinutes(DateTime checkIn, DateTime checkOut)
{
    var diff = (checkOut - checkIn).TotalMinutes;
    
    if (diff < 0) // ✅ لو الانصراف قبل الحضور (غالباً خطأ)
        diff += 24 * 60; // ✅ نضيف 24 ساعة
    
    if (diff > 0 && diff <= 24 * 60)
        return (int)diff;
    
    return 0;
}

        /// <summary>
        /// إزالة البصمات المكررة (اللي بينها أقل من 30 ثانية)
        /// </summary>
        private List<FingerPrint> RemoveDuplicateScans(List<FingerPrint> scans)
        {
            if (scans.Count <= 1) return scans;

            var result = new List<FingerPrint> { scans[0] };

            for (int i = 1; i < scans.Count; i++)
            {
                var timeDiff = (scans[i].FingerPrintDate - scans[i - 1].FingerPrintDate).Duration();

                // لو الفرق أقل من 30 ثانية، تجاهل البصمة
                if (timeDiff.TotalMinutes < 15)
                {
                    // نحتفظ بالبصمة الأحدث
                    result[result.Count - 1] = scans[i];
                    continue;
                }

                result.Add(scans[i]);
            }

            return result;
        }

        /// <summary>
        /// سحب بصمات موظف مع إزالة المكررة
        /// </summary>
        public async Task<(bool Success, string Message)> PullEmployeeScansAsync(
    int userId, DateOnly date, int? specificShiftId = null)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Users
                .Include(u => u.Shift)
                .Include(u => u.WeekHoliday)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return (false, "الموظف غير موجود");

            var shift = specificShiftId.HasValue
                ? await db.Shifts.FindAsync(specificShiftId.Value)
                : user.Shift;

            if (shift == null) return (false, "لا توجد وردية محددة للموظف");

            // نوسّع نطاق البحث يوم قبل ويوم بعد عشان نلقط بصمات الوردية العابرة لمنتصف الليل
            var searchStart = date.AddDays(-1).ToDateTime(TimeOnly.MinValue);
            var searchEnd = date.AddDays(1).ToDateTime(TimeOnly.MaxValue);

            var candidateScans = await db.FingerPrints
                .Where(f => f.UserId == userId && f.FingerPrintDate >= searchStart && f.FingerPrintDate <= searchEnd)
                .OrderBy(f => f.FingerPrintDate)
                .ToListAsync();

            // فلترة اللي فعلاً بيخص يوم الوردية المطلوب
            var scans = candidateScans
                .Where(f => GetShiftLogicalDay(f.FingerPrintDate, shift) == date)
                .OrderBy(f => f.FingerPrintDate)
                .ToList();

            if (scans.Count == 0) return (false, "لا توجد بصمات لهذا الموظف في هذا اليوم");

            var deduplicatedScans = RemoveDuplicateScans(scans);

            var existingAttendance = await db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == date.ToDateTime(TimeOnly.MinValue));
            if (existingAttendance != null)
                db.Attendances.Remove(existingAttendance);
            var officialHolidays = await GetOfficialHolidayDatesAsync(db, date, date);
            var attendance = new Attendance
            {
                UserId = userId,
                AttendanceDate = date.ToDateTime(TimeOnly.MinValue),
                ShiftId = shift.Id,
                CheckInBranchId = user.BranchId,
                CheckOutBranchId = user.BranchId,
                IsHoliday = IsWeeklyRestDay(date.DayOfWeek, user.WeekHoliday) || officialHolidays.Contains(date)
            };

            if (deduplicatedScans.Count == 1)
            {
                var scan = deduplicatedScans[0];
                var distToIn = CircularDistance(scan.FingerPrintDate.TimeOfDay, shift.StartTime);
                var distToOut = CircularDistance(scan.FingerPrintDate.TimeOfDay, shift.EndTime);

                if (distToIn <= distToOut)
                    attendance.CheckInTime = scan.FingerPrintDate;
                else
                    attendance.CheckOutTime = scan.FingerPrintDate;
            }
            else
            {
                attendance.CheckInTime = deduplicatedScans.First().FingerPrintDate;
                attendance.CheckOutTime = deduplicatedScans.Last().FingerPrintDate;
            }

            FillMissingPunch(attendance, date, shift);

            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, shift, date);
            else
                attendance.IsAbsence = !attendance.IsHoliday;

            db.Attendances.Add(attendance);
            await db.SaveChangesAsync();

            return (true, $"تم سحب {deduplicatedScans.Count} بصمة (تم تجاهل {scans.Count - deduplicatedScans.Count} بصمة مكررة)");
        }

        // تعديل يدوي سريع لسجل واحد من قائمة المراجعة (تأكيد أو تصحيح نوع البصمة)
        public async Task ResolveAsync(int attendanceId, bool isCheckIn, bool isCheckOut)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var att = await _db.Attendances.FindAsync(attendanceId) ?? throw new Exception("السجل غير موجود");
            var shift = await _db.Shifts.FindAsync(att.ShiftId);
            var scanTime = att.CheckInTime ?? att.CheckOutTime;

            if (scanTime == null || shift == null) return;

            att.CheckInTime = isCheckIn ? scanTime : null;
            att.CheckOutTime = isCheckOut ? scanTime : null;

            if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                CalculateTimes(att, att.CheckInTime.Value, att.CheckOutTime.Value, shift,
                    DateOnly.FromDateTime(att.AttendanceDate));

            await _db.SaveChangesAsync();
        }

        private bool[] GetWeekHolidayFlags(WeekHoliday? wh)
        {
            // ترتيب DayOfWeek في C#: الأحد=0 ... السبت=6
            if (wh == null) return new bool[7];
            return new[] { wh.Day2, wh.Day3, wh.Day4, wh.Day5, wh.Day6, wh.Day7, wh.Day1 };
        }

        public async Task<List<RawScanItem>> GetRawScansAsync(int? branchId, DateOnly startDate, DateOnly endDate)
        {
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);
            using var _db = await _dbFactory.CreateDbContextAsync();

            var query = _db.FingerPrints
                .Include(f => f.User)
                .Where(f => f.FingerPrintDate >= start && f.FingerPrintDate <= end);

            if (branchId.HasValue)
                query = query.Where(f => f.User!.BranchId == branchId.Value);

            return await query
                .OrderByDescending(f => f.FingerPrintDate)
                .Select(f => new RawScanItem
                {
                    Id = f.Id,
                    EmployeeCode = f.User!.Code,
                    EmployeeName = f.User.FullName,
                    ScanTime = f.FingerPrintDate,
                    IsCheckIn = f.Status == 1
                })
                .ToListAsync();
        }

        public async Task<PagedResult<RawScanItem>> GetRawScansPagedAsync(
    int? branchId, int? userId, DateOnly startDate, DateOnly endDate, int page, int pageSize)
        {
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);
            using var _db = await _dbFactory.CreateDbContextAsync();

            var query = _db.FingerPrints
                .Include(f => f.User)
                .Where(f => f.FingerPrintDate >= start && f.FingerPrintDate <= end);

            if (userId.HasValue)
                query = query.Where(f => f.UserId == userId.Value);
            else if (branchId.HasValue)
                query = query.Where(f => f.User!.BranchId == branchId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(f => f.FingerPrintDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new RawScanItem
                {
                    Id = f.Id,
                    EmployeeCode = f.User!.Code,
                    EmployeeName = f.User.FullName,
                    ScanTime = f.FingerPrintDate,
                    IsCheckIn = f.Status == 1,
                    IsManualEntry = f.IsManualEntry ?? false
                })
                .ToListAsync();

            return new PagedResult<RawScanItem> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task AddManualScanAsync(int userId, DateTime scanTime, bool isCheckIn, string addedBy)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return;
            _db.FingerPrints.Add(new FingerPrint
            {
                UserId = userId,
                FingerPrintDate = scanTime,
                Status = isCheckIn ? 1 : 0,
                IsManualEntry = true,
                AddedByUsername = addedBy,
                BranchId = user.BranchId
            });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateScanAsync(int fingerPrintId, DateTime newTime, bool isCheckIn)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var scan = await _db.FingerPrints.FindAsync(fingerPrintId) ?? throw new Exception("البصمة غير موجودة");
            scan.FingerPrintDate = newTime;
            scan.Status = isCheckIn ? 1 : 0;
            scan.IsManualEntry = true;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteScanAsync(int fingerPrintId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var scan = await _db.FingerPrints.FindAsync(fingerPrintId) ?? throw new Exception("البصمة غير موجودة");
            _db.FingerPrints.Remove(scan); 
            await _db.SaveChangesAsync();
        }

        public async Task<(int StartDay, int EndDay)> GetMonthSettingsAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.Settings.FirstOrDefaultAsync();
            return (settings?.StartOfMonth ?? 26, settings?.EndOfMonth ?? 25);
        }

       

        /// <summary>
        /// يرجّع كل تواريخ العطلات الرسمية (موسّعة من Date لحد EndDate) في نطاق زمني معين، كـ HashSet للبحث السريع
        /// </summary>
        private async Task<HashSet<DateOnly>> GetOfficialHolidayDatesAsync(AppDbContext db, DateOnly startDate, DateOnly endDate)
        {
            var start = startDate.ToDateTime(TimeOnly.MinValue);
            var end = endDate.ToDateTime(TimeOnly.MaxValue);

            var holidays = await db.OfficialHolidays
                .Where(h => h.Date <= end && (h.EndDate ?? h.Date) >= start)
                .ToListAsync();

            var dates = new HashSet<DateOnly>();
            foreach (var h in holidays)
            {
                var from = DateOnly.FromDateTime(h.Date);
                var to = DateOnly.FromDateTime(h.EndDate ?? h.Date);
                for (var d = from; d <= to; d = d.AddDays(1))
                    dates.Add(d);
            }
            return dates;
        }

        private Dictionary<DateOnly, List<FingerPrint>> GroupScansByWorkDay(List<FingerPrint> scans, Shift shift)
{
    var sorted = scans.OrderBy(f => f.FingerPrintDate).ToList();
    bool shiftCrossesMidnight = shift.EndTime < shift.StartTime;

    Dictionary<DateOnly, List<FingerPrint>> byDay;

    // ═══ ✅ حالة: شيفت بيبدأ منتصف الليل (12:00 AM) ═══
    bool startsAtMidnight = shift.StartTime == TimeSpan.Zero;

    if (shiftCrossesMidnight || startsAtMidnight)
    {
        // ✅ التجميع حسب "اليوم المنطقي" للشيفت
        byDay = sorted.GroupBy(f => GetShiftLogicalDay(f.FingerPrintDate, shift))
                      .ToDictionary(g => g.Key, g => g.OrderBy(f => f.FingerPrintDate).ToList());
        return byDay;
    }

    // ═══ وردية عادية (مش منتصف الليل) ═══
    byDay = sorted.GroupBy(f => DateOnly.FromDateTime(f.FingerPrintDate))
                  .ToDictionary(g => g.Key, g => g.OrderBy(f => f.FingerPrintDate).ToList());

    // نافذة الحضور المبكر
    var shiftDuration = shift.EndTime - shift.StartTime;
    if (shiftDuration < TimeSpan.Zero)
        shiftDuration += TimeSpan.FromHours(24);

    var earlyArrivalWindow = TimeSpan.FromHours(Math.Min(shiftDuration.TotalHours / 2, 4));

    foreach (var day in byDay.Keys.OrderBy(d => d).ToList())
    {
        if (!byDay.TryGetValue(day, out var dayScans) || dayScans.Count == 0) continue;

        var firstScan = dayScans.First();
        var scanTime = firstScan.FingerPrintDate.TimeOfDay;

        bool isEarlyArrival = IsEarlyArrivalForToday(scanTime, shift.StartTime, earlyArrivalWindow);
        if (isEarlyArrival) continue;

        bool isLateCheckout = IsLateCheckoutFromPreviousDay(scanTime);
        if (!isLateCheckout) continue;

        var prevDay = day.AddDays(-1);
        if (!byDay.TryGetValue(prevDay, out var prevDayScans) || prevDayScans.Count == 0) continue;
        if (prevDayScans.Count % 2 != 1) continue;

        dayScans.Remove(firstScan);
        prevDayScans.Add(firstScan);

        if (dayScans.Count == 0)
            byDay.Remove(day);
    }

    return byDay;
}

/// <summary>
/// ✅ تحديد "اليوم المنطقي" للبصمة مع دعم شيفت منتصف الليل
/// </summary>
private DateOnly GetShiftLogicalDay(DateTime scanTime, Shift shift)
{
    // ✅ شيفت من 12 AM لـ 12 PM
    if (shift.StartTime == TimeSpan.Zero)
    {
        var timeOfDay = scanTime.TimeOfDay;

        // ✅ لو البصمة مساءً (6 PM - 11:59 PM) => دي حضور مبكر لليوم التالي
        // ✅ لأن الموظف جاي بدري قبل شيفته اللي بيبدأ 12 AM
        if (timeOfDay >= TimeSpan.FromHours(18))
        {
            return DateOnly.FromDateTime(scanTime).AddDays(1);
        }

        // ✅ لو البصمة من 12 AM لـ 6 PM => نفس اليوم التقويمي
        return DateOnly.FromDateTime(scanTime);
    }

    // ✅ شيفت عابر لمنتصف الليل (مثلاً 10 PM - 6 AM)
    if (shift.EndTime < shift.StartTime)
    {
        var timeOfDay = scanTime.TimeOfDay;

        // ✅ البصمة بعد بداية الشيفت (10 PM أو بعدين) => نفس اليوم
        if (timeOfDay >= shift.StartTime)
            return DateOnly.FromDateTime(scanTime);

        // ✅ البصمة قبل نهاية الشيفت (6 AM أو قبله) => اليوم السابق
        if (timeOfDay <= shift.EndTime)
            return DateOnly.FromDateTime(scanTime).AddDays(-1);

        // ✅ خارج نطاق الشيفت
        return DateOnly.FromDateTime(scanTime);
    }

    // ✅ شيفت عادي (مثلاً 8 AM - 4 PM)
    return DateOnly.FromDateTime(scanTime);
}

    private bool IsEarlyArrivalForToday(TimeSpan scanTime, TimeSpan shiftStart, TimeSpan earlyArrivalWindow)
    {
        // ✅ لو البصمة قبل الشيفت في نفس اليوم
        if (scanTime <= shiftStart)
        {
            var diff = shiftStart - scanTime;
            return diff <= earlyArrivalWindow;
        }
        
        // ✅ لو الشيفت بيبدأ بدري (مثلاً 1 صباحاً) والبصمة في آخر اليوم السابق (11 مساءً)
        // ✅ نحسب الفرق من ناحية تانية
        var diffFromPreviousDay = (scanTime + TimeSpan.FromHours(24)) - shiftStart;
        return diffFromPreviousDay <= earlyArrivalWindow;
    }

        /// <summary>
        /// ✅ التحقق: هل البصمة "انصراف متأخر" من اليوم السابق؟
        /// البصمة تكون في الفترة الصباحية المبكرة (بعد منتصف الليل)
        /// </summary>
        private bool IsLateCheckoutFromPreviousDay(TimeSpan scanTime)
        {
            // ✅ لو البصمة بين 12:00 AM و 6:00 AM => انصراف متأخر
            return scanTime >= TimeSpan.Zero && scanTime < TimeSpan.FromHours(6);
        }
    }
}