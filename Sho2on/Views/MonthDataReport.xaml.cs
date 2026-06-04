using System.Windows; using HR_Application.Helpers;
using System.Windows.Documents;

namespace HR_Application.Views
{
    /// <summary>
    /// Interaction logic for MonthDataReport.xaml
    /// </summary>
    public partial class MonthDataReport : Window
    {
        FlowDocument flowDocument1 = new FlowDocument();
        public MonthDataReport(FlowDocument flowDocument)
        {
            InitializeComponent();
            flowDocument1 = flowDocument;
            flowReader.Document = flowDocument;
        }

        private void PrintDocument(FlowDocument document)
        {

            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Print the FlowDocument
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Attendance Report");
            }
        }
        private void print_btn_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument(flowDocument1);
            this.Close();

        }
    }
}
