using Sho2on.Database.Models;

namespace Sho2on.Web.Models
{

    public class SalaryPaymentGroupDto
    {
        public int FirstSalaryId { get; set; }
        public UserBasicInfo? User { get; set; }
        public decimal FixedSalary { get; set; }
        public decimal VariableSalary { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal OvertimeValue { get; set; }
        public decimal LateValue { get; set; }
        public decimal AbsenceValue { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public bool IsPaid { get; set; }
        public string StatusDisplay => IsPaid ? "مدفوع" : "غير مدفوع";
        public string StatusClass => IsPaid ? "status-paid" : "status-unpaid";
    }

    public class SalarySummaryDto
    {
        public int TotalEmployees { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNetSalary { get; set; }
        public int PaidCount { get; set; }
        public int UnpaidCount { get; set; }
    }

    namespace Sho2on.Web.Models
    {
        public class SalaryDetailsDto
        {
            public int Id { get; set; }
            public string EmployeeName { get; set; } = "";
            public int UserId { get; set; }
            public string EmployeeCode { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string DepartmentName { get; set; } = "";
            public string JobTitleName { get; set; } = "";

            // المرتب الأساسي
            public decimal BasicSalary { get; set; }


            // الإجماليات
            public decimal TotalAdditions { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal NetSalary { get; set; }

            // معلومات الدفع
            public bool IsPaid { get; set; }
            public bool IsOffCycle { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public DateTime? PaymentDate { get; set; }
            public DateTime? ActualPaymentDate { get; set; }
            public string? Notes { get; set; }
            public DateTime? CreatedAt { get; set; }

            // خصائص محسوبة
            public string PaymentStatus => IsPaid ? "مدفوع" : "غير مدفوع";
            public string PeriodDisplay => $"{Month}/{Year}";
            public string PaymentType => IsOffCycle ? "صرف فوري" : "راتب شهري";
        }
    }

    public class UserBasicInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string FullName { get; set; } = "";
        public decimal FixedSalary { get; set; }
        public decimal VariableSalary { get; set; }
    }

    public class MonthSettings
    {
        public int StartDay { get; set; } = 26;
        public int EndDay { get; set; } = 25;
    }

    public class SalaryCalculationResult
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserCode { get; set; } = "";

        // المدخلات
        public decimal BaseSalary { get; set; }
        public decimal Benefits { get; set; } // استحقاقات
        public decimal OvertimeBonus { get; set; } // أضافي
        public decimal PenaltyDeduction { get; set; }     // خصم جزاءات
        public decimal SocialParticipation { get; set; }  // مشاركة اجتماعية
        public decimal FriendshipBoxDeduction { get; set; }
        public decimal HousingAllowance { get; set; }     // بدل سكن
        public decimal TransportationAllowance { get; set; } // بدل انتقال
        public decimal ManagementAllowance { get; set; }  // بدل إدارة
        public decimal NatureAllowance { get; set; }      // بدل طبيعة عمل
        public decimal Rewards { get; set; }              // مكافآت
        public decimal TargetCommission { get; set; }     // عمولات تحقيق
        public decimal ExternalCommission { get; set; }   // عمولات خارجية
        // الخصومات
        public decimal Deductions { get; set; } // استقطاعات
        public decimal AbsenceDeduction { get; set; } // خصم الغياب
        public decimal LateDeduction { get; set; } // خصم التأخير
        public decimal LoanInstallments { get; set; } // أقساط السلف
        public decimal OffCyclePayments { get; set; } // صرف فوري

        // الضرايب والتأمينات (على مستوى الشركة)
        public decimal Taxes { get; set; } // ضريبة الدخل
        public decimal Insurance { get; set; } // تأمينات
        public decimal TotalAdditions =>
    Rewards + OvertimeBonus + TargetCommission + ExternalCommission;
        // النواتج
        public decimal GrossSalary => BaseSalary + Benefits + OvertimeBonus;
        public decimal TotalDeductions => Deductions + AbsenceDeduction + LateDeduction + LoanInstallments + OffCyclePayments + Taxes + Insurance;
        public decimal NetSalary => GrossSalary - TotalDeductions;

        // الشهر
        public int Month { get; set; }
        public int Year { get; set; }

        // حالة الصرف
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }


        public string Currency { get; set; } = "ج.م";
    }
}