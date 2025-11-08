using System;

namespace EducationalPlatform.Models
{
    public class GroupChatMessage
    {
        public int MessageId { get; set; }
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsSystemMessage { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public bool IsMyMessage { get; set; }
        public bool IsFileMessage { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FileSize { get; set; }
        public bool IsDelivered { get; set; } = true;

        // Для отображения времени
        public string SentDate => SentAt.ToString("HH:mm");
        public string SentDateFull => SentAt.ToString("dd.MM.yyyy HH:mm");
    }
}