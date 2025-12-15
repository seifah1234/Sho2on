using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using Button = System.Windows.Controls.Button;
using FlowDirection = System.Windows.FlowDirection;

namespace HR_Application.Views
{
    public partial class EmployeeDocumentsPrintWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private ObservableCollection<DocumentPrintItem> _documents;
        private int _employeeId;
        private string _employeeName;

        public EmployeeDocumentsPrintWindow(List<CompanyDocument> documents, int employeeId, string employeeName = null)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = employeeName;
            InitializeDocuments(documents);
            LoadEmployeeInfo();
        }

        private void InitializeDocuments(List<CompanyDocument> documents)
        {
            _documents = new ObservableCollection<DocumentPrintItem>(
                documents.Select(d => new DocumentPrintItem
                {
                    Document = d,
                    IsSelected = true,
                    CategoryName = GetCategoryName(d.Category),
                    FileSizeFormatted = FormatFileSize(d.FileSize)
                })
            );

            documentsGrid.ItemsSource = _documents;
        }

        private async void LoadEmployeeInfo()
        {
            try
            {
                if (string.IsNullOrEmpty(_employeeName))
                {
                    var employee = await _context.Users.FindAsync(_employeeId);
                    if (employee != null)
                    {
                        _employeeName = employee.FullName;
                    }
                }

                employeeInfoText.Text = $"الموظف: {_employeeName} - الكود: {_employeeId}";
            }
            catch (Exception ex)
            {
                employeeInfoText.Text = $"الموظف: {_employeeId}";
            }
        }

        private string GetCategoryName(DocumentCategory category)
        {
            return category switch
            {
                DocumentCategory.JobDescription => "وصف الوظيفة",
                DocumentCategory.CompanyPolicy => "سياسات الشركة",
                DocumentCategory.HRManual => "دليل الموارد البشرية",
                DocumentCategory.CodeOfConduct => "قواعد السلوك",
                DocumentCategory.SafetyProcedure => "إجراءات السلامة",
                DocumentCategory.Contract => "العقود",
                DocumentCategory.Other => "أخرى",
                _ => "أخرى"
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

        private void selectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _documents)
            {
                item.IsSelected = true;
            }
            documentsGrid.Items.Refresh();
        }

        private void deselectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _documents)
            {
                item.IsSelected = false;
            }
            documentsGrid.Items.Refresh();
        }

        private void PreviewDocument_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int documentId)
            {
                var document = _documents.FirstOrDefault(d => d.Document.Id == documentId);
                if (document != null)
                {
                    PreviewDocument(document.Document);
                }
            }
        }

        private void PreviewDocument(CompanyDocument document)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(),
                    "CompanyDocuments", document.FileName);

                if (File.Exists(filePath))
                {
                    if (document.FileType.ToLower() == ".pdf")
                    {
                        // For PDF preview, you might need a PDF viewer control
                        // This is a simplified version - you might want to use a proper PDF viewer
                        System.Diagnostics.Process.Start(filePath);
                    }
                    else
                    {
                        // For other file types, open with default application
                        System.Diagnostics.Process.Start(filePath);
                    }
                }
                else
                {
                    MessageBox.Show("الملف غير موجود", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معاينة الملف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void downloadSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedDocuments = _documents.Where(d => d.IsSelected).ToList();
            if (!selectedDocuments.Any())
            {
                MessageBox.Show("يرجى اختيار مستند واحد على الأقل", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "اختر مجلد لحفظ المستندات"
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    int downloadedCount = 0;
                    foreach (var docItem in selectedDocuments)
                    {
                        var sourcePath = Path.Combine(Directory.GetCurrentDirectory(),
                            "CompanyDocuments", docItem.Document.FileName);

                        if (File.Exists(sourcePath))
                        {
                            var destinationPath = Path.Combine(folderDialog.SelectedPath,
                                $"{docItem.Document.Title}{docItem.Document.FileType}");

                            File.Copy(sourcePath, destinationPath, true);
                            downloadedCount++;
                        }
                    }

                    MessageBox.Show($"تم تحميل {downloadedCount} من {selectedDocuments.Count} ملف",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الملفات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedDocuments = _documents.Where(d => d.IsSelected).ToList();
            if (!selectedDocuments.Any())
            {
                MessageBox.Show("يرجى اختيار مستند واحد على الأقل", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var printPreviewWindow = new PrintPreviewWindow(selectedDocuments.Select(d => d.Document).ToList(),
                _employeeId, _employeeName, includeHeaderCheck.IsChecked == true,
                includeFooterCheck.IsChecked == true, includeSignatureLineCheck.IsChecked == true);

            printPreviewWindow.ShowDialog();
        }

        private void printBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedDocuments = _documents.Where(d => d.IsSelected).ToList();
            if (!selectedDocuments.Any())
            {
                MessageBox.Show("يرجى اختيار مستند واحد على الأقل", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    foreach (var docItem in selectedDocuments)
                    {
                        var document = CreatePrintDocument(docItem.Document);
                        printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,
                            docItem.Document.Title);
                    }

                    MessageBox.Show($"تم طباعة {selectedDocuments.Count} مستند بنجاح",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument(CompanyDocument document)
        {
            var flowDocument = new FlowDocument
            {
                PagePadding = new Thickness(50),
                ColumnWidth = 500,
                FlowDirection = FlowDirection.RightToLeft
            };

            // Header
            if (includeHeaderCheck.IsChecked == true)
            {
                var headerParagraph = new Paragraph
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                headerParagraph.Inlines.Add(new Run("شركة: [اسم الشركة]"));
                headerParagraph.Inlines.Add(new LineBreak());
                headerParagraph.Inlines.Add(new Run("مستندات الموظف للتوقيع"));
                flowDocument.Blocks.Add(headerParagraph);
            }

            // Employee Info
            var employeeParagraph = new Paragraph
            {
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };
            employeeParagraph.Inlines.Add(new Run($"اسم الموظف: {_employeeName}"));
            employeeParagraph.Inlines.Add(new LineBreak());
            employeeParagraph.Inlines.Add(new Run($"كود الموظف: {_employeeId}"));
            employeeParagraph.Inlines.Add(new LineBreak());
            employeeParagraph.Inlines.Add(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd}"));
            flowDocument.Blocks.Add(employeeParagraph);

            // Document Title
            var titleParagraph = new Paragraph
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };
            titleParagraph.Inlines.Add(new Run(document.Title));
            flowDocument.Blocks.Add(titleParagraph);

            // Document Description
            if (!string.IsNullOrEmpty(document.Description))
            {
                var descParagraph = new Paragraph
                {
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                descParagraph.Inlines.Add(new Run(document.Description));
                flowDocument.Blocks.Add(descParagraph);
            }

            // Signature Area
            if (includeSignatureLineCheck.IsChecked == true)
            {
                var signatureParagraph = new Paragraph
                {
                    FontSize = 12,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                signatureParagraph.Inlines.Add(new Run("توقيع الموظف: ________________________"));
                signatureParagraph.Inlines.Add(new LineBreak());
                signatureParagraph.Inlines.Add(new Run("التاريخ: ________/________/________"));
                flowDocument.Blocks.Add(signatureParagraph);
            }

            // Footer
            if (includeFooterCheck.IsChecked == true)
            {
                var footerParagraph = new Paragraph
                {
                    FontSize = 10,
                    FontStyle = FontStyles.Italic,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 0)
                };
                footerParagraph.Inlines.Add(new Run("هذا المستند جزء من ملف الموظف الرسمي"));
                flowDocument.Blocks.Add(footerParagraph);
            }

            return flowDocument;
        }

        private void uploadSignedBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedDocuments = _documents.Where(d => d.IsSelected).ToList();
            if (!selectedDocuments.Any())
            {
                MessageBox.Show("يرجى اختيار مستند واحد على الأقل", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }

    public class DocumentPrintItem
    {
        public CompanyDocument Document { get; set; }
        public bool IsSelected { get; set; }
        public string CategoryName { get; set; }
        public string FileSizeFormatted { get; set; }
    }
}