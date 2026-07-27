using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class EmployeeService
    {
        private readonly AppDbContext _db;
        public EmployeeService(AppDbContext db) => _db = db;

        public async Task<EmployeeLookups> GetLookupsAsync()
        {
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
            };
        }

        public async Task SaveAsync(EmployeeFormModel m)
        {
            User u;
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

            await _db.SaveChangesAsync();
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
            var u = await _db.Users.FindAsync(id) ?? throw new Exception("الموظف غير موجود");
            u.IsArchived = !u.IsArchived;
            u.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        public async Task<PagedResult<EmployeeListItem>> GetPagedListAsync(
            string? search, int? branchId, bool includeArchived, int page, int pageSize)
        {
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
    }
}