using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Net.Http;
using System.Text.Json;

public class AttendanceService
{
    public async Task<bool> AddAttendanceAsync(int userId, int status, int branchId)
    {
        double? latitude = null;
        double? longitude = null;
        string? locationName = null;

        try
        {
            // 1️⃣ احصل على موقع الجهاز
            var statusLoc = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (statusLoc != PermissionStatus.Granted)
                statusLoc = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (statusLoc != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("خطأ", "الصلاحية للوصول للموقع مرفوضة", "OK");
                return false;
            }

            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });

            if (location != null)
            {
                latitude = location.Latitude;
                longitude = location.Longitude;

                // 2️⃣ استخدم Nominatim (OSM) لجلب العنوان
                locationName = await GetAddressFromCoordinatesAsync(latitude.Value, longitude.Value);
            }

            // 3️⃣ احفظ في كلا الجدولين
            using var db = new AppDbContext();
            var user = db.Users.Include(u => u.Shift).FirstOrDefault(u => u.Id == userId);

            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            // حفظ في جدول FingerPrints (سجل البصمات)
            var fingerPrint = new FingerPrint
            {
                UserId = userId,
                Status = status, // 1 حضور – 0 انصراف
                BranchId = branchId,
                FingerPrintDate = now,
                Latitude = latitude,
                Longitude = longitude,
                LocationName = locationName
            };

            // الحل: حفظ fingerPrint أولاً للحصول على الـ ID
            db.FingerPrints.Add(fingerPrint);
            await db.SaveChangesAsync(); // ✅ حفظ للحصول على ID

