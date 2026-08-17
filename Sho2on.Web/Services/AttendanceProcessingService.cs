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
                    CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, user.Shift);

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
                        CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, user.Shift);

                    db.Attendances.Add(attendance);
                    result.DaysAutoResolved++;
                }

                result.EmployeesProcessed++;
            }

            await db.SaveChangesAsync();
            return result;
        }

        /// <summary>
        /// لو اليوم فيه حضور بس من غير انصراف (أو العكس)، نكمّل الناقص تلقائيًا بميعاد الوردية القياسي.
        /// ده منفصل تمامًا عن موضوع تعدي منتصف الليل - هنا الموظف أصلاً مبصمش الطرف التاني خالص.
        /// </summary>
        private bool FillMissingPunch(Attendance attendance, DateOnly day, Shift shift)
        {
            if (attendance.CheckInTime == null && attendance.CheckOutTime == null)
                return false;

            if (attendance.CheckInTime != null && attendance.CheckOutTime != null)
                return false;

            if (attendance.CheckInTime != null)
            {
                var checkOutTime = day.ToDateTime(TimeOnly.MinValue).Add(shift.EndTime);

                if (shift.EndTime < shift.StartTime)
                    checkOutTime = checkOutTime.AddDays(1);

                attendance.CheckOutTime = checkOutTime;
                attendance.IsCheckOutAutoFilled = true;
                return true;
            }

            attendance.CheckInTime = day.ToDateTime(TimeOnly.MinValue).Add(shift.StartTime);
            attendance.IsCheckInAutoFilled = true;
            return true;
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

                // ═══ التطبيع التلقائي الكامل (تكرار + نمط تبديل + بصمة واحدة) ═══
                var (cleanScans, dupCount, correctedCount, singleCount) = await NormalizeUserScansAsync(db, allScans, user.Shift);

                var duplicates = allScans.Except(cleanScans).ToList();
                if (duplicates.Count > 0) db.FingerPrints.RemoveRange(duplicates);

                result.DuplicateScansRemoved += dupCount;
                result.StatusesAutoCorrected += correctedCount;
                result.SingleScanDaysAutoResolved += singleCount;

                var scansByDay = GroupScansByWorkDay(cleanScans, user.Shift);

                var existingAttendances = await db.Attendances
                    .Where(a => a.UserId == user.Id && a.AttendanceDate >= start && a.AttendanceDate <= end)
                    .ToListAsync();
                if (existingAttendances.Count > 0) db.Attendances.RemoveRange(existingAttendances);

                var weekHoliDays = GetWeekHolidayFlags(user.WeekHoliday);

                for (var day = startDate; day <= endDate; day = day.AddDays(1))
                {
                    var dayIndex = (int)day.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                    bool isHoliday = weekHoliDays[dayIndex] || officialHolidays.Contains(day);
                    scansByDay.TryGetValue(day, out var dayScans);

                    DateTime? checkIn = dayScans?.FirstOrDefault(s => s.Status == 1)?.FingerPrintDate;
                    DateTime? checkOut = dayScans?.LastOrDefault(s => s.Status == 0)?.FingerPrintDate
                                          ?? (dayScans?.Count > 1 ? dayScans.Last().FingerPrintDate : null);

                    bool isAbsence = checkIn == null && checkOut == null && !isHoliday;

                    var attendance = new Attendance
                    {
                        UserId = user.Id,
                        AttendanceDate = day.ToDateTime(TimeOnly.MinValue),
                        CheckInTime = checkIn,
                        CheckOutTime = checkOut,
                        CheckInBranchId = user.BranchId,
                        CheckOutBranchId = user.BranchId,
                        IsAbsence = isAbsence,
                        IsHoliday = isHoliday,
                        ShiftId = user.ShiftId
                    };

                    if (!isAbsence)
                    {
                        bool wasFilled = FillMissingPunch(attendance, day, user.Shift);
                        if (wasFilled) result.MissingPunchesAutoFilled++;
                    }

                    if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                        CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, user.Shift);


                    db.Attendances.Add(attendance);

                    if (dayScans != null && dayScans.Count > 0) result.DaysAutoResolved++;
                }

                result.EmployeesProcessed++;
            }

            await db.SaveChangesAsync();
            return result;
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
                CalculateTimes(attendance, attendance.CheckInTime.Value, attendance.CheckOutTime.Value, shift);
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
                CalculateTimes(att, att.CheckInTime.Value, att.CheckOutTime.Value, shift);

            await _db.SaveChangesAsync();
        }

        private void CalculateTimes(Attendance attendance, DateTime checkIn, DateTime checkOut, Shift shift)
        {
            // نبني الميعاد المتوقع الكامل (تاريخ + وقت) بناءً على تاريخ الحضور الفعلي
            var shiftDate = checkIn.Date;
            var expectedStart = shiftDate.Add(shift.StartTime);

            // لو الوردية عابرة لمنتصف الليل أصلاً، نهايتها المتوقعة في اليوم التالي
            var expectedEnd = shift.EndTime < shift.StartTime
                ? shiftDate.AddDays(1).Add(shift.EndTime)
                : shiftDate.Add(shift.EndTime);

            // ═══ حساب التأخير / الحضور المبكر ═══
            if (checkIn > expectedStart)
                attendance.Late = checkIn - expectedStart;
            else if (checkIn < expectedStart)
                attendance.EarlyEnter = expectedStart - checkIn;

            // ═══ حساب الانصراف المبكر / الإضافي ═══
            // المقارنة بقت بالـ DateTime الكامل، فلو الانصراف حصل بعد منتصف الليل
            // (سواء وردية عابرة أصلاً أو حالة استثنائية) هيتحسب صح كإضافي
            if (checkOut > expectedEnd)
                attendance.Overtime = checkOut - expectedEnd;
            else if (checkOut < expectedEnd)
                attendance.EarlyLeave = expectedEnd - checkOut;

            // ═══ إجمالي ساعات العمل ═══
            attendance.TotalWorkHours = checkOut - checkIn;
        }

        private TimeSpan CircularDistance(TimeSpan a, TimeSpan b)
        {
            var diff = (a - b).Duration();
            var wrap = TimeSpan.FromHours(24) - diff;
            return diff < wrap ? diff : wrap;
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
        /// تحديد "يوم الوردية المنطقي" للبصمة بدل اليوم التقويمي، عشان الورديات العابرة لمنتصف الليل
        /// (مثلاً: حضور 8 صباحاً وانصراف 1 صباحاً اليوم التالي) تتحسب على نفس يوم العمل
        /// </summary>
        private DateOnly GetShiftLogicalDay(DateTime scanTime, Shift shift)
        {
            // وردية عادية (مش عابرة لمنتصف الليل) => نفس اليوم التقويمي
            if (shift.EndTime >= shift.StartTime)
                return DateOnly.FromDateTime(scanTime);

            var timeOfDay = scanTime.TimeOfDay;

            // البصمة وقعت في نطاق بداية الوردية أو بعده (مثلاً 8 صباحاً وبعدين) => نفس اليوم
            if (timeOfDay >= shift.StartTime)
                return DateOnly.FromDateTime(scanTime);

            // البصمة وقعت في الفترة الصباحية اللي بعد منتصف الليل وقبل نهاية الوردية (مثلاً 1 صباحاً)
            // => دي فعلياً امتداد ليوم العمل اللي قبلها
            if (timeOfDay <= shift.EndTime)
                return DateOnly.FromDateTime(scanTime).AddDays(-1);

            // خارج نطاق الوردية تماماً (نادر) => سيبها على يومها التقويمي
            return DateOnly.FromDateTime(scanTime);
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

        /// <summary>
        /// تجميع البصمات حسب "يوم العمل الفعلي" مش اليوم التقويمي.
        /// حالة 1: وردية معرّفة أصلاً كعابرة لمنتصف الليل (EndTime < StartTime)
        /// حالة 2: وردية صباحية عادية لكن في يوم معين الموظف انصرف بعد منتصف الليل (استثناء)
        /// </summary>
        private Dictionary<DateOnly, List<FingerPrint>> GroupScansByWorkDay(List<FingerPrint> scans, Shift shift)
        {
            var sorted = scans.OrderBy(f => f.FingerPrintDate).ToList();
            bool shiftCrossesMidnight = shift.EndTime < shift.StartTime;

            Dictionary<DateOnly, List<FingerPrint>> byDay;

            if (shiftCrossesMidnight)
            {
                // الوردية أصلاً عابرة لمنتصف الليل (اتعالجت في الرد اللي فات)
                byDay = sorted.GroupBy(f => GetShiftLogicalDay(f.FingerPrintDate, shift))
                              .ToDictionary(g => g.Key, g => g.OrderBy(f => f.FingerPrintDate).ToList());
                return byDay;
            }

            // ═══ وردية عادية: تجميع مبدئي حسب اليوم التقويمي ═══
            byDay = sorted.GroupBy(f => DateOnly.FromDateTime(f.FingerPrintDate))
                          .ToDictionary(g => g.Key, g => g.OrderBy(f => f.FingerPrintDate).ToList());

            // سقف زمني: أي بصمة قبل الساعة دي بفترة كافية من بداية الوردية تعتبر مرشحة "انصراف متأخر"
            // مثلاً وردية تبدأ 8 الصبح => أي بصمة قبل الساعة 6 الصبح تعتبر مشكوك فيها (مش حضور طبيعي)
            var earlyMorningCutoff = shift.StartTime.Subtract(TimeSpan.FromHours(2));
            if (earlyMorningCutoff < TimeSpan.Zero) earlyMorningCutoff = TimeSpan.FromHours(4); // حماية لو الوردية بتبدأ بدري جداً

            foreach (var day in byDay.Keys.OrderBy(d => d).ToList())
            {
                if (!byDay.TryGetValue(day, out var dayScans) || dayScans.Count == 0) continue;

                var firstScan = dayScans.First();
                bool isSuspiciousLateCheckout = firstScan.FingerPrintDate.TimeOfDay < earlyMorningCutoff;
                if (!isSuspiciousLateCheckout) continue;

                var prevDay = day.AddDays(-1);
                if (!byDay.TryGetValue(prevDay, out var prevDayScans) || prevDayScans.Count == 0) continue;

                // اليوم السابق لازم يكون فيه عدد فردي من البصمات (بصمة حضور من غير انصراف يقابلها)
                if (prevDayScans.Count % 2 != 1) continue;

                // ننقل البصمة عشان تُحسب كانصراف اليوم السابق
                dayScans.Remove(firstScan);
                prevDayScans.Add(firstScan);

                if (dayScans.Count == 0)
                    byDay.Remove(day);
            }

            return byDay;
        }
    }
}