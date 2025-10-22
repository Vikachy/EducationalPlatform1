using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    namespace EducationalPlatform.Models
    {
        public class PrivacyConsent
        {
            public int ConsentId { get; set; }
            public int UserId { get; set; }
            public string ConsentText { get; set; } = string.Empty;
            public string Version { get; set; } = "1.0";
            public DateTime ConsentDate { get; set; } = DateTime.Now;
            public string IPAddress { get; set; } = string.Empty;
        }
    }
}
