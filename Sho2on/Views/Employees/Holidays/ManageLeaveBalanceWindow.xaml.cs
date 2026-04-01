using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Brushes = System.Drawing.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class ManageLeaveBalanceWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private User _selectedUser;
        private ObservableCollection<LeaveBalanceViewModel> _leaveBalances;
        private List<User> users = new List<User>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ManageLeaveBalanceWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _leaveBalances = new ObservableCollection<LeaveBalanceViewModel>();
            DataContext = this;
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                LoadLeaveBalances();
                UpdateEmployeeInfo();
            }
        }

        public ObservableCollection<LeaveBalanceViewModel> LeaveBalances
        {
            get => _leaveBalances;
            set
            {
                _leaveBalances = value;
                OnPropertyChanged(nameof(LeaveBalances));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                var query = _context.Users
                    .Include(u => u.Department)
                    .Include(u => u.Branch)
                    .Include(u => u.JobTitle)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(txtSearchEmployeeId.Text))
                {
                    query = query.Where(u => u.Code == txtSearchEmployeeId.Text);
                }

                var users = await query.Take(10).ToListAsync();

                if (users.Count == 1)
                {
                    SelectedUser = users.First();
                    txtSearchEmployeeId.Text = SelectedUser.Code.ToString();
                    user_box.SelectedValue = SelectedUser.Code;
                    
                }
                else if (users.Count > 1)
                {
                    var selectionWindow = new EmployeeSelectionWindow(users);
                    selectionWindow.Owner = this;

                    if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedUser != null)
                    {
                        SelectedUser = selectionWindow.SelectedUser;
                        txtSearchEmployeeId.Text = SelectedUser.Code.ToString();
                        user_box.SelectedValue = SelectedUser.Code;
                    }
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على موظفين", "بحث", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (user_box.SelectedItem is User selectedUser)
            {
                txtSearchEmployeeId.Text = user_box.SelectedValue.ToString();
                _selectedUser = selectedUser;
            }

        }

        private void searchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            var textBox = (System.Windows.Controls.TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);

            textBox.TextChanged -= searchComboBox_TextChanged;
            textBox.TextChanged += searchComboBox_TextChanged;
        }

        private void searchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            var comboBox = FindParent<System.Windows.Controls.ComboBox>(textBox);
            var searchText = textBox.Text;

            var itemsList = comboBox.Tag as List<User>;

            switch (comboBox.Name)
            {
                case "user_box":
                    itemsList = users;
                    break;
            }

            if (itemsList == null)
                return;

            if (string.IsNullOrEmpty(searchText))
            {
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = itemsList;
            }
            else
            {
                var filteredItems = itemsList
                    .Where(item => item.FullName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = filteredItems;
            }

            comboBox.IsDropDownOpen = true;
            textBox.Text = searchText;
            textBox.CaretIndex = searchText.Length;
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            while (parentObject != null)
            {
                if (parentObject is T parent)
                {
                    return parent;
                }
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }

        private async void LoadLeaveBalances()
        {
            try
            {
                if (SelectedUser == null)
                {
                    LeaveBalances.Clear();
                    dgLeaveBalances.ItemsSource = null;
                    return;
                }

                // الحصول على أنواع الإجازات النشطة
                var leaveTypes = await _context.LeaveTypes
                    .Where(lt => lt.IsActive)
                    .OrderBy(lt => lt.Name)
                    .ToListAsync();

                var balances = new List<LeaveBalanceViewModel>();

                foreach (var leaveType in leaveTypes)
                {
                    // البحث عن رصيد الإجازة الحالي
                    var existingBalance = await _context.LeaveBalances
                        .FirstOrDefaultAsync(lb => lb.UserId == SelectedUser.Id &&
                                                  lb.LeaveTypeId == leaveType.Id);

                    var usedBalance = await _context.Leaves
                        .Where(l => l.UserId == SelectedUser.Id &&
                                   l.LeaveTypeId == leaveType.Id &&
                                   l.Status == 2) // الموافق عليها فقط
                        .SumAsync(l => (int?)l.Duration) ?? 0;

                    var viewModel = new LeaveBalanceViewModel
                    {
                        Id = existingBalance?.Id ?? 0,
                        UserId = SelectedUser.Id,
                        LeaveTypeId = leaveType.Id,
                        LeaveTypeName = leaveType.Name,
                        LeaveTypeCode = leaveType.Code,
                        TotalBalance = existingBalance?.TotalBalance ?? leaveType.DefaultBalance,
                        UsedBalance = usedBalance,
                        UpdatedAt = existingBalance?.UpdatedAt ?? DateTime.Now
                    };

                    balances.Add(viewModel);
                }

                LeaveBalances = new ObservableCollection<LeaveBalanceViewModel>(balances);
                dgLeaveBalances.ItemsSource = LeaveBalances;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل رصيد الإجازات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmployeeInfo()
        {
            if (SelectedUser != null)
            {
                txtEmployeeInfo.Text = $"{SelectedUser.FullName} - كود: {SelectedUser.Id}";
                txtEmployeeDetails.Text = $"{SelectedUser.Department?.Name} - {SelectedUser.Branch?.Name}";
                btnClearSelection.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmployeeInfo.Text = "لم يتم اختيار موظف";
                txtEmployeeDetails.Text = "الرجاء البحث عن موظف";
                btnClearSelection.Visibility = Visibility.Collapsed;
            }
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            SelectedUser = null;
            LeaveBalances.Clear();
            txtSearchEmployeeId.Text = string.Empty;
            user_box.SelectedIndex = -1;
            txtGeneralNotes.Text = string.Empty;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedUser == null)
                {
                    MessageBox.Show("الرجاء اختيار موظف أولاً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من صحة البيانات
                if (!ValidateLeaveBalances())
                    return;

                // استخدام استراتيجية التنفيذ الصحيحة للمعاملة
                var executionStrategy = _context.Database.CreateExecutionStrategy();

                await executionStrategy.ExecuteAsync(async () =>
                {
                    // بدء المعاملة باستخدام CreateExecutionStrategy
                    await using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (var balanceVM in LeaveBalances)
                            {
                                if (balanceVM.TotalBalance < balanceVM.UsedBalance)
                                {
                                    await transaction.RollbackAsync();
                                    MessageBox.Show($"الرصيد الكلي للإجازة '{balanceVM.LeaveTypeName}' لا يمكن أن يكون أقل من المستخدم ({balanceVM.UsedBalance})",
                                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                var existingBalance = await _context.LeaveBalances
                                    .FirstOrDefaultAsync(lb => lb.UserId == balanceVM.UserId &&
                                                              lb.LeaveTypeId == balanceVM.LeaveTypeId);

                                if (existingBalance == null)
                                {
                                    // إنشاء رصيد جديد
                                    var newBalance = new LeaveBalance
                                    {
                                        UserId = balanceVM.UserId,
                                        LeaveTypeId = balanceVM.LeaveTypeId,
                                        TotalBalance = balanceVM.TotalBalance,
                                        UsedBalance = balanceVM.UsedBalance,
                                        CreatedAt = DateTime.Now,
                                        UpdatedAt = DateTime.Now
                                    };
                                    _context.LeaveBalances.Add(newBalance);
                                }
                                else
                                {
                                    // تحديث الرصيد الموجود
                                    existingBalance.TotalBalance = balanceVM.TotalBalance;
                                    existingBalance.UpdatedAt = DateTime.Now;
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            MessageBox.Show("تم حفظ رصيد الإجازات بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                await transaction.RollbackAsync();
                            }
                            catch (Exception rollbackEx)
                            {
                                // تجاهل خطأ التراجع إذا فشل
                            }

                            MessageBox.Show($"خطأ في الحفظ: {ex.InnerException}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                            throw;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateLeaveBalances()
        {
            foreach (var balance in LeaveBalances)
            {
                if (balance.TotalBalance < 0)
                {
                    MessageBox.Show($"قيمة الرصيد للإجازة '{balance.LeaveTypeName}' يجب أن تكون عدداً صحيحاً موجباً",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد إعادة تعيين جميع الأرصيد إلى القيم الافتراضية؟",
                "تأكيد إعادة التعيين", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LoadLeaveBalances();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            var _employees = _context.Users.Include(e => e.Shift).Include(e => e.Manager).ToList();

            users.AddRange(_employees);
            user_box.ItemsSource = users;
        }
    }

    // ViewModel لرصيد الإجازات
    public class LeaveBalanceViewModel : INotifyPropertyChanged
    {
        private int _totalBalance;
        private string _notes;

        public int Id { get; set; }
        public int UserId { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveTypeCode { get; set; }

        public int TotalBalance
        {
            get => _totalBalance;
            set
            {
                _totalBalance = value;
                OnPropertyChanged(nameof(TotalBalance));
                OnPropertyChanged(nameof(RemainingBalance));
            }
        }

        public int UsedBalance { get; set; }

        public int RemainingBalance => TotalBalance - UsedBalance;

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public DateTime UpdatedAt { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Converter للون الرصيد
    public class BalanceToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int remaining && values[1] is int total)
            {
                if (total == 0) return Brushes.Gray;

                double percentage = (double)remaining / total;

                if (percentage >= 0.5) return Brushes.Green;
                if (percentage >= 0.25) return Brushes.Orange;
                return Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}