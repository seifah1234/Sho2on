using System.Windows; using HR_Application.Helpers;

public enum ExportType
{
    None,
    ForImport,
    DetailedReport
}

public partial class ExportTypeDialog : Window
{
    public ExportType ExportType { get; private set; } = ExportType.None;

    public ExportTypeDialog()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void InitializeComponent()
    {
        Width = 400;
        Height = 280;
        Title = "اختر نوع التصدير";
        ResizeMode = ResizeMode.NoResize;

        var stackPanel = new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(20)
        };

        var titleText = new System.Windows.Controls.TextBlock
        {
            Text = "اختر نوع ملف التصدير:",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var radioForImport = new System.Windows.Controls.RadioButton
        {
            Content = "تصدير بنفس تنسيق Template الإستيراد",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 10),
            Tag = ExportType.ForImport,
            IsChecked = true
        };

        var radioDetailed = new System.Windows.Controls.RadioButton
        {
            Content = "تصدير تقرير مفصل للقراءة فقط",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 20),
            Tag = ExportType.DetailedReport
        };

        var descriptionText = new System.Windows.Controls.TextBlock
        {
            Text = "ملاحظة: النوع الأول مناسب إذا كنت تريد تعديل البيانات وإعادة استيرادها.\nالنوع الثاني مناسب للعرض والطباعة فقط.",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        var btnOk = new System.Windows.Controls.Button
        {
            Content = "موافق",
            Width = 80,
            Height = 30,
            Margin = new Thickness(10),
            Background = System.Windows.Media.Brushes.LightBlue
        };

        var btnCancel = new System.Windows.Controls.Button
        {
            Content = "إلغاء",
            Width = 80,
            Height = 30,
            Margin = new Thickness(10),
            Background = System.Windows.Media.Brushes.LightGray
        };

        btnOk.Click += (s, e) =>
        {
            if (radioForImport.IsChecked == true)
                ExportType = ExportType.ForImport;
            else if (radioDetailed.IsChecked == true)
                ExportType = ExportType.DetailedReport;

            DialogResult = true;
            Close();
        };

        btnCancel.Click += (s, e) =>
        {
            ExportType = ExportType.None;
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(btnOk);
        buttonPanel.Children.Add(btnCancel);

        stackPanel.Children.Add(titleText);
        stackPanel.Children.Add(radioForImport);
        stackPanel.Children.Add(radioDetailed);
        stackPanel.Children.Add(descriptionText);
        stackPanel.Children.Add(buttonPanel);

        Content = stackPanel;
    }
}