using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class EmployeeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public EmployeeService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        public class EmployeeSearchItem
        {
            public int Id { get; set; }
            public string Label { get; set; } = "";
        }

        public async Task<List<Branch>> GetBranchesAsync()
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            return await _db.Branches
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        /// <summary>
        /// جلب أنواع الاستحقاقات المرتبطة بالموظف
        /// </summary>
        public async Task<List<int>> GetEmployeeBenefitTypeIdsAsync(int employeeId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.EmployeeBenefits
                .Where(eb => eb.UserId == employeeId)
                .Select(eb => eb.BenefitTypeId)
                .ToListAsync();
        }

        /// <summary>
        /// حفظ أنواع الاستحقاقات المرتبطة بالموظف
        /// </summary>
        public async Task SaveEmployeeBenefitsAsync(int employeeId, List<int> benefitTypeIds)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            // حذف القديم
            var existing = await db.EmployeeBenefits
                .Where(eb => eb.UserId == employeeId)
                .ToListAsync();
            db.EmployeeBenefits.RemoveRange(existing);

            // إضافة الجديد
            foreach (var typeId in benefitTypeIds)
            {
                db.EmployeeBenefits.Add(new EmployeeBenefit
                {
                    UserId = employeeId,
                    BenefitTypeId = typeId,
                    CreatedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task<EmployeeDetailDto?> GetEmployeeDetailAsync(int userId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => new EmployeeDetailDto
                {
                    Id = u.Id,
                    Code = u.Code ?? "",
                    FullName = u.FullName ?? "غير معروف",
                    MainSalary = u.MainSalary ?? 0,
                    FixedSalary = u.FixedSalary,    // ⬅️ جديد
                    HourlyRate = u.HourlyRate,      // ⬅️ جديد
                    MaxLoanAmount = u.MaxLoanAmount,
                    CanTakeLoan = u.CanTakeLoan
                })
                .FirstOrDefaultAsync();

            return user;
        }

        public async Task<EmployeeSalaryInfo?> GetEmployeeSalaryInfoAsync(int employeeId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var employee = await db.Users
                .Where(u => u.Id == employeeId)
                .Select(u => new EmployeeSalaryInfo
                {
                    EmployeeId = u.Id,
                    EmployeeName = u.FullName,
                    EmployeeCode = u.Code,
                    SalaryType = u.SalaryType,
                    FixedSalary = u.FixedSalary,
                    HourlyRate = u.HourlyRate,
                    MonthlyWorkingHours = u.MonthlyWorkingHours,
                    DailyWorkingHours = u.DailyWorkingHours,
                    WorkingDaysPerMonth = u.WorkingDaysPerMonth,
                    MainSalary = u.MainSalary,
                    MinSalary = u.MinSalary,
                    MaxLoanAmount = u.MaxLoanAmount,
                    CanTakeLoan = u.CanTakeLoan
                })
                .FirstOrDefaultAsync();

            return employee;
        }

        public async Task<List<EmployeeSearchItem>> SearchAsync(string? term, int limit = 15)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Users.Where(u => !u.IsArchived).AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(u => u.FullName.Contains(term) || u.Code.Contains(term));

            return await query
                .OrderBy(u => u.FullName)
                .Take(limit)
                .Select(u => new EmployeeSearchItem { Id = u.Id, Label = u.Code + " - " + u.FullName })
                .ToListAsync();
        }
        public async Task<EmployeeLookups> GetLookupsAsync()
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            return new EmployeeLookups
            {
                Branches = await _db.Branches.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Departments = await _db.Departments.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                JobTitles = await _db.JobTitles.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Degrees = await _db.Degrees.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Shifts = await _db.Shifts.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Breaks = await _db.Breaks.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                WeekHolidays = await _db.WeekHolidays.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                JobTypes = await _db.JobTypes.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Qualifications = await _db.Qualifications.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Areas = await _db.Areas.Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
                Managers = await _db.Users.Where(u => !u.IsArchived).Select(x => new ValueTuple<int, string>(x.Id, x.FullName)).ToListAsync(),
            };
        }

        public async Task<EmployeeFormModel?> GetByIdAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var u = await _db.Users.FindAsync(id);
            if (u == null) return null;

            return new EmployeeFormModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Code = u.Code,
                NationalID = u.NationalID,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                Address = u.Address,
                BirthDate = u.BirthDate,
                Gender = u.Gender,
                HireDate = u.HireDate,
                BranchId = u.BranchId,
                DepartmentId = u.DepartmentId,
                JobTitleId = u.JobTitleId,
                DegreeId = u.DegreeId,
                ManagerId = u.ManagerId,
                ShiftId = u.ShiftId,
                BreakId = u.BreakId,
                WeekHolidayId = u.WeekHolidayId,
                JobTypeId = u.JobTypeId,
                QualificationId = u.QualificationId,
                AreaId = u.AreaId,
                WorkHours = u.WorkHours,
                UnderTraining = u.UnderTraining,
                UnderEmployment = u.UnderEmployment,
                InDuty = u.InDuty,
                FinishJob = u.FinishJob,
                MainSalary = u.MainSalary,
                MinSalary = u.MinSalary,
                MaxLoanAmount = u.MaxLoanAmount,
                CanTakeLoan = u.CanTakeLoan,
                HolidayBalance = u.HolidayBalance,
                ExemptLate = u.ExemptLate,
                ExemptEarlyLeave = u.ExemptEarlyLeave,
                ExemptOvertime = u.ExemptOvertime,
                ExemptAbsence = u.ExemptAbsence,
                ExemptEarlyEnter = u.ExemptEarlyEnter,
                NationalIDExpiration = u.NationalIDExpiration,
                DriverLicenseExpiration = u.DriverLicenseExpiration,
                VehicleLicenseExpiration = u.VehicleLicenseExpiration,
                ArmyCertificateExpiration = u.ArmyCertificateExpiration,
                ArmyCertificateNumber = u.ArmyCertificateNumber,
                SSN = u.SSN,
                HealthInsuranceNumber = u.HealthInsuranceNumber,
                Username = u.Username,
                IsUser = u.IsUser,
                IsMobileUser = u.IsMobileUser ?? false,
                Blacklist = u.Blacklist,
                BlacklistReason = u.BlacklistReason,
                MaritalId = u.MaritalId,
                RecidenceId = u.RecidenceId,
                InsuredId = u.InsuredId ?? 0,
                FixedSalary = u.FixedSalary,
                HourlyRate = u.HourlyRate,
                MonthlyWorkingHours = u.MonthlyWorkingHours,
                SalaryType = u.SalaryType,
                DailyWorkingHours = u.DailyWorkingHours,
                WorkingDaysPerMonth = u.WorkingDaysPerMonth
            };
        }

        public async Task SaveAsync(EmployeeFormModel m)
        {
            User u;
            using var _db = await _dbFactory.CreateDbContextAsync();
            if (m.Id.HasValue)
            {
                u = await _db.Users.FindAsync(m.Id.Value) ?? throw new Exception("الموظف غير موجود");
            }
            else
            {
                u = new User { CreatedAt = DateTime.Now };
                _db.Users.Add(u);
            }

            u.FullName = m.FullName; u.Code = m.Code; u.NationalID = m.NationalID;
            u.PhoneNumber = m.PhoneNumber; u.Email = m.Email; u.Address = m.Address;
            u.BirthDate = m.BirthDate; u.Gender = m.Gender; u.HireDate = m.HireDate;
            u.BranchId = m.BranchId; u.DepartmentId = m.DepartmentId; u.JobTitleId = m.JobTitleId;
            u.DegreeId = m.DegreeId; u.ManagerId = m.ManagerId; u.ShiftId = m.ShiftId; u.BreakId = m.BreakId;
            u.WeekHolidayId = m.WeekHolidayId; u.JobTypeId = m.JobTypeId; u.QualificationId = m.QualificationId;
            u.AreaId = m.AreaId; u.WorkHours = m.WorkHours; u.UnderTraining = m.UnderTraining;
            u.UnderEmployment = m.UnderEmployment; u.InDuty = m.InDuty; u.FinishJob = m.FinishJob;
            u.MainSalary = m.MainSalary; u.MinSalary = m.MinSalary; u.MaxLoanAmount = m.MaxLoanAmount;
            u.CanTakeLoan = m.CanTakeLoan; u.HolidayBalance = m.HolidayBalance;
            u.ExemptLate = m.ExemptLate; u.ExemptEarlyLeave = m.ExemptEarlyLeave;
            u.ExemptOvertime = m.ExemptOvertime; u.ExemptAbsence = m.ExemptAbsence; u.ExemptEarlyEnter = m.ExemptEarlyEnter;
            u.NationalIDExpiration = m.NationalIDExpiration; u.DriverLicenseExpiration = m.DriverLicenseExpiration;
            u.VehicleLicenseExpiration = m.VehicleLicenseExpiration; u.ArmyCertificateExpiration = m.ArmyCertificateExpiration;
            u.ArmyCertificateNumber = m.ArmyCertificateNumber; u.SSN = m.SSN; u.HealthInsuranceNumber = m.HealthInsuranceNumber;
            u.Username = m.Username; u.IsUser = m.IsUser; u.IsMobileUser = m.IsMobileUser;
            u.Blacklist = m.Blacklist; u.BlacklistReason = m.BlacklistReason;
            u.MaritalId = m.MaritalId; u.RecidenceId = m.RecidenceId; u.InsuredId = m.InsuredId;
            u.UpdatedAt = DateTime.Now;


            u.SalaryType = m.SalaryType;
            u.MonthlyWorkingHours = m.MonthlyWorkingHours;
            u.DailyWorkingHours = m.DailyWorkingHours;
            u.WorkingDaysPerMonth = m.WorkingDaysPerMonth;
            u.FixedSalary = m.FixedSalary;
            u.HourlyRate = m.HourlyRate;
            u.MainSalary = m.TotalSalary; // إجمالي الراتب
            u.MinSalary = m.MinuteRate; // سعر الدقيقة

            if (m.SelectedBenefitTypeIds?.Count > 0)
            {
                // حذف القديم
                var existingBenefits = await _db.EmployeeBenefits
                    .Where(eb => eb.UserId == u.Id)
                    .ToListAsync();
                _db.EmployeeBenefits.RemoveRange(existingBenefits);

                // إضافة الجديد
                foreach (var typeId in m.SelectedBenefitTypeIds)
                {
                    _db.EmployeeBenefits.Add(new EmployeeBenefit
                    {
                        UserId = u.Id,
                        BenefitTypeId = typeId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private decimal CalculateMonthlySalary(EmployeeFormModel m)
        {
            return m.SalaryType switch
            {
                SalaryTypeEnum.Fixed => m.FixedSalary ?? 0,
                SalaryTypeEnum.MonthlyHourly => (m.HourlyRate ?? 0) * (m.MonthlyWorkingHours ?? 208),
                SalaryTypeEnum.DailyHourly => (m.HourlyRate ?? 0) * (m.DailyWorkingHours ?? 8) * (m.WorkingDaysPerMonth ?? 26),
                _ => m.FixedSalary ?? m.MainSalary ?? 0
            };
        }

        private decimal CalculateMinuteRate(decimal monthlySalary, decimal monthlyWorkingHours)
        {
            if (monthlySalary <= 0 || monthlyWorkingHours <= 0) return 0;
            decimal totalMinutes = monthlyWorkingHours * 60;
            return monthlySalary / totalMinutes;
        }

        public class EmployeeListItem
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string FullName { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string DepartmentName { get; set; } = "";
            public string JobTitleName { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public bool IsArchived { get; set; }
        }

        public async Task<List<EmployeeListItem>> GetListAsync(string? search, int? branchId, bool includeArchived)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(u => !u.IsArchived);

            if (branchId.HasValue)
                query = query.Where(u => u.BranchId == branchId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Code.Contains(search) || u.PhoneNumber.Contains(search));

            return await query
                .OrderBy(u => u.FullName)
                .Select(u => new EmployeeListItem
                {
                    Id = u.Id,
                    Code = u.Code,
                    FullName = u.FullName,
                    BranchName = u.Branch.Name,
                    DepartmentName = u.Department.Name,
                    JobTitleName = u.JobTitle.Name,
                    PhoneNumber = u.PhoneNumber,
                    IsArchived = u.IsArchived
                })
                .ToListAsync();
        }

        public async Task ToggleArchiveAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var u = await _db.Users.FindAsync(id) ?? throw new Exception("الموظف غير موجود");
            u.IsArchived = !u.IsArchived;
            u.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<EmployeeListItem>> GetPagedListAsync(
            string? search, int? branchId, bool includeArchived, int page, int pageSize)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .AsQueryable();

            if (!includeArchived)
                query = query.Where(u => !u.IsArchived);

            if (branchId.HasValue)
                query = query.Where(u => u.BranchId == branchId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Code.Contains(search) || u.PhoneNumber.Contains(search));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new EmployeeListItem
                {
                    Id = u.Id,
                    Code = u.Code,
                    FullName = u.FullName,
                    BranchName = u.Branch.Name,
                    DepartmentName = u.Department.Name,
                    JobTitleName = u.JobTitle.Name,
                    PhoneNumber = u.PhoneNumber,
                    IsArchived = u.IsArchived
                })
                .ToListAsync();

            return new PagedResult<EmployeeListItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<EmployeeListItem>> GetPagedListAsync(EmployeeFilterModel filter, int page, int pageSize)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .AsQueryable();

            if (!filter.IncludeArchived)
                query = query.Where(u => !u.IsArchived);

            if (filter.BranchId.HasValue)
                query = query.Where(u => u.BranchId == filter.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u => u.FullName.Contains(filter.Search) || u.Code.Contains(filter.Search) || u.PhoneNumber.Contains(filter.Search));

            if (filter.DepartmentId.HasValue)
                query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);

            if (filter.JobTitleId.HasValue)
                query = query.Where(u => u.JobTitleId == filter.JobTitleId.Value);

            if (filter.Gender.HasValue)
                query = query.Where(u => u.Gender == filter.Gender.Value);

            if (filter.MaritalId.HasValue)
                query = query.Where(u => u.MaritalId == filter.MaritalId.Value);

            if (filter.InsuredId.HasValue)
                query = query.Where(u => u.InsuredId == filter.InsuredId.Value);

            if (filter.RecidenceId.HasValue)
                query = query.Where(u => u.RecidenceId == filter.RecidenceId.Value);

            if (filter.DegreeId.HasValue)
                query = query.Where(u => u.DegreeId == filter.DegreeId.Value);

            if (filter.QualificationId.HasValue)
                query = query.Where(u => u.QualificationId == filter.QualificationId.Value);

            if (filter.AreaId.HasValue)
                query = query.Where(u => u.AreaId == filter.AreaId.Value);

            if (filter.InDuty.HasValue)
                query = query.Where(u => u.InDuty == filter.InDuty.Value);

            if (filter.UnderTraining.HasValue)
                query = query.Where(u => u.UnderTraining == filter.UnderTraining.Value);

            if (filter.Blacklist.HasValue)
                query = query.Where(u => u.Blacklist == filter.Blacklist.Value);

            if (filter.HireDateFrom.HasValue)
                query = query.Where(u => u.HireDate >= filter.HireDateFrom.Value);

            if (filter.HireDateTo.HasValue)
                query = query.Where(u => u.HireDate <= filter.HireDateTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.PhoneNumber))
                query = query.Where(u => u.PhoneNumber.Contains(filter.PhoneNumber));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new EmployeeListItem
                {
                    Id = u.Id,
                    Code = u.Code,
                    FullName = u.FullName,
                    BranchName = u.Branch.Name,
                    DepartmentName = u.Department.Name,
                    JobTitleName = u.JobTitle.Name,
                    PhoneNumber = u.PhoneNumber,
                    IsArchived = u.IsArchived
                })
                .ToListAsync();

            return new PagedResult<EmployeeListItem> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }
    }
}