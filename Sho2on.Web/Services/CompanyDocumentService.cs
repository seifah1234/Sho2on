using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Models;

namespace Sho2on.Web.Services
{
    public class CompanyDocumentService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly FileStorageService _files;

        public CompanyDocumentService(IDbContextFactory<AppDbContext> dbFactory, FileStorageService files)
        {
            _dbFactory = dbFactory;
            _files = files;
        }

        public async Task<List<DocumentFolder>> GetGroupedAsync(bool includeInactive = false)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var query = _db.CompanyDocuments
                .Include(d => d.Uploader)
                .Include(d => d.JobTitle)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(d => d.IsActive);

            var docs = await query.OrderByDescending(d => d.UploadDate).ToListAsync();

            return docs
                .GroupBy(d => d.Category)
                .Select(g => new DocumentFolder
                {
                    Category = g.Key,
                    Documents = g.Select(d => new CompanyDocumentItem
                    {
                        Id = d.Id,
                        Title = d.Title,
                        FileName = d.FileName,
                        FileType = d.FileType,
                        FileSize = d.FileSize,
                        Category = d.Category,
                        IsRequired = d.IsRequired,
                        IsActive = d.IsActive,
                        JobTitleName = d.JobTitle?.Name,
                        UploadDate = d.UploadDate,
                        UploaderName = d.Uploader != null ? d.Uploader.FullName : ""
                    }).ToList()
                })
                .OrderBy(f => f.Category)
                .ToList();
        }

        public async Task UploadAsync(string title, DocumentCategory category, bool isRequired, int? jobTitleId,
            string? description, int uploadedBy, string fileName, string fileType, Stream content)
        {
            var (storedPath, size) = await _files.SaveAsync("Company", fileName, content);

            using var _db = await _dbFactory.CreateDbContextAsync();
            _db.CompanyDocuments.Add(new CompanyDocument
            {
                Title = title,
                FileName = fileName,
                FilePath = storedPath,
                FileType = fileType,
                FileSize = size,
                Category = category,
                IsRequired = isRequired,
                JobTitleId = jobTitleId,
                Description = description ?? "",
                UploadedBy = uploadedBy,
                IsActive = true
            });

            await _db.SaveChangesAsync();
        }

        public async Task<(Stream Content, string FileName, string FileType)> DownloadAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.CompanyDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            return (_files.OpenRead(doc.FilePath!), doc.FileName, doc.FileType);
        }

        public async Task ToggleActiveAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.CompanyDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            doc.IsActive = !doc.IsActive;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var doc = await _db.CompanyDocuments.FindAsync(id) ?? throw new Exception("المستند غير موجود");
            _files.Delete(doc.FilePath!);
            _db.CompanyDocuments.Remove(doc);
            await _db.SaveChangesAsync();
        }
    }
}