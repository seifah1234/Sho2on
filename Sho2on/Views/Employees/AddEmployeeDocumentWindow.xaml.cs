// AddEmployeeDocumentWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using HR_Application.Helpers;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.IO;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Collections.Generic;

namespace HR_Application.Views
{
    public partial class AddEmployeeDocumentWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private string _selectedFilePath;

        // قائمة بأنواع الملفات المسموحة
        private readonly Dictionary<string, string> _supportedFileTypes = new Dictionary<string, string>
        {
            // الصور
            { ".jpg", "JPEG Image" },
            { ".jpeg", "JPEG Image" },
            { ".png", "PNG Image" },
            { ".bmp", "Bitmap Image" },
            { ".gif", "GIF Image" },
            { ".tiff", "TIFF Image" },
            
            // مستندات Office
            { ".doc", "Word Document" },
            { ".docx", "Word Document" },
            { ".xls", "Excel Document" },
            { ".xlsx", "Excel Document" },
            { ".ppt", "PowerPoint Document" },
            { ".pptx", "PowerPoint Document" },
            
            // PDF وملفات أخرى
            { ".pdf", "PDF Document" },
            { ".txt", "Text File" }
        };

        public AddEmployeeDocumentWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            LoadDocumentTypes();
            SetupFileFilter();
        }

        private void SetupFileFilter()
        {
            // بناء فلتر الملفات ديناميكياً
            string allSupported = LocalizationManager.Translate("جميع الملفات المدعومة|");
            string images = LocalizationManager.Translate("الصور|");
            string documents = LocalizationManager.Translate("المستندات|");
            string pdf = "PDF|";

            foreach (var type in _supportedFileTypes)
            {
                string filter = $"*{type.Key};";

                allSupported += filter;

                if (type.Key == ".pdf")
                {
                    pdf += filter;
                }
                else if (type.Key == ".jpg" || type.Key == ".jpeg" || type.Key == ".png" ||
                         type.Key == ".bmp" || type.Key == ".gif" || type.Key == ".tiff")
                {
                    images += filter;
                }
                else
                {
                    documents += filter;
                }
            }

            // إزالة الفاصلة الأخيرة
            allSupported = allSupported.TrimEnd(';');
            images = images.TrimEnd(';');
            documents = documents.TrimEnd(';');
            pdf = pdf.TrimEnd(';');

            browseFileDialog.Filter = $"{allSupported}|{images}|{documents}|{pdf}|جميع الملفات (*.*)|*.*";
            browseFileDialog.FilterIndex = 1;
        }

        // باقي الكود يبقى كما هو مع تعديل بسيط في browseBtn_Click
        private OpenFileDialog browseFileDialog = new OpenFileDialog();

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (browseFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = browseFileDialog.FileName;
                var fileInfo = new FileInfo(_selectedFilePath);

                string fileExtension = fileInfo.Extension.ToLower();
                if (!_supportedFileTypes.ContainsKey(fileExtension))
                {
                    LocalizationManager.ShowMessage("نوع الملف غير مدعوم. يرجى اختيار ملف من الأنواع المسموحة.", LocalizationManager.Translate("تحذير"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedFileText.Text = $"{fileInfo.Name} ({FormatFileSize(fileInfo.Length)}) - {_supportedFileTypes[fileExtension]}";

                if (string.IsNullOrEmpty(titleTextBox.Text))
                {
                    titleTextBox.Text = Path.GetFileNameWithoutExtension(fileInfo.Name);
                }
            }
        }

        // باقي الدوال تبقى كما هي...
        private void LoadDocumentTypes()
        {
            documentTypeComboBox.ItemsSource = Enum.GetValues(typeof(EmployeeDocumentType))
                .Cast<EmployeeDocumentType>()
                .Select(t => new { Value = (int)t, Name = GetDocumentTypeName(t) })
                .ToList();

            documentTypeComboBox.DisplayMemberPath = "Name";
            documentTypeComboBox.SelectedValuePath = "Value";
        }

        private string GetDocumentTypeName(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.TrainingCertificate => LocalizationManager.Translate("التدريب"),
                EmployeeDocumentType.WorkPermit => LocalizationManager.Translate("وثائق التعيين"),
                EmployeeDocumentType.SignedCompanyDocument => LocalizationManager.Translate("وثائق موقعه"),
                EmployeeDocumentType.Other => LocalizationManager.Translate("أخرى"),
                _ => LocalizationManager.Translate("أخرى")
            };
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

        private async void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(titleTextBox.Text) || documentTypeComboBox.SelectedValue == null ||
                string.IsNullOrEmpty(_selectedFilePath))
            {
                LocalizationManager.ShowMessage("يرجى ملء جميع الحقول واختيار ملف", LocalizationManager.Translate("تحذير"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_selectedFilePath);

                // استخدام المسار المركزي لوثائق الموظفين
                string storagePath = AppDbContext.CentralStoragePath;
                string employeeDocumentsPath = Path.Combine(storagePath, "EmployeeDocuments");

                if (!Directory.Exists(employeeDocumentsPath))
                    Directory.CreateDirectory(employeeDocumentsPath);

                // توليد اسم فريد للملف
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileInfo.Name)}{fileInfo.Extension}";
                var destinationPath = Path.Combine(employeeDocumentsPath, fileName);

                // نسخ الملف للمسار المركزي
                File.Copy(_selectedFilePath, destinationPath, true);

                // Save to database
                var document = new EmployeeDocument
                {
                    EmployeeId = _employeeId,
                    Title = titleTextBox.Text,
                    DocumentType = (EmployeeDocumentType)documentTypeComboBox.SelectedValue,
                    FileName = fileName,
                    FileType = Path.GetExtension(_selectedFilePath).ToLower(),
                    FileSize = fileInfo.Length,
                    Description = descriptionTextBox.Text,
                    UploadedBy = App.CurrentUser?.Id ?? 1,
                    Status = DocumentStatus.Active,
                    UploadDate = DateTime.Now,
                    Notes = descriptionTextBox.Text,
                    StoragePath = employeeDocumentsPath,
                    FullPath = destinationPath,
                    StorageType = "Central"
                };

                // إذا كان تاريخ انتهاء محدد
                if (expiryDatePicker.SelectedDate.HasValue)
                {
                    document.ExpiryDate = expiryDatePicker.SelectedDate.Value;
                }

                _context.EmployeeDocuments.Add(document);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage($"تم إضافة الوثيقة بنجاح إلى:\n{destinationPath}", LocalizationManager.Translate("نجاح"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في إضافة الوثيقة: {ex.InnerException?.Message ?? ex.Message}", LocalizationManager.Translate("خطأ"),
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
