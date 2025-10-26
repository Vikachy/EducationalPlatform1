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
        public string? Icon { get; set; } 
        public string ConditionType { get; set; } = string.Empty;
        public int ConditionValue { get; set; }
        public int RewardCurrency { get; set; } = 0;
        public DateTime EarnedDate { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsNew { get; set; } = true;
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
}