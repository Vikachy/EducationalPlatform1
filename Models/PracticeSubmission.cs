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
        public string StudentName { get; set; } = string.Empty;


    }
}

