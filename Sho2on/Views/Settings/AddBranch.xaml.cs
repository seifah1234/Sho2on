using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
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
                area_box.SelectedIndex = -1;
                _branches.Clear();

                _branches = _context.Branches.ToList();
                var areas = _context.Areas.ToList();
                area_box.ItemsSource = areas;
                branch_list.ItemsSource = _branches;
            
            }
            catch (Exception e)
            {
                LocalizationManager.ShowMessage(e.Message);
            }
            
        }

        

        private async void save_Branch(object sender, EventArgs e)
        {

            try
            {
                
                string name = name_box.Text;
                int id = int.Parse(code_box.Text);
                int areaId = (area_box.SelectedItem as Area)?.Id ?? 0;
                if (_context.Branches.Any(b => b.Id == id))
                {
                    LocalizationManager.ShowMessage("الفرع موجود بالفعل!");
                    return;
                }
                var branch = new Branch { Id = id, Name = name, AreaId = areaId };
                _context.Branches.Add(branch);
                _context.SaveChanges();

                LocalizationManager.ShowMessage("تم اضافة الفرع", LocalizationManager.Translate("تم"), MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();

            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);

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
                LocalizationManager.ShowMessage("لم يتم اختيار الفرع", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    _context.Branches.Remove(branch);
                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم حذف الفرع", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                catch
                {
                    LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    int areaId = (area_box.SelectedItem as Area)?.Id ?? 0;
                    branch.Name = name;
                    branch.AreaId = areaId;
                    _context.Branches.Update(branch);
                    await  _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم تعديل الفرع", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    LocalizationManager.ShowMessage("لم تختار اي فرع", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void branch_list_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (branch_list.SelectedItem is Branch branch)
            {
                
                name_box.Text = branch.Name;
                area_box.SelectedValue = branch.AreaId;
                code_box.Text = branch.Id.ToString();
                       
            }

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            LoadData();
        }

    }
}

