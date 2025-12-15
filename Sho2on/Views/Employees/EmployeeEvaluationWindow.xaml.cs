using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;

namespace HR_Application.Views
{
    public partial class EmployeeEvaluationWindow : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);
        private int _employeeId;
        private ObservableCollection<EvaluationCriteriaModel> _administrativeCriteria;
        private ObservableCollection<EvaluationCriteriaModel> _technicalCriteria;
        private bool _isAdministrativeActive = true;

        public EmployeeEvaluationWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;

            // تهيئة القوائم
            _administrativeCriteria = new ObservableCollection<EvaluationCriteriaModel>();
            _technicalCriteria = new ObservableCollection<EvaluationCriteriaModel>();

            // ربط القوائم بعناصر التحكم
            administrativeItemsControl.ItemsSource = _administrativeCriteria;
            technicalItemsControl.ItemsSource = _technicalCriteria;

            LoadEmployeeInfo();
            CalculateStatistics();
        }

        private async Task LoadEvaluationData()
        {
            try
            {
                var evaluation = await _context.EmployeeEvaluations
                    .Include(ev => ev.EvaluationCriterias)
                    .FirstOrDefaultAsync(ev => ev.EmployeeId == _employeeId);

                if (evaluation != null)
                {
                    // تفريغ القوائم الحالية
                    _administrativeCriteria.Clear();
                    _technicalCriteria.Clear();

                    // فصل المعايير الإدارية والفنية
                    foreach (var criteria in evaluation.EvaluationCriterias)
                    {
                        var criteriaModel = new EvaluationCriteriaModel
                        {
                            Id = criteria.Id,
                            Name = criteria.CriteriaName,
                            Index = criteria.OrderIndex,
                            GroupName = $"CriteriaGroup_{criteria.OrderIndex}",
                            IsSuccessful = criteria.IsSuccessful,
                            Percentage = criteria.Score,
                            Notes = criteria.Notes
                        };

                        // تحديد نوع المعيار (بناءً على اسمه أو أي معيار آخر)
                        if (criteria.EvaluationType == EvaluationType.Technical)
                        {
                            _technicalCriteria.Add(criteriaModel);
                        }
                        else
                        {
                            _administrativeCriteria.Add(criteriaModel);
                        }
                    }

                    // تحميل الملاحظات
                    txtGeneralNotes.Text = evaluation.GeneralNotes;
                    txtAdministrativeNotes.Text = evaluation.AdministrativeNotes ?? "";
                    txtTechnicalNotes.Text = evaluation.TechnicalNotes ?? "";

                    // تحديث الإحصائيات
                    CalculateStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات التقييم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadEmployeeInfo()
        {
            try
            {
                var employee = _context.Users
                    .FirstOrDefault(e => e.Id == _employeeId);

                if (employee != null)
                {
                    txtEmployeeName.Text = employee.FullName;
                    txtEmployeeCode.Text = $"كود: {employee.Id}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الموظف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAdministrativeCriteria_Click(object sender, RoutedEventArgs e)
        {
            AddCriteria(_administrativeCriteria, txtNewAdministrativeCriteria, "إداري");
        }

        private void AddTechnicalCriteria_Click(object sender, RoutedEventArgs e)
        {
            AddCriteria(_technicalCriteria, txtNewTechnicalCriteria, "فني");
        }

        private void AddCriteria(ObservableCollection<EvaluationCriteriaModel> criteriaList, TextBox textBox, string type)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                MessageBox.Show($"يرجى إدخال اسم المعيار {type}", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            criteriaList.Add(new EvaluationCriteriaModel
            {
                Id = criteriaList.Count + 1,
                Name = textBox.Text,
                Index = criteriaList.Count + 1,
                GroupName = $"{type}_Group_{criteriaList.Count}",
                IsSuccessful = false,
                Percentage = 0,
                Notes = ""
            });

            textBox.Clear();
            CalculateStatistics();
        }

        private void DeleteAdministrativeCriteria_Click(object sender, RoutedEventArgs e)
        {
            DeleteCriteria(sender, _administrativeCriteria);
        }

        private void DeleteTechnicalCriteria_Click(object sender, RoutedEventArgs e)
        {
            DeleteCriteria(sender, _technicalCriteria);
        }

        private void DeleteCriteria(object sender, ObservableCollection<EvaluationCriteriaModel> criteriaList)
        {
            var button = sender as Button;
            var criteria = button?.Tag as EvaluationCriteriaModel;

            if (criteria != null)
            {
                criteriaList.Remove(criteria);
                // إعادة ترقيم العناصر
                for (int i = 0; i < criteriaList.Count; i++)
                {
                    criteriaList[i].Index = i + 1;
                }
                CalculateStatistics();
            }
        }

        private void Percentage_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var criteria = textBox?.Tag as EvaluationCriteriaModel;

            if (criteria != null)
            {
                if (decimal.TryParse(textBox.Text, out decimal percentage))
                {
                    // التأكد من أن النسبة بين 0 و 100
                    criteria.Percentage = Math.Max(0, Math.Min(100, percentage));

                    // تحديث حالة النجاح بناءً على النسبة (50% فما فوق تعتبر ناجحة)
                    criteria.IsSuccessful = criteria.Percentage >= 50;
                }
                CalculateStatistics();
            }
        }

        private void CriteriaName_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateStatistics();
        }

        private void Notes_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateStatistics();
        }

        private void CriteriaRadio_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            var criteria = radio?.Tag as EvaluationCriteriaModel;

            if (criteria != null)
            {
                criteria.IsSuccessful = radio.Content.ToString() == "موفق";
                CalculateStatistics();
            }
        }

        private void EvaluationType_Changed(object sender, RoutedEventArgs e)
        {
            // تحديث حالة التبويب النشط
            _isAdministrativeActive = rbAdministrative.IsChecked == true;

            // يمكنك إضافة منطق إضافي هنا إذا أردت
        }

        private void FinalResult_Checked(object sender, RoutedEventArgs e)
        {
            CalculateStatistics();
        }

        private void CalculateStatistics()
        {
            // حساب إحصائيات التقييم الإداري
            CalculateSectionStatistics(_administrativeCriteria,
                txtAdministrativeTotal,
                txtAdministrativeSuccess,
                txtAdministrativeUnsuccess,
                txtAdministrativePercentage);

            // حساب إحصائيات التقييم الفني
            CalculateSectionStatistics(_technicalCriteria,
                txtTechnicalTotal,
                txtTechnicalSuccess,
                txtTechnicalUnsuccess,
                txtTechnicalPercentage);

            // حساب النسبة الإجمالية (متوسط النسبتين)
            decimal administrativePercentage = GetAveragePercentage(_administrativeCriteria);
            decimal technicalPercentage = GetAveragePercentage(_technicalCriteria);
            decimal finalPercentage = (administrativePercentage + technicalPercentage) / 2;

            // تحديث النسبة الإجمالية
            txtFinalPercentage.Text = $"{finalPercentage:F1}%";
            UpdatePercentageColor(txtFinalPercentage, finalPercentage);

            // تحديث النتيجة النهائية تلقائياً بناءً على النسبة الإجمالية
            if (finalPercentage >= 50)
            {
                if (!rbSuccessful.IsChecked == true)
                    rbSuccessful.IsChecked = true;
            }
            else
            {
                if (!rbUnsuccessful.IsChecked == true)
                    rbUnsuccessful.IsChecked = true;
            }
        }

        private void CalculateSectionStatistics(
            ObservableCollection<EvaluationCriteriaModel> criteriaList,
            TextBlock totalText, TextBlock successText,
            TextBlock unsuccessText, TextBlock percentageText)
        {
            if (criteriaList == null || criteriaList.Count == 0)
            {
                totalText.Text = "0";
                successText.Text = "0";
                unsuccessText.Text = "0";
                percentageText.Text = "0%";
                return;
            }

            var totalCount = criteriaList.Count;
            var successfulCount = criteriaList.Count(c => c.IsSuccessful);
            var unsuccessfulCount = criteriaList.Count(c => !c.IsSuccessful);
            var averagePercentage = GetAveragePercentage(criteriaList);

            totalText.Text = totalCount.ToString();
            successText.Text = successfulCount.ToString();
            unsuccessText.Text = unsuccessfulCount.ToString();
            percentageText.Text = $"{averagePercentage:F1}%";

            UpdatePercentageColor(percentageText, averagePercentage);
        }

        private decimal GetAveragePercentage(ObservableCollection<EvaluationCriteriaModel> criteriaList)
        {
            if (criteriaList == null || criteriaList.Count == 0)
                return 0;

            return criteriaList.Average(c => c.Percentage);
        }

        private void UpdatePercentageColor(TextBlock textBlock, decimal percentage)
        {
            if (percentage >= 50)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                textBlock.FontWeight = FontWeights.Bold;
            }
            else
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                textBlock.FontWeight = FontWeights.Bold;
            }
        }

        // التحقق من إدخال النسب فقط (أرقام ونقطة)
        private void PercentageValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text + e.Text;

            // السماح فقط بالأرقام والنقطة
            e.Handled = !IsPercentageAllowed(newText);
        }

        private static bool IsPercentageAllowed(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;

            // السماح فقط بالأرقام والنقطة
            if (text.Count(c => c == '.') > 1) return false;

            // التحقق من أن القيمة بين 0 و 100
            if (decimal.TryParse(text, out decimal value))
            {
                return value >= 0 && value <= 100;
            }

            return text.All(c => char.IsDigit(c) || c == '.');
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_administrativeCriteria.Count == 0 && _technicalCriteria.Count == 0)
            {
                MessageBox.Show("يرجى إضافة معايير تقييم على الأقل", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!rbSuccessful.IsChecked.HasValue && !rbUnsuccessful.IsChecked.HasValue)
            {
                MessageBox.Show("يرجى تحديد النتيجة النهائية", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // حذف التقييمات القديمة
                _context.EmployeeEvaluations.RemoveRange(
                    _context.EmployeeEvaluations.Where(ev => ev.EmployeeId == _employeeId));

                // حساب النسب
                decimal administrativePercentage = GetAveragePercentage(_administrativeCriteria);
                decimal technicalPercentage = GetAveragePercentage(_technicalCriteria);
                decimal finalPercentage = (administrativePercentage + technicalPercentage) / 2;

                // إنشاء تقييم جديد
                var evaluation = new EmployeeEvaluation
                {
                    EmployeeId = _employeeId,
                    EvaluatorId = App.CurrentUser?.Id ?? 1,
                    EvaluationDate = DateTime.Now,
                    Status = EvaluationStatus.Completed,
                    TotalScore = finalPercentage,
                    MaxPossibleScore = 100,
                    SuccessPercentage = finalPercentage,
                    FinalResult = rbSuccessful.IsChecked == true ? EvaluationResult.Successful : EvaluationResult.Unsuccessful,
                    GeneralNotes = txtGeneralNotes.Text,
                    AdministrativeNotes = txtAdministrativeNotes.Text,
                    TechnicalNotes = txtTechnicalNotes.Text,
                    AdministrativeScore = administrativePercentage,
                    TechnicalScore = technicalPercentage
                };

                // دمج المعايير الإدارية والفنية
                var allCriteria = new List<EvaluationCriteria>();

                // إضافة المعايير الإدارية
                int orderIndex = 1;
                foreach (var criteria in _administrativeCriteria)
                {
                    allCriteria.Add(new EvaluationCriteria
                    {
                        CriteriaName = criteria.Name,
                        Score = criteria.Percentage,
                        MaxScore = 100,
                        IsSuccessful = criteria.IsSuccessful,
                        Notes = criteria.Notes,
                        OrderIndex = orderIndex++,
                        EvaluationType = EvaluationType.Administrative
                    });
                }

                // إضافة المعايير الفنية
                foreach (var criteria in _technicalCriteria)
                {
                    allCriteria.Add(new EvaluationCriteria
                    {
                        CriteriaName = criteria.Name,
                        Score = criteria.Percentage,
                        MaxScore = 100,
                        IsSuccessful = criteria.IsSuccessful,
                        Notes = criteria.Notes,
                        OrderIndex = orderIndex++,
                        EvaluationType = EvaluationType.Technical
                    });
                }

                evaluation.EvaluationCriterias = allCriteria;
                _context.EmployeeEvaluations.Add(evaluation);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ التقييم بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ التقييم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadEvaluationData();
        }
    }

    // نموذج معايير التقييم
    public class EvaluationCriteriaModel : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name;
        private bool _isSuccessful;
        private decimal _percentage;
        private string _notes;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public int Index { get; set; }
        public string GroupName { get; set; }

        public bool IsSuccessful
        {
            get => _isSuccessful;
            set
            {
                _isSuccessful = value;
                OnPropertyChanged(nameof(IsSuccessful));
                OnPropertyChanged(nameof(IsUnsuccessful));
            }
        }

        public decimal Percentage
        {
            get => _percentage;
            set
            {
                _percentage = value;
                OnPropertyChanged(nameof(Percentage));
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        // خاصية محسوبة للعرض
        public bool IsUnsuccessful
        {
            get => !IsSuccessful;
            set
            {
                IsSuccessful = !value;
                OnPropertyChanged(nameof(IsUnsuccessful));
            }
        }

        public Brush RowColor => Index % 2 == 0 ?
            new SolidColorBrush(Color.FromArgb(15, 0, 0, 0)) :
            Brushes.Transparent;

        // تنفيذ INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
    
}