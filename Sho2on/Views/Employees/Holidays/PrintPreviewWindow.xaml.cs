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

        public PrintPreviewWindow(FlowDocument document, string title = "„⁄«Ì‰… «·ÿ»«⁄…")
        {
            InitializeComponent();
            _flowDocument = document;
            _title = title;

            Loaded += PrintPreviewWindow_Loaded;
        }

        private async void PrintPreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ⁄—÷ „ƒ‘— «·«‰ Ÿ«—
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                await System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    LoadDocumentForPreview();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„⁄«Ì‰…: {ex.Message}", "Œÿ√",
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
                // ≈‰‘«¡ „” ‰œ XPS „ƒﬁ  ›Ì «·–«ﬂ—…
                string tempFilePath = Path.GetTempFileName();
                tempFilePath = Path.ChangeExtension(tempFilePath, ".xps");

                // ≈‰‘«¡ „·› XPS „ƒﬁ 
                using (var xpsDoc = new XpsDocument(tempFilePath, FileAccess.ReadWrite))
                {
                    var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

                    // «” Œœ«„ DocumentPaginator „‰ FlowDocument
                    var paginator = ((IDocumentPaginatorSource)_flowDocument).DocumentPaginator;
                    paginator.PageSize = new System.Windows.Size(
                        _flowDocument.PageWidth,
                        _flowDocument.PageHeight);

                    writer.Write(paginator);

                    //  Õ„Ì· «·„” ‰œ ›Ì DocumentViewer
                    DocumentViewer.Document = xpsDoc.GetFixedDocumentSequence();
                }

                //  ‰ŸÌ› «·„·› «·„ƒﬁ  ⁄‰œ ≈€·«ﬁ «·‰«›–…
                this.Closed += (s, e) =>
                {
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch { /*  Ã«Â· √Œÿ«¡ «·Õ–› */ }
                };
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·„⁄«Ì‰…: {ex.Message}", "Œÿ√",
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

                // ≈⁄œ«œ«  «·’›Õ… ·  ‰«”» „⁄ FlowDocument
                printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(
                    PageMediaSizeName.ISOA4);

                if (printDialog.ShowDialog() == true)
                {
                    // «” Œœ«„ DocumentPaginator „‰ FlowDocument «·√’·Ì
                    var paginator = ((IDocumentPaginatorSource)_flowDocument).DocumentPaginator;
                    paginator.PageSize = new System.Windows.Size(
                        _flowDocument.PageWidth,
                        _flowDocument.PageHeight);

                    printDialog.PrintDocument(paginator, _title);

                    LocalizationManager.ShowMessage(" „  «·ÿ»«⁄… »‰Ã«Õ", "ÿ»«⁄…",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·ÿ»«⁄…: {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
