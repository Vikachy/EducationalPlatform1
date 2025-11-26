using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class ChatMessage
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = "default_avatar.png";
        public bool IsMyMessage { get; set; }
        public bool IsFileMessage { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FileSize { get; set; }
        public string? FilePath { get; set; }
        public string? UserEmoji { get; set; }
        public bool IsDelivered { get; set; } = true;
    }

    public class StudentChatItem
    {
        public int ChatId { get; set; }
        public string ChatName { get; set; } = string.Empty;
        public string ChatType { get; set; } = string.Empty; // "group", "teacher", "support"
        public string Description { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageTime { get; set; }
        public int UnreadMessages { get; set; }
        public string Avatar { get; set; } = "default_avatar.png";

        // Для групповых чатов
        public int? GroupId { get; set; }
        public string? CourseName { get; set; }
        public string? TeacherName { get; set; }

        // Для индивидуальных чатов
        public int? TeacherId { get; set; }
        public string? TeacherSubject { get; set; }
    }
}