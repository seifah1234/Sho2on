using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows; using HR_Application.Helpers;
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
                throw new FileNotFoundException("„·› Excel €Ì— „ÊÃÊœ.", filePath);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new Exception("·„ Ì „ «·⁄ÀÊ— ⁄·Ï √Ì Ê—ﬁ… ⁄„· ›Ì „·› Excel.");

            var usersFromExcel = new List<User>();
            var idsFromExcel = new HashSet<string>();
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int importedCount = 0;
            int successCount = 0;
            int errorCount = 0;

            // √Ê·«: ﬁ—«¡… Ã„Ì⁄ «·„” Œœ„Ì‰ „‰ Excel „⁄  ÕœÌÀ «· ﬁœ„
            for (int row = 2; row <= rowCount; row++)
            {
                // «· Õﬁﬁ „‰ «·≈·€«¡
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(" „ ≈·€«¡ ⁄„·Ì… «·«” Ì—«œ.");
                }

                try
                {
                    //  ÕœÌÀ «· ﬁœ„
                    if (_progressCallback != null)
                    {
                        bool shouldContinue = _progressCallback(row - 1, rowCount - 1,
                            $"Ã«—Ì ﬁ—«¡… «·’› {row - 1} „‰ {rowCount - 1}");
                        if (!shouldContinue) break;
                    }

                    var user = MapRowToUser(worksheet, row);

                    if (user != null)
                    {
                        if (idsFromExcel.Contains(user.Code))
                        {
                            errorCount++;
                            continue;
                        }

                        usersFromExcel.Add(user);
                        idsFromExcel.Add(user.Code);
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Œÿ√ ›Ì ﬁ—«¡… «·’› {row}: {ex.Message}");
                }
            }

            //  ÕœÌÀ Õ«·… «· ﬁœ„
            if (_progressCallback != null)
            {
                _progressCallback(importedCount, importedCount, "Ã«—Ì „⁄«·Ã… «·»Ì«‰« ...");
            }

            // À«‰Ì«: Ã·» «·„” Œœ„Ì‰ «·„ÊÃÊœÌ‰
            var existingUserIds = await _context.Users
                .Where(u => idsFromExcel.Contains(u.Code))
                .Select(u => u.Code)
                .ToListAsync();

            // À«·À«: ›’· «·„” Œœ„Ì‰
            var newUsers = new List<User>();
            var existingUsersMap = await _context.Users
                .Where(u => existingUserIds.Contains(u.Code))
                .ToDictionaryAsync(u => u.Code);

            int current = 0;
            foreach (var user in usersFromExcel)
            {
                current++;

                //  ÕœÌÀ «· ﬁœ„
                if (_progressCallback != null)
                {
                    _progressCallback(current, usersFromExcel.Count,
                        $"Ã«—Ì „⁄«·Ã… «·„ÊŸ› {current} „‰ {usersFromExcel.Count}");
                }

                if (existingUsersMap.ContainsKey(user.Code))
                {
                    continue;
                }
                else
                {
                    // ≈÷«›… „” Œœ„ ÃœÌœ
                    newUsers.Add(user);
                }
            }

            //  ÕœÌÀ Õ«·… «· ﬁœ„
            if (_progressCallback != null)
            {
                _progressCallback(usersFromExcel.Count, usersFromExcel.Count,
                    "Ã«—Ì Õ›Ÿ «·»Ì«‰«  ›Ì ﬁ«⁄œ… «·»Ì«‰« ...");
            }

            // —«»⁄«: ≈÷«›… «·„” Œœ„Ì‰ «·Ãœœ
            await _context.Users.AddRangeAsync(newUsers);

            try
            {
                var result = await _context.SaveChangesAsync();
                successCount = newUsers.Count + existingUsersMap.Count;
                LocalizationManager.ShowMessage(errorCount.ToString());

                return successCount;
            }
            catch (DbUpdateException ex)
            {
                //  ”ÃÌ· «· ›«’Ì· ··√Œÿ«¡
                Console.WriteLine($"Œÿ√ ›Ì Õ›Ÿ «·»Ì«‰« : {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($" ›«’Ì· «·Œÿ√: {ex.InnerException.Message}");
                LocalizationManager.ShowMessage(ex.InnerException.Message);

                throw;
            }
        }

        private User? MapRowToUser(ExcelWorksheet worksheet, int row)
        {
            // ≈–« ﬂ«‰ «·⁄„Êœ «·√Ê· ›«—€«° ‰ Ã«Â· «·’›
            if (worksheet.Cells[row, 1].Value == null)
                return null;

            var user = new User();

            // Col 2:  Code
            user.Code = worksheet.Cells[row, 2].Value?.ToString();

            // Col 3:  NationalID
            user.NationalID = worksheet.Cells[row, 3].Value?.ToString() ?? "";

            // Col 4:  PhoneNumber
            user.PhoneNumber = worksheet.Cells[row, 4].Value?.ToString() ?? "";

            // Col 5:  FullName
            user.FullName = worksheet.Cells[row, 5].Value?.ToString() ?? "";

            // Col 6:  Email
            user.Email = worksheet.Cells[row, 6].Value?.ToString();

            // Col 7:  Address
            user.Address = worksheet.Cells[row, 7].Value?.ToString();

            // Col 8:  HireDate
            user.HireDate = GetDateOnlyValue(worksheet.Cells[row, 8].Value);

            // Col 9:  BirthDate
            user.BirthDate = GetDateOnlyValue(worksheet.Cells[row, 9].Value);

            // Col 10: MainSalary
            user.MainSalary = GetDecimalValue(worksheet.Cells[row, 10].Value);

            // Col 11: MinSalary
            user.MinSalary = GetDecimalValue(worksheet.Cells[row, 11].Value);

            // Col 12: Gender
            var gender = worksheet.Cells[row, 12].Value?.ToString();
            user.Gender = gender?.Length > 0 ? gender[0] : 'M';

            // Col 13: AreaId
            user.AreaId = GetNullableIntValue(worksheet.Cells[row, 13].Value);

            // Col 14: BranchId
            user.BranchId = GetIntValue(worksheet.Cells[row, 14].Value, 1);

            // Col 15: DepartmentId
            user.DepartmentId = GetIntValue(worksheet.Cells[row, 15].Value, 1);

            // Col 16: JobTitleId
            user.JobTitleId = GetIntValue(worksheet.Cells[row, 16].Value, 1);

            // Col 17: DegreeId
            user.DegreeId = GetIntValue(worksheet.Cells[row, 17].Value, 1);

            // Col 18: ShiftId
            user.ShiftId = GetIntValue(worksheet.Cells[row, 18].Value, 1);

            // Col 19: BreakId
            user.BreakId = GetIntValue(worksheet.Cells[row, 19].Value, 1);

            // Col 20: WeekHolidayId
            user.WeekHolidayId = GetIntValue(worksheet.Cells[row, 20].Value, 15);

            // Col 21: JobTypeId
            user.JobTypeId = GetIntValue(worksheet.Cells[row, 21].Value, 1);

            // Col 22: ExemptLate
            user.ExemptLate = GetBoolValue(worksheet.Cells[row, 22].Value);

            // Col 23: ExemptEarlyLeave
            user.ExemptEarlyLeave = GetBoolValue(worksheet.Cells[row, 23].Value);

            // Col 24: ExemptOvertime
            user.ExemptOvertime = GetBoolValue(worksheet.Cells[row, 24].Value);

            // Col 25: ExemptAbsence
            user.ExemptAbsence = GetBoolValue(worksheet.Cells[row, 25].Value);

            // Col 26: ExemptEarlyEnter
            user.ExemptEarlyEnter = GetBoolValue(worksheet.Cells[row, 26].Value);

            // Col 27: WorkHours
            var workHoursValue = worksheet.Cells[row, 27].Value;
            if (workHoursValue != null)
            {
                if (workHoursValue is string timeStr && TimeSpan.TryParse(timeStr, out TimeSpan time))
                    user.WorkHours = time;
                else if (workHoursValue is double hoursDouble)
                    user.WorkHours = TimeSpan.FromHours(hoursDouble);
                else if (double.TryParse(workHoursValue.ToString(), out double hours))
                    user.WorkHours = TimeSpan.FromHours(hours);
            }

            // Col 28: InDuty
            user.InDuty = GetBoolValue(worksheet.Cells[row, 28].Value);

            // Col 29: InsuredId
            user.InsuredId = GetIntValue(worksheet.Cells[row, 29].Value, 0);

            // Col 30: HolidayBalance
            user.HolidayBalance = GetIntValue(worksheet.Cells[row, 30].Value, 0);

            // Col 31: Blacklist
            user.Blacklist = GetBoolValue(worksheet.Cells[row, 31].Value);

            // Col 32: BlacklistReason
            user.BlacklistReason = worksheet.Cells[row, 32].Value?.ToString();

            // Col 33: UnderTraining
            user.UnderTraining = GetBoolValue(worksheet.Cells[row, 33].Value);

            // Col 34: UnderEmployment
            user.UnderEmployment = GetBoolValue(worksheet.Cells[row, 34].Value);

            // Col 35: IsArchived
            user.IsArchived = GetBoolValue(worksheet.Cells[row, 35].Value);

            // Col 36: FinishJob
            user.FinishJob = GetDateOnlyValue(worksheet.Cells[row, 36].Value);

            // Col 37: DriverLicenseExpiration
            user.DriverLicenseExpiration = GetDateOnlyValue(worksheet.Cells[row, 37].Value);

            // Col 38: VehicleLicenseExpiration
            user.VehicleLicenseExpiration = GetDateOnlyValue(worksheet.Cells[row, 38].Value);

            // Col 39: NationalIDExpiration
            user.NationalIDExpiration = GetDateOnlyValue(worksheet.Cells[row, 39].Value);

            // Col 40: ArmyCertificateExpiration
            user.ArmyCertificateExpiration = GetDateOnlyValue(worksheet.Cells[row, 40].Value);

            // Col 41: ArmyCertificateNumber
            user.ArmyCertificateNumber = worksheet.Cells[row, 41].Value?.ToString();

            // Col 42: SSN
            user.SSN = worksheet.Cells[row, 42].Value?.ToString();

            // Col 43: HealthInsuranceNumber
            user.HealthInsuranceNumber = worksheet.Cells[row, 43].Value?.ToString();

            // Col 44: Username
            user.Username = worksheet.Cells[row, 44].Value?.ToString();

            // Col 45: PasswordHash
            user.PasswordHash = worksheet.Cells[row, 45].Value?.ToString();

            // Col 46: IsUser
            user.IsUser = GetBoolValue(worksheet.Cells[row, 46].Value);

            // Col 47: ProfileImageData ó stored separately; skip binary import from Excel
            user.ProfileImageData = null;

            // Col 48: CreatedAt
            var createdAt = worksheet.Cells[row, 48].Value;
            user.CreatedAt = createdAt is DateTime createdDt ? createdDt : DateTime.Now;

            // Col 49: UpdatedAt
            var updatedAt = worksheet.Cells[row, 49].Value;
            user.UpdatedAt = updatedAt is DateTime updatedDt ? updatedDt : DateTime.Now;

            // Col 50: RegisteredDeviceId
            user.RegisteredDeviceId = worksheet.Cells[row, 50].Value?.ToString();

            // Col 51: IsMobileUser
            user.IsMobileUser = GetBoolValue(worksheet.Cells[row, 51].Value);

            // Col 52: MaxLoanAmount
            user.MaxLoanAmount = GetDecimalValue(worksheet.Cells[row, 52].Value);

            // Col 53: CurrentLoanBalance
            user.CurrentLoanBalance = GetDecimalValue(worksheet.Cells[row, 53].Value);

            // Col 54: CanTakeLoan
            user.CanTakeLoan = GetBoolValue(worksheet.Cells[row, 54].Value);

            // Col 55: LoanMaxAmount
            user.LoanMaxAmount = GetDecimalValue(worksheet.Cells[row, 55].Value);

            // Col 56: ManagerId
            user.ManagerId = GetNullableIntValue(worksheet.Cells[row, 56].Value);

            // Col 57: RecidenceId
            user.RecidenceId = GetNullableIntValue(worksheet.Cells[row, 57].Value);

            // Col 58: MaritalId
            user.MaritalId = GetNullableIntValue(worksheet.Cells[row, 58].Value);

            // Col 59: QualificationId
            user.QualificationId = GetNullableIntValue(worksheet.Cells[row, 59].Value);

            return user;
        }

        // œ«·«  „”«⁄œ…
        private int GetIntValue(object value, int defaultValue)
        {
            if (value == null) return defaultValue;

            if (value is int i) return i;
            if (value is double d) return (int)d;
            if (value is decimal dec) return (int)dec;
            if (int.TryParse(value.ToString(), out int result)) return result;

            return defaultValue;
        }

        private int? GetNullableIntValue(object value)
        {
            if (value == null) return null;

            if (value is int i) return i;
            if (value is double d) return (int)d;
            if (value is decimal dec) return (int)dec;
            if (int.TryParse(value.ToString(), out int result)) return result;

            return null;
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
                   strValue.Equals("‰⁄„", StringComparison.OrdinalIgnoreCase) ||
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
