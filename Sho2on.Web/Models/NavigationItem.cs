namespace Sho2on.Web.Models.Navigation;

public class NavigationItem
{
    public string Title { get; set; } = "";

    public string Icon { get; set; } = "";

    public string Url { get; set; } = "";

    public bool Expanded { get; set; }

    public bool IsSection { get; set; }
    public string? RequiredPermission { get; set; }
    public string? Permission { get; set; }

    public List<NavigationItem> Children { get; set; } = [];
}