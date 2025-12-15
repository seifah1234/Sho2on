using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for MachineWindow.xaml
    /// </summary>
    public partial class MachineWindow : Window
    {
        private readonly AppDbContext _context;
        private ObservableCollection<MachineViewModel> _machines = new ObservableCollection<MachineViewModel>();
        private ObservableCollection<Branch> _branches = new ObservableCollection<Branch>();
        private MachineViewModel _selectedMachine;

        public MachineWindow()
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadBranchesAsync();
                await LoadMachinesAsync();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadBranchesAsync()
        {
            try
            {
                branch_box.Items.Clear();
                _branches.Clear();

                var branches = await _context.Branches
                    .Where(b => App.userBranches.Contains(b.Id))
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    _branches.Add(branch);
                    branch_box.Items.Add(branch.Name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفروع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMachinesAsync()
        {
            try
            {
                _machines.Clear();

                var machines = await _context.Machines
                    .Include(m => m.Branch)
                    .Where(m => App.userBranches.Contains(m.BranchId))
                    .OrderBy(m => m.Branch.Name)
                    .ThenBy(m => m.MIP)
                    .ToListAsync();

                int rowNumber = 1;
                foreach (var machine in machines)
                {
                    var machineVM = new MachineViewModel
                    {
                        RowNumber = rowNumber++,
                        Id = machine.Id,
                        Code = machine.BranchId,
                        Branch = machine.Branch.Name,
                        MIP = machine.MIP,
                        SIP = machine.SIP
                    };
                    _machines.Add(machineVM);
                }

                list.ItemsSource = _machines;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الأجهزة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetForm()
        {
            branch_box.Text = "";
            machine_num.Clear();
            server_num.Clear();
            _selectedMachine = null;
        }

        public class MachineViewModel
        {
            public int RowNumber { get; set; }
            public int Id { get; set; }
            public int Code { get; set; }
            public string Branch { get; set; }
            public string MIP { get; set; }
            public string SIP { get; set; }
        }

        private void B_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        private async void save_Btn(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(branch_box.Text) ||
                    string.IsNullOrWhiteSpace(machine_num.Text) ||
                    string.IsNullOrWhiteSpace(server_num.Text))
                {
                    MessageBox.Show("يرجى ملء جميع الحقول", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedBranch = _branches.FirstOrDefault(b => b.Name == branch_box.Text);
                if (selectedBranch == null)
                {
                    MessageBox.Show("الفرع المحدد غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التحقق من عدم تكرار عنوان IP للجهاز
                bool machineExists = await _context.Machines
                    .AnyAsync(m => m.MIP == machine_num.Text && m.BranchId == selectedBranch.Id);

                if (machineExists)
                {
                    MessageBox.Show("هذا الجهاز مضاف مسبقاً لهذا الفرع", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var machine = new Machine
                {
                    BranchId = selectedBranch.Id,
                    MIP = machine_num.Text.Trim(),
                    SIP = server_num.Text.Trim()
                };

                await _context.Machines.AddAsync(machine);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم إضافة الجهاز بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exit_Btn(object sender, EventArgs e)
        {
            Close();
        }

        private void list_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedItem is MachineViewModel selectedMachine)
            {
                _selectedMachine = selectedMachine;
                machine_num.Text = selectedMachine.MIP;
                branch_box.Text = selectedMachine.Branch;
                server_num.Text = selectedMachine.SIP;
            }
        }

        private async void delete_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedMachine == null)
            {
                MessageBox.Show("لم يتم اختيار جهاز", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الجهاز '{_selectedMachine.MIP}'؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var machine = await _context.Machines.FindAsync(_selectedMachine.Id);
                    if (machine != null)
                    {
                        _context.Machines.Remove(machine);
                        await _context.SaveChangesAsync();

                        MessageBox.Show("تم حذف الجهاز بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void edit_Btn(object sender, RoutedEventArgs e)
        {
            if (_selectedMachine == null)
            {
                MessageBox.Show("لم يتم اختيار جهاز", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(branch_box.Text) ||
                    string.IsNullOrWhiteSpace(machine_num.Text) ||
                    string.IsNullOrWhiteSpace(server_num.Text))
                {
                    MessageBox.Show("يرجى ملء جميع الحقول", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedBranch = _branches.FirstOrDefault(b => b.Name == branch_box.Text);
                if (selectedBranch == null)
                {
                    MessageBox.Show("الفرع المحدد غير موجود", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var machine = await _context.Machines.FindAsync(_selectedMachine.Id);
                if (machine != null)
                {
                    // التحقق من عدم تكرار عنوان IP للجهاز (باستثناء الجهاز الحالي)
                    bool machineExists = await _context.Machines
                        .AnyAsync(m => m.MIP == machine_num.Text &&
                                     m.BranchId == selectedBranch.Id &&
                                     m.Id != _selectedMachine.Id);

                    if (machineExists)
                    {
                        MessageBox.Show("هذا الجهاز مضاف مسبقاً لهذا الفرع", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    machine.BranchId = selectedBranch.Id;
                    machine.MIP = machine_num.Text.Trim();
                    machine.SIP = server_num.Text.Trim();

                    _context.Machines.Update(machine);
                    await _context.SaveChangesAsync();

                    MessageBox.Show("تم تعديل الجهاز بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void reset_Btn(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}