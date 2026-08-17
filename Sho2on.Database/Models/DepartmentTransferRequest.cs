using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public enum TransferRequestStatus
    {
        PendingDirectManager = 0,   // مستني موافقة المدير المباشر
        PendingSecondApprover = 1,  // مستني موافقة الموافق التاني
        Approved = 2,               // اتنقل فعليًا
        RejectedByDirectManager = 3,
        RejectedBySecondApprover = 4
    }

    public class DepartmentTransferRequest
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int FromDepartmentId { get; set; }
        public int ToDepartmentId { get; set; }

        public int DirectManagerId { get; set; }
        public int SecondApproverId { get; set; }

        public string? Reason { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;

        public TransferRequestStatus Status { get; set; } = TransferRequestStatus.PendingDirectManager;

        public DateTime? DirectManagerActionDate { get; set; }
        public string? DirectManagerNote { get; set; }

        public DateTime? SecondApproverActionDate { get; set; }
        public string? SecondApproverNote { get; set; }

        public DateTime? EffectiveDate { get; set; }   // إمتى اتنقل فعليًا في جدول Users

        [ForeignKey(nameof(UserId))] public User User { get; set; }
        [ForeignKey(nameof(FromDepartmentId))] public Department FromDepartment { get; set; }
        [ForeignKey(nameof(ToDepartmentId))] public Department ToDepartment { get; set; }
        [ForeignKey(nameof(DirectManagerId))] public User DirectManager { get; set; }
        [ForeignKey(nameof(SecondApproverId))] public User SecondApprover { get; set; }
    }
}