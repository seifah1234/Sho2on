// EmployeeArchiveWindow.xaml.cs
using DocumentFormat.OpenXml.Wordprocessing;
using MahApps.Metro.IconPacks;
using HR_Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using static HR_Application.Views.EmployeeArchiveWindow;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using Border = System.Windows.Controls.Border;

namespace HR_Application.Views
{
    public partial class EmployeeArchiveWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private List<DocumentCardModel> _documents = new List<DocumentCardModel>();

        public class FolderItem
        {
            /// <summary>Display name shown under the folder icon.</summary>
            public string FolderName { get; set; }

            /// <summary>SolidColorBrush used for folder body + accent strip.</summary>
            public SolidColorBrush FolderColor { get; set; }

            /// <summary>PackIconMaterialKind drawn inside the folder body.</summary>
            public PackIconMaterialKind FolderIconKind { get; set; }

            /// <summary>Total documents in this category.</summary>
            public int DocumentCount { get; set; }

            /// <summary>
            /// The DocumentType integer that matches your existing data model.
            /// Used in FolderCard_Click to filter documentsItemsControl.
            /// </summary>
            public int TypeId { get; set; }
        }

        public EmployeeArchiveWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
        }

        // ?? Navigation state ??????????????????????????????????????????
        private int _currentFolderTypeId = -1;

        // ?? Build folder list from your existing document collection ??
        private void LoadFolders()
        {
            // Adjust this list to match your actual DocumentType enum/ids.
            // FolderColor uses the same named brushes already in XAML Resources.
            var folders = new System.Collections.Generic.List<FolderItem>
            {
                new FolderItem
                {
                    TypeId        = 1,
                    FolderName    = LocalizationManager.Translate("وثائق موقعة"),
                    FolderColor   = (SolidColorBrush)FindResource("SignedColor"),
                    FolderIconKind= PackIconMaterialKind.FileSign,
                    DocumentCount = CountDocsOfType(1)
                },
                new FolderItem
                {
                    TypeId        = 7,
                    FolderName    = LocalizationManager.Translate("وثاق العمل"),
                    FolderColor   = (SolidColorBrush)FindResource("CVColor"),
                    FolderIconKind= PackIconMaterialKind.FileAccount,
                    DocumentCount = CountDocsOfType(7)
                },
                new FolderItem
                {
                    TypeId        = 6,
                    FolderName    = LocalizationManager.Translate("وثائق التدريب"),
                    FolderColor   = (SolidColorBrush)FindResource("CertificateColor"),
                    FolderIconKind= PackIconMaterialKind.Certificate,
                    DocumentCount = CountDocsOfType(6)
                },
                new FolderItem
                {
                    TypeId        = 99,
                    FolderName    = LocalizationManager.Translate("أخرى"),
                    FolderColor   = (SolidColorBrush)FindResource("DefaultColor"),
                    FolderIconKind= PackIconMaterialKind.FolderMultiple,
                    DocumentCount = CountDocsOfType(99)
                },
            };

            // Update total badge
            int total = 0;
            foreach (var f in folders) total += f.DocumentCount;
            totalDocsCount.Text = $"{total} وثيقة";

            foldersItemsControl.ItemsSource = folders;
        }


        /// <summary>Count documents matching a type id from your data source.</summary>
        private int CountDocsOfType(int typeId)
        {
            // Replace with your actual data access:
             return _documents.Count(d => d.DocumentTypeId == typeId);
        }

        // ?? Folder card clicked ? drill into folder ???????????????????
        private void FolderCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is FolderItem folder)
            {
                _currentFolderTypeId = folder.TypeId;

                // Update breadcrumb
                folderCrumbText.Text = folder.FolderName;
                folderCrumbPanel.Visibility = Visibility.Visible;
                backToFoldersBtn.Visibility = Visibility.Visible;

                // Load filtered documents
                LoadDocumentsForFolder(folder.TypeId);

                // Switch views
                foldersItemsControl.Visibility = Visibility.Collapsed;
                documentsScrollViewer.Visibility = Visibility.Visible;

                statusText.Text = $"عرض وثائق: {folder.FolderName}";
            }
        }

        /// <summary>Populate documentsItemsControl with docs of a given type.</summary>
        private void LoadDocumentsForFolder(int typeId)
        {
            // Replace with your actual data access:
            var filtered = _documents.Where(d => ((int)d.DocumentType) == typeId).ToList();
            documentsItemsControl.ItemsSource = filtered;
        }

        // ?? Back button ? return to folder grid ???????????????????????
        private void backToFoldersBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRoot();
        }

        // ?? Root breadcrumb clicked ????????????????????????????????????
        private void rootCrumbLink_Click(object sender, RoutedEventArgs e)
        {
            NavigateToRoot();
        }

        private void NavigateToRoot()
        {
            _currentFolderTypeId = -1;

            folderCrumbPanel.Visibility = Visibility.Collapsed;
            backToFoldersBtn.Visibility = Visibility.Collapsed;

            documentsScrollViewer.Visibility = Visibility.Collapsed;
            foldersItemsControl.Visibility = Visibility.Visible;

            statusText.Text = LocalizationManager.Translate("جاهز");
        }

        // ?? Call LoadFolders() from your existing Window_Loaded or constructor ??
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            LoadEmployeeInfo();
            LoadArchiveDocuments();
        }

        private void LoadEmployeeInfo()
        {
            try
            {
                var employee = _context.Users
                    .FirstOrDefault(e => e.Id == _employeeId);

                if (employee != null)
                {
                    txtEmployeeName.Text = employee.FullName;
                    txtEmployeeCode.Text = $"كود: {employee.Id}";
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الموظف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadArchiveDocuments()
        {
            try
            {
                var documents = await _context.EmployeeDocuments
                    .Include(ed => ed.Document)
                    .Include(ed => ed.Employee)
                    .Include(ed => ed.Uploader)
                    .Where(ed => ed.EmployeeId == _employeeId && ed.IsActive)
                    .OrderByDescending(ed => ed.UploadDate)
                    .ToListAsync();

                _documents = documents.Select(d => new DocumentCardModel
                {
                    Id = d.Id,
                    Title = d.Title,
                    Description = d.Description ?? LocalizationManager.Translate("لا يوجد وصف"),
                    UploadDate = d.UploadDate,
                    DocumentType = d.DocumentType,
                    Status = d.Status,
                    FileName = d.FileName,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    StoragePath = d.StoragePath,
                    FullPath = d.FullPath
                }).ToList();

                documentsItemsControl.ItemsSource = _documents;
                statusText.Text = $"تم تحميل {_documents.Count} وثيقة من المسار المركزي";
                LoadFolders();

            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الأرشيف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DocumentCardModel GetSelectedDocument(object sender)
        {
            var button = sender as Button;
            return button?.Tag as DocumentCardModel;
        }

        // في دوال CardView_Click و CardPreview_Click في EmployeeArchiveWindow
        private void CardView_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            try
            {
                string filePath = GetDocumentFilePath(document);
                if (File.Exists(filePath))
                {
                    var previewWindow = new DocumentPreviewWindow(document.Id, true);
                    previewWindow.Owner = this;
                    previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    previewWindow.ShowDialog();
                }
                else
                {
                    LocalizationManager.ShowMessage($"الملف غير موجود في المسار: {filePath}", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في عرض الوثيقة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CardPreview_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            try
            {
                string filePath = GetDocumentFilePath(document);

                if (File.Exists(filePath))
                {
                    // فتح نافذة المعاينة مع تحديد أن هذه وثيقة موظف
                    var previewWindow = new DocumentPreviewWindow(document.Id, true); // true تعني employee document
                    previewWindow.Owner = this;
                    previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    previewWindow.ShowDialog();
                }
                else
                {
                    LocalizationManager.ShowMessage("الملف غير موجود", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في معاينة الوثيقة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CardDownload_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            try
            {
                var sourcePath = GetDocumentFilePath(document);

                if (!File.Exists(sourcePath))
                {
                    LocalizationManager.ShowMessage("الملف غير موجود", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    FileName = document.Title + document.FileType,
                    Filter = $"جميع الملفات (*.*)|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(sourcePath, saveDialog.FileName, true);
                    LocalizationManager.ShowMessage("تم تحميل الوثيقة بنجاح", LocalizationManager.Translate("نجاح"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الوثيقة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CardPrint_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            try
            {
                // فتح نافذة المعاينة واستخدام زر الطباعة الموجود فيها
                var previewWindow = new DocumentPreviewWindow(document.Id, true);
                previewWindow.Owner = this;
                previewWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في الطباعة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CardDelete_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            var result = LocalizationManager.ShowMessage($"هل أنت متأكد من حذف الوثيقة '{document.Title}'؟",
                LocalizationManager.Translate("تأكيد الحذف"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // حذف الملف الفعلي
                    var filePath = GetDocumentFilePath(document);
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    // حذف من قاعدة البيانات
                    var dbDocument = await _context.EmployeeDocuments
                        .FirstOrDefaultAsync(ed => ed.Id == document.Id);

                    if (dbDocument != null)
                    {
                        _context.EmployeeDocuments.Remove(dbDocument);
                        await _context.SaveChangesAsync();
                    }

                    LocalizationManager.ShowMessage("تم حذف الوثيقة بنجاح", LocalizationManager.Translate("نجاح"),
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadArchiveDocuments();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ في حذف الوثيقة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetDocumentFilePath(DocumentCardModel document)
        {
            // أولوية: المسار الكامل المباشر
            if (!string.IsNullOrEmpty(document.FullPath) && File.Exists(document.FullPath))
                return document.FullPath;

            // ثانوية: مسار التخزين
            if (!string.IsNullOrEmpty(document.StoragePath) && File.Exists(document.StoragePath))
                return document.StoragePath;

            // افتراضي: المسار المركزي
            string subFolder = document.DocumentType == EmployeeDocumentType.SignedCompanyDocument ?
                "SignedDocuments" : "EmployeeDocuments";

            string centralPath = Path.Combine(AppDbContext.CentralStoragePath, subFolder, document.FileName);

            // إذا لم يكن موجوداً في المسار المركزي، جرب المسار المحلي
            if (File.Exists(centralPath))
                return centralPath;

            // المحاولة الأخيرة: المسار المحلي القديم
            string localPath = Path.Combine(Directory.GetCurrentDirectory(), subFolder, document.FileName);
            return localPath;
        }

        private void addPersonalDocBtn_Click(object sender, RoutedEventArgs e)
        {
            var addDocWindow = new AddEmployeeDocumentWindow(_employeeId);
            addDocWindow.Closed += (s, args) => LoadArchiveDocuments();
            addDocWindow.ShowDialog();
        }

        private void showSigningModalBtn_Click(object sender, RoutedEventArgs e)
        {
            var signingModal = new DocumentSigningModal(_employeeId);
            signingModal.Closed += (s, args) => LoadArchiveDocuments();
            signingModal.Owner = this;
            signingModal.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            signingModal.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }

    public class DocumentCardModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime UploadDate { get; set; }
        public EmployeeDocumentType DocumentType { get; set; }
        public DocumentStatus Status { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string StoragePath { get; set; }
        public string FullPath { get; set; }
        public long FileSize { get; set; }

        // الخصائص المحسوبة للعرض
        public string DocumentTypeName => GetDocumentTypeName(DocumentType);
        public int DocumentTypeId => GetDocumentTypeInt(DocumentType);
        public string DocumentIcon => GetDocumentIcon(DocumentType);
        public Brush DocumentTypeColor => GetDocumentTypeColor(DocumentType);
        public string StatusName => GetStatusName(Status);
        public Brush StatusColor => GetStatusColor(Status);
        public string FormattedFileSize => FormatFileSize(FileSize);
        public string FormattedUploadDate => UploadDate.ToString("yyyy/MM/dd HH:mm");

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


        private int GetDocumentTypeInt(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => 1,
                EmployeeDocumentType.TrainingCertificate => 6,
                EmployeeDocumentType.WorkPermit => 7,
                _ => 99
            };
        }

        private string GetDocumentTypeName(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => LocalizationManager.Translate("وثائق موقعه"),
                EmployeeDocumentType.TrainingCertificate => LocalizationManager.Translate("وثائق التدريب"),
                EmployeeDocumentType.WorkPermit => LocalizationManager.Translate("وثائق التعيين"),
                _ => LocalizationManager.Translate("وثيقة أخرى")
            };
        }

        private string GetDocumentIcon(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => "??",
                EmployeeDocumentType.TrainingCertificate => "??",
                EmployeeDocumentType.WorkPermit => "??",
                _ => "??"
            };
        }
        

        private Brush GetDocumentTypeColor(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                EmployeeDocumentType.TrainingCertificate => new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                EmployeeDocumentType.WorkPermit => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
            };
        }

        private string GetStatusName(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Pending => LocalizationManager.Translate("قيد الانتظار"),
                DocumentStatus.Signed => LocalizationManager.Translate("موقعة"),
                DocumentStatus.Rejected => LocalizationManager.Translate("مرفوضة"),
                DocumentStatus.Expired => LocalizationManager.Translate("منتهية"),
                DocumentStatus.Active => LocalizationManager.Translate("نشطة"),
                DocumentStatus.Archived => LocalizationManager.Translate("مؤرشفة"),
                _ => LocalizationManager.Translate("غير معروف")
            };
        }

        private Brush GetStatusColor(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Pending => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                DocumentStatus.Signed => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                DocumentStatus.Rejected => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                DocumentStatus.Expired => new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                DocumentStatus.Active => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                DocumentStatus.Archived => new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
            };
        }
    }
}
