using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.IO;
using System.Printing;
using System.Windows; using HR_Application.Helpers;
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
            // ≈‰‘«¡ FlowDocument ÃœÌœ
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

            // ≈÷«›… —√” «·’›Õ…
            AddHeader(document);

            // ≈÷«›… „⁄·Ê„«  «·„ÊŸ›
            AddEmployeeInfo(document);

            // ≈÷«›… „⁄·Ê„«  «·≈Ã«“…
            AddLeaveInfo(document);

            // ≈÷«›… Õ«·… «·ÿ·»
            AddStatusInfo(document);

            // ≈÷«›… „⁄·Ê„«  «·—’Ìœ
            AddBalanceInfo(document);

            // ≈÷«›…  –ÌÌ· «·’›Õ…
            AddFooter(document);

            return document;
        }

        private void AddHeader(FlowDocument document)
        {
            // «·ﬁ”„ «·⁄·ÊÌ „⁄ «·‘⁄«— Ê«·⁄‰Ê«‰
            Table headerTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // À·«À… √⁄„œ…: ‘⁄«— - ⁄‰Ê«‰ -  «—ÌŒ
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(300) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(95) });

            TableRowGroup headerGroup = new TableRowGroup();
            headerTable.RowGroups.Add(headerGroup);

            // «·’› «·√Ê·: «·⁄‰Ê«‰ «·—∆Ì”Ì
            TableRow titleRow = new TableRow();
            headerGroup.Rows.Add(titleRow);
