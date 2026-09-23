using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Components.Pages.Settings;
using Sho2on.Web.Models;
using ClosedXML.Excel;
using static ClosedXML.Excel.XLColor;

namespace Sho2on.Web.Services
{
    public class EmployeeService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly CurrentUserService _currentUser;
        public EmployeeService(IDbContextFactory<AppDbContext> dbFactory, CurrentUserService currentUser)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;
        }

        public class EmployeeSearchItem
        {
            public int Id { get; set; }
            public string Label { get; set; } = "";
        }

        // ============================================================
//  Audit Log — جلب وعرض
// ============================================================

public class EmployeeAuditListItem
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string? EmployeeCode { get; set; }
    public string ChangedByName { get; set; } = "";
    public AuditAction Action { get; set; }
    public string ActionLabel { get; set; } = "";
    public int ChangesCount { get; set; }
    public string? Summary { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class EmployeeAuditFilter
{
    public int? EmployeeId { get; set; }
    public string? Search { get; set; }        // بحث في اسم الموظف / كوده
    public AuditAction? Action { get; set; }
    public int? ChangedById { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

private static IQueryable<EmployeeAuditLog> ApplyAuditFilter(
    IQueryable<EmployeeAuditLog> q, EmployeeAuditFilter f)
{
    if (f.EmployeeId.HasValue)
        q = q.Where(x => x.EmployeeId == f.EmployeeId.Value);

    if (f.ChangedById.HasValue)
        q = q.Where(x => x.ChangedById == f.ChangedById.Value);

    if (f.Action.HasValue)
        q = q.Where(x => x.Action == f.Action.Value);

    if (f.FromDate.HasValue)
        q = q.Where(x => x.ChangedAt >= f.FromDate.Value);

    if (f.ToDate.HasValue)
    {
        var to = f.ToDate.Value.Date.AddDays(1).AddTicks(-1);
        q = q.Where(x => x.ChangedAt <= to);
    }

    if (!string.IsNullOrWhiteSpace(f.Search))
    {
        var s = f.Search.Trim();
        q = q.Where(x => x.EmployeeName.Contains(s)
                      || (x.EmployeeCode != null && x.EmployeeCode.Contains(s))
                      || x.ChangedByName.Contains(s));
    }

    return q;
}

private static string ActionLabel(AuditAction a) => a switch
{
    AuditAction.Create => "إضافة",
    AuditAction.Update => "تعديل",
    AuditAction.Archive => "أرشفة",
    AuditAction.Restore => "استعادة",
    AuditAction.Delete => "حذف",
    _ => a.ToString()
};

public async Task<PagedResult<EmployeeAuditListItem>> GetAuditLogPagedAsync(
    EmployeeAuditFilter filter, int page, int pageSize)
{
    using var db = await _dbFactory.CreateDbContextAsync();

    var q = db.EmployeeAuditLogs.AsQueryable();
    q = ApplyAuditFilter(q, filter);

    var total = await q.CountAsync();

    var items = await q
        .OrderByDescending(x => x.ChangedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new EmployeeAuditListItem
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            EmployeeName = x.EmployeeName,
            EmployeeCode = x.EmployeeCode,
            ChangedByName = x.ChangedByName,
            Action = x.Action,
            ChangesCount = x.ChangesJson == null ? 0
                : System.Text.Json.JsonSerializer.Deserialize<List<FieldChange>>(x.ChangesJson, (System.Text.Json.JsonSerializerOptions?)null)!.Count,
            Summary = x.ChangesJson,
            Notes = x.Notes,
            ChangedAt = x.ChangedAt
        })
        .ToListAsync();

    foreach (var item in items)
        item.ActionLabel = ActionLabel(item.Action);

    return new PagedResult<EmployeeAuditListItem>
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}

/// <summary>جلب تفاصيل التعديلات لحقل واحد</summary>
public async Task<List<FieldChange>> GetAuditChangesAsync(int auditId)
{
    using var db = await _dbFactory.CreateDbContextAsync();
    var log = await db.EmployeeAuditLogs.FindAsync(auditId);
    if (log == null || string.IsNullOrEmpty(log.ChangesJson))
        return new List<FieldChange>();

    return System.Text.Json.JsonSerializer.Deserialize<List<FieldChange>>(log.ChangesJson)
           ?? new List<FieldChange>();
}

/// <summary>تصدير سجل التعديلات لملف Excel</summary>
public async Task<byte[]> ExportAuditToExcelAsync(EmployeeAuditFilter filter)
{
    using var db = await _dbFactory.CreateDbContextAsync();

    var q = db.EmployeeAuditLogs.AsQueryable();
    q = ApplyAuditFilter(q, filter);

    var logs = await q
        .OrderByDescending(x => x.ChangedAt)
        .Take(5000)     // حد أقصى للتصدير
        .ToListAsync();

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("سجل التعديلات");
    ws.RightToLeft = true;

    var headers = new[]
    {
        "#", "التاريخ والوقت", "الموظف", "الكود", "العملية",
        "بواسطة", "عدد الحقول المتغيرة", "تفاصيل التغييرات", "ملاحظات"
    };

    // العنوان
    ws.Cell(1, 1).Value = $"سجل تعديلات الموظفين - {DateTime.Now:yyyy/MM/dd HH:mm}";
    var titleRange = ws.Range(1, 1, 1, headers.Length);
    titleRange.Merge();
    titleRange.Style
        .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.White)
        .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A5F"))
        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    ws.Row(1).Height = 34;

    // الهيدر
    const int headerRow = 2;
    for (int i = 0; i < headers.Length; i++)
    {
        var c = ws.Cell(headerRow, i + 1);
        c.Value = headers[i];
        c.Style
            .Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2E5C8A"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }
    ws.Row(headerRow).Height = 26;

    int row = headerRow + 1;
    int idx = 1;

    foreach (var log in logs)
    {
        var changes = string.IsNullOrEmpty(log.ChangesJson)
            ? new List<FieldChange>()
            : System.Text.Json.JsonSerializer.Deserialize<List<FieldChange>>(log.ChangesJson) ?? new();

        var details = string.Join(" | ", changes.Select(c =>
            $"{c.FieldLabel}: {c.OldValue ?? "—"} ➜ {c.NewValue ?? "—"}"));

        ws.Cell(row, 1).Value = idx;
        ws.Cell(row, 2).Value = log.ChangedAt;
        ws.Cell(row, 2).Style.DateFormat.Format = "yyyy/MM/dd HH:mm";
        ws.Cell(row, 3).Value = log.EmployeeName;
        ws.Cell(row, 4).Value = log.EmployeeCode ?? "";
        ws.Cell(row, 5).Value = ActionLabel(log.Action);
        ws.Cell(row, 6).Value = log.ChangedByName;
        ws.Cell(row, 7).Value = changes.Count;
        ws.Cell(row, 8).Value = details;
        ws.Cell(row, 9).Value = log.Notes ?? "";

        // لون حسب العملية
        var opColor = log.Action switch
        {
            AuditAction.Create => "#D1FAE5",
            AuditAction.Update => "#DBEAFE",
            AuditAction.Archive => "#FEF3C7",
            AuditAction.Restore => "#D1FAE5",
            AuditAction.Delete => "#FEE2E2",
            _ => "#FFFFFF"
        };
        ws.Range(row, 1, row, headers.Length).Style
            .Fill.SetBackgroundColor(XLColor.FromHtml(opColor));

        row++;
        idx++;
    }

    int lastRow = row - 1;

    if (lastRow >= headerRow + 1)
    {
        var dataRange = ws.Range(headerRow, 1, lastRow, headers.Length);
        dataRange.Style
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Font.SetFontSize(10.5);

        ws.Range(headerRow, 1, lastRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(headerRow, 5, lastRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Range(headerRow, 8, lastRow, 8).Style.Alignment.SetWrapText(true);
        ws.Range(headerRow, 8, lastRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
    }

    ws.SheetView.FreezeRows(headerRow);
    if (lastRow >= headerRow + 1)
        ws.Range(headerRow, 1, lastRow, headers.Length).SetAutoFilter();

    ws.Column(1).Width = 6;
    ws.Column(2).Width = 18;
    ws.Column(3).Width = 28;
    ws.Column(4).Width = 12;
    ws.Column(5).Width = 12;
    ws.Column(6).Width = 22;
    ws.Column(7).Width = 16;
    ws.Column(8).Width = 70;
    ws.Column(9).Width = 30;

    using var stream = new MemoryStream();
    wb.SaveAs(stream);
    return stream.ToArray();
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
                EndDutyTypes = await _db.Reasons.Where(x => x.ReasonType == ReasonType.EndDuty).Select(x => new ValueTuple<int, string>(x.Id, x.Name)).ToListAsync(),
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



        /// <summary>
        /// تصدير قائمة الموظفين (حسب الفلتر الحالي) لملف Excel منسّق
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(EmployeeFilterModel filter)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();

            var query = _db.Users
                .Include(u => u.Branch)
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Include(u => u.Degree)
                .Include(u => u.Qualification)
                .Include(u => u.Area)
                .AsQueryable();

            // نفس الفلاتر اللي في GetPagedListAsync
            if (!filter.IncludeArchived)
                query = query.Where(u => !u.IsArchived);

            if (filter.BranchId.HasValue)
                query = query.Where(u => u.BranchId == filter.BranchId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u => u.FullName.Contains(filter.Search)
                                    || u.Code.Contains(filter.Search)
                                    || u.PhoneNumber.Contains(filter.Search));

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

            var employees = await query
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Code,
                    u.FullName,
                    BranchName = u.Branch != null ? u.Branch.Name : "",
                    DepartmentName = u.Department != null ? u.Department.Name : "",
                    JobTitleName = u.JobTitle != null ? u.JobTitle.Name : "",
                    DegreeName = u.Degree != null ? u.Degree.Name : "",
                    QualificationName = u.Qualification != null ? u.Qualification.Name : "",
                    AreaName = u.Area != null ? u.Area.Name : "",
                    u.PhoneNumber,
                    u.NationalID,
                    u.Email,
                    u.HireDate,
                    u.MainSalary,
                    u.InDuty,
                    u.UnderTraining,
                    u.Blacklist,
                    u.IsArchived
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("الموظفون");

            // اتجاه الشيت من اليمين لليسار
            ws.RightToLeft = true;

            // ===== العنوان الرئيسي =====
            var headers = new[]
            {
                "الكود", "الاسم", "الفرع", "الإدارة", "الوظيفة", "الدرجة",
                "المؤهل", "المنطقة", "الموبايل", "الرقم القومي", "البريد الإلكتروني",
                "تاريخ التعيين", "الراتب الأساسي", "في الخدمة", "تحت التدريب",
                "قائمة سوداء", "مؤرشف"
            };

            // عنوان فوق الجدول
            ws.Cell(1, 1).Value = $"تقرير الموظفين - {DateTime.Now:yyyy/MM/dd HH:mm}";
            var titleRange = ws.Range(1, 1, 1, headers.Length);
            titleRange.Merge();
            titleRange.Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A5F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Row(1).Height = 34;

            // ===== صف الهيدر =====
            const int headerRow = 2;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold()
                    .Font.SetFontSize(11)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2E5C8A"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetBottomBorder(XLBorderStyleValues.Medium)
                    .Border.SetBottomBorderColor(XLColor.White);
            }
            ws.Row(headerRow).Height = 26;

            // ===== صفوف البيانات =====
            int row = headerRow + 1;
            foreach (var e in employees)
            {
                ws.Cell(row, 1).Value = e.Code ?? "";
                ws.Cell(row, 2).Value = e.FullName ?? "";
                ws.Cell(row, 3).Value = e.BranchName;
                ws.Cell(row, 4).Value = e.DepartmentName;
                ws.Cell(row, 5).Value = e.JobTitleName;
                ws.Cell(row, 6).Value = e.DegreeName;
                ws.Cell(row, 7).Value = e.QualificationName;
                ws.Cell(row, 8).Value = e.AreaName;
                ws.Cell(row, 9).Value = e.PhoneNumber ?? "";
                ws.Cell(row, 10).Value = e.NationalID ?? "";
                ws.Cell(row, 11).Value = e.Email ?? "";
                ws.Cell(row, 12).Value = e.HireDate.ToString();
                ws.Cell(row, 12).Style.DateFormat.Format = "yyyy/MM/dd";

                if (e.MainSalary.HasValue)
                    ws.Cell(row, 13).Value = e.MainSalary.Value;
                ws.Cell(row, 13).Style.NumberFormat.Format = "#,##0.00";

                ws.Cell(row, 14).Value = e.InDuty ? "نعم" : "لا";
                ws.Cell(row, 15).Value = e.UnderTraining ? "نعم" : "لا";
                ws.Cell(row, 16).Value = e.Blacklist ? "نعم" : "لا";
                ws.Cell(row, 17).Value = e.IsArchived ? "نعم" : "لا";

                // تلوين خفيف للصفوف المؤرشفة
                if (e.IsArchived)
                {
                    ws.Range(row, 1, row, headers.Length).Style
                        .Font.SetFontColor(XLColor.Gray)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5"));
                }

                row++;
            }

            int lastRow = row - 1;

            // ===== الحدود والتنسيق العام =====
            if (lastRow >= headerRow + 1)
            {
                var dataRange = ws.Range(headerRow, 1, lastRow, headers.Length);
                dataRange.Style
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#B0BEC5"))
                    .Border.SetInsideBorderColor(XLColor.FromHtml("#CFD8DC"))
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Font.SetFontSize(10.5);

                // الأسماء على اليمين
                ws.Column(2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // تخطيط الصفوف بالتبادل (Zebra)
                for (int r = headerRow + 1; r <= lastRow; r++)
                {
                    if ((r - headerRow) % 2 == 0 && !employees[r - headerRow - 1].IsArchived)
                    {
                        ws.Range(r, 1, r, headers.Length).Style
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                    }
                }
            }

            // ===== صف الإجمالي =====
            int totalRow = lastRow + 1;
            ws.Cell(totalRow, 1).Value = "الإجمالي";
            ws.Range(totalRow, 1, totalRow, 2).Merge();
            ws.Cell(totalRow, 3).Value = employees.Count;
            ws.Range(totalRow, 1, totalRow, headers.Length).Style
                .Font.SetBold()
                .Font.SetFontSize(11)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A5F"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ===== تجميد الهيدر + الفلتر =====
            ws.SheetView.FreezeRows(headerRow);
            ws.Range(headerRow, 1, lastRow, headers.Length).SetAutoFilter();

            // ===== عرض الأعمدة =====
            ws.Column(1).Width = 12;   // الكود
            ws.Column(2).Width = 28;   // الاسم
            ws.Column(3).Width = 18;   // الفرع
            ws.Column(4).Width = 18;   // الإدارة
            ws.Column(5).Width = 20;   // الوظيفة
            ws.Column(6).Width = 15;   // الدرجة
            ws.Column(7).Width = 20;   // المؤهل
            ws.Column(8).Width = 15;   // المنطقة
            ws.Column(9).Width = 16;   // الموبايل
            ws.Column(10).Width = 20;   // الرقم القومي
            ws.Column(11).Width = 26;   // الإيميل
            ws.Column(12).Width = 14;   // تاريخ التعيين
            ws.Column(13).Width = 14;   // الراتب
            ws.Column(14).Width = 11;   // في الخدمة
            ws.Column(15).Width = 12;   // تحت التدريب
            ws.Column(16).Width = 12;   // قائمة سوداء
            ws.Column(17).Width = 10;   // مؤرشف

            // ===== الحفظ كـ Stream =====
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        
        // ============================================================
//  Audit Log — تسجيل كل التعديلات على الموظفين
// ============================================================

/// <summary>
/// بيقارن القيم القديمة بالجديدة ويرجّع اللي اتغير بس
/// </summary>
private static List<FieldChange> ComputeChanges(User oldUser, EmployeeFormModel m)
{
    var changes = new List<FieldChange>();

    void Compare<T>(string field, string label, T? oldVal, T? newVal)
    {
        var oldStr = oldVal?.ToString() ?? "";
        var newStr = newVal?.ToString() ?? "";
        if (oldStr != newStr)
        {
            changes.Add(new FieldChange
            {
                Field = field,
                FieldLabel = label,
                OldValue = oldStr,
                NewValue = newStr
            });
        }
    }

    Compare("FullName", "الاسم", oldUser.FullName, m.FullName);
    Compare("Code", "الكود", oldUser.Code, m.Code);
    Compare("NationalID", "الرقم القومي", oldUser.NationalID, m.NationalID);
    Compare("PhoneNumber", "الموبايل", oldUser.PhoneNumber, m.PhoneNumber);
    Compare("Email", "البريد الإلكتروني", oldUser.Email, m.Email);
    Compare("Address", "العنوان", oldUser.Address, m.Address);
    Compare("BirthDate", "تاريخ الميلاد", oldUser.BirthDate, m.BirthDate);
    Compare("Gender", "النوع", oldUser.Gender, m.Gender);
    Compare("HireDate", "تاريخ التعيين", oldUser.HireDate, m.HireDate);
    Compare("BranchId", "الفرع", oldUser.BranchId, m.BranchId);
    Compare("DepartmentId", "الإدارة", oldUser.DepartmentId, m.DepartmentId);
    Compare("JobTitleId", "الوظيفة", oldUser.JobTitleId, m.JobTitleId);
    Compare("DegreeId", "الدرجة", oldUser.DegreeId, m.DegreeId);
    Compare("ManagerId", "المدير", oldUser.ManagerId, m.ManagerId);
    Compare("ShiftId", "الوردية", oldUser.ShiftId, m.ShiftId);
    Compare("BreakId", "البريك", oldUser.BreakId, m.BreakId);
    Compare("WeekHolidayId", "الراحة الأسبوعية", oldUser.WeekHolidayId, m.WeekHolidayId);
    Compare("JobTypeId", "نوع الوظيفة", oldUser.JobTypeId, m.JobTypeId);
    Compare("QualificationId", "المؤهل", oldUser.QualificationId, m.QualificationId);
    Compare("AreaId", "المنطقة", oldUser.AreaId, m.AreaId);
    Compare("WorkHours", "ساعات العمل", oldUser.WorkHours, m.WorkHours);
    Compare("UnderTraining", "تحت التدريب", oldUser.UnderTraining, m.UnderTraining);
    Compare("UnderEmployment", "تحت التوظيف", oldUser.UnderEmployment, m.UnderEmployment);
    Compare("InDuty", "في الخدمة", oldUser.InDuty, m.InDuty);
    Compare("FinishJob", "إنهاء الخدمة", oldUser.FinishJob, m.FinishJob);
    Compare("MainSalary", "الراتب الأساسي", oldUser.MainSalary, m.MainSalary);
    Compare("MinSalary", "سعر الدقيقة", oldUser.MinSalary, m.MinSalary);
    Compare("MaxLoanAmount", "أقصى قرض", oldUser.MaxLoanAmount, m.MaxLoanAmount);
    Compare("CanTakeLoan", "يمكنه أخذ قرض", oldUser.CanTakeLoan, m.CanTakeLoan);
    Compare("HolidayBalance", "رصيد الإجازات", oldUser.HolidayBalance, m.HolidayBalance);
    Compare("Blacklist", "قائمة سوداء", oldUser.Blacklist, m.Blacklist);
    Compare("BlacklistReason", "سبب القائمة السوداء", oldUser.BlacklistReason, m.BlacklistReason);
    Compare("MaritalId", "الحالة الاجتماعية", oldUser.MaritalId, m.MaritalId);
    Compare("RecidenceId", "نوع الإقامة", oldUser.RecidenceId, m.RecidenceId);
    Compare("InsuredId", "التأمين", oldUser.InsuredId, m.InsuredId);
    Compare("SalaryType", "نوع الراتب", oldUser.SalaryType, m.SalaryType);
    Compare("FixedSalary", "راتب ثابت", oldUser.FixedSalary, m.FixedSalary);
    Compare("HourlyRate", "سعر الساعة", oldUser.HourlyRate, m.HourlyRate);
    Compare("MonthlyWorkingHours", "ساعات العمل الشهرية", oldUser.MonthlyWorkingHours, m.MonthlyWorkingHours);
    Compare("DailyWorkingHours", "ساعات العمل اليومية", oldUser.DailyWorkingHours, m.DailyWorkingHours);
    Compare("WorkingDaysPerMonth", "أيام العمل الشهرية", oldUser.WorkingDaysPerMonth, m.WorkingDaysPerMonth);
    Compare("Username", "اسم المستخدم", oldUser.Username, m.Username);
    Compare("IsUser", "مستخدم نظام", oldUser.IsUser, m.IsUser);
    Compare("IsMobileUser", "مستخدم موبايل", oldUser.IsMobileUser, m.IsMobileUser);
    Compare("EndDutyTypeId", "سبب إنهاء الخدمة", oldUser.EndDutyTypeId, m.EndDutyTypeId);
    Compare("EndDutyNotes", "ملاحظات إنهاء الخدمة", oldUser.EndDutyNotes, m.EndDutyNotes);

    return changes;
}

/// <summary>
/// تسجيل عملية في Audit Log
/// </summary>
private static async Task LogAuditAsync(
    AppDbContext db,
    User employee,
    int? changedById,
    string changedByName,
    AuditAction action,
    List<FieldChange>? changes = null,
    string? notes = null)
{
    var log = new EmployeeAuditLog
    {
        EmployeeId = employee.Id,
        EmployeeName = employee.FullName ?? "",
        EmployeeCode = employee.Code,
        ChangedById = changedById,
        ChangedByName = changedByName,
        Action = action,
        ChangesJson = changes != null && changes.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(changes)
            : null,
        Notes = notes,
        ChangedAt = DateTime.Now
    };
    db.EmployeeAuditLogs.Add(log);
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
                ProfileImageData = u.ProfileImageData,
                ProfileImageBase64 = u.ProfileImageData != null ? Convert.ToBase64String(u.ProfileImageData) : null,
                NationalID = u.NationalID,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                Address = u.Address,
                IsFreeLocation = u.IsFreeLocation ?? false,
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
                WorkingDaysPerMonth = u.WorkingDaysPerMonth,
                EndDutyNotes = u.EndDutyNotes,
                EndDutyTypeId = u.EndDutyTypeId
            };
        }

        public async Task SaveAsync(EmployeeFormModel m)
        {
            // حماية على مستوى السيرفر: تعديل موظف موجود يحتاج "بيانات الموظفين"،
            // وإضافة موظف جديد تحتاج "إضافة موظف". كده مش معتمدين بس على إخفاء الزراير في الواجهة.
            await _currentUser.RequireEditAsync(m.Id.HasValue ? "بيانات الموظفين" : "إضافة موظف");
            var currentUserId = await _currentUser.GetCurrentUserIdAsync();
            var currentUserName = await _currentUser.GetCurrentUserNameAsync(); 
            User u;
            using var _db = await _dbFactory.CreateDbContextAsync();
            bool isNew = !m.Id.HasValue;
            List<FieldChange>? changes = null;
            if (m.Id.HasValue)
            {
                u = await _db.Users.FindAsync(m.Id.Value) ?? throw new Exception("الموظف غير موجود");
                changes = ComputeChanges(u, m); 
            }
            else
            {
                u = new User { CreatedAt = DateTime.Now, CreatedById =  currentUserId};
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
            u.IsFreeLocation = m.IsFreeLocation;
            u.EndDutyNotes = m.EndDutyNotes;
            u.EndDutyTypeId = m.EndDutyTypeId;
            u.UpdatedById = currentUserId;
            if (!string.IsNullOrEmpty(m.ProfileImageBase64))
            {
                try
                {
                    u.ProfileImageData = Convert.FromBase64String(m.ProfileImageBase64);
                }
                catch
                {
                    // تجاهل إذا كانت Base64 غير صالحة
                }
            }else
            {
                u.ProfileImageData = null;
            }

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

            if (isNew)
            {
                await LogAuditAsync(_db, u, currentUserId, currentUserName,
                    AuditAction.Create, notes: "إضافة موظف جديد");
            }
            else if (changes != null && changes.Count > 0)
            {
                await LogAuditAsync(_db, u, currentUserId, currentUserName,
                    AuditAction.Update, changes);
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
            await _currentUser.RequireEditAsync("بيانات الموظفين");
            var currentUserId = await _currentUser.GetCurrentUserIdAsync();
            var currentUserName = await _currentUser.GetCurrentUserNameAsync();

            using var _db = await _dbFactory.CreateDbContextAsync();
            var u = await _db.Users.FindAsync(id) ?? throw new Exception("الموظف غير موجود");
            u.IsArchived = !u.IsArchived;
            u.UpdatedAt = DateTime.Now;

            await LogAuditAsync(_db, u, currentUserId, currentUserName,
                u.IsArchived ? AuditAction.Archive : AuditAction.Restore,
                notes: u.IsArchived ? "أرشفة الموظف" : "استعادة الموظف");
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