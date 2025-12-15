using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
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

                // إظهار أو إخفاء الحالة الفارغة
                if (companyDocuments.Count == 0)
                {
                    emptyState.Visibility = Visibility.Visible;
                    statusText.Text = "لا توجد وثائق متاحة للتوقيع - جميع الوثائق موقعة";
                }
                else
                {
                    emptyState.Visibility = Visibility.Collapsed;
                    statusText.Text = $"يوجد {companyDocuments.Count} وثيقة متاحة للتوقيع";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الوثائق: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "حدث خطأ في تحميل الوثائق";
            }
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedDocument = button?.Tag as CompanyDocument;

            if (selectedDocument == null)
            {
                MessageBox.Show("يرجى اختيار وثيقة للمعاينة", "تحذير",
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
                MessageBox.Show("يرجى اختيار وثيقة للتوقيع", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var signatureWindow = new DocumentSignatureWindow(_employeeId, selectedDocument.Id);
            signatureWindow.Closed += (s, args) =>
            {
                // إعادة تحميل القائمة بعد التوقيع
                LoadAvailableDocuments();

                // إذا كانت النافذة الرئيسية تحتاج للتحديث
                if (this.Owner is EmployeeArchiveWindow mainWindow)
                {
                    // يمكن إضافة منطق لتحديث الأرشيف إذا لزم الأمر
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

        // معالجة اختيار عنصر من القائمة
        private void documentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق إضافي هنا إذا احتجنا
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}