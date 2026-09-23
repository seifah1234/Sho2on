namespace Sho2on.Web.Models
{
    public class DashboardTreeNode
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";          // الشركة / قطاع / فرع / إدارة
        public string ChildrenType { get; set; } = "";   // قطاعات / فروع / إدارات
        public int TotalEmployees { get; set; }
        public int TotalBranches { get; set; }
        public int TotalDeparts { get; set; }
        public int TotalChildren { get; set; }
        public List<DashboardTreeNode> Children { get; set; } = new();
        public bool IsExpanded { get; set; } = false;
    }

    public class HiringStats
{
    // عدد الموظفين المعيّنين في الفترة
    public int HiredInPeriod { get; set; }

    // عدد الموظفين اللي مشيوا في الفترة
    public int LeftInPeriod { get; set; }

    // صافي التغيير
    public int NetChange => HiredInPeriod - LeftInPeriod;

    // عدد الموظفين النشطين حالياً
    public int ActiveEmployees { get; set; }

    // عدد الموظفين المؤرشفين (مشيوا)
    public int ArchivedEmployees { get; set; }

    // إجمالي الموظفين (نشطين + مؤرشفين)
    public int TotalEverEmployed { get; set; }

    // المعدل الشهري للتعيين
    public double MonthlyHiringRate { get; set; }

    // المعدل الشهري للمغادرة
    public double MonthlyTurnoverRate { get; set; }
}

public class HiringTrendPoint
{
    public string MonthLabel { get; set; } = "";  // "يناير 2025"
    public int Year { get; set; }
    public int Month { get; set; }
    public int Hired { get; set; }
    public int Left { get; set; }
}

    public class DashboardAlert
    {
        public string Icon { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class DashboardChartsData
    {
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public List<string> DepartmentLabels { get; set; } = new();
        public List<int> DepartmentCounts { get; set; } = new();
        public List<string> BranchLabels { get; set; } = new();
        public List<int> BranchCounts { get; set; } = new();
        public List<string> SectorLabels { get; set; } = new();
        public List<int> SectorCounts { get; set; } = new();
    }
}