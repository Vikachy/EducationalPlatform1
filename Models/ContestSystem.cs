using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class Contest
    {
        public int ContestId { get; set; }
        public string ContestName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int LanguageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxParticipants { get; set; }
        public int PrizeCurrency { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool OnlyForGroups { get; set; } = true;
        public ProgrammingLanguage? Language { get; set; }
        public List<ContestSubmission> Submissions { get; set; } = new List<ContestSubmission>();
        
        // Удобное свойство для биндинга в XAML
        public string LanguageName => Language?.LanguageName ?? "Не указан";
    }

    public class ContestSubmission
    {
        public int SubmissionId { get; set; }
        public int ContestId { get; set; }
        public int StudentId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ProjectFileUrl { get; set; }
        public string? Description { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int? TeacherScore { get; set; }
        public string? TeacherComment { get; set; }
        public User? Student { get; set; }
        public Contest? Contest { get; set; }
        
        // Удобные свойства для биндинга в XAML
        public string ContestName => Contest?.ContestName ?? "Неизвестный конкурс";
        public string Status => TeacherScore.HasValue ? "Оценено" : "На проверке";
    }

    public class ProjectFile
    {
        public int FileId { get; set; }
        public int ContestId { get; set; }
        public int StudentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class TeacherReport
    {
        public int ReportId { get; set; }
        public int TeacherId { get; set; }
        public int? GroupId { get; set; }
        public string ReportType { get; set; } = string.Empty; // progress, grades, attendance
        public string? ReportData { get; set; } // JSON с данными отчета
        public DateTime GeneratedDate { get; set; }
        public string? FilePath { get; set; } // Путь к сгенерированному файлу
        public User? Teacher { get; set; }
        public StudyGroup? Group { get; set; }
    }
}