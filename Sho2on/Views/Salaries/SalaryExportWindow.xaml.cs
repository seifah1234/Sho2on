using ClosedXML.Excel;
using Microsoft.Win32;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using GroupBox = System.Windows.Controls.GroupBox;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using RadioButton = System.Windows.Controls.RadioButton;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace HR_Application.Views
{
    public partial class SalaryExportWindow : Window
    {
        private List<EmployeeSalaryViewModel> _employees;
        private int _month;
        private int _year;

        public SalaryExportWindow(List<EmployeeSalaryViewModel> employees, int month, int year)
        {
            InitializeComponent();
            _employees = employees;
            _month = month;
            _year = year;

            ShowExportOptions();
        }

        private void InitializeComponent()
        {
            Title = LocalizationManager.Translate("تصدير بيانات المرتبات إلى Excel");
            Width = 500;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // العنوان
            var titleBorder = new Border
            {
                Background = System.Windows.Media.Brushes.LightBlue,
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(5, 5, 0, 0)
            };

            var titleText = new TextBlock
            {
                Text = LocalizationManager.Translate("تصدير بيانات المرتبات"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.DarkBlue
            };

            titleBorder.Child = titleText;
            Grid.SetRow(titleBorder, 0);
            grid.Children.Add(titleBorder);

            // خيارات التصدير
            var optionsPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            var monthYearText = new TextBlock
            {
                Text = $"الشهر: {_month} - السنة: {_year}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var countText = new TextBlock
            {
                Text = $"عدد السجلات: {_employees.Count}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var formatGroup = new GroupBox
            {
                Header = LocalizationManager.Translate("تنسيق التصدير"),
                Margin = new Thickness(0, 10, 0, 20),
                Padding = new Thickness(10)
            };

            var formatStack = new StackPanel();

            var rbDetailed = new RadioButton
            {
                Content = LocalizationManager.Translate("تفصيلي (جميع الحقول)"),
                IsChecked = true,
                Margin = new Thickness(5),
                FontSize = 12
            };

            var rbSummary = new RadioButton
            {
                Content = LocalizationManager.Translate("ملخص (الحقول الأساسية فقط)"),
                Margin = new Thickness(5),
                FontSize = 12
            };

            var chkIncludeHeader = new CheckBox
            {
                Content = LocalizationManager.Translate("تضمين رأس الجدول"),
                IsChecked = true,
                Margin = new Thickness(5),
                FontSize = 12
            };

            var chkAutoFormat = new CheckBox
            {
                Content = LocalizationManager.Translate("تنسيق تلقائي للأعمدة"),
                IsChecked = true,
                Margin = new Thickness(5),
                FontSize = 12
            };

            var chkArabicNumbers = new CheckBox
            {
                Content = LocalizationManager.Translate("استخدام الأرقام العربية"),
                IsChecked = true,
                Margin = new Thickness(5),
                FontSize = 12
            };

            formatStack.Children.Add(rbDetailed);
            formatStack.Children.Add(rbSummary);
            formatStack.Children.Add(chkIncludeHeader);
            formatStack.Children.Add(chkAutoFormat);
            formatStack.Children.Add(chkArabicNumbers);
            formatGroup.Content = formatStack;

            optionsPanel.Children.Add(monthYearText);
            optionsPanel.Children.Add(countText);
            optionsPanel.Children.Add(formatGroup);

            Grid.SetRow(optionsPanel, 1);
            grid.Children.Add(optionsPanel);

            // الأزرار
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var btnExport = new Button
            {
                Content = LocalizationManager.Translate("تصدير إلى Excel"),
                Width = 120,
                Height = 35,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnExport.Click += (s, e) => ExportToExcel(rbDetailed.IsChecked == true,
                chkIncludeHeader.IsChecked == true,
                chkAutoFormat.IsChecked == true,
                chkArabicNumbers.IsChecked == true);

            var btnCancel = new Button
            {
                Content = LocalizationManager.Translate("إلغاء"),
                Width = 100,
                Height = 35,
                Margin = new Thickness(10),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();

            buttonPanel.Children.Add(btnExport);
            buttonPanel.Children.Add(btnCancel);

            var buttonBorder = new Border
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(0, 0, 5, 5)
            };
            buttonBorder.Child = buttonPanel;

            Grid.SetRow(buttonBorder, 2);
            grid.Children.Add(buttonBorder);

            Content = grid;
        }

        private void ShowExportOptions()
        {
            // يمكن إضافة أي إعدادات إضافية هنا
        }

        private void ExportToExcel(bool isDetailed, bool includeHeader, bool autoFormat, bool arabicNumbers)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"مرتبات_{_month}_{_year}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                    Title = LocalizationManager.Translate("حفظ ملف Excel")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(LocalizationManager.Translate("مرتبات"));

                        int currentRow = 1;

                        // العنوان الرئيسي
                        worksheet.Cell(currentRow, 1).Value = $"تقرير المرتبات - شهر {_month} سنة {_year}";
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
                        worksheet.Range(currentRow, 1, currentRow, isDetailed ? 12 : 8).Merge();
                        currentRow += 2;

                        // رأس الجدول
                        if (includeHeader)
                        {
                            string[] headers;
                            if (isDetailed)
                            {
                                headers = new[]
                                {
                                    LocalizationManager.Translate("الكود"), LocalizationManager.Translate("الاسم"), LocalizationManager.Translate("الفرع"), LocalizationManager.Translate("الراتب الأساسي"), LocalizationManager.Translate("الإضافات"),
                                    LocalizationManager.Translate("الاستقطاعات"), LocalizationManager.Translate("صندوق الزمالة"), LocalizationManager.Translate("السلف"), LocalizationManager.Translate("الغياب"), LocalizationManager.Translate("التأخير"),
                                    LocalizationManager.Translate("المشاركة الاجتماعية"), LocalizationManager.Translate("الصافي")
                                };
                            }
                            else
                            {
                                headers = new[]
                                {
                                    LocalizationManager.Translate("الكود"), LocalizationManager.Translate("الاسم"), LocalizationManager.Translate("الفرع"), LocalizationManager.Translate("الراتب الأساسي"),
                                    LocalizationManager.Translate("الإضافات"), LocalizationManager.Translate("الاستقطاعات"), LocalizationManager.Translate("صندوق الزمالة"), LocalizationManager.Translate("الصافي")
                                };
                            }

                            for (int i = 0; i < headers.Length; i++)
                            {
                                var cell = worksheet.Cell(currentRow, i + 1);
                                cell.Value = headers[i];
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }
                            currentRow++;
                        }

                        // البيانات
                        foreach (var emp in _employees)
                        {
                            if (isDetailed)
                            {
                                worksheet.Cell(currentRow, 1).Value = emp.Code;
                                worksheet.Cell(currentRow, 2).Value = emp.Name;
                                worksheet.Cell(currentRow, 3).Value = emp.Branch;
                                worksheet.Cell(currentRow, 4).Value = emp.BasicSalary;
                                worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 5).Value = emp.Additions;
                                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 6).Value = emp.Deductions;
                                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 7).Value = emp.FriendshipBoxAmount;
                                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 8).Value = emp.LoanDeduction;
                                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.00";

                                // يمكن إضافة المزيد من الحقول التفصيلية هنا
                                worksheet.Cell(currentRow, 9).Value = 0; // الغياب
                                worksheet.Cell(currentRow, 10).Value = 0; // التأخير
                                worksheet.Cell(currentRow, 11).Value = 0; // المشاركة الاجتماعية

                                worksheet.Cell(currentRow, 12).Value = emp.NetSalary;
                                worksheet.Cell(currentRow, 12).Style.NumberFormat.Format = "#,##0.00";
                                worksheet.Cell(currentRow, 12).Style.Font.Bold = true;
                            }
                            else
                            {
                                worksheet.Cell(currentRow, 1).Value = emp.Code;
                                worksheet.Cell(currentRow, 2).Value = emp.Name;
                                worksheet.Cell(currentRow, 3).Value = emp.Branch;
                                worksheet.Cell(currentRow, 4).Value = emp.BasicSalary;
                                worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 5).Value = emp.Additions;
                                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 6).Value = emp.Deductions;
                                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 7).Value = emp.FriendshipBoxAmount;
                                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";

                                worksheet.Cell(currentRow, 8).Value = emp.NetSalary;
                                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.00";
                                worksheet.Cell(currentRow, 8).Style.Font.Bold = true;
                            }
                            currentRow++;
                        }

                        // الإجماليات
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = LocalizationManager.Translate("الإجماليات:");
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;

                        if (isDetailed)
                        {
                            worksheet.Cell(currentRow, 4).Value = _employees.Sum(e => e.BasicSalary);
                            worksheet.Cell(currentRow, 5).Value = _employees.Sum(e => e.Additions);
                            worksheet.Cell(currentRow, 6).Value = _employees.Sum(e => e.Deductions);
                            worksheet.Cell(currentRow, 7).Value = _employees.Sum(e => e.FriendshipBoxAmount);
                            worksheet.Cell(currentRow, 8).Value = _employees.Sum(e => e.LoanDeduction);
                            worksheet.Cell(currentRow, 12).Value = _employees.Sum(e => e.NetSalary);

                            // تنسيق خلايا الإجماليات
                            for (int i = 4; i <= 12; i++)
                            {
                                if (i == 9 || i == 10 || i == 11) continue; // تخطي الحقول الفارغة مؤقتاً
                                worksheet.Cell(currentRow, i).Style.NumberFormat.Format = "#,##0.00";
                                worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                        }
                        else
                        {
                            worksheet.Cell(currentRow, 4).Value = _employees.Sum(e => e.BasicSalary);
                            worksheet.Cell(currentRow, 5).Value = _employees.Sum(e => e.Additions);
                            worksheet.Cell(currentRow, 6).Value = _employees.Sum(e => e.Deductions);
                            worksheet.Cell(currentRow, 7).Value = _employees.Sum(e => e.FriendshipBoxAmount);
                            worksheet.Cell(currentRow, 8).Value = _employees.Sum(e => e.NetSalary);

                            for (int i = 4; i <= 8; i++)
                            {
                                worksheet.Cell(currentRow, i).Style.NumberFormat.Format = "#,##0.00";
                                worksheet.Cell(currentRow, i).Style.Font.Bold = true;
                                worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                        }

                        // ضبط عرض الأعمدة تلقائياً
                        if (autoFormat)
                        {
                            worksheet.Columns().AdjustToContents();
                        }

                        // إضافة ورقة إحصائيات
                        var statsSheet = workbook.Worksheets.Add(LocalizationManager.Translate("إحصائيات"));

                        statsSheet.Cell(1, 1).Value = LocalizationManager.Translate("إحصائيات المرتبات");
                        statsSheet.Cell(1, 1).Style.Font.Bold = true;
                        statsSheet.Cell(1, 1).Style.Font.FontSize = 14;
                        statsSheet.Range(1, 1, 1, 2).Merge();

                        var statistics = new Dictionary<string, string>
                        {
                            { LocalizationManager.Translate("عدد الموظفين"), _employees.Count.ToString() },
                            { LocalizationManager.Translate("إجمالي الرواتب الأساسية"), _employees.Sum(e => e.BasicSalary).ToString("N2") },
                            { LocalizationManager.Translate("إجمالي الإضافات"), _employees.Sum(e => e.Additions).ToString("N2") },
                            { LocalizationManager.Translate("إجمالي الاستقطاعات"), _employees.Sum(e => e.Deductions).ToString("N2") },
                            { LocalizationManager.Translate("إجمالي صندوق الزمالة"), _employees.Sum(e => e.FriendshipBoxAmount).ToString("N2") },
                            { LocalizationManager.Translate("إجمالي السلف"), _employees.Sum(e => e.LoanDeduction).ToString("N2") },
                            { LocalizationManager.Translate("إجمالي الصافي"), _employees.Sum(e => e.NetSalary).ToString("N2") },
                            { LocalizationManager.Translate("متوسط الراتب"), (_employees.Sum(e => e.NetSalary) / _employees.Count).ToString("N2") },
                            { LocalizationManager.Translate("أعلى راتب"), _employees.Max(e => e.NetSalary).ToString("N2") },
                            { LocalizationManager.Translate("أقل راتب"), _employees.Min(e => e.NetSalary).ToString("N2") }
                        };

                        int statRow = 3;
                        foreach (var stat in statistics)
                        {
                            statsSheet.Cell(statRow, 1).Value = stat.Key;
                            statsSheet.Cell(statRow, 1).Style.Font.Bold = true;
                            statsSheet.Cell(statRow, 2).Value = stat.Value;
                            statRow++;
                        }

                        statsSheet.Columns().AdjustToContents();

                        // حفظ الملف
                        workbook.SaveAs(saveFileDialog.FileName);

                        LocalizationManager.ShowMessage($"تم التصدير بنجاح إلى:\n{saveFileDialog.FileName}",
                            LocalizationManager.Translate("تم التصدير"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // فتح الملف تلقائياً
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveFileDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // تجاهل الخطأ إذا لم يتمكن من فتح الملف
                        }

                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في التصدير: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
