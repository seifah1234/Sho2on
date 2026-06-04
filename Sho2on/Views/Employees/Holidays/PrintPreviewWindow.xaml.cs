using System; using HR_Application.Helpers;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly FlowDocument _flowDocument;
        private readonly string _title;

        public PrintPreviewWindow(FlowDocument document, string? title = null)
        {
            InitializeComponent();
            _flowDocument = document;
            _title = title ?? LocalizationManager.Translate("معاينة الطباعة");

            Loaded += PrintPreviewWindow_Loaded;
        }

        private async void PrintPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // عرض مؤشر الانتظار
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                await System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    LoadDocumentForPreview();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل المعاينة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void LoadDocumentForPreview()
        {
            try
            {
                // إنشاء مستند XPS مؤقت في الذاكرة
                string tempFilePath = Path.GetTempFileName();
                tempFilePath = Path.ChangeExtension(tempFilePath, ".xps");

                // إنشاء ملف XPS مؤقت
                using (var xpsDoc = new XpsDocument(tempFilePath, FileAccess.ReadWrite))
                {
                    var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

                    // استخدام DocumentPaginator من FlowDocument
                    var paginator = ((IDocumentPaginatorSource)_flowDocument).DocumentPaginator;
                    paginator.PageSize = new System.Windows.Size(
                        _flowDocument.PageWidth,
                        _flowDocument.PageHeight);

                    writer.Write(paginator);

                    // تحميل المستند في DocumentViewer
                    DocumentViewer.Document = xpsDoc.GetFixedDocumentSequence();
                }

                // تنظيف الملف المؤقت عند إغلاق النافذة
                this.Closed += (s, e) =>
                {
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                };
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل المعاينة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                printDialog.PageRangeSelection = PageRangeSelection.AllPages;
                printDialog.UserPageRangeEnabled = true;

                // إعدادات الصفحة لتتناسب مع FlowDocument
                printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(
                    PageMediaSizeName.ISOA4);

                if (printDialog.ShowDialog() == true)
                {
                    // استخدام DocumentPaginator من FlowDocument الأصلي
                    var paginator = ((IDocumentPaginatorSource)_flowDocument).DocumentPaginator;
                    paginator.PageSize = new System.Windows.Size(
                        _flowDocument.PageWidth,
                        _flowDocument.PageHeight);

                    printDialog.PrintDocument(paginator, _title);

                    LocalizationManager.ShowMessage("تمت الطباعة بنجاح", LocalizationManager.Translate("طباعة"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في الطباعة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
