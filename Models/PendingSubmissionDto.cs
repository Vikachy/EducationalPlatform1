namespace EducationalPlatform.Models
{
    public class PendingSubmissionDto
    {
        public int AttemptId { get; set; }
        public int LessonId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public string AnswerFilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Submitted";
    }
}