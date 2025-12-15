using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PdfiumViewer;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HR_Application.Views
{
    public partial class DocumentPreviewWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _documentId;
        private bool _isEmployeeDocument = false;
        private CompanyDocument _companyDocument;
        private EmployeeDocument _employeeDocument;
        private string _filePath;
        private PdfDocument _pdfDocument;
        private PdfViewer _pdfViewer;
        private BitmapImage _imageDocument;
        private string _tempFilePath;

        public DocumentPreviewWindow(int documentId, bool isEmployeeDocument = false)
        {
            InitializeComponent();
            _documentId = documentId;
            _isEmployeeDocument = isEmployeeDocument;
            LoadDocument();
        }

        private async void LoadDocument()
        {
            try
            {
                if (_isEmployeeDocument)
                {
                    _employeeDocument = await _context.EmployeeDocuments
                        .FirstOrDefaultAsync(d => d.Id == _documentId);

                    if (_employeeDocument == null)
                    {
                        MessageBox.Show("الوثيقة غير موجودة", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    Title = $"معاينة: {_employeeDocument.Title}";
                    txtTitle.Text = $"معاينة: {_employeeDocument.Title}";

                    // البحث عن الملف في المسارات الممكنة
                    _filePath = FindDocumentFile(_employeeDocument);
                }
                else
                {
                    _companyDocument = await _context.CompanyDocuments
                        .FirstOrDefaultAsync(d => d.Id == _documentId);

                    if (_companyDocument == null)
                    {
                        MessageBox.Show("الوثيقة غير موجودة", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    Title = $"معاينة: {_companyDocument.Title}";
                    txtTitle.Text = $"معاينة: {_companyDocument.Title}";

                    // البحث عن الملف في المسارات الممكنة
                    _filePath = FindDocumentFile(_companyDocument);
                }

                if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                {
                    ShowMessage("الملف غير موجود على السيرفر");
                    loadingIndicator.Visibility = Visibility.Collapsed;
                    return;
                }

                // تحميل الملف حسب نوعه
                string fileExtension = _isEmployeeDocument ?
                    _employeeDocument.FileType.ToLower() :
                    _companyDocument.FileType.ToLower();

                if (fileExtension == ".pdf")
                {
                    LoadPdfDocument();
                }
                else if (IsImageFile(fileExtension))
                {
                    LoadImageDocument();
                }
                else if (IsOfficeDocument(fileExtension))
                {
                    LoadOfficeDocument();
                }
                else if (fileExtension == ".txt")
                {
                    LoadTextDocument();
                }
                else
                {
                    ShowMessage($"نوع الملف غير مدعوم للمعاينة المباشرة: {fileExtension}\n\nيمكنك تحميل الملف وفتحه يدوياً");
                }

                loadingIndicator.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الوثيقة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private string FindDocumentFile(CompanyDocument document)
        {
            // قائمة المسارات الممكنة للبحث
            string[] possiblePaths = {
                document.FullPath,
                document.FilePath,
                Path.Combine(AppDbContext.CentralStoragePath, "CompanyDocuments", document.FileName),
                Path.Combine(Directory.GetCurrentDirectory(), "CompanyDocuments", document.FileName)
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private string FindDocumentFile(EmployeeDocument document)
        {
            // تحديد المجلد الفرعي بناءً على نوع الوثيقة
            string subFolder = document.DocumentType == EmployeeDocumentType.SignedCompanyDocument ?
                "SignedDocuments" : "EmployeeDocuments";

            // قائمة المسارات الممكنة للبحث
            string[] possiblePaths = {
                document.FullPath,
                document.StoragePath,
                Path.Combine(AppDbContext.CentralStoragePath, subFolder, document.FileName),
                Path.Combine(Directory.GetCurrentDirectory(), subFolder, document.FileName)
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".ico" };
            return imageExtensions.Contains(extension);
        }

        private bool IsOfficeDocument(string extension)
        {
            string[] officeExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
            return officeExtensions.Contains(extension);
        }

        private void LoadPdfDocument()
        {
            try
            {
                _pdfDocument = PdfDocument.Load(_filePath);
                InitializePdfViewer();

                pdfHost.Visibility = Visibility.Visible;
                imageViewer.Visibility = Visibility.Collapsed;
                textViewer.Visibility = Visibility.Collapsed;
                fallbackContainer.Visibility = Visibility.Collapsed;

                UpdatePageInfo();
            }
            catch (Exception ex)
            {
                ShowMessage($"خطأ في تحميل ملف PDF: {ex.Message}");
            }
        }

        private void LoadImageDocument()
        {
            try
            {
                _imageDocument = new BitmapImage();
                _imageDocument.BeginInit();
                _imageDocument.UriSource = new Uri(_filePath);
                _imageDocument.CacheOption = BitmapCacheOption.OnLoad;
                _imageDocument.EndInit();

                imageDisplay.Source = _imageDocument;

                pdfHost.Visibility = Visibility.Collapsed;
                imageViewer.Visibility = Visibility.Visible;
                textViewer.Visibility = Visibility.Collapsed;
                fallbackContainer.Visibility = Visibility.Collapsed;

                UpdatePageInfo();
            }
            catch (Exception ex)
            {
                ShowMessage($"خطأ في تحميل الصورة: {ex.Message}");
            }
        }

        private void LoadTextDocument()
        {
            try
            {
                string text = File.ReadAllText(_filePath);
                textContent.Text = text;

                pdfHost.Visibility = Visibility.Collapsed;
                imageViewer.Visibility = Visibility.Collapsed;
                textViewer.Visibility = Visibility.Visible;
                fallbackContainer.Visibility = Visibility.Collapsed;

                UpdatePageInfo();
            }
            catch (Exception ex)
            {
                ShowMessage($"خطأ في تحميل الملف النصي: {ex.Message}");
            }
        }

        private void LoadOfficeDocument()
        {
            try
            {
                // محاولة تحويل ملفات Office إلى PDF للمعاينة والطباعة
                string pdfPath = ConvertOfficeToPdf(_filePath);
                if (File.Exists(pdfPath))
                {
                    _tempFilePath = pdfPath;
                    _pdfDocument = PdfDocument.Load(pdfPath);
                    InitializePdfViewer();

                    pdfHost.Visibility = Visibility.Visible;
                    imageViewer.Visibility = Visibility.Collapsed;
                    textViewer.Visibility = Visibility.Collapsed;
                    fallbackContainer.Visibility = Visibility.Collapsed;

                    UpdatePageInfo();
                }
                else
                {
                    string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                    string fileType = _isEmployeeDocument ? _employeeDocument.FileType : _companyDocument.FileType;

                    ShowMessage($"للمعاينة الكاملة، يرجى فتح الملف باستخدام البرنامج المناسب\n\nالملف: {documentTitle}\nالنوع: {fileType}\n\nيمكنك تحميل الملف ومعاينته يدوياً");
                }
            }
            catch (Exception ex)
            {
                string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                string fileType = _isEmployeeDocument ? _employeeDocument.FileType : _companyDocument.FileType;

                ShowMessage($"للمعاينة الكاملة، يرجى فتح الملف باستخدام البرنامج المناسب\n\nالملف: {documentTitle}\nالنوع: {fileType}\n\nيمكنك تحميل الملف ومعاينته يدوياً");
            }
        }

        private string ConvertOfficeToPdf(string officeFilePath)
        {
            // في بيئة إنتاجية، يمكنك استخدام مكتبات مثل:
            // - Microsoft.Office.Interop (يتطلب تثبيت Office)
            // - Spire.Doc / Spire.XLS (مكتبات مدفوعة)
            // - LibreOffice (مجاني)

            // حالياً نعيد نفس المسار (لا تحويل)
            return officeFilePath;
        }

        private void InitializePdfViewer()
        {
            try
            {
                _pdfViewer = new PdfViewer();
                _pdfViewer.Dock = System.Windows.Forms.DockStyle.Fill;
                _pdfViewer.Document = _pdfDocument;
                pdfHost.Child = _pdfViewer;
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في تهيئة عارض PDF: {ex.Message}", ex);
            }
        }

        private void ShowMessage(string message)
        {
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Red,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };

            fallbackContainer.Child = textBlock;
            pdfHost.Visibility = Visibility.Collapsed;
            imageViewer.Visibility = Visibility.Collapsed;
            textViewer.Visibility = Visibility.Collapsed;
            fallbackContainer.Visibility = Visibility.Visible;
        }

        private void UpdatePageInfo()
        {
            if (_pdfDocument != null)
            {
                txtPageInfo.Text = $"عدد الصفحات: {_pdfDocument.PageCount}";
                txtPageInfo.Visibility = Visibility.Visible;
            }
            else if (_imageDocument != null)
            {
                txtPageInfo.Text = "صورة";
                txtPageInfo.Visibility = Visibility.Visible;
            }
            else if (!string.IsNullOrEmpty(textContent.Text))
            {
                txtPageInfo.Text = $"مستند نصي - {textContent.Text.Length} حرف";
                txtPageInfo.Visibility = Visibility.Visible;
            }
            else
            {
                txtPageInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void printBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((_companyDocument == null && _employeeDocument == null) || !File.Exists(_filePath)) return;

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    string fileExtension = _isEmployeeDocument ?
                        _employeeDocument.FileType.ToLower() :
                        _companyDocument.FileType.ToLower();

                    if (fileExtension == ".pdf")
                    {
                        PrintPdfDocument(printDialog);
                    }
                    else if (IsImageFile(fileExtension))
                    {
                        PrintImageDocument(printDialog);
                    }
                    else if (fileExtension == ".txt")
                    {
                        PrintTextDocument(printDialog);
                    }
                    else if (IsOfficeDocument(fileExtension))
                    {
                        PrintOfficeDocument(printDialog);
                    }
                    else
                    {
                        // محاولة الطباعة كملف عادي
                        PrintGenericDocument(printDialog);
                    }

                    string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                    MessageBox.Show($"تم إرسال الوثيقة '{documentTitle}' للطباعة", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPdfDocument(PrintDialog printDialog)
        {
            if (_pdfDocument == null) return;

            // PdfiumViewer يدعم الطباعة مباشرة
            using (var printDocument = _pdfDocument.CreatePrintDocument())
            {
                printDocument.PrintController = new System.Drawing.Printing.StandardPrintController();
                printDocument.Print();
            }
        }

        private void PrintImageDocument(PrintDialog printDialog)
        {
            if (_imageDocument == null) return;

            var printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
            var pageSize = new System.Windows.Size(printCapabilities.PageImageableArea.ExtentWidth,
                                                 printCapabilities.PageImageableArea.ExtentHeight);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var imageBrush = new System.Windows.Media.ImageBrush(_imageDocument);
                context.DrawRectangle(imageBrush, null, new System.Windows.Rect(0, 0, pageSize.Width, pageSize.Height));
            }

            string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
            printDialog.PrintVisual(visual, documentTitle);
        }

        private void PrintTextDocument(PrintDialog printDialog)
        {
            var flowDocument = new FlowDocument(new Paragraph(new Run(textContent.Text)))
            {
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontSize = 12,
                PagePadding = new Thickness(50)
            };

            // إنشاء DocumentPaginator للطباعة
            IDocumentPaginatorSource paginatorSource = flowDocument;
            string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
            printDialog.PrintDocument(paginatorSource.DocumentPaginator, documentTitle);
        }

        private void PrintOfficeDocument(PrintDialog printDialog)
        {
            try
            {
                // محاولة الطباعة باستخدام البرنامج الافتراضي
                ProcessStartInfo info = new ProcessStartInfo
                {
                    Verb = "print",
                    FileName = _filePath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(info);
            }
            catch (Exception ex)
            {
                string fileType = _isEmployeeDocument ? _employeeDocument.FileType : _companyDocument.FileType;
                MessageBox.Show($"لا يمكن طباعة ملف {fileType} مباشرة. يرجى فتح الملف وطباعته يدوياً.\n\n{ex.Message}",
                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PrintGenericDocument(PrintDialog printDialog)
        {
            try
            {
                // محاولة فتح الملف بالبرنامج الافتراضي وطباعته
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = _filePath,
                    UseShellExecute = true
                };

                Process.Start(info);

                MessageBox.Show("تم فتح الملف بالبرنامج الافتراضي. يرجى استخدام أمر الطباعة من داخل البرنامج.",
                    "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"لا يمكن طباعة هذا النوع من الملفات مباشرة. يرجى تحميل الملف وطباعته يدوياً.\n\n{ex.Message}",
                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((_companyDocument == null && _employeeDocument == null) || !File.Exists(_filePath)) return;

                string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                string fileType = _isEmployeeDocument ? _employeeDocument.FileType : _companyDocument.FileType;

                var saveDialog = new SaveFileDialog
                {
                    FileName = documentTitle + fileType,
                    Filter = $"{fileType.ToUpper()} files (*{fileType})|*{fileType}|All files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(_filePath, saveDialog.FileName, true);
                    MessageBox.Show($"تم تحميل الوثيقة من:\n{_filePath}", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التحميل: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            CleanupResources();
            this.Close();
        }

        private void CleanupResources()
        {
            _pdfDocument?.Dispose();
            _pdfViewer?.Dispose();

            // حذف الملف المؤقت إذا كان موجوداً
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // تجاهل الأخطاء في حذف الملف المؤقت
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupResources();
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}