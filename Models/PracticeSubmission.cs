using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Models
{
    public class PracticeSubmission : INotifyPropertyChanged
    {
        private int _submissionId;
        public int SubmissionId
        {
            get => _submissionId;
            set { _submissionId = value; OnPropertyChanged(); }
        }

        private int _lessonId;
        public int LessonId
        {
            get => _lessonId;
            set { _lessonId = value; OnPropertyChanged(); }
        }

        private int _studentId;
        public int StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
        }

        private string? _submissionText;
        public string? SubmissionText
        {
            get => _submissionText;
            set { _submissionText = value; OnPropertyChanged(); OnPropertyChanged(nameof(Preview)); }
        }

        private string? _submissionFileUrl;
        public string? SubmissionFileUrl
        {
            get => _submissionFileUrl;
            set { _submissionFileUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(Preview)); OnPropertyChanged(nameof(HasFile)); }
        }

        private DateTime _submissionDate;
        public DateTime SubmissionDate
        {
            get => _submissionDate;
            set { _submissionDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedDate)); }
        }

        private int? _teacherScore;
        public int? TeacherScore
        {
            get => _teacherScore;
            set { _teacherScore = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreDisplay)); }
        }

        private string? _teacherComment;
        public string? TeacherComment
        {
            get => _teacherComment;
            set { _teacherComment = value; OnPropertyChanged(); }
        }

        private string _status = "submitted";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); OnPropertyChanged(nameof(StatusColor)); }
        }

        private int? _gradedBy;
        public int? GradedBy
        {
            get => _gradedBy;
            set { _gradedBy = value; OnPropertyChanged(); }
        }

        private DateTime? _gradedAt;
        public DateTime? GradedAt
        {
            get => _gradedAt;
            set { _gradedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedGradedDate)); }
        }

        // Для отображения на странице преподавателя
        private string _studentName = string.Empty;
        public string StudentName
        {
            get => _studentName;
            set { _studentName = value; OnPropertyChanged(); }
        }

        private string _studentFullName = string.Empty;
        public string StudentFullName
        {
            get => _studentFullName;
            set { _studentFullName = value; OnPropertyChanged(); }
        }

        private string _courseName = string.Empty;
        public string CourseName
        {
            get => _courseName;
            set { _courseName = value; OnPropertyChanged(); }
        }

        private string _lessonTitle = string.Empty;
        public string LessonTitle
        {
            get => _lessonTitle;
            set { _lessonTitle = value; OnPropertyChanged(); }
        }

        private string? _teacherName;
        public string? TeacherName
        {
            get => _teacherName;
            set { _teacherName = value; OnPropertyChanged(); }
        }

        // Вычисляемые свойства для UI
        public bool HasFile => !string.IsNullOrEmpty(SubmissionFileUrl);

        public string FileName => HasFile ? Path.GetFileName(SubmissionFileUrl) : string.Empty;

        public string FileExtension => HasFile ? Path.GetExtension(SubmissionFileUrl)?.ToLower() ?? "" : "";

        public string FileIcon => FileExtension switch
        {
            ".pdf" => "📄",
            ".doc" or ".docx" => "📝",
            ".xls" or ".xlsx" => "📊",
            ".ppt" or ".pptx" => "📽️",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
            ".zip" or ".rar" or ".7z" => "🗜️",
            ".txt" => "📃",
            ".mp4" or ".avi" or ".mov" => "🎬",
            _ => "📎"
        };

        public string Preview
        {
            get
            {
                if (!string.IsNullOrEmpty(SubmissionText))
                    return SubmissionText.Length > 80 ? SubmissionText.Substring(0, 80) + "..." : SubmissionText;
                if (!string.IsNullOrEmpty(SubmissionFileUrl))
                    return $"📎 Файл: {FileName}";
                return "❌ Нет ответа";
            }
        }

        public string ScoreDisplay => TeacherScore.HasValue ? $"{TeacherScore}/100" : "—/100";

        public string FormattedDate => SubmissionDate.ToString("dd.MM.yyyy HH:mm");

        public string FormattedGradedDate => GradedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";

        public string StatusDisplay => Status switch
        {
            "submitted" => "Ожидает проверки",
            "graded" => "Проверено",
            "returned" => "Возвращено",
            _ => Status
        };

        public Color StatusColor => Status switch
        {
            "submitted" => Color.FromArgb("#FF9800"), // Оранжевый
            "graded" => Color.FromArgb("#4CAF50"),    // Зеленый
            "returned" => Color.FromArgb("#F44336"),  // Красный
            _ => Color.FromArgb("#999999")
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}