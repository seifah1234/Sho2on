using System; using HR_Application.Helpers;
using System.ComponentModel;
using System.Windows; using HR_Application.Helpers;

namespace HR_Application.Views
{
    public partial class ProgressDialog : Window
    {
        public bool IsCancelled { get; private set; }

        public ProgressDialog()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string status, string details = "")
        {
            Dispatcher.Invoke(() =>
            {
                statusText.Text = status;
                if (!string.IsNullOrEmpty(details))
                    detailsText.Text = details;
            });
        }

        public void SetProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Maximum = total;
                progressBar.Value = current;

                double percentage = (current / (double)total) * 100;
                detailsText.Text = $"{current} من {total} ({percentage:0.0}%)";
            });
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}