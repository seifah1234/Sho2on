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
    /// Interaction logic for AddJobDegree.xaml
    /// </summary>
    public partial class AddJobDegree : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        private List<Degree> _degrees = new List<Degree>();

        public AddJobDegree()
        {
            InitializeComponent();
        }

        private async Task LoadData()
        {
            try
            {
                name_box.Clear();
                _degrees.Clear();

                _degrees = await _context.Degrees.ToListAsync();
                list.ItemsSource = _degrees;
            }
            catch (Exception e)
            {
                LocalizationManager.ShowMessage($"خطأ في تحميل البيانات: {e.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("يرجى إدخال اسم الدرجة الوظيفية", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var degree = new Degree
                {
                    Name = name_box.Text.Trim(),
                    EditedAt = DateTime.Now
                };

                await _context.Degrees.AddAsync(degree);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم اضافة الدرجة الوظيفية", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (list.SelectedItem is not Degree degree)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار الدرجة الوظيفية", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = LocalizationManager.ShowMessage($"هل أنت متأكد من حذف الدرجة الوظيفية '{degree.Name}'؟",
                    LocalizationManager.Translate("تأكيد الحذف"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Degrees.Remove(degree);
                    await _context.SaveChangesAsync();

                    LocalizationManager.ShowMessage("تم حذف الدرجة الوظيفية", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
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
                if (list.SelectedItem is not Degree degree)
                {
                    LocalizationManager.ShowMessage("لم تختار أي درجة وظيفية", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    LocalizationManager.ShowMessage("يرجى إدخال اسم الدرجة الوظيفية", LocalizationManager.Translate("تحذير"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                degree.Name = name_box.Text.Trim();
                degree.EditedAt = DateTime.Now;

                _context.Degrees.Update(degree);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage("تم تعديل الدرجة الوظيفية", LocalizationManager.Translate("نجاح"), MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ أثناء التعديل: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is Degree degree)
            {
                name_box.Text = degree.Name;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        // إزالة الدوال غير المستخدمة من الكود القديم
        private void Exit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
        private void B_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
        private void Exit_Click(object sender, RoutedEventArgs e) { }
        private void Min_Click(object sender, RoutedEventArgs e) { }
        private void Max_Click(object sender, RoutedEventArgs e) { }
    }
}
