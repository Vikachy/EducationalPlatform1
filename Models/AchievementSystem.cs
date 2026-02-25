using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class Achievement
    {
        public int AchievementId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "🏆";
        public string ConditionType { get; set; } = string.Empty;
        public int ConditionValue { get; set; }
        public int RewardCurrency { get; set; }
        public DateTime? EarnedDate { get; set; }
        public bool IsEarned => EarnedDate.HasValue;
    }

    public class StudentAchievement
    {
        public int StudentAchievementId { get; set; }
        public int StudentId { get; set; }
        public int AchievementId { get; set; }
        public DateTime EarnedDate { get; set; }
        public bool IsVisible { get; set; } = true;
        public Achievement? Achievement { get; set; }

    }

    public class ProfileCustomization
    {
        public int CustomizationId { get; set; }
        public int StudentId { get; set; }
        public string ItemType { get; set; } = string.Empty; // avatar_frame, emoji, theme
        public string ItemValue { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public bool PurchasedWithCurrency { get; set; } = false;
    }

    public class UserTitle
    {
        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RequiredCourses { get; set; }
        public double RequiredRating { get; set; }
    }

    public class ProgressItem
    {
        public int ProgressId { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int Attempts { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }

        public Color StatusColor => Status switch
        {
            "completed" => Color.FromArgb("#4CAF50"),
            "in_progress" => Color.FromArgb("#2196F3"),
            "not_started" => Color.FromArgb("#9E9E9E"),
            _ => Color.FromArgb("#9E9E9E")
        };

        public string StatusText => Status switch
        {
            "completed" => "Завершено",
            "in_progress" => "В процессе",
            "not_started" => "Не начато",
            _ => Status
        };

        public string FormattedCompletionDate => CompletionDate?.ToString("dd.MM.yyyy") ?? "Не завершено";
        public double ProgressPercent => TotalLessons > 0 ? (double)CompletedLessons / TotalLessons : 0;
    }


    public class UserTask
    {
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty; // lesson, practice, test, review
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int CourseId { get; set; }
        public int LessonId { get; set; }
    }
    public class UserStatistics
    {
        public int CompletedCourses { get; set; }
        public double AverageScore { get; set; }
        public double CompletionRate { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalDays { get; set; }
        public int PendingTasks { get; set; }
        public int TotalTasks { get; set; }
        public int TotalHours { get; set; }
        public int AchievementsCount { get; set; }
    }
}