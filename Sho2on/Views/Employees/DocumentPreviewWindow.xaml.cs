using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using HR_Application.Helpers;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Rendering.Skia;
using UglyToad.PdfPig.Graphics.Colors;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Brushes = System.Windows.Media.Brushes;
using Application = System.Windows.Application;
using FlowDirection = System.Windows.FlowDirection;

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

        // ááãÓÊäÏÇÊ ÇáäÕíÉ æÇáÕæÑ
        private BitmapImage _imageDocument;
        private string _textContent;
        private int _pdfPageCount = 0;
        private int _currentPdfPage = 1;

        // ááãÚÇíäÉ ÇáãÄŞÊÉ
        private string _tempPdfImagesFolder;

        public DocumentPreviewWindow(int documentId, bool isEmployeeDocument = false)
        {
            InitializeComponent();
            _documentId = documentId;
            _isEmployeeDocument = isEmployeeDocument;

            // ÅÚÏÇÏ ÃÍÏÇË ÇáÕİÍÇÊ
            SetupPdfNavigation();

            LoadDocument();
        }

        private void SetupPdfNavigation()
        {
            btnPrevPage.Click += (s, e) => ChangePdfPage(-1);
            btnNextPage.Click += (s, e) => ChangePdfPage(1);
            btnFirstPage.Click += (s, e) => GoToPdfPage(1);
            btnLastPage.Click += (s, e) => GoToPdfPage(_pdfPageCount);
        }

        private async void LoadDocument()
        {
            try
            {
                loadingIndicator.Visibility = Visibility.Visible;

                if (_isEmployeeDocument)
                {
                    _employeeDocument = await _context.EmployeeDocuments
                        .FirstOrDefaultAsync(d => d.Id == _documentId);

                    if (_employeeDocument == null)
                    {
                        ShowErrorAndClose("ÇáæËíŞÉ ÛíÑ ãæÌæÏÉ");
                        return;
                    }

                    Title = $"ãÚÇíäÉ: {_employeeDocument.Title}";
                    txtTitle.Text = $"ãÚÇíäÉ: {_employeeDocument.Title}";
                    _filePath = FindDocumentFile(_employeeDocument);
                }
                else
                {
                    _companyDocument = await _context.CompanyDocuments
                        .FirstOrDefaultAsync(d => d.Id == _documentId);

                    if (_companyDocument == null)
                    {
                        ShowErrorAndClose("ÇáæËíŞÉ ÛíÑ ãæÌæÏÉ");
                        return;
                    }

                    Title = $"ãÚÇíäÉ: {_companyDocument.Title}";
                    txtTitle.Text = $"ãÚÇíäÉ: {_companyDocument.Title}";
                    _filePath = FindDocumentFile(_companyDocument);
                }

                if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                {
                    ShowFallbackMessage("Çáãáİ ÛíÑ ãæÌæÏ Úáì ÇáÓíÑİÑ");
                    return;
                }

                await LoadDocumentByTypeAsync();
            }
            catch (Exception ex)
            {
                ShowFallbackMessage($"ÍÏË ÎØÃ: {ex.Message}");
            }
            finally
            {
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task LoadDocumentByTypeAsync()
        {
            string fileExtension = GetFileExtension().ToLower();

            if (fileExtension == ".pdf")
            {
                await LoadPdfDocumentAsync();
            }
            else if (IsImageFile(fileExtension))
            {
                LoadImageDocument();
            }
            else if (fileExtension == ".txt")
            {
                LoadTextDocument();
            }
            else
            {
                ShowFallbackMessage($"äæÚ Çáãáİ ÛíÑ ãÏÚæã ááãÚÇíäÉ ÇáãÈÇÔÑÉ: {fileExtension}\n\níãßäß ÊÍãíá Çáãáİ æİÊÍå íÏæíÇğ");
            }
        }

        private string GetFileExtension()
        {
            return _isEmployeeDocument ? _employeeDocument.FileType : _companyDocument.FileType;
        }

        private bool IsImageFile(string extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".ico" };
            return imageExtensions.Contains(extension);
        }

        private string FindDocumentFile(CompanyDocument document)
        {
            string[] possiblePaths = {
                document.FullPath,
                document.FilePath,
                Path.Combine(AppDbContext.CentralStoragePath, "CompanyDocuments", document.FileName),
                Path.Combine(Directory.GetCurrentDirectory(), "CompanyDocuments", document.FileName)
            };

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        private string FindDocumentFile(EmployeeDocument document)
        {
            string subFolder = document.DocumentType == EmployeeDocumentType.SignedCompanyDocument ?
                "SignedDocuments" : "EmployeeDocuments";

            string[] possiblePaths = {
                document.FullPath,
                document.StoragePath,
                Path.Combine(AppDbContext.CentralStoragePath, subFolder, document.FileName),
                Path.Combine(Directory.GetCurrentDirectory(), subFolder, document.FileName)
            };

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        #region PDF Methods

        private async System.Threading.Tasks.Task LoadPdfDocumentAsync()
        {
            try
            {
                using (var document = PdfDocument.Open(_filePath))
                {
                    _pdfPageCount = document.NumberOfPages;

                    if (_pdfPageCount == 0)
                    {
                        ShowFallbackMessage("ãáİ PDF İÇÑÛ");
                        return;
                    }

                    // ÅÖÇİÉ ãÕäÚ ÕİÍÇÊ Skia (ÎØæÉ ãåãÉ)
                    document.AddSkiaPageFactory();

                    // ÅäÔÇÁ ãÌáÏ ãÄŞÊ ááÕæÑ
                    _tempPdfImagesFolder = Path.Combine(Path.GetTempPath(), "PDFPreview_" + Guid.NewGuid().ToString());
                    Directory.CreateDirectory(_tempPdfImagesFolder);

                    // ÊÍæíá ÇáÕİÍÉ ÇáÃæáì
                    await RenderPdfPageWithSkiaAsync(document, 1);

                    pdfHost.Visibility = Visibility.Visible;
                    imageViewer.Visibility = Visibility.Collapsed;
                    textViewer.Visibility = Visibility.Collapsed;
                    fallbackContainer.Visibility = Visibility.Collapsed;

                    pdfNavigationPanel.Visibility = _pdfPageCount > 1 ? Visibility.Visible : Visibility.Collapsed;
                    UpdatePdfPageInfo();
                }
            }
            catch (Exception ex)
            {
                ShowFallbackMessage($"ÎØÃ İí ÊÍãíá ãáİ PDF: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task RenderPdfPageWithSkiaAsync(PdfDocument document, int pageNumber)
        {
            try
            {
                loadingIndicator.Visibility = Visibility.Visible;

                var pageImagePath = Path.Combine(_tempPdfImagesFolder, $"page_{pageNumber}.png");

                if (!File.Exists(pageImagePath))
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        // ÇÓÊÎÏÇã PdfPig.Rendering.Skia ááÍÕæá Úáì ÇáÕİÍÉ ßÜ SKBitmap
                        using (var bitmap = document.GetPageAsSKBitmap(pageNumber, scale: 2.0f))
                        {
                            using (var image = SKImage.FromBitmap(bitmap))
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 90))
                            {
                                using (var stream = File.OpenWrite(pageImagePath))
                                {
                                    data.SaveTo(stream);
                                }
                            }
                        }
                    });
                }

                // ÊÍãíá ÇáÕæÑÉ Åáì æÇÌåÉ WPF
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(pageImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 1200;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    pdfImageDisplay.Source = bitmap;
                    _currentPdfPage = pageNumber;
                    UpdatePdfPageInfo();
                });
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ İí ÚÑÖ ÇáÕİÍÉ: {ex.Message}");
            }
            finally
            {
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void ChangePdfPage(int delta)
        {
            int newPage = _currentPdfPage + delta;
            if (newPage >= 1 && newPage <= _pdfPageCount)
            {
                using (var document = PdfDocument.Open(_filePath))
                {
                    document.AddSkiaPageFactory();
                    await RenderPdfPageWithSkiaAsync(document, newPage);
                }
            }
        }

        private async void GoToPdfPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= _pdfPageCount && pageNumber != _currentPdfPage)
            {
                using (var document = PdfDocument.Open(_filePath))
                {
                    document.AddSkiaPageFactory();
                    await RenderPdfPageWithSkiaAsync(document, pageNumber);
                }
            }
        }

        private void UpdatePdfPageInfo()
        {
            txtPageInfo.Text = $"ÕİÍÉ {_currentPdfPage} ãä {_pdfPageCount}";
        }

        #endregion

        #region Image Methods

        private void LoadImageDocument()
        {
            try
            {
                _imageDocument = new BitmapImage();
                _imageDocument.BeginInit();
                _imageDocument.UriSource = new Uri(_filePath);
                _imageDocument.CacheOption = BitmapCacheOption.OnLoad;
                _imageDocument.DecodePixelWidth = 1200;
                _imageDocument.EndInit();

                imageDisplay.Source = _imageDocument;

                pdfHost.Visibility = Visibility.Collapsed;
                imageViewer.Visibility = Visibility.Visible;
                textViewer.Visibility = Visibility.Collapsed;
                fallbackContainer.Visibility = Visibility.Collapsed;
                pdfNavigationPanel.Visibility = Visibility.Collapsed;

                txtPageInfo.Text = "ÕæÑÉ";
            }
            catch (Exception ex)
            {
                ShowFallbackMessage($"ÎØÃ İí ÊÍãíá ÇáÕæÑÉ: {ex.Message}");
            }
        }

        #endregion

        #region Text Methods

        private void LoadTextDocument()
        {
            try
            {
                _textContent = File.ReadAllText(_filePath);
                textContent.Text = _textContent;

                pdfHost.Visibility = Visibility.Collapsed;
                imageViewer.Visibility = Visibility.Collapsed;
                textViewer.Visibility = Visibility.Visible;
                fallbackContainer.Visibility = Visibility.Collapsed;
                pdfNavigationPanel.Visibility = Visibility.Collapsed;

                txtPageInfo.Text = $"ãÓÊäÏ äÕí - {_textContent.Length} ÍÑİ";
            }
            catch (Exception ex)
            {
                ShowFallbackMessage($"ÎØÃ İí ÊÍãíá Çáãáİ ÇáäÕí: {ex.Message}");
            }
        }

        #endregion

        #region Printing Methods

        private void printBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    string extension = GetFileExtension().ToLower();

                    if (extension == ".pdf")
                    {
                        PrintPdfDocument(printDialog);
                    }
                    else if (IsImageFile(extension))
                    {
                        PrintImageDocument(printDialog);
                    }
                    else if (extension == ".txt")
                    {
                        PrintTextDocument(printDialog);
                    }
                    else
                    {
                        PrintWithDefaultApp();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ İí ÇáØÈÇÚÉ: {ex.Message}", "ÎØÃ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintPdfDocument(PrintDialog printDialog)
        {
            try
            {
                using (var document = PdfDocument.Open(_filePath))
                {
                    document.AddSkiaPageFactory();

                    for (int i = 1; i <= document.NumberOfPages; i++)
                    {
                        // ÊÍæíá ÇáÕİÍÉ Åáì SKBitmap
                        using (var bitmap = document.GetPageAsSKBitmap(i, scale: 3.0f))
                        {
                            // ÊÍæíá SKBitmap Åáì BitmapSource
                            var info = new SKImageInfo(bitmap.Width, bitmap.Height);
                            var skImage = SKImage.FromBitmap(bitmap);
                            var bitmapSource = bitmap.ToWriteableBitmap();

                            var visual = new DrawingVisual();
                            using (var context = visual.RenderOpen())
                            {
                                context.DrawImage(bitmapSource, new Rect(0, 0,
                                    printDialog.PrintableAreaWidth,
                                    printDialog.PrintableAreaHeight));
                            }

                            printDialog.PrintVisual(visual, $"ÕİÍÉ {i}");
                        }
                    }
                }

                string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                LocalizationManager.ShowMessage($"Êã ÅÑÓÇá ÇáæËíŞÉ '{documentTitle}' ááØÈÇÚÉ", "äÌÇÍ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ İí ØÈÇÚÉ PDF: {ex.Message}", "ÎØÃ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintImageDocument(PrintDialog printDialog)
        {
            if (_imageDocument == null) return;

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawImage(_imageDocument, new Rect(0, 0,
                    printDialog.PrintableAreaWidth,
                    printDialog.PrintableAreaHeight));
            }

            string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
            printDialog.PrintVisual(visual, documentTitle);

            LocalizationManager.ShowMessage($"Êã ÅÑÓÇá ÇáæËíŞÉ '{documentTitle}' ááØÈÇÚÉ", "äÌÇÍ",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintTextDocument(PrintDialog printDialog)
        {
            var flowDocument = new FlowDocument(new Paragraph(new Run(_textContent)))
            {
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontSize = 12,
                PagePadding = new Thickness(50),
                FlowDirection = FlowDirection.RightToLeft
            };

            IDocumentPaginatorSource paginatorSource = flowDocument;
            string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
            printDialog.PrintDocument(paginatorSource.DocumentPaginator, documentTitle);

            LocalizationManager.ShowMessage($"Êã ÅÑÓÇá ÇáæËíŞÉ '{documentTitle}' ááØÈÇÚÉ", "äÌÇÍ",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintWithDefaultApp()
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = _filePath,
                    UseShellExecute = true,
                    Verb = "print"
                };

                Process.Start(info);

                LocalizationManager.ShowMessage("Êã İÊÍ Çáãáİ ÈÇáÈÑäÇãÌ ÇáÇİÊÑÇÖí ááØÈÇÚÉ",
                    "ãÚáæãÇÊ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"áÇ íãßä ØÈÇÚÉ åĞÇ ÇáäæÚ ãä ÇáãáİÇÊ ãÈÇÔÑÉ: {ex.Message}",
                    "ÊÍĞíÑ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Download Methods

        private void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                string documentTitle = _isEmployeeDocument ? _employeeDocument.Title : _companyDocument.Title;
                string fileType = GetFileExtension();

                var saveDialog = new SaveFileDialog
                {
                    FileName = documentTitle + fileType,
                    Filter = $"{fileType.ToUpper()} files (*{fileType})|*{fileType}|All files (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(_filePath, saveDialog.FileName, true);
                    LocalizationManager.ShowMessage($"Êã ÊÍãíá ÇáæËíŞÉ ÈäÌÇÍ", "äÌÇÍ",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÎØÃ İí ÇáÊÍãíá: {ex.Message}", "ÎØÃ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private void ShowFallbackMessage(string message)
        {
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Red,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20),
                FlowDirection = FlowDirection.RightToLeft
            };

            fallbackContainer.Child = textBlock;
            pdfHost.Visibility = Visibility.Collapsed;
            imageViewer.Visibility = Visibility.Collapsed;
            textViewer.Visibility = Visibility.Collapsed;
            fallbackContainer.Visibility = Visibility.Visible;
            pdfNavigationPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowErrorAndClose(string message)
        {
            LocalizationManager.ShowMessage(message, "ÎØÃ", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        private void CleanupResources()
        {
            try
            {
                // ÍĞİ ÇáãÌáÏ ÇáãÄŞÊ ááÕæÑ
                if (!string.IsNullOrEmpty(_tempPdfImagesFolder) && Directory.Exists(_tempPdfImagesFolder))
                {
                    Directory.Delete(_tempPdfImagesFolder, true);
                }
            }
            catch
            {
                // ÊÌÇåá ÃÎØÇÁ ÇáÍĞİ
            }
        }

        #endregion

        #region Event Handlers

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            CleanupResources();
            _context?.Dispose();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupResources();
            _context?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}
