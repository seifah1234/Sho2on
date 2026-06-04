using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace HR_Application
{
    public partial class AddLate : Window
    {
        private readonly AppDbContext _context = new AppDbContext(App.ConnectionString);
        public ObservableCollection<LateOvertime> Lates { get; set; } = new ObservableCollection<LateOvertime>();
        public ObservableCollection<LateOvertime> LatesMoney { get; set; } = new ObservableCollection<LateOvertime>();

        private int _type = 1; // 1: ≈÷«›Ì, 0:  √ŒÌ—
        private int _currentTab = 0; // 0: œﬁ«∆ﬁ, 1: „«·Ì…
        int type;

        public AddLate()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();

            delay_repeat.Text = Properties.Settings.Default.LateRepeat.ToString();
            delay_value.Text = Properties.Settings.Default.LateValue.ToString();

            if (Properties.Settings.Default.LateType.ToString() == "1")
            {
                moneyBtn.IsChecked = true;
                minuteBtn.IsChecked = false;
            }
            else
            {
                minuteBtn.IsChecked = true;
                moneyBtn.IsChecked = false;
            }
        }

        private async Task LoadData()
        {
            try
            {
                Lates.Clear();
                LatesMoney.Clear();

                //  Õ„Ì· »Ì«‰«  «·œﬁ«∆ﬁ (MoneyType = 0)
                var minutesData = await _context.LateOvertimes
                    .Where(x => x.MoneyType == 0)
                    .OrderBy(x => x.StartTime)
                    .ToListAsync();

                foreach (var item in minutesData)
                {
                    Lates.Add(item);
                }

                //  Õ„Ì· »Ì«‰«  «·„«·Ì… (MoneyType = 1)
                var moneyData = await _context.LateOvertimes
                    .Where(x => x.MoneyType == 1)
                    .OrderBy(x => x.StartTime)
                    .ToListAsync();

                foreach (var item in moneyData)
                {
                    LatesMoney.Add(item);
                }

                list.ItemsSource = Lates;
                listMoney.ItemsSource = LatesMoney;
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√ ›Ì  Õ„Ì· «·»Ì«‰« : {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _type = 1; // ≈÷«›Ì
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            _type = 0; //  √ŒÌ—
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            await SaveData(0); // 0: œﬁ«∆ﬁ
        }

        private async void saveMoney_Btn(object sender, RoutedEventArgs e)
        {
            await SaveData(1); // 1: „«·Ì…
        }

        private async Task SaveData(int moneyType)
        {
            try
            {
                var fromTimePicker = moneyType == 0 ? FromTimePicker : FromTimePickerMoney;
                var toTimePicker = moneyType == 0 ? ToTimePicker : ToTimePickerMoney;
                var valueTextBox = moneyType == 0 ? ValueMoneyTextBox : ValueMoneyMoneyTextBox;

                if (string.IsNullOrEmpty(fromTimePicker.Text) || string.IsNullOrEmpty(toTimePicker.Text))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— Êﬁ  «·»œ«Ì… Ê«·‰Â«Ì…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(valueTextBox.Text))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· «·ﬁÌ„…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(valueTextBox.Text, out decimal value))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· ﬁÌ„… —ﬁ„Ì… ’ÕÌÕ…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(fromTimePicker.Text, out TimeSpan fromTime) ||
                    !TimeSpan.TryParse(toTimePicker.Text, out TimeSpan toTime))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· Êﬁ  ’«·Õ", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }



                if (fromTime >= toTime)
                {
                    LocalizationManager.ShowMessage("Êﬁ  «·»œ«Ì… ÌÃ» √‰ ÌﬂÊ‰ ﬁ»· Êﬁ  «·‰Â«Ì…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var lateOvertime = new LateOvertime
                {
                    Name = $"{fromTime:hh\\:mm} - {toTime:hh\\:mm}",
                    StartTime = fromTime,
                    EndTime = toTime,
                    Value = value,
                    Type = _type,
                    MoneyType = moneyType,
                    CreatedAt = DateTime.Now,
                    EditedAt = DateTime.Now
                };

                await _context.LateOvertimes.AddAsync(lateOvertime);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage(" „ Õ›Ÿ «·»Ì«‰«  »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
                ClearForm(moneyType);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «·Õ›Ÿ: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            await EditData(0); // 0: œﬁ«∆ﬁ
        }

        private async void editMoney_Btn(object sender, RoutedEventArgs e)
        {
            await EditData(1); // 1: „«·Ì…
        }

        private async Task EditData(int moneyType)
        {
            try
            {
                var selectedItem = moneyType == 0 ? list.SelectedItem as LateOvertime : listMoney.SelectedItem as LateOvertime;

                if (selectedItem == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— ⁄‰’— ·· ⁄œÌ·", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fromTimePicker = moneyType == 0 ? FromTimePicker : FromTimePickerMoney;
                var toTimePicker = moneyType == 0 ? ToTimePicker : ToTimePickerMoney;
                var valueTextBox = moneyType == 0 ? ValueMoneyTextBox : ValueMoneyMoneyTextBox;

                if (string.IsNullOrEmpty(fromTimePicker.Text) || string.IsNullOrEmpty(toTimePicker.Text))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— Êﬁ  «·»œ«Ì… Ê«·‰Â«Ì…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(valueTextBox.Text))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· «·ﬁÌ„…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(valueTextBox.Text, out decimal value))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· ﬁÌ„… —ﬁ„Ì… ’ÕÌÕ…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(fromTimePicker.Text, out TimeSpan fromTime) ||
                    !TimeSpan.TryParse(toTimePicker.Text, out TimeSpan toTime))
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ ≈œŒ«· Êﬁ  ’«·Õ", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fromTime >= toTime)
                {
                    LocalizationManager.ShowMessage("Êﬁ  «·»œ«Ì… ÌÃ» √‰ ÌﬂÊ‰ ﬁ»· Êﬁ  «·‰Â«Ì…", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //  ÕœÌÀ «·»Ì«‰« 
                selectedItem.Name = $"{fromTime:hh\\:mm} - {toTime:hh\\:mm}";
                selectedItem.StartTime = fromTime;
                selectedItem.EndTime = toTime;
                selectedItem.Value = value;
                selectedItem.Type = _type;
                selectedItem.EditedAt = DateTime.Now;

                _context.LateOvertimes.Update(selectedItem);
                await _context.SaveChangesAsync();

                LocalizationManager.ShowMessage(" „  ⁄œÌ· «·»Ì«‰«  »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadData();
                ClearForm(moneyType);
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «· ⁄œÌ·: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            await DeleteData(0); // 0: œﬁ«∆ﬁ
        }

        private async void deleteMoney_Btn(object sender, RoutedEventArgs e)
        {
            await DeleteData(1); // 1: „«·Ì…
        }

        private async Task DeleteData(int moneyType)
        {
            try
            {
                var selectedItem = moneyType == 0 ? list.SelectedItem as LateOvertime : listMoney.SelectedItem as LateOvertime;

                if (selectedItem == null)
                {
                    LocalizationManager.ShowMessage("Ì—ÃÏ «Œ Ì«— ⁄‰’— ··Õ–›", " Õ–Ì—", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = LocalizationManager.ShowMessage(
                    $"Â· √‰  „ √ﬂœ „‰ Õ–› '{selectedItem.Name}'ø",
                    " √ﬂÌœ «·Õ–›",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _context.LateOvertimes.Remove(selectedItem);
                    await _context.SaveChangesAsync();

                    LocalizationManager.ShowMessage(" „ Õ–› «·»Ì«‰«  »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadData();
                    ClearForm(moneyType);
                }
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"ÕœÀ Œÿ√ √À‰«¡ «·Õ–›: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exit_Btn(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void exitMoney_Btn(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearForm(int moneyType)
        {
            var fromTimePicker = moneyType == 0 ? FromTimePicker : FromTimePickerMoney;
            var toTimePicker = moneyType == 0 ? ToTimePicker : ToTimePickerMoney;
            var valueTextBox = moneyType == 0 ? ValueMoneyTextBox : ValueMoneyMoneyTextBox;

            fromTimePicker.Clear();
            toTimePicker.Clear();
            valueTextBox.Clear();
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is LateOvertime selectedItem)
            {
                FillFormWithData(selectedItem, 0);
            }
        }

        private void listMoney_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listMoney.SelectedItem is LateOvertime selectedItem)
            {
                FillFormWithData(selectedItem, 1);
            }
        }

        private void FillFormWithData(LateOvertime item, int moneyType)
        {
            var fromTimePicker = moneyType == 0 ? FromTimePicker : FromTimePickerMoney;
            var toTimePicker = moneyType == 0 ? ToTimePicker : ToTimePickerMoney;
            var valueTextBox = moneyType == 0 ? ValueMoneyTextBox : ValueMoneyMoneyTextBox;

            fromTimePicker.Text = item.StartTime.ToString();
            toTimePicker.Text = item.EndTime.ToString();
            valueTextBox.Text = item.Value.ToString();

            //  ÕœÌœ ‰Ê⁄ «· √ŒÌ—/«·≈÷«›Ì
            if (item.Type == 1) // ≈÷«›Ì
            {
                var radioButton = moneyType == 0 ?
                    FindVisualChild<RadioButton>(this, "RadioButton_Add") :
                    FindVisualChild<RadioButton>(this, "RadioButton_Add_Money");
                radioButton.IsChecked = true;
            }
            else //  √ŒÌ—
            {
                var radioButton = moneyType == 0 ?
                    FindVisualChild<RadioButton>(this, "RadioButton_Late") :
                    FindVisualChild<RadioButton>(this, "RadioButton_Late_Money");
                radioButton.IsChecked = true;
            }
        }

        // œ«·… „”«⁄œ… ··⁄ÀÊ— ⁄·Ï ⁄‰«’— Ê«ÃÂ… «·„” Œœ„
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T frameworkElement && frameworkElement.Name == name)
                {
                    return frameworkElement;
                }
                else
                {
                    T result = FindVisualChild<T>(child, name);
                    if (result != null) return result;
                }
            }
            return null;
        }

        // ≈“«·… «·œÊ«· €Ì— «·„” Œœ„… „‰ «·ﬂÊœ «·ﬁœÌ„
        private void Exit_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
        private void B_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
        private void Exit_Click(object sender, RoutedEventArgs e) { }
        private void Min_Click(object sender, RoutedEventArgs e) { }
        private void Max_Click(object sender, RoutedEventArgs e) { }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.LateType = type;
            Properties.Settings.Default.LateValue = decimal.Parse(delay_value.Text);
            Properties.Settings.Default.LateRepeat = int.Parse(delay_repeat.Text);
            Properties.Settings.Default.Save();
            LocalizationManager.ShowMessage("Settings saved successfully!");

        }


        private void minuteBtn_Checked(object sender, RoutedEventArgs e)
        {
            type = 0;
        }

        private void moneyBtn_Checked(object sender, RoutedEventArgs e)
        {
            type = 1;
        }
    }
}
