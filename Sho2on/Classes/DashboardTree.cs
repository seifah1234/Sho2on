using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.Classes
{
    public class DashboardTree
    {
        public int Id { get; set; }
        public string Type { get; set; } // "Department" or "JobTitle"
        public string ChildrenType { get; set; } // "Department" or "JobTitle"
        public string Name { get; set; }
        public DashboardTree? Parent { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalChildren { get; set; }
        public int? TotalDeparts { get; set; }
        public int? TotaBranches { get; set; }
        public List<DashboardTree> Children { get; set; } = new List<DashboardTree>();
    }
}