            // حفظ في جدول Attendances (سجل الحضور والانصراف)
            var attendance = await db.Attendances
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId &&
                    a.AttendanceDate.Date == today);

            if (attendance == null)
            {
                // إنشاء تسجيل حضور جديد
                attendance = new Attendance
                {
                    UserId = userId,
                    AttendanceDate = today,
                    CheckInBranchId = branchId,
                    CheckInLocation = locationName,
                    CheckInLatitude = latitude,
                    CheckInLongitude = longitude,
                    CheckInTime = now,
                    ShiftId = user?.ShiftId,
                    // إشارة أن الحضور تم بالبصمة
                    // حفظ معرف البصمة للربط - الآن fingerPrint.Id له قيمة
                    CheckInFingerPrintId = fingerPrint.Id
                };
                db.Attendances.Add(attendance);
            }
            else
            {
                // تحديث تسجيل الانصراف
                attendance.CheckOutBranchId = branchId;
                attendance.CheckOutLocation = locationName;
                attendance.CheckOutLatitude = latitude;
                attendance.CheckOutLongitude = longitude;
                attendance.CheckOutTime = now;
                // إشارة أن الانصراف تم بالبصمة
                // حفظ معرف البصمة للربط - الآن fingerPrint.Id له قيمة
                attendance.CheckOutFingerPrintId = fingerPrint.Id;

                // حساب ساعات العمل إذا كان هناك حضور وانصراف
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                {
                    attendance.TotalWorkHours = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;

                    // حساب التأخير والعمل الإضافي
                    if (user?.Shift != null)
                    {
                        CalculateLateAndEarly(attendance, user.Shift);
                    }
                }
            }

            await db.SaveChangesAsync(); // ✅ حفظ التغييرات على Attendance

            // 4️⃣ إرسال إشعار بنجاح العملية
            string message = status == 1 ?
                "✅ تم تسجيل الحضور بالبصمة" :
                "✅ تم تسجيل الانصراف بالبصمة";
            await Application.Current.MainPage.DisplayAlert("نجاح", message, "OK");

            return true;
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("خطأ", $"خطأ في تسجيل البصمة: {ex.Message}", "OK");
            Console.WriteLine($"Attendance/FP error: {ex}");
            return false;
        }
    }

    private async Task<string?> GetAddressFromCoordinatesAsync(double lat, double lon)
    {
        try
        {
            string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Sho2onApp/1.0");
            var json = await client.GetStringAsync(url);

            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("display_name", out var displayName))
            {
                return displayName.GetString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reverse geocoding error: {ex.Message}");
        }

        return null;
    }

    // دالة مساعدة للحصول على بيانات اليوم للمستخدم
    public async Task<Attendance?> GetTodayAttendanceAsync(int userId)
    {
        try
        {
            using var db = new AppDbContext();
            var today = DateTime.Now.Date;

            return await db.Attendances
                .Include(a => a.User)
                .Include(a => a.Shift)
                .Include(a => a.CheckInFingerPrint) // ✅ تضمين بيانات البصمة
                .Include(a => a.CheckOutFingerPrint) // ✅ تضمين بيانات البصمة
                .FirstOrDefaultAsync(a =>
                    a.UserId == userId &&
                    a.AttendanceDate.Date == today);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetTodayAttendance error: {ex.Message}");
            return null;
        }
    }

    // دالة مساعدة للحصول على بصمات اليوم
    public async Task<List<FingerPrint>> GetTodayFingerPrintsAsync(int userId)
    {
        try
        {
            using var db = new AppDbContext();
            var today = DateTime.Now.Date;

            return await db.FingerPrints
                .Where(fp => fp.UserId == userId && fp.FingerPrintDate.Date == today)
                .OrderBy(fp => fp.FingerPrintDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetTodayFingerPrints error: {ex.Message}");
            return new List<FingerPrint>();
        }
    }

    // دالة مساعدة لحساب التأخير والمغادرة المبكرة
    private void CalculateLateAndEarly(Attendance attendance, Shift shift)
    {
        if (attendance.CheckInTime.HasValue)
        {
            var shiftStart = attendance.AttendanceDate.Date + shift.StartTime;
            if (attendance.CheckInTime.Value > shiftStart)
            {
                attendance.Late = attendance.CheckInTime.Value - shiftStart;
            }
            else if (attendance.CheckInTime.Value < shiftStart)
            {
                attendance.EarlyEnter = shiftStart - attendance.CheckInTime.Value;
            }
        }

        if (attendance.CheckOutTime.HasValue)
        {
            var shiftEnd = attendance.AttendanceDate.Date + shift.EndTime;
            if (attendance.CheckOutTime.Value < shiftEnd)
            {
                attendance.EarlyLeave = shiftEnd - attendance.CheckOutTime.Value;
            }
            else if (attendance.CheckOutTime.Value > shiftEnd)
            {
                attendance.Overtime = attendance.CheckOutTime.Value - shiftEnd;
            }
        }
    }

    // دالة لإلغاء آخر بصمة (إن وجدت)
    public async Task<bool> CancelLastFingerPrintAsync(int userId)
    {
        try
        {
            using var db = new AppDbContext();
            var today = DateTime.Now.Date;

            var lastFingerPrint = await db.FingerPrints
                .Where(fp => fp.UserId == userId && fp.FingerPrintDate.Date == today)
                .OrderByDescending(fp => fp.FingerPrintDate)
                .FirstOrDefaultAsync();

            if (lastFingerPrint != null)
            {
                // إذا كانت بصمة حضور
                if (lastFingerPrint.Status == 1)
                {
                    var attendance = await db.Attendances
                        .FirstOrDefaultAsync(a =>
                            a.UserId == userId &&
                            a.AttendanceDate.Date == today &&
                            a.CheckInFingerPrintId == lastFingerPrint.Id);

                    if (attendance != null)
                    {
                        attendance.CheckInTime = null;
                        attendance.CheckInLocation = null;
                        attendance.CheckInLatitude = null;
                        attendance.CheckInLongitude = null;
                        attendance.CheckInFingerPrintId = null;
                    }
                }
                // إذا كانت بصمة انصراف
                else if (lastFingerPrint.Status == 0)
                {
                    var attendance = await db.Attendances
                        .FirstOrDefaultAsync(a =>
                            a.UserId == userId &&
                            a.AttendanceDate.Date == today &&
                            a.CheckOutFingerPrintId == lastFingerPrint.Id);

                    if (attendance != null)
                    {
                        attendance.CheckOutTime = null;
                        attendance.CheckOutLocation = null;
                        attendance.CheckOutLatitude = null;
                        attendance.CheckOutLongitude = null;
                        attendance.CheckOutFingerPrintId = null;
                        attendance.TotalWorkHours = null;
                        attendance.Late = null;
                        attendance.EarlyLeave = null;
                        attendance.Overtime = null;
                        attendance.EarlyEnter = null;
                    }
                }

                db.FingerPrints.Remove(lastFingerPrint);
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CancelLastFingerPrint error: {ex.Message}");
            return false;
        }
    }

    // دالة بديلة باستخدام Transaction لضمان سلامة البيانات
    public async Task<bool> AddAttendanceWithTransactionAsync(int userId, int status, int branchId)
    {
        using var db = new AppDbContext();
        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            double? latitude = null;
            double? longitude = null;
            string? locationName = null;

            // الحصول على الموقع
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });

            if (location != null)
            {
                latitude = location.Latitude;
                longitude = location.Longitude;
                locationName = await GetAddressFromCoordinatesAsync(latitude.Value, longitude.Value);
            }

            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            var user = db.Users.Include(u => u.Shift).FirstOrDefault(u => u.Id == userId);

            // 1. حفظ FingerPrint
            var fingerPrint = new FingerPrint
            {
                UserId = userId,
                Status = status,
                BranchId = branchId,
                FingerPrintDate = now,
                Latitude = latitude,
                Longitude = longitude,
                LocationName = locationName
            };

            db.FingerPrints.Add(fingerPrint);
            await db.SaveChangesAsync(); // الحصول على ID

            // 2. حفظ/تحديث Attendance
            var attendance = await db.Attendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AttendanceDate.Date == today);

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    UserId = userId,
                    AttendanceDate = today,
                    CheckInBranchId = branchId,
                    CheckInLocation = locationName,
                    CheckInLatitude = latitude,
                    CheckInLongitude = longitude,
                    CheckInTime = now,
                    ShiftId = user?.ShiftId,
                    CheckInFingerPrintId = fingerPrint.Id
                };
                db.Attendances.Add(attendance);
            }
            else
            {
                attendance.CheckOutBranchId = branchId;
                attendance.CheckOutLocation = locationName;
                attendance.CheckOutLatitude = latitude;
                attendance.CheckOutLongitude = longitude;
                attendance.CheckOutTime = now;
                attendance.CheckOutFingerPrintId = fingerPrint.Id;

                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                {
                    attendance.TotalWorkHours = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;

                    if (user?.Shift != null)
                    {
                        CalculateLateAndEarly(attendance, user.Shift);
                    }
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            string message = status == 1 ?
                "✅ تم تسجيل الحضور بالبصمة" :
                "✅ تم تسجيل الانصراف بالبصمة";
            await Application.Current.MainPage.DisplayAlert("نجاح", message, "OK");

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            await Application.Current.MainPage.DisplayAlert("خطأ", $"خطأ في تسجيل البصمة: {ex.Message}", "OK");
            return false;
        }
    }
}