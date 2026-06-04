using ClosedXML.Excel;

using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;

using System.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Windows.Controls;

namespace HR_Application
{
    /// <summary>
    /// Interaction logic for DataEdit.xaml
    /// </summary>
    public partial class DataEdit : Window
    {
        List<AttendData> dataList = new List<AttendData>();

        public DataEdit()
        {
            InitializeComponent();
            List<string> months = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames.Take(12).ToList();
            months.Add("_");
            monthComboBox.ItemsSource = months;
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {

                this.WindowState = WindowState.Maximized;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string ConvertDate(DateTime dateTime)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            string formattedDate = dateTime.ToString("dd/MM/yyyy", enUS);
            return formattedDate;
        }

        private string ConvertTime(DateTime dateTime)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            string formattedDate = dateTime.ToString("hh:mm:ss tt", enUS);
            return formattedDate;
        }




        private void data_load_btn_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection con = new SqlConnection("Server=DESKTOP-9QIUVM0;Database=Original;User Id=sa;Password=p@ssw0rd;");
            string query = "SELECT Code, Name, Br, Job FROM t_employee";
            string branch = "";
            using (con)
            {

                SqlCommand command = new SqlCommand(query, con);
                con.Open();

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (reader["Code"].ToString() == code_box.TextField.Text)
                    {
                        branch = reader["Br"].ToString();
                        name_box.TextField.Text = reader["Name"].ToString();
                        job_box.Text = reader["Job"].ToString();
                    }
                }

                reader.Close();

                

