using ClosedXML.Excel;
using HR_Application.Dashboard;
using HR_Application.Helpers;
using HR_Application.Services;
using HR_Application.Views;
using HR_Application.Views.Employees;
using HR_Application.Views.Employees.Holidays;
using HR_Application.Views.Salaries;
using HR_Application.Views.Settings;
using MahApps.Metro.IconPacks;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Services;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using static FastReport.Export.Html.HTMLExport;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using MahApps.Metro.IconPacks;
using HR_Application.Views.Conversations;

namespace HR_Application
{
    public partial class MainWindow : Window
    {
        private bool _isSettingVisible;
        private bool _isReportVisible;
        private bool _isMachineVisible;
        private bool _isPersonnelVisible;
        private bool _isHolidayManagementVisible;
        private bool _isErrandsVisible;
        private bool _isLoanManagementVisible;
        private readonly Sho2on.Database.AppDbContext _context = new Sho2on.Database.AppDbContext(App.ConnectionString);
        private List<string> UserPerm = App.userPermissions;
        private ExcelReaderService _excelReader;
        private CommissionProcessorService _commissionProcessor;

        private int _totalUnreadCount = 0;
        public MainWindow()
        {
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
            DataContext = this;

            _excelReader = new ExcelReaderService();
            _commissionProcessor = new CommissionProcessorService(_context);
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            await SetupGlobalNotifications();
            await RefreshUnreadBadge();
        }

        private async Task SetupGlobalNotifications()
        {
            var manager = SignalRManager.Instance;

            // ReceiveMessage — always active regardless of which window is open
            manager.OnMessageReceived += async (fromUserId, toUserId, message, timestamp) =>
            {
                if (toUserId != App.CurrentUser.Id) return;

                // Is the ChatWindow open AND showing this user?
                bool chatIsOpen = IsChatWindowOpenFor(fromUserId);

                if (!chatIsOpen)
                {
                    // Show popup notification
                    var sender = await GetUserNameAsync(fromUserId);
                    Helpers.NotificationsHelper.ShowPopupNotification(
                        $"رسالة من {sender}",
                        message.Length > 60 ? message[..60] + "..." : message,
                        this,
                        () => OpenChatWith(fromUserId)
                    );
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }
            };

            // UnreadCount badge on the chat nav button
            manager.OnUnreadCountChanged += async (userId) =>
            {
                if (userId == App.CurrentUser.Id)
                    await RefreshUnreadBadge();
            };

            // Task notifications
            manager.OnTaskNotification += (notificationType, taskId, fromUserId, desc, ts) =>
            {
                if (fromUserId == App.CurrentUser.Id) return;

                string title = notificationType switch
                {
                    "NewTask" => "مهمة جديدة",
                    "TaskStatusChanged" => "تحديث حالة مهمة",
                    "TaskDeleted" => "تم حذف مهمة",
                    _ => "إشعار مهمة"
                };

                Helpers.NotificationsHelper.ShowPopupNotification(
                    title, desc, this,
                    () => OpenTasksWindow()
                );
                Helpers.NotificationsHelper.PlayNotificationSound();
            };
        }

        private async Task RefreshUnreadBadge()
        {
            _totalUnreadCount = await SignalRManager.Instance
                .GetTotalUnreadAsync(App.CurrentUser.Id);

            // Update your badge UI element — غيّر "ChatBadge" لاسم الـ element بتاعك
            ChatBadge.Visibility = _totalUnreadCount > 0
                ? Visibility.Visible : Visibility.Collapsed;
            ChatBadgeText.Text = _totalUnreadCount > 99
                ? "99+" : (_totalUnreadCount - 1).ToString();
        }

