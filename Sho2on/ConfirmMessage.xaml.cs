using System;
using System.Collections.Generic;
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

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for ConfirmMessage.xaml
    /// </summary>
    public partial class ConfirmMessage : Window
    {
        public bool Result { get; private set; }

        public ConfirmMessage(string message, string okButtonText, string cancelButtonText)
        {
            InitializeComponent();
            MessageText.Text = message;
            OkButton.Content = okButtonText;
            CancelButton.Content = cancelButtonText;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true; // OK was clicked
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false; // Cancel was clicked
            Close();
        }
    }
}
