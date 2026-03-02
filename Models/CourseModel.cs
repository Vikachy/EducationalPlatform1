public class CourseModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string DifficultyName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int StudentCount { get; set; }
    public int LessonCount { get; set; }
    public DateTime CreatedDate { get; set; }
}