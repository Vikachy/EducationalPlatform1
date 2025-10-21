using System;
using System.Collections.Generic;

namespace EducationalPlatform.Models
{
    public class TeacherCourse
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string DifficultyName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int StudentCount { get; set; }
        public double AverageRating { get; set; }
        public List<StudyGroup> Groups { get; set; } = new();
    }

    public class StudyGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentProgress
    {
        public int UserId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int Attempts { get; set; }
    }
}