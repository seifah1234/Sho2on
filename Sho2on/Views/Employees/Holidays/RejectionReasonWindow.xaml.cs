using System.Windows;
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
                MessageBox.Show("الرجاء إدخال سبب الرفض", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
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