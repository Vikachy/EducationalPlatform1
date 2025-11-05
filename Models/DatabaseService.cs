using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class PracticeDto
    {
        public int LessonId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StarterCode { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? TestCasesJson { get; set; }
        public string? Hint { get; set; }
        public string? AnswerType { get; set; } = "text";
        public int MaxFileSize { get; set; } = 10;
        public string? AllowedFileTypes { get; set; }
    }
    public class TestMeta
    {
        public int TestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int PassingScore { get; set; }
        public int MaxScore { get; set; }
        public int TimeLimitMinutes { get; set; }
        public string? Description { get; set; }
    }
}