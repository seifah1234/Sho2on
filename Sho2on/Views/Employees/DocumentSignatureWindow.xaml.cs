// DocumentSignatureWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace HR_Application.Views
{
    public partial class DocumentSignatureWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private int _documentId;
        private string _signedFilePath;

        public DocumentSignatureWindow(int employeeId, int documentId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _documentId = documentId;
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var document = await _context.CompanyDocuments
                    .FirstOrDefaultAsync(d => d.Id == _documentId);

                var employee = await _context.Users
                    .FirstOrDefaultAsync(e => e.Id == _employeeId);

                if (document != null && employee != null)
                {
                    txtDocumentTitle.Text = document.Title;
                    txtEmployeeName.Text = employee.FullName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void browseSignedFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf|Image files (*.jpg;*.png)|*.jpg;*.png|All files (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _signedFilePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(_signedFilePath);
                txtSignedFile.Text = $"{fileInfo.Name} ({FormatFileSize(fileInfo.Length)})";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void signBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_signedFilePath))
            {
                MessageBox.Show("يرجى اختيار الملف الموقع", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_signedFilePath);

                // استخدام المسار المركزي للتوقيعات
                string storagePath = AppDbContext.CentralStoragePath;
                string signedDocumentsPath = Path.Combine(storagePath, "SignedDocuments");

                if (!Directory.Exists(signedDocumentsPath))
                    Directory.CreateDirectory(signedDocumentsPath);

                // توليد اسم فريد للملف
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileInfo.Name)}{fileInfo.Extension}";
                var destinationPath = Path.Combine(signedDocumentsPath, fileName);

                // نسخ الملف الموقع للمسار المركزي
                File.Copy(_signedFilePath, destinationPath, true);

                // Get company document info
                var companyDocument = await _context.CompanyDocuments
                    .FirstOrDefaultAsync(cd => cd.Id == _documentId);

                if (companyDocument == null)
                {
                    MessageBox.Show("الوثيقة غير موجودة", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create employee document record
                var employeeDocument = new EmployeeDocument
                {
                    EmployeeId = _employeeId,
                    DocumentId = _documentId,
                    Title = companyDocument.Title,
                    DocumentType = EmployeeDocumentType.SignedCompanyDocument,
                    FileName = fileName,
                    FileType = Path.GetExtension(_signedFilePath),
                    FileSize = fileInfo.Length,
                    Description = companyDocument.Description,
                    UploadedBy = App.CurrentUser?.Id ?? 1,
                    Status = DocumentStatus.Signed,
                    SignedDate = DateTime.Now,
                    UploadDate = DateTime.Now,
                    Notes = txtNotes.Text,
                    IsActive = true,
                    StoragePath = signedDocumentsPath,
                    FullPath = destinationPath,
                    StorageType = "Central"
                };

                _context.EmployeeDocuments.Add(employeeDocument);
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم توقيع الوثيقة بنجاح وحفظها في:\n{destinationPath}", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في توقيع الوثيقة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}