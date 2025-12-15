using Sho2on.Database.Models;
using System.Timers;

namespace Sho2onMobile.Pages
{
    public partial class MainPage : ContentPage
    {
        User user;
        AttendanceService attendance = new AttendanceService();
        private System.Timers.Timer timer;

        public MainPage(User u)
        {
            InitializeComponent();
            user = u;

            // تحديث التاريخ والوقت
            UpdateDateTime();
            StartTimer();

            // تحميل بيانات اليوم
            LoadTodayData();
        }

        private void StartTimer()
        {
            timer = new System.Timers.Timer(1000); // تحديث كل ثانية
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDateTime();
            });
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;

            // تحديث التاريخ بالعربية
            dateLabel.Text = now.ToString("dddd، dd MMMM yyyy",
                new System.Globalization.CultureInfo("ar-SA"));

            // تحديث الوقت بالعربية
            timeLabel.Text = now.ToString("hh:mm tt",
                new System.Globalization.CultureInfo("ar-SA"));
        }

        private async void LoadTodayData()
        {
            try
            {
                // هنا يمكنك استدعاء API أو قاعدة بيانات لتحميل بيانات اليوم
                // هذا مثال فقط
                checkInTimeLabel.Text = "--:--";
                checkOutTimeLabel.Text = "--:--";
                workHoursLabel.Text = "0 ساعة";
                todayStatusLabel.Text = "لم يسجل بعد";
                statusLabel.Text = "حالة الحضور: غير مسجل";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading today data: {ex.Message}");
            }
        }

        // حدث الضغط على زر البصمة
        private async void OnFingerprintClicked(object sender, EventArgs e)
        {

            // هنا يمكنك إضافة كود البصمة الفعلي

            // عرض خيارات الحضور والانصراف
            attendanceActions.IsVisible = true;

            // إضافة تأثير اهتزاز بسيط (اختياري)
            await fingerprintBtn.ScaleTo(0.9, 100);
            await fingerprintBtn.ScaleTo(1, 100);
        }

        // حدث إلغاء
        private void OnCancelAttendance(object sender, EventArgs e)
        {
            attendanceActions.IsVisible = false;
        }

        private async void DoCheckIn(object sender, EventArgs e)
        {
            try
            {
                // عرض مؤشر تحميل
                var loadingTask = DisplayAlert("جاري المعالجة", "جاري تسجيل الحضور...", "OK");

                var status = await attendance.AddAttendanceAsync(user.Id, 1, user.BranchId);

                await loadingTask;

                if (status)
                {

                    // تحديث البيانات
                    checkInTimeLabel.Text = DateTime.Now.ToString("hh:mm tt");
                    statusLabel.Text = "حالة الحضور: حاضر";
                    todayStatusLabel.Text = "حاضر";
                    todayStatusLabel.TextColor = Color.FromArgb("#27AE60");

                    // إخفاء خيارات الحضور
                    attendanceActions.IsVisible = false;

                    // تحديث ساعات العمل (مثال)
                    UpdateWorkHours();
                }
                else
                {
                    await DisplayAlert("خطأ", "فشل تسجيل الحضور", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", $"حدث خطأ: {ex.InnerException}", "OK");
            }
        }

        private async void DoCheckOut(object sender, EventArgs e)
        {
            try
            {
                // عرض مؤشر تحميل
                var loadingTask = DisplayAlert("جاري المعالجة", "جاري تسجيل الانصراف...", "OK");

                var status = await attendance.AddAttendanceAsync(user.Id, 0, user.BranchId);

                await loadingTask;

                if (status)
                {

                    // تحديث البيانات
                    checkOutTimeLabel.Text = DateTime.Now.ToString("hh:mm tt");
                    statusLabel.Text = "حالة الحضور: منصرف";
                    todayStatusLabel.Text = "منصرف";
                    todayStatusLabel.TextColor = Color.FromArgb("#E74C3C");

                    // إخفاء خيارات الحضور
                    attendanceActions.IsVisible = false;

                    // تحديث ساعات العمل
                    UpdateWorkHours();
                }
                else
                {
                    await DisplayAlert("خطأ", "فشل تسجيل الانصراف", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", $"حدث خطأ: {ex.Message}", "OK");
            }
        }

        private void UpdateWorkHours()
        {
            try
            {
                // هنا يمكنك حساب ساعات العمل الفعلية
                // هذا مثال فقط
                if (checkInTimeLabel.Text != "--:--" && checkOutTimeLabel.Text != "--:--")
                {
                    // حساب الفرق بين الوقتين
                    workHoursLabel.Text = "8 ساعات"; // مثال
                }
                else if (checkInTimeLabel.Text != "--:--")
                {
                    workHoursLabel.Text = "جاري العمل...";
                }
            }
            catch
            {
                workHoursLabel.Text = "0 ساعة";
            }
        }

        private async void Logout(object sender, EventArgs e)
        {
            var result = await DisplayAlert("تأكيد", "هل أنت متأكد من تسجيل الخروج؟", "نعم", "لا");

            if (result)
            {
                // إيقاف المؤقت
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }

                await Navigation.PopToRootAsync();
            }
        }

        // تنظيف الموارد عند مغادرة الصفحة
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
        }
    }
}