/*
            // «·Œ·Ì… «·√Ê·Ï: «·‘⁄«— (Ì„ﬂ‰ﬂ ≈÷«›… ’Ê—… Â‰«)
            TableCell logoCell = new TableCell
            {
                ColumnSpan = 1,
                BorderThickness = new Thickness(0)
            };
            logoCell.Blocks.Add(new Paragraph(new Run("‘⁄«— «·‘—ﬂ…"))
            {
                FontSize = 10,
                Foreground = Brushes.Gray
            });
            titleRow.Cells.Add(logoCell);*/

            // «·Œ·Ì… «·À«‰Ì…: «·⁄‰Ê«‰
            TableCell titleCell = new TableCell
            {
                ColumnSpan = 1,
                TextAlignment = System.Windows.TextAlignment.Center,
                BorderThickness = new Thickness(0)
            };

            Paragraph titleParagraph = new Paragraph();
            titleParagraph.Inlines.Add(new Run("ÿ·» ≈Ã«“…")
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
            });

            titleCell.Blocks.Add(titleParagraph);
            titleRow.Cells.Add(titleCell);

            // «·Œ·Ì… «·À«·À…: «· «—ÌŒ
            TableCell dateCell = new TableCell
            {
                ColumnSpan = 1,
                TextAlignment = System.Windows.TextAlignment.Left,
                BorderThickness = new Thickness(0)
            };

            Paragraph dateParagraph = new Paragraph();
            dateParagraph.Inlines.Add(new Run($" «—ÌŒ «·ÿ»«⁄…: {DateTime.Now:yyyy/MM/dd HH:mm}")
            {
                FontSize = 9,
                Foreground = Brushes.Gray
            });
            dateParagraph.Inlines.Add(new LineBreak());
            dateParagraph.Inlines.Add(new Run($"—ﬁ„ «·ÿ·»: {_leave.Id}")
            {
                FontSize = 9,
                Foreground = Brushes.Gray
            });

            dateCell.Blocks.Add(dateParagraph);
            titleRow.Cells.Add(dateCell);

            document.Blocks.Add(headerTable);

            // Œÿ ›«’·
            document.Blocks.Add(new Paragraph(new Run(new string('?', 100)))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 20)
            });
        }

        private void AddEmployeeInfo(FlowDocument document)
        {
            // ⁄‰Ê«‰ «·ﬁ”„
            Paragraph sectionTitle = new Paragraph(new Run("1. „⁄·Ê„«  «·„ÊŸ›"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // ÃœÊ· „⁄·Ê„«  «·„ÊŸ›
            Table employeeTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            //  ⁄—Ì› «·√⁄„œ…
            for (int i = 0; i < 4; i++)
            {
                employeeTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            }

            TableRowGroup employeeGroup = new TableRowGroup();
            employeeTable.RowGroups.Add(employeeGroup);

            // ≈÷«›… «·’›Ê›
            string[,] employeeData = {
                { "ﬂÊœ «·„ÊŸ›", _user?.Id.ToString() ?? "€Ì— „ Ê›—", "«”„ «·„ÊŸ›", _user?.FullName ?? "€Ì— „ Ê›—" },
                { "«·≈œ«—…", _user?.Department?.Name ?? "€Ì— „ Ê›—", "«·›—⁄", _user?.Branch?.Name ?? "€Ì— „ Ê›—" },
                { " «—ÌŒ «· ⁄ÌÌ‰", _user?.HireDate.ToString("yyyy/MM/dd") ?? "€Ì— „ Ê›—", "«·ÊŸÌ›…", _user?.JobTitle?.Name ?? "€Ì— „ Ê›—" },
                { "‰Ÿ«„ «·⁄„·", _user?.JobType?.Name ?? "€Ì— „ Ê›—", "«·Ê—œÌ…", _user?.Shift?.Name ?? "€Ì— „ Ê›—" }
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

                    if (j % 2 == 0) //  ”„Ì« 
                    {
                        cellParagraph.Inlines.Add(new Run(employeeData[i, j])
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.DimGray
                        });
                    }
                    else // ﬁÌ„
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
            // ⁄‰Ê«‰ «·ﬁ”„
            Paragraph sectionTitle = new Paragraph(new Run("2. „⁄·Ê„«  «·≈Ã«“…"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // ÃœÊ· „⁄·Ê„«  «·≈Ã«“…
            Table leaveTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            //  ⁄—Ì› «·√⁄„œ…
            for (int i = 0; i < 4; i++)
            {
                leaveTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            }

            TableRowGroup leaveGroup = new TableRowGroup();
            leaveTable.RowGroups.Add(leaveGroup);

            // ≈÷«›… «·’›Ê›
            string[,] leaveData = {
                { "‰Ê⁄ «·≈Ã«“…", _leaveType?.Name ?? "€Ì— „ Ê›—", "—ﬁ„ «·‰Ê⁄", _leaveType?.Code ?? "€Ì— „ Ê›—" },
                { "„‰  «—ÌŒ", _leave.StartDate.ToString("yyyy/MM/dd"), "≈·Ï  «—ÌŒ", _leave.EndDate.ToString("yyyy/MM/dd") },
                { "«·„œ…", $"{_leave.Duration} ÌÊ„", " «—ÌŒ «·ÿ·»", _leave.RequestDate.ToString("yyyy/MM/dd HH:mm") },
                { "Ì ÿ·» „Ê«›ﬁ…", _leaveType?.RequiresApproval == true ? "‰⁄„" : "·«",
                  "ÌŒ’„ „‰ «·—’Ìœ", _leaveType?.DeductFromBalance == true ? "‰⁄„" : "·«" }
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

                    if (j % 2 == 0) //  ”„Ì« 
                    {
                        cellParagraph.Inlines.Add(new Run(leaveData[i, j])
                        {
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.DimGray
                        });
                    }
                    else // ﬁÌ„
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

            // ﬁ”„ «·”»»
            Paragraph reasonTitle = new Paragraph(new Run("”»» «·≈Ã«“…:"))
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

            Paragraph reasonParagraph = new Paragraph(new Run(_leave.Reason ?? "·« ÌÊÃœ ”»»"))
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
            // ⁄‰Ê«‰ «·ﬁ”„
            Paragraph sectionTitle = new Paragraph(new Run("3. Õ«·… «·ÿ·»"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // Õ«·… «·ÿ·»
            Table statusTable = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };

            //  ⁄—Ì› «·√⁄„œ…
            statusTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            statusTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup statusGroup = new TableRowGroup();
            statusTable.RowGroups.Add(statusGroup);

            string statusText = GetStatusText(_leave.Status);
            Brush statusColor = GetStatusColor(_leave.Status);

            // ’› «·Õ«·…
            TableRow statusRow = new TableRow();
            statusGroup.Rows.Add(statusRow);

            TableCell statusLabelCell = new TableCell
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(8, 5, 8, 5),
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 152, 0))
            };
            statusLabelCell.Blocks.Add(new Paragraph(new Run("«·Õ«·…:"))
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

            // „⁄·Ê„«  ≈÷«›Ì… Õ”» «·Õ«·…
            if (_leave.Status == 2 || _leave.Status == 3) // „Ê«›ﬁ ⁄·ÌÂ √Ê „—›Ê÷
            {
                AddStatusDetailRow(statusGroup, " «—ÌŒ «·„Ê«›ﬁ…/«·—›÷",
                    _leave.ApprovalDate?.ToString("yyyy/MM/dd HH:mm") ?? "€Ì— „ Ê›—");

                AddStatusDetailRow(statusGroup, " „  «·„Ê«›ﬁ… »Ê«”ÿ…",
                    _leave.Approver?.FullName ?? "€Ì— „ Ê›—");

                if (_leave.Status == 3 && !string.IsNullOrEmpty(_leave.RejectionReason))
                {
                    AddStatusDetailRow(statusGroup, "”»» «·—›÷", _leave.RejectionReason);
                }
            }
            else if (_leave.Status == 4) // „·€Ï
            {
                AddStatusDetailRow(statusGroup, " «—ÌŒ «·≈·€«¡",
                    _leave.CancelledDate?.ToString("yyyy/MM/dd HH:mm") ?? "€Ì— „ Ê›—");

                AddStatusDetailRow(statusGroup, " „ «·≈·€«¡ »Ê«”ÿ…",
                    _leave.Canceller?.FullName ?? "€Ì— „ Ê›—");

                if (!string.IsNullOrEmpty(_leave.CancellationReason))
                {
                    AddStatusDetailRow(statusGroup, "”»» «·≈·€«¡", _leave.CancellationReason);
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
            // ⁄‰Ê«‰ «·ﬁ”„
            Paragraph sectionTitle = new Paragraph(new Run("4. „⁄·Ê„«  «·—’Ìœ"))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(sectionTitle);

            // ’‰«œÌﬁ «·—’Ìœ
            Grid balanceGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            // À·«À… √⁄„œ…
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            balanceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ≈÷«›… «·’‰«œÌﬁ
            AddBalanceBox(balanceGrid, 0, "«·—’Ìœ «·ﬂ·Ì", _totalBalance.ToString(),
                Color.FromRgb(46, 125, 50), "E8F5E8");

            AddBalanceBox(balanceGrid, 1, "«·„” Œœ„", _usedBalance.ToString(),
                Color.FromRgb(198, 40, 40), "FFEBEE");

            AddBalanceBox(balanceGrid, 2, "«·„ »ﬁÌ", _remainingBalance.ToString(),
                Color.FromRgb(21, 101, 192), "E3F2FD");

            document.Blocks.Add(new BlockUIContainer(balanceGrid));

            // „·«ÕŸ«  «·—’Ìœ
            if (_leaveType?.DeductFromBalance == true && _leave.Status == 2)
            {
                Paragraph balanceNote = new Paragraph(new Run($"„·«ÕŸ…:  „ Œ’„ {_leave.Duration} ÌÊ„ „‰ —’Ìœ {_leaveType.Name}"))
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

            // «·⁄‰Ê«‰
            TextBlock titleText = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(titleColor),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // «·ﬁÌ„…
            TextBlock valueText = new TextBlock
            {
                Text = $"{value} ÌÊ„",
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
            // Œÿ ›«’·
            document.Blocks.Add(new Paragraph(new Run(new string('?', 100)))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 20, 0, 20)
            });

            //  –ÌÌ· «·’›Õ…
            Table footerTable = new Table
            {
                CellSpacing = 0
            };

            // À·«À… √⁄„œ…
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup footerGroup = new TableRowGroup();
            footerTable.RowGroups.Add(footerGroup);

            TableRow row = new TableRow();
            footerGroup.Rows.Add(row);

            // «· ÊﬁÌ⁄« 
            string[] signatures = {
                " ÊﬁÌ⁄ «·„ÊŸ›\n\n________________\n«· «—ÌŒ: ____/____/____",
                " ÊﬁÌ⁄ —∆Ì” «·ﬁ”„\n\n________________\n«· «—ÌŒ: ____/____/____",
                " ÊﬁÌ⁄ „œÌ— «·„Ê«—œ «·»‘—Ì…\n\n________________\n«· «—ÌŒ: ____/____/____"
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

            // —ﬁ„ «·’›Õ…
            Paragraph pageNumber = new Paragraph(new Run($"’›Õ… 1 „‰ 1"))
            {
                FontSize = 9,
                Foreground = Brushes.Gray,
                TextAlignment = System.Windows.TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            document.Blocks.Add(pageNumber);

            // ÕﬁÊﬁ «·‰‘—
            Paragraph copyright = new Paragraph(new Run("© ‰Ÿ«„ ≈œ«—… «·„Ê«—œ «·»‘—Ì… - Ã„Ì⁄ «·ÕﬁÊﬁ „Õ›ÊŸ…"))
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
                0 => "„”Êœ…",
                1 => "ﬁÌœ «·«‰ Ÿ«—",
                2 => "„Ê«›ﬁ ⁄·ÌÂ",
                3 => "„—›Ê÷",
                4 => "„·€Ï",
                _ => "€Ì— „⁄—Ê›"
            };
        }

        private Brush GetStatusColor(int status)
        {
            return status switch
            {
                0 => new SolidColorBrush(Colors.Gray),      // „”Êœ…
                1 => new SolidColorBrush(Colors.Orange),    // ﬁÌœ «·«‰ Ÿ«—
                2 => new SolidColorBrush(Colors.Green),     // „Ê«›ﬁ ⁄·ÌÂ
                3 => new SolidColorBrush(Colors.Red),       // „—›Ê÷
                4 => new SolidColorBrush(Colors.Purple),    // „·€Ï
                _ => new SolidColorBrush(Colors.Gray)       // €Ì— „⁄—Ê›
            };
        }

        public void Print()
        {
            try
            {
                FlowDocument document = CreatePrintDocument();

                PrintDialog printDialog = new PrintDialog();

                // ≈⁄œ«œ«  «·ÿ«»⁄… «·«› —«÷Ì…
                printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);

                if (printDialog.ShowDialog() == true)
                {
                    // ≈‰‘«¡ IDocumentPaginatorSource „‰ FlowDocument
                    IDocumentPaginatorSource paginatorSource = document;

                    // «·ÿ»«⁄…
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator,
                        $"ÿ·» ≈Ã«“… - {_leave.Id} - {_user?.FullName}");
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·ÿ»«⁄…: {ex.Message}", "Œÿ√",
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

                LocalizationManager.ShowMessage($" „ Õ›Ÿ «·„·› ›Ì: {filePath}", "Õ›Ÿ ﬂ‹ XPS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ „·› XPS: {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    
    }
}
