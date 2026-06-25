// EmployeeDocument.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class EmployeeDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        // يمكن أن تكون null للوثائق الشخصية
        public int? DocumentId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public EmployeeDocumentType DocumentType { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; }

        [Required]
        [StringLength(50)]
        public string FileType { get; set; }

        [Required]
        public long FileSize { get; set; }

        public string Description { get; set; }
        public string StoragePath { get; set; }
        public string StorageType { get; set; } = "Central";
        public string FullPath { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        // يمكن أن تكون null للوثائق الشخصية
        public DateTime? SignedDate { get; set; }

        [Required]
        public int UploadedBy { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Active;

        public string Notes { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        // العلاقات
        [ForeignKey(nameof(EmployeeId))]
        public virtual User Employee { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public virtual CompanyDocument Document { get; set; }

        [ForeignKey(nameof(UploadedBy))]
        public virtual User Uploader { get; set; }
    }

    public enum EmployeeDocumentType
    {
        // وثائق الشركة الموقعة
        SignedCompanyDocument = 1,

        TrainingCertificate = 6,// شهادات التدريب
        WorkPermit = 7,         // تصريح العمل
        Other = 99              // أخرى
    }

    public enum DocumentStatus
    {
        Pending = 0,
        Signed = 1,
        Rejected = 2,
        Expired = 3,
        Active = 4,     // للوثائق الشخصية النشطة
        Archived = 5    // للوثائق المؤرشفة
    }
}