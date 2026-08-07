namespace Sho2on.Web.Services
{
    public class UiStateService
    {
        public bool SidebarCollapsed { get; private set; } = true;
        public event Action? OnChange;

        public void ToggleSidebar()
        {
            SidebarCollapsed = !SidebarCollapsed;
            OnChange?.Invoke();
        }
    }
}