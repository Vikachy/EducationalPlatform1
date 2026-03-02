public class GradedWork
{
    public int SubmissionId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public int TeacherScore { get; set; }
    public string? TeacherComment { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? TeacherName { get; set; }

    public string FormattedDate => GradedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—";
    public string ScoreDisplay => $"{TeacherScore}/100";
}