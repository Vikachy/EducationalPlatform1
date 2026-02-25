namespace EducationalPlatform.Models
{
    public class PracticeSubmission
    {
        public int SubmissionId { get; set; }
        public int LessonId { get; set; }
        public int StudentId { get; set; }
        public string? SubmissionText { get; set; }
        public string? SubmissionFileUrl { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int? TeacherScore { get; set; }
        public string? TeacherComment { get; set; }
        public string Status { get; set; } = "submitted"; // submitted, graded, returned
        public int? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }

        // Для отображения на странице преподавателя (можно заполнять при запросе)
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;

        // Удобный предпросмотр для UI
        public string Preview => !string.IsNullOrEmpty(SubmissionText)
            ? (SubmissionText.Length > 80 ? SubmissionText.Substring(0, 80) + "..." : SubmissionText)
            : (!string.IsNullOrEmpty(SubmissionFileUrl) ? $"Файл: {Path.GetFileName(SubmissionFileUrl)}" : "Нет ответа");


    }
}

