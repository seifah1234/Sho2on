using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sho2on.API.Dtos;
using Sho2on.Database.Models;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AttendanceController(AppDbContext db) { _db = db; }

        [HttpGet("today/{userId}")]
        public async Task<IActionResult> GetTodayAttendance(int userId)
        {
            var today = DateTime.Today;
            var att = await _db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == today);

            if (att == null) return Ok(new { Status = "لم يسجل بعد" });

            return Ok(new
            {
                CheckInTime = att.CheckInTime,
                CheckOutTime = att.CheckOutTime,
                Late = att.Late,
                Overtime = att.Overtime,
                IsAbsence = att.IsAbsence
            });
        }


        [HttpPost("record")]
        public async Task<IActionResult> Record([FromBody] RecordDto dto)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // ✅ التحقق من الموقع أولاً
                var locationCheck = await ValidateLocation(dto);
                if (!locationCheck.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = locationCheck.ErrorMessage
                    });
                }

                var now = dto.DeviceTime ?? DateTime.Now;
                var fp = new FingerPrint
                {
                    UserId = dto.UserId,
                    Status = dto.Status,
                    BranchId = dto.BranchId,
                    FingerPrintDate = now,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    LocationName = dto.LocationName
                };
                _db.FingerPrints.Add(fp);
                await _db.SaveChangesAsync();

                var today = now.Date;
                var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == dto.UserId && a.AttendanceDate == today);
                var user = await _db.Users.Include(u => u.Shift).FirstOrDefaultAsync(u => u.Id == dto.UserId);

                if (attendance == null)
                {
                    if (dto.Status == 1)
                    {
                        attendance = new Attendance
                        {
                            UserId = dto.UserId,
                            AttendanceDate = today,
                            CheckInBranchId = dto.BranchId,
                            CheckInLocation = dto.LocationName,
                            CheckInLatitude = dto.Latitude,
                            CheckInLongitude = dto.Longitude,
                            IsAbsence = false,
                            CheckInTime = now,
                            ShiftId = user?.ShiftId,
                            CheckInFingerPrintId = fp.Id
                        };
                        _db.Attendances.Add(attendance);
                    }
                    else
                    {
                        attendance = new Attendance
                        {
                            UserId = dto.UserId,
                            AttendanceDate = today,
                            CheckOutBranchId = dto.BranchId,
                            CheckOutLocation = dto.LocationName,
                            CheckOutLatitude = dto.Latitude,
                            CheckOutLongitude = dto.Longitude,
                            CheckOutTime = now,
                            IsAbsence = false,
                            ShiftId = user?.ShiftId,
                            CheckOutFingerPrintId = fp.Id
                        };
                        _db.Attendances.Add(attendance);
                    }
                }
                else
                {
                    if (dto.Status == 0)
                    {
                        attendance.CheckOutBranchId = dto.BranchId;
                        attendance.CheckOutLocation = dto.LocationName;
                        attendance.CheckOutLatitude = dto.Latitude;
                        attendance.CheckOutLongitude = dto.Longitude;
                        attendance.CheckOutTime = now;
                        attendance.CheckOutFingerPrintId = fp.Id;
                    }
                    else
                    {
                        attendance.CheckInBranchId = dto.BranchId;
                        attendance.CheckInLocation = dto.LocationName;
                        attendance.CheckInLatitude = dto.Latitude;
                        attendance.CheckInLongitude = dto.Longitude;
                        attendance.CheckInTime = now;
                        attendance.CheckInFingerPrintId = fp.Id;
                    }

                    if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                    {
                        attendance.TotalWorkHours = attendance.CheckOutTime - attendance.CheckInTime;
                        if (user?.Shift != null)
                        {
                            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue && user?.Shift != null)
                            {
                                var shift = user.Shift;

                                if (attendance.CheckInTime.Value.TimeOfDay > shift.StartTime)
                                    attendance.Late = attendance.CheckInTime.Value.TimeOfDay - shift.StartTime;

                                if (attendance.CheckOutTime.Value.TimeOfDay < shift.EndTime)
                                    attendance.EarlyLeave = shift.EndTime - attendance.CheckOutTime.Value.TimeOfDay;

                                if (attendance.CheckOutTime.Value.TimeOfDay > attendance.CheckInTime.Value.TimeOfDay)
                                    attendance.TotalWorkHours = attendance.CheckOutTime.Value.TimeOfDay - attendance.CheckInTime.Value.TimeOfDay;
                                else if (attendance.CheckOutTime.Value.Date > attendance.CheckInTime.Value.Date)
                                    attendance.TotalWorkHours = (attendance.CheckOutTime - attendance.CheckInTime);
                                else
                                    attendance.TotalWorkHours = TimeSpan.Zero;

                                if (attendance.CheckOutTime.Value.TimeOfDay > shift.EndTime)
                                    attendance.Overtime = attendance.CheckOutTime.Value.TimeOfDay - shift.EndTime;

                                if (attendance.CheckInTime.Value.TimeOfDay < shift.StartTime)
                                    attendance.EarlyEnter = shift.StartTime - attendance.CheckInTime.Value.TimeOfDay;
                            }
                        }
                    }
                    attendance.IsAbsence = false;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ✅ دالة التحقق من الموقع
        private async Task<(bool IsValid, string ErrorMessage)> ValidateLocation(RecordDto dto)
        {
            // جلب الموظف مع الفرع
            var user = await _db.Users
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId);

            if (user == null)
                return (false, "الموظف غير موجود");

             if (user.IsFreeLocation.HasValue && user.IsFreeLocation.Value)
                return (true, "");

            // جلب الفرع
            var branch = user.Branch;
            if (branch == null)
                return (false, "الفرع غير موجود للموظف");

            // إذا لم يكن للفرع إحداثيات، تخطي التحقق
            if (branch.Latitude == null || branch.Longitude == null)
                return (true, ""); // لا توجد إحداثيات للتحقق

            if (dto.Latitude == null || dto.Longitude == null)
                return (false, "لم يتم تحديد الموقع. يرجى تفعيل GPS والمحاولة مرة أخرى");

            var distance = CalculateDistance(
                dto.Latitude.Value,
                dto.Longitude.Value,
                branch.Latitude.Value,
                branch.Longitude.Value
            );

            var radius = branch.RadiusMeters > 0 ? branch.RadiusMeters : 100;

            if (distance > radius)
                return (false, $"أنت خارج نطاق الفرع. المسافة: {distance:F0} متر والمسموح: {radius} متر");

            return (true, "");
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;

            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);
            var deltaLat = ToRadians(lat2 - lat1);
            var deltaLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        [HttpGet("today/{userId}")]
        public async Task<IActionResult> Today(int userId)
        {
            var today = DateTime.Now.Date;
            var att = await _db.Attendances.Include(a => a.CheckInFingerPrint).Include(a => a.CheckOutFingerPrint)
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate == today);
            return Ok(att);
        }

        [Authorize]
        [HttpGet("fingerprints/today/{userId}")]
        public async Task<IActionResult> Fingerprints(int userId)
        {
            var today = DateTime.Now.Date;
            var fps = await _db.FingerPrints.Where(fp => fp.UserId == userId && fp.FingerPrintDate.Date == today)
                        .OrderBy(fp => fp.FingerPrintDate).ToListAsync();
            return Ok(fps);
        }

        [HttpDelete("fingerprint/last/{userId}")]
        public async Task<IActionResult> DeleteLast(int userId)
        {
            var today = DateTime.Now.Date;
            var last = await _db.FingerPrints.Where(fp => fp.UserId == userId && fp.FingerPrintDate.Date == today)
                        .OrderByDescending(fp => fp.FingerPrintDate).FirstOrDefaultAsync();
            if (last == null) return NotFound();
            // detach relations like في كودك...
            _db.FingerPrints.Remove(last);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

}
