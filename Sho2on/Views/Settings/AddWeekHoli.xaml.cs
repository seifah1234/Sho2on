using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddWeekHoli.xaml
    /// </summary>
    public partial class AddWeekHoli : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        private List<WeekHoliday> _weekHolidays = new List<WeekHoliday>();

        public AddWeekHoli()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                name_box.Clear();
                _weekHolidays.Clear();
                list.Items.Clear();

                // تحميل البيانات من قاعدة البيانات
                _weekHolidays = await _context.WeekHolidays
                    .OrderBy(w => w.Name)
                    .ToListAsync();

                // إضافة عنصر LocalizationManager.Translate("لا يوجد") في البداية
                list.Items.Add(LocalizationManager.Translate("لا يوجد"));

                // إضافة الإجازات إلى القائمة
                foreach (var holiday in _weekHolidays)
                {
                    list.Items.Add(holiday.Name);
                }

                // إعادة تعيين أيام الأسبوع
                ResetDays();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDays()
        {
            Week.D1.IsChecked = false;
            Week.D2.IsChecked = false;
            Week.D3.IsChecked = false;
            Week.D4.IsChecked = false;
            Week.D5.IsChecked = false;
            Week.D6.IsChecked = false;
            Week.D7.IsChecked = false;
            editBtn.Visibility = Visibility.Collapsed;
            deleteBtn.Visibility = Visibility.Collapsed;
            saveBtn.Visibility = Visibility.Visible;
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("يرجى إدخال اسم الإجازة", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من عدم وجود اسم مكرر
                bool exists = await _context.WeekHolidays
                    .AnyAsync(w => w.Name == name_box.Text.Trim());

                if (exists)
                {
                    LocalizationManager.ShowMessage("اسم الإجازة موجود مسبقاً", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var weekHoliday = new WeekHoliday
                {
                    Name = name_box.Text.Trim(),
                    Day1 = Week.D1.IsChecked ?? false,
                    Day2 = Week.D2.IsChecked ?? false,
                    Day3 = Week.D3.IsChecked ?? false,
                    Day4 = Week.D4.IsChecked ?? false,
                    Day5 = Week.D5.IsChecked ?? false,
                    Day6 = Week.D6.IsChecked ?? false,
                    Day7 = Week.D7.IsChecked ?? false,
                    CreatedAt = DateTime.Now,
                    EditedAt = DateTime.Now
                };


                if (_weekHolidays.FirstOrDefault(a => a.Name == weekHoliday.Name) != null)
                {
                    LocalizationManager.ShowMessage("هذه الإجازة موجودة بالفعل", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _context.WeekHolidays.AddAsync(weekHoliday);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم اضافة الإجازة الأسبوعية", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء الحفظ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exit_Btn(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem == null || list.SelectedItem.ToString() == LocalizationManager.Translate("لا يوجد"))
            {
                LocalizationManager.ShowMessage("لم يتم اختيار الإجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string selectedName = list.SelectedItem.ToString();
                var weekHoliday = await _context.WeekHolidays
                    .FirstOrDefaultAsync(w => w.Name == selectedName);

                if (weekHoliday != null)
                {
                    var result = LocalizationManager.ShowMessage(
                        $"هل أنت متأكد من حذف الإجازة '{selectedName}'؟",
                        LocalizationManager.Translate("تأكيد الحذف"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        _context.WeekHolidays.Remove(weekHoliday);
                        await _context.SaveChangesAsync();

                        LocalizationManager.ShowMessage("تم حذف الإجازة", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء الحذف: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (list.SelectedItem == null || list.SelectedItem.ToString() == LocalizationManager.Translate("لا يوجد"))
                {
                    LocalizationManager.ShowMessage("لم تختار أي إجازة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("يرجى إدخال اسم الإجازة", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string selectedName = list.SelectedItem.ToString();
                var weekHoliday = await _context.WeekHolidays
                    .FirstOrDefaultAsync(w => w.Name == selectedName);

                if (weekHoliday != null)
                {
                    // التحقق من عدم وجود اسم مكرر (إذا تم تغيير الاسم)
                    if (weekHoliday.Name != name_box.Text.Trim())
                    {
                        bool exists = await _context.WeekHolidays
                            .AnyAsync(w => w.Name == name_box.Text.Trim());

                        if (exists)
                        {
                            LocalizationManager.ShowMessage("اسم الإجازة موجود مسبقاً", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // تحديث البيانات
                    weekHoliday.Name = name_box.Text.Trim();
                    weekHoliday.Day1 = Week.D1.IsChecked ?? false;
                    weekHoliday.Day2 = Week.D2.IsChecked ?? false;
                    weekHoliday.Day3 = Week.D3.IsChecked ?? false;
                    weekHoliday.Day4 = Week.D4.IsChecked ?? false;
                    weekHoliday.Day5 = Week.D5.IsChecked ?? false;
                    weekHoliday.Day6 = Week.D6.IsChecked ?? false;
                    weekHoliday.Day7 = Week.D7.IsChecked ?? false;
                    weekHoliday.EditedAt = DateTime.Now;

                    _context.WeekHolidays.Update(weekHoliday);
                    await _context.SaveChangesAsync();

                    LocalizationManager.ShowMessage("تم تعديل الإجازة", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء التعديل: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem != null && list.SelectedItem.ToString() != LocalizationManager.Translate("لا يوجد"))
            {
                try
                {
                    string selectedName = list.SelectedItem.ToString();
                    var weekHoliday = await _context.WeekHolidays
                        .FirstOrDefaultAsync(w => w.Name == selectedName);

                    if (weekHoliday != null)
                    {
                        name_box.Text = weekHoliday.Name;
                        Week.D1.IsChecked = weekHoliday.Day1;
                        Week.D2.IsChecked = weekHoliday.Day2;
                        Week.D3.IsChecked = weekHoliday.Day3;
                        Week.D4.IsChecked = weekHoliday.Day4;
                        Week.D5.IsChecked = weekHoliday.Day5;
                        Week.D6.IsChecked = weekHoliday.Day6;
                        Week.D7.IsChecked = weekHoliday.Day7;
                        editBtn.Visibility = Visibility.Visible;
                        deleteBtn.Visibility = Visibility.Visible;
                        saveBtn.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    LocalizationManager.ShowMessage($"خطأ في تحميل بيانات الإجازة: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                name_box.Clear();
                ResetDays();
            }
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetDays();
            list.SelectedItem = null;
        }
    }
}
