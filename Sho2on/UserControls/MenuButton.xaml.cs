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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HR_Application.UserControls
{
    /// <summary>
    /// Interaction logic for MenuButton.xaml
    /// </summary>
    public partial class MenuButton : System.Windows.Controls.UserControl
    {

        public event EventHandler ButtonClicked;

        public MenuButton()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(MenuButton));

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(MenuButton));

        private void Border_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            // Raise the CardClicked event when the border is clicked
            OnButtonClicked();
        }

        protected virtual void OnButtonClicked()
        {
            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btn_mouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btn_txt.FontSize = 18;
            btn_border.Width += 2;
            btn_border.Height += 2;
            btn_border.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#acb8d2"));

            btn_txt.Foreground = new SolidColorBrush(Colors.White);

        }

        private void btn_mouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            btn_txt.FontSize = 16;
            btn_border.Width -= 2;
            btn_border.Height -= 2;
            btn_border.Background = new SolidColorBrush(Colors.White);
            btn_txt.Foreground = new SolidColorBrush((Colors.Black));
        }
    }
}
