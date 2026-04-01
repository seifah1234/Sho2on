using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Sho2on.Services
{
    public class UserImportService
    {
        private readonly AppDbContext _context;
        private readonly Func<int, int, string, bool> _progressCallback;
        private CancellationTokenSource _cancellationTokenSource;

        public UserImportService(AppDbContext context, Func<int, int, string, bool> progressCallback = null)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _progressCallback = progressCallback;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void CancelImport()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task<int> ImportUsersFromExcelAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("ملف Excel غير موجود.", filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new Exception("لم يتم العثور على أي ورقة عمل في ملف Excel.");

            var usersFromExcel = new List<User>();
            var idsFromExcel = new HashSet<int>();
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int importedCount = 0;
            int successCount = 0;
            int errorCount = 0;

            // أولاً: قراءة جميع المستخدمين من Excel مع تحديث التقدم
            for (int row = 2; row <= rowCount; row++)
            {
                // التحقق من الإلغاء
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    throw new OperationCanceledException("تم إلغاء عملية الاستيراد.");
                }

                try
                {
                    // تحديث التقدم
                    if (_progressCallback != null)
                    {
                        bool shouldContinue = _progressCallback(row - 1, rowCount - 1,
                            $"جاري قراءة الصف {row - 1} من {rowCount - 1}");
                        if (!shouldContinue) break;
                    }

                    var user = MapRowToUser(worksheet, row);

                    if (user != null)
                    {
                        if (idsFromExcel.Contains(user.Id))
                        {
                            errorCount++;
                            continue;
                        }

                        usersFromExcel.Add(user);
                        idsFromExcel.Add(user.Id);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"خطأ في قراءة الصف {row}: {ex.Message}");
                }
            }

            // تحديث حالة التقدم
            if (_progressCallback != null)
            {
                _progressCallback(importedCount, importedCount, "جاري معالجة البيانات...");
            }

            // ثانياً: جلب المستخدمين الموجودين
            var existingUserIds = await _context.Users
                .Where(u => idsFromExcel.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            // ثالثاً: فصل المستخدمين
            var newUsers = new List<User>();
            var existingUsersMap = await _context.Users
                .Where(u => existingUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            int current = 0;
            foreach (var user in usersFromExcel)
            {
                current++;

                // تحديث التقدم
                if (_progressCallback != null)
                {
                    _progressCallback(current, usersFromExcel.Count,
                        $"جاري معالجة الموظف {current} من {usersFromExcel.Count}");
                }

                if (existingUsersMap.ContainsKey(user.Id))
                {
                    continue;
                }
                else
                {
                    // إضافة مستخدم جديد
                    newUsers.Add(user);
                }
            }

            // تحديث حالة التقدم
            if (_progressCallback != null)
            {
                _progressCallback(usersFromExcel.Count, usersFromExcel.Count,
                    "جاري حفظ البيانات في قاعدة البيانات...");
            }

            // رابعاً: إضافة المستخدمين الجدد
            await _context.Users.AddRangeAsync(newUsers);

            try
            {
                var result = await _context.SaveChangesAsync();
                successCount = newUsers.Count + existingUsersMap.Count;

                return successCount;
            }
            catch (DbUpdateException ex)
            {
                // تسجيل التفاصيل للأخطاء
                Console.WriteLine($"خطأ في حفظ البيانات: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"تفاصيل الخطأ: {ex.InnerException.Message}");

                throw;
            }
        }

        private User? MapRowToUser(ExcelWorksheet worksheet, int row)
        {
            // إذا كان العمود الأول فارغاً، نتجاهل الصف
            if (worksheet.Cells[row, 1].Value == null)
                return null;

            var user = new User();

            // Id
            user.Id = GetIntValue(worksheet.Cells[row, 1].Value, 0);

            // NationalID
            user.NationalID = worksheet.Cells[row, 2].Value?.ToString() ?? "";

            // PhoneNumber
            user.PhoneNumber = worksheet.Cells[row, 3].Value?.ToString() ?? "";

            // FullName
            user.FullName = worksheet.Cells[row, 4].Value?.ToString() ?? "";

            // Email (قد يكون فارغاً)
            user.Email = worksheet.Cells[row, 5].Value?.ToString();

            // Address (قد يكون فارغاً)
            user.Address = worksheet.Cells[row, 6].Value?.ToString();

            // HireDate
            user.HireDate = GetDateOnlyValue(worksheet.Cells[row, 7].Value);

            // BirthDate
            user.BirthDate = GetDateOnlyValue(worksheet.Cells[row, 8].Value);

            // MainSalary و MinSalary
            user.MainSalary = GetDecimalValue(worksheet.Cells[row, 9].Value);
            user.MinSalary = GetDecimalValue(worksheet.Cells[row, 10].Value);

            // Gender
            var gender = worksheet.Cells[row, 11].Value?.ToString();
            user.Gender = gender?.Length > 0 ? gender[0] : 'M';

            // IDs المختلفة
            user.BranchId = GetIntValue(worksheet.Cells[row, 12].Value, 1);
            user.DepartmentId = GetIntValue(worksheet.Cells[row, 13].Value, 1);
            user.JobTitleId = GetIntValue(worksheet.Cells[row, 14].Value, 1);
            user.DegreeId = GetIntValue(worksheet.Cells[row, 15].Value, 1);
            user.ShiftId = GetIntValue(worksheet.Cells[row, 16].Value, 1);
            user.BreakId = GetIntValue(worksheet.Cells[row, 17].Value, 1);
            user.WeekHolidayId = GetIntValue(worksheet.Cells[row, 18].Value, 15);
            user.JobTypeId = GetIntValue(worksheet.Cells[row, 19].Value, 1);

            // Exempt Columns
            user.ExemptLate = GetBoolValue(worksheet.Cells[row, 20].Value);
            user.ExemptEarlyLeave = GetBoolValue(worksheet.Cells[row, 21].Value);
            user.ExemptOvertime = GetBoolValue(worksheet.Cells[row, 22].Value);
            user.ExemptAbsence = GetBoolValue(worksheet.Cells[row, 23].Value);
            user.ExemptEarlyEnter = GetBoolValue(worksheet.Cells[row, 24].Value);

            // WorkHours
            var workHoursValue = worksheet.Cells[row, 25].Value;
            if (workHoursValue != null)
            {
                if (workHoursValue is string timeStr && TimeSpan.TryParse(timeStr, out TimeSpan time))
                    user.WorkHours = time;
                else if (workHoursValue is double hoursDouble)
                    user.WorkHours = TimeSpan.FromHours(hoursDouble);
                else if (double.TryParse(workHoursValue.ToString(), out double hours))
                    user.WorkHours = TimeSpan.FromHours(hours);
            }

            // Boolean values
            user.InDuty = GetBoolValue(worksheet.Cells[row, 26].Value);
            user.Blacklist = GetBoolValue(worksheet.Cells[row, 29].Value);
            user.UnderTraining = GetBoolValue(worksheet.Cells[row, 31].Value);
            user.UnderEmployment = GetBoolValue(worksheet.Cells[row, 32].Value);
            user.IsArchived = GetBoolValue(worksheet.Cells[row, 33].Value);
            user.IsUser = GetBoolValue(worksheet.Cells[row, 43].Value);
            user.IsMobileUser = GetBoolValue(worksheet.Cells[row, 47].Value);

            // HolidayBalance
            user.HolidayBalance = GetIntValue(worksheet.Cells[row, 28].Value, 0);

            // BlacklistReason
            user.BlacklistReason = worksheet.Cells[row, 30].Value?.ToString();

            // FinishJob
            user.FinishJob = GetDateOnlyValue(worksheet.Cells[row, 34].Value);

            // تواريخ انتهاء الوثائق
            user.DriverLicenseExpiration = GetDateOnlyValue(worksheet.Cells[row, 35].Value);
            user.VehicleLicenseExpiration = GetDateOnlyValue(worksheet.Cells[row, 36].Value);
            user.NationalIDExpiration = GetDateOnlyValue(worksheet.Cells[row, 37].Value);
            user.ArmyCertificateExpiration = GetDateOnlyValue(worksheet.Cells[row, 38].Value);

            // ArmyCertificateNumber و SSN و HealthInsuranceNumber
            user.ArmyCertificateNumber = worksheet.Cells[row, 39].Value?.ToString();
            user.SSN = worksheet.Cells[row, 40].Value?.ToString();
            user.HealthInsuranceNumber = worksheet.Cells[row, 41].Value?.ToString();

            // PasswordHash
            user.PasswordHash = worksheet.Cells[row, 42].Value?.ToString();

            // ProfileImageData
            user.ProfileImageData = null;

            // التواريخ
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            // RegisteredDeviceId
            user.RegisteredDeviceId = worksheet.Cells[row, 46].Value?.ToString();

            return user;
        }

        // دالات مساعدة
        private int GetIntValue(object value, int defaultValue)
        {
            if (value == null) return defaultValue;

            if (value is int i) return i;
            if (value is double d) return (int)d;
            if (value is decimal dec) return (int)dec;
            if (int.TryParse(value.ToString(), out int result)) return result;

            return defaultValue;
        }

        private decimal GetDecimalValue(object value)
        {
            if (value == null) return 0;

            if (value is decimal dec) return dec;
            if (value is double d) return (decimal)d;
            if (value is int i) return i;
            if (decimal.TryParse(value.ToString(), out decimal result)) return result;

            return 0;
        }

        private bool GetBoolValue(object value)
        {
            if (value == null) return false;

            var strValue = value.ToString().Trim();
            return strValue == "1" ||
                   strValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   strValue.Equals("نعم", StringComparison.OrdinalIgnoreCase) ||
                   strValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private DateOnly GetDateOnlyValue(object value)
        {
            if (value == null) return default;

            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime dt))
                return DateOnly.FromDateTime(dt);

            return default;
        }
    }
}