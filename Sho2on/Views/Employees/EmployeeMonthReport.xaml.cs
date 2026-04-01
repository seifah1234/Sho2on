using ClosedXML.Excel;
using HR_Application.Views;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using static HR_Application.MonthlyData;
using static HR_Application.MonthlySalaryData;
using MessageBox = System.Windows.MessageBox;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for EmployeeMonthReport.xaml
    /// </summary>
    public partial class EmployeeMonthReport : Window
    {
        private MonthlyData.MonthSettings monthSettings;
        private ObservableCollection<EmployeeData> employees = new ObservableCollection<EmployeeData>();
        private Dictionary<string, int> branches = new Dictionary<string, int>();
        private Dictionary<string, string> totalWH = new Dictionary<string, string>();
        private Dictionary<string, List<int>> holiday = new Dictionary<string, List<int>>();
        List<EmployeeHoilday> GlobalResult = new List<EmployeeHoilday>();
        private CultureInfo arabicCulture = new CultureInfo("ar-SA");
        int? branchCode = null;

        private AppDbContext _context = new AppDbContext(App.ConnectionString);

        public EmployeeMonthReport()
        {
            InitializeComponent();
            InitializeDateSelections();
            LoadData();
        }

        private void InitializeDateSelections()
        {
            month_box.SelectedItem = DateTime.Now.ToString("MMMM", CultureInfo.CurrentCulture);
            year_box.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            year_box.SelectedItem = DateTime.Now.Year;
        }

        public FlowDocument CreateDocument(string year, string month, string branch)
        {
            // Create FlowDocument dynamically
            FlowDocument flowDocument = new FlowDocument
            {
                PagePadding = new Thickness(30),
                ColumnWidth = 500,
                FlowDirection = System.Windows.FlowDirection.RightToLeft
            };

            // Create Header Table
            Table headerTable = new Table();
            headerTable.Columns.Add(new TableColumn());
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(15, GridUnitType.Star) });
            TableRowGroup headerRowGroup = new TableRowGroup();
            TableRow headerRow = new TableRow();

            // Create branch and month paragraph
            TableCell branchCell = new TableCell(new Paragraph
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Inlines = {
                    new Run($"الفرع: {branch}"),
                    new LineBreak(),
                    new LineBreak(),
                    new Run($"شهر: {month} - {year}")
                }
            });
            branchCell.BorderThickness = new Thickness(0);
            headerRow.Cells.Add(branchCell);

            // Create Image cell
            System.Windows.Controls.Image img = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/Back.jfif")),
                Width = 80,
                Height = 80,
                Stretch = Stretch.Fill,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            TableCell imageCell = new TableCell(new BlockUIContainer(img));
            imageCell.BorderThickness = new Thickness(0);
            headerRow.Cells.Add(imageCell);

            // Add row to the header table
            headerRowGroup.Rows.Add(headerRow);
            headerTable.RowGroups.Add(headerRowGroup);

            // Add header table to the document
            flowDocument.Blocks.Add(headerTable);

            // Add title paragraph
            Paragraph titleParagraph = new Paragraph(new Run("تقرير ساعات عمل شهري"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Red,
                TextAlignment = TextAlignment.Center
            };
            flowDocument.Blocks.Add(titleParagraph);

            // Create Data Table
            Table dataTable = new Table
            {
                CellSpacing = 0,
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black
            };

            // Define table columns
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
            dataTable.Columns.Add(new TableColumn { Width = GridLength.Auto });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(80) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(80) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });
            dataTable.Columns.Add(new TableColumn { Width = new GridLength(60) });

            // Table Header
            TableRowGroup headerRowGroupData = new TableRowGroup();
            TableRow dataHeaderRow = new TableRow();
            dataHeaderRow.Background = System.Windows.Media.Brushes.LightGray;
            dataHeaderRow.FontSize = 13;
            dataHeaderRow.FontWeight = FontWeights.Bold;

            // Add header cells
            dataHeaderRow.Cells.Add(CreateCell("الكود", true, false));
            dataHeaderRow.Cells.Add(CreateCell("الاسم", true, true));
            dataHeaderRow.Cells.Add(CreateCell("ع الساعات", true, false));
            dataHeaderRow.Cells.Add(CreateCell("التأخير", true, false));
            dataHeaderRow.Cells.Add(CreateCell("خ مبكر", true, false));
            dataHeaderRow.Cells.Add(CreateCell("الاضافي", true, false));
            dataHeaderRow.Cells.Add(CreateCell("س الفعلية", true, false));
            dataHeaderRow.Cells.Add(CreateCell("الاجازات", true, false));
            dataHeaderRow.Cells.Add(CreateCell("الغياب", true, false));

            // Add header row to the data table
            headerRowGroupData.Rows.Add(dataHeaderRow);
            dataTable.RowGroups.Add(headerRowGroupData);

            TableRowGroup dataRowGroupData = new TableRowGroup();

            foreach (var record in GlobalResult)
            {
                string FormattedWorkHours = record.dataTotal.TotalWorkTime != null ? ConvertToArabicNumerals(record.dataTotal.TotalWorkTime) : string.Empty;
                string FormattedLate = record.dataTotal.TotalLate != null ? ConvertToArabicNumerals(record.dataTotal.TotalLate) : string.Empty;
                string FormattedEarly = record.dataTotal.TotalEarly != null ? ConvertToArabicNumerals(record.dataTotal.TotalEarly) : string.Empty;
                string FormattedOT = record.dataTotal.TotalOT != null ? ConvertToArabicNumerals(record.dataTotal.TotalOT) : string.Empty;
                string FormattedAttendHours = record.dataTotal.TotalAttendTime != null ? ConvertToArabicNumerals(record.dataTotal.TotalAttendTime) : string.Empty;

                System.Windows.Documents.TableRow dataRow = new System.Windows.Documents.TableRow();
                dataRow.Cells.Add(CreateCell(record.dataTotal.Code, false, true));
                dataRow.Cells.Add(CreateCell(record.dataTotal.Name, false, true));
                dataRow.Cells.Add(CreateCell(FormattedWorkHours, false, false));
                dataRow.Cells.Add(CreateCell(FormattedLate, false, false));
                dataRow.Cells.Add(CreateCell(FormattedEarly, false, false));
                dataRow.Cells.Add(CreateCell(FormattedOT, false, false));
                dataRow.Cells.Add(CreateCell(FormattedAttendHours, false, false));
                dataRow.Cells.Add(CreateCell(record.holidays.ToString(), false, false));
                dataRow.Cells.Add(CreateCell(record.absences.ToString(), false, false));

                dataRowGroupData.Rows.Add(dataRow);
            }
            dataTable.RowGroups.Add(dataRowGroupData);

            // Add the data table to the document
            flowDocument.Blocks.Add(dataTable);

            return flowDocument;
        }

        // Helper method to create table cells with border
        private System.Windows.Documents.TableCell CreateCell(string content, bool isHeader, bool isArabic)
        {
            var cell = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(content)));
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.BorderThickness = new Thickness(1);
            cell.Padding = new Thickness(1);
            cell.FontSize = 12;
            cell.OverridesDefaultStyle = false;
            if (isHeader)
            {
                cell.Background = System.Windows.Media.Brushes.Black;
                cell.Foreground = System.Windows.Media.Brushes.White;
            }
            if (isArabic)
            {
                cell.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                cell.TextAlignment = System.Windows.TextAlignment.Center;
            }
            return cell;
        }

        private string ConvertToArabicNumerals(string input)
        {
            string arabicNumerals = "٠١٢٣٤٥٦٧٨٩";
            string westernNumerals = "0123456789";

            return new string(input.Select(c =>
                westernNumerals.Contains(c) ? arabicNumerals[westernNumerals.IndexOf(c)] : c
            ).ToArray());
        }

        private void PrintDocument(FlowDocument document)
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Attendance Report");
            }
        }

        public void ExportDataGridToExcel()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "ExportedData"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Sheet1");

                        var headers = new[]
                        {
                            "الكود",
                            "الاسم",
                            "س رسمية",
                            "التأخير",
                            "خ مبكر",
                            "الاضافي",
                            "س فعلية",
                            "الاجازات",
                            "الغياب"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            cell.Style.Font.FontColor = XLColor.Black;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        }

                        for (int i = 0; i < GlobalResult.Count; i++)
                        {
                            var item = GlobalResult[i];

                            worksheet.Cell(i + 2, 1).Value = item.dataTotal.Code;
                            worksheet.Cell(i + 2, 2).Value = item.dataTotal.Name;
                            worksheet.Cell(i + 2, 3).Value = item.dataTotal.TotalWorkTime;
                            worksheet.Cell(i + 2, 4).Value = item.dataTotal.TotalLate;
                            worksheet.Cell(i + 2, 5).Value = item.dataTotal.TotalEarly;
                            worksheet.Cell(i + 2, 6).Value = item.dataTotal.TotalOT;
                            worksheet.Cell(i + 2, 7).Value = item.dataTotal.TotalAttendTime;
                            worksheet.Cell(i + 2, 8).Value = item.holidays;
                            worksheet.Cell(i + 2, 9).Value = item.absences;
                        }

                        workbook.SaveAs(filePath);
                        MessageBox.Show("تم استخراج الاكسيل!");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "خطأ");
                }
            }
        }

        private async void LoadData()
        {
            try
            {
                month_box.ItemsSource = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
                year_box.ItemsSource = Enumerable.Range(2010, 21).ToList();

                branches.Clear();

                monthSettings = new MonthlyData.MonthSettings
                {
                    StartDate = Properties.Settings.Default.StartOfMonth.ToString(),
                    EndDate = Properties.Settings.Default.EndOfMonth.ToString()
                };

                // Load branches using EF
                var dbBranches = await _context.Branches.ToListAsync();
                var _branches = new List<Branch>();
                foreach (var branch in dbBranches)
                {
                    if (App.userBranches.Contains(branch.Id))
                    {
                        _branches.Add(branch);
                        branches.Add(branch.Name, branch.Id);
                    }
                }
                branch_box.ItemsSource = _branches;

                var sections = await _context.Degrees.ToListAsync();
                var depts = await _context.Departments.ToListAsync();
                var jobs = await _context.JobTitles.ToListAsync();

                section_box.ItemsSource = sections;
                dept_box.ItemsSource = depts;
                job_box.ItemsSource = jobs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (DateTime Start, DateTime End) GetCustomMonthDates(int month, int year)
        {
            try
            {
                int startDay = Convert.ToInt16(Properties.Settings.Default.StartOfMonth);
                int endDay = (month == 2 && Convert.ToInt16(Properties.Settings.Default.EndOfMonth) > 29) ? 29 : Convert.ToInt16(Properties.Settings.Default.EndOfMonth);

                DateTime startDate = new DateTime(year, month, startDay);
                DateTime endDate = new DateTime(year, month, endDay);

                if (15 < startDay) startDate = startDate.AddMonths(-1);

                return (startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حساب تواريخ الشهر المخصص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return (DateTime.MinValue, DateTime.MaxValue);
            }
        }

        private async void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            employees.Clear();
            GlobalResult.Clear();
            totalWH.Clear();
            holiday.Clear();

            var dayMapping = new Dictionary<string, int>
            {
                { "السبت", 0 }, { "الأحد", 1 }, { "الاثنين", 2 }, { "الثلاثاء", 3 },
                { "الأربعاء", 4 }, { "الخميس", 5 }, { "الجمعة", 6 }
            };


            try
            {
                int monthNumber = DateTime.ParseExact(month_box.Text, "MMMM", CultureInfo.CurrentCulture).Month;
                int year = Convert.ToInt16(year_box.Text);
                (DateTime startMonth, DateTime endMonth) = GetCustomMonthDates(monthNumber, year);

                // Nested dictionary to store data per employee
                var employeeDataDict = new Dictionary<DateTime, Dictionary<string, DateTime>>();

                // Load attendance data using EF
                var attendances = _context.Attendances
                    .Include(a => a.User)
                    .Include(a => a.CheckInBranch)
                    .Where(a => a.AttendanceDate >= startMonth &&
                                a.AttendanceDate <= endMonth)
                    .OrderBy(a => a.UserId)
                    .AsQueryable();


                if (branch_box.SelectedValue != null)
                {
                    attendances = attendances.Where(a => a.User != null && a.User.BranchId.ToString() == branch_box.SelectedValue.ToString());
                }
                if (dept_box.SelectedValue != null)
                {
                    attendances = attendances.Where(a => a.User != null && a.User.DepartmentId.ToString() == dept_box.SelectedValue.ToString());
                }

                if (section_box.SelectedValue != null)
                {
                    attendances = attendances.Where(a => a.User != null && a.User.DegreeId.ToString() == section_box.SelectedValue.ToString());
                }

                if (job_box.SelectedValue != null)
                {
                    attendances = attendances.Where(a => a.User != null && a.User.JobTitleId.ToString() == job_box.SelectedValue.ToString());
                }

                foreach (var att in attendances)
                {
                        
                        var code = att.User.Code;
                        var name = att.User.FullName;
                        var workTime = att.TotalWorkHours ?? TimeSpan.Zero;
                        var attendTime = att.TotalWorkHours ?? TimeSpan.Zero;
                        var dateOnly = att.AttendanceDate;

                        var exemptL = att.ExemptLate;
                        var exemptE = att.ExemptEarlyLeave;
                        var exemptO = att.ExemptOvertime;

                        var late = exemptL ? TimeSpan.Zero : (att.Late ?? TimeSpan.Zero);
                        var early = exemptE ? TimeSpan.Zero : (att.EarlyLeave ?? TimeSpan.Zero);
                        var ot = exemptO ? TimeSpan.Zero : (att.Overtime ?? TimeSpan.Zero);

                        var record = new EmployeeData
                        {
                            Code = code,
                            Name = name,
                            WH = workTime,
                            AH = attendTime,
                            Late = late,
                            Early = early,
                            OT = ot,
                            Absence = att.IsAbsence ? 1 : 0,
                            Holiday = att.IsHoliday ? 1 : 0
                        };

                        employees.Add(record);

                        // Ensure there's a dictionary for each employee
                        if (!employeeDataDict.ContainsKey(dateOnly))
                        {
                            employeeDataDict[dateOnly] = new Dictionary<string, DateTime>();
                        }
                        if (!employeeDataDict[dateOnly].ContainsKey(code))
                        {
                            employeeDataDict[dateOnly][code] = dateOnly;
                        }
                        
                }
                   

                var result = employees
                        .GroupBy(a => new { a.Code, a.Name })
                        .Select(g => new EmployeeDataTotal
                        {
                            Code = g.Key.Code,
                            Name = g.Key.Name,
                            TotalWorkTime = totalWH.ContainsKey(g.Key.Code) ? totalWH[g.Key.Code] : "0",
                            TotalAttendTime = FormatTime(g.Sum(a => a.AH.TotalHours), g.Sum(a => a.AH.Minutes)),
                            TotalLate = FormatTime(g.Sum(a => a.Late.TotalHours), g.Sum(a => a.Late.Minutes)),
                            TotalEarly = FormatTime(g.Sum(a => a.Early.TotalHours), g.Sum(a => a.Early.Minutes)),
                            TotalOT = FormatTime(g.Sum(a => a.OT.TotalHours), g.Sum(a => a.OT.Minutes)),
                            TotalAbsence = g.Sum(a => a.Absence).ToString(),
                            TotalHoliday = g.Sum(a => a.Holiday).ToString()
                        })
                        .ToList();

                    

                    foreach (EmployeeDataTotal employee in result)
                    {

                        var record1 = new EmployeeHoilday
                        {
                            dataTotal = employee
                        };

                        GlobalResult.Add(record1);
                    }

                    dataGrid.Items.Refresh();
                    dataGrid.ItemsSource = GlobalResult;
                    
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        public string FormatTime(double totalHours, double totalMinutes)
        {
            int hours = (int)totalHours;
            int minutes = (int)totalMinutes;

            // Handle overflow minutes
            hours += minutes / 60;
            minutes = minutes % 60;

            // Format the time as "HH:MM"
            return $"{hours:D2}:{minutes:D2}";
        }

        public class EmployeeHoilday
        {
            public EmployeeDataTotal dataTotal { get; set; }
            public int holidays { get; set; }
            public int absences { get; set; }
        }

        public class EmployeeData
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public TimeSpan WH { get; set; }
            public TimeSpan AH { get; set; }
            public TimeSpan Late { get; set; }
            public TimeSpan Early { get; set; }
            public TimeSpan OT { get; set; }
            public int Absence { get; set; }
            public int Holiday { get; set; }
        }

        public class EmployeeDataTotal
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string TotalWorkTime { get; set; }
            public string TotalAttendTime { get; set; }
            public string TotalLate { get; set; }
            public string TotalEarly { get; set; }
            public string TotalOT { get; set; }
            public string TotalAbsence { get; set; }
            public string TotalHoliday { get; set; }
        }

        private void print_btn_Click(object sender, RoutedEventArgs e)
        {
            string year = year_box.Text;
            string month = month_box.Text;
            string branch = branch_box.Text;
            FlowDocument document = CreateDocument(year, month, branch);

            MonthDataReport monthData = new MonthDataReport(document);
            monthData.ShowDialog();
        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGridToExcel();
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            branch_box.SelectedIndex = -1;
            dept_box.SelectedIndex = -1;
            section_box.SelectedIndex = -1;
            job_box.SelectedIndex = -1;
            month_box.SelectedIndex = -1;
            year_box.SelectedIndex = -1;
            GlobalResult.Clear();
            dataGrid.ItemsSource = null;

        }
    }
}