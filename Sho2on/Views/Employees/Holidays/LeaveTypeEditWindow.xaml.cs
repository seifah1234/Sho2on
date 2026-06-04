using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
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
                windowTitle.Text = "‰Ê⁄ ≈Ã«“… ÃœÌœ";
            }
        }

        private async void LoadLeaveTypeData()
        {
            try
            {
                _leaveType = await _context.LeaveTypes.FindAsync(_leaveTypeId);

                if (_leaveType != null)
                {
                    windowTitle.Text = $" ⁄œÌ· ‰Ê⁄ «·≈Ã«“…: {_leaveType.Name}";

                    //  ⁄»∆… «·»Ì«‰« 
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
                    LocalizationManager.ShowMessage("·„ Ì „ «·⁄ÀÊ— ⁄·Ï ‰Ê⁄ «·≈Ã«“…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    //  ÕœÌÀ ‰Ê⁄ «·≈Ã«“… «·„ÊÃÊœ
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

                    LocalizationManager.ShowMessage(" „  ÕœÌÀ ‰Ê⁄ «·≈Ã«“… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // ≈‰‘«¡ ‰Ê⁄ ≈Ã«“… ÃœÌœ
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

                    // «· Õﬁﬁ „‰ «·Õœ «·√ﬁ’Ï ··√Ì«„ «·„  «·Ì…
                    if (int.TryParse(txtMaxConsecutiveDays.Text, out int maxDays) && maxDays > 0)
                        newLeaveType.MaxConsecutiveDays = maxDays;

                    _context.LeaveTypes.Add(newLeaveType);
                    await _context.SaveChangesAsync();

                    LocalizationManager.ShowMessage(" „ ≈‰‘«¡ ‰Ê⁄ «·≈Ã«“… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì Õ›Ÿ «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // «· Õﬁﬁ „‰ «·«”„
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· «”„ ‰Ê⁄ «·≈Ã«“…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            // «· Õﬁﬁ „‰ «·ﬂÊœ
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ﬂÊœ «·‰Ê⁄", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCode.Focus();
                return false;
            }

            // «· Õﬁﬁ „‰ «·—’Ìœ «·«› —«÷Ì
            if (!int.TryParse(txtDefaultBalance.Text, out int defaultBalance) || defaultBalance < 0)
            {
                LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ﬁÌ„… ’ÕÌÕ… ··—’Ìœ «·«› —«÷Ì", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDefaultBalance.Focus();
                return false;
            }

            // «· Õﬁﬁ „‰ «·Õœ «·√ﬁ’Ï ··√Ì«„ «·„  «·Ì…
            if (!string.IsNullOrWhiteSpace(txtMaxConsecutiveDays.Text))
            {
                if (!int.TryParse(txtMaxConsecutiveDays.Text, out int maxDays) || maxDays < 0)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· ﬁÌ„… ’ÕÌÕ… ··Õœ «·√ﬁ’Ï ··√Ì«„", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMaxConsecutiveDays.Focus();
                    return false;
                }
            }

            // «· Õﬁﬁ „‰ ⁄œ„  ﬂ—«— «·ﬂÊœ (›Ì Õ«·… «·≈÷«›… ›ﬁÿ)
            if (!_isEditMode)
            {
                var existingCode = _context.LeaveTypes
                    .Any(lt => lt.Code.ToUpper() == txtCode.Text.Trim().ToUpper());

                if (existingCode)
                {
                    LocalizationManager.ShowMessage("Â–« «·ﬂÊœ „” Œœ„ „”»ﬁ«° «·—Ã«¡ «Œ Ì«— ﬂÊœ ¬Œ—", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCode.Focus();
                    return false;
                }
            }
            else
            {
                // ›Ì Õ«·… «· ⁄œÌ·° «· Õﬁﬁ „‰ ⁄œ„  ﬂ—«— «·ﬂÊœ „⁄ «” À‰«¡ «·”Ã· «·Õ«·Ì
                var existingCode = _context.LeaveTypes
                    .Any(lt => lt.Code.ToUpper() == txtCode.Text.Trim().ToUpper() && lt.Id != _leaveTypeId);

                if (existingCode)
                {
                    LocalizationManager.ShowMessage("Â–« «·ﬂÊœ „” Œœ„ „”»ﬁ«° «·—Ã«¡ «Œ Ì«— ﬂÊœ ¬Œ—", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            // «·”„«Õ ›ﬁÿ »«·√—ﬁ«„
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}
