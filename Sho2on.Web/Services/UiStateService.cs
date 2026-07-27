namespace Sho2on.Web.Services
{
    public class UiStateService
    {
        public bool SidebarCollapsed { get; private set; }
        public event Action? OnChange;

        public void ToggleSidebar()
        {
            SidebarCollapsed = !SidebarCollapsed;
            OnChange?.Invoke();
        }
    }
}