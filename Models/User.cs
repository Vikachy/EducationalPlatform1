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
        public int GameCurrency { get; set; } = 0;
        public int StreakDays { get; set; } = 0;
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}

