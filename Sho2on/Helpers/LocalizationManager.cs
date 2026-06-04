using System.Globalization;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using HR_Application.Properties;
using Application = System.Windows.Application;
using FlowDirection = System.Windows.FlowDirection;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.Helpers
{
    public static class LocalizationManager
    {
        private const string ArabicCode = "ar";
        private const string EnglishCode = "en";
        private const string LocalizationDictionaryPathPart = "Resources/Localization.";

        public static string CurrentLanguage { get; private set; } = ArabicCode;

        public static bool IsEnglish => CurrentLanguage == EnglishCode;

        private static readonly Dictionary<string, string> English = new()
        {
            ["Welcome to Sho2on"] = "Welcome to Sho2on",
            ["Username"] = "Username",
            ["Password"] = "Password",
            ["Database Host"] = "Database Host",
            ["تسجيل الدخول"] = "Login",
            ["نظام الموارد البشرية"] = "Human Resources System",
            ["القائمة الرئيسية"] = "Main Menu",
            ["التقارير"] = "Reports",
            ["بيانات الموظفين"] = "Employee Data",
            ["شهري الموظفين"] = "Monthly Employees",
            ["شئون العاملين"] = "Personnel Affairs",
            ["الاجراءات"] = "Requests",
            ["الإجراءات"] = "Requests",
            ["ادارة الاجازات"] = "Leave Management",
            ["إدارة الاجازات"] = "Leave Management",
            ["ادارة موظف"] = "Employee Management",
            ["تقييم موظف"] = "Employee Evaluation",
            ["رفع ملف الموظفين"] = "Import Employees",
            ["الحضور و الانصراف"] = "Attendance",
            ["مراجعة الحركات"] = "Review Records",
            ["الحركات الشهرية"] = "Monthly Records",
            ["الاجور والمرتبات"] = "Payroll",
            ["ادارة ماليات موظف"] = "Employee Financials",
            ["تقرير المرتبات"] = "Salary Report",
            ["بيانات و مرتبات الموظفين"] = "Employee Salaries",
            ["استحقاقات و استقطاعت"] = "Benefits and Deductions",
            ["رفع العمولات"] = "Import Commissions",
            ["البيانات الشهرية"] = "Monthly Data",
            ["صرف المرتبات الجماعي"] = "Bulk Salary Payment",
            ["ادارة السلف"] = "Loan Management",
            ["البيانات"] = "Data",
            ["اضافة البيانات"] = "Add Data",
            ["الإدارات"] = "Departments",
            ["الادارات"] = "Departments",
            ["المؤهلات"] = "Qualifications",
            ["الوظائف"] = "Jobs",
            ["القطاعات"] = "Sectors",
            ["المناطق"] = "Areas",
            ["الورديات"] = "Shifts",
            ["الراحات"] = "Breaks",
            ["التأخير و الاضافي"] = "Late and Overtime",
            ["الاجازات الاسبوعية"] = "Weekly Holidays",
            ["أنواع الاجازات"] = "Leave Types",
            ["أنواع الإجازات"] = "Leave Types",
            ["المسؤولون"] = "Officials",
            ["الصلاحيات"] = "Permissions",
            ["صلاحيات البرنامج"] = "Program Permissions",
            ["صلاحيات المستخدم"] = "User Permissions",
            ["صلاحيات الفروع"] = "Branch Permissions",
            ["الجروبات"] = "Groups",
            ["الإعدادات"] = "Settings",
            ["الاعدادات العامة"] = "General Settings",
            ["اعدادات عامة"] = "General Settings",
            ["الارشيف"] = "Archive",
            ["تخزين الملفات"] = "File Storage",
            ["اضافة الفروع"] = "Add Branches",
            ["إدارة الرصيد"] = "Manage Balance",
            ["طلبات الاجازة"] = "Leave Requests",
            ["طلب إجازة"] = "Leave Request",
            ["تقرير الرصيد"] = "Balance Report",
            ["طلب مأمورية"] = "Mission Request",
            ["طلبات المأموريات"] = "Mission Requests",
            ["طلبات الاذن"] = "Permission Requests",
            ["طلب اذن"] = "Permission Request",
            ["طلب سلفة"] = "Loan Request",
            ["الموافقة على السلفات"] = "Loan Approval",
            ["كشف حساب ص الزمالة"] = "Friendship Fund Statement",
            ["تحميل بيانات"] = "Load Data",
            ["إعدادات التحميل"] = "Load Settings",
            ["تاريخ البدء:"] = "Start Date:",
            ["تحميل بيانات الفرع"] = "Load Branch Data",
            ["خروج"] = "Exit",
            ["حفظ"] = "Save",
            ["تعديل"] = "Edit",
            ["حذف"] = "Delete",
            ["إلغاء"] = "Cancel",
            ["موافق"] = "OK",
            ["إضافة"] = "Add",
            ["بحث"] = "Search",
            ["طباعة"] = "Print",
            ["إغلاق"] = "Close",
            ["معاينة"] = "Preview",
            ["تحميل"] = "Download",
            ["رفع الملف"] = "Upload File",
            ["اختر الملف"] = "Choose File",
            ["لم يتم اختيار ملف"] = "No file selected",
            ["إدارة ملفات الشركة"] = "Company Documents",
            ["رفع ملف جديد"] = "Upload New File",
            ["عنوان الملف"] = "File Title",
            ["التصنيف"] = "Category",
            ["الوصف"] = "Description",
            ["مطلوب للتوقيع"] = "Required for Signature",
            ["الملفات النشطة فقط"] = "Active Files Only",
            ["إجراءات الملفات"] = "File Actions",
            ["معاينة الملف"] = "Preview File",
            ["تحميل الملف"] = "Download File",
            ["حذف الملف"] = "Delete File",
            ["عرض التوقيعات"] = "View Signatures",
            ["جاهز"] = "Ready",
            ["إضافة وثيقة للموظف"] = "Add Employee Document",
            ["عنوان الوثيقة:"] = "Document Title:",
            ["نوع الوثيقة:"] = "Document Type:",
            ["الملف:"] = "File:",
            ["تاريخ الانتهاء:"] = "Expiry Date:",
            ["الوصف:"] = "Description:",
            ["الملفات المدعومة:"] = "Supported Files:",
            ["الحد الأقصى للحجم: 10MB"] = "Maximum size: 10MB",
            ["إدارة الصلاحيات"] = "Permission Management",
            ["الدور:"] = "Role:",
            ["حفظ الصلاحيات"] = "Save Permissions",
            ["قائمة الصلاحيات"] = "Permission List",
            ["بيانات الشركة"] = "Company Data",
            ["إعدادات الشهر"] = "Month Settings",
            ["إعدادات البرنامج"] = "Program Settings",
            ["المهام"] = "Tasks",
            ["الالوان"] = "Colors",
            ["اللون الرئيسي"] = "Primary Color",
            ["اللون الفرعي"] = "Secondary Color",
            ["اللون الثالث"] = "Third Color",
            ["لون الخلفية"] = "Background Color",
            ["لون القائمة"] = "Menu Color",
            ["لون الكلام الرئيسي"] = "Primary Text Color",
            ["لون الكلام الفرعي"] = "Secondary Text Color",
            ["صلاحيات الفروع"] = "Branch Permissions",
            ["المستخدم :"] = "User:",
            ["الفرع"] = "Branch",
            ["صلاحيات المستخدم"] = "User Permissions",
            ["الجروب"] = "Group",
            ["اسم المستخددم"] = "Username",
            ["كلمة المرور"] = "Password",
            ["الكود"] = "Code",
            ["الاسم"] = "Name",
            ["المحادثات"] = "Chats",
            ["إضافة محادثة"] = "Add Chat",
            ["تغيير الخلفية"] = "Change Background",
            ["إضافة محادثة جديدة"] = "Add New Chat",
            ["إنشاء مجموعة جديدة"] = "Create New Group",
            ["اسم المجموعة"] = "Group Name",
            ["إضافة أعضاء"] = "Add Members",
            ["اختر محادثة للبدء"] = "Choose a chat to start",
            ["تعديل رسالة..."] = "Editing message...",
            ["إرفاق ملف"] = "Attach File",
            ["سبب الرفض"] = "Rejection Reason",
            ["الرجاء إدخال سبب رفض طلب الإجازة:"] = "Please enter the reason for rejecting the leave request:",
            ["معاينة الطباعة"] = "Print Preview",
            ["إعدادات الشبكة والتخزين"] = "Network and Storage Settings",
            ["إعدادات تخزين الملفات"] = "File Storage Settings",
            ["اختبار الاتصال"] = "Test Connection",
            ["حفظ الإعدادات"] = "Save Settings",
            ["إجمالي الموظفين"] = "Total Employees",
            ["عدد الفروع"] = "Branch Count",
            ["إجازات معلقة"] = "Pending Leaves",
            ["حضور اليوم"] = "Today Attendance",
            ["موظف"] = "Employee",
            ["فرع"] = "Branch",
            ["إدارة"] = "Department",
            ["عرض الفروع"] = "Show Branches",
            ["عرض الإدارات"] = "Show Departments",
            ["إجراءات سريعة"] = "Quick Actions",
            ["إدارة الموظفين"] = "Employee Management",
            ["الحضور الشهري"] = "Monthly Attendance",
            ["صرف المرتبات"] = "Salary Payment",
            ["النسخ الاحتياطي"] = "Backup",
            ["الإنذارات والإشعارات"] = "Alerts and Notifications",
            ["توزيع النوع"] = "Gender Distribution",
            ["الموظفون بالقطاعات"] = "Employees by Sector",
            ["الموظفون بالإدارات"] = "Employees by Department",
            ["الموظفون بالفروع"] = "Employees by Branch",
            ["ذكور: 0"] = "Male: 0",
            ["إناث: 0"] = "Female: 0",
            ["رجوع للقطاعات"] = "Back to Sectors",
            ["الفروع — اضغط على فرع لعرض إداراته"] = "Branches - click a branch to view departments",
            ["القطاعات — اضغط على أي قطاع لعرض فروعه"] = "Sectors - click a sector to view branches",
            ["خطأ"] = "Error",
            ["لا يمكن الاتصال بقاعدة البيانات. تأكد من إعدادات الخادم."] = "Cannot connect to the database. Check the server settings.",
            ["Please fill in all required fields."] = "Please fill in all required fields.",
            ["Invalid username or password."] = "Invalid username or password.",
        };

        private static readonly Dictionary<string, string> Arabic;

        static LocalizationManager()
        {
            Arabic = new(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in English)
            {
                var normalized = Normalize(kvp.Value);
                if (!Arabic.ContainsKey(normalized))
                {
                    Arabic[normalized] = kvp.Key;
                }
            }
        }

        public static void Initialize()
        {
            SetLanguage(string.IsNullOrWhiteSpace(Properties.Settings.Default.Language)
                ? ArabicCode
                : Properties.Settings.Default.Language, false);
        }

        public static void RegisterAutomaticLocalization()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    SetLanguage(string.IsNullOrWhiteSpace(Properties.Settings.Default.Language)
                        ? ArabicCode
                        : Properties.Settings.Default.Language, false);
                }));

            EventManager.RegisterClassHandler(typeof(UserControl), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    SetLanguage(string.IsNullOrWhiteSpace(Properties.Settings.Default.Language)
                        ? ArabicCode
                        : Properties.Settings.Default.Language, false);
                }));
        }

        public static void ToggleLanguage()
        {
            SetLanguage(IsEnglish ? ArabicCode : EnglishCode);
        }

        public static void SetLanguage(string language, bool save = true)
        {
            CurrentLanguage = language == EnglishCode ? EnglishCode : ArabicCode;
            var culture = IsEnglish ? new CultureInfo("en-US") : new CultureInfo("ar-EG");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            ApplyResourceDictionary();

            if (save)
            {
                Properties.Settings.Default.Language = CurrentLanguage;
                Properties.Settings.Default.Save();
            }

            foreach (Window window in Application.Current.Windows)
            {
                ApplyWindowCulture(window);
            }
        }

        public static string Translate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var normalized = Normalize(value);
            if (IsEnglish)
            {
                return English.TryGetValue(normalized, out var translated)
                    ? PreserveAffixes(value, translated)
                    : value;
            }

            return Arabic.TryGetValue(normalized, out var translatedArabic)
                ? PreserveAffixes(value, translatedArabic)
                : value;
        }

        public static MessageBoxResult ShowMessage(string message)
        {
            return ShowMessage(message, null, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
        }

        public static MessageBoxResult ShowMessage(string message, string caption)
        {
            return ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
        }

        public static MessageBoxResult ShowMessage(string message, string caption, MessageBoxButton buttons)
        {
            return ShowMessage(message, caption, buttons, MessageBoxImage.None, MessageBoxResult.None);
        }

        public static MessageBoxResult ShowMessage(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        {
            return ShowMessage(message, caption, buttons, icon, MessageBoxResult.None);
        }

        public static MessageBoxResult ShowMessage(string message, string caption, MessageBoxButton buttons,
            MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            var translatedMessage = Translate(message);
            var translatedCaption = string.IsNullOrWhiteSpace(caption) ? caption : Translate(caption);
            return System.Windows.MessageBox.Show(translatedMessage, translatedCaption, buttons, icon, defaultResult);
        }

        public static FlowDirection CurrentFlowDirection =>
            IsEnglish ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

        private static void ApplyResourceDictionary()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var existing = dictionaries.FirstOrDefault(IsLocalizationDictionary);

            if (existing != null)
            {
                dictionaries.Remove(existing);
            }

            var insertIndex = 0;
            for (var i = dictionaries.Count - 1; i >= 0; i--)
            {
                if (IsThemeDictionary(dictionaries[i]))
                {
                    insertIndex = i + 1;
                    break;
                }
            }

            dictionaries.Insert(insertIndex, new ResourceDictionary
            {
                Source = new Uri($"Resources/Localization.{CurrentLanguage}.xaml", UriKind.Relative)
            });
        }

        private static bool IsLocalizationDictionary(ResourceDictionary dictionary)
        {
            return dictionary.Source?.OriginalString.Contains(LocalizationDictionaryPathPart, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsThemeDictionary(ResourceDictionary dictionary)
        {
            var source = dictionary.Source?.OriginalString;
            return !string.IsNullOrWhiteSpace(source) &&
                   source.Contains("Theme.xaml", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyWindowCulture(Window window)
        {
            window.FlowDirection = CurrentFlowDirection;
            window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(
                Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
        }

        private static void LocalizeElement(DependencyObject element)
        {
            if (element == null)
            {
                return;
            }

            if (element is Window window && !BindingOperations.IsDataBound(window, Window.TitleProperty))
            {
                window.Title = Translate(window.Title);
            }
            else if (element is TextBlock textBlock && !BindingOperations.IsDataBound(textBlock, TextBlock.TextProperty))
            {
                textBlock.Text = Translate(textBlock.Text);
            }
            else if (element is HeaderedContentControl headeredControl && !BindingOperations.IsDataBound(headeredControl, HeaderedContentControl.HeaderProperty) && headeredControl.Header is string headerText)
            {
                headeredControl.Header = Translate(headerText);
            }
            else if (element is ContentControl contentControl && !BindingOperations.IsDataBound(contentControl, ContentControl.ContentProperty) && contentControl.Content is string contentText)
            {
                contentControl.Content = Translate(contentText);
            }
            else if (element is MenuItem menuItem && !BindingOperations.IsDataBound(menuItem, MenuItem.HeaderProperty) && menuItem.Header is string menuHeader)
            {
                menuItem.Header = Translate(menuHeader);
            }
            else if (element is DataGrid dataGrid)
            {
                foreach (var column in dataGrid.Columns)
                {
                    if (column.Header is string headerValue)
                    {
                        column.Header = Translate(headerValue);
                    }
                }
            }
            else if (element is ContentPresenter contentPresenter && contentPresenter.Content is string presenterText)
            {
                contentPresenter.Content = Translate(presenterText);
            }
            else if (element is ListBoxItem listBoxItem && listBoxItem.Content is string listItemText)
            {
                listBoxItem.Content = Translate(listItemText);
            }
            else if (element is ComboBoxItem comboBoxItem && comboBoxItem.Content is string comboItemText)
            {
                comboBoxItem.Content = Translate(comboItemText);
            }
            else if (element is TreeViewItem treeViewItem && treeViewItem.Header is string treeHeaderText)
            {
                treeViewItem.Header = Translate(treeHeaderText);
            }

            if (element is FrameworkElement frameworkElement && frameworkElement.ToolTip is string tooltipText)
            {
                frameworkElement.ToolTip = Translate(tooltipText);
            }

            foreach (var child in GetChildren(element))
            {
                LocalizeElement(child);
            }
        }

        private static IEnumerable<DependencyObject> GetChildren(DependencyObject element)
        {
            if (element == null)
            {
                yield break;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is DependencyObject childObject)
                {
                    yield return childObject;
                }
            }

            var count = VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < count; i++)
            {
                var visualChild = VisualTreeHelper.GetChild(element, i);
                if (visualChild != null)
                {
                    yield return visualChild;
                }
            }
        }

        private static string Normalize(string value)
        {
            return value.Trim()
                .Replace("✏️ ", string.Empty)
                .Replace("🗑️ ", string.Empty)
                .Replace("❌ ", string.Empty)
                .Replace("💾 ", string.Empty)
                .Replace("🖨️ ", string.Empty)
                .Replace("📁 ", string.Empty)
                .Replace("✨ ", string.Empty)
                .Replace("💬 ", string.Empty)
                .Replace("👥 ", string.Empty)
                .Replace("📦 ", string.Empty)
                .Replace("📤 ", string.Empty)
                .Replace("+ ", string.Empty);
        }

        private static string PreserveAffixes(string original, string translated)
        {
            var prefix = original.TrimStart().StartsWith("+", StringComparison.Ordinal) ? "+ " : string.Empty;
            return prefix + translated;
        }
    }
}

