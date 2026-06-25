using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class CompanyDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; }

        public string? StorageType { get; set; } = "Central";

        public string? FullPath { get; set; }
        public string? FilePath { get; set; }

        [Required]
        [StringLength(50)]
        public string FileType { get; set; }

        [Required]
        public long FileSize { get; set; }

        [Required]
        public DocumentCategory Category { get; set; }

        [Required]
        public bool IsRequired { get; set; }

        public int? JobTitleId { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        public int UploadedBy { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(UploadedBy))]
        public virtual User Uploader { get; set; }

        public virtual ICollection<EmployeeDocument> EmployeeDocuments { get; set; }

        [ForeignKey(nameof(JobTitleId))]
        public virtual JobTitle JobTitle { get; set; }
    }


    public enum DocumentCategory
    {
        JobDescription = 1,
        CompanyPolicy = 2,
        HRManual = 3,
        CodeOfConduct = 4,
        SafetyProcedure = 5,
        Contract = 6,
        Other = 7
    }

}