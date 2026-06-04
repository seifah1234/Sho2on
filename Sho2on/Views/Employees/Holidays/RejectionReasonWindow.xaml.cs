using System.Windows; using HR_Application.Helpers;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class RejectionReasonWindow : Window
    {
        public string RejectionReason { get; private set; }

        public RejectionReasonWindow()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ”»» «·—›÷", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RejectionReason = txtReason.Text;
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
