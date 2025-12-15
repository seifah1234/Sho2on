// Services/ExcelReaderService.cs
using OfficeOpenXml;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Services
{
    public class ExcelReaderService
    {
        public List<CommissionData> ReadCommissionExcel(string filePath)
        {
            var commissionList = new List<CommissionData>();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // أول شيت

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // تخطي الصف الأول (العناوين)
                    {
                        // قراءة البيانات من الأعمدة الأربعة
                        var employeeCode = worksheet.Cells[row, 1].Value?.ToString();
                        var commissionRate = worksheet.Cells[row, 2].Value?.ToString();
                        var commissionValue = worksheet.Cells[row, 3].Value?.ToString();
                        var commissionType = int.Parse(worksheet.Cells[row, 4].Value?.ToString());

                        // التأكد من وجود بيانات أساسية
                        if (string.IsNullOrEmpty(employeeCode) ||
                            (string.IsNullOrEmpty(commissionRate) && string.IsNullOrEmpty(commissionValue)))
                        {
                            continue; // تخطي الصفوف غير المكتملة
                        }

                        var commissionData = new CommissionData
                        {
                            EmployeeCode = employeeCode.Trim(),
                            CommissionRate = commissionRate?.Trim(),
                            CommissionValue = commissionValue?.Trim(),
                            CommissionType = commissionType
                        };

                        commissionList.Add(commissionData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في قراءة ملف Excel: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return commissionList;
        }
    }

    public class CommissionData
    {
        public string EmployeeCode { get; set; }
        public string CommissionRate { get; set; } // نسبة العمولة
        public string CommissionValue { get; set; } // قيمة العمولة
        public int CommissionType { get; set; } // نوع العمولة
    }
}