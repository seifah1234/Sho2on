// Services/ExcelReaderService.cs
using OfficeOpenXml;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Windows; using HR_Application.Helpers;
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
                    var worksheet = package.Workbook.Worksheets[0]; // √Ê· ‘Ì 

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) //  ŒÿÌ «·’› «·√Ê· («·⁄‰«ÊÌ‰)
                    {
                        // ﬁ—«¡… «·»Ì«‰«  „‰ «·√⁄„œ… «·√—»⁄…
                        var employeeCode = worksheet.Cells[row, 1].Value?.ToString();
                        var commissionRate = worksheet.Cells[row, 2].Value?.ToString();
                        var commissionValue = worksheet.Cells[row, 3].Value?.ToString();
                        var commissionType = int.Parse(worksheet.Cells[row, 4].Value?.ToString());

                        // «· √ﬂœ „‰ ÊÃÊœ »Ì«‰«  √”«”Ì…
                        if (string.IsNullOrEmpty(employeeCode) ||
                            (string.IsNullOrEmpty(commissionRate) && string.IsNullOrEmpty(commissionValue)))
                        {
                            continue; //  ŒÿÌ «·’›Ê› €Ì— «·„ﬂ „·…
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
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì ﬁ—«¡… „·› Excel: {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return commissionList;
        }
    }

    public class CommissionData
    {
        public string EmployeeCode { get; set; }
        public string CommissionRate { get; set; } // ‰”»… «·⁄„Ê·…
        public string CommissionValue { get; set; } // ﬁÌ„… «·⁄„Ê·…
        public int CommissionType { get; set; } // ‰Ê⁄ «·⁄„Ê·…
    }
}
