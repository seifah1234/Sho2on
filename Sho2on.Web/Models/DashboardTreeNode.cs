namespace Sho2on.Web.Models
{
    public class DashboardTreeNode
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";          // الشركة / قطاع / فرع / إدارة
        public string ChildrenType { get; set; } = "";   // قطاعات / فروع / إدارات
        public int TotalEmployees { get; set; }
        public int TotalChildren { get; set; }
        public List<DashboardTreeNode> Children { get; set; } = new();
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