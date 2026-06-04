using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HR_Application.Views
{
    // ?????????????????????????????????????????????
    // ViewModel: ملف واحد داخل الـ Folder
    // ?????????????????????????????????????????????
    public class DocumentViewModel : INotifyPropertyChanged
    {
        private CompanyDocument _doc;

        public DocumentViewModel(CompanyDocument doc)
        {
            _doc = doc;
        }

        // تفويض جميع خصائص الـ CompanyDocument الأصلية
        public int Id => _doc.Id;
        public string Title => _doc.Title;
        public string FileName => _doc.FileName;
        public string FilePath => _doc.FilePath;
        public string FullPath => _doc.FullPath;
        public string FileType => _doc.FileType;
        public long FileSize => _doc.FileSize;
        public bool IsRequired => _doc.IsRequired;
        public bool IsActive => _doc.IsActive;
        public DateTime UploadDate => _doc.UploadDate;
        public JobTitle JobTitle => _doc.JobTitle;
        public bool HasJobTitle => _doc.JobTitle != null;

        // أيقونة الملف حسب الامتداد
        public string FileIcon => _doc.FileType?.ToLower() switch
        {
            ".pdf" => "??",
            ".docx" => "??",
            ".doc" => "??",
            ".xlsx" => "??",
            ".xls" => "??",
            ".pptx" => "??",
            ".txt" => "??",
            ".png" or ".jpg" or ".jpeg" => "??",
            _ => "??"
        };

        public string ActiveText => _doc.IsActive ? LocalizationManager.Translate("نشط") : LocalizationManager.Translate("معطّل");
        public string ActiveColor => _doc.IsActive ? "#27ae60" : "#95a5a6";

        // الكائن الأصلي للعمليات (حذف، تحميل…)
        public CompanyDocument Original => _doc;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // ?????????????????????????????????????????????
    // ViewModel: Folder = تصنيف + قائمة ملفاته
    // ?????????????????????????????????????????????
    public class FolderViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string CategoryName { get; set; }
        public List<DocumentViewModel> Documents { get; set; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                OnPropertyChanged(nameof(ArrowText));
            }
        }

        public string CountText => $"{Documents.Count} ملف";
        public string ArrowText => _isExpanded ? "?" : "?";

        public void Toggle() => IsExpanded = !IsExpanded;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ?????????????????????????????????????????????
    // Code-Behind
    // ?????????????????????????????????????????????
    public partial class CompanyDocumentsWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private string _selectedFilePath;
        private CompanyDocument _selectedDocument;

        // قائمة الـ Folders المعروضة حالياً
        private List<FolderViewModel> _folders = new();

        public CompanyDocumentsWindow()
        {
            InitializeComponent();


            CheckStorageAccessibility();
        }

        // ??????????????????????????????????????????
        // تحميل البيانات
        // ??????????????????????????????????????????

        private async Task LoadDocuments()
        {
            try
            {
                var query = _context.CompanyDocuments
                    .Include(d => d.Uploader)
                    .Include(d => d.JobTitle)
                    .AsQueryable();

                // فلتر التصنيف
                if (categoryFilter.SelectedValue != null)
                {
                    var selectedCategory = (int)categoryFilter.SelectedValue;
                    query = query.Where(d => d.Category == (DocumentCategory)selectedCategory);
                }

                // فلتر الوظيفة
                if (jobTitleFilterComboBox.SelectedValue != null)
                {
                    var selectedJobTitleId = (int)jobTitleFilterComboBox.SelectedValue;
                    query = query.Where(d => d.JobTitleId == selectedJobTitleId);
                }

                // فلتر النشط
                if (activeOnlyCheck.IsChecked == true)
                    query = query.Where(d => d.IsActive);

                var documents = await query
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                // ???? تجميع الملفات داخل Folders ????
                _folders = documents
                    .GroupBy(d => d.Category)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        // هل كان الـ Folder مفتوحاً قبل إعادة التحميل؟
                        var prev = _folders.FirstOrDefault(f => f.CategoryName == GetCategoryName(g.Key));
                        return new FolderViewModel
                        {
                            CategoryName = GetCategoryName(g.Key),
                            IsExpanded = prev?.IsExpanded ?? true,
                            Documents = g.Select(d => new DocumentViewModel(d)).ToList()
                        };
                    })
                    .ToList();

                foldersPanel.ItemsSource = _folders;
                statusText.Text = $"تم تحميل {documents.Count} ملف في {_folders.Count} تصنيف";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الملفات: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ??????????????????????????????????????????
        // أحداث الـ Folders
        // ??????????????????????????????????????????

        /// <summary>النقر على رأس الـ Folder ? فتح/إغلاق</summary>
        private void FolderHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is FolderViewModel folder)
            {
                folder.Toggle();
            }
        }

        /// <summary>النقر على صف ملف ? اختياره كـ SelectedDocument</summary>
        private void FileRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is DocumentViewModel vm)
            {
                _selectedDocument = vm.Original;
                statusText.Text = $"تم اختيار: {_selectedDocument.Title}";

                // إزالة الـ highlight من كل الصفوف
                foreach (var folder in _folders)
                    foreach (var doc in folder.Documents)
                        border.Background = System.Windows.Media.Brushes.Transparent;

                // تلوين الصف المحدد
                border.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString("#2000ADEF"));
            }
        }

        // ??????????????????????????????????????????
        // باقي الكود (بدون تغيير جوهري)
        // ??????????????????????????????????????????

        private void CheckStorageAccessibility()
        {
            try
            {
                string storagePath = AppDbContext.CentralStoragePath;
                bool isNetworkPath = storagePath.StartsWith(@"\\");

                if (isNetworkPath)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    bool canAccess = Directory.Exists(storagePath);
                    stopwatch.Stop();

                    if (canAccess)
                    {
                        statusText.Text = $"المسار المركزي: {storagePath} (زمن الوصول: {stopwatch.ElapsedMilliseconds}ms)";
                        if (stopwatch.ElapsedMilliseconds > 1000)
                            statusText.Text += LocalizationManager.Translate(" - اتصال بطيء");
                    }
                    else
                    {
                        statusText.Text = LocalizationManager.Translate("غير متصل بالمسار المركزي - استخدام المسار المحلي");
                    }
                }
                else
                {
                    statusText.Text = $"المسار المحلي: {storagePath}";
                }
            }
            catch (Exception ex)
            {
                statusText.Text = $"خطأ في الوصول للمسار: {ex.Message}";
            }
        }

        private async Task LoadCategories()
        {
            categoryBox.ItemsSource = Enum.GetValues(typeof(DocumentCategory))
                .Cast<DocumentCategory>()
                .Select(c => new { Value = (int)c, Name = GetCategoryName(c) })
                .ToList();

            categoryBox.DisplayMemberPath = "Name";
            categoryBox.SelectedValuePath = "Value";

            categoryFilter.ItemsSource = categoryBox.ItemsSource;
            categoryFilter.DisplayMemberPath = "Name";
            categoryFilter.SelectedValuePath = "Value";

            await LoadJobTitles();
        }

        private async Task LoadJobTitles()
        {
            try
            {
                var jobTitles = await _context.JobTitles
                    .OrderBy(j => j.Name)
                    .ToListAsync();

                jobTitleComboBox.ItemsSource = jobTitles;
                jobTitleComboBox.DisplayMemberPath = "Name";
                jobTitleComboBox.SelectedValuePath = "Id";

                jobTitleFilterComboBox.ItemsSource = jobTitles;
                jobTitleFilterComboBox.DisplayMemberPath = "Name";
                jobTitleFilterComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الوظائف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCategoryName(DocumentCategory category) => category switch
        {
            DocumentCategory.JobDescription => LocalizationManager.Translate("وصف الوظيفة"),
            DocumentCategory.CompanyPolicy => LocalizationManager.Translate("سياسات الشركة"),
            DocumentCategory.HRManual => LocalizationManager.Translate("دليل الموارد البشرية"),
            DocumentCategory.CodeOfConduct => LocalizationManager.Translate("قواعد السلوك"),
            DocumentCategory.SafetyProcedure => LocalizationManager.Translate("إجراءات السلامة"),
            DocumentCategory.Contract => LocalizationManager.Translate("العقود"),
            DocumentCategory.Other => LocalizationManager.Translate("أخرى"),
            _ => LocalizationManager.Translate("أخرى")
        };

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = LocalizationManager.Translate("جميع الملفات (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt"),
                FilterIndex = 2,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(_selectedFilePath);
                selectedFileText.Text = $"{fileInfo.Name} ({FormatFileSize(fileInfo.Length)})";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void uploadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(titleBox.Text) || categoryBox.SelectedValue == null ||
                string.IsNullOrEmpty(_selectedFilePath))
            {
                LocalizationManager.ShowMessage("يرجى ملء جميع الحقول واختيار ملف", LocalizationManager.Translate("تحذير"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_selectedFilePath);
                string storagePath = AppDbContext.CentralStoragePath;
                string companyDocumentsPath = Path.Combine(storagePath, "CompanyDocuments");

                if (!Directory.Exists(companyDocumentsPath))
                    Directory.CreateDirectory(companyDocumentsPath);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileInfo.Name)}{fileInfo.Extension}";
                var destinationPath = Path.Combine(companyDocumentsPath, fileName);

                File.Copy(_selectedFilePath, destinationPath, true);

                var document = new CompanyDocument
                {
                    Title = titleBox.Text,
                    FileName = fileName,
                    FilePath = destinationPath,
                    FileType = Path.GetExtension(_selectedFilePath),
                    FileSize = fileInfo.Length,
                    Category = (DocumentCategory)categoryBox.SelectedValue,
                    IsRequired = isRequiredCheck.IsChecked == true,
                    Description = descriptionBox.Text,
                    UploadedBy = App.CurrentUser?.Id ?? 1,
                    IsActive = true,
                    StorageType = "Central",
                    FullPath = destinationPath
                };

                var selectedCategory = (DocumentCategory)categoryBox.SelectedValue;
                if (selectedCategory == DocumentCategory.JobDescription &&
                    jobTitleComboBox.SelectedValue != null)
                {
                    document.JobTitleId = (int)jobTitleComboBox.SelectedValue;
                }

                _context.CompanyDocuments.Add(document);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage($"تم رفع الملف بنجاح إلى:\n{destinationPath}", LocalizationManager.Translate("نجاح"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                await LoadDocuments();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في رفع الملف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            titleBox.Clear();
            categoryBox.SelectedIndex = -1;
            descriptionBox.Clear();
            isRequiredCheck.IsChecked = false;
            selectedFileText.Text = "";
            _selectedFilePath = null;
            jobTitleComboBox.SelectedIndex = -1;
            jobTitleSection.Visibility = Visibility.Collapsed;
        }

        private async void downloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                LocalizationManager.ShowMessage("يرجى اختيار ملف أولاً", LocalizationManager.Translate("تحذير"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sourcePath;

                if (!string.IsNullOrEmpty(_selectedDocument.FullPath) && File.Exists(_selectedDocument.FullPath))
                    sourcePath = _selectedDocument.FullPath;
                else if (!string.IsNullOrEmpty(_selectedDocument.FilePath) && File.Exists(_selectedDocument.FilePath))
                    sourcePath = _selectedDocument.FilePath;
                else
                    sourcePath = Path.Combine(AppDbContext.CentralStoragePath, "CompanyDocuments", _selectedDocument.FileName);

                if (!File.Exists(sourcePath))
                {
                    LocalizationManager.ShowMessage("الملف غير موجود على السيرفر", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    FileName = _selectedDocument.Title + _selectedDocument.FileType,
                    Filter = $"Files (*{_selectedDocument.FileType})|*{_selectedDocument.FileType}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(sourcePath, saveDialog.FileName, true);
                    LocalizationManager.ShowMessage("تم تحميل الملف بنجاح", LocalizationManager.Translate("نجاح"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل الملف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void toggleActiveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null) return;

            try
            {
                _selectedDocument.IsActive = !_selectedDocument.IsActive;
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage($"تم {(_selectedDocument.IsActive ? "تفعيل" : "تعطيل")} الملف بنجاح",
                    LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadDocuments();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحديث حالة الملف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null) return;

            var result = LocalizationManager.ShowMessage($"هل أنت متأكد من حذف الملف '{_selectedDocument.Title}'؟",
                LocalizationManager.Translate("تأكيد الحذف"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string[] possiblePaths = {
                        _selectedDocument.FullPath,
                        _selectedDocument.FilePath,
                        Path.Combine(AppDbContext.CentralStoragePath, "CompanyDocuments", _selectedDocument.FileName),
                        Path.Combine(Directory.GetCurrentDirectory(), "CompanyDocuments", _selectedDocument.FileName)
                    };

                    foreach (var path in possiblePaths)
                        if (!string.IsNullOrEmpty(path) && File.Exists(path)) { File.Delete(path); break; }

                    _context.CompanyDocuments.Remove(_selectedDocument);
                    await _context.SaveChangesAsync();

                    LocalizationManager.ShowMessage("تم حذف الملف بنجاح", LocalizationManager.Translate("نجاح"),
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _selectedDocument = null;
                    await LoadDocuments();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ في حذف الملف: {ex.Message}", LocalizationManager.Translate("خطأ"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void viewSignaturesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                LocalizationManager.ShowMessage("يرجى اختيار ملف أولاً", LocalizationManager.Translate("تحذير"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // أضف كود عرض التوقيعات هنا
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            // دعم زر المعاينة من داخل الـ DataGrid Row
            if (sender is Button btn && btn.DataContext is DocumentViewModel vm)
                _selectedDocument = vm.Original;

            if (_selectedDocument == null)
            {
                LocalizationManager.ShowMessage("يرجى اختيار ملف أولاً", LocalizationManager.Translate("تحذير"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var previewWindow = new DocumentPreviewWindow(_selectedDocument.Id, false);
                previewWindow.Owner = this;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في فتح المعاينة: {ex.Message}", LocalizationManager.Translate("خطأ"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // الفلاتر
        private async void categoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            await LoadDocuments();

        private async void jobTitleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            await LoadDocuments();

        private async void activeOnlyCheck_Changed(object sender, RoutedEventArgs e) =>
            await LoadDocuments();

        private void categoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryBox.SelectedValue != null)
            {
                var selectedCategory = (DocumentCategory)categoryBox.SelectedValue;
                jobTitleSection.Visibility = selectedCategory == DocumentCategory.JobDescription
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                if (selectedCategory != DocumentCategory.JobDescription)
                    jobTitleComboBox.SelectedIndex = -1;
            }
        }

        private void clearJobTitleFilter_Click(object sender, RoutedEventArgs e) =>
            jobTitleFilterComboBox.SelectedIndex = -1;

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCategories();
            activeOnlyCheck.IsChecked = true;
        }

        
    }
}

