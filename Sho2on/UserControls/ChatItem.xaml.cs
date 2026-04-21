using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UserControl = System.Windows.Controls.UserControl;

namespace HR_Application.UserControls
{
    public partial class ChatItem : UserControl, INotifyPropertyChanged
    {
        // Dependency Properties
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(ChatItem),
                new PropertyMetadata("اسم المستحدم"));

        public static readonly DependencyProperty LastMessageProperty =
            DependencyProperty.Register(nameof(LastMessage), typeof(string), typeof(ChatItem),
                new PropertyMetadata("اخر رسالة"));

        public static readonly DependencyProperty LastMessageTimeProperty =
            DependencyProperty.Register(nameof(LastMessage), typeof(string), typeof(ChatItem),
                new PropertyMetadata(""));

        public static readonly DependencyProperty UserCodeProperty =
            DependencyProperty.Register(nameof(UserCode), typeof(string), typeof(ChatItem),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ProfileImageDataProperty =
    DependencyProperty.Register(nameof(ProfileImageData), typeof(byte[]), typeof(ChatItem),
        new PropertyMetadata(null, OnProfileImageDataChanged));

        public byte[] ProfileImageData
        {
            get => (byte[])GetValue(ProfileImageDataProperty);
            set => SetValue(ProfileImageDataProperty, value);
        }

        // Properties
        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        public string LastMessage
        {
            get => (string)GetValue(LastMessageProperty);
            set => SetValue(LastMessageProperty, value);
        }

        public DateTime LastMessageTime
        {
            get => (DateTime)GetValue(LastMessageTimeProperty);
            set => SetValue(LastMessageTimeProperty, value);
        }

        public static readonly DependencyProperty ProfileImageProperty =
    DependencyProperty.Register(nameof(ProfileImage), typeof(ImageSource), typeof(ChatItem),
        new PropertyMetadata(null));

        public ImageSource ProfileImage
        {
            get => (ImageSource)GetValue(ProfileImageProperty);
            set => SetValue(ProfileImageProperty, value);
        }

        public string UserCode
        {
            get => (string)GetValue(UserCodeProperty);
            set => SetValue(UserCodeProperty, value);
        }

        public ChatItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static void OnProfileImageDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChatItem;
            if (control != null && e.NewValue is byte[] imageData)
            {
                control.ProfileImage = ConvertByteArrayToImageSource(imageData);
            }
        }

        private static ImageSource ConvertByteArrayToImageSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return new BitmapImage(new Uri("/assets/images/avatar.jpg", UriKind.Relative));

            try
            {
                using (var stream = new System.IO.MemoryStream(imageData))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                return new BitmapImage(new Uri("/assets/images/avatar.jpg", UriKind.Relative));
            }
        }
    }
}