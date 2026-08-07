using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{
    public class CompanyDocumentItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public DocumentCategory Category { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public string? JobTitleName { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploaderName { get; set; } = "";
    }

    public class DocumentFolder
    {
        public DocumentCategory Category { get; set; }
        public List<CompanyDocumentItem> Documents { get; set; } = new();
    }
}