using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class EmployeeDocumentItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public EmployeeDocumentType DocumentType { get; set; }
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public DocumentStatus Status { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Notes { get; set; }
        public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now.AddDays(30) && ExpiryDate.Value >= DateTime.Now;
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
    }

    public class RequiredDocumentStatus
    {
        public int CompanyDocumentId { get; set; }
        public string Title { get; set; } = "";
        public bool IsUploaded { get; set; }
    }

    public class EmployeeDocumentsSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public List<EmployeeDocumentItem> Documents { get; set; } = new();
        public List<RequiredDocumentStatus> RequiredDocuments { get; set; } = new();
    }
}