namespace Sho2on.Web.Models
{
    public class ConversationListItem
    {
        public int Id { get; set; }
        public bool IsGroup { get; set; }
        public int? OtherUserId { get; set; }
        public string DisplayName { get; set; } = "";
        public string LastMessage { get; set; } = "";
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageItem
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime SentAt { get; set; }
    }
}