using DocumentFormat.OpenXml.Spreadsheet;
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
        }

        // Method to refresh tasks (you can call this method after adding/updating tasks to refresh the lists)
        public async void RefreshTasks()
        {
            try
            {
                manageTaskGrid.Visibility = Visibility.Collapsed;
                _taskId = -1;
                var currentUserId = App.CurrentUser.Id;
                var userTasks = allUsersTasks.Where(t => t.AssignedToUserId == currentUserId);
                var assignedTasks = allUsersTasks.Where(t => t.AssignedByUserId == currentUserId);
                MyTasks = new ObservableCollection<UserTask>(userTasks);
                AssignedTasks = new ObservableCollection<UserTask>(assignedTasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing tasks: {ex.Message}");
            }
        }

        // Method to update task status (this is just a placeholder, you would need to implement the actual logic to update the task status in the database and refresh the lists)
        public void UpdateTaskStatus(int taskId, int newStatus)
        {
            try
            {
                var task = _context.UserTasks.Find(taskId);
                if (task != null)
                {
                    task.Status = newStatus;
                    _context.SaveChanges();
                    RefreshTasks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating task status: {ex.Message}");
            }
        }

        // Method to delete a task (this is just a placeholder, you would need to implement the actual logic to delete the task from the database and refresh the lists)
        public void DeleteTask(int taskId)
        {
            try
            {
                var task = _context.UserTasks.Find(taskId);
                if (task != null)
                {
                    _context.UserTasks.Remove(task);
                    _context.SaveChanges();
                    RefreshTasks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting task: {ex.Message}");
            }
        }

        // Method to edit a task (this is just a placeholder, you would need to implement the actual logic to update the task details in the database and refresh the lists)

        public void EditTask()
        {
            try
            {
                var task = _context.UserTasks.Find(_taskId);
                if (task != null)
                {
                    task.Description = taskDescriptionBox.Text;
                    task.DueDate = dueDatePicker.SelectedDate;
                    task.AssignedToUserId = (int)assignToBox.SelectedValue;

                    _context.SaveChanges();
                    RefreshTasks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing task: {ex.Message}");
            }
        }



        // Method to add a new task (this is just a placeholder, you would need to implement the actual logic to save the task to the database and refresh the lists)
        public void AddTask()
        {
            try
            {
                var newTask = new UserTask
                {
                    Description = taskDescriptionBox.Text,
                    AssignedByUserId = App.CurrentUser.Id,
                    AssignedToUserId = (int)assignToBox.SelectedValue,
                    DueDate = dueDatePicker.SelectedDate,
                    CreatedAt = DateTime.Now,
                    Status = (int)UserTaskStatus.Sent
                };

                _context.UserTasks.Add(newTask);
                _context.SaveChanges();
                RefreshTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding task: {ex.InnerException.Message}");

            }
        }

        // Load tasks for the current user when the window is loaded

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                allUsersTasks = await _context.UserTasks.Include(t => t.AssignedToUser).Include(t => t.AssignedByUser).ToListAsync();
                var currentUserId = App.CurrentUser.Id;
                var userTasks = allUsersTasks.Where(t => t.AssignedToUserId == currentUserId);
                var assignedTasks = allUsersTasks.Where(t => t.AssignedByUserId == currentUserId);
                users = await _context.Users.ToListAsync();
                if (userTasks.Any())
                {
                    foreach (var task in userTasks)
                    {
                        if (task.Status == (int)UserTaskStatus.Sent)
                        {
                            task.Status = (int)UserTaskStatus.Received;
                            _context.SaveChanges();
                        }
                    }
                }
                MyTasks = new ObservableCollection<UserTask>(userTasks);
                AssignedTasks = new ObservableCollection<UserTask>(assignedTasks);
                assignToBox.ItemsSource = users;
                

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.InnerException.Message}");
            }
        }


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
                MessageBox.Show("الرجاء اختيار مهمهة للتعديل.");
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is UserTask selectedTask)
            {
                var result = MessageBox.Show("هل انت متأكد من حذف المهمه?", "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteTask(selectedTask.Id);
                }
            }
            else
            {
                MessageBox.Show("الرجاء اختيار المهمة لحذفها.");
            }
        }

        private void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {

            if (assignToBox.SelectedValue is int newAssignedToUserId)
            {
                if (string.IsNullOrWhiteSpace(taskDescriptionBox.Text))
                {
                    MessageBox.Show("الرجاء إدخال وصف للمهمة.");
                    return;
                }

                if (_taskId == -1)
                    AddTask();
                else
                    EditTask();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار المستخدم الذي سيتم تعيين المهمة له.");
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshTasks();
        }



        private void addTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            manageTaskGrid.Visibility = Visibility.Visible;
            manageTaskTitle.Text = "إضافة مهمة جديدة";
            assignToBox.SelectedIndex = -1;
            taskDescriptionBox.Text = string.Empty;
            dueDatePicker.SelectedDate = null;

        }

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

        private void statusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null)
                return;
            var editedTask = combo.DataContext as UserTask;
            if (editedTask != null && editedTask.Status != combo.SelectedIndex)
            {
                try
                {
                    editedTask.Status = combo.SelectedIndex;
                    var taskInDb = _context.UserTasks.Find(editedTask.Id);
                    if (taskInDb != null)
                    {
                        taskInDb.Status = editedTask.Status;
                        _context.SaveChanges();
                        RefreshTasks();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating task: {ex.Message}");
                }

            }
        }

        private async void refreshTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            _context.ChangeTracker.Clear(); // Clear the change tracker to ensure we get fresh data from the database-
            allUsersTasks = await _context.UserTasks.Include(t => t.AssignedToUser).Include(t => t.AssignedByUser).ToListAsync();
            RefreshTasks();
        }

        private void userComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (assignToBox.SelectedItem != null)
            {
                assignToCodeBox.Text = assignToBox.SelectedValue.ToString();
            }
        }

        private void searchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            comboBox.ApplyTemplate();

            // جلب جميع الأجزاء الموجودة في Template
            var allParts = comboBox.Template.VisualTree;
            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox);

            if (textBox == null)
            {
                // محاولة البحث بأسماء بديلة
                textBox = comboBox.Template.FindName("TextBoxBase", comboBox);
            }

            if (textBox is TextBox txt)
            {

                txt.TextChanged -= searchComboBox_TextChanged;
                txt.TextChanged += searchComboBox_TextChanged;
            }
        }

        private void searchComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            var comboBox = FindParent<System.Windows.Controls.ComboBox>(textBox);
            var searchText = textBox.Text;

            var itemsList = comboBox.Tag as List<User>;
            switch (comboBox.Name)
            {
                case "assignToBox":
                    itemsList = users;
                    break;
            }

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
            textBox.Text = searchText;
            textBox.CaretIndex = searchText.Length;
        }

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

        private void statusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null)
                return;
            var selectedStatus = comboBox.SelectedIndex;
            if (selectedStatus == 0) // "All"
            {
                MyTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedToUserId == App.CurrentUser.Id));
            } else
            {
                MyTasks = new ObservableCollection<UserTask>(allUsersTasks.Where(t => t.AssignedToUserId == App.CurrentUser.Id && t.Status == selectedStatus - 1));
            }
        }


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

        // Converter StatusToColorConverter is defined in XAML resources to convert task status to corresponding colors.
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
                        (int)UserTaskStatus.OnHold => Brushes.Red,
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