                int rowNumber = 1;
                if (from_picker.SelectedDate != null && to_picker.SelectedDate != null) 
                {
                    query = "SELECT * FROM t_data";
                    command = new SqlCommand(query, con);
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        DateTime tDate = DateTime.Parse(reader["TDate"].ToString());
                        if (reader["UserID"].ToString() == code_box.TextField.Text &&
          
                            tDate >= from_picker.SelectedDate.Value && tDate <= to_picker.SelectedDate.Value)
                        {

                            DateTime d = Convert.ToDateTime(reader["TDate"]);


                            string day = Convert.ToDateTime(reader["TDate"]).DayOfWeek.ToString();
                            dataList.Add(new AttendData(
                                rowNumber++,
                                d.ToString(),
                                reader["Status"].ToString(),
                                "_",
                                branch,
                                day,
                                "_"
                                ));

                        }
                        
                    }
                    con.Close();
                    list.ItemsSource = dataList;
                    list.Items.Refresh();
                    data_load_btn.IsDefault = false;
                    edit_btn.IsDefault = true;
                }else
                {
                    LocalizationManager.ShowMessage("ÇÎÊÇÑ ÝÊÑÉ");
                }
                
                
            }
        }

        static DateTime GetStartOfCustomMonth(int year, int customMonth)
        {
            switch (customMonth)
            {
                case 1:
                    return new DateTime(year - 1, 12, 28); // January: 28 Dec (previous year) to 27 Jan
                case 2:
                    return new DateTime(year, 1, 28); // February: 28 Jan to 27 Feb
                case 3:
                    return new DateTime(year, 2, 28); // March: 28 Feb to 27 Mar
                case 4:
                    return new DateTime(year, 3, 28); // April: 28 Mar to 27 Apr
                case 5:
                    return new DateTime(year, 4, 28); // May: 28 Apr to 27 May
                case 6:
                    return new DateTime(year, 5, 28); // June: 28 May to 27 Jun
                case 7:
                    return new DateTime(year, 6, 28); // July: 28 Jun to 27 Jul
                case 8:
                    return new DateTime(year, 7, 28); // August: 28 Jul to 27 Aug
                case 9:
                    return new DateTime(year, 8, 28); // September: 28 Aug to 27 Sep
                case 10:
                    return new DateTime(year, 9, 28); // October: 28 Sep to 27 Oct
                case 11:
                    return new DateTime(year, 10, 28); // November: 28 Oct to 27 Nov
                case 12:
                    return new DateTime(year, 11, 28); // December: 28 Nov to 27 Dec
                default:
                    throw new ArgumentOutOfRangeException("Invalid custom month");
            }
        }

        static DateTime GetEndOfCustomMonth(int year, int customMonth)
        {
            switch (customMonth)
            {
                case 1:
                    return new DateTime(year, 1, 27); // January: 28 Dec (previous year) to 27 Jan
                case 2:
                    return new DateTime(year, 2, 27); // February: 28 Jan to 27 Feb
                case 3:
                    return new DateTime(year, 3, 27); // March: 28 Feb to 27 Mar
                case 4:
                    return new DateTime(year, 4, 27); // April: 28 Mar to 27 Apr
                case 5:
                    return new DateTime(year, 5, 27); // May: 28 Apr to 27 May
                case 6:
                    return new DateTime(year, 6, 27); // June: 28 May to 27 Jun
                case 7:
                    return new DateTime(year, 7, 27); // July: 28 Jun to 27 Jul
                case 8:
                    return new DateTime(year, 8, 27); // August: 28 Jul to 27 Aug
                case 9:
                    return new DateTime(year, 9, 27); // September: 28 Aug to 27 Sep
                case 10:
                    return new DateTime(year, 10, 27); // October: 28 Sep to 27 Oct
                case 11:
                    return new DateTime(year, 11, 27); // November: 28 Oct to 27 Nov
                case 12:
                    return new DateTime(year, 12, 27); // December: 28 Nov to 27 Dec
                default:
                    throw new ArgumentOutOfRangeException("Invalid custom month");
            }
        }

        public class AttendData : INotifyPropertyChanged
        {
            private string _status;
            private string _dateEdit;

            public int rowNumber { get; set; }
            public string date { get; set; }
            public string status
            {
                get { return _status; }
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged("status");
                    }
                }
            }
            public string dateEdit
            {
                get { return _dateEdit; }
                set
                {
                    if (_dateEdit != value)
                    {
                        _dateEdit = value;
                        OnPropertyChanged("dateEdit");
                    }
                }
            }
            public string procedures { get; set; }
            public string branch { get; set; }
            public string day { get; set; }
            public string user { get; set; }

            public AttendData(int num, string dateTime, string s, string p, string b, string d, string u)
            {
                rowNumber = num;
                date = dateTime;
                dateEdit = dateTime;
                status = s;
                procedures = p;
                branch = b;
                day = d;
                user = u;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            foreach(AttendData attend in dataList)
            {
                attend.status = (attend.status == "ÇäÕÑÇÝ") ? "ÍÖæÑ" : "ÇäÕÑÇÝ";
            }
            list.ItemsSource = dataList;
            list.Items.Refresh();
        }

     

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection con = new SqlConnection("Server=DESKTOP-9QIUVM0;Database=Original;User Id=sa;Password=p@ssw0rd;");
            con.Open();
            list.Items.Refresh();
            using (SqlTransaction transaction = con.BeginTransaction())
            {
                try
                {
                    foreach (AttendData attend in dataList)
                    {
                        string updateQuery = "UPDATE t_data SET Status = @Status, TDate = @DateEdit WHERE UserID = @UserID AND TDate = @TDate";
                        SqlCommand command = new SqlCommand(updateQuery, con, transaction);
                        command.Parameters.AddWithValue("@Status", attend.status);
                        command.Parameters.AddWithValue("@UserID", code_box.TextField.Text);
                        command.Parameters.AddWithValue("@DateEdit", Convert.ToDateTime(attend.dateEdit));
                        command.Parameters.AddWithValue("@TDate", Convert.ToDateTime(attend.date));

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    LocalizationManager.ShowMessage("Database updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LocalizationManager.ShowMessage($"Error updating database: {ex.Message}");
                }
            }

            con.Close();

        }

        private void excel_btn_Click(object sender, RoutedEventArgs e)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance Data");

                // Add header row
                worksheet.Cell(1, 1).Value = "ã";
                worksheet.Cell(1, 2).Value = "ÇáÊÇÑíÎ";
                worksheet.Cell(1, 3).Value = "ÇáÍÇáÉ";
                worksheet.Cell(1, 4).Value = "ÇáÝÑÚ";
                worksheet.Cell(1, 5).Value = "ÇáÇÌÑÇÁÇÊ";
                worksheet.Cell(1, 6).Value = "Çáíæã";
                worksheet.Cell(1, 7).Value = "ÇáãÓÊÎÏã";

                // Add data rows
                for (int i = 0; i < dataList.Count; i++)
                {
                    var data = dataList[i];
                    worksheet.Cell(i + 2, 1).Value = data.rowNumber;
                    worksheet.Cell(i + 2, 2).Value = data.dateEdit;
                    worksheet.Cell(i + 2, 3).Value = data.status;
                    worksheet.Cell(i + 2, 4).Value = data.branch;
                    worksheet.Cell(i + 2, 5).Value = data.procedures;
                    worksheet.Cell(i + 2, 6).Value = data.day;
                    worksheet.Cell(i + 2, 7).Value = data.user;
                }

                // Save the workbook to a file
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = "AttendanceData.xlsx";
                if (saveFileDialog.ShowDialog() == true)
                {
                    workbook.SaveAs(saveFileDialog.FileName);
                }
                
            }
        }
    }
}

