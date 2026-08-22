using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.API.Data;
using Sho2on.API.Dtos;
using Sho2on.API.Models;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AttendanceController(AppDbContext db) { _db = db; }

        [HttpPost("record")]
        public async Task<IActionResult> Record([FromBody] RecordDto dto)
        {
            // dto: userId, status, branchId, latitude, longitude, locationName, deviceTime
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
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
                            CheckInTime = now,
                            ShiftId = user?.ShiftId,
                            CheckInFingerPrintId = fp.Id
                        };

                        _db.Attendances.Add(attendance);

                    }else
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

                                // تأخير الدخول
                                if (attendance.CheckInTime.Value.TimeOfDay > shift.StartTime)
                                    attendance.Late = attendance.CheckInTime.Value.TimeOfDay - shift.StartTime;

                                // خروج مبكر
                                if (attendance.CheckOutTime.Value.TimeOfDay < shift.EndTime)
                                    attendance.EarlyLeave = shift.EndTime - attendance.CheckOutTime.Value.TimeOfDay;

                                // ساعات العمل الفعلية
                                if (attendance.CheckOutTime.Value.TimeOfDay > attendance.CheckInTime.Value.TimeOfDay)
                                    attendance.TotalWorkHours = attendance.CheckOutTime.Value.TimeOfDay - attendance.CheckInTime.Value.TimeOfDay;
                                else if (attendance.CheckOutTime.Value.Date > attendance.CheckInTime.Value.Date)
                                {
                                    // حالة الوردية المسائية (تخطت منتصف الليل)
                                    attendance.TotalWorkHours = (attendance.CheckOutTime - attendance.CheckInTime);
                                }
                                else
                                {
                                    // حالة وقت الخروج أصغر من وقت الدخول في نفس اليوم
                                    attendance.TotalWorkHours = TimeSpan.Zero;
                                }

                                // أوفر تايم
                                if (attendance.CheckOutTime.Value.TimeOfDay > shift.EndTime)
                                    attendance.Overtime = attendance.CheckOutTime.Value.TimeOfDay - shift.EndTime;

                                // Early Enter
                                if (attendance.CheckInTime.Value.TimeOfDay < shift.StartTime)
                                    attendance.EarlyEnter = shift.StartTime - attendance.CheckInTime.Value.TimeOfDay;
                            }

                        }
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex) { await transaction.RollbackAsync(); return BadRequest(ex.Message); }
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
