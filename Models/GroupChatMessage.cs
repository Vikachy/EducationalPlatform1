using System;

namespace EducationalPlatform.Models
{
    public class GroupChatMessage
    {
        public int MessageId { get; set; }
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; } = false;
        public string MessageType { get; set; } = "text"; // text, file, image
        public User? Sender { get; set; }
        public string SenderName { get; set; } = string.Empty; // Добавить это свойство
        public bool IsMyMessage { get; set; } // Добавить это свойство
        public string ReadStatus { get; set; } = "Отправлено";
        
    }
}