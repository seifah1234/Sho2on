using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using static HR_Application.MonthlyData;

namespace HR_Application
{
    public partial class Report1 : Window
    {
        public Report1(ObservableCollection<MonthData> monthDatas, string employeeName, string employeeCode, string year, string month)
        {
            InitializeComponent();

            InputBindings.Add(new KeyBinding(ApplicationCommands.Print, Key.P, ModifierKeys.Control));


            // Calculate total work hours
            TimeSpan totalWorkHours = TimeSpan.Zero;
            foreach (var data in monthDatas)
            {
                totalWorkHours += data.workHours;
            }

            // Create and set the ViewModel
            ReportViewModel viewModel = new ReportViewModel();
            viewModel.MonthDatas = monthDatas;
            viewModel.EmployeeName = employeeName;
            viewModel.EmployeeCode = employeeCode;
            viewModel.Year = year;
            viewModel.Month = month;
            viewModel.TotalWorkHours = totalWorkHours.ToString(@"hh\:mm");

            // Set DataContext
            this.DataContext = viewModel;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                PrintDocument();
                e.Handled = true; // Mark the event as handled to prevent other actions
            }
        }

        private void PrintDocument()
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                // Create a VisualBrush to render the DataGrid
                VisualBrush visualBrush = new VisualBrush(ReportDataGrid);
                Rect bounds = VisualTreeHelper.GetDescendantBounds(ReportDataGrid);

                // Create a new FixedDocument
                FixedDocument fixedDoc = new FixedDocument();
                PageContent pageContent = new PageContent();
                FixedPage fixedPage = new FixedPage();
                fixedPage.Width = bounds.Width;
                fixedPage.Height = bounds.Height;
                fixedPage.Background = System.Windows.Media.Brushes.White;

                // Create a new DataGrid instance for printing
                DataGrid printDataGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    ItemsSource = ReportDataGrid.ItemsSource
                };

                // Copy the columns from the original DataGrid to the new one
                foreach (var column in ReportDataGrid.Columns)
                {
                    printDataGrid.Columns.Add(column);
                }

                // Set up the new DataGrid
                Border border = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    Child = printDataGrid,
                    Width = bounds.Width,
                    Height = bounds.Height
                };
                fixedPage.Children.Add(border);

                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDoc.Pages.Add(pageContent);

                // Print the document
                printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Printing Report");
            }
        }

        private void print_btn_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument();
        }
    }

    
}
