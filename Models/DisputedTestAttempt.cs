using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class DisputedTestAttempt
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;
        public int Score { get; set; }
    }

}
