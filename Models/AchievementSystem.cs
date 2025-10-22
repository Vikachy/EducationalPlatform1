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
        public string Icon { get; set; } = string.Empty;
        public string AchievementType { get; set; } = string.Empty;
        public int RequiredValue { get; set; }
        public int RewardCurrency { get; set; }
        public bool IsSecret { get; set; }
        public DateTime EarnedDate { get; set; }
        public bool IsNew { get; set; } = true;
    }

    public class UserAchievement
    {
        public int UserAchievementId { get; set; }
        public int UserId { get; set; }
        public int AchievementId { get; set; }
        public DateTime EarnedDate { get; set; }
        public bool IsNew { get; set; } = true;
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