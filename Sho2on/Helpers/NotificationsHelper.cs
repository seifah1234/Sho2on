using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System; using HR_Application.Helpers;
using System.IO;
using System.Media;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application.Helpers
{
    public static class NotificationsHelper
    {
        private static readonly string SoundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sounds", "notification.wav");

        // ÊÔÛíá ÕæÊ ÇáÅÔÚÇÑ
        public static void PlayNotificationSound()
        {
            try
            {
                if (File.Exists(SoundPath))
                {
                    using (var player = new SoundPlayer(SoundPath))
                    {
                        player.Play();
                    }
                }
                else
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            catch { }
        }

        // ÅÙåÇÑ Toast Notification
        public static void ShowToastNotification(string title, string message, Action onClickAction = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // ÅíÌÇÏ ÇáäÇÝÐÉ ÇáäÔØÉ
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow is MetroWindow metroWindow)
                {
                    // ÇÓÊÎÏÇã MahApps Metro Toast
                    metroWindow.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = "ÚÑÖ",
                        DialogMessageFontSize = 14,
                        DialogTitleFontSize = 16,
                        ColorScheme = MetroDialogColorScheme.Accented
                    }).ContinueWith(task =>
                    {
                        if (task.Result == MessageDialogResult.Affirmative && onClickAction != null)
                        {
                            onClickAction();
                        }
                    });
                }
                else
                {
                    // ÇÓÊÎÏÇã MessageBox ÇáÚÇÏí ßÈÏíá
                    LocalizationManager.ShowMessage(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }

        // ÅÙåÇÑ Popup ÕÛíÑ Ýí ÇáÒÇæíÉ
        public static void ShowPopupNotification(string title, string message, Window owner, Action onClickAction = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var popup = new Popup
                {
                    PlacementTarget = owner,
                    Placement = PlacementMode.Absolute, // Ãæ Absolute
                    AllowsTransparency = true,
                    HorizontalOffset = owner.Width - 10, // 320 åæ ÚÑÖ ÇáÈæÈ???
                    VerticalOffset = 10, // 120 åæ ÇÑÊÝÇÚ ÇáÈæÈ???
                    Margin = new Thickness(20),
                    StaysOpen = false
                };

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(240, 0, 188, 212)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15, 10, 15, 10),
                    Margin = new Thickness(0, 0, 10, 10)
                };

                var stackPanel = new StackPanel();
                var titleText = new System.Windows.Controls.TextBlock
                {
                    Text = title,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                var messageText = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    MaxWidth = 250
                };

                stackPanel.Children.Add(titleText);
                stackPanel.Children.Add(messageText);
                border.Child = stackPanel;

                popup.Child = border;
                popup.IsOpen = true;

                // ÅÛáÇÞ ÇáÜ Popup ÈÚÏ 5 ËæÇäí
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    popup.IsOpen = false;
                };
                timer.Start();

                // ÅÖÇÝÉ ÍÏË ÚäÏ ÇáÖÛØ
                border.MouseLeftButtonUp += (s, e) =>
                {
                    popup.IsOpen = false;
                    onClickAction?.Invoke();
                };
            });
        }
    }
}
