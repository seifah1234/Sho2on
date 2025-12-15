using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.Helpers
{

    public static class ExcelTemplateHelper
    {
        public static readonly List<string> TemplateHeaders = new List<string>
    {
        "UserId",
        "AttendanceDate (yyyy-MM-dd)",
        "CheckInTime (HH:mm:ss)",
        "CheckOutTime (HH:mm:ss)",
        "ShiftId",
        "CheckInBranchId",
        "CheckOutBranchId",
        "CheckInLocation",
        "CheckOutLocation",
        "ExemptLate (true/false)",
        "ExemptEarlyLeave (true/false)",
        "ExemptOvertime (true/false)",
        "ExemptEarlyEnter (true/false)",
        "IsHoliday (true/false)",
        "IsAbsence (true/false)",
        "Late (HH:mm:ss)",
        "EarlyLeave (HH:mm:ss)",
        "EarlyEnter (HH:mm:ss)",
        "Overtime (HH:mm:ss)",
        "TotalWorkHours (HH:mm:ss)",
        "CheckInLatitude",
        "CheckInLongitude",
        "CheckOutLatitude",
        "CheckOutLongitude"
    };

        public static string GenerateImportTemplate()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "Attendance_Import_Template.xlsx",
                Title = "Save Import Template"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Attendance");

                       

                        // كتابة العناوين
                        for (int i = 0; i < TemplateHeaders.Count; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = TemplateHeaders[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }



                        // ضبط عرض الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveFileDialog.FileName);

                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });

                        return saveFileDialog.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في إنشاء القالب: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
    }
}
