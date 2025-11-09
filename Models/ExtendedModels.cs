using System.Collections.ObjectModel;

namespace EducationalPlatform.Models
{
    // Модель для создания теста с вопросами
    public class TestCreationModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TimeLimit { get; set; } = 60;
        public int PassingScore { get; set; } = 60;
        public ObservableCollection<ExtendedQuestionCreationModel> Questions { get; set; } = new();
    }

    // Переименуем классы, чтобы избежать конфликта
    public class ExtendedQuestionCreationModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "single"; // single, multiple, text
        public int Score { get; set; } = 1;
        public ObservableCollection<ExtendedAnswerOptionModel> AnswerOptions { get; set; } = new();
    }

    public class ExtendedAnswerOptionModel
    {
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    // Модель для практики с разными типами ответов
    public class PracticeCreationModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AnswerType { get; set; } = "text"; // text, code, file
        public string? StarterCode { get; set; }
        public string? ExpectedAnswer { get; set; }
        public string? Hint { get; set; }
        public int MaxFileSizeMB { get; set; } = 10;
        public List<string> AllowedFileTypes { get; set; } = new() { ".zip", ".jpg", ".png", ".pdf" };
    }

    // Модель для простой теории (не HTML)
    public class TheoryCreationModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Простой текст
        public List<string> Attachments { get; set; } = new(); // Ссылки на файлы
    }

    // Модель для отображения уроков курса (ОБНОВЛЕННАЯ)
    public class CourseLesson
    {
        public int LessonId { get; set; }
        public int ModuleId { get; set; }
        public string LessonType { get; set; } = string.Empty; // theory, practice, test
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Модель для отображения вложений
    public class AttachmentViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileIcon { get; set; } = "📎";
    }
}