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
            Title = LocalizationManager.Translate("معاينة صرف المرتبات");
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // العنوان
            var titleBorder = new Border
            {
                Background = System.Windows.Media.Brushes.LightBlue,
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(5, 5, 0, 0)
            };

            var titleText = new TextBlock
            {
                Text = $"معاينة صرف المرتبات - شهر {_month} سنة {_year}",
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
                new DataGridTextColumn { Header = LocalizationManager.Translate("الكود"), Binding = new System.Windows.Data.Binding("Code"), Width = 80 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الاسم"), Binding = new System.Windows.Data.Binding("Name"), Width = 150 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الفرع"), Binding = new System.Windows.Data.Binding("Branch"), Width = 100 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الراتب الأساسي"), Binding = new System.Windows.Data.Binding("BasicSalary") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الإضافات"), Binding = new System.Windows.Data.Binding("Additions") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الاستقطاعات"), Binding = new System.Windows.Data.Binding("Deductions") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("صندوق الزمالة"), Binding = new System.Windows.Data.Binding("FriendshipBoxAmount") { StringFormat = "N2" }, Width = 100 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("السلف"), Binding = new System.Windows.Data.Binding("LoanDeduction") { StringFormat = "N2" }, Width = 80 },
                new DataGridTextColumn { Header = LocalizationManager.Translate("الصافي"), Binding = new System.Windows.Data.Binding("NetSalary") { StringFormat = "N2" }, Width = 100 }
            };

            foreach (var column in columns)
            {
                dataGrid.Columns.Add(column);
            }

            Grid.SetRow(dataGrid, 1);
            grid.Children.Add(dataGrid);

            // الملخص والأزرار
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
                Content = LocalizationManager.Translate("طباعة"),
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
                Content = LocalizationManager.Translate("إغلاق"),
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

            // تخزين المرجع للـ DataGrid
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
                totalText.Text = $"إجمالي الصافي: {totalNet:N2} | الإضافات: {totalAdditions:N2} | الاستقطاعات: {totalDeductions:N2} | صندوق الزمالة: {totalFriendshipBox:N2} | السلف: {totalLoanDeduction:N2}";
            }

            if (FindName("txtEmployeeCount") is TextBlock countText)
            {
                countText.Text = $"عدد الموظفين: {count} | إجمالي الرواتب الأساسية: {totalBasic:N2}";
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // إنشاء FlowDocument للطباعة
                    var document = CreatePrintDocument();
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, LocalizationManager.Translate("معاينة صرف المرتبات"));
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في الطباعة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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

            // العنوان
            var title = new Paragraph(new Run($"معاينة صرف المرتبات - شهر {_month} سنة {_year}"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(title);

            // تاريخ الطباعة
            var printDate = new Paragraph(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(printDate);

            // إنشاء الجدول
            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(60) }); // الكود
            table.Columns.Add(new TableColumn { Width = new GridLength(120) }); // الاسم
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الفرع
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الأساسي
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الإضافات
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الاستقطاعات
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // صندوق الزمالة
            table.Columns.Add(new TableColumn { Width = new GridLength(70) }); // السلف
            table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // الصافي

            // رأس الجدول
            var headerRow = new TableRow { Background = System.Windows.Media.Brushes.LightGray };
            string[] headers = { LocalizationManager.Translate("الكود"), LocalizationManager.Translate("الاسم"), LocalizationManager.Translate("الفرع"), LocalizationManager.Translate("الراتب الأساسي"), LocalizationManager.Translate("الإضافات"), LocalizationManager.Translate("الاستقطاعات"), LocalizationManager.Translate("صندوق الزمالة"), LocalizationManager.Translate("السلف"), LocalizationManager.Translate("الصافي") };

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

            // بيانات الجدول
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

            // الملخص
            var summary = new Paragraph();
            summary.Inlines.Add(new Run("\n\n"));
            summary.Inlines.Add(new Run(LocalizationManager.Translate("ملخص الإجماليات:"))
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

            summary.Inlines.Add(new Run($"عدد الموظفين: {_employees.Count}\n"));
            summary.Inlines.Add(new Run($"إجمالي الرواتب الأساسية: {totalBasic:N2}\n"));
            summary.Inlines.Add(new Run($"إجمالي الإضافات: {totalAdditions:N2}\n"));
            summary.Inlines.Add(new Run($"إجمالي الاستقطاعات: {totalDeductions:N2}\n"));
            summary.Inlines.Add(new Run($"إجمالي صندوق الزمالة: {totalFriendshipBox:N2}\n"));
            summary.Inlines.Add(new Run($"إجمالي السلف: {totalLoanDeduction:N2}\n"));
            summary.Inlines.Add(new Run($"إجمالي الصافي: {totalNet:N2}\n"));

            doc.Blocks.Add(summary);

            return doc;
        }
    }
}
