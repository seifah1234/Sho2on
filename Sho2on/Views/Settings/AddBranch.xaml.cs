using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for AddBranch.xaml
    /// </summary>
    public partial class AddBranch : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);

        private List<Branch> _branches = new List<Branch>();

        public AddBranch()
        {
            InitializeComponent();
        }

        private void LoadData()
        {
            try
            {
                code_box.Clear();
                name_box.Clear();
                _branches.Clear();

                _branches = _context.Branches.ToList();
                branch_list.ItemsSource = _branches;
            
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            
        }

        

        private async void save_Branch(object sender, EventArgs e)
        {

            try
            {
                
                string name = name_box.Text;
                int id = int.Parse(code_box.Text);
                if (_context.Branches.Any(b => b.Id == id))
                {
                    MessageBox.Show("الفرع موجود بالفعل!");
                    return;
                }
                var branch = new Branch { Id = id, Name = name };
                _context.Branches.Add(branch);
                _context.SaveChanges();

                System.Windows.MessageBox.Show("تم اضافة الفرع", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void exit_Branch(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void delete_branch(object sender, EventArgs e)
        { 
            if (branch_list.SelectedItem is not Branch branch)
            {
                System.Windows.MessageBox.Show("لم يتم اختيار الفرع", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    _context.Branches.Remove(branch);
                    await _context.SaveChangesAsync();
                    System.Windows.MessageBox.Show("تم حذف الفرع", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                catch
                {
                    System.Windows.MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void edit_Branch(object sender, EventArgs e)
        {

            try
            {
                if (branch_list.SelectedItem is Branch branch)
                {
                    
                    string name = name_box.Text;
                    branch.Name = name;
                    _context.Branches.Update(branch);
                    await  _context.SaveChangesAsync();
                    System.Windows.MessageBox.Show("تم تعديل الفرع", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    System.Windows.MessageBox.Show("لم تختار اي فرع", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                System.Windows.MessageBox.Show("حدث خطأ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void branch_list_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (branch_list.SelectedItem is Branch branch)
            {
                
                name_box.Text = branch.Name;
                code_box.Text = branch.Id.ToString();
                       
            }

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            LoadData();
        }

    }
}
