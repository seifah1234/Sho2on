// Services/CommissionProcessorService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;

namespace HR_Application.Services
{
    public class CommissionProcessorService
    {
        private readonly AppDbContext _context;

        public CommissionProcessorService(AppDbContext context)
        {
            _context = context;
        }

        public (List<Salary> salaries, List<string> errors) ProcessCommissions(List<CommissionData> commissionDataList)
        {
            var salaries = new List<Salary>();
            var errors = new List<string>();

            foreach (var data in commissionDataList)
            {
                try
                {
                    // البحث عن الموظف باستخدام الكود
                    var employee = _context.Users
                        .FirstOrDefault(u => u.Id.ToString() == data.EmployeeCode);

                    if (employee == null)
                    {
                        errors.Add($"موظف غير موجود - الكود: {data.EmployeeCode}");
                        continue;
                    }

                    // حساب قيمة العمولة
                    decimal commissionAmount = CalculateCommissionAmount(data, employee);

                    if (commissionAmount <= 0)
                    {
                        errors.Add($"قيمة العمولة غير صالحة للموظف: {employee.FullName} (كود: {data.EmployeeCode})");
                        continue;
                    }

                    // تحديد نوع العملية (إضافة أو خصم)

                    // إنشاء كائن الراتب
                    var salary = new Salary
                    {
                        UserId = employee.Id,
                        Amount = commissionAmount,
                        Type = data.CommissionType, // 2 للعمولات (افتراضي)
                        Operation = 1,
                        DayDate = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        EditedAt = DateTime.Now
                    };

                    salaries.Add(salary);
                }
                catch (Exception ex)
                {
                    errors.Add($"خطأ في معالجة بيانات الموظف {data.EmployeeCode}: {ex.Message}");
                }
            }

            return (salaries, errors);
        }

        private decimal CalculateCommissionAmount(CommissionData data, User employee)
        {
            decimal amount = 0;

            // إذا كانت نسبة العمولة موجودة، احسب من الراتب الأساسي
            if (!string.IsNullOrEmpty(data.CommissionRate) &&
                decimal.TryParse(data.CommissionRate, out decimal rate))
            {
                // احصل على آخر راتب أساسي للموظف
                var lastSalary = _context.Salaries
                    .Where(s => s.UserId == employee.Id && s.Type == 1) // الرواتب الأساسية
                    .OrderByDescending(s => s.DayDate)
                    .FirstOrDefault();

                decimal baseSalary = lastSalary?.Amount ?? 0;

                if (baseSalary > 0)
                {
                    amount = baseSalary * (rate / 100);
                }
                else
                {
                    amount = rate; // استخدم القيمة مباشرة إذا لم يوجد راتب أساسي
                }
            }
            // إذا كانت قيمة العمولة موجودة مباشرة
            else if (!string.IsNullOrEmpty(data.CommissionValue) &&
                     decimal.TryParse(data.CommissionValue, out decimal value))
            {
                amount = value;
            }

            return amount;
        }

    }
}