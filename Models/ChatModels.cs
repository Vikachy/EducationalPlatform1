using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace EducationalPlatform.Models
{
    public class GroupChatMessage : INotifyPropertyChanged
    {
        private int _messageId;
        public int MessageId
        {
            get => _messageId;
            set { _messageId = value; OnPropertyChanged(); }
        }

        private int _groupId;
        public int GroupId
        {
            get => _groupId;
            set { _groupId = value; OnPropertyChanged(); }
        }

        private int _senderId;
        public int SenderId
        {
            get => _senderId;
            set { _senderId = value; OnPropertyChanged(); }
        }

        private string _messageText = string.Empty;
        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFileMessage));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        private DateTime _sentAt;
        public DateTime SentAt
        {
            get => _sentAt;
            set
            {
                _sentAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTime));
                OnPropertyChanged(nameof(FormattedDateTime));
                OnPropertyChanged(nameof(FormattedDate));
            }
        }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(MessageStatus)); OnPropertyChanged(nameof(StatusColor)); }
        }

        private bool _isSystemMessage;
        public bool IsSystemMessage
        {
            get => _isSystemMessage;
            set { _isSystemMessage = value; OnPropertyChanged(); }
        }

        private bool _isDelivered;
        public bool IsDelivered
        {
            get => _isDelivered;
            set { _isDelivered = value; OnPropertyChanged(); OnPropertyChanged(nameof(MessageStatus)); OnPropertyChanged(nameof(StatusColor)); }
        }

        // Данные отправителя
        private string _senderName = string.Empty;
        public string SenderName
        {
            get => _senderName;
            set { _senderName = value; OnPropertyChanged(); }
        }

        private string _senderAvatar = "default_avatar.png";
        public string SenderAvatar
        {
            get => _senderAvatar;
            set { _senderAvatar = value; OnPropertyChanged(); }
        }

        private string? _senderFrameColor;
        public string? SenderFrameColor
        {
            get => _senderFrameColor;
            set { _senderFrameColor = value; OnPropertyChanged(); }
        }

        private string? _userEmoji;
        public string? UserEmoji
        {
            get => _userEmoji;
            set { _userEmoji = value; OnPropertyChanged(); }
        }

        private bool _isMyMessage;
        public bool IsMyMessage
        {
            get => _isMyMessage;
            set { _isMyMessage = value; OnPropertyChanged(); }
        }

        // Для подсветки при поиске
        private Color _backgroundColor = Colors.Transparent;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        // Для файловых сообщений
        public bool IsFileMessage => MessageText?.StartsWith("[FILE]") == true;

        private string? _fileName;
        public string? FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        private string? _fileType;
        public string? FileType
        {
            get => _fileType;
            set { _fileType = value; OnPropertyChanged(); }
        }

        private string? _fileSize;
        public string? FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        // Статусы сообщения
        public string MessageStatus
        {
            get
            {
                if (!IsDelivered) return "🕒";
                if (IsRead) return "✓✓";
                return "✓";
            }
        }

        public string StatusColor
        {
            get
            {
                if (!IsDelivered) return "#999999";
                if (IsRead) return "#34B7F1";
                return "#999999";
            }
        }

        // Форматированное время
        public string FormattedTime => SentAt.ToString("HH:mm");

        public string FormattedDateTime
        {
            get
            {
                var now = DateTime.Now;
                if (SentAt.Date == now.Date)
                    return $"Сегодня в {SentAt:HH:mm}";
                if (SentAt.Date == now.AddDays(-1).Date)
                    return $"Вчера в {SentAt:HH:mm}";
                if (SentAt.Year == now.Year)
                    return SentAt.ToString("dd MMM в HH:mm");
                return SentAt.ToString("dd.MM.yyyy HH:mm");
            }
        }

        public string FormattedDate
        {
            get
            {
                var now = DateTime.Now;
                if (SentAt.Date == now.Date)
                    return "Сегодня";
                if (SentAt.Date == now.AddDays(-1).Date)
                    return "Вчера";
                return SentAt.ToString("dd.MM.yyyy");
            }
        }

        public string DisplayText
        {
            get
            {
                if (IsFileMessage)
                    return $"📎 {FileName}";
                if (IsSystemMessage)
                    return $"🛡️ {MessageText}";
                return MessageText;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class PrivateChatMessage : INotifyPropertyChanged
    {
        private int _messageId;
        public int MessageId
        {
            get => _messageId;
            set { _messageId = value; OnPropertyChanged(); }
        }

        private int _senderId;
        public int SenderId
        {
            get => _senderId;
            set { _senderId = value; OnPropertyChanged(); }
        }

        private int? _receiverId;
        public int? ReceiverId
        {
            get => _receiverId;
            set { _receiverId = value; OnPropertyChanged(); }
        }

        private string _messageText = string.Empty;
        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFileMessage));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        private DateTime _sentAt;
        public DateTime SentAt
        {
            get => _sentAt;
            set
            {
                _sentAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTime));
                OnPropertyChanged(nameof(FormattedDateTime));
                OnPropertyChanged(nameof(FormattedDate));
            }
        }

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); OnPropertyChanged(nameof(MessageStatus)); OnPropertyChanged(nameof(StatusColor)); }
        }

        private bool _isDelivered;
        public bool IsDelivered
        {
            get => _isDelivered;
            set { _isDelivered = value; OnPropertyChanged(); OnPropertyChanged(nameof(MessageStatus)); OnPropertyChanged(nameof(StatusColor)); }
        }

        private bool _isSystemMessage;
        public bool IsSystemMessage
        {
            get => _isSystemMessage;
            set { _isSystemMessage = value; OnPropertyChanged(); }
        }

        private string _senderName = string.Empty;
        public string SenderName
        {
            get => _senderName;
            set { _senderName = value; OnPropertyChanged(); }
        }

        private string _senderAvatar = "default_avatar.png";
        public string SenderAvatar
        {
            get => _senderAvatar;
            set { _senderAvatar = value; OnPropertyChanged(); }
        }

        private string? _senderFrameColor;
        public string? SenderFrameColor
        {
            get => _senderFrameColor;
            set { _senderFrameColor = value; OnPropertyChanged(); }
        }

        private string? _userEmoji;
        public string? UserEmoji
        {
            get => _userEmoji;
            set { _userEmoji = value; OnPropertyChanged(); }
        }

        private bool _isMyMessage;
        public bool IsMyMessage
        {
            get => _isMyMessage;
            set { _isMyMessage = value; OnPropertyChanged(); }
        }

        // Для подсветки при поиске
        private Color _backgroundColor = Colors.Transparent;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsFileMessage => MessageText?.StartsWith("[FILE]") == true;

        private string? _fileName;
        public string? FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        private string? _fileType;
        public string? FileType
        {
            get => _fileType;
            set { _fileType = value; OnPropertyChanged(); }
        }

        private string? _fileSize;
        public string? FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string MessageStatus
        {
            get
            {
                if (!IsDelivered) return "🕒";
                if (IsRead) return "✓✓";
                return "✓";
            }
        }

        public string StatusColor
        {
            get
            {
                if (!IsDelivered) return "#999999";
                if (IsRead) return "#34B7F1";
                return "#999999";
            }
        }

        public string DisplayText
        {
            get
            {
                if (IsFileMessage)
                    return $"📎 {FileName}";
                if (IsSystemMessage)
                    return $"🛡️ {MessageText}";
                return MessageText;
            }
        }

        public string FormattedTime => SentAt.ToString("HH:mm");

        public string FormattedDateTime
        {
            get
            {
                var now = DateTime.Now;
                if (SentAt.Date == now.Date)
                    return $"Сегодня в {SentAt:HH:mm}";
                if (SentAt.Date == now.AddDays(-1).Date)
                    return $"Вчера в {SentAt:HH:mm}";
                if (SentAt.Year == now.Year)
                    return SentAt.ToString("dd MMM в HH:mm");
                return SentAt.ToString("dd.MM.yyyy HH:mm");
            }
        }

        public string FormattedDate
        {
            get
            {
                var now = DateTime.Now;
                if (SentAt.Date == now.Date)
                    return "Сегодня";
                if (SentAt.Date == now.AddDays(-1).Date)
                    return "Вчера";
                return SentAt.ToString("dd.MM.yyyy");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Для группового чата
    public class GroupedGroupMessages
    {
        public string Date { get; set; } = string.Empty;
        public ObservableCollection<GroupChatMessage> Messages { get; set; } = new();
    }

    // Для личного чата
    public class GroupedPrivateMessages
    {
        public string Date { get; set; } = string.Empty;
        public ObservableCollection<PrivateChatMessage> Messages { get; set; } = new();
    }

    public class StudentChatItem : INotifyPropertyChanged
    {
        private int _chatId;
        public int ChatId
        {
            get => _chatId;
            set { _chatId = value; OnPropertyChanged(); }
        }

        private string _chatName = string.Empty;
        public string ChatName
        {
            get => _chatName;
            set { _chatName = value; OnPropertyChanged(); }
        }

        private string _chatType = string.Empty;
        public string ChatType
        {
            get => _chatType;
            set { _chatType = value; OnPropertyChanged(); }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private string? _avatar;
        public string? Avatar
        {
            get => _avatar;
            set { _avatar = value; OnPropertyChanged(); }
        }

        private int? _groupId;
        public int? GroupId
        {
            get => _groupId;
            set { _groupId = value; OnPropertyChanged(); }
        }

        private int? _teacherId;
        public int? TeacherId
        {
            get => _teacherId;
            set { _teacherId = value; OnPropertyChanged(); }
        }

        private string? _teacherName;
        public string? TeacherName
        {
            get => _teacherName;
            set { _teacherName = value; OnPropertyChanged(); }
        }

        private string? _courseName;
        public string? CourseName
        {
            get => _courseName;
            set { _courseName = value; OnPropertyChanged(); }
        }

        private int _participantCount;
        public int ParticipantCount
        {
            get => _participantCount;
            set { _participantCount = value; OnPropertyChanged(); }
        }

        // Добавлено свойство StudentCount для обратной совместимости
        public int StudentCount
        {
            get => _participantCount;
            set { _participantCount = value; OnPropertyChanged(); }
        }

        private int _unreadMessages;
        public int UnreadMessages
        {
            get => _unreadMessages;
            set
            {
                _unreadMessages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnreadMessages));
                OnPropertyChanged(nameof(UnreadBadgeText));
            }
        }

        private string _lastMessage = string.Empty;
        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; OnPropertyChanged(); }
        }

        private DateTime _lastMessageTime = DateTime.Now;
        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set
            {
                _lastMessageTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastMessageTimeDisplay));
            }
        }

        private string? _userEmoji;
        public string? UserEmoji
        {
            get => _userEmoji;
            set { _userEmoji = value; OnPropertyChanged(); }
        }

        private string? _frameColor;
        public string? FrameColor
        {
            get => _frameColor;
            set { _frameColor = value; OnPropertyChanged(); }
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        public bool HasUnreadMessages => UnreadMessages > 0;
        public string UnreadBadgeText => UnreadMessages > 0 ? UnreadMessages.ToString() : "";
        public bool ShowOnlineIndicator => ChatType == "teacher" && IsOnline;

        public string LastMessageTimeDisplay
        {
            get
            {
                var diff = DateTime.Now - _lastMessageTime;
                if (diff.TotalMinutes < 1)
                    return "только что";
                if (diff.TotalMinutes < 60)
                    return $"{diff.Minutes} мин назад";
                if (diff.TotalHours < 24)
                    return $"{diff.Hours} ч назад";
                if (diff.TotalDays < 7)
                    return $"{diff.Days} дн назад";
                return _lastMessageTime.ToString("dd.MM.yyyy");
            }
        }

        public string ChatTypeDisplay
        {
            get
            {
                return ChatType switch
                {
                    "group" => $"👥 {ParticipantCount} участников",
                    "teacher" => "👨‍🏫 Преподаватель",
                    "support" => "🛟 Поддержка",
                    _ => ""
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}