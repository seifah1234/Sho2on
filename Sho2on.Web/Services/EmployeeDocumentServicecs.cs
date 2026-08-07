using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class EmployeeDocumentService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly FileStorageService _files;

        public EmployeeDocumentService(IDbContextFactory<AppDbContext> dbFactory, FileStorageService files)
        {
            _dbFactory = dbFactory;
            _files = files;
        }

        public async Task UploadAsync(
    int employeeId,
    string title,
    EmployeeDocumentType type,
    int? linkedCompanyDocId,
    DateTime? expiryDate,
    string notes,
    int uploadedByUserId,
    string fileName,
    string fileExtension,
    Stream fileStream,
    string description = "") // إضافة parameter للوصف مع قيمة افتراضية
        {
            var (storedPath, size) = await _files.SaveAsync($"Employees/{employeeId}", fileName, fileStream);

            using var _db = await _dbFactory.CreateDbContextAsync();
            _db.EmployeeDocuments.Add(new EmployeeDocument
            {
                EmployeeId = employeeId,
                DocumentId = linkedCompanyDocId,
                Title = title,
                DocumentType = type,
                FileName = fileName,
                FileType = fileExtension,
                FileSize = size,
                StoragePath = storedPath,
                FullPath = storedPath,
                Description = description ?? "", // تأكد إنه مش null
                ExpiryDate = expiryDate,
                Notes = notes,
                UploadedBy = uploadedByUserId,
                Status = DocumentStatus.Active,
                IsActive = true
            });

            await _db.SaveChangesAsync();
        }

        public async Task<EmployeeDocumentsSummary> GetSummaryAsync(int employeeId)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId) ?? throw new Exception("الموظف غير موجود");

            var docs = await _db.EmployeeDocuments
                .Where(d => d.EmployeeId == employeeId && d.IsActive)
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new EmployeeDocumentItem
                {
                    Id = d.Id,
                    Title = d.Title,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    Status = d.Status,
                    UploadDate = d.UploadDate,
                    ExpiryDate = d.ExpiryDate,
                    Notes = d.Notes
                })
                .ToListAsync();

            // المستندات المطلوبة لوظيفة الموظف ده تحديدًا (أو مطلوبة لكل الوظائف)
            var requiredCompanyDocs = await _db.CompanyDocuments
                .Where(cd => cd.IsRequired && cd.IsActive && (cd.JobTitleId == null || cd.JobTitleId == employee.JobTitleId))
                .ToListAsync();

            var uploadedCompanyDocIds = await _db.EmployeeDocuments
                .Where(d => d.EmployeeId == employeeId && d.IsActive && d.DocumentId != null)
                .Select(d => d.DocumentId!.Value)
                .ToListAsync();

            var requiredStatus = requiredCompanyDocs.Select(cd => new RequiredDocumentStatus
            {
                CompanyDocumentId = cd.Id,
                Title = cd.Title,
                IsUploaded = uploadedCompanyDocIds.Contains(cd.Id)
            }).ToList();

            return new EmployeeDocumentsSummary
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeCode = employee.Code,
                Documents = docs,
                RequiredDocuments = requiredStatus
            };
        }


        public async Task<(Stream Content, string FileName, string FileType)> DownloadAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.EmployeeDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            return (_files.OpenRead(doc.StoragePath), doc.FileName, doc.FileType);
        }

        public async Task ArchiveAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.EmployeeDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            doc.Status = DocumentStatus.Archived;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.EmployeeDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            _files.Delete(doc.StoragePath);
            _db.EmployeeDocuments.Remove(doc);
            await _db.SaveChangesAsync();
        }
    }
}