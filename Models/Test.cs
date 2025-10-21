using System;
using System.Collections.Generic;

namespace EducationalPlatform.Models
{
    public class Test
    {
        public int TestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TimeLimitMinutes { get; set; }
        public int MaxScore { get; set; }
        public int PassingScore { get; set; }
        public List<Question> Questions { get; set; } = new();
    }

    public class Question
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty; 
        public int Score { get; set; }
        public List<AnswerOption> AnswerOptions { get; set; } = new();
    }

    public class AnswerOption
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}