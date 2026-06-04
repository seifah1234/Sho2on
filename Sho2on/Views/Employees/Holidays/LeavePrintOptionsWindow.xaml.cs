using System.Windows; using HR_Application.Helpers;

namespace HR_Application.Views.Employees.Holidays
{
    public enum PrintOption
    {
        Print,
        Preview
    }

    public partial class LeavePrintOptionsWindow : Window
    {
        public PrintOption SelectedOption { get; private set; }

        public LeavePrintOptionsWindow()
        {
            InitializeComponent();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = PrintOption.Print;
            DialogResult = true;
            Close();
        }


        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = PrintOption.Preview;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}