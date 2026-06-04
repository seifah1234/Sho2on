using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Documents;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using PrintDialog = System.Windows.Controls.PrintDialog;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace HR_Application.Views
{
    public partial class SalaryPaymentPreviewWindow : Window
    {
        private List<EmployeeSalaryViewModel> _employees;
        private int _month;
        private int _year;

        public SalaryPaymentPreviewWindow(List<EmployeeSalaryViewModel> employees, int month, int year)
        {
            InitializeComponent();
            _employees = employees;
            _month = month;
            _year = year;

            LoadData();
            CalculateTotals();
        }

        private void InitializeComponent()
        {
            Title = "„⁄«Ì‰… ’—› «·„— »« ";
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // «·⁄‰Ê«‰
            var titleBorder = new Border
            {
                Background = System.Windows.Media.Brushes.LightBlue,
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5, 5, 0, 0)
            };

            var titleText = new TextBlock
            {
                Text = $"„⁄«Ì‰… ’—› «·„— »«  - ‘Â— {_month} ”‰… {_year}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            titleBorder.Child = titleText;
            Grid.SetRow(titleBorder, 0);
            grid.Children.Add(titleBorder);

            // DataGrid
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                Margin = new Thickness(10),
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                AlternatingRowBackground = System.Windows.Media.Brushes.LightGray
            };

            var columns = new[]
            {
                new DataGridTextColumn { Header = "«·ﬂÊœ", Binding = new System.Windows.Data.Binding("Code"), Width = 80 },
                new DataGridTextColumn { Header = "«·«”„", Binding = new System.Windows.Data.Binding("Name"), Width = 150 },
                new DataGridTextColumn { Header = "«·›—⁄", Binding = new System.Windows.Data.Binding("Branch"), Width = 100 },
                new DataGridTextColumn { Header = "«·—« » «·√”«”Ì", Binding = new System.Windows.Data.Binding("BasicSalary") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = "«·≈÷«›« ", Binding = new System.Windows.Data.Binding("Additions") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = "«·«” ﬁÿ«⁄« ", Binding = new System.Windows.Data.Binding("Deductions") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = "’‰œÊﬁ «·“„«·…", Binding = new System.Windows.Data.Binding("FriendshipBoxAmount") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = "«·”·›", Binding = new System.Windows.Data.Binding("LoanDeduction") { StringFormat = "N2" }, Width = 80 },
                new DataGridTextColumn { Header = "«·’«›Ì", Binding = new System.Windows.Data.Binding("NetSalary") { StringFormat = "N2" }, Width = 100 }
            };

            foreach (var column in columns)
            {
                dataGrid.Columns.Add(column);
            }

            Grid.SetRow(dataGrid, 1);
            grid.Children.Add(dataGrid);

            // «·„·Œ’ Ê«·√“—«—
            var summaryPanel = new Grid();
            summaryPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            summaryPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var summaryStack = new StackPanel
            {
                Margin = new Thickness(10)
            };

            var totalText = new TextBlock
            {
                Name = "txtTotalSummary",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Blue
            };

            var countText = new TextBlock
            {
                Name = "txtEmployeeCount",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 0)
            };

            summaryStack.Children.Add(totalText);
            summaryStack.Children.Add(countText);

            Grid.SetColumn(summaryStack, 0);
            summaryPanel.Children.Add(summaryStack);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnPrint = new Button
            {
                Content = "ÿ»«⁄…",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Orange,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnPrint.Click += BtnPrint_Click;

            var btnClose = new Button
            {
                Content = "≈€·«ﬁ",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnClose.Click += (s, e) => Close();

            buttonPanel.Children.Add(btnPrint);
            buttonPanel.Children.Add(btnClose);

            Grid.SetColumn(buttonPanel, 1);
            summaryPanel.Children.Add(buttonPanel);

            var border = new Border
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 5, 5)
            };

            border.Child = summaryPanel;
            Grid.SetRow(border, 2);
            grid.Children.Add(border);

            Content = grid;

            //  Œ“Ì‰ «·„—Ã⁄ ··‹ DataGrid
            dataGrid.Name = "dgPreview";
        }

        private void LoadData()
        {
            if (FindName("dgPreview") is DataGrid dataGrid)
            {
                dataGrid.ItemsSource = _employees;
            }
        }

        private void CalculateTotals()
        {
            if (_employees == null || _employees.Count == 0)
                return;

            int count = _employees.Count;
            decimal totalBasic = _employees.Sum(e => e.BasicSalary);
            decimal totalAdditions = _employees.Sum(e => e.Additions);
            decimal totalDeductions = _employees.Sum(e => e.Deductions);
            decimal totalFriendshipBox = _employees.Sum(e => e.FriendshipBoxAmount);
            decimal totalLoanDeduction = _employees.Sum(e => e.LoanDeduction);
            decimal totalNet = _employees.Sum(e => e.NetSalary);

            if (FindName("txtTotalSummary") is TextBlock totalText)
            {
                totalText.Text = $"≈Ã„«·Ì «·’«›Ì: {totalNet:N2} | «·≈÷«›« : {totalAdditions:N2} | «·«” ﬁÿ«⁄« : {totalDeductions:N2} | ’‰œÊﬁ «·“„«·…: {totalFriendshipBox:N2} | «·”·›: {totalLoanDeduction:N2}";
            }

            if (FindName("txtEmployeeCount") is TextBlock countText)
            {
                countText.Text = $"⁄œœ «·„ÊŸ›Ì‰: {count} | ≈Ã„«·Ì «·—Ê« » «·√”«”Ì…: {totalBasic:N2}";
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // ≈‰‘«¡ FlowDocument ··ÿ»«⁄…
                    var document = CreatePrintDocument();
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "„⁄«Ì‰… ’—› «·„— »« ");
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·ÿ»«⁄…: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument()
        {
            var doc = new FlowDocument
            {
                PageWidth = 794, // A4 width in points
                PageHeight = 1123, // A4 height in points
                PagePadding = new Thickness(50),
                ColumnWidth = 694,
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontSize = 12
            };

            // «·⁄‰Ê«‰
            var title = new Paragraph(new Run($"„⁄«Ì‰… ’—› «·„— »«  - ‘Â— {_month} ”‰… {_year}"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(title);

            //  «—ÌŒ «·ÿ»«⁄…
            var printDate = new Paragraph(new Run($" «—ÌŒ «·ÿ»«⁄…: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(printDate);

            // ≈‰‘«¡ «·ÃœÊ·
            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(60) }); // «·ﬂÊœ
            table.Columns.Add(new TableColumn { Width = new GridLength(120) }); // «·«”„
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·›—⁄
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·√”«”Ì
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·≈÷«›« 
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·«” ﬁÿ«⁄« 
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // ’‰œÊﬁ «·“„«·…
            table.Columns.Add(new TableColumn { Width = new GridLength(70) }); // «·”·›
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // «·’«›Ì

            // —√” «·ÃœÊ·
            var headerRow = new TableRow { Background = System.Windows.Media.Brushes.LightGray };
            string[] headers = { "«·ﬂÊœ", "«·«”„", "«·›—⁄", "«·—« » «·√”«”Ì", "«·≈÷«›« ", "«·«” ﬁÿ«⁄« ", "’‰œÊﬁ «·“„«·…", "«·”·›", "«·’«›Ì" };

            foreach (var header in headers)
            {
                var cell = new TableCell(new Paragraph(new Run(header))
                {
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                });
                cell.BorderBrush = System.Windows.Media.Brushes.Black;
                cell.BorderThickness = new Thickness(1);
                cell.Padding = new Thickness(5);
                headerRow.Cells.Add(cell);
            }
            table.RowGroups.Add(new TableRowGroup());
            table.RowGroups[0].Rows.Add(headerRow);

            // »Ì«‰«  «·ÃœÊ·
            foreach (var emp in _employees)
            {
                var row = new TableRow();
                string[] values =
                {
                    emp.Code,
                    emp.Name,
                    emp.Branch,
                    emp.BasicSalary.ToString("N2"),
                    emp.Additions.ToString("N2"),
                    emp.Deductions.ToString("N2"),
                    emp.FriendshipBoxAmount.ToString("N2"),
                    emp.LoanDeduction.ToString("N2"),
                    emp.NetSalary.ToString("N2")
                };

                foreach (var value in values)
                {
                    var cell = new TableCell(new Paragraph(new Run(value))
                    {
                        TextAlignment = TextAlignment.Center
                    });
                    cell.BorderBrush = System.Windows.Media.Brushes.Black;
                    cell.BorderThickness = new Thickness(0.5);
                    cell.Padding = new Thickness(3);
                    row.Cells.Add(cell);
                }
                table.RowGroups[0].Rows.Add(row);
            }

            doc.Blocks.Add(table);

            // «·„·Œ’
            var summary = new Paragraph();
            summary.Inlines.Add(new Run("\n\n"));
            summary.Inlines.Add(new Run("„·Œ’ «·≈Ã„«·Ì« :")
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14
            });
            summary.Inlines.Add(new Run("\n"));

            decimal totalBasic = _employees.Sum(e => e.BasicSalary);
            decimal totalAdditions = _employees.Sum(e => e.Additions);
            decimal totalDeductions = _employees.Sum(e => e.Deductions);
            decimal totalFriendshipBox = _employees.Sum(e => e.FriendshipBoxAmount);
            decimal totalLoanDeduction = _employees.Sum(e => e.LoanDeduction);
            decimal totalNet = _employees.Sum(e => e.NetSalary);

            summary.Inlines.Add(new Run($"⁄œœ «·„ÊŸ›Ì‰: {_employees.Count}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì «·—Ê« » «·√”«”Ì…: {totalBasic:N2}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì «·≈÷«›« : {totalAdditions:N2}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì «·«” ﬁÿ«⁄« : {totalDeductions:N2}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì ’‰œÊﬁ «·“„«·…: {totalFriendshipBox:N2}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì «·”·›: {totalLoanDeduction:N2}\n"));
            summary.Inlines.Add(new Run($"≈Ã„«·Ì «·’«›Ì: {totalNet:N2}\n"));

            doc.Blocks.Add(summary);

            return doc;
        }
    }
}
