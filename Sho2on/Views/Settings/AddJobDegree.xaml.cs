using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
                MessageBox.Show($"خطأ في تحميل البيانات: {e.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم الدرجة الوظيفية", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var degree = new Degree
                {
                    Name = name_box.Text.Trim(),
                    EditedAt = DateTime.Now
                };

                await _context.Degrees.AddAsync(degree);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم اضافة الدرجة الوظيفية", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("لم يتم اختيار الدرجة الوظيفية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف الدرجة الوظيفية '{degree.Name}'؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Degrees.Remove(degree);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم حذف الدرجة الوظيفية", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (list.SelectedItem is not Degree degree)
                {
                    MessageBox.Show("لم تختار أي درجة وظيفية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(name_box.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم الدرجة الوظيفية", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                degree.Name = name_box.Text.Trim();
                degree.EditedAt = DateTime.Now;

                _context.Degrees.Update(degree);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم تعديل الدرجة الوظيفية", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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