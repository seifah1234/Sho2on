namespace Sho2on.Web.Services
{
    public class ToastMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = "";
        public string Type { get; set; } = "error"; // error | success
    }

    public class NotificationService
    {
        public List<ToastMessage> Messages { get; } = new();
        public event Action? OnChange;

        public void ShowError(string message) => Show(message, "error");
        public void ShowSuccess(string message) => Show(message, "success");

        void Show(string message, string type)
        {
            var toast = new ToastMessage { Message = message, Type = type };
            Messages.Add(toast);
            OnChange?.Invoke();
            _ = AutoDismiss(toast.Id);
        }

        async Task AutoDismiss(Guid id)
        {
            await Task.Delay(5000);
            Messages.RemoveAll(m => m.Id == id);
            OnChange?.Invoke();
        }
    }
}