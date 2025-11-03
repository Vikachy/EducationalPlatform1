using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LanguageId { get; set; }
        public int DifficultyId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsPublished { get; set; } = false;
        public bool IsGroupCourse { get; set; } = false;
        public decimal Price { get; set; } = 0;
        public int EstimatedHours { get; set; }
        public string? Tags { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string DifficultyName { get; set; } = string.Empty;
        public string? TeacherName { get; set; }
        public double AverageRating { get; set; }
        public int StudentCount { get; set; }
        public List<CourseModule> Modules { get; set; } = new List<CourseModule>();
    }

    public class CourseModule
    {
        public int ModuleId { get; set; }
        public int CourseId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public int ModuleOrder { get; set; }
        public string? Description { get; set; }
        public List<Lesson> Lessons { get; set; } = new List<Lesson>();
    }

    public class Lesson
    {
        public int LessonId { get; set; }
        public int ModuleId { get; set; }
        public string LessonType { get; set; } = string.Empty; // theory, practice, test, video
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<PracticeExercise> Exercises { get; set; } = new List<PracticeExercise>();
    }

    public class PracticeExercise
    {
        public int ExerciseId { get; set; }
        public int LessonId { get; set; }
        public string? StarterCode { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? TestCases { get; set; } // JSON с тест-кейсами
        public string? Hint { get; set; }
    }

    public class TeacherCourse : Course
    {
        public List<StudyGroup> Groups { get; set; } = new List<StudyGroup>();
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public DateTime LastActivity { get; set; }
    }

}
