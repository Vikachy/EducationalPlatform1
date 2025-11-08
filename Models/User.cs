using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; } 
        public int RoleId { get; set; }
        public string? LanguagePref { get; set; } = "ru";
        public string? InterfaceStyle { get; set; } = "standard";
        public int GameCurrency { get; set; } = 0;
        public int StreakDays { get; set; } = 0;
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RoleName { get; set; }
        public bool HasPrivacyConsent { get; set; } = false;

    }

    public class PrivacyConsent
    {
        public int ConsentId { get; set; }
        public int UserId { get; set; }
        public string ConsentText { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime ConsentDate { get; set; }
        public string? IPAddress { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; }
    }

    public class UserActivity
    {
        public int ActivityId { get; set; }
        public int UserId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string? ActivityData { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}

