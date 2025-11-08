using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool CanCreateCourses { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanManageSystem { get; set; }
        public bool CanViewAllData { get; set; }
        public bool CanTakeCourses { get; set; }
        public bool CanJoinGroups { get; set; }
        public bool CanPurchaseItems { get; set; }
        public bool CanManageGroups { get; set; }
        public bool CanGradeTests { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanManageContent { get; set; }
        public bool CanPublishNews { get; set; }
        public bool CanModerateReviews { get; set; }
    }

    public class StudyGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public int CourseId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int MaxStudents { get; set; }
        public int StudentCount { get; set; }
        public string CourseName { get; set; } = string.Empty; 

    }
}