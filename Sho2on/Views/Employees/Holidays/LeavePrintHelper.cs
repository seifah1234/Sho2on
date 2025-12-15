using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

using Table = System.Windows.Documents.Table;
using Paragraph = System.Windows.Documents.Paragraph;
using TableRow = System.Windows.Documents.TableRow;
using TableCell = System.Windows.Documents.TableCell;
using Run = System.Windows.Documents.Run;
using TextAlignment = System.Windows.TextAlignment;
using Border = System.Windows.Controls.Border;

namespace HR_Application.Views.Employees.Holidays
{
    public class LeavePrintHelper
    {
        private readonly Leave _leave;
        private readonly User _user;
        private readonly LeaveType _leaveType;
        private readonly int _totalBalance;
        private readonly int _usedBalance;
        private readonly int _remainingBalance;

        public LeavePrintHelper(Leave leave, User user, LeaveType leaveType,
                               int totalBalance, int usedBalance, int remainingBalance)
        {
            _leave = leave;
            _user = user;
            _leaveType = leaveType;
            _totalBalance = totalBalance;
            _usedBalance = usedBalance;
            _remainingBalance = remainingBalance;
        }

        public FlowDocument CreatePrintDocument()
        {
            // إنشاء FlowDocument جديد
            FlowDocument document = new FlowDocument
            {
                PageHeight = 842,  // A4 height in points
                PageWidth = 595,   // A4 width in points
                PagePadding = new Thickness(50),
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                ColumnGap = 0,
                ColumnWidth = 495, // PageWidth - 2*PagePadding
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Right
            };

            // إضافة رأس الصفحة
            AddHeader(document);

            // إضافة معلومات الموظف
            AddEmployeeInfo(document);

            // إضافة معلومات الإجازة
            AddLeaveInfo(document);

            // إضافة حالة الطلب
            AddStatusInfo(document);

            // إضافة معلومات الرصيد
            AddBalanceInfo(document);

            // إضافة تذييل الصفحة
            AddFooter(document);

            return document;
        }

        private void AddHeader(FlowDocument document)
        {
            // القسم العلوي مع الشعار والعنوان
            Table headerTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // ثلاثة أعمدة: شعار - عنوان - تاريخ
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(300) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(95) });

            TableRowGroup headerGroup = new TableRowGroup();
            headerTable.RowGroups.Add(headerGroup);

            // الصف الأول: العنوان الرئيسي
            TableRow titleRow = new TableRow();
            headerGroup.Rows.Add(titleRow);
/*
            // الخلية الأولى: الشعار (يمكنك إضافة صورة هنا)
            TableCell logoCell = new TableCell
            {
                ColumnSpan = 1,
                BorderThickness = new Thickness(0)
            };
            logoCell.Blocks.Add(new Paragraph(new Run("شعار الشركة"))
            {
                FontSize = 10,
                Foreground = Brushes.Gray
            });
            titleRow.Cells.Add(logoCell);*/

            // الخلية الثانية: العنوان
            TableCell titleCell = new TableCell
            {
                ColumnSpan = 1,
                TextAlignment = System.Windows.TextAlignment.Center,
                BorderThickness = new Thickness(0)
            };