        private bool IsChatWindowOpenFor(int userId)
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is ChatWindow cw
                    && cw.IsVisible
                    && cw.ChatBoxControl.SelectedUserId == userId)
                    return true;
            }
            return false;
        }

        private async Task<string> GetUserNameAsync(int userId)
        {
            try
            {
                using var ctx = new AppDbContext(App.ConnectionString);
                var user = await ctx.Users.FindAsync(userId);
                return user?.FullName ?? "مستخدم";
            }
            catch { return "مستخدم"; }
        }

        private void OpenChatWith(int userId)
        {
            // افتح أو انتقل لـ ConversationsWindow مع هذا اليوزر
            // غيّر حسب structure الـ navigation بتاعتك
        }

        private void OpenTasksWindow()
        {
            var w = new Views.Conversations.TasksWindow();
            w.Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            var result = MessageBox.Show("هل تريد الخروج من البرنامج ؟", "خروج", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }
        public bool ReportP => UserPerm.Contains("التقارير");
        public bool EmploP => UserPerm.Contains("شئون العاملين");
        public bool AttendanceP => UserPerm.Contains("الحضور و الانصراف");
        public bool MachineP => UserPerm.Contains("FingerPrints");
        public bool SettingsP => UserPerm.Contains("Settings");
        public bool EmploDataP => UserPerm.Contains("بيانات العاملين");
        public bool EmploSalaryDataP => UserPerm.Contains("بيانات و مرتبات الموظفين");
        public bool HoliReqP => true;
        public bool BulkSalaryPaymentP => UserPerm.Contains("صرف المرتبات الجماعي");
        public bool LoanRequestP => UserPerm.Contains("طلب سلفة");
        public bool LoanManagementP => UserPerm.Contains("ادارة السلف");
        public bool LoanApproveP => UserPerm.Contains("الموافقة على السلفات");
        public bool FriendBoxReportP => UserPerm.Contains("كشف حساب ص الزمالة");
        public bool uploadEmployeesP => App.CurrentUser.FullName == "OR" || App.CurrentUser.Code == "0";
        public bool HolidaysManagementP => UserPerm.Contains("ادارة الاجازات");
        public bool EmploMonthP => UserPerm.Contains("شهري العاملين");
        public bool DataEditP => UserPerm.Contains("مراجعة الحركات");
        public bool UploadDataP => UserPerm.Contains("تحميل الداتا");
        public bool DownloadDataP => UserPerm.Contains("تنزيل الداتا");
        public bool ManualP => UserPerm.Contains("Manual");
        public bool ErrandP => UserPerm.Contains("إجراءات");
        public bool MonthlyP => UserPerm.Contains("الحركات الشهرية");
        public bool AddMachineP => UserPerm.Contains("إضافة");
        public bool GetDataP => UserPerm.Contains("تحميل البيانات");
        public bool BranchP => UserPerm.Contains("الفروع");
        public bool RoleP => UserPerm.Contains("الجروبات");
        public bool DepartP => UserPerm.Contains("الادارات");
        public bool QualificationP => UserPerm.Contains("المؤهلات");
        public bool JobP => UserPerm.Contains("الوظائف");
        public bool DegreeP => UserPerm.Contains("القطاعات");
        public bool AreaP => UserPerm.Contains("المناطق");
        public bool AddEmploP => UserPerm.Contains("اضافة موظف");
        public bool AllDatasP => UserPerm.Contains("اضافة البيانات");
        public bool AllPermissionP => UserPerm.Contains("الصلاحيات");
        public bool AllSettingsP => UserPerm.Contains("الاعدادات العامة");
        public bool ShiftP => UserPerm.Contains("الورديات");
        public bool BreakP => UserPerm.Contains("الراحات");
        public bool LateP => UserPerm.Contains("التأخيرات");
        public bool WHP => UserPerm.Contains("الاجازات الاسبوعية");
        public bool HTypeP => UserPerm.Contains("أنواع الاجازات");
        public bool EmpEvalP => UserPerm.Contains("تقييم موظف");
        public bool PermissionP => UserPerm.Contains("صلاحيات البرنامج");
        public bool MonthSettingP => UserPerm.Contains("اعدادات الشهر");
        public bool UserPermissionP => UserPerm.Contains("صلاحيات المستخدم");
        public bool BranchPermissionP => UserPerm.Contains("صلاحيات الفروع");
        public bool Backup => UserPerm.Contains("Backup");
        public bool AddMainSalaryP => UserPerm.Contains("ادارة ماليات موظف");
        public bool SalaryP => UserPerm.Contains("الاجور والمرتبات");
        public bool AddDeductionsP => UserPerm.Contains("استحقاقت و استقطاعات");
        public bool SalaryReportP => UserPerm.Contains("تقرير المرتبات");
        public bool ArchiveP => UserPerm.Contains("الارشيف");
        public bool ManageBalanceP => UserPerm.Contains("إدارة الرصيد");
        public bool HoliRequestsP => UserPerm.Contains("طلبات الاجازة");
        public bool NewRequestP => UserPerm.Contains("طلب إجازة");
        public bool ManageLeaveTypesP => UserPerm.Contains("أنواع الإجازات");
        public bool BalanceReportP => UserPerm.Contains("تقرير الرصيد");
        public bool NewMissionP => UserPerm.Contains("طلب مأمورية");
        public bool ManageMissionP => UserPerm.Contains("طلبات المأموريات");
        public bool StorageP => true;

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            // Update icon & label to reflect NEW state
            if (ThemeManager.IsDark)
            {
                ThemeModeIcon.Kind = PackIconMaterialKind.WeatherSunny;
            }
            else
            {
                ThemeModeIcon.Kind = PackIconMaterialKind.WeatherNight;
            }
        }

        private async Task LoadGIFAsync()
        {


            // Perform any heavy loading asynchronously
            await Task.Run(() =>
            {
                // Simulate a time-consuming operation
                Thread.Sleep(1000);
            });

            // Update UI on the UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                /*var gifUri = new Uri("pack://application:,,,/assets/images/exit.gif");
                var gifImage = new BitmapImage(gifUri);
                ImageBehavior.SetAnimatedSource(GIFImage, gifImage);*/
                var gifUri1 = new Uri("pack://application:,,,/assets/images/Back.gif");
                var gifImage1 = new BitmapImage(gifUri1);
                ImageBehavior.SetAnimatedSource(GIFBack, gifImage1);
                var gifUri2 = new Uri("pack://application:,,,/assets/images/icon.gif");
                var gifImage2 = new BitmapImage(gifUri2);
                ImageBehavior.SetAnimatedSource(GIFIcon, gifImage2);

            });


        }


        private void btnManageBalance_Click(object sender, RoutedEventArgs e)
        {
            var manageBalanceWindow = new ManageLeaveBalanceWindow();
            manageBalanceWindow.ShowDialog();
        }


        private void btnNewRequest_Click(object sender, RoutedEventArgs e)
        {
            var holidayRequestWindow = new HolidayRequestWindow();
            holidayRequestWindow.Show();
        }


        private void OpenEvaluation_Click(object sender, RoutedEventArgs e)
        {
            var evaluationWindow = new EmployeeEvaluationWindow();
            evaluationWindow.Show();
        }


        private void btnBalanceReport_Click(object sender, RoutedEventArgs e)
        {
            var balanceReportWindow = new LeaveBalanceReportWindow();
            balanceReportWindow.ShowDialog();
        }

        private void btnManageLeaveTypes_Click(object sender, RoutedEventArgs e)
        {
            var leaveTypesWindow = new LeaveTypesManagementWindow();
            leaveTypesWindow.ShowDialog();
        }
        private void InitializeFlags()
        {
            _isSettingVisible = false;
            _isReportVisible = false;
            _isMachineVisible = false;
            _isPersonnelVisible = false;

        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
            if (e.ClickCount == 2)
            {
                ToggleWindowState();
            }
        }

        private void ToggleWindowState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
        private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Max_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

        private void AddBranchOpen(object sender, RoutedEventArgs e)
        {
            OpenWindow<AddBranch>();
        }

        private void AddRoleOpen(object sender, RoutedEventArgs e) => OpenWindow<AddRole>();

        private void AddAttendOpen(object sender, RoutedEventArgs e) => OpenWindow<AddAttendRecord>();

        private void SettingsOpen(object sender, RoutedEventArgs e) => OpenWindow<Settings>();

        private void PermissionsOpen(object sender, RoutedEventArgs e) => OpenWindow<Permissions>();
        private void BulkSalaryPaymentOpen(object sender, RoutedEventArgs e) => OpenWindow<BulkSalaryPaymentWindow>();

        private void UserPermissionsOpen(object sender, RoutedEventArgs e) => OpenWindow<UserPermission>();

        private void BranchPermissionsOpen(object sender, RoutedEventArgs e) => OpenWindow<UserBranches>();

        private void AddEmploOpen(object sender, RoutedEventArgs e) => OpenWindow<AddEmplo>();

        private void EmployeeDataOpen(object sender, RoutedEventArgs e) => OpenWindow<EmployeeData>();
        private void EmployeeMonthOpen(object sender, RoutedEventArgs e) => OpenWindow<EmployeeMonthReport>();

        private void AddShiftOpen(object sender, RoutedEventArgs e) => OpenWindow<AddShift>();
        private void AddAreaOpen(object sender, RoutedEventArgs e) => OpenWindow<AddArea>();

        private void MonthlyDataOpen(object sender, RoutedEventArgs e) => OpenWindow<MonthlyData>();
        private void MonthlySalaryDataOpen(object sender, RoutedEventArgs e) => OpenWindow<MonthlySalaryData>();

        private void DataEditOpen(object sender, RoutedEventArgs e) => OpenWindow<DataEdit>();

        private void AddHoliOpen(object sender, RoutedEventArgs e) => OpenWindow<AddWeekHoli>();

        private void AddMachineOpen(object sender, RoutedEventArgs e) => OpenWindow<MachineWindow>();

        private void GetDataClicked(object sender, RoutedEventArgs e) => OpenWindow<DataLoad>();

        private void AddDepartOpen(object sender, RoutedEventArgs e) => OpenWindow<AddDepart>();
        private void AddQualificationOpen(object sender, RoutedEventArgs e) => OpenWindow<AddQualification>();

        private void AddJobOpen(object sender, RoutedEventArgs e) => OpenWindow<AddJob>();
        private void AddMainSalary(object sender, RoutedEventArgs e) => OpenWindow<MainSalaryWindow>();
        private void AddDeductions(object sender, RoutedEventArgs e) => OpenWindow<BenefitsDeductions>();
        private void SalaryReport(object sender, RoutedEventArgs e) => OpenWindow<SalaryReport>();
        private void ErrandsOpen(object sender, RoutedEventArgs e)
        {
            ErrandsWindow window = new ErrandsWindow();
            window.Show();
        }
        private void ErrandsRequestsOpen(object sender, RoutedEventArgs e)
        {
            MissionsRequestWindow window = new MissionsRequestWindow();
            window.Show();
        }

        private void AddBreakOpen(object sender, RoutedEventArgs e) => OpenWindow<AddBreak>();
        private void EmployeeSalaryDataOpen(object sender, RoutedEventArgs e) => OpenWindow<EmployeeSalaryData>();
        private void HoliReqOpen(object sender, RoutedEventArgs e) => OpenWindow<HolidayRequestWindow>();
        private void ArchiveOpen(object sender, RoutedEventArgs e) => OpenWindow<CompanyDocumentsWindow>();
        private void StorageOpen(object sender, RoutedEventArgs e) => OpenWindow<NetworkSettingsWindow>();

        private void AddLateOpen(object sender, RoutedEventArgs e) => OpenWindow<AddLate>();

        private void AddJobDegreeOpen(object sender, RoutedEventArgs e) => OpenWindow<AddJobDegree>();

        private void AddHoliTypeOpen(object sender, RoutedEventArgs e) => OpenWindow<LeaveTypesManagementWindow>();
        private void HolidaysManagementOpen(object sender, RoutedEventArgs e) => OpenWindow<LeaveManagementWindow>();

        private void OpenWindow<T>() where T : Window, new()
        {
            var window = new T();
            window.Activate();
            window.Show();
        }

        private void BtnGenerateTemplate_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExcelTemplateHelper.GenerateImportTemplate();
            if (!string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show($"تم إنشاء القالب بنجاح: {filePath}");

                // فتح الملف في Excel
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"تم إنشاء القالب ولكن حدث خطأ في فتحه: {ex.Message}");
                }
            }
        }

        private async void upload_data_btn_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
        "تحذير: سيتم استيراد البيانات من ملف Excel.\n" +
        "البيانات المكررة سيتم تحديثها.\n" +
        "هل تريد المتابعة؟",
        "تأكيد الاستيراد",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await ImportFromExcel(App.ConnectionString);
            }
        }

        private async void download_data_btn_ButtonClicked(object sender, RoutedEventArgs e)
        {
            await ExportAttendanceData(App.ConnectionString);
        }

        public static async Task ExportAttendanceData(string connectionString)
        {
            // عرض نافذة اختيار نوع التصدير
            var dialog = new ExportTypeDialog();
            dialog.ShowDialog();

            if (dialog.ExportType == ExportType.None)
                return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "Export Attendance Data"
            };

            // تعيين اسم الملف الافتراضي بناءً على نوع التصدير
            if (dialog.ExportType == ExportType.ForImport)
            {
                saveFileDialog.FileName = $"Attendance_Data_ForImport_{DateTime.Now:yyyyMMdd}.xlsx";
            }
            else
            {
                saveFileDialog.FileName = $"Attendance_Detailed_Report_{DateTime.Now:yyyyMMdd}.xlsx";
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var context = new AppDbContext(connectionString))
                    {
                        var attendanceData = await context.Attendances
                            .Include(a => a.User)
                            .Include(a => a.Shift)
                            .Include(a => a.CheckInBranch)
                            .Include(a => a.CheckOutBranch)
                            .Include(a => a.Leave)
                            .ThenInclude(a => a.LeaveType)
                            .OrderBy(a => a.AttendanceDate)
                            .ThenBy(a => a.UserId)
                            .ToListAsync();

                        if (!attendanceData.Any())
                        {
                            MessageBox.Show("لا توجد بيانات للتصدير");
                            return;
                        }

                        using (var workbook = new XLWorkbook())
                        {
                            if (dialog.ExportType == ExportType.ForImport)
                            {
                                // التصدير بنفس تنسيق Template الإستيراد
                                ExportForImport(workbook, attendanceData);
                            }
                            else
                            {
                                // التصدير بتقرير مفصل
                                ExportDetailedReport(workbook, attendanceData);
                            }

                            workbook.SaveAs(saveFileDialog.FileName);

                            string message = $"تم التصدير بنجاح إلى: {saveFileDialog.FileName}";

                            if (dialog.ExportType == ExportType.ForImport)
                            {
                                message += "\n\nملاحظة: تم تصدير البيانات بنفس تنسيق Template الإستيراد.";
                                message += "\nيمكنك تعديل هذا الملف ثم إعادة استيراده.";
                            }

                            MessageBox.Show(message, "تم التصدير");

                            // فتح الملف تلقائياً
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveFileDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التصدير: {ex.Message}");
                }
            }
        }

        private static void ExportForImport(XLWorkbook workbook, List<Attendance> attendanceData)
        {
            var worksheet = workbook.Worksheets.Add("AttendanceData");

            // كتابة العناوين من Template
            for (int i = 0; i < ExcelTemplateHelper.TemplateHeaders.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = ExcelTemplateHelper.TemplateHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // إضافة البيانات بنفس التنسيق
            int row = 2;
            foreach (var attendance in attendanceData)
            {
                // UserId
                worksheet.Cell(row, 1).Value = attendance.UserId;

                // AttendanceDate
                worksheet.Cell(row, 2).Value = attendance.AttendanceDate.ToString("yyyy-MM-dd");

                // CheckInTime
                if (attendance.CheckInTime.HasValue)
                    worksheet.Cell(row, 3).Value = attendance.CheckInTime.Value.ToString("HH:mm:ss");

                // CheckOutTime
                if (attendance.CheckOutTime.HasValue)
                    worksheet.Cell(row, 4).Value = attendance.CheckOutTime.Value.ToString("HH:mm:ss");

                // ShiftId
                worksheet.Cell(row, 5).Value = attendance.ShiftId;

                // CheckInBranchId
                worksheet.Cell(row, 6).Value = attendance.CheckInBranchId;

                // CheckOutBranchId
                worksheet.Cell(row, 7).Value = attendance.CheckOutBranchId;

                // Locations
                worksheet.Cell(row, 8).Value = attendance.CheckInLocation;
                worksheet.Cell(row, 9).Value = attendance.CheckOutLocation;

                // Boolean fields
                worksheet.Cell(row, 10).Value = attendance.ExemptLate;
                worksheet.Cell(row, 11).Value = attendance.ExemptEarlyLeave;
                worksheet.Cell(row, 12).Value = attendance.ExemptOvertime;
                worksheet.Cell(row, 13).Value = attendance.ExemptEarlyEnter;
                worksheet.Cell(row, 14).Value = attendance.IsHoliday;
                worksheet.Cell(row, 15).Value = attendance.IsAbsence;

                // Time fields
                if (attendance.Late.HasValue)
                    worksheet.Cell(row, 16).Value = attendance.Late.Value.ToString(@"hh\:mm\:ss");

                if (attendance.EarlyLeave.HasValue)
                    worksheet.Cell(row, 17).Value = attendance.EarlyLeave.Value.ToString(@"hh\:mm\:ss");

                if (attendance.EarlyEnter.HasValue)
                    worksheet.Cell(row, 18).Value = attendance.EarlyEnter.Value.ToString(@"hh\:mm\:ss");

                if (attendance.Overtime.HasValue)
                    worksheet.Cell(row, 19).Value = attendance.Overtime.Value.ToString(@"hh\:mm\:ss");

                if (attendance.TotalWorkHours.HasValue)
                    worksheet.Cell(row, 20).Value = attendance.TotalWorkHours.Value.ToString(@"hh\:mm\:ss");

                // Coordinates
                worksheet.Cell(row, 21).Value = attendance.CheckInLatitude;
                worksheet.Cell(row, 22).Value = attendance.CheckInLongitude;
                worksheet.Cell(row, 23).Value = attendance.CheckOutLatitude;
                worksheet.Cell(row, 24).Value = attendance.CheckOutLongitude;

                row++;
            }

            // ضبط عرض الأعمدة
            worksheet.Columns().AdjustToContents();

            // إضافة تعليمات في ورقة منفصلة
            var instructionsSheet = workbook.Worksheets.Add("تعليمات");

            instructionsSheet.Cell(1, 1).Value = "تعليمات استخدام الملف";
            instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
            instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

            var instructions = new[]
            {
        "هذا الملف تم تصديره بنفس تنسيق Template الإستيراد",
        "",
        "لإعادة استيراد البيانات بعد التعديل:",
        "1. يمكنك تعديل أي حقل في ورقة AttendanceData",
        "2. احتفظ بنفس أسماء الأعمدة",
        "3. احتفظ بنفس التنسيق للتواريخ والأوقات",
        "4. عند استيراد الملف بعد التعديل:",
        "   - السجلات المكررة (بنفس UserId و AttendanceDate) سيتم تحديثها",
        "   - السجلات الجديدة سيتم إضافتها",
        "   - السجلات المحذوفة من الملف لن تحذف من قاعدة البيانات",
        "",
        "تنسيقات البيانات:",
        "- التواريخ: yyyy-MM-dd (مثال: 2024-12-30)",
        "- الأوقات: HH:mm:ss (مثال: 08:30:00)",
        "- الحقول المنطقية: true أو false",
        "- الحقول الرقمية: أرقام فقط",
        "",
        "ملاحظة: تأكد من صحة الـ IDs (UserId, ShiftId, BranchId)",
        "قبل الاستيراد"
    };

            for (int i = 0; i < instructions.Length; i++)
            {
                instructionsSheet.Cell(i + 3, 1).Value = instructions[i];
            }

            instructionsSheet.Columns().AdjustToContents();
        }

        private static void ExportDetailedReport(XLWorkbook workbook, List<Attendance> attendanceData)
        {
            var worksheet = workbook.Worksheets.Add("تقرير تفصيلي");

            // عناوين التقرير المفصل
            var detailedHeaders = new[]
            {
        "ID",
        "كود الموظف",
        "اسم الموظف",
        "التاريخ",
        "وقت الحضور",
        "وقت الانصراف",
        "الوردية",
        "وقت الدوام الرسمي",
        "وقت الانتهاء الرسمي",
        "التأخير",
        "الانصراف المبكر",
        "الحضور المبكر",
        "وقت إضافي",
        "ساعات العمل",
        "فرع الحضور",
        "فرع الانصراف",
        "موقع الحضور",
        "موقع الانصراف",
        "معفي من التأخير",
        "معفي من الانصراف المبكر",
        "معفي من الوقت الإضافي",
        "إجازة",
        "غياب",
        "نوع الإجازة",
        "خط العرض (حضور)",
        "خط الطول (حضور)",
        "خط العرض (انصراف)",
        "خط الطول (انصراف)"
    };

            // كتابة العناوين
            for (int i = 0; i < detailedHeaders.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = detailedHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // إضافة البيانات
            int row = 2;
            foreach (var attendance in attendanceData)
            {
                worksheet.Cell(row, 1).Value = attendance.Id;
                worksheet.Cell(row, 2).Value = attendance.UserId;
                worksheet.Cell(row, 3).Value = attendance.User?.FullName ?? "غير محدد";
                worksheet.Cell(row, 4).Value = attendance.AttendanceDate.ToString("yyyy-MM-dd");

                if (attendance.CheckInTime.HasValue)
                    worksheet.Cell(row, 5).Value = attendance.CheckInTime.Value.ToString("HH:mm");

                if (attendance.CheckOutTime.HasValue)
                    worksheet.Cell(row, 6).Value = attendance.CheckOutTime.Value.ToString("HH:mm");

                worksheet.Cell(row, 7).Value = attendance.Shift?.Name ?? "غير محدد";
                worksheet.Cell(row, 8).Value = attendance.Shift?.StartTime.ToString(@"hh\:mm") ?? "";
                worksheet.Cell(row, 9).Value = attendance.Shift?.EndTime.ToString(@"hh\:mm") ?? "";

                if (attendance.Late.HasValue)
                    worksheet.Cell(row, 10).Value = attendance.Late.Value.ToString(@"hh\:mm");

                if (attendance.EarlyLeave.HasValue)
                    worksheet.Cell(row, 11).Value = attendance.EarlyLeave.Value.ToString(@"hh\:mm");

                if (attendance.EarlyEnter.HasValue)
                    worksheet.Cell(row, 12).Value = attendance.EarlyEnter.Value.ToString(@"hh\:mm");

                if (attendance.Overtime.HasValue)
                    worksheet.Cell(row, 13).Value = attendance.Overtime.Value.ToString(@"hh\:mm");

                if (attendance.TotalWorkHours.HasValue)
                    worksheet.Cell(row, 14).Value = attendance.TotalWorkHours.Value.ToString(@"hh\:mm");

                worksheet.Cell(row, 15).Value = attendance.CheckInBranch?.Name ?? "غير محدد";
                worksheet.Cell(row, 16).Value = attendance.CheckOutBranch?.Name ?? "غير محدد";
                worksheet.Cell(row, 17).Value = attendance.CheckInLocation ?? "";
                worksheet.Cell(row, 18).Value = attendance.CheckOutLocation ?? "";
                worksheet.Cell(row, 19).Value = attendance.ExemptLate ? "نعم" : "لا";
                worksheet.Cell(row, 20).Value = attendance.ExemptEarlyLeave ? "نعم" : "لا";
                worksheet.Cell(row, 21).Value = attendance.ExemptOvertime ? "نعم" : "لا";
                worksheet.Cell(row, 22).Value = attendance.IsHoliday ? "نعم" : "لا";
                worksheet.Cell(row, 23).Value = attendance.IsAbsence ? "نعم" : "لا";
                worksheet.Cell(row, 24).Value = attendance.Leave?.LeaveType?.Name ?? "";

                worksheet.Cell(row, 25).Value = attendance.CheckInLatitude;
                worksheet.Cell(row, 26).Value = attendance.CheckInLongitude;
                worksheet.Cell(row, 27).Value = attendance.CheckOutLatitude;
                worksheet.Cell(row, 28).Value = attendance.CheckOutLongitude;

                row++;
            }

            worksheet.Columns().AdjustToContents();

        }

        public async Task ImportFromExcel(string connectionString)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls",
                Title = "Select Excel File to Import"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // التحقق من التنسيق
                    if (!IsValidTemplateFormat(openFileDialog.FileName))
                    {
                        var result = MessageBox.Show(
                            "يبدو أن هذا الملف ليس بنفس تنسيق Template الإستيراد.\n" +
                            "هل تريد المتابعة على أي حال؟",
                            "تحذير",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    var progressWindow = new ProgressDialog();
                    progressWindow.Show();

                    progressWindow.UpdateStatus("جاري رفع الحركات...");

                    await ImportFileAsync(openFileDialog.FileName, connectionString, progressWindow);

                    progressWindow.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الاستيراد: {ex.Message}");
                }
            }
        }

        private static bool IsValidTemplateFormat(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet("AttendanceData");
                    if (worksheet == null) return false;

                    var firstRow = worksheet.FirstRowUsed();
                    if (firstRow == null) return false;

                    // التحقق من عدد الأعمدة
                    int columnCount = firstRow.CellsUsed().Count();
                    if (columnCount != ExcelTemplateHelper.TemplateHeaders.Count)
                        return false;

                    // التحقق من بعض العناوين الرئيسية
                    var headers = new List<string>();
                    foreach (var cell in firstRow.CellsUsed())
                    {
                        headers.Add(cell.GetString().ToLower());
                    }

                    // التحقق من وجود عناوين أساسية
                    var requiredHeaders = new[] { "userid", "attendancedate" };
                    foreach (var required in requiredHeaders)
                    {
                        if (!headers.Any(h => h.Contains(required)))
                            return false;
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task ImportFileAsync(string filePath, string connectionString, ProgressDialog progressWindow)
        {
            using (var context = new AppDbContext(connectionString))
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet("AttendanceData");
                    if (worksheet == null)
                    {
                        MessageBox.Show("لم يتم العثور على ورقة باسم 'AttendanceData'");
                        return;
                    }

                    var rows = worksheet.RowsUsed().Skip(1);
                    int totalRows = rows.Count();
                    int processed = 0;
                    int imported = 0;
                    int updated = 0;
                    int errors = 0;

                    List<string> errorMessages = new List<string>();

                    foreach (var row in rows)
                    {
                        try
                        {
                            processed++;
                            progressWindow.UpdateStatus($"معالجة سطر {processed} من {totalRows}");

                            var userId = row.Cell(1).GetValue<int?>();
                            var dateStr = row.Cell(2).GetString();

                            if (!userId.HasValue || string.IsNullOrWhiteSpace(dateStr))
                            {
                                errors++;
                                errorMessages.Add($"سطر {row.RowNumber()}: بيانات ناقصة");
                                continue;
                            }

                            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var attendanceDate))
                            {
                                errors++;
                                errorMessages.Add($"سطر {row.RowNumber()}: تنسيق تاريخ غير صحيح");
                                continue;
                            }

                            // البحث عن سجل موجود
                            var existingAttendance = await context.Attendances
                                .FirstOrDefaultAsync(a =>
                                    a.UserId == userId.Value &&
                                    a.AttendanceDate.Date == attendanceDate.Date);

                            var attendance = existingAttendance ?? new Attendance
                            {
                                UserId = userId.Value,
                                AttendanceDate = attendanceDate
                            };

                            // تحديث البيانات من Excel
                            UpdateAttendanceFromExcel(row, attendance);

                            // التحقق من الصحة
                            var validation = await ValidateAttendance(context, attendance);
                            if (!validation.IsValid)
                            {
                                errors++;
                                errorMessages.Add($"سطر {row.RowNumber()}: {validation.ErrorMessage}");
                                continue;
                            }

                            if (existingAttendance == null)
                            {
                                context.Attendances.Add(attendance);
                                imported++;
                            }
                            else
                            {
                                updated++;
                            }

                            // حفظ كل 100 سجل
                            if (processed % 100 == 0)
                            {
                                await context.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            errorMessages.Add($"سطر {row.RowNumber()}: {ex.Message}");
                        }
                    }

                    // حفظ الباقي
                    await context.SaveChangesAsync();

                    // عرض النتائج
                    ShowImportResults(imported, errorMessages);
                }
            }
        }

        private static void UpdateAttendanceFromExcel(IXLRow row, Attendance attendance)
        {
            // CheckInTime
            var checkInTimeStr = row.Cell(3).GetString();
            if (!string.IsNullOrWhiteSpace(checkInTimeStr) &&
                TimeSpan.TryParse(checkInTimeStr, out var checkInTime))
            {
                attendance.CheckInTime = attendance.AttendanceDate.Date.Add(checkInTime);
            }

            // CheckOutTime
            var checkOutTimeStr = row.Cell(4).GetString();
            if (!string.IsNullOrWhiteSpace(checkOutTimeStr) &&
                TimeSpan.TryParse(checkOutTimeStr, out var checkOutTime))
            {
                attendance.CheckOutTime = attendance.AttendanceDate.Date.Add(checkOutTime);
            }

            // IDs
            attendance.ShiftId = row.Cell(5).GetValue<int?>();
            attendance.CheckInBranchId = row.Cell(6).GetValue<int?>();
            attendance.CheckOutBranchId = row.Cell(7).GetValue<int?>();

            // Locations
            attendance.CheckInLocation = row.Cell(8).GetString();
            attendance.CheckOutLocation = row.Cell(9).GetString();

            // Boolean fields
            attendance.ExemptLate = GetBoolValue(row.Cell(10).GetString());
            attendance.ExemptEarlyLeave = GetBoolValue(row.Cell(11).GetString());
            attendance.ExemptOvertime = GetBoolValue(row.Cell(12).GetString());
            attendance.ExemptEarlyEnter = GetBoolValue(row.Cell(13).GetString());
            attendance.IsHoliday = GetBoolValue(row.Cell(14).GetString());
            attendance.IsAbsence = GetBoolValue(row.Cell(15).GetString());

            // Time fields
            attendance.Late = ParseTimeSpan(row.Cell(16).GetString());
            attendance.EarlyLeave = ParseTimeSpan(row.Cell(17).GetString());
            attendance.EarlyEnter = ParseTimeSpan(row.Cell(18).GetString());
            attendance.Overtime = ParseTimeSpan(row.Cell(19).GetString());
            attendance.TotalWorkHours = ParseTimeSpan(row.Cell(20).GetString());

            // Coordinates
            attendance.CheckInLatitude = row.Cell(21).GetValue<double?>();
            attendance.CheckInLongitude = row.Cell(22).GetValue<double?>();
            attendance.CheckOutLatitude = row.Cell(23).GetValue<double?>();
            attendance.CheckOutLongitude = row.Cell(24).GetValue<double?>();
        }

        private static TimeSpan? ParseTimeSpan(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (TimeSpan.TryParse(value, out var result))
                return result;

            // محاولة تحويل تنسيق hh:mm:ss إذا كان فيه شرطة
            if (value.Contains(':'))
            {
                var parts = value.Split(':');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes))
                {
                    int seconds = parts.Length > 2 && int.TryParse(parts[2], out int s) ? s : 0;
                    return new TimeSpan(hours, minutes, seconds);
                }
            }

            return null;
        }

        private static bool GetBoolValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            value = value.Trim().ToLower();
            return value == "true" || value == "yes" || value == "نعم" || value == "1";
        }

        private static async Task<(bool IsValid, string ErrorMessage)> ValidateAttendance(
            AppDbContext context, Attendance attendance)
        {
            // التحقق من وجود المستخدم
            var userExists = await context.Users.AnyAsync(u => u.Id == attendance.UserId);
            if (!userExists)
            {
                return (false, $"المستخدم برقم {attendance.UserId} غير موجود");
            }

            // التحقق من وجود Shift إذا تم تحديده
            if (attendance.ShiftId.HasValue)
            {
                var shiftExists = await context.Shifts.AnyAsync(s => s.Id == attendance.ShiftId.Value);
                if (!shiftExists)
                {
                    return (false, $"الوردية برقم {attendance.ShiftId} غير موجودة");
                }
            }

            // التحقق من وجود الفروع
            if (attendance.CheckInBranchId.HasValue)
            {
                var branchExists = await context.Branches.AnyAsync(b => b.Id == attendance.CheckInBranchId.Value);
                if (!branchExists)
                {
                    return (false, $"الفرع برقم {attendance.CheckInBranchId} غير موجود");
                }
            }

            if (attendance.CheckOutBranchId.HasValue)
            {
                var branchExists = await context.Branches.AnyAsync(b => b.Id == attendance.CheckOutBranchId.Value);
                if (!branchExists)
                {
                    return (false, $"الفرع برقم {attendance.CheckOutBranchId} غير موجود");
                }
            }

            // التحقق من التواريخ
            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                if (attendance.CheckOutTime.Value < attendance.CheckInTime.Value)
                {
                    return (false, "وقت الانصراف لا يمكن أن يكون قبل وقت الحضور");
                }
            }

            return (true, string.Empty);
        }

        bool IsDrawer = false;

        private void menu_btn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsDrawer)
            {
                SideBar.Width = 210;

            }
        }

        private void menu_btn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsDrawer)
            {
                SideBar.Width = 100;

            }

        }

        private void menu_btn_Click(object sender, RoutedEventArgs e)
        {
            StackPanel[] panels = { ReportPanel, EmployeePanel, FingerPrintsPanel, SettingsPanel, SalaryPanel };
            var grid = Content as Grid;
            if (IsDrawer)
            {
                SideBar.Width = 100;
                IsDrawer = false;
                foreach (var group in panels)
                {

                    var collapseStoryboard = (Storyboard)FindResource("CollapseAnimation");
                    collapseStoryboard.Completed += (s, a) => group.Visibility = Visibility.Collapsed;
                    group.BeginStoryboard(collapseStoryboard);

                }
                HideOtherDrawers(grid);

            }
            else
            {
                IsDrawer = true;
                SideBar.Width = 210;
            }
        }



        // Hide other panels except the current one
        private void HideOtherPanels(StackPanel? excludeGroup, StackPanel[] panels)
        {
            foreach (var group in panels)
            {
                if (group != excludeGroup && group.Visibility == Visibility.Visible)
                {
                    var collapseStoryboard = (Storyboard)FindResource("CollapseAnimation");
                    collapseStoryboard.Completed += (s, a) => group.Visibility = Visibility.Collapsed;
                    group.BeginStoryboard(collapseStoryboard);
                }
            }
        }

        // Drawer Control Logic
        private void HandleDrawerAnimation(string showStoryboardName, string hideStoryboardName, ref bool isVisible)
        {
            var grid = Content as Grid;
            if (grid == null) return;

            var storyboardToShow = (Storyboard)grid.Resources[showStoryboardName];
            var storyboardToHide = (Storyboard)grid.Resources[hideStoryboardName];

            if (isVisible)
            {
                storyboardToHide?.Begin();
            }
            else
            {
                HideOtherDrawers(grid);
                storyboardToShow?.Begin();
            }

            isVisible = !isVisible;
        }

        private void HideOtherDrawers(Grid? grid = null)
        {
            var hideStoryboards = new[] { "HideDrawer1","HideDrawer4","HideDrawer3", "HideDrawer2", "HideDrawer5", "HideDrawer" };
            foreach (var storyboardName in hideStoryboards)
            {
                var storyboard = (Storyboard)grid.Resources[storyboardName];
                storyboard?.Begin();
            }
        }

        private void ToggleGroup_Click(object sender, StackPanel[] panels)
        {
            SideBar.Width = 210;
            var button = sender as System.Windows.Controls.Button;
            var targetGroupName = button?.Tag as string;

            if (targetGroupName == null) return;

            // Find the currently selected group
            StackPanel currentlyVisibleGroup = null;

            foreach (var group in panels)
            {
                if (group.Name == targetGroupName)
                {
                    currentlyVisibleGroup = group;
                    break;
                }
            }

            if (currentlyVisibleGroup == null) return;

            if (currentlyVisibleGroup.Visibility == Visibility.Visible)
            {
                // Collapse the currently visible group
                var collapseStoryboard = (Storyboard)FindResource("CollapseAnimation");
                collapseStoryboard.Completed += (s, a) => currentlyVisibleGroup.Visibility = Visibility.Collapsed;
                currentlyVisibleGroup.BeginStoryboard(collapseStoryboard);
            }
            else
            {
                HideOtherPanels(currentlyVisibleGroup, panels);

                currentlyVisibleGroup.Visibility = Visibility.Visible;
                // Hide other panels and expand the selected one
                var expandStoryboard = (Storyboard)FindResource("ExpandAnimation");
                currentlyVisibleGroup.BeginStoryboard(expandStoryboard);
            }

            // Toggle the visibility flag
        }

        // Button click event handlers
        private void Panel_btn_Click(object sender, RoutedEventArgs e)
        {
            var grid = Content as Grid;

            HideOtherDrawers(grid);
            StackPanel[] panels = { ReportPanel, EmployeePanel, FingerPrintsPanel, SettingsPanel, SalaryPanel, AttendancePanel };
            ToggleGroup_Click(sender, panels);

        }


        private void Datas_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            HandleDrawerAnimation("ShowDrawer", "HideDrawer", ref _isReportVisible);
        }
        private void Permission_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

            HandleDrawerAnimation("ShowDrawer1", "HideDrawer1", ref _isPersonnelVisible);
        }
        private void MainSettings_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

            HandleDrawerAnimation("ShowDrawer2", "HideDrawer2", ref _isSettingVisible);
        }
        private void HolidayManagement_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

            HandleDrawerAnimation("ShowDrawer3", "HideDrawer3", ref _isHolidayManagementVisible);
        }
        private void Errands_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {

            HandleDrawerAnimation("ShowDrawer5", "HideDrawer5", ref _isErrandsVisible);
        }
        private void LoanManagementOpen(object sender, RoutedEventArgs e)
        {

            HandleDrawerAnimation("ShowDrawer4", "HideDrawer4", ref _isLoanManagementVisible);
        }

        private void logout_btn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();

        }
        public static async Task CreateBackup(string connectionString)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Backup files (*.bak)|*.bak",
                FileName = $"Attendance_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
                Title = "Create Database Backup"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // إنشاء نسخة احتياطية مباشرة من SQL Server
                    string backupQuery = $@"
                BACKUP DATABASE [{GetDatabaseName(connectionString)}] 
                TO DISK = '{saveFileDialog.FileName}' 
                WITH FORMAT, MEDIANAME = 'AttendanceBackup', 
                NAME = 'Full Backup of Attendance Database';";

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = new SqlCommand(backupQuery, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    MessageBox.Show($"تم إنشاء نسخة احتياطية بنجاح: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}");
                }
            }
        }

        private static string GetDatabaseName(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog;
        }

        private async void Backup_Button_Click(object sender, RoutedEventArgs e)
        {
            await CreateBackup(App.ConnectionString);

        }

        private async void btnImportCommissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // فتح ملف Excel
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                    Title = "اختر ملف Excel للعمولات"
                };

                if (openFileDialog.ShowDialog() != true)
                    return;

                // قراءة البيانات من Excel
                var commissionData = _excelReader.ReadCommissionExcel(openFileDialog.FileName);

                if (commissionData.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على بيانات صالحة في الملف", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // معالجة البيانات
                var (salaries, errors) = _commissionProcessor.ProcessCommissions(commissionData);

                // عرض النتائج للمستخدم
                ShowImportResults(salaries.Count, errors);

                if (salaries.Count > 0)
                {
                    // حفظ في قاعدة البيانات
                    await SaveSalariesToDatabase(salaries);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استيراد البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowImportResults(int successCount, List<string> errors)
        {
            string message = $"تم معالجة {successCount} عمولة بنجاح";

            if (errors.Count > 0)
            {
                message += $"\n\nالأخطاء ({errors.Count}):\n" + string.Join("\n", errors.Take(10));

                if (errors.Count > 10)
                    message += $"\n...و {errors.Count - 10} خطأ آخر";
            }

            MessageBox.Show(message, "نتيجة الاستيراد",
                MessageBoxButton.OK,
                errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private async System.Threading.Tasks.Task SaveSalariesToDatabase(List<Salary> salaries)
        {
            try
            {
                _context.Salaries.AddRange(salaries);
                await _context.SaveChangesAsync();

                MessageBox.Show($"تم حفظ {salaries.Count} عمولة في قاعدة البيانات بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void uploadEmployeesBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "ملفات Excel|*.xls;*.xlsx",
                Title = "اختر ملف Excel للموظفين"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            // إنشاء ونشر نافذة التحميل
            var progressDialog = new ProgressDialog
            {
                Owner = this
            };

            // متغير لتتبع الإلغاء
            bool isCancelled = false;

            // عرض نافذة التحميل
            progressDialog.Show();

            try
            {
                // تحديث حالة التحميل
                progressDialog.UpdateStatus("جاري قراءة ملف Excel...");

                // إنشاء Service للاستيراد مع callback للتقدم
                var userImporter = new UserImportService(_context, (current, total, status) =>
                {
                    if (isCancelled) return false;

                    Dispatcher.Invoke(() =>
                    {
                        progressDialog.SetProgress(current, total);
                        progressDialog.UpdateStatus(status);
                    });

                    return true;
                });

                // التحقق من إلغاء العملية
                progressDialog.Closing += (s, args) =>
                {
                    if (progressDialog.IsCancelled)
                    {
                        isCancelled = true;
                        userImporter.CancelImport();
                    }
                };

                // بدء الاستيراد (في thread منفصل)
                int importedCount = 0;
                var importTask = Task.Run(async () =>
                {
                    try
                    {
                        importedCount = await userImporter.ImportUsersFromExcelAsync(openFileDialog.FileName);
                        return importedCount;
                    }
                    catch (OperationCanceledException)
                    {
                        return -1; // تم الإلغاء
                    }
                    catch (Exception ex)
                    {
                        throw; // رمي الاستثناء للتعامل معه في الخارج
                    }
                });

                // الانتظار حتى انتهاء المهمة
                var result = await importTask;

                // إغلاق نافذة التحميل
                Dispatcher.Invoke(() => progressDialog.Close());

                // عرض النتائج
                if (isCancelled)
                {
                    MessageBox.Show("تم إلغاء عملية الاستيراد", "إلغاء",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result == -1)
                {
                    MessageBox.Show("تم إلغاء العملية", "إلغاء",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result > 0)
                {
                    MessageBox.Show($"تم استيراد {result} موظف بنجاح!", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("لم يتم استيراد أي موظفين", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    progressDialog.Close();
                    MessageBox.Show($"خطأ في استيراد الموظفين: {ex.Message}",
                        "خطأ في الاستيراد",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void btnLoanRequest_Click(object sender, RoutedEventArgs e)
        {
            var window = new LoanRequestWindow();
            window.Show();
        }

        private void btnLoanApprove_Click(object sender, RoutedEventArgs e)
        {
            var window = new LoanApprovalWindow();
            window.Show();
        }

        private void btnFriendBoxReport_Click(object sender, RoutedEventArgs e)
        {
            var window = new FriendshipBoxStatementWindow();
            window.Show();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.Initialize();
            InitializeFlags();
            LoadGIFAsync();
            DashboardManager dashboardManager = new DashboardManager();
            dashboardControl.Children.Add(dashboardManager.GetDashboardWindow());
            UpdateThemeButton();
            if (Properties.Settings.Default.Logo != null)
                GIFBack.Source = new BitmapImage(new Uri(Properties.Settings.Default.Logo));
            WelcomeText.Text = $"مرحباً بك، {App.CurrentUser.FullName}";


        }

        private void UpdateThemeButton()
        {
            if (ThemeManager.IsDark)
            {
                ThemeModeIcon.Kind = PackIconMaterialKind.WeatherSunny;
            }
            else
            {
                ThemeModeIcon.Kind = PackIconMaterialKind.WeatherNight;
            }
        }

        private void btnPermissionRequests_Click(object sender, RoutedEventArgs e)
        {
            new PermissionManagementWindow().Show();
        }

        private void btnPermissionNewRequest_Click(object sender, RoutedEventArgs e)
        {
            new PermissionRequestWindow().Show();
        }

        private void DashboardDisplay_Click(object sender, RoutedEventArgs e)
        {
            if (dashboardControl.Visibility == Visibility.Visible)
            {
                GIFBack.Visibility = Visibility.Visible;
                dashboardControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                GIFBack.Visibility = Visibility.Collapsed;
                dashboardControl.Visibility = Visibility.Visible;
            }
        }

        private void TasksBtn_Click(object sender, RoutedEventArgs e)
        {
            TasksWindow tasksWindow = new TasksWindow();
            tasksWindow.Show();
        }

        private void ChatsBtn_Click(object sender, RoutedEventArgs e)
        {
            ChatWindow chatWindow = new ChatWindow(App.CurrentUser);
            chatWindow.Show();
        }

    }
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
