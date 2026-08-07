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