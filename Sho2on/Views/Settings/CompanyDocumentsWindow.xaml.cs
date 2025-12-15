using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HR_Application.Views
{
    public partial class CompanyDocumentsWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private string _selectedFilePath;
        private CompanyDocument _selectedDocument;

        public CompanyDocumentsWindow()
        {
            InitializeComponent();
            CheckStorageAccessibility();
        }

        private void CheckStorageAccessibility()
        {
            try
            {
                string storagePath = AppDbContext.CentralStoragePath;
                bool isNetworkPath = storagePath.StartsWith(@"\\");

                if (isNetworkPath)
                {
                    // اختبار سرعة الوصول للمسار الشبكي
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    bool canAccess = Directory.Exists(storagePath);
                    stopwatch.Stop();

                    if (canAccess)
                    {
                        statusText.Text = $"المسار المركزي: {storagePath} (زمن الوصول: {stopwatch.ElapsedMilliseconds}ms)";

                        if (stopwatch.ElapsedMilliseconds > 1000)
                        {
                            statusText.Text += " - اتصال بطيء";
                        }
                    }
                    else
                    {
                        statusText.Text = "غير متصل بالمسار المركزي - استخدام المسار المحلي";
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
            // تحميل التصنيفات
            categoryBox.ItemsSource = Enum.GetValues(typeof(DocumentCategory))
                .Cast<DocumentCategory>()
                .Select(c => new { Value = (int)c, Name = GetCategoryName(c) })
                .ToList();

            categoryBox.DisplayMemberPath = "Name";
            categoryBox.SelectedValuePath = "Value";

            categoryFilter.ItemsSource = categoryBox.ItemsSource;
            categoryFilter.DisplayMemberPath = "Name";
            categoryFilter.SelectedValuePath = "Value";

            // تحميل الوظائف إذا كان التصنيف JobDescription
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
                MessageBox.Show($"خطأ في تحميل الوظائف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task LoadDocuments()
        {
            try
            {
                var query = _context.CompanyDocuments
                    .Include(d => d.Uploader)
                    .Include(d => d.JobTitle) // تضمين بيانات الوظيفة
                    .AsQueryable();

                // Apply category filter
                if (categoryFilter.SelectedValue != null)
                {
                    var selectedCategory = (int)categoryFilter.SelectedValue;
                    query = query.Where(d => d.Category == (DocumentCategory)selectedCategory);
                }

                // Apply job title filter
                if (jobTitleFilterComboBox.SelectedValue != null)
                {
                    var selectedJobTitleId = (int)jobTitleFilterComboBox.SelectedValue;
                    query = query.Where(d => d.JobTitleId == selectedJobTitleId);
                }

                // Apply active filter
                if (activeOnlyCheck.IsChecked == true)
                {
                    query = query.Where(d => d.IsActive);
                }

                var documents = await query
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();

                documentsGrid.ItemsSource = documents;
                statusText.Text = $"تم تحميل {documents.Count} ملف";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الملفات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "جميع الملفات (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt",
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
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void uploadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(titleBox.Text) || categoryBox.SelectedValue == null ||
                string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("يرجى ملء جميع الحقول واختيار ملف", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_selectedFilePath);

                // استخدام المسار المركزي
                string storagePath = AppDbContext.CentralStoragePath;
                string companyDocumentsPath = Path.Combine(storagePath, "CompanyDocuments");

                if (!Directory.Exists(companyDocumentsPath))
                    Directory.CreateDirectory(companyDocumentsPath);

                // توليد اسم فريد للملف
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileInfo.Name)}{fileInfo.Extension}";
                var destinationPath = Path.Combine(companyDocumentsPath, fileName);

                // نسخ الملف للمسار المركزي
                File.Copy(_selectedFilePath, destinationPath, true);

                // إنشاء سجل الملف في قاعدة البيانات
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
                    StorageType = "Central", // إضافة حقل لتحديد نوع التخزين
                    FullPath = destinationPath
                };

                // إذا كان التصنيف "وصف الوظيفة" وتم اختيار وظيفة
                var selectedCategory = (DocumentCategory)categoryBox.SelectedValue;
                if (selectedCategory == DocumentCategory.JobDescription &&
                    jobTitleComboBox.SelectedValue != null)
                {
                    document.JobTitleId = (int)jobTitleComboBox.SelectedValue;
                }

                _context.CompanyDocuments.Add(document);
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم رفع الملف بنجاح إلى:\n{destinationPath}", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // إعادة تعيين النموذج
                ClearForm();
                await LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في رفع الملف: {ex.Message}", "خطأ",
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
                MessageBox.Show("يرجى اختيار ملف أولاً", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sourcePath;

                // تحديد المسار بناءً على نوع التخزين
                if (!string.IsNullOrEmpty(_selectedDocument.FullPath) && File.Exists(_selectedDocument.FullPath))
                {
                    sourcePath = _selectedDocument.FullPath;
                }
                else if (!string.IsNullOrEmpty(_selectedDocument.FilePath) && File.Exists(_selectedDocument.FilePath))
                {
                    sourcePath = _selectedDocument.FilePath;
                }
                else
                {
                    // البحث في المسار المركزي
                    string storagePath = AppDbContext.CentralStoragePath;
                    sourcePath = Path.Combine(storagePath, "CompanyDocuments", _selectedDocument.FileName);
                }

                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("الملف غير موجود على السيرفر", "خطأ",
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
                    MessageBox.Show("تم تحميل الملف بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الملف: {ex.Message}", "خطأ",
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

                MessageBox.Show($"تم {(_selectedDocument.IsActive ? "تفعيل" : "تعطيل")} الملف بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث حالة الملف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null) return;

            var result = MessageBox.Show($"هل أنت متأكد من حذف الملف '{_selectedDocument.Title}'؟",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // البحث عن الملف وحذفه
                    string[] possiblePaths = {
                        _selectedDocument.FullPath,
                        _selectedDocument.FilePath,
                        Path.Combine(AppDbContext.CentralStoragePath, "CompanyDocuments", _selectedDocument.FileName),
                        Path.Combine(Directory.GetCurrentDirectory(), "CompanyDocuments", _selectedDocument.FileName)
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            File.Delete(path);
                            break;
                        }
                    }

                    // حذف من قاعدة البيانات
                    _context.CompanyDocuments.Remove(_selectedDocument);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم حذف الملف بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _selectedDocument = null;
                    await LoadDocuments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف الملف: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void viewSignaturesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("يرجى اختيار ملف أولاً", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // يمكنك إضافة كود عرض التوقيعات هنا لاحقاً
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("يرجى اختيار ملف أولاً", "تحذير",
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
                MessageBox.Show($"خطأ في فتح المعاينة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void documentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDocument = documentsGrid.SelectedItem as CompanyDocument;
        }

        private async void categoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadDocuments();
        }

        private async void jobTitleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadDocuments();
        }

        private async void activeOnlyCheck_Changed(object sender, RoutedEventArgs e)
        {
            await LoadDocuments();
        }

        // حدث عند تغيير التصنيف في رفع الملف الجديد
        private void categoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categoryBox.SelectedValue != null)
            {
                var selectedCategory = (DocumentCategory)categoryBox.SelectedValue;

                // إظهار قسم اختيار الوظيفة فقط إذا كان التصنيف "وصف الوظيفة"
                if (selectedCategory == DocumentCategory.JobDescription)
                {
                    jobTitleSection.Visibility = Visibility.Visible;
                }
                else
                {
                    jobTitleSection.Visibility = Visibility.Collapsed;
                    jobTitleComboBox.SelectedIndex = -1;
                }
            }
        }

        // حدث لمسح فلتر الوظيفة
        private void clearJobTitleFilter_Click(object sender, RoutedEventArgs e)
        {
            jobTitleFilterComboBox.SelectedIndex = -1;
        }

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