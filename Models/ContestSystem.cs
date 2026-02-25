using System;
using System.Collections.Generic;

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
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now; // ДОБАВЛЕНО
        public ProgrammingLanguage? Language { get; set; }
        public User? CreatedBy { get; set; }
        public List<ContestSubmission> Submissions { get; set; } = new List<ContestSubmission>();

        // Удобные свойства для биндинга в XAML
        public string LanguageName => Language?.LanguageName ?? "Не указан";
        public string CreatedByName => CreatedBy != null ? $"{CreatedBy.FirstName} {CreatedBy.LastName}" : "Система";
        public string CreatedDateFormatted => CreatedDate.ToString("dd.MM.yyyy HH:mm");
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
        public int? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
        public string Status { get; set; } = "pending";
        public User? Student { get; set; }
        public User? Grader { get; set; }
        public Contest? Contest { get; set; }

        public string ContestName => Contest?.ContestName ?? "Неизвестный конкурс";
        public string StatusDisplay => Status switch
        {
            "pending" => "На проверке",
            "graded" => "Оценено",
            "rejected" => "Отклонено",
            _ => Status
        };
        public string StatusDisplayEn => Status switch
        {
            "pending" => "Pending",
            "graded" => "Graded",
            "rejected" => "Rejected",
            _ => Status
        };
        public string GradedByName => Grader != null ? $"{Grader.FirstName} {Grader.LastName}" : "—";
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
        public string ReportType { get; set; } = string.Empty;
        public string? ReportData { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string? FilePath { get; set; }
        public User? Teacher { get; set; }
        public StudyGroup? Group { get; set; }
    }
}