// EmployeeArchiveWindow.xaml.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace HR_Application.Views
{
    public partial class EmployeeArchiveWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private List<DocumentCardModel> _documents;

        public EmployeeArchiveWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
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
                MessageBox.Show($"خطأ في تحميل بيانات الموظف: {ex.Message}", "خطأ",
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
                    Description = d.Description ?? "لا يوجد وصف",
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الأرشيف: {ex.Message}", "خطأ",
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
                    MessageBox.Show($"الملف غير موجود في المسار: {filePath}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض الوثيقة: {ex.Message}", "خطأ",
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
                    MessageBox.Show("الملف غير موجود", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معاينة الوثيقة: {ex.Message}", "خطأ",
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
                    MessageBox.Show("الملف غير موجود", "خطأ",
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
                    MessageBox.Show("تم تحميل الوثيقة بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الوثيقة: {ex.Message}", "خطأ",
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
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CardDelete_Click(object sender, RoutedEventArgs e)
        {
            var document = GetSelectedDocument(sender);
            if (document == null) return;

            var result = MessageBox.Show($"هل أنت متأكد من حذف الوثيقة '{document.Title}'؟",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

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

                    MessageBox.Show("تم حذف الوثيقة بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadArchiveDocuments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف الوثيقة: {ex.Message}", "خطأ",
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

        private string GetDocumentTypeName(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => "وثيقة موقعة",
                EmployeeDocumentType.CV => "السيرة الذاتية",
                EmployeeDocumentType.NationalID => "البطاقة الشخصية",
                EmployeeDocumentType.DrivingLicense => "رخصة القيادة",
                EmployeeDocumentType.DegreeCertificate => "شهادة المؤهل",
                EmployeeDocumentType.TrainingCertificate => "شهادات التدريب",
                EmployeeDocumentType.WorkPermit => "تصريح العمل",
                EmployeeDocumentType.Insurance => "التأمين",
                EmployeeDocumentType.MilitaryCertificate => "الشهادة العسكرية",
                EmployeeDocumentType.Passport => "الجواز",
                EmployeeDocumentType.PersonalContract => "العقد الشخصي",
                EmployeeDocumentType.Photo => "صورة شخصية",
                _ => "وثيقة أخرى"
            };
        }

        private string GetDocumentIcon(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => "📄",
                EmployeeDocumentType.CV => "📝",
                EmployeeDocumentType.NationalID => "🆔",
                EmployeeDocumentType.DrivingLicense => "🚗",
                EmployeeDocumentType.DegreeCertificate => "🎓",
                EmployeeDocumentType.TrainingCertificate => "📜",
                EmployeeDocumentType.WorkPermit => "💼",
                EmployeeDocumentType.Insurance => "🛡️",
                EmployeeDocumentType.MilitaryCertificate => "🎖️",
                EmployeeDocumentType.Passport => "🌍",
                EmployeeDocumentType.PersonalContract => "📃",
                EmployeeDocumentType.Photo => "📸",
                _ => "📎"
            };
        }

        private Brush GetDocumentTypeColor(EmployeeDocumentType type)
        {
            return type switch
            {
                EmployeeDocumentType.SignedCompanyDocument => new SolidColorBrush(Color.FromRgb(39, 174, 96)),
                EmployeeDocumentType.CV => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                EmployeeDocumentType.NationalID => new SolidColorBrush(Color.FromRgb(155, 89, 182)),
                EmployeeDocumentType.DrivingLicense => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                EmployeeDocumentType.DegreeCertificate => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                EmployeeDocumentType.TrainingCertificate => new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                EmployeeDocumentType.WorkPermit => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                EmployeeDocumentType.Insurance => new SolidColorBrush(Color.FromRgb(41, 128, 185)),
                EmployeeDocumentType.MilitaryCertificate => new SolidColorBrush(Color.FromRgb(142, 68, 173)),
                EmployeeDocumentType.Passport => new SolidColorBrush(Color.FromRgb(22, 160, 133)),
                EmployeeDocumentType.PersonalContract => new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                EmployeeDocumentType.Photo => new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
            };
        }

        private string GetStatusName(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Pending => "قيد الانتظار",
                DocumentStatus.Signed => "موقعة",
                DocumentStatus.Rejected => "مرفوضة",
                DocumentStatus.Expired => "منتهية",
                DocumentStatus.Active => "نشطة",
                DocumentStatus.Archived => "مؤرشفة",
                _ => "غير معروف"
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