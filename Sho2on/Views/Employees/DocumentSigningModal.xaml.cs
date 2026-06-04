using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using HR_Application.Helpers;
using System.IO;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views
{
    public partial class DocumentSigningModal : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;

        public DocumentSigningModal(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            LoadAvailableDocuments();
        }

        private async void LoadAvailableDocuments()
        {
            try
            {
                var employee = await _context.Users
                    .FirstOrDefaultAsync(e => e.Id == _employeeId);
                var companyDocuments = await _context.CompanyDocuments
                    .Where(cd => cd.IsActive && cd.IsRequired)
                    .Where(cd =>  (!cd.JobTitleId.HasValue || (cd.JobTitleId.HasValue && cd.JobTitleId.Value == employee.JobTitleId)))
                    .Where(cd => !_context.EmployeeDocuments
                        .Any(ed => ed.EmployeeId == _employeeId &&
                                  ed.DocumentId == cd.Id &&
                                  (ed.Status == DocumentStatus.Signed || ed.Status == DocumentStatus.Active)))
                    .ToListAsync();

                documentsListBox.ItemsSource = companyDocuments;

                // ≈ŸÂ«— √Ê ≈Œ›«¡ «·Õ«·… «·›«—€…
                if (companyDocuments.Count == 0)
                {
                    emptyState.Visibility = Visibility.Visible;
                    statusText.Text = "·«  ÊÃœ ÊÀ«∆ﬁ „ «Õ… ·· ÊﬁÌ⁄ - Ã„Ì⁄ «·ÊÀ«∆ﬁ „Êﬁ⁄…";
                }
                else
                {
                    emptyState.Visibility = Visibility.Collapsed;
                    statusText.Text = $"ÌÊÃœ {companyDocuments.Count} ÊÀÌﬁ… „ «Õ… ·· ÊﬁÌ⁄";
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·ÊÀ«∆ﬁ: {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "ÕœÀ Œÿ√ ›Ì  Õ„Ì· «·ÊÀ«∆ﬁ";
            }
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedDocument = button?.Tag as CompanyDocument;

            if (selectedDocument == null)
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— ÊÀÌﬁ… ··„⁄«Ì‰…", " Õ–Ì—",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewWindow = new DocumentPreviewWindow(selectedDocument.Id);
            previewWindow.Owner = this;
            previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            previewWindow.ShowDialog();
        }

        private void signBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedDocument = button?.Tag as CompanyDocument;

            if (selectedDocument == null)
            {
                LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— ÊÀÌﬁ… ·· ÊﬁÌ⁄", " Õ–Ì—",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var signatureWindow = new DocumentSignatureWindow(_employeeId, selectedDocument.Id);
            signatureWindow.Closed += (s, args) =>
            {
                // ≈⁄«œ…  Õ„Ì· «·ﬁ«∆„… »⁄œ «· ÊﬁÌ⁄
                LoadAvailableDocuments();

                // ≈–« ﬂ«‰  «·‰«›–… «·—∆Ì”Ì…  Õ «Ã ·· ÕœÌÀ
                if (this.Owner is EmployeeArchiveWindow mainWindow)
                {
                    // Ì„ﬂ‰ ≈÷«›… „‰ÿﬁ · ÕœÌÀ «·√—‘Ì› ≈–« ·“„ «·√„—
                }
            };
            signatureWindow.Owner = this;
            signatureWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            signatureWindow.ShowDialog();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // „⁄«·Ã… «Œ Ì«— ⁄‰’— „‰ «·ﬁ«∆„…
        private void documentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ì„ﬂ‰ ≈÷«›… „‰ÿﬁ ≈÷«›Ì Â‰« ≈–« «Õ Ã‰«
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}