            Paragraph titleParagraph = new Paragraph();
            titleParagraph.Inlines.Add(new Run("طلب إجازة")
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
            });

            titleCell.Blocks.Add(titleParagraph);
            titleRow.Cells.Add(titleCell);

            // الخلية الثالثة: التاريخ
            TableCell dateCell = new TableCell
            {
                ColumnSpan = 1,
                TextAlignment = System.Windows.TextAlignment.Left,
                BorderThickness = new Thickness(0)
            };

            Paragraph dateParagraph = new Paragraph();
            dateParagraph.Inlines.Add(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}")
            {
                FontSize = 9,
                Foreground = Brushes.Gray
            });
            dateParagraph.Inlines.Add(new LineBreak());
            dateParagraph.Inlines.Add(new Run($"رقم الطلب: {_leave.Id}")
            {
                FontSize = 9,
                Foreground = Brushes.Gray
            });

            dateCell.Blocks.Add(dateParagraph);
            titleRow.Cells.Add(dateCell);

            document.Blocks.Add(headerTable);

            // خط فاصل
            document.Blocks.Add(new Paragraph(new Run(new string('─', 100)))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 20)
            });
        }

        private void AddEmployeeInfo(FlowDocument document)
        {
            // عنوان القسم
            Paragraph sectionTitle = new Paragraph(new Run("1. معلومات الموظف"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // جدول معلومات الموظف
            Table employeeTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // تعريف الأعمدة
            for (int i = 0; i < 4; i++)
            {
                employeeTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            }

            TableRowGroup employeeGroup = new TableRowGroup();
            employeeTable.RowGroups.Add(employeeGroup);

            // إضافة الصفوف
            string[,] employeeData = {
                { "كود الموظف", _user?.Id.ToString() ?? "غير متوفر", "اسم الموظف", _user?.FullName ?? "غير متوفر" },
                { "الإدارة", _user?.Department?.Name ?? "غير متوفر", "الفرع", _user?.Branch?.Name ?? "غير متوفر" },
                { "تاريخ التعيين", _user?.HireDate.ToString("yyyy/MM/dd") ?? "غير متوفر", "الوظيفة", _user?.JobTitle?.Name ?? "غير متوفر" },
                { "نظام العمل", _user?.JobType?.Name ?? "غير متوفر", "الوردية", _user?.Shift?.Name ?? "غير متوفر" }
            };

            for (int i = 0; i < employeeData.GetLength(0); i++)
            {
                TableRow row = new TableRow();
                employeeGroup.Rows.Add(row);

                for (int j = 0; j < 4; j++)
                {
                    TableCell cell = new TableCell
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(8, 5, 8, 5),
                        Background = j % 2 == 0 ? Brushes.WhiteSmoke : Brushes.White
                    };

                    Paragraph cellParagraph = new Paragraph();

                    if (j % 2 == 0) // تسميات
                    {
                        cellParagraph.Inlines.Add(new Run(employeeData[i, j])
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.DimGray
                        });
                    }
                    else // قيم
                    {
                        cellParagraph.Inlines.Add(new Run(employeeData[i, j])
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.Black
                        });
                    }

                    cell.Blocks.Add(cellParagraph);
                    row.Cells.Add(cell);
                }
            }

            document.Blocks.Add(employeeTable);
        }

        private void AddLeaveInfo(FlowDocument document)
        {
            // عنوان القسم
            Paragraph sectionTitle = new Paragraph(new Run("2. معلومات الإجازة"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // جدول معلومات الإجازة
            Table leaveTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // تعريف الأعمدة
            for (int i = 0; i < 4; i++)
            {
                leaveTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            }

            TableRowGroup leaveGroup = new TableRowGroup();
            leaveTable.RowGroups.Add(leaveGroup);

            // إضافة الصفوف
            string[,] leaveData = {
                { "نوع الإجازة", _leaveType?.Name ?? "غير متوفر", "رقم النوع", _leaveType?.Code ?? "غير متوفر" },
                { "من تاريخ", _leave.StartDate.ToString("yyyy/MM/dd"), "إلى تاريخ", _leave.EndDate.ToString("yyyy/MM/dd") },
                { "المدة", $"{_leave.Duration} يوم", "تاريخ الطلب", _leave.RequestDate.ToString("yyyy/MM/dd HH:mm") },
                { "يتطلب موافقة", _leaveType?.RequiresApproval == true ? "نعم" : "لا",
                  "يخصم من الرصيد", _leaveType?.DeductFromBalance == true ? "نعم" : "لا" }
            };

            for (int i = 0; i < leaveData.GetLength(0); i++)
            {
                TableRow row = new TableRow();
                leaveGroup.Rows.Add(row);

                for (int j = 0; j < 4; j++)
                {
                    TableCell cell = new TableCell
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(8, 5, 8, 5),
                        Background = j % 2 == 0 ? new SolidColorBrush(Color.FromArgb(20, 76, 175, 80)) : Brushes.White
                    };

                    Paragraph cellParagraph = new Paragraph();

                    if (j % 2 == 0) // تسميات
                    {
                        cellParagraph.Inlines.Add(new Run(leaveData[i, j])
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.DimGray
                        });
                    }
                    else // قيم
                    {
                        Color valueColor = j == 1 ? Color.FromRgb(76, 175, 80) :
                                          j == 3 ? Color.FromRgb(33, 150, 243) : Colors.Black;

                        cellParagraph.Inlines.Add(new Run(leaveData[i, j])
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(valueColor)
                        });
                    }

                    cell.Blocks.Add(cellParagraph);
                    row.Cells.Add(cell);
                }
            }

            document.Blocks.Add(leaveTable);

            // قسم السبب
            Paragraph reasonTitle = new Paragraph(new Run("سبب الإجازة:"))
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            document.Blocks.Add(reasonTitle);

            Border reasonBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20),
                Background = new SolidColorBrush(Color.FromArgb(10, 76, 175, 80))
            };

            Paragraph reasonParagraph = new Paragraph(new Run(_leave.Reason ?? "لا يوجد سبب"))
            {
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Left,
                LineHeight = 20
            };

            var richTextBox = new RichTextBox();
            richTextBox.Document.Blocks.Add(reasonParagraph);
            reasonBorder.Child = richTextBox; document.Blocks.Add(new BlockUIContainer(reasonBorder));
        }

        private void AddStatusInfo(FlowDocument document)
        {
            // عنوان القسم
            Paragraph sectionTitle = new Paragraph(new Run("3. حالة الطلب"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // حالة الطلب
            Table statusTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // تعريف الأعمدة
            statusTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            statusTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup statusGroup = new TableRowGroup();
            statusTable.RowGroups.Add(statusGroup);

            string statusText = GetStatusText(_leave.Status);
            Brush statusColor = GetStatusColor(_leave.Status);

            // صف الحالة
            TableRow statusRow = new TableRow();
            statusGroup.Rows.Add(statusRow);

            TableCell statusLabelCell = new TableCell
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 152, 0))
            };
            statusLabelCell.Blocks.Add(new Paragraph(new Run("الحالة:"))
            {
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.SemiBold
            });
            statusRow.Cells.Add(statusLabelCell);

            TableCell statusValueCell = new TableCell
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5),
                Background = statusColor
            };
            statusValueCell.Blocks.Add(new Paragraph(new Run(statusText))
            {
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = System.Windows.TextAlignment.Center
            });
            statusRow.Cells.Add(statusValueCell);

            // معلومات إضافية حسب الحالة
            if (_leave.Status == 2 || _leave.Status == 3) // موافق عليه أو مرفوض
            {
                AddStatusDetailRow(statusGroup, "تاريخ الموافقة/الرفض",
                    _leave.ApprovalDate?.ToString("yyyy/MM/dd HH:mm") ?? "غير متوفر");

                AddStatusDetailRow(statusGroup, "تمت الموافقة بواسطة",
                    _leave.Approver?.FullName ?? "غير متوفر");

                if (_leave.Status == 3 && !string.IsNullOrEmpty(_leave.RejectionReason))
                {
                    AddStatusDetailRow(statusGroup, "سبب الرفض", _leave.RejectionReason);
                }
            }
            else if (_leave.Status == 4) // ملغى
            {
                AddStatusDetailRow(statusGroup, "تاريخ الإلغاء",
                    _leave.CancelledDate?.ToString("yyyy/MM/dd HH:mm") ?? "غير متوفر");

                AddStatusDetailRow(statusGroup, "تم الإلغاء بواسطة",
                    _leave.Canceller?.FullName ?? "غير متوفر");

                if (!string.IsNullOrEmpty(_leave.CancellationReason))
                {
                    AddStatusDetailRow(statusGroup, "سبب الإلغاء", _leave.CancellationReason);
                }
            }

            document.Blocks.Add(statusTable);
        }

        private void AddStatusDetailRow(TableRowGroup group, string label, string value)
        {
            TableRow row = new TableRow();
            group.Rows.Add(row);

            TableCell labelCell = new TableCell
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5),
                Background = Brushes.WhiteSmoke
            };
            labelCell.Blocks.Add(new Paragraph(new Run(label))
            {
                FontWeight = FontWeights.SemiBold
            });
            row.Cells.Add(labelCell);

            TableCell valueCell = new TableCell
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5)
            };
            valueCell.Blocks.Add(new Paragraph(new Run(value)));
            row.Cells.Add(valueCell);
        }

        private void AddBalanceInfo(FlowDocument document)
        {
            // عنوان القسم
            Paragraph sectionTitle = new Paragraph(new Run("4. معلومات الرصيد"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // صناديق الرصيد
            Grid balanceGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            // ثلاثة أعمدة
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // إضافة الصناديق
            AddBalanceBox(balanceGrid, 0, "الرصيد الكلي", _totalBalance.ToString(),
                Color.FromRgb(46, 125, 50), "E8F5E8");

            AddBalanceBox(balanceGrid, 1, "المستخدم", _usedBalance.ToString(),
                Color.FromRgb(198, 40, 40), "FFEBEE");

            AddBalanceBox(balanceGrid, 2, "المتبقي", _remainingBalance.ToString(),
                Color.FromRgb(21, 101, 192), "E3F2FD");

            document.Blocks.Add(new BlockUIContainer(balanceGrid));

            // ملاحظات الرصيد
            if (_leaveType?.DeductFromBalance == true && _leave.Status == 2)
            {
                Paragraph balanceNote = new Paragraph(new Run($"ملاحظة: تم خصم {_leave.Duration} يوم من رصيد {_leaveType.Name}"))
                {
                    FontSize = 10,
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Green,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                document.Blocks.Add(balanceNote);
            }
        }

        private void AddBalanceBox(Grid grid, int column, string title, string value, Color titleColor, string backgroundColor)
        {
            Border box = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString($"#{backgroundColor}"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, titleColor.R, titleColor.G, titleColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(5)
            };

            StackPanel content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // العنوان
            TextBlock titleText = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(titleColor),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // القيمة
            TextBlock valueText = new TextBlock
            {
                Text = $"{value} يوم",
                Foreground = new SolidColorBrush(titleColor),
                FontWeight = FontWeights.Bold,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(titleText);
            content.Children.Add(valueText);
            box.Child = content;

            Grid.SetColumn(box, column);
            grid.Children.Add(box);
        }

        private void AddFooter(FlowDocument document)
        {
            // خط فاصل
            document.Blocks.Add(new Paragraph(new Run(new string('─', 100)))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 20, 0, 20)
            });

            // تذييل الصفحة
            Table footerTable = new Table
            {
                CellSpacing = 0
            };

            // ثلاثة أعمدة
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup footerGroup = new TableRowGroup();
            footerTable.RowGroups.Add(footerGroup);

            TableRow row = new TableRow();
            footerGroup.Rows.Add(row);

            // التوقيعات
            string[] signatures = {
                "توقيع الموظف\n\n________________\nالتاريخ: ____/____/____",
                "توقيع رئيس القسم\n\n________________\nالتاريخ: ____/____/____",
                "توقيع مدير الموارد البشرية\n\n________________\nالتاريخ: ____/____/____"
            };

            for (int i = 0; i < 3; i++)
            {
                TableCell cell = new TableCell
                {
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    TextAlignment = System.Windows.TextAlignment.Center
                };

                Paragraph cellParagraph = new Paragraph(new Run(signatures[i]))
                {
                    FontSize = 10,
                    Foreground = Brushes.DimGray
                };

                cell.Blocks.Add(cellParagraph);
                row.Cells.Add(cell);
            }

            document.Blocks.Add(footerTable);

            // رقم الصفحة
            Paragraph pageNumber = new Paragraph(new Run($"صفحة 1 من 1"))
            {
                FontSize = 9,
                Foreground = Brushes.Gray,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            document.Blocks.Add(pageNumber);

            // حقوق النشر
            Paragraph copyright = new Paragraph(new Run("© نظام إدارة الموارد البشرية - جميع الحقوق محفوظة"))
            {
                FontSize = 8,
                Foreground = Brushes.LightGray,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            document.Blocks.Add(copyright);
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "مسودة",
                1 => "قيد الانتظار",
                2 => "موافق عليه",
                3 => "مرفوض",
                4 => "ملغى",
                _ => "غير معروف"
            };
        }

        private Brush GetStatusColor(int status)
        {
            return status switch
            {
                0 => new SolidColorBrush(Colors.Gray),      // مسودة
                1 => new SolidColorBrush(Colors.Orange),    // قيد الانتظار
                2 => new SolidColorBrush(Colors.Green),     // موافق عليه
                3 => new SolidColorBrush(Colors.Red),       // مرفوض
                4 => new SolidColorBrush(Colors.Purple),    // ملغى
                _ => new SolidColorBrush(Colors.Gray)       // غير معروف
            };
        }

        public void Print()
        {
            try
            {
                FlowDocument document = CreatePrintDocument();

                PrintDialog printDialog = new PrintDialog();

                // إعدادات الطابعة الافتراضية
                printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);

                if (printDialog.ShowDialog() == true)
                {
                    // إنشاء IDocumentPaginatorSource من FlowDocument
                    IDocumentPaginatorSource paginatorSource = document;

                    // الطباعة
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator,
                        $"طلب إجازة - {_leave.Id} - {_user?.FullName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PrintToXps(string filePath)
        {
            try
            {
                FlowDocument document = CreatePrintDocument();

                using (XpsDocument xpsDocument = new XpsDocument(filePath, FileAccess.Write))
                {
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                    writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
                }

                MessageBox.Show($"تم حفظ الملف في: {filePath}", "حفظ كـ XPS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ ملف XPS: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    
    }
}