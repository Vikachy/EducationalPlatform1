using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class CourseDifficulty
    {
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool HasTheory { get; set; } = true;
        public bool HasPractice { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int CoursesCount { get; set; }
    }
}
