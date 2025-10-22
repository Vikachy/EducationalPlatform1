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
        public string Description { get; set; } = string.Empty;
        public int LanguageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int PrizeCurrency { get; set; }
        public bool IsActive { get; set; }
        public bool OnlyForGroups { get; set; }
        public string Requirements { get; set; } = string.Empty;
    }

    public class ContestSubmission
    {
        public int SubmissionId { get; set; }
        public int ContestId { get; set; }
        public int StudentId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectFileUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public int? TeacherScore { get; set; }
        public string TeacherComment { get; set; } = string.Empty;
        public int? FinalPlace { get; set; }
    }
}