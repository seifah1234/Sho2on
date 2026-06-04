using ClosedXML.Excel;
using HR_Application.Dashboard;
using HR_Application.Helpers;
using HR_Application.Services;
using HR_Application.Views;
using HR_Application.Views.Conversations;
using HR_Application.Views.Employees;
using HR_Application.Views.Employees.Holidays;
using HR_Application.Views.Salaries;
using HR_Application.Views.Settings;
using MahApps.Metro.IconPacks;
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
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;
using static FastReport.Export.Html.HTMLExport;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace HR_Application
{
    public partial class MainWindow : Window, INotifyPropertyChanged
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
        private DispatcherTimer _timer;
        private string _currentDateTime;

        protected virtual void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int _totalUnreadCount = 0;

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set { _currentDateTime = value; OnPropertyChanged(nameof(CurrentDateTime)); }
        }

        public MainWindow()
        {
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

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) =>
                CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _timer.Start();
        }
        private async Task SetupGlobalNotifications()
        {
            var manager = SignalRManager.Instance;

            // ReceiveMessage ó always active regardless of which window is open
            manager.OnMessageReceived += async (fromUserId, toUserId, message, timestamp) =>
            {
                if (toUserId != App.CurrentUser.Id) return;

                bool chatIsOpen = IsChatWindowOpenFor(fromUserId);

                if (!chatIsOpen)
                {
                    var sender = await GetUserNameAsync(fromUserId);
                    Helpers.NotificationsHelper.ShowPopupNotification(
                        $"—”«·… „‰ {sender}",
                        message.Length > 60 ? message[..60] + "..." : message,
                        this,
                        () => OpenChatWith(fromUserId)
                    );
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }

                // FIX BUG #2: Always refresh badge
                await RefreshUnreadBadge();
            };

            // FIX BUG #2: Subscribe to unread count changes
            manager.OnUnreadCountChanged += async (userId) =>
            {
                if (userId == App.CurrentUser.Id)
                    await RefreshUnreadBadge();
            };

            manager.OnGroupMessageReceived += async (groupId, senderId, message, timestamp, senderName) =>
            {
                if (senderId == App.CurrentUser.Id) return;

                bool groupIsOpen = IsChatWindowGroupOpenFor(groupId);
                if (!groupIsOpen)
                {
                    using var ctx = new AppDbContext(App.ConnectionString);
                    var group = await ctx.ChatGroups.FindAsync(groupId);

                    var shortMsg = string.IsNullOrEmpty(message) ? "?? „—›ﬁ"
                        : (message.Length > 60 ? message[..60] + "..." : message);

                    Helpers.NotificationsHelper.ShowPopupNotification(
                        $"{group?.Name ?? "Ã—Ê»"}: {senderName}",
                        shortMsg, this, null
                    );
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }

                // Always refresh badge for group messages
                await RefreshUnreadBadge();
            };

            // Task notifications
            manager.OnTaskNotification += (notificationType, taskId, fromUserId, desc, ts) =>
            {
                if (fromUserId == App.CurrentUser.Id) return;

                string title = notificationType switch
                {
                    "NewTask" => "„Â„… ÃœÌœ…",
                    "TaskStatusChanged" => " ÕœÌÀ Õ«·… „Â„…",
                    "TaskDeleted" => " „ Õ–› „Â„…",
                    _ => "≈‘⁄«— „Â„…"
                };

                Helpers.NotificationsHelper.ShowPopupNotification(
                    title, desc, this,
                    () => OpenTasksWindow()
                );
                Helpers.NotificationsHelper.PlayNotificationSound();
            };

            // FIX BUG #2: Also listen for group message edits/deletes to refresh badge
            manager.OnGroupMessageDeleted += async (messageId, groupId) =>
            {
                await RefreshUnreadBadge();
            };
        }

        private bool IsChatWindowGroupOpenFor(int groupId)
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is ChatWindow cw
                    && cw.IsVisible
                    && cw.GroupChatBoxControl.SelectedGroupId == groupId
                    && cw.GroupChatBoxControl.IsVisible)
                    return true;
            }
            return false;
        }

        // FIX BUG #2: Refresh badge including group unread counts
        private async Task RefreshUnreadBadge()
        {
            try
            {
                _totalUnreadCount = await SignalRManager.Instance
                    .GetTotalUnreadAsync(App.CurrentUser.Id);

                // Update badge UI on the main thread
                await Dispatcher.InvokeAsync(() =>
                {
                    ChatBadge.Visibility = _totalUnreadCount > 0
                        ? Visibility.Visible : Visibility.Collapsed;
                    ChatBadgeText.Text = _totalUnreadCount > 99
                        ? "99+" : _totalUnreadCount.ToString();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshUnreadBadge error: {ex.Message}");
            }
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
                return user?.FullName ?? "„” Œœ„";
            }
            catch { return "„” Œœ„"; }
        }

        private void OpenChatWith(int userId)
        {
            var chatWindow = new ChatWindow();
            chatWindow.Show();
            // Open chat with specific user
            chatWindow.OpenSpecificChat(userId);
        }

        private void OpenTasksWindow()
        {
            var w = new Views.Conversations.TasksWindow();
            w.Show();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            var result = LocalizationManager.ShowMessage("Â·  —Ìœ «·Œ—ÊÃ „‰ «·»—‰«„Ã ø", "Œ—ÊÃ", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else
            {
                e.Cancel = true;
            }
        }
    
        public bool ReportP => UserPerm.Contains("«· ﬁ«—Ì—");
        public bool EmploP => UserPerm.Contains("‘∆Ê‰ «·⁄«„·Ì‰");
        public bool AttendanceP => UserPerm.Contains("«·Õ÷Ê— Ê «·«‰’—«›");
        public bool MachineP => UserPerm.Contains("FingerPrints");
        public bool SettingsP => UserPerm.Contains("Settings");
        public bool EmploDataP => UserPerm.Contains("»Ì«‰«  «·⁄«„·Ì‰");
        public bool EmploSalaryDataP => UserPerm.Contains("»Ì«‰«  Ê „— »«  «·„ÊŸ›Ì‰");
        public bool HoliReqP => true;
        public bool BulkSalaryPaymentP => UserPerm.Contains("’—› «·„— »«  «·Ã„«⁄Ì");
        public bool LoanRequestP => UserPerm.Contains("ÿ·» ”·›…");
        public bool LoanManagementP => UserPerm.Contains("«œ«—… «·”·›");
        public bool LoanApproveP => UserPerm.Contains("«·„Ê«›ﬁ… ⁄·Ï «·”·›« ");
        public bool FriendBoxReportP => UserPerm.Contains("ﬂ‘› Õ”«» ’ «·“„«·…");
        public bool uploadEmployeesP => (App.CurrentUser.JobTitle.IsManager.HasValue && App.CurrentUser.JobTitle.IsManager.Value) || App.CurrentUser.Username == "OR";
        public bool HolidaysManagementP => UserPerm.Contains("«œ«—… «·«Ã«“« ");
        public bool EmploMonthP => UserPerm.Contains("‘Â—Ì «·⁄«„·Ì‰");
        public bool DataEditP => UserPerm.Contains("„—«Ã⁄… «·Õ—ﬂ« ");
        public bool UploadDataP => UserPerm.Contains(" Õ„Ì· «·œ« «");
        public bool DownloadDataP => UserPerm.Contains(" ‰“Ì· «·œ« «");
        public bool ManualP => UserPerm.Contains("Manual");
        public bool ErrandP => UserPerm.Contains("≈Ã—«¡« ");
        public bool OfficalsP => UserPerm.Contains("«·„”ƒÊ·Ê‰");
        public bool MonthlyP => UserPerm.Contains("«·Õ—ﬂ«  «·‘Â—Ì…");
        public bool AddMachineP => UserPerm.Contains("≈÷«›…");
        public bool GetDataP => UserPerm.Contains(" Õ„Ì· «·»Ì«‰« ");
        public bool BranchP => UserPerm.Contains("«·›—Ê⁄");
        public bool RoleP => UserPerm.Contains("«·Ã—Ê»« ");
        public bool DepartP => UserPerm.Contains("«·«œ«—« ");
        public bool QualificationP => UserPerm.Contains("«·„ƒÂ·« ");
        public bool JobP => UserPerm.Contains("«·ÊŸ«∆›");
        public bool DegreeP => UserPerm.Contains("«·ﬁÿ«⁄« ");
        public bool AreaP => UserPerm.Contains("«·„‰«ÿﬁ");
        public bool AddEmploP => UserPerm.Contains("«÷«›… „ÊŸ›");
        public bool AllDatasP => UserPerm.Contains("«÷«›… «·»Ì«‰« ");
        public bool AllPermissionP => UserPerm.Contains("«·’·«ÕÌ« ");
        public bool AllSettingsP => UserPerm.Contains("«·«⁄œ«œ«  «·⁄«„…");
        public bool ShiftP => UserPerm.Contains("«·Ê—œÌ« ");
        public bool BreakP => UserPerm.Contains("«·—«Õ« ");
        public bool LateP => UserPerm.Contains("«· √ŒÌ—« ");
        public bool WHP => UserPerm.Contains("«·«Ã«“«  «·«”»Ê⁄Ì…");
        public bool HTypeP => UserPerm.Contains("√‰Ê«⁄ «·«Ã«“« ");
        public bool EmpEvalP => UserPerm.Contains(" ﬁÌÌ„ „ÊŸ›");
        public bool PermissionP => UserPerm.Contains("’·«ÕÌ«  «·»—‰«„Ã");
        public bool MonthSettingP => UserPerm.Contains("«⁄œ«œ«  «·‘Â—");
        public bool UserPermissionP => UserPerm.Contains("’·«ÕÌ«  «·„” Œœ„");
        public bool BranchPermissionP => UserPerm.Contains("’·«ÕÌ«  «·›—Ê⁄");
        public bool Backup => UserPerm.Contains("Backup");
        public bool AddMainSalaryP => UserPerm.Contains("«œ«—… „«·Ì«  „ÊŸ›");
        public bool SalaryP => UserPerm.Contains("«·«ÃÊ— Ê«·„— »« ");
        public bool AddDeductionsP => UserPerm.Contains("«” Õﬁ«ﬁ  Ê «” ﬁÿ«⁄« ");
        public bool SalaryReportP => UserPerm.Contains(" ﬁ—Ì— «·„— »« ");
        public bool ArchiveP => UserPerm.Contains("«·«—‘Ì›");
        public bool ManageBalanceP => UserPerm.Contains("≈œ«—… «·—’Ìœ");
        public bool HoliRequestsP => UserPerm.Contains("ÿ·»«  «·«Ã«“…");
        public bool NewRequestP => UserPerm.Contains("ÿ·» ≈Ã«“…");
        public bool ManageLeaveTypesP => UserPerm.Contains("√‰Ê«⁄ «·≈Ã«“« ");
        public bool BalanceReportP => UserPerm.Contains(" ﬁ—Ì— «·—’Ìœ");
        public bool NewMissionP => UserPerm.Contains("ÿ·» „√„Ê—Ì…");
        public bool ManageMissionP => UserPerm.Contains("ÿ·»«  «·„√„Ê—Ì« ");
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
        private void AddOfficalsOpen(object sender, RoutedEventArgs e) => OpenWindow<AddOffical>();

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
                LocalizationManager.ShowMessage($" „ ≈‰‘«¡ «·ﬁ«·» »‰Ã«Õ: {filePath}");

                // › Õ «·„·› ›Ì Excel
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
                    LocalizationManager.ShowMessage($" „ ≈‰‘«¡ «·ﬁ«·» Ê·ﬂ‰ ÕœÀ Œÿ√ ›Ì › ÕÂ: {ex.Message}");
                }
            }
        }

        private async void upload_data_btn_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var result = LocalizationManager.ShowMessage(
        " Õ–Ì—: ”Ì „ «” Ì—«œ «·»Ì«‰«  „‰ „·› Excel.\n" +
        "«·»Ì«‰«  «·„ﬂ——… ”Ì „  ÕœÌÀÂ«.\n" +
        "Â·  —Ìœ «·„ «»⁄…ø",
        " √ﬂÌœ «·«” Ì—«œ",
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
            // ⁄—÷ ‰«›–… «Œ Ì«— ‰Ê⁄ «· ’œÌ—
            var dialog = new ExportTypeDialog();
            dialog.ShowDialog();

            if (dialog.ExportType == ExportType.None)
                return;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = "Export Attendance Data"
            };

            //  ⁄ÌÌ‰ «”„ «·„·› «·«› —«÷Ì »‰«¡ ⁄·Ï ‰Ê⁄ «· ’œÌ—
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
                            LocalizationManager.ShowMessage("·«  ÊÃœ »Ì«‰«  ·· ’œÌ—");
                            return;
                        }

                        using (var workbook = new XLWorkbook())
                        {
                            if (dialog.ExportType == ExportType.ForImport)
                            {
                                // «· ’œÌ— »‰›”  ‰”Ìﬁ Template «·≈” Ì—«œ
                                ExportForImport(workbook, attendanceData);
                            }
                            else
                            {
                                // «· ’œÌ— » ﬁ—Ì— „›’·
                                ExportDetailedReport(workbook, attendanceData);
                            }

                            workbook.SaveAs(saveFileDialog.FileName);

                            string message = $" „ «· ’œÌ— »‰Ã«Õ ≈·Ï: {saveFileDialog.FileName}";

                            if (dialog.ExportType == ExportType.ForImport)
                            {
                                message += "\n\n„·«ÕŸ…:  „  ’œÌ— «·»Ì«‰«  »‰›”  ‰”Ìﬁ Template «·≈” Ì—«œ.";
                                message += "\nÌ„ﬂ‰ﬂ  ⁄œÌ· Â–« «·„·› À„ ≈⁄«œ… «” Ì—«œÂ.";
                            }

                            LocalizationManager.ShowMessage(message, " „ «· ’œÌ—");

                            // › Õ «·„·›  ·ﬁ«∆Ì«
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
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì «· ’œÌ—: {ex.Message}");
                }
            }
        }

        private static void ExportForImport(XLWorkbook workbook, List<Attendance> attendanceData)
        {
            var worksheet = workbook.Worksheets.Add("AttendanceData");

            // ﬂ «»… «·⁄‰«ÊÌ‰ „‰ Template
            for (int i = 0; i < ExcelTemplateHelper.TemplateHeaders.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = ExcelTemplateHelper.TemplateHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // ≈÷«›… «·»Ì«‰«  »‰›” «· ‰”Ìﬁ
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

            // ÷»ÿ ⁄—÷ «·√⁄„œ…
            worksheet.Columns().AdjustToContents();

            // ≈÷«›…  ⁄·Ì„«  ›Ì Ê—ﬁ… „‰›’·…
            var instructionsSheet = workbook.Worksheets.Add(" ⁄·Ì„« ");

            instructionsSheet.Cell(1, 1).Value = " ⁄·Ì„«  «” Œœ«„ «·„·›";
            instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
            instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

            var instructions = new[]
            {
        "Â–« «·„·›  „  ’œÌ—Â »‰›”  ‰”Ìﬁ Template «·≈” Ì—«œ",
        "",
        "·≈⁄«œ… «” Ì—«œ «·»Ì«‰«  »⁄œ «· ⁄œÌ·:",
        "1. Ì„ﬂ‰ﬂ  ⁄œÌ· √Ì Õﬁ· ›Ì Ê—ﬁ… AttendanceData",
        "2. «Õ ›Ÿ »‰›” √”„«¡ «·√⁄„œ…",
        "3. «Õ ›Ÿ »‰›” «· ‰”Ìﬁ ·· Ê«—ÌŒ Ê«·√Êﬁ« ",
        "4. ⁄‰œ «” Ì—«œ «·„·› »⁄œ «· ⁄œÌ·:",
        "   - «·”Ã·«  «·„ﬂ——… (»‰›” UserId Ê AttendanceDate) ”Ì „  ÕœÌÀÂ«",
        "   - «·”Ã·«  «·ÃœÌœ… ”Ì „ ≈÷«› Â«",
        "   - «·”Ã·«  «·„Õ–Ê›… „‰ «·„·› ·‰  Õ–› „‰ ﬁ«⁄œ… «·»Ì«‰« ",
        "",
        " ‰”Ìﬁ«  «·»Ì«‰« :",
        "- «· Ê«—ÌŒ: yyyy-MM-dd („À«·: 2024-12-30)",
        "- «·√Êﬁ« : HH:mm:ss („À«·: 08:30:00)",
        "- «·ÕﬁÊ· «·„‰ÿﬁÌ…: true √Ê false",
        "- «·ÕﬁÊ· «·—ﬁ„Ì…: √—ﬁ«„ ›ﬁÿ",
        "",
        "„·«ÕŸ…:  √ﬂœ „‰ ’Õ… «·‹ IDs (UserId, ShiftId, BranchId)",
        "ﬁ»· «·«” Ì—«œ"
    };

            for (int i = 0; i < instructions.Length; i++)
            {
                instructionsSheet.Cell(i + 3, 1).Value = instructions[i];
            }

            instructionsSheet.Columns().AdjustToContents();
        }

        private static void ExportDetailedReport(XLWorkbook workbook, List<Attendance> attendanceData)
        {
            var worksheet = workbook.Worksheets.Add(" ﬁ—Ì—  ›’Ì·Ì");

            // ⁄‰«ÊÌ‰ «· ﬁ—Ì— «·„›’·
            var detailedHeaders = new[]
            {
        "ID",
        "ﬂÊœ «·„ÊŸ›",
        "«”„ «·„ÊŸ›",
        "«· «—ÌŒ",
        "Êﬁ  «·Õ÷Ê—",
        "Êﬁ  «·«‰’—«›",
        "«·Ê—œÌ…",
        "Êﬁ  «·œÊ«„ «·—”„Ì",
        "Êﬁ  «·«‰ Â«¡ «·—”„Ì",
        "«· √ŒÌ—",
        "«·«‰’—«› «·„»ﬂ—",
        "«·Õ÷Ê— «·„»ﬂ—",
        "Êﬁ  ≈÷«›Ì",
        "”«⁄«  «·⁄„·",
        "›—⁄ «·Õ÷Ê—",
        "›—⁄ «·«‰’—«›",
        "„Êﬁ⁄ «·Õ÷Ê—",
        "„Êﬁ⁄ «·«‰’—«›",
        "„⁄›Ì „‰ «· √ŒÌ—",
        "„⁄›Ì „‰ «·«‰’—«› «·„»ﬂ—",
        "„⁄›Ì „‰ «·Êﬁ  «·≈÷«›Ì",
        "≈Ã«“…",
        "€Ì«»",
        "‰Ê⁄ «·≈Ã«“…",
        "Œÿ «·⁄—÷ (Õ÷Ê—)",
        "Œÿ «·ÿÊ· (Õ÷Ê—)",
        "Œÿ «·⁄—÷ («‰’—«›)",
        "Œÿ «·ÿÊ· («‰’—«›)"
    };

            // ﬂ «»… «·⁄‰«ÊÌ‰
            for (int i = 0; i < detailedHeaders.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = detailedHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ≈÷«›… «·»Ì«‰« 
            int row = 2;
            foreach (var attendance in attendanceData)
            {
                worksheet.Cell(row, 1).Value = attendance.Id;
                worksheet.Cell(row, 2).Value = attendance.UserId;
                worksheet.Cell(row, 3).Value = attendance.User?.FullName ?? "€Ì— „Õœœ";
                worksheet.Cell(row, 4).Value = attendance.AttendanceDate.ToString("yyyy-MM-dd");

                if (attendance.CheckInTime.HasValue)
                    worksheet.Cell(row, 5).Value = attendance.CheckInTime.Value.ToString("HH:mm");

                if (attendance.CheckOutTime.HasValue)
                    worksheet.Cell(row, 6).Value = attendance.CheckOutTime.Value.ToString("HH:mm");

                worksheet.Cell(row, 7).Value = attendance.Shift?.Name ?? "€Ì— „Õœœ";
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

                worksheet.Cell(row, 15).Value = attendance.CheckInBranch?.Name ?? "€Ì— „Õœœ";
                worksheet.Cell(row, 16).Value = attendance.CheckOutBranch?.Name ?? "€Ì— „Õœœ";
                worksheet.Cell(row, 17).Value = attendance.CheckInLocation ?? "";
                worksheet.Cell(row, 18).Value = attendance.CheckOutLocation ?? "";
                worksheet.Cell(row, 19).Value = attendance.ExemptLate ? "‰⁄„" : "·«";
                worksheet.Cell(row, 20).Value = attendance.ExemptEarlyLeave ? "‰⁄„" : "·«";
                worksheet.Cell(row, 21).Value = attendance.ExemptOvertime ? "‰⁄„" : "·«";
                worksheet.Cell(row, 22).Value = attendance.IsHoliday ? "‰⁄„" : "·«";
                worksheet.Cell(row, 23).Value = attendance.IsAbsence ? "‰⁄„" : "·«";
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
                    // «· Õﬁﬁ „‰ «· ‰”Ìﬁ
                    if (!IsValidTemplateFormat(openFileDialog.FileName))
                    {
                        var result = LocalizationManager.ShowMessage(
                            "Ì»œÊ √‰ Â–« «·„·› ·Ì” »‰›”  ‰”Ìﬁ Template «·≈” Ì—«œ.\n" +
                            "Â·  —Ìœ «·„ «»⁄… ⁄·Ï √Ì Õ«·ø",
                            " Õ–Ì—",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                            return;
                    }

                    var progressWindow = new ProgressDialog();
                    progressWindow.Show();

                    progressWindow.UpdateStatus("Ã«—Ì —›⁄ «·Õ—ﬂ« ...");

                    await ImportFileAsync(openFileDialog.FileName, connectionString, progressWindow);

                    progressWindow.Close();
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì «·«” Ì—«œ: {ex.Message}");
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

                    // «· Õﬁﬁ „‰ ⁄œœ «·√⁄„œ…
                    int columnCount = firstRow.CellsUsed().Count();
                    if (columnCount != ExcelTemplateHelper.TemplateHeaders.Count)
                        return false;

                    // «· Õﬁﬁ „‰ »⁄÷ «·⁄‰«ÊÌ‰ «·—∆Ì”Ì…
                    var headers = new List<string>();
                    foreach (var cell in firstRow.CellsUsed())
                    {
                        headers.Add(cell.GetString().ToLower());
                    }

                    // «· Õﬁﬁ „‰ ÊÃÊœ ⁄‰«ÊÌ‰ √”«”Ì…
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
                        LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï Ê—ﬁ… »«”„ 'AttendanceData'");
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
                            progressWindow.UpdateStatus($"„⁄«·Ã… ”ÿ— {processed} „‰ {totalRows}");

                            var userId = row.Cell(1).GetValue<int?>();
                            var dateStr = row.Cell(2).GetString();

                            if (!userId.HasValue || string.IsNullOrWhiteSpace(dateStr))
                            {
                                errors++;
                                errorMessages.Add($"”ÿ— {row.RowNumber()}: »Ì«‰«  ‰«ﬁ’…");
                                continue;
                            }

                            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var attendanceDate))
                            {
                                errors++;
                                errorMessages.Add($"”ÿ— {row.RowNumber()}:  ‰”Ìﬁ  «—ÌŒ €Ì— ’ÕÌÕ");
                                continue;
                            }

                            // «·»ÕÀ ⁄‰ ”Ã· „ÊÃÊœ
                            var existingAttendance = await context.Attendances
                                .FirstOrDefaultAsync(a =>
                                    a.UserId == userId.Value &&
                                    a.AttendanceDate.Date == attendanceDate.Date);

                            var attendance = existingAttendance ?? new Attendance
                            {
                                UserId = userId.Value,
                                AttendanceDate = attendanceDate
                            };

                            //  ÕœÌÀ «·»Ì«‰«  „‰ Excel
                            UpdateAttendanceFromExcel(row, attendance);

                            // «· Õﬁﬁ „‰ «·’Õ…
                            var validation = await ValidateAttendance(context, attendance);
                            if (!validation.IsValid)
                            {
                                errors++;
                                errorMessages.Add($"”ÿ— {row.RowNumber()}: {validation.ErrorMessage}");
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

                            // Õ›Ÿ ﬂ· 100 ”Ã·
                            if (processed % 100 == 0)
                            {
                                await context.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            errorMessages.Add($"”ÿ— {row.RowNumber()}: {ex.Message}");
                        }
                    }

                    // Õ›Ÿ «·»«ﬁÌ
                    await context.SaveChangesAsync();

                    // ⁄—÷ «·‰ «∆Ã
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

            // „Õ«Ê·…  ÕÊÌ·  ‰”Ìﬁ hh:mm:ss ≈–« ﬂ«‰ ›ÌÂ ‘—ÿ…
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
            return value == "true" || value == "yes" || value == "‰⁄„" || value == "1";
        }

        private static async Task<(bool IsValid, string ErrorMessage)> ValidateAttendance(
            AppDbContext context, Attendance attendance)
        {
            // «· Õﬁﬁ „‰ ÊÃÊœ «·„” Œœ„
            var userExists = await context.Users.AnyAsync(u => u.Id == attendance.UserId);
            if (!userExists)
            {
                return (false, $"«·„” Œœ„ »—ﬁ„ {attendance.UserId} €Ì— „ÊÃÊœ");
            }

            // «· Õﬁﬁ „‰ ÊÃÊœ Shift ≈–«  „  ÕœÌœÂ
            if (attendance.ShiftId.HasValue)
            {
                var shiftExists = await context.Shifts.AnyAsync(s => s.Id == attendance.ShiftId.Value);
                if (!shiftExists)
                {
                    return (false, $"«·Ê—œÌ… »—ﬁ„ {attendance.ShiftId} €Ì— „ÊÃÊœ…");
                }
            }

            // «· Õﬁﬁ „‰ ÊÃÊœ «·›—Ê⁄
            if (attendance.CheckInBranchId.HasValue)
            {
                var branchExists = await context.Branches.AnyAsync(b => b.Id == attendance.CheckInBranchId.Value);
                if (!branchExists)
                {
                    return (false, $"«·›—⁄ »—ﬁ„ {attendance.CheckInBranchId} €Ì— „ÊÃÊœ");
                }
            }

            if (attendance.CheckOutBranchId.HasValue)
            {
                var branchExists = await context.Branches.AnyAsync(b => b.Id == attendance.CheckOutBranchId.Value);
                if (!branchExists)
                {
                    return (false, $"«·›—⁄ »—ﬁ„ {attendance.CheckOutBranchId} €Ì— „ÊÃÊœ");
                }
            }

            // «· Õﬁﬁ „‰ «· Ê«—ÌŒ
            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                if (attendance.CheckOutTime.Value < attendance.CheckInTime.Value)
                {
                    return (false, "Êﬁ  «·«‰’—«› ·« Ì„ﬂ‰ √‰ ÌﬂÊ‰ ﬁ»· Êﬁ  «·Õ÷Ê—");
                }
            }

            return (true, string.Empty);
        }

        bool IsDrawer = false;

        public event PropertyChangedEventHandler? PropertyChanged;

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
                    // ≈‰‘«¡ ‰”Œ… «Õ Ì«ÿÌ… „»«‘—… „‰ SQL Server
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

                    LocalizationManager.ShowMessage($" „ ≈‰‘«¡ ‰”Œ… «Õ Ì«ÿÌ… »‰Ã«Õ: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì ≈‰‘«¡ «·‰”Œ… «·«Õ Ì«ÿÌ…: {ex.Message}");
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
                // › Õ „·› Excel
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls",
                    Title = "«Œ — „·› Excel ··⁄„Ê·« "
                };

                if (openFileDialog.ShowDialog() != true)
                    return;

                // ﬁ—«¡… «·»Ì«‰«  „‰ Excel
                var commissionData = _excelReader.ReadCommissionExcel(openFileDialog.FileName);

                if (commissionData.Count == 0)
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï »Ì«‰«  ’«·Õ… ›Ì «·„·›", " Õ–Ì—",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // „⁄«·Ã… «·»Ì«‰« 
                var (salaries, errors) = _commissionProcessor.ProcessCommissions(commissionData);

                // ⁄—÷ «·‰ «∆Ã ··„” Œœ„
                ShowImportResults(salaries.Count, errors);

                if (salaries.Count > 0)
                {
                    // Õ›Ÿ ›Ì ﬁ«⁄œ… «·»Ì«‰« 
                    await SaveSalariesToDatabase(salaries);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì «” Ì—«œ «·»Ì«‰« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowImportResults(int successCount, List<string> errors)
        {
            string message = $" „ „⁄«·Ã… {successCount} ⁄„Ê·… »‰Ã«Õ";

            if (errors.Count > 0)
            {
                message += $"\n\n«·√Œÿ«¡ ({errors.Count}):\n" + string.Join("\n", errors.Take(10));

                if (errors.Count > 10)
                    message += $"\n...Ê {errors.Count - 10} Œÿ√ ¬Œ—";
            }

            LocalizationManager.ShowMessage(message, "‰ ÌÃ… «·«” Ì—«œ",
                MessageBoxButton.OK,
                errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private async System.Threading.Tasks.Task SaveSalariesToDatabase(List<Salary> salaries)
        {
            try
            {
                _context.Salaries.AddRange(salaries);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage($" „ Õ›Ÿ {salaries.Count} ⁄„Ê·… ›Ì ﬁ«⁄œ… «·»Ì«‰«  »‰Ã«Õ", "‰Ã«Õ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ «·»Ì«‰« : {ex.Message}", "Œÿ√",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void uploadEmployeesBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "„·›«  Excel|*.xls;*.xlsx",
                Title = "«Œ — „·› Excel ··„ÊŸ›Ì‰"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            // ≈‰‘«¡ Ê‰‘— ‰«›–… «· Õ„Ì·
            var progressDialog = new ProgressDialog
            {
                Owner = this
            };

            // „ €Ì— ·  »⁄ «·≈·€«¡
            bool isCancelled = false;

            // ⁄—÷ ‰«›–… «· Õ„Ì·
            progressDialog.Show();

            try
            {
                //  ÕœÌÀ Õ«·… «· Õ„Ì·
                progressDialog.UpdateStatus("Ã«—Ì ﬁ—«¡… „·› Excel...");

                // ≈‰‘«¡ Service ··«” Ì—«œ „⁄ callback ·· ﬁœ„
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

                // «· Õﬁﬁ „‰ ≈·€«¡ «·⁄„·Ì…
                progressDialog.Closing += (s, args) =>
                {
                    if (progressDialog.IsCancelled)
                    {
                        isCancelled = true;
                        userImporter.CancelImport();
                    }
                };

                // »œ¡ «·«” Ì—«œ (›Ì thread „‰›’·)
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
                        return -1; //  „ «·≈·€«¡
                    }
                    catch (Exception ex)
                    {
                        throw; // —„Ì «·«” À‰«¡ ·· ⁄«„· „⁄Â ›Ì «·Œ«—Ã
                    }
                });

                // «·«‰ Ÿ«— Õ Ï «‰ Â«¡ «·„Â„…
                var result = await importTask;

                // ≈€·«ﬁ ‰«›–… «· Õ„Ì·
                Dispatcher.Invoke(() => progressDialog.Close());

                // ⁄—÷ «·‰ «∆Ã
                if (isCancelled)
                {
                    LocalizationManager.ShowMessage(" „ ≈·€«¡ ⁄„·Ì… «·«” Ì—«œ", "≈·€«¡",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result == -1)
                {
                    LocalizationManager.ShowMessage(" „ ≈·€«¡ «·⁄„·Ì…", "≈·€«¡",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result > 0)
                {
                    LocalizationManager.ShowMessage($" „ «” Ì—«œ {result} „ÊŸ› »‰Ã«Õ!", "‰Ã«Õ",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LocalizationManager.ShowMessage("·„ Ì „ «” Ì—«œ √Ì „ÊŸ›Ì‰", " Õ–Ì—",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    progressDialog.Close();
                    LocalizationManager.ShowMessage($"Œÿ√ ›Ì «” Ì—«œ «·„ÊŸ›Ì‰: {ex.Message}",
                        "Œÿ√ ›Ì «·«” Ì—«œ",
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
            InitializeFlags();
            LoadGIFAsync();
            DashboardManager dashboardManager = new DashboardManager();
            dashboardControl.Children.Add(dashboardManager.GetDashboardWindow());
            UpdateThemeButton();
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Logo))
                GIFBack.Source = new BitmapImage(new Uri(Properties.Settings.Default.Logo));
            if (Properties.Settings.Default.Language == "ar")
                WelcomeText.Text = $"„—Õ»« »ﬂ° {App.CurrentUser.FullName}";
            else
                WelcomeText.Text = $"Welcome, {App.CurrentUser.FullName}";


            StartTimer();


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

