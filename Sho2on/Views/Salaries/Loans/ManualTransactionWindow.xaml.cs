// ManualTransactionWindow.xaml.cs
using HR_Application.Services;
using Sho2on.Database;
using Sho2on.Database.Models;
using System; using HR_Application.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; using HR_Application.Helpers;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace HR_Application.Views
{
    public partial class ManualTransactionWindow : Window
    {
        public event EventHandler TransactionCompleted;

        private readonly AppDbContext _context;
        private readonly FriendshipBoxService _friendshipBoxService;
        private readonly string _transactionType;

        public ManualTransactionWindow(string transactionType)
        {
            InitializeComponent();
            _context = new AppDbContext(App.ConnectionString);
            _friendshipBoxService = new FriendshipBoxService(_context);
            _transactionType = transactionType;

            Title = transactionType == "Deposit" ? "≈÷«›… ≈Ìœ«⁄ ÌœÊÌ" : "≈÷«›… ”Õ» ÌœÊÌ";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // «·⁄‰Ê«‰
            var titleText = new TextBlock
            {
                Text = _transactionType == "Deposit" ? "≈÷«›… ≈Ìœ«⁄ ÌœÊÌ ·’‰œÊﬁ «·“„«·…" : "≈÷«›… ”Õ» ÌœÊÌ „‰ ’‰œÊﬁ «·“„«·…",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10),
                Foreground = _transactionType == "Deposit" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red
            };

            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            // „Õ ÊÏ «·‰„Ê–Ã
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            var amountPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var lblAmount = new Label
            {
                Content = "«·„»·€:",
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center
            };

            var txtAmount = new TextBox
            {
                Width = 150,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center
            };
            txtAmount.Name = "txtAmount";

            amountPanel.Children.Add(lblAmount);
            amountPanel.Children.Add(txtAmount);

            var descriptionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var lblDescription = new Label
            {
                Content = "«·Ê’›:",
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center
            };

            var txtDescription = new TextBox
            {
                Width = 200,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center
            };
            txtDescription.Name = "txtDescription";

            descriptionPanel.Children.Add(lblDescription);
            descriptionPanel.Children.Add(txtDescription);

            var notesPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            var lblNotes = new Label
            {
                Content = "„·«ÕŸ« :"
            };

            var txtNotes = new TextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            txtNotes.Name = "txtNotes";

            notesPanel.Children.Add(lblNotes);
            notesPanel.Children.Add(txtNotes);

            stackPanel.Children.Add(amountPanel);
            stackPanel.Children.Add(descriptionPanel);
            stackPanel.Children.Add(notesPanel);

            Grid.SetRow(stackPanel, 1);
            grid.Children.Add(stackPanel);

            // «·√“—«—
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var btnSave = new Button
            {
                Content = "Õ›Ÿ",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5),
                Background = _transactionType == "Deposit" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Orange,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnSave.Click += async (s, e) => await SaveTransaction();

            var btnCancel = new Button
            {
                Content = "≈·€«¡",
                Width = 100,
                Height = 35,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            btnCancel.Click += (s, e) => Close();

            buttonPanel.Children.Add(btnSave);
            buttonPanel.Children.Add(btnCancel);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private async Task SaveTransaction()
        {
            try
            {
                if (FindName("txtAmount") is not TextBox txtAmount ||
                    FindName("txtDescription") is not TextBox txtDescription ||
                    FindName("txtNotes") is not TextBox txtNotes)
                    return;

                if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· „»·€ ’ÕÌÕ", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    LocalizationManager.ShowMessage("«·—Ã«¡ ≈œŒ«· Ê’› ··⁄„·Ì…", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_transactionType == "Withdrawal")
                {
                    // «· Õﬁﬁ „‰ —’Ìœ «·’‰œÊﬁ ··”Õ»
                    var balance = await _friendshipBoxService.GetCurrentBalanceAsync();
                    if (amount > balance)
                    {
                        LocalizationManager.ShowMessage($"—’Ìœ «·’‰œÊﬁ €Ì— ﬂ«›Ì. «·—’Ìœ «·„ «Õ: {balance:N2}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                //  ”ÃÌ· «·Õ—ﬂ…
                if (_transactionType == "Deposit")
                {
                    await _friendshipBoxService.RecordDepositAsync(0, amount, 0, txtDescription.Text);
                }
                else
                {
                    await _friendshipBoxService.RecordWithdrawalAsync(0, amount, 0, txtDescription.Text);
                }

                LocalizationManager.ShowMessage(" „ Õ›Ÿ «·Õ—ﬂ… »‰Ã«Õ", "‰Ã«Õ", MessageBoxButton.OK, MessageBoxImage.Information);
                TransactionCompleted?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception ex)
            {
                LocalizationManager.ShowMessage($"Œÿ√: {ex.Message}", "Œÿ√", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
