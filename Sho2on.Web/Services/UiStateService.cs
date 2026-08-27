namespace Sho2on.Web.Services
{
    public class UiStateService
    {
        public event Action? OnChange;

        private bool _isSidebarOpen = false; // ✅ افتراضياً مفتوح
        private bool _isSidebarCollapsed = true; // ✅ حالة الطي للديسكتوب

        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            private set
            {
                if (_isSidebarOpen != value)
                {
                    _isSidebarOpen = value;
                    OnChange?.Invoke();
                }
            }
        }

        // ✅ حالة الطي للديسكتوب
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            private set
            {
                if (_isSidebarCollapsed != value)
                {
                    _isSidebarCollapsed = value;
                    OnChange?.Invoke();
                }
            }
        }

        // ✅ تبديل للموبايل (فتح/إغلاق)
        public void ToggleSidebar()
        {
            IsSidebarOpen = !IsSidebarOpen;
        }

        // ✅ تبديل للديسكتوب (طي/توسيع)
        public void ToggleSidebarCollapse()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        public void CloseSidebar()
        {
            IsSidebarOpen = false;
        }

        public void OpenSidebar()
        {
            IsSidebarOpen = true;
        }
    }
}