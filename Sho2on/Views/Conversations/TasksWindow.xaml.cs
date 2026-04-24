using DocumentFormat.OpenXml.Spreadsheet;
using HR_Application.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace HR_Application.Views.Conversations
{
    /// <summary>
    /// Interaction logic for TasksWindow.xaml
    /// </summary>
    public partial class TasksWindow : Window, INotifyPropertyChanged
    {
        public AppDbContext _context = new AppDbContext(App.ConnectionString);
        public int _taskId = -1;
        private List<User> users = new List<User>();
        private List<UserTask> allUsersTasks = new List<UserTask>();
        private bool _signalRInitialized = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        ObservableCollection<UserTask> _myTasks;
        public ObservableCollection<UserTask> MyTasks
        {
            get { return _myTasks; }
            set
            {
                _myTasks = value;
                OnPropertyChanged(nameof(MyTasks));
            }
        }

        ObservableCollection<UserTask> _assignedTasks;

        public ObservableCollection<UserTask> AssignedTasks
        {
            get { return _assignedTasks; }
            set
            {
                _assignedTasks = value;
                OnPropertyChanged(nameof(AssignedTasks));
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TasksWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += async (s, e) =>
            {
                await SetupSignalRListener();
            };
        }

        // تحميل المهام مع Refresh
        public async Task LoadTasksAsync()
        {
            try
            {
                manageTaskGrid.Visibility = Visibility.Collapsed;
                _taskId = -1;

                allUsersTasks = await _context.UserTasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .ToListAsync();

                var currentUserId = App.CurrentUser.Id;
                var userTasks = allUsersTasks.Where(t => t.AssignedToUserId == currentUserId);
                var assignedTasks = allUsersTasks.Where(t => t.AssignedByUserId == currentUserId);

                // تحديث حالة المهام المستقبلة
                foreach (var task in userTasks.Where(t => t.Status == (int)UserTaskStatus.Sent))
                {
                    task.Status = (int)UserTaskStatus.Received;
                    await _context.SaveChangesAsync();
                }

                MyTasks = new ObservableCollection<UserTask>(userTasks);
                AssignedTasks = new ObservableCollection<UserTask>(assignedTasks);

                // تحديث القوائم المنسدلة
                users = await _context.Users.ToListAsync();
                assignToBox.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing tasks: {ex.Message}");
            }
        }

        // Refresh Tasks (للاستخدام الداخلي)
        public async void RefreshTasks()
        {
            await LoadTasksAsync();
        }

        // إضافة مهمة جديدة مع إشعار SignalR
        public async Task AddTaskAsync(User user)
        {
            try
            {
                var newTask = new UserTask
                {
                    Description = taskDescriptionBox.Text,
                    AssignedByUserId = App.CurrentUser.Id,
                    AssignedToUserId = user.Id,
                    DueDate = dueDatePicker.SelectedDate,
                    CreatedAt = DateTime.Now,
                    Status = (int)UserTaskStatus.Sent
                };

                _context.UserTasks.Add(newTask);
                await _context.SaveChangesAsync();

                // تحميل بيانات المستخدمين للرسالة
                var assignedByUser = await _context.Users.FindAsync(newTask.AssignedByUserId);
                newTask.AssignedToUser = user;
                newTask.AssignedByUser = assignedByUser;

                // إرسال إشعار SignalR
                await SendTaskNotification(newTask, "NewTask");

                await LoadTasksAsync();

                // إغلاق نافذة الإضافة
                manageTaskGrid.Visibility = Visibility.Collapsed;

                // إظهار إشعار للمستخدم الحالي
                if (newTask.AssignedToUserId == App.CurrentUser.Id)
                {
                    Helpers.NotificationsHelper.ShowPopupNotification(
                        "مهمة جديدة",
                        $"تم تكليفك بمهمة جديدة: {newTask.Description}",
                        this,
                        null
                    );
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding task: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // تعديل مهمة مع إشعار SignalR
        public async Task EditTaskAsync()
        {
            try
            {
                var task = await _context.UserTasks.FindAsync(_taskId);
                if (task != null)
                {
                    var oldAssignedTo = task.AssignedToUserId;
                    var oldStatus = task.Status;

                    task.Description = taskDescriptionBox.Text;
                    task.DueDate = dueDatePicker.SelectedDate;
                    task.AssignedToUserId = (int)assignToBox.SelectedValue;

                    await _context.SaveChangesAsync();

                    // تحميل البيانات المحدثة
                    await LoadTasksAsync();

                    // إرسال إشعار SignalR إذا تغير المستخدم
                    if (oldAssignedTo != task.AssignedToUserId)
                    {
                        var updatedTask = await _context.UserTasks
                            .Include(t => t.AssignedToUser)
                            .Include(t => t.AssignedByUser)
                            .FirstOrDefaultAsync(t => t.Id == _taskId);

                        await SendTaskNotification(updatedTask, "TaskUpdated");
                    }

                    manageTaskGrid.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing task: {ex.Message}");
            }
        }

        // حذف مهمة مع إشعار SignalR
        public async Task DeleteTaskAsync(int taskId)
        {
            try
            {
                var task = await _context.UserTasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task != null)
                {
                    // إرسال إشعار بالحذف قبل الحذف
                    await SendTaskNotification(task, "TaskDeleted");

                    _context.UserTasks.Remove(task);
                    await _context.SaveChangesAsync();
                    await LoadTasksAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting task: {ex.Message}");
            }
        }

        // تحديث حالة المهمة مع إشعار SignalR
        public async Task UpdateTaskStatusAsync(int taskId, int newStatus)
        {
            try
            {
                var task = await _context.UserTasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task != null && task.Status != newStatus)
                {
                    var oldStatus = task.Status;
                    task.Status = newStatus;
                    await _context.SaveChangesAsync();

                    // إرسال إشعار بتحديث الحالة
                    await SendTaskNotification(task, "TaskStatusChanged");

                    await LoadTasksAsync();

                    // إظهار إشعار للمستخدم الذي قام بالتحديث
                    if (task.AssignedByUserId == App.CurrentUser.Id)
                    {
                        var statusText = GetStatusText(newStatus);
                        Helpers.NotificationsHelper.ShowPopupNotification(
                            "تحديث حالة المهمة",
                            $"قام {task.AssignedToUser?.FullName} بتحديث حالة المهمة إلى {statusText}",
                            this,
                            null
                        );
                        Helpers.NotificationsHelper.PlayNotificationSound();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating task status: {ex.Message}");
            }
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                (int)UserTaskStatus.Sent => "مرسلة",
                (int)UserTaskStatus.Received => "مستلمة",
                (int)UserTaskStatus.OnHold => "معلقة",
                (int)UserTaskStatus.InProgress => "قيد التنفيذ",
                (int)UserTaskStatus.Completed => "مكتملة",
                _ => "غير معروف"
            };
        }

        // إعداد SignalR Listener للمهام
        private async Task SetupSignalRListener()
        {
            if (_signalRInitialized) return;

            SignalRManager.Instance.OnTaskNotification += HandleTaskNotification;
            _signalRInitialized = true;

            Closed += (s, e) =>
                SignalRManager.Instance.OnTaskNotification -= HandleTaskNotification;
        }

        private async void HandleTaskNotification(
            string notificationType, int taskId, int fromUserId,
            string taskDescription, DateTime timestamp)
        {
            await LoadTasksAsync();

            if (notificationType == "NewTask" && fromUserId != App.CurrentUser.Id)
            {
                var task = allUsersTasks.FirstOrDefault(t => t.Id == taskId);
                if (task?.AssignedToUserId == App.CurrentUser.Id)
                {
                    Helpers.NotificationsHelper.ShowPopupNotification(
                        "مهمة جديدة",
                        $"تم تكليفك بمهمة: {taskDescription}",
                        this, null);
                    Helpers.NotificationsHelper.PlayNotificationSound();
                }
            }
        }

        // إرسال إشعار SignalR للمهمة
        private async Task SendTaskNotification(UserTask task, string notificationType)
        {
            try
            {
                if (App.SignalRConnection != null && App.SignalRConnection.State == HubConnectionState.Connected)
                {
                    // إرسال إشعار للمستخدم المعني
                    if (task.AssignedToUserId != null)
                    {
                        await App.SignalRConnection.InvokeAsync("SendTaskNotification",
                            notificationType,
                            task.Id,
                            App.CurrentUser.Id,
                            task.AssignedToUserId,
                            task.Description,
                            DateTime.Now);
                    }

                    // إرسال إشعار للمستخدم الذي أنشأ المهمة (إذا كان مختلفاً)
                    if (task.AssignedByUserId != task.AssignedToUserId)
                    {
                        await App.SignalRConnection.InvokeAsync("SendTaskNotification",
                            notificationType,
                            task.Id,
                            App.CurrentUser.Id,
                            task.AssignedByUserId,
                            task.Description,
                            DateTime.Now);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending task notification: {ex.Message}");
            }
        }

        // Window Loaded Event
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTasksAsync();
        }

        // Edit Task Button
        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is UserTask selectedTask)
            {
                manageTaskGrid.Visibility = Visibility.Visible;
                manageTaskTitle.Text = "تعديل المهمة";
                taskDescriptionBox.Text = selectedTask.Description;
                dueDatePicker.SelectedDate = selectedTask.DueDate;
                assignToBox.SelectedValue = selectedTask.AssignedToUserId;
                _taskId = selectedTask.Id;
            }
            else
            {
                MessageBox.Show("الرجاء اختيار مهمة للتعديل.");
            }
        }

        // Delete Task Button
        private async void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is UserTask selectedTask)
            {
                var result = MessageBox.Show("هل انت متأكد من حذف المهمة?", "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    await DeleteTaskAsync(selectedTask.Id);
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار المهمة لحذفها.");
            }
        }

        // Save Task Button
        private async void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(assignToCodeBox.Text, out int code))
            {
                if (string.IsNullOrWhiteSpace(taskDescriptionBox.Text))
                {
                    MessageBox.Show("الرجاء إدخال وصف للمهمة.");
                    return;
                }
                var user = users.FirstOrDefault(u => u.Code == code.ToString());
                if (user == null)
                {
                    MessageBox.Show("الرجاء إدخال رقم مستخدم صالح.");
                    return;
                }
                if (_taskId == -1)
                    await AddTaskAsync(user);
                else
                    await EditTaskAsync();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار المستخدم الذي سيتم تعيين المهمة له.");
            }
        }

        // Cancel Edit Button
        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            manageTaskGrid.Visibility = Visibility.Collapsed;
            _taskId = -1;
            taskDescriptionBox.Text = string.Empty;
            dueDatePicker.SelectedDate = null;
            assignToBox.SelectedIndex = -1;
            assignToCodeBox.Clear();
        }

        // Add Task Button
        private void addTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            manageTaskGrid.Visibility = Visibility.Visible;
            manageTaskTitle.Text = "إضافة مهمة جديدة";
            assignToBox.SelectedIndex = -1;
            taskDescriptionBox.Text = string.Empty;
            dueDatePicker.SelectedDate = null;
            _taskId = -1;
        }

        // Search User by Code
        private void assignToCodeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (int.TryParse(assignToCodeBox.Text, out int userId))
                {
                    var user = _context.Users.Find(userId);
                    if (user != null)
                    {
                        assignToBox.SelectedValue = user.Id;
                    }
                    else
                    {
                        MessageBox.Show("المستخدم غير موجود.");
                    }
                }
                else
                {
                    MessageBox.Show("الرجاء إدخال رقم مستخدم صالح.");
                }
            }
        }

        // Status Change Handler
        private async void statusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null)
                return;
            var editedTask = combo.DataContext as UserTask;
            if (editedTask != null && editedTask.Status != combo.SelectedIndex)
            {
                await UpdateTaskStatusAsync(editedTask.Id, combo.SelectedIndex);
            }
        }

        // Refresh Button
        private async void refreshTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            _context.ChangeTracker.Clear();
            await LoadTasksAsync();
        }

        // User ComboBox Selection Changed
        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (assignToBox.SelectedItem != null)
            {
                assignToCodeBox.Text = assignToBox.SelectedValue?.ToString();
            }
        }

        // Search ComboBox Loaded
        private void searchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            comboBox?.ApplyTemplate();
            var textBox = comboBox?.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

            if (textBox != null)
            {
                textBox.TextChanged -= searchComboBox_TextChanged;
                textBox.TextChanged += searchComboBox_TextChanged;
            }
        }

        // Search ComboBox Text Changed
        private void searchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var comboBox = FindParent<ComboBox>(textBox);
            var searchText = textBox?.Text;

            var itemsList = users;

            if (itemsList == null)
                return;

            if (string.IsNullOrEmpty(searchText))
            {
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = itemsList;
            }
            else
            {
                var filteredItems = itemsList
                    .Where(item => item.FullName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                comboBox.ItemsSource = null;
                comboBox.ItemsSource = filteredItems;
            }

            comboBox.IsDropDownOpen = true;
            if (textBox != null)
            {
                textBox.Text = searchText;
                textBox.CaretIndex = searchText?.Length ?? 0;
            }
        }

        // Find Parent Helper
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            while (parentObject != null)
            {
                if (parentObject is T parent)
                {
                    return parent;
                }
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }

        // Status Filter
        private void statusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;
            var selectedStatus = comboBox.SelectedIndex;
            if (selectedStatus == 0) // "All"
            {
                MyTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedToUserId == App.CurrentUser.Id));
            }
            else
            {
                MyTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedToUserId == App.CurrentUser.Id && t.Status == selectedStatus - 1));
            }
        }

        // Assigned Status Filter
        private void assignedStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;
            var selectedStatus = comboBox.SelectedIndex;
            if (selectedStatus == 0) // "All"
            {
                AssignedTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedByUserId == App.CurrentUser.Id));
            }
            else
            {
                AssignedTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedByUserId == App.CurrentUser.Id && t.Status == selectedStatus - 1));
            }
        }
    }

    // StatusToColorConverter
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    (int)UserTaskStatus.Sent => Brushes.LightBlue,
                    (int)UserTaskStatus.Received => Brushes.LightGreen,
                    (int)UserTaskStatus.OnHold => Brushes.Orange,
                    (int)UserTaskStatus.InProgress => Brushes.LightCoral,
                    (int)UserTaskStatus.Completed => Brushes.LightGray,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}