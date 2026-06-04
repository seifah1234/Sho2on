using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application
{
    public class MonthDetails
    {
        public int rowNumber { get; set; }
        public string day { get; set; }
        public DateTime date { get; set; }
        public DateTime? attend { get; set; }
        public DateTime? departure { get; set; }
        public string shift { get; set; }
        public TimeSpan workHours { get; set; }

        public MonthDetails(int row, string Day, DateTime dateTime, DateTime? Attend, DateTime? Departure, string Shift, TimeSpan hours)
        {
            rowNumber = row;
            day = Day;
            date = dateTime;
            attend = Attend;
            departure = Departure;
            shift = Shift;
            workHours = hours;
        }
    }
}
