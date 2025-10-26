using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class UserStatistics
    {
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int? TotalTimeSpent { get; set; } // nullable int
        public double? AverageScore { get; set; } // nullable double
        public double? CompletionRate { get; set; } // nullable double
        public int? CurrentStreak { get; set; }
        public int? LongestStreak { get; set; }
        public int? TotalDays { get; set; }
    }
}