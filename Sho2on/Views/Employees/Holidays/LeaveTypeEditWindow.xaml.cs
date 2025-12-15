using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Views.Employees.Holidays
{
    public partial class LeaveTypeEditWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _leaveTypeId;
        private LeaveType _leaveType;
        private bool _isEditMode;

        public LeaveTypeEditWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _isEditMode = false;
            InitializeWindow();
        }

        public LeaveTypeEditWindow(int leaveTypeId)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _leaveTypeId = leaveTypeId;
            _isEditMode = true;
            InitializeWindow();
            LoadLeaveTypeData();
        }

        private void InitializeWindow()
        {
            if (!_isEditMode)
            {
                windowTitle.Text = "نوع إجازة جديد";
            }
        }

        private async void LoadLeaveTypeData()
        {
            try
            {
                _leaveType = await _context.LeaveTypes.FindAsync(_leaveTypeId);

                if (_leaveType != null)
                {
                    windowTitle.Text = $"تعديل نوع الإجازة: {_leaveType.Name}";

                    // تعبئة البيانات
                    txtName.Text = _leaveType.Name;
                    txtCode.Text = _leaveType.Code;
                    txtDefaultBalance.Text = _leaveType.DefaultBalance.ToString();
                    txtMaxConsecutiveDays.Text = _leaveType.MaxConsecutiveDays?.ToString() ?? "0";
                    txtNotes.Text = _leaveType.Notes ?? string.Empty;

                    chkIsActive.IsChecked = _leaveType.IsActive;
                    chkDeductFromBalance.IsChecked = _leaveType.DeductFromBalance;
                    chkRequiresApproval.IsChecked = _leaveType.RequiresApproval;
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على نوع الإجازة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_isEditMode)
                {
                    // تحديث نوع الإجازة الموجود
                    _leaveType.Name = txtName.Text.Trim();
                    _leaveType.Code = txtCode.Text.Trim().ToUpper();
                    _leaveType.DefaultBalance = int.Parse(txtDefaultBalance.Text);

                    if (int.TryParse(txtMaxConsecutiveDays.Text, out int maxDays) && maxDays > 0)
                        _leaveType.MaxConsecutiveDays = maxDays;
                    else
                        _leaveType.MaxConsecutiveDays = null;

                    _leaveType.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
                    _leaveType.IsActive = chkIsActive.IsChecked == true;
                    _leaveType.DeductFromBalance = chkDeductFromBalance.IsChecked == true;
                    _leaveType.RequiresApproval = chkRequiresApproval.IsChecked == true;
                    _leaveType.UpdatedAt = DateTime.Now;

                    _context.LeaveTypes.Update(_leaveType);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم تحديث نوع الإجازة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // إنشاء نوع إجازة جديد
                    var newLeaveType = new LeaveType
                    {
                        Name = txtName.Text.Trim(),
                        Code = txtCode.Text.Trim().ToUpper(),
                        DefaultBalance = int.Parse(txtDefaultBalance.Text),
                        IsActive = chkIsActive.IsChecked == true,
                        DeductFromBalance = chkDeductFromBalance.IsChecked == true,
                        RequiresApproval = chkRequiresApproval.IsChecked == true,
                        Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    // التحقق من الحد الأقصى للأيام المتتالية
                    if (int.TryParse(txtMaxConsecutiveDays.Text, out int maxDays) && maxDays > 0)
                        newLeaveType.MaxConsecutiveDays = maxDays;

                    _context.LeaveTypes.Add(newLeaveType);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم إنشاء نوع الإجازة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // التحقق من الاسم
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("الرجاء إدخال اسم نوع الإجازة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            // التحقق من الكود
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                MessageBox.Show("الرجاء إدخال كود النوع", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCode.Focus();
                return false;
            }

            // التحقق من الرصيد الافتراضي
            if (!int.TryParse(txtDefaultBalance.Text, out int defaultBalance) || defaultBalance < 0)
            {
                MessageBox.Show("الرجاء إدخال قيمة صحيحة للرصيد الافتراضي", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDefaultBalance.Focus();
                return false;
            }

            // التحقق من الحد الأقصى للأيام المتتالية
            if (!string.IsNullOrWhiteSpace(txtMaxConsecutiveDays.Text))
            {
                if (!int.TryParse(txtMaxConsecutiveDays.Text, out int maxDays) || maxDays < 0)
                {
                    MessageBox.Show("الرجاء إدخال قيمة صحيحة للحد الأقصى للأيام", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMaxConsecutiveDays.Focus();
                    return false;
                }
            }

            // التحقق من عدم تكرار الكود (في حالة الإضافة فقط)
            if (!_isEditMode)
            {
                var existingCode = _context.LeaveTypes
                    .Any(lt => lt.Code.ToUpper() == txtCode.Text.Trim().ToUpper());

                if (existingCode)
                {
                    MessageBox.Show("هذا الكود مستخدم مسبقاً، الرجاء اختيار كود آخر", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCode.Focus();
                    return false;
                }
            }
            else
            {
                // في حالة التعديل، التحقق من عدم تكرار الكود مع استثناء السجل الحالي
                var existingCode = _context.LeaveTypes
                    .Any(lt => lt.Code.ToUpper() == txtCode.Text.Trim().ToUpper() && lt.Id != _leaveTypeId);

                if (existingCode)
                {
                    MessageBox.Show("هذا الكود مستخدم مسبقاً، الرجاء اختيار كود آخر", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCode.Focus();
                    return false;
                }
            }

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // السماح فقط بالأرقام
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}