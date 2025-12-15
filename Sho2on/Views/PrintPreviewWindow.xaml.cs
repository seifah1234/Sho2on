using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using FlowDirection = System.Windows.FlowDirection;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace HR_Application.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private List<CompanyDocument> _documents;
        private int _employeeId;
        private string _employeeName;
        private bool _includeHeader;
        private bool _includeFooter;
        private bool _includeSignatureLine;

        public PrintPreviewWindow(List<CompanyDocument> documents, int employeeId, string employeeName,
                                bool includeHeader, bool includeFooter, bool includeSignatureLine)
        {
            InitializeComponent();
            _documents = documents;
            _employeeId = employeeId;
            _employeeName = employeeName;
            _includeHeader = includeHeader;
            _includeFooter = includeFooter;
            _includeSignatureLine = includeSignatureLine;

            LoadPreview();
        }

        private void LoadPreview()
        {
            if (_documents.Count > 0)
            {
                var flowDocument = CreatePrintDocument(_documents[0]);

                // Set page size and enable pagination
                flowDocument.PageHeight = 1122; // A4 height in points
                flowDocument.PageWidth = 793;   // A4 width in points
                flowDocument.PagePadding = new Thickness(50);

                // Get the paginator
                var paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
                paginator.PageSize = new System.Windows.Size(flowDocument.PageWidth, flowDocument.PageHeight);

                // Create fixed document from paginator
                var fixedDocument = CreateFixedDocumentFromPaginator(paginator);
                documentViewer.Document = fixedDocument;
            }
        }

        private FixedDocument CreateFixedDocumentFromPaginator(DocumentPaginator paginator)
        {
            var fixedDocument = new FixedDocument();

            for (int i = 0; i < paginator.PageCount; i++)
            {
                var page = paginator.GetPage(i);

                var fixedPage = new FixedPage
                {
                    Width = page.Size.Width,
                    Height = page.Size.Height
                };

                var container = new Canvas();
                container.Children.Add((UIElement)page.Visual);

                fixedPage.Children.Add(container);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDocument.Pages.Add(pageContent);
            }

            return fixedDocument;
        }

        private FlowDocument CreatePrintDocument(CompanyDocument document)
        {
            // Same implementation as in the main window
            var flowDocument = new FlowDocument
            {
                PagePadding = new Thickness(50),
                ColumnWidth = 500,
                FlowDirection = FlowDirection.RightToLeft
            };

            // Add content similar to the CreatePrintDocument method in main window
            // ... (same content as above)

            return flowDocument;
        }

        private void printBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    foreach (var document in _documents)
                    {
                        var printDocument = CreatePrintDocument(document);
                        printDialog.PrintDocument(((IDocumentPaginatorSource)printDocument).DocumentPaginator,
                            document.Title);
                    }

                    MessageBox.Show($"تم طباعة {_documents.Count} مستند بنجاح",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}