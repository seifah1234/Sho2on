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
            ["                    مدقق: ___________________"] = "Auditor: ___________________",
            ["                    مدير المالية: ___________________"] = "Finance Manager: ___________________",
            ["     إجمالي السحوبات: "] = "Total withdrawals:",
            ["     عدد الحركات: "] = "Number of movements:",
            ["   غير قادر على اكتشاف IPs الشبكة\\n"] = "Unable to discover network IPs\\n",
            [" - اتصال بطيء"] = "- Slow connection",
            [" - تحذير: لا توجد صلاحيات كتابة"] = "- Warning: No writing permissions",
            [" - صلاحيات الكتابة متاحة"] = "- Writing permissions are available",
            ["+ إنشاء مجموعة جديدة"] = "+ Create a new group",
            ["0 تعني لا يوجد حد"] = "0 means no limit",
            ["0 وثيقة"] = "0 document",
            ["0 يوم"] = "0 days",
            ["1 شهر"] = "1 month",
            ["1. معلومات الموظف"] = "1. Employee information",
            ["2 شهر"] = "2 months",
            ["2. معلومات الإجازة"] = "2. Leave information",
            ["3 شهر"] = "3 month",
            ["3. حالة الطلب"] = "3. Order status",
            ["4 شهر"] = "4 month",
            ["4. معلومات الرصيد"] = "4. Balance information",
            ["5 شهر"] = "5 month",
            ["6 شهر"] = "6 month",
            ["? الاتصال فاشل - المسار غير متاح"] = "? Connection failed - path not available",
            ["? الاتصال ناجح - المسار متاح"] = "? Connection successful - path available",
            ["? صلاحيات الكتابة متاحة"] = "? Write permissions are available",
            ["? لا توجد صلاحيات كتابة على المجلد"] = "? There are no write permissions on the folder",
            ["?? تم التعديل"] = "?? Modified",
            ["?? مرفق"] = "?? attached",
            ["HR Application - نظام إدارة الموارد البشرية"] = "HR Application - Human Resources Management System",
            ["\\n6. يمكنك استخدام المسار المحلي مؤقتاً"] = "\\n6. You can use the local path temporarily",
            ["© نظام إدارة الموارد البشرية - جميع الحقوق محفوظة"] = "© Human Resources Management System - All rights reserved",
            ["، "] = ",",
            ["آخر تحديث"] = "Latest update",
            ["آخر راتب"] = "Last salary",
            ["أخرى"] = "Other",
            ["أدخل المسار الشبكي للسيرفر المركزي"] = "Enter the network path to the central server",
            ["أدخل النسبة من 0 إلى 100%"] = "Enter the percentage from 0 to 100%",
            ["أدخل عنواناً واضحاً للوثيقة"] = "Enter a clear title for the document",
            ["أدخل وصفاً للوثيقة (اختياري)"] = "Enter a description of the document (optional)",
            ["أرشيف الموظف"] = "Employee archive",
            ["أعضاء المجموعة"] = "Group members",
            ["أعلى راتب"] = "Highest salary",
            ["أقصى مدة متتالية"] = "Maximum consecutive duration",
            ["أقل راتب"] = "Lowest salary",
            ["أنثى"] = "feminine",
            ["أنواع الإجازات:"] = "Types of leave:",
            ["إجازات مقبولة"] = "Acceptable vacations",
            ["إجازة"] = "vacation",
            ["إجراء"] = "procedure",
            ["إجراءات السلامة"] = "Safety procedures",
            ["إجراءات على الملف المحدد"] = "Actions on the selected file",
            ["إجمالي الإضافات"] = "Total additions",
            ["إجمالي الإيداعات"] = "Total deposits",
            ["إجمالي الإيداعات:"] = "Total deposits:",
            ["إجمالي الإيداعات: "] = "Total deposits:",
            ["إجمالي الاستحقاقات"] = "Total entitlements",
            ["إجمالي الاستقطاعات"] = "Total deductions",
            ["إجمالي الرصيد"] = "Total balance",
            ["إجمالي الرواتب الأساسية"] = "Total basic salaries",
            ["إجمالي السحوبات"] = "Total withdrawals",
            ["إجمالي السحوبات:"] = "Total withdrawals:",
            ["إجمالي السداد"] = "Total payment",
            ["إجمالي السداد:"] = "Total payment:",
            ["إجمالي السلف"] = "Total advances",
            ["إجمالي السلف:"] = "Total advances:",
            ["إجمالي الصافي"] = "Total net",
            ["إجمالي المستخدم"] = "Total user",
            ["إجمالي صندوق الزمالة"] = "Total Fellowship Fund",
            ["إجمالي قيمة الأضافي"] = "Total additional value",
            ["إجمالي قيمة التأخير"] = "Total delay value",
            ["إحصائيات"] = "statistics",
            ["إحصائيات التخزين"] = "Storage statistics",
            ["إحصائيات التقرير"] = "Report statistics",
            ["إحصائيات الشهر الحالي"] = "Statistics for the current month",
            ["إحصائيات الشهور"] = "Monthly statistics",
            ["إحصائيات الصندوق"] = "Fund statistics",
            ["إحصائيات الفروع"] = "Branch statistics",
            ["إحصائيات المرتبات"] = "Salary statistics",
            ["إحصائيات حسب الشهر"] = "Statistics by month",
            ["إحصائيات حسب الفرع"] = "Statistics by branch",
            ["إدارة أنواع الإجازات"] = "Managing leave types",
            ["إدارة الأعضاء"] = "Member management",
            ["إدارة الفريق"] = "Team management",
            ["إدارة المرتبات"] = "Payroll management",
            ["إدارة رصيد الإجازات"] = "Managing leave balance",
            ["إدارة صندوق الزمالة المشترك"] = "Managing the joint fellowship fund",
            ["إدارة طلبات الإجازة"] = "Managing leave requests",
            ["إداري"] = "administrative",
            ["إذن"] = "permission",
            ["إذن شخصي"] = "Personal permission",
            ["إرسال الطلب للمدير"] = "Send the request to the manager",
            ["إزالة"] = "removal",
            ["إزالة الصورة"] = "Remove image",
            ["إزالة من المجموعة"] = "Remove from group",
            ["إضافة إيداع يدوي"] = "Add a manual deposit",
            ["إضافة إيداع يدوي لصندوق الزمالة"] = "Add a manual deposit to the Fellowship Fund",
            ["إضافة جديد"] = "Add new",
            ["إضافة سحب يدوي"] = "Add manual withdrawal",
            ["إضافة سحب يدوي من صندوق الزمالة"] = "Adding a manual withdrawal from the fellowship fund",
            ["إضافة عضو جديد"] = "Add a new member",
            ["إضافة مرتب أساسي"] = "Add a basic salary",
            ["إضافة مهمة جديدة"] = "Add a new task",
            ["إضافة موظف جديد"] = "Add a new employee",
            ["إضافة وثيقة"] = "Add document",
            ["إضافي"] = "additional",
            ["إعادة الضبط"] = "Reset",
            ["إعادة تعيين"] = "Reset",
            ["إعدادات الصندوق"] = "Box settings",
            ["إعدادات النوع"] = "Type settings",
            ["إلغاء الاختيار"] = "Cancel selection",
            ["إلغاء التحديد"] = "Deselect",
            ["إلغاء العملية"] = "Cancel the operation",
            ["إلغاء غياب"] = "Cancel absence",
            ["إلى"] = "to",
            ["إلى تاريخ"] = "To date",
            ["إلى تاريخ:"] = "To date:",
            ["إناث"] = "Females",
            ["إنشاء"] = "construction",
            ["إنشاء المجلدات الهيكلية"] = "Create structured folders",
            ["إنشاء جميع المجلدات المطلوبة"] = "Create all required folders",
            ["إنهاء الخدمات"] = "Termination of services",
            ["إنهاء خدمات"] = "Termination of services",
            ["إيداع"] = "Deposit",
            ["ابدأ المحادثة الآن"] = "Start the conversation now",
            ["اجراء"] = "procedure",
            ["اجمالي الاستحقاقات"] = "Total entitlements",
            ["اجمالي الاستقطاعات"] = "Total deductions",
            ["اختبار Ping للسيرفر"] = "Ping test for the server",
            ["اختبار الصلاحيات"] = "Authority testing",
            ["اختبار مشاركة المجلد"] = "Test folder sharing",
            ["اختر الملف الموقع"] = "Choose the file location",
            ["اختر الملفات لإرفاقها"] = "Choose files to attach",
            ["اختر الموافق"] = "Select OK",
            ["اختر الموافق على الإجازة"] = "Select OK to leave",
            ["اختر الموافق على الإذن"] = "Choose to agree to the permission",
            ["اختر الموافق على المأمورية"] = "Choose to approve the mission",
            ["اختر الموظف"] = "Select employee",
            ["اختر الموظف القائم عن العمل"] = "Select the current employee",
            ["اختر الموظف لطلب الإجازة"] = "Select the employee to request leave",
            ["اختر الموظف لطلب الإذن"] = "Select the employee to request permission",
            ["اختر الموظف ليتم تقييمه"] = "Select the employee to be evaluated",
            ["اختر الوظيفة المرتبطة بملف وصف الوظيفة"] = "Select the job associated with the job description file",
            ["اختر تاريخ انتهاء الصلاحية (اختياري)"] = "Choose an expiration date (optional)",
            ["اختر جروب للبدء"] = "Choose a group to get started",
            ["اختر خلفية للشات"] = "Choose a chat background",
            ["اختر خيار الطباعة:"] = "Choose print option:",
            ["اختر صورة الموظف"] = "Choose an employee photo",
            ["اختر صورة للمجموعة"] = "Choose a photo for the group",
            ["اختر مدير للموافقة:"] = "Select a manager to approve:",
            ["اختر ملف من جهازك"] = "Choose a file from your device",
            ["اختر موظف"] = "Select an employee",
            ["اختر نوع التصدير"] = "Choose the export type",
            ["اختر نوع الوثيقة المناسب"] = "Choose the appropriate document type",
            ["اختر نوع ملف التصدير:"] = "Choose the export file type:",
            ["اختيار"] = "to choose",
            ["اختيار المدير للموافقة"] = "Director's selection for approval",
            ["اخر رسالة"] = "Last message",
            ["ادارات"] = "Departments",
            ["ادارة"] = "administration",
            ["ادارة طلبات الاذن"] = "Manage permission requests",
            ["ادارة طلبات المأموريات"] = "Managing errand requests",
            ["ادارة ماليات"] = "Financial management",
            ["اذونات:"] = "Permissions:",
            ["ارسلت"] = "sent",
            ["استحقاقات"] = "Entitlements",
            ["استحقاقات واستقطاعات"] = "Entitlements and deductions",
            ["استخدام IP الجهاز الحالي"] = "Use the current device IP",
            ["استخدام IP المحلي"] = "Use local IP",
            ["استخدام اسم السيرفر"] = "Use the server name",
            ["استخدام الأرقام العربية"] = "Use Arabic numbers",
            ["استخدام مجلد محلي"] = "Use a local folder",
            ["استقطاعات"] = "Deductions",
            ["استلمت"] = "I received",
            ["اسم الفرع"] = "Branch name",
            ["اسم المستحدم"] = "Username",
            ["اسم المعيار"] = "Standard name",
            ["اسم الموظف"] = "Employee name",
            ["اسم الموظف:"] = "Employee name:",
            ["اسم نوع الإجازة *"] = "Leave type name *",
            ["اضافة"] = "addition",
            ["اضافة مهمه"] = "An important addition",
            ["اضافة موظف"] = "Add an employee",
            ["اضافي"] = "additional",
            ["اضافي:"] = "additional:",
            ["اظهار المسؤولون للكل"] = "Show administrators to everyone",
            ["اعدادات حساب التأخير و الاضافي"] = "Delay and additional account settings",
            ["اقتراحات لحل المشكلة:\\n\\n"] = "Suggestions to solve the problem:\\n\\n",
            ["الأحد"] = "Sunday",
            ["الأخيرة"] = "The last",
            ["الأربعاء"] = "Wednesday",
            ["الأرشيف"] = "archives",
            ["الأعضاء المختارين"] = "Selected members",
            ["الأولى"] = "The first",
            ["الأيام"] = "The days",
            ["الإثنين"] = "Monday",
            ["الإجازات"] = "Vacations",
            ["الإجازة الأسبوعية"] = "Weekly vacation",
            ["الإجازة الأسبوعية:"] = "Weekly vacation:",
            ["الإجازة السنوية:"] = "annual leave:",
            ["الإجمالي"] = "Total",
            ["الإجمالي الصافي للموظف:"] = "Employee net total:",
            ["الإجمالي:"] = "Total:",
            ["الإجماليات:"] = "Totals:",
            ["الإحصائيات"] = "Statistics",
            ["الإدارة"] = "Management",
            ["الإدارة:"] = "Management:",
            ["الإضافات"] = "Extras",
            ["الإضافات والخصومات"] = "Add-ons and discounts",
            ["الإضافي"] = "Extra",
            ["الإعفاءات"] = "Exemptions",
            ["الإيداعات"] = "Deposits",
            ["الإيداعات الشهرية:"] = "Monthly deposits:",
            ["الاثنين"] = "Monday",
            ["الاجازات"] = "Vacations",
            ["الاجراء"] = "Procedure",
            ["الاجمالي"] = "Total",
            ["الادارة"] = "Management",
            ["الادارة :"] = "Administration:",
            ["الاستحقاقات"] = "Entitlements",
            ["الاستقطاعات"] = "Deductions",
            ["الاستقطاعات والاستحقاقات"] = "Deductions and benefits",
            ["الاسم:"] = "the name:",
            ["الاضافي"] = "The extra",
            ["الاعدادات"] = "Settings",
            ["الاقامة"] = "Accommodation",
            ["البريد الإلكتروني"] = "e-mail",
            ["البند"] = "item",
            ["البيانات الأساسية"] = "Basic data",
            ["التأثير على الرصيد"] = "Impact on balance",
            ["التأخير"] = "Delay",
            ["التأخير:"] = "Delay:",
            ["التأمين الصحي"] = "health insurance",
            ["التاريخ"] = "the date",
            ["التاريخ :"] = "the date :",
            ["التالي"] = "the next",
            ["التالية"] = "Next",
            ["التدريب"] = "Training",
            ["الترقيات والنقل"] = "Promotions and transfers",
            ["التعيين"] = "Appointment",
            ["التفاصيل"] = "the details",
            ["التقييم الإداري"] = "Administrative evaluation",
            ["التقييم الفني"] = "Technical evaluation",
            ["التوقيت"] = "Timing",
            ["الثلاثاء"] = "Tuesday",
            ["الجمعة"] = "Friday",
            ["الحالة"] = "the condition",
            ["الحالة الاجتماعية"] = "marital status",
            ["الحالة الوظيفية"] = "Employment status",
            ["الحالة:"] = "the condition:",
            ["الحد الأقصى المسموح:"] = "Maximum allowed:",
            ["الحد الأقصى للأيام المتتالية"] = "Maximum consecutive days",
            ["الحد الأقصى:"] = "maximum:",
            ["الحركات التفصيلية"] = "Detailed movements",
            ["الحضور اليوم"] = "Attendance today",
            ["الخميس"] = "Thursday",
            ["الدخول"] = "Login",
            ["الراتب"] = "Salary",
            ["الراتب الأساسي"] = "Basic salary",
            ["الراتب الأساسي:"] = "Basic salary:",
            ["الراحة"] = "Comfort",
            ["الراحة الاسبوعية"] = "Weekly rest",
            ["الرجاء البحث عن موظف"] = "Please search for an employee",
            ["الرصيد"] = "Balance",
            ["الرصيد الافتتاحي"] = "Opening balance",
            ["الرصيد الافتراضي"] = "Default balance",
            ["الرصيد الافتراضي (أيام)"] = "Default balance (days)",
            ["الرصيد الحالي:"] = "Current balance:",
            ["الرصيد الختامي"] = "Closing balance",
            ["الرصيد الكلي"] = "Total balance",
            ["الرصيد بعد"] = "Balance after",
            ["الرصيد قبل"] = "Balance before",
            ["الرقم التأميني"] = "Insurance number",
            ["الرقم القومي"] = "National number",
            ["السابق"] = "the previous",
            ["السابقة"] = "Previous",
            ["السبب"] = "the reason",
            ["السبت"] = "Saturday",
            ["السحوبات"] = "Withdrawals",
            ["السداد"] = "Payment",
            ["السلف"] = "predecessor",
            ["السلف الحالية"] = "Current advances",
            ["السلف الشهرية:"] = "Monthly advances:",
            ["السلف والخصميات"] = "Advances and deductions",
            ["السلفة المستحقة:"] = "Due advance:",
            ["السنة"] = "Sunnah",
            ["السنة :"] = "Sunnah:",
            ["السنة:"] = "Sunnah:",
            ["الشركة"] = "Company",
            ["الشهر"] = "month",
            ["الشهر :"] = "Month:",
            ["الشهر:"] = "Month:",
            ["الصافي"] = "Net",
            ["الصور|"] = "Photos|",
            ["العدد"] = "number",
            ["العدد الإجمالي:"] = "Total number:",
            ["العقود"] = "Contracts",
            ["العمر"] = "the age",
            ["العملية"] = "Process",
            ["العنوان"] = "the address",
            ["الغياب"] = "Absence",
            ["الغياب:"] = "Absence:",
            ["الفترة"] = "Period",
            ["الفرع :"] = "Branch:",
            ["الفرع:"] = "Branch:",
            ["الفروع"] = "Branches",
            ["القائم عن العمل"] = "Incumbent",
            ["القائمة السوداء"] = "black list",
            ["القسط الشهري"] = "Monthly installment",
            ["القسط الشهري:"] = "Monthly installment:",
            ["القطاع"] = "sector",
            ["القطاع :"] = "Sector:",
            ["القيمة"] = "Value",
            ["الكل"] = "everyone",
            ["الكود:"] = "Code:",
            ["المأموريات"] = "Errands",
            ["المؤهل"] = "qualification",
            ["المبلغ"] = "Amount",
            ["المبلغ:"] = "Amount:",
            ["المتبقي"] = "residual",
            ["المتبقي:"] = "residual:",
            ["المجموع"] = "the total",
            ["المجموع الكلي:"] = "Grand total:",
            ["المجموع:"] = "the total:",
            ["المدة"] = "Duration",
            ["المدة (أيام)"] = "Duration (days)",
            ["المدة (ساعات)"] = "Duration (hours)",
            ["المدير"] = "the boss",
            ["المدير المختار:"] = "Selected director:",
            ["المرتب"] = "Salary",
            ["المرتب:"] = "Salary:",
            ["المرتبات"] = "Salaries",
            ["المرسل"] = "Sender",
            ["المساحة المتاحة:"] = "Available space:",
            ["المسار الحالي:"] = "Current path:",
            ["المسار الشبكي المركزي"] = "Central retinal path",
            ["المستحق المتبقي:"] = "Remaining due:",
            ["المستخدم"] = "user",
            ["المستخدم هذا العام:"] = "User this year:",
            ["المستخدم:"] = "user:",
            ["المستلم"] = "Recipient",
            ["المستندات|"] = "Documents|",
            ["المسمى الوظيفي"] = "Job title",
            ["المسمى الوظيفي:"] = "Job title:",
            ["المشاركة الاجتماعية"] = "Social sharing",
            ["المعايير الناجحة:"] = "Successful criteria:",
            ["المعايير غير الناجحة:"] = "Unsuccessful criteria:",
            ["المعلومات الأساسية"] = "Basic information",
            ["المعلومات الشخصية"] = "Personal information",
            ["المعلومات الوظيفية"] = "Functional information",
            ["الملف غير موجود على السيرفر"] = "The file is not found on the server",
            ["المنطقة"] = "Area",
            ["المنطقة المسؤول عنها"] = "The area he is responsible for",
            ["الموافق على الإذن"] = "Approval of permission",
            ["الموافق على المأمورية"] = "Approval of the mission",
            ["الموافقة على الإجازة"] = "Approval of leave",
            ["الموافقة على طلبات السلفة"] = "Approval of advance requests",
            ["الموظف"] = "Employee",
            ["الموظف:"] = "Employee:",
            ["الموظفين"] = "Staff",
            ["الموظفين:"] = "Staff:",
            ["النتيجة النهائية"] = "Final result",
            ["النسبة %"] = "Percentage %",
            ["النسبة الإجمالية:"] = "Total ratio:",
            ["النص"] = "Text",
            ["النوع"] = "Type",
            ["الهاتف"] = "Phone",
            ["الوثائق المتاحة للتوقيع"] = "Documents available for signature",
            ["الوثيقة غير موجودة"] = "The document does not exist",
            ["الوثيقة:"] = "Document:",
            ["الوردية"] = "Rosary",
            ["الوظيفة"] = "Job",
            ["الوظيفة :"] = "Job:",
            ["الوظيفة المرفوعة لها"] = "The function raised to it",
            ["الوظيفة:"] = "Function:",
            ["الى"] = "to",
            ["الى :"] = "to :",
            ["اليوم"] = "today",
            ["انتهاء"] = "an end",
            ["انتهاء البطاقة"] = "Card expires",
            ["انتهاء الشهادة العسكرية"] = "Expiry of military certificate",
            ["انتهاء رخصة القيادة"] = "Expiry of driving license",
            ["انتهاء رخصة المركبة"] = "Expiry of vehicle license",
            ["انثى"] = "feminine",
            ["انصراف"] = "departure",
            ["بانتظار الموافقة"] = "Waiting for approval",
            ["بحث عن الموظف"] = "Search for the employee",
            ["بحث في بيانات الموظفين"] = "Search employee data",
            ["بحث وتصفية طلبات الإجازة"] = "Search and filter leave requests",
            ["بحث وتصفية طلبات الاذن"] = "Search and filter permission requests",
            ["بحث وتصفية طلبات المأموريات"] = "Search and filter errand requests",
            ["بحث وتصفية طلباي"] = "Search and filter my orders",
            ["بحث:"] = "research:",
            ["بدل إدارة"] = "Management allowance",
            ["بدل ادارة"] = "Management allowance",
            ["بدل ادارة:"] = "Management allowance:",
            ["بدل الإدارة"] = "Management allowance",
            ["بدل الانتقال"] = "Relocation allowance",
            ["بدل السكن"] = "Housing allowance",
            ["بدل انتقال"] = "Transfer allowance",
            ["بدل انتقال:"] = "Transfer allowance:",
            ["بدل سكن"] = "Housing allowance",
            ["بدل سكن:"] = "Housing allowance:",
            ["بدل طبيعة العمل"] = "Instead of the nature of the work",
            ["بدل طبيعة عمل"] = "Allowance for the nature of work",
            ["بدل طبيعة عمل:"] = "Nature of work allowance:",
            ["بوجه عام:"] = "In general:",
            ["بيان"] = "statement",
            ["بيانات الإجازة"] = "Leave data",
            ["بيانات الإذن"] = "Permission data",
            ["بيانات الاجازات"] = "Vacation data",
            ["بيانات الادارات"] = "Department data",
            ["بيانات التأخير و الاضافي"] = "Delay and additional data",
            ["بيانات الجروبات"] = "Group data",
            ["بيانات الراحة"] = "Comfort data",
            ["بيانات العاملين"] = "Employee data",
            ["بيانات الفروع"] = "Branch data",
            ["بيانات القطاعات"] = "Sector data",
            ["بيانات المؤهلات"] = "Qualifications data",
            ["بيانات الماكينات"] = "Machine data",
            ["بيانات المسؤولون"] = "Officials' data",
            ["بيانات المناطق"] = "Regions data",
            ["بيانات الموظف"] = "Employee data",
            ["بيانات الورديات"] = "Shift data",
            ["بيانات الوظائف"] = "Job data",
            ["بياناتي الشخصية"] = "My personal data",
            ["ت الشركة"] = "T company",
            ["تأخير"] = "delay",
            ["تأكيد"] = "to be sure",
            ["تأكيد إعادة التعيين"] = "Confirm the reset",
            ["تأكيد الإلغاء"] = "Confirm cancellation",
            ["تأكيد الحذف"] = "Confirm deletion",
            ["تأكيد الرفض"] = "Confirm rejection",
            ["تأكيد الصرف"] = "Confirm exchange",
            ["تأكيد الموافقة"] = "Confirm approval",
            ["تأكيد النقل"] = "Confirm transfer",
            ["تأمين"] = "insurance",
            ["تأمين الشركة:"] = "Company insurance:",
            ["تأمينات الشركة"] = "Company insurances",
            ["تأمينات الموظف"] = "Employee insurances",
            ["تأمينات الموظف:"] = "Employee insurances:",
            ["تاريخ آخر تحميل"] = "Date of last download",
            ["تاريخ الإلغاء"] = "Cancellation date",
            ["تاريخ التسليم"] = "Delivery date",
            ["تاريخ التصدير:"] = "Export date:",
            ["تاريخ التعيين"] = "Appointment date",
            ["تاريخ التعيين:"] = "Appointment date:",
            ["تاريخ السداد المتوقع:"] = "Expected payment date:",
            ["تاريخ الطلب"] = "Order date",
            ["تاريخ الطلب:"] = "Order date:",
            ["تاريخ الموافقة/الرفض"] = "Date of approval/rejection",
            ["تاريخ الميلاد"] = "date of birth",
            ["تاريخ انتهاء العمل"] = "Work end date",
            ["تاريخ بدء العمل"] = "Work start date",
            ["تاريخ نهاية العمل"] = "End date of employment",
            ["تحت التدريب"] = "Under training",
            ["تحت التوظيف"] = "Under employment",
            ["تحت المراجعة"] = "Under review",
            ["تحديث"] = "to update",
            ["تحديث البيانات"] = "Data update",
            ["تحديث النسبة"] = "Update the ratio",
            ["تحديث حالة المهمة"] = "Update task status",
            ["تحديد الكل"] = "Select all",
            ["تحذير"] = "warning",
            ["تحميل الإحصائيات"] = "Download statistics",
            ["تحميل البيانات"] = "Download data",
            ["تحميل الموظفين"] = "Loading staff",
            ["تصدير"] = "export",
            ["تصدير Excel"] = "Excel export",
            ["تصدير إلى Excel"] = "Export to Excel",
            ["تصدير البيانات إلى Excel"] = "Export data to Excel",
            ["تصدير بنفس تنسيق Template الإستيراد"] = "Export in the same format as the import Template",
            ["تصدير بيانات المرتبات"] = "Export payroll data",
            ["تصدير بيانات المرتبات إلى Excel"] = "Export payroll data to Excel",
            ["تصدير تقرير مفصل للقراءة فقط"] = "Export a detailed read-only report",
            ["تصفية"] = "filtering",
            ["تصفية حسب الإدارة"] = "Filter by department",
            ["تصفية حسب الحالة"] = "Filter by status",
            ["تضمين الجميع"] = "Include everyone",
            ["تضمين رأس الجدول"] = "Include table header",
            ["تضمين صندوق الزمالة"] = "Include a fellowship fund",
            ["تطبيق النسبة"] = "Apply the ratio",
            ["تعارض في التواريخ"] = "Conflict in dates",
            ["تعديل البيانات"] = "Modify data",
            ["تعديل التاريخ"] = "Edit date",
            ["تعديل المهمة"] = "Modify the task",
            ["تعديل بيانات"] = "Edit data",
            ["تعديل بيانات الموظفين"] = "Modify employee data",
            ["تعيين إلى"] = "Set to",
            ["تغيير المدير"] = "Change manager",
            ["تغيير الوردية"] = "Change of shift",
            ["تفاصيل الإذن"] = "Permission details",
            ["تفاصيل الحركات:"] = "Movement details:",
            ["تفاصيل الساعة الشهرية"] = "Monthly hourly details",
            ["تفاصيل الساعة اليومية"] = "Daily hour details",
            ["تفاصيل المأمورية"] = "Mission details",
            ["تفاصيل المرتب"] = "Salary details",
            ["تفاصيل طلب الإجازة"] = "Leave request details",
            ["تفصيلي (جميع الحقول)"] = "Detailed (all fields)",
            ["تفعيل/تعطيل"] = "Activate/deactivate",
            ["تقارير"] = "Reports",
            ["تقديم الطلب"] = "Submit the application",
            ["تقرير"] = "a report",
            ["تقرير الإنتاجية"] = "Productivity report",
            ["تقرير الحضور"] = "Attendance report",
            ["تقرير المهام"] = "Task report",
            ["تقرير رصيد الإجازات"] = "Leave balance report",
            ["تقرير ساعات عمل شهري"] = "Monthly working hours report",
            ["تقرير سنوي"] = "Annual report",
            ["تقرير شهري"] = "Monthly report",
            ["تقييمات الموظفين"] = "Employee evaluations",
            ["تكرار التأخير"] = "Repeated delay",
            ["تم"] = "It was completed",
            ["تم الإلغاء بواسطة"] = "Canceled by",
            ["تم التصدير"] = "Exported",
            ["تم الصرف"] = "Disbursed",
            ["تم تحديث المعلومات الشخصية"] = "Personal information has been updated",
            ["تم تقديم طلب الإجازة بنجاح وتحديث سجلات الحضور"] = "The leave request has been successfully submitted and attendance records are updated",
            ["تم تقديم طلب الإجازة بنجاح وهو قيد انتظار الموافقة"] = "Your leave request has been submitted successfully and is awaiting approval",
            ["تمت الموافقة"] = "Approved",
            ["تمت الموافقة بواسطة"] = "Approved by",
            ["تنبيه"] = "alert",
            ["تنسيق التصدير"] = "Export format",
            ["تنسيق تلقائي للأعمدة"] = "Automatic column formatting",
            ["تهيئة النظام"] = "System initialization",
            ["توزيع الموظفين بالإدارات"] = "Distribution of employees among departments",
            ["توقيع"] = "signature",
            ["توقيع الموظف\\n\\n________________\\nالتاريخ: ____/____/____"] = "Employee Signature\\n\\n________________\\nDate: ____/____/____",
            ["توقيع الوثائق"] = "Signing documents",
            ["توقيع الوثيقة"] = "Sign the document",
            ["توقيع رئيس القسم\\n\\n________________\\nالتاريخ: ____/____/____"] = "Signature of Department Head\\n\\n________________\\nDate: ____/____/____",
            ["توقيع مدير الموارد البشرية\\n\\n________________\\nالتاريخ: ____/____/____"] = "Signature of Human Resources Director\\n\\n________________\\nDate: ____/____/____",
            ["توقيع وثائق"] = "Signing documents",
            ["جاري اختبار الاتصال..."] = "Testing connection...",
            ["جاري استيراد بيانات الموظفين..."] = "Importing employee data...",
            ["جاري الاستيراد..."] = "Importing...",
            ["جاري التحميل..."] = "Loading...",
            ["جاري المعالجة"] = "Processing",
            ["جاري تحميل الوثائق..."] = "Loading documents...",
            ["جاري سحب الحركات..."] = "Dragging movements...",
            ["جديد"] = "new",
            ["جزاء"] = "penalty",
            ["جزاء:"] = "penalty:",
            ["جميع الأنواع"] = "All types",
            ["جميع الإدارات"] = "All departments",
            ["جميع الحالات"] = "All cases",
            ["جميع الملفات (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt"] = "All files (*.*)|*.*|PDF files (*.pdf)|*.pdf|Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt",
            ["جميع الملفات المدعومة|"] = "All supported files|",
            ["جميع الوثائق المطلوبة موقعة بالفعل"] = "All required documents are already signed",
            ["حاضر"] = "present",
            ["حاضر اليوم"] = "Present today",
            ["حالة الاتصال:"] = "Connection status:",
            ["حالة الحساب"] = "Account status",
            ["حالة الصرف"] = "Exchange status",
            ["حالة الطلب"] = "Order status",
            ["حالة المهام"] = "Task status",
            ["حالة الموظف:"] = "Employee status:",
            ["حد السلف"] = "The limit of the predecessor",
            ["حد السلف:"] = "Limit of advance:",
            ["حدث خطأ في تحميل الوثائق"] = "An error occurred loading documents",
            ["حذف المعيار"] = "Delete the criterion",
            ["حذف المكرر"] = "Delete duplicate",
            ["حركات الصندوق"] = "Fund movements",
            ["حساب"] = "account",
            ["حساب الإجمالي"] = "Calculate the total",
            ["حساب الكل"] = "Calculate all",
            ["حساب تلقائي"] = "Automatic calculation",
            ["حضور"] = "presence",
            ["حضور القطاع اليوم"] = "Attend the sector today",
            ["حضور و اصراف مانوال"] = "Attendance and payment manual",
            ["حضوري الشهري"] = "My monthly attendance",
            ["حفظ التعديلات"] = "Save modifications",
            ["حفظ الوثيقة"] = "Save the document",
            ["حفظ كـ XPS"] = "Save as XPS",
            ["حفظ كمسودة"] = "Save as draft",
            ["حفظ مسودة"] = "Save a draft",
            ["حفظ ملف Excel"] = "Save the Excel file",
            ["حقل مطلوب"] = "Required field",
            ["خ مبكر"] = "It's early",
            ["خروج مبكر"] = "Early exit",
            ["خطأ في الإدخال"] = "Input error",
            ["خطأ في الاتصال"] = "Communication error",
            ["خطأ في التاريخ"] = "Error in history",
            ["خطأ في الصلاحية"] = "Validity error",
            ["خطأ في القيمة"] = "Value error",
            ["خطأ في الوصول"] = "Access error",
            ["خظأ"] = "Wrong",
            ["خيارات الطباعة"] = "Printing options",
            ["د مبكر"] = "d early",
            ["دخول مبكر"] = "Early entry",
            ["دخول متأخر"] = "Late entry",
            ["دليل الموارد البشرية"] = "Human Resources Guide",
            ["ذكر"] = "male",
            ["ذكور"] = "Males",
            ["راتب أساسي"] = "Basic salary",
            ["راتب ثابت"] = "Fixed salary",
            ["راحة أسبوعية"] = "Weekly rest",
            ["رجوع"] = "Back",
            ["رجوع للفروع"] = "Back to branches",
            ["رجوع للوحة التحكم"] = "Back to the control panel",
            ["رسمي"] = "official",
            ["رصيد الإجازات"] = "Leave balance",
            ["رصيد الاجازات"] = "Leave balance",
            ["رصيد صندوق الزمالة:"] = "Fellowship Fund Balance:",
            ["رفض"] = "to reject",
            ["رفع الوثيقة الموقعة:"] = "Upload the signed document:",
            ["رقم السجل"] = "Registration number",
            ["رقم الطلب:"] = "order number:",
            ["رقم الماكينة"] = "Machine number",
            ["رقم النوع"] = "Type number",
            ["س العمل"] = "o work",
            ["س الفعلية"] = "S actual",
            ["س رسمية"] = "S official",
            ["س فعلية"] = "S is actual",
            ["سائق"] = "driver",
            ["ساعات الحضور"] = "Attendance hours",
            ["ساعات العمل"] = "working hours",
            ["ساعات الفعلية"] = "Actual hours",
            ["سبب الإجازة"] = "Reason for leave",
            ["سبب الإجازة:"] = "Reason for leave:",
            ["سبب الإذن"] = "Reason for permission",
            ["سبب الإلغاء"] = "Reason for cancellation",
            ["سبب السلفة:"] = "Reason for advance:",
            ["سجلي الوظيفي"] = "My employment history",
            ["سحب"] = "to withdraw",
            ["سداد"] = "pay",
            ["سلف"] = "ancestor",
            ["سلف:"] = "ancestor:",
            ["سلفة"] = "advance",
            ["سياسات الشركة"] = "Company policies",
            ["سيتم حفظ الوثيقة الموقعة في أرشيف الموظف"] = "The signed document will be saved in the employee's archive",
            ["شاشة المهام"] = "Tasks screen",
            ["شعار الشركة"] = "Company logo",
            ["ص الزمالة - م اجتماعية"] = "P Fellowship - M Social",
            ["ص الزمالة:"] = "P Fellowship:",
            ["ص طوارئ"] = "r emergency",
            ["صافي الراتب"] = "Net salary",
            ["صافي الراتب:"] = "Net salary:",
            ["صافي الساعات"] = "net hours",
            ["صافي الشهر:"] = "Net month:",
            ["صافي المساهمة"] = "Net contribution",
            ["صباحاً"] = "A.M",
            ["صرف الكل"] = "Spend all",
            ["صرف المحددين"] = "Exchange the specified ones",
            ["صرف جماعي"] = "Collective exchange",
            ["صندوق الزمالة"] = "Fellowship Fund",
            ["صورة"] = "image",
            ["صورة الموظف"] = "Employee photo",
            ["ض كسب عمل"] = "Z earn a job",
            ["ض كسب عمل:"] = "Z gain work:",
            ["ض. كسب عمل"] = "Z. Gain a job",
            ["ضريبة"] = "tax",
            ["ضريبة كسب العمل"] = "Employment gain tax",
            ["طارئ"] = "emergency",
            ["طباعة كشف الرصيد"] = "Print balance statement",
            ["طباعة مباشرة على الطابعة"] = "Print directly on the printer",
            ["طلب"] = "to request",
            ["طلب إذن"] = "Request permission",
            ["طلب جديد"] = "New order",
            ["طلب سلفة من صندوق الزمالة"] = "Request an advance from the Fellowship Fund",
            ["طلبات إجازة"] = "Leave requests",
            ["طلبات الإجازة"] = "Leave requests",
            ["طلبات الإجازة المعلقة"] = "Pending leave requests",
            ["طلبات الموظفين"] = "Employee requests",
            ["طلباتي"] = "My requests",
            ["ع الساعات"] = "On the hours",
            ["ع ايام"] = "For days",
            ["ع ساعات"] = "On hours",
            ["ع ساعات فعلية"] = "Real hours",
            ["عجز"] = "inability",
            ["عجز:"] = "inability:",
            ["عدد الأشهر"] = "Number of months",
            ["عدد الأشهر:"] = "Number of months:",
            ["عدد الحركات"] = "Number of movements",
            ["عدد الحركات:"] = "Number of movements:",
            ["عدد الدقائق"] = "Number of minutes",
            ["عدد السجلات"] = "Number of records",
            ["عدد المعايير:"] = "Number of standards:",
            ["عدد الموظفين"] = "Number of employees",
            ["عدد الموظفين بدون رصيد:"] = "Number of employees without balance:",
            ["عدد الموظفين:"] = "Number of employees:",
            ["عرض"] = "an offer",
            ["عرض البيانات"] = "Display data",
            ["عرض التفاصيل"] = "View details",
            ["عرض التقرير"] = "View report",
            ["عرض جميع الحركات"] = "View all movements",
            ["عضو"] = "member",
            ["عقود تنتهي قريباً"] = "Contracts expiring soon",
            ["عمولات تحقيق"] = "Realization commissions",
            ["عمولات خارجية"] = "External commissions",
            ["عمولة تحقيق"] = "Investigation commission",
            ["عمولة خارجية"] = "External commission",
            ["غائب"] = "absent",
            ["غائب اليوم"] = "Absent today",
            ["غياب"] = "absence",
            ["غير قادر على تحديد المسار"] = "Unable to specify path",
            ["غير متاح"] = "Not available",
            ["غير متصل ?"] = "Offline ?",
            ["غير متصل بالمسار المركزي - استخدام المسار المحلي"] = "Not connected to the central path - use the local path",
            ["غير متوفر"] = "unavailable",
            ["غير محدد"] = "undefined",
            ["غير مسموح بالسلفة"] = "Advance is not allowed",
            ["غير معروف"] = "unknown",
            ["غير موفق"] = "Unsuccessful",
            ["غير نشط"] = "Inactive",
            ["فاتورة تليفون"] = "Telephone bill",
            ["فاتورة تليقون:"] = "Taiqon invoice:",
            ["فتح الملف"] = "Open the file",
            ["فرع الانصراف"] = "Departure branch",
            ["فرع الحضور"] = "Attendance branch",
            ["فروع"] = "Branches",
            ["فريق العمل"] = "work team",
            ["فلترة حسب التصنيف"] = "Filter by category",
            ["فلترة حسب الوظيفة"] = "Filter by job",
            ["فني"] = "technical",
            ["في الخدمة"] = "on duty",
            ["قائمة الوظائف"] = "List of jobs",
            ["قطاع"] = "sector",
            ["قطاعات"] = "Sectors",
            ["قواعد السلوك"] = "Code of conduct",
            ["قيد الانتظار"] = "On hold",
            ["قيد التطوير"] = "Under development",
            ["قيد التنفيذ"] = "Under implementation",
            ["قيمة اضافي:"] = "Additional value:",
            ["قيمة الأضافي"] = "Additional value",
            ["قيمة التأخير"] = "Delay value",
            ["قيمة التأخير:"] = "Delay value:",
            ["قيمة الساعة شهري"] = "Hourly value is monthly",
            ["قيمة الساعة يومي"] = "Hourly value is daily",
            ["قيمة الغياب:"] = "Absence value:",
            ["قيمة دقائق"] = "Minutes value",
            ["قيمة مالية"] = "Monetary value",
            ["كشف المرتب"] = "Payroll",
            ["كشف حساب الموظفين"] = "Employee account statement",
            ["كشف حساب صندوق الزمالة"] = "Fellowship Fund Account Statement",
            ["كشف حساب صندوق الزمالة المشترك"] = "Joint Fellowship Fund Account Statement",
            ["كود الفرع"] = "Branch code",
            ["كود الموظف"] = "Employee code",
            ["كود النوع *"] = "Type code *",
            ["كود: جاري التحميل..."] = "Code: Loading...",
            ["لا"] = "no",
            ["لا توجد أيام إجازة"] = "There are no days off",
            ["لا توجد رسائل"] = "No messages",
            ["لا توجد رسائل بعد"] = "No messages yet",
            ["لا توجد ملاحظات"] = "There are no notes",
            ["لا توجد نتائج"] = "No results found",
            ["لا توجد وثائق متاحة للتوقيع"] = "There are no documents available to sign",
            ["لا توجد وثائق متاحة للتوقيع - جميع الوثائق موقعة"] = "There are no documents available to sign - all documents are signed",
            ["لا شيء"] = "nothing",
            ["لا يخصم من الرصيد"] = "It is not deducted from the balance",
            ["لا يوجد"] = "nothing",
            ["لا يوجد حد"] = "There is no limit",
            ["لا يوجد سبب"] = "There is no reason",
            ["لا يوجد عنوان IP محلي"] = "No local IP address",
            ["لا يوجد مديرين متاحين"] = "There are no managers available",
            ["لا يوجد وصف"] = "There is no description",
            ["لم تتم الموافقة بعد"] = "Not approved yet",
            ["لم يتم اختيار موظف"] = "No employee has been selected",
            ["لم يسجل"] = "Did not register",
            ["لم يصرف"] = "Not spent",
            ["لوحة تحكم المدير"] = "Administrator control panel",
            ["لوحة تحكم الموظف"] = "Employee control panel",
            ["لوحة تحكم شؤون الموظفين"] = "Personnel control panel",
            ["م"] = "M",
            ["مؤرشفة"] = "Archived",
            ["مؤمن عليه"] = "insured",
            ["مبلغ السلفة"] = "Advance amount",
            ["مبلغ السلفة:"] = "Advance amount:",
            ["متأخر"] = "late",
            ["متأكد"] = "sure",
            ["متصل ?"] = "Online?",
            ["متصل الآن"] = "Online now",
            ["متوسط الراتب"] = "Average salary",
            ["متوسط النسبة %"] = "average ratio %",
            ["محادثة جديدة"] = "New conversation",
            ["مدير"] = "boss",
            ["مدير_النظام"] = "system_admin",
            ["مرتبات"] = "Salaries",
            ["مرتبات مستحقة"] = "Salaries owed",
            ["مرسلة"] = "Sent",
            ["مرفوض"] = "unacceptable",
            ["مرفوضة"] = "Rejected",
            ["مسؤولو المنطقة"] = "District officials",
            ["مسائاً"] = "Evening",
            ["مسار السيرفر المركزي:"] = "Central server path:",
            ["مسار محلي"] = "Local path",
            ["مستحقات أخرى"] = "Other dues",
            ["مستحقاتي"] = "My dues",
            ["مستخدم"] = "user",
            ["مستخدم اندرويد"] = "Android user",
            ["مستلمة"] = "Received",
            ["مستنداتي"] = "My documents",
            ["مستوى الأداء: جيد"] = "Performance level: good",
            ["مسح جميع الفلاتر"] = "Clear all filters",
            ["مسح فلتر الوظيفة"] = "Clear job filter",
            ["مسموح بالسلفة"] = "Advance is allowed",
            ["مسودة"] = "draft",
            ["مشاركة اجتماعي"] = "Social sharing",
            ["مشاركة اجتماعية"] = "Social sharing",
            ["مشاركة اجتماعية:"] = "Social sharing:",
            ["مطلوب"] = "required",
            ["معاينة المستند"] = "Document preview",
            ["معاينة صرف المرتبات"] = "Inspection of salary disbursement",
            ["معاينة قبل الطباعة"] = "Preview before printing",
            ["معد التقرير: ___________________"] = "Report prepared by: ___________________",
            ["معدل الإنتاجية"] = "Productivity rate",
            ["معدل الحضور اليومي منخفض"] = "Low daily attendance rate",
            ["معطّل"] = "Disabled",
            ["معفي الاضافي"] = "Additional exempt",
            ["معفي تأخير"] = "Delay exempt",
            ["معفي خ مبكر"] = "Exempt early",
            ["معفي د مبكر"] = "Exempt d early",
            ["معفي من إضافي بعد اوقات العمل"] = "Exempt from additional fees after working hours",
            ["معفي من إضافي قبل اوقات العمل"] = "Exempt from additional fees before working hours",
            ["معفي من تأخير"] = "Exempt from delay",
            ["معفي من خروج مبكر"] = "Exempt from early exit",
            ["معفي من غياب"] = "Exempt from absence",
            ["معلقة"] = "suspended",
            ["معلومات"] = "information",
            ["معلومات إضافية"] = "Additional information",
            ["معلومات الإجازة"] = "Leave information",
            ["معلومات الدوام"] = "Working hours information",
            ["معلومات الرصيد"] = "Balance information",
            ["معلومات السلفة"] = "Advance information",
            ["معلومات الصندوق"] = "Fund information",
            ["معلومات القيادة"] = "Driving information",
            ["معلومات الموظف"] = "Employee information",
            ["معلومات: اضغط مرتين على الصف لفتح بيانات الموظف للتعديل"] = "Information: Double click on the row to open the employee data for editing",
            ["معلومة"] = "Information",
            ["مغادرين (شهرياً)"] = "Departures (monthly)",
            ["مكافآت"] = "Rewards",
            ["مكافآت:"] = "Rewards:",
            ["مكافأة"] = "reward",
            ["مكتملة"] = "Complete",
            ["ملاحظات"] = "comments",
            ["ملاحظات (اختياري)"] = "Notes (optional)",
            ["ملاحظات عامة:"] = "General notes:",
            ["ملاحظات:"] = "comments:",
            ["ملاحظة: النوع الأول مناسب إذا كنت تريد تعديل البيانات وإعادة استيرادها.\\nالنوع الثاني مناسب للعرض والطباعة فقط."] = "Note: The first type is suitable if you want to modify and re-import the data.\\nThe second type is suitable for display and printing only.",
            ["ملاحظة: هذا الحقل إلزامي لملفات وصف الوظيفة"] = "Note: This field is mandatory for job description files",
            ["ملاحظة: يجب اختيار الموافق على الإجازة إذا كانت تتطلب موافقة"] = "Note: You must select Approval for the leave if it requires approval",
            ["ملخص"] = "summary",
            ["ملخص (الحقول الأساسية فقط)"] = "Summary (basic fields only)",
            ["ملخص الإجماليات:"] = "Summary of totals:",
            ["ملخص التقييم الإداري"] = "Summary of administrative evaluation",
            ["ملخص التقييم الفني"] = "Technical evaluation summary",
            ["ملخص السلفة"] = "Summary of advance",
            ["ملخص رصيد الإجازات"] = "Vacation balance summary",
            ["ملغى"] = "Canceled",
            ["ملف PDF فارغ"] = "Blank PDF file",
            ["ملفات Excel|*.xlsx"] = "Excel|*.xlsx files",
            ["من"] = "from",
            ["من :"] = "from :",
            ["من تاريخ"] = "From date",
            ["من تاريخ:"] = "From date:",
            ["منتهية"] = "Finished",
            ["مهام مستحقة"] = "Due assignments",
            ["مهمة"] = "a task",
            ["مهمة جديدة"] = "New mission",
            ["موافق عليه"] = "approved",
            ["موافقة"] = "consent",
            ["موظفو الفرع"] = "Branch employees",
            ["موظفين"] = "employees",
            ["موظفين جدد (شهرياً)"] = "New employees (monthly)",
            ["موعد التسليم"] = "Delivery time",
            ["موفق"] = "Good luck",
            ["موقع الانصراف"] = "Check-out site",
            ["موقع الحضور"] = "Attendance site",
            ["موقعة"] = "Signed",
            ["نتيجة الإدخال"] = "Input result",
            ["نتيجة التحميل"] = "Loading result",
            ["نجاح"] = "success",
            ["نجح"] = "He succeeded",
            ["نسبة الخصم من الراتب (%):"] = "Salary deduction percentage (%):",
            ["نسبة المساهمة"] = "Contribution percentage",
            ["نسبة الموظفين بدون رصيد:"] = "Percentage of employees without balance:",
            ["نسبة صندوق الزمالة (%):"] = "Fellowship Fund Percentage (%):",
            ["نشط"] = "active",
            ["نشطة"] = "Active",
            ["نطاق العمل"] = "Scope of work",
            ["نظام العمل"] = "Work system",
            ["نظام العمل:"] = "Work system:",
            ["نعم"] = "Yes",
            ["نقل الملفات الحالية"] = "Transfer existing files",
            ["نقل الملفات من المسار المحلي إلى المسار المركزي"] = "Move files from local path to central path",
            ["نوع إجازة"] = "Leave type",
            ["نوع إجازة جديد"] = "New leave type",
            ["نوع الإجازة"] = "Leave type",
            ["نوع الإجازة المختار:"] = "Selected type of leave:",
            ["نوع الإذن:"] = "Permission type:",
            ["نوع الاذن"] = "Ear type",
            ["نوع التغيير"] = "Type of change",
            ["نوع التقييم"] = "Evaluation type",
            ["نوع الحركة:"] = "Movement type:",
            ["نوع الراتب"] = "Salary type",
            ["هل أنت متأكد من مراجعة البيانات قبل المعالجة ؟"] = "Are you sure to review the data before processing?",
            ["هل انت متأكد من حذف هذه البصمة ؟"] = "Are you sure to delete this fingerprint?",
            ["هل تريد سحب البيانات ؟"] = "Do you want to pull data?",
            ["هناك إجازات متعارضة في الفترة المحددة:\\n"] = "There are conflicting vacations in the specified period:\\n",
            ["وثائق التدريب"] = "Training documents",
            ["وثائق التعيين"] = "Appointment documents",
            ["وثائق موقعة"] = "Signed documents",
            ["وثائق موقعه"] = "His site documents",
            ["وثاق العمل"] = "Work documents",
            ["وثيقة"] = "document",
            ["وثيقة أخرى"] = "Another document",
            ["وردية مسائية"] = "Evening shift",
            ["وصف الوظيفة"] = "Job description",
            ["وقت البداية"] = "Start time",
            ["وقت النهاية"] = "End time",
            ["يتطلب موافقة"] = "Requires approval",
            ["يتطلب موافقة المسؤول"] = "Requires administrator approval",
            ["يتطلب موافقة:"] = "Requires approval:",
            ["يتم تحميل البيانات..."] = "Data is being loaded...",
            ["يخصم من الرصيد"] = "Deducted from the balance",
            ["— مسؤولون عن جميع فروع المنطقة"] = "— Responsible for all branches of the region",
            ["⚙️ الإدارة"] = "⚙️ Management",
            ["⚙️ تقييم فني"] = "⚙️ Technical evaluation",
            ["✅ موفق"] = "✅ Good luck",
            ["✍️ توقيع"] = "✍️ Signature",
            ["✏️ تعديل"] = "✏️ Edit",
            ["✏️ تعديل رسالة..."] = "✏️ Edit message...",
            ["✕ إلغاء"] = "✕ Cancel",
            ["✨ إضافة محادثة جديدة"] = "✨ Add a new conversation",
            ["❌ إغلاق"] = "❌ Close",
            ["❌ إلغاء"] = "❌ Cancel",
            ["❌ غير موفق"] = "❌ Not successful",
            ["➕ إضافة"] = "➕ Addition",
            ["＋ اظهار كامل"] = "＋Full show",
            ["－ اظهار جزئي"] = "－ Show partial",
            ["👁️ معاينة"] = "👁️ Preview",
            ["👥 إدارة الموظفين"] = "👥 Employee management",
            ["👥 مجموعات"] = "👥 Collections",
            ["💬 محادثات"] = "💬 Conversations",
            ["💰 المستحقات والمستقطعات"] = "💰 Dues and deductions",
            ["💾 حفظ"] = "💾 Save",
            ["💾 حفظ التقييم"] = "💾 Save rating",
            ["📁 اختر الملف"] = "📁 Select the file",
            ["📄 المستندات: DOC, DOCX, XLS, XLSX, PPT, PPTX"] = "📄 Documents: DOC, DOCX, XLS, XLSX, PPT, PPTX",
            ["📊 التقارير"] = "📊 Reports",
            ["📊 تصدير إلى Excel"] = "📊 Export to Excel",
            ["📊 تقاريري"] = "📊 My reports",
            ["📋 أخرى: PDF, TXT"] = "📋 Other: PDF, TXT",
            ["📋 تقييم إداري"] = "📋 Administrative evaluation",
            ["📋 قائمة الموكلة إليّ"] = "📋 List entrusted to me",
            ["📤 إلغاء الأرشفة"] = "📤 Cancel archiving",
            ["📤 قائمة المهام التي قمت بتكليفها"] = "📤 List of tasks you have assigned",
            ["📦 أرشفة المحادثة"] = "📦 Archive the conversation",
            ["📦 أرشيف"] = "📦 Archive",
            ["📷 الصور: JPG, JPEG, PNG, BMP, GIF, TIFF"] = "📷 Images: JPG, JPEG, PNG, BMP, GIF, TIFF",
            ["🔄 آخر التغييرات على الموظفين"] = "🔄 Latest changes to employees",
            ["🖨️ طباعة"] = "🖨️ Print",
            ["🗑️ حذف"] = "🗑️ Delete",
            ["🛠️ الخدمات الذاتية"] = "🛠️ Self-services",

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

