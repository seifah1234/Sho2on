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
    public partial class AddArea : Window
    {
        private AppDbContext _context = new AppDbContext(App.ConnectionString);

        private List<Area> _areas= new List<Area>();

        public AddArea()
        {
            InitializeComponent();
        }

        private void LoadData()
        {
            try
            {
                code_box.Clear();
                name_box.Clear();
                _areas.Clear();

                _areas = _context.Areas.ToList();
                area_list.ItemsSource = _areas;
            
            }
            catch (Exception e)
            {
                LocalizationManager.ShowMessage(e.Message);
            }
            
        }

        

        private async void save_Area(object sender, EventArgs e)
        {

            try
            {
                
                string name = name_box.Text;
                if (_context.Areas.Any(b => b.Name == name))
                {
                    LocalizationManager.ShowMessage("المنطقة موجودة بالفعل!");
                    return;
                }
                var area = new Area { Name = name };
                _context.Areas.Add(area);
                _context.SaveChanges();

                LocalizationManager.ShowMessage("تم اضافة المنطقة", LocalizationManager.Translate("تم"), MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();

            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"حدث خطأ: {ex.Message}", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void exit_Area(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void delete_area(object sender, EventArgs e)
        { 
            if (area_list.SelectedItem is not Area area)
            {
                LocalizationManager.ShowMessage("لم يتم اختيار المنطقة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                try
                {
                    _context.Areas.Remove(area);
                    await _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم حذف المنطقة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                catch
                {
                    LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void edit_Area(object sender, EventArgs e)
        {

            try
            {
                if (area_list.SelectedItem is Area area)
                {
                    
                    string name = name_box.Text;
                    area.Name = name;
                    _context.Areas.Update(area);
                    await  _context.SaveChangesAsync();
                    LocalizationManager.ShowMessage("تم تعديل المنطقة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    LocalizationManager.ShowMessage("لم تختار اي منطقة", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch
            {
                LocalizationManager.ShowMessage("حدث خطأ", LocalizationManager.Translate("خطأ"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void area_list_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (area_list.SelectedItem is Area area)
            {
                
                name_box.Text = area.Name;
                code_box.Text = area.Id.ToString();
                       
            }

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            LoadData();
        }

    }
}

