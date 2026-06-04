using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Globalization;
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

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for StatWindow.xaml
    /// </summary>
    public partial class StatWindow : Window
    {
        public StatWindow(List<StatClass> stat)
        {
            InitializeComponent();
            data_grid.LoadingRow += dataTable_LoadingRow;
            data_grid.ItemsSource = stat;

        }

        private void dataTable_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            e.Row.Header = (e.Row.GetIndex() + 1).ToString(CultureInfo.CurrentCulture);
        }
    }
}
