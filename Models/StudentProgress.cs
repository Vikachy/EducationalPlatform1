using System;

namespace EducationalPlatform.Models
{
    public class StudentProgress
    {
        public int ProgressId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Status { get; set; } = "not_started";
        public DateTime? StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? Score { get; set; }
        public int Attempts { get; set; }
        public string? CourseName { get; set; }
    }
}