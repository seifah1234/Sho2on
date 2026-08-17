using ClosedXML.Excel;
using Sho2on.Database.Models;
using Sho2on.Web.Models;
using Sho2on.Web.Models.Sho2on.Web.Models;
using Sho2on.Web.Services;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        // تحميل مستندات الشركة
        app.MapGet("/files/company-documents/{id:int}", async (int id, CompanyDocumentService svc) =>
        {
            try
            {
                var (content, fileName, fileType) = await svc.DownloadAsync(id);
                return Results.File(content, GetContentType(fileType), fileName);
            }
            catch
            {
                return Results.NotFound("المستند غير موجود");
            }
        }).RequireAuthorization();

        // تحميل مستندات الموظف
        app.MapGet("/files/employee-documents/{id:int}", async (int id, EmployeeDocumentService svc) =>
        {
            try
            {
                var (content, fileName, fileType) = await svc.DownloadAsync(id);
                return Results.File(content, GetContentType(fileType), fileName);
            }
            catch
            {
                return Results.NotFound("المستند غير موجود");
            }
        }).RequireAuthorization();

        app.MapGet("/api/salary/export/{userId:int}", async (int userId, int? month, int? year, SalaryService salarySvc, BenefitService benefitSvc) =>
        {
            var details = await salarySvc.GetSalaryPreviewAsync(userId, month ?? DateTime.Now.Month, year ?? DateTime.Now.Year);
            if (details == null) return Results.NotFound();

            var (benefits, deductions) = await benefitSvc.GetEmployeeBenefitsForMonthAsync(userId, details.Month, details.Year);

            var stream = new MemoryStream();
            GenerateExcelToStream(stream, details, benefits, deductions);
            stream.Position = 0;

            var fileName = $"Salary_{details.EmployeeName}_{details.Month}_{details.Year}.xlsx";
            return Results.File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        });

        // معاينة المستند (يفتح في المتصفح)
        app.MapGet("/files/preview/{id:int}", async (int id, CompanyDocumentService companySvc, EmployeeDocumentService employeeSvc) =>
        {
            try
            {
                // نجرب مستندات الموظف أولاً
                try
                {
                    var (content, fileName, fileType) = await employeeSvc.DownloadAsync(id);
                    return Results.File(content, GetContentType(fileType));
                }
                catch
                {
                    // لو مش موجود، نجرب مستندات الشركة
                    var (content, fileName, fileType) = await companySvc.DownloadAsync(id);
                    return Results.File(content, GetContentType(fileType));
                }
            }
            catch
            {
                return Results.NotFound("المستند غير موجود");
            }
        }).RequireAuthorization();
    }


    private static void GenerateExcelToStream(
    MemoryStream stream,
    SalaryDetailsDto details,
    List<BenefitDto> allBenefits,
    List<BenefitDto> allDeductions)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("كشف الراتب");
        ws.RightToLeft = true;

        // =========================
        // تجهيز البيانات
        // =========================
        var benefitGroups = allBenefits
            .GroupBy(x => x.BenefitTypeName?.Trim() ?? "غير محدد")
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Amount) })
            .ToList();

        var deductionGroups = allDeductions
            .GroupBy(x => x.BenefitTypeName?.Trim() ?? "غير محدد")
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Amount) })
            .ToList();

        // =========================
        // حساب الأعمدة
        // =========================
        // الأعمدة: الاسم (1) + الوظيفة (2) + الاستحقاقات (benefitGroups.Count) +
        // مجموع الاستحقاقات (1) + الصافي المستحق (1) + الاستقطاعات (deductionGroups.Count)
        int totalColumns = 2 + Math.Max(benefitGroups.Count + 6, deductionGroups.Count);

        // حساب مواقع الأعمدة المهمة
        int startBenefitsCol = 6; // أول عمود للاستحقاقات
        int endBenefitsCol = totalColumns - 2; // آخر عمود للاستحقاقات
        int totalBenefitsCol = endBenefitsCol + 1; // عمود مجموع الاستحقاقات
        int netSalaryCol = totalBenefitsCol + 1; // عمود الصافي المستحق
        int startDeductionsCol = netSalaryCol + 1; // أول عمود للاستقطاعات

        // =========================
        // إعداد عرض الأعمدة
        // =========================

        for (int i = 1; i < totalColumns - 1; i++)
            ws.Column(i).Width = 15;

        ws.Column(1).Width = 18; // الاسم
        ws.Column(2).Width = 18; // الوظيفة
        ws.Column(totalBenefitsCol).Width = 19; // مجموع الاستحقاقات
        ws.Column(netSalaryCol).Width = 18; // الصافي المستحق

        // =========================
        // الصف الأول: العنوان
        // =========================
        ws.Range(1, 1, 1, totalColumns).Merge();
        var title = ws.Cell(1, 1);
        title.Value = $"كشف راتب - {details.EmployeeName} - {details.Month}/{details.Year}";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 35;

        // =========================
        // الصف الثاني: الاستحقاقات والاستقطاعات (العناوين الرئيسية)
        // =========================
        // دمج الاستحقاقات من العمود 1 إلى مجموع الاستحقاقات
        ws.Range(2, 1, 3, 3).Merge();
        ws.Cell(2, 1).Value = "الإســــــــــــــــــــــــــــــــــــــــم";
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 4, 3, 5).Merge();
        ws.Cell(2, 4).Value = "الوظيفة";
        ws.Cell(2, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // دمج الاستقطاعات من بعد الصافي المستحق إلى نهاية الجدول
        ws.Range(2, startBenefitsCol, 2, endBenefitsCol).Merge();
        ws.Cell(2, startBenefitsCol).Value = "استحقاقــــــــــــــــــــــــــــــــــــــــات";
        ws.Cell(2, startBenefitsCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(5, 1, 5, totalColumns - 2).Merge();
        ws.Cell(5, 1).Value = "استقطاعــــــــــــــــــــــــــــــــــــــــات";
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // =========================
        // الصف الثالث: أسماء الأعمدة
        // =========================
        int col = startBenefitsCol;

        ws.Cell(3, col).Value = "الراتب الاساسي";
        ws.Cell(3, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        col++;


        // الاستحقاقات
        foreach (var benefit in benefitGroups)
        {
            ws.Cell(3, col).Value = benefit.Name;
            ws.Cell(3, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;
        }

        // مجموع الاستحقاقات
        ws.Range(2, col, 3, col).Merge();
        ws.Cell(2, col).Value = "مجموع الإستحقاقات";
        ws.Cell(2, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        col++;

        // الصافي المستحق
        ws.Range(2, col, 6, col).Merge();
        ws.Cell(2, col).Value = "الصافي المستحق";
        ws.Cell(2, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        col = 1;

        // الاستقطاعات
        foreach (var deduction in deductionGroups)
        {
            ws.Cell(6, col).Value = deduction.Name;
            ws.Cell(6, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;
        }


        // =========================
        // الصف الرابع: البيانات
        // =========================
        col = startBenefitsCol;

        // الاسم
        ws.Range(4, 1, 4, 3).Merge();
        ws.Cell(4, 1).Value = details.EmployeeName;
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // الوظيفة
        ws.Range(4, 4, 4, 5).Merge();
        ws.Cell(4, 4).Value = details.EmployeeJob ?? details.BranchName;
        ws.Cell(4, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // الاستحقاقات - القيم
        decimal totalBenefits = 0;
        decimal totalDeductions = 0;

        ws.Cell(4, col).Value = details.BasicSalary;
        ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        totalBenefits += details.BasicSalary;
        col++;

        foreach (var benefit in benefitGroups)
        {
            ws.Cell(4, col).Value = benefit.Total;
            ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            totalBenefits += benefit.Total;
            col++;
        }

        // مجموع الاستحقاقات
        ws.Cell(4, col).Value = totalBenefits;
        ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        col++;

        // الصافي المستحق
        ws.Cell(7, col).Value = details.NetSalary;
        ws.Cell(7, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        col = 1;

        // الاستقطاعات - القيم
        foreach (var deduction in deductionGroups)
        {
            ws.Cell(7, col).Value = deduction.Total;
            totalDeductions += deduction.Total;

            ws.Cell(7, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;
        }


        ws.Range(5, totalColumns - 1, 6, totalColumns - 1).Merge();
        ws.Cell(5, totalColumns - 1).Value = "مجموع الاستقطاعات";
        ws.Cell(5, totalColumns - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(7, totalColumns - 1).Value = totalDeductions;
        ws.Cell(7, totalColumns - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // =========================
        // تطبيق الـ Styling
        // =========================

        // تنسيق الصف الثاني (العناوين الرئيسية)
        ws.Row(2).Height = 40;
        ws.Range(2, 1, 2, totalColumns).Style.Font.Bold = true;
        ws.Range(2, 1, 2, totalColumns).Style.Font.FontSize = 14;
        ws.Range(2, 1, 2, totalColumns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(2, 1, 2, totalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(2, 1, 2, totalColumns).Style.Fill.BackgroundColor = XLColor.FromHtml("#252525");
        ws.Range(2, 1, 2, totalColumns).Style.Font.FontColor = XLColor.White;

        ws.Row(5).Height = 40;
        ws.Range(5, 1, 5, totalColumns).Style.Font.Bold = true;
        ws.Range(5, 1, 5, totalColumns).Style.Font.FontSize = 14;
        ws.Range(5, 1, 5, totalColumns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(5, 1, 5, totalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(5, 1, 5, totalColumns).Style.Fill.BackgroundColor = XLColor.FromHtml("#252525");
        ws.Range(5, 1, 5, totalColumns).Style.Font.FontColor = XLColor.White;

        // تنسيق الصف الثالث (أسماء الأعمدة)
        ws.Row(3).Height = 30;
        ws.Range(3, 1, 3, totalColumns).Style.Font.Bold = true;
        ws.Range(3, 1, 3, totalColumns).Style.Font.FontSize = 10;
        ws.Range(3, 1, 3, totalColumns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(3, 1, 3, totalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(3, 1, 3, totalColumns).Style.Alignment.WrapText = true;
        ws.Range(3, 1, 3, totalColumns).Style.Fill.BackgroundColor = XLColor.FromHtml("#252525");
        ws.Range(3, 1, 3, totalColumns).Style.Font.FontColor = XLColor.White;

        ws.Row(6).Height = 30;
        ws.Range(6, 1, 6, totalColumns).Style.Font.Bold = true;
        ws.Range(6, 1, 6, totalColumns).Style.Font.FontSize = 10;
        ws.Range(6, 1, 6, totalColumns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(6, 1, 6, totalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(6, 1, 6, totalColumns).Style.Alignment.WrapText = true;
        ws.Range(6, 1, 6, totalColumns).Style.Fill.BackgroundColor = XLColor.FromHtml("#252525");
        ws.Range(6, 1, 6, totalColumns).Style.Font.FontColor = XLColor.White;

        // تنسيق الصف الرابع (البيانات)
        ws.Row(4).Height = 30;
        ws.Row(4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(4).Style.Alignment.WrapText = true;

        ws.Row(7).Height = 30;
        ws.Row(7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(7).Style.Alignment.WrapText = true;

        // تنسيق الأرقام
        ws.Range(4, startBenefitsCol, 4, totalColumns).Style.NumberFormat.Format = "#,##0.00";
        ws.Range(7, 1, 4, totalColumns).Style.NumberFormat.Format = "#,##0.00";

        // =========================
        // الحدود
        // =========================
        var tableRange = ws.Range(2, 1, 7, totalColumns);
        tableRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // =========================
        // تجميد الصفوف
        // =========================
        // ws.SheetView.FreezeRows(3);

        // =========================
        // حفظ الملف
        // =========================
        workbook.SaveAs(stream);
    }

    private static string GetContentType(string ext) => ext?.ToLower() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xls" => "application/vnd.ms-excel",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        _ => "application/octet-stream"
    };
}