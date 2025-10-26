using System;
using System.Collections.Generic;

namespace EducationalPlatform.Models
{
    public class Test
    {
        public int TestId { get; set; }
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int MaxScore { get; set; } = 100;
        public int PassingScore { get; set; } = 60;
        public List<Question> Questions { get; set; } = new List<Question>();
    }

    public class Question
    {
        public int QuestionId { get; set; }
        public int TestId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty; // single, multiple, code
        public int Score { get; set; } = 1;
        public int QuestionOrder { get; set; }
        public List<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    }

    public class AnswerOption
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public bool IsCorrect { get; set; } = false;
    }

    public class TestAttempt
    {
        public int AttemptId { get; set; }
        public int TestId { get; set; }
        public int StudentId { get; set; }
        public int? GroupId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Score { get; set; }
        public int? AutoScore { get; set; }
        public int? TeacherScore { get; set; }
        public string Status { get; set; } = "in_progress"; // in_progress, completed, under_review
        public bool IsDisputed { get; set; } = false;
        public List<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }

    public class StudentAnswer
    {
        public int AnswerId { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public int? SelectedAnswerId { get; set; }
        public string? CodeAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public string? TeacherComment { get; set; }
    }

}