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
        public bool HasTheory { get; set; }
        public bool HasPractice { get; set; }
    }
}
