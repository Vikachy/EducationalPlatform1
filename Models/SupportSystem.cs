using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class SupportTicket
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TicketType { get; set; } = "bug"; // bug, question, feature
        public string Status { get; set; } = "open"; // open, in_progress, resolved
        public int Priority { get; set; } = 3; // 1: high, 2: medium, 3: low
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string? AdminComment { get; set; }
        public User? User { get; set; }
    }

    public class CourseReview
    {
        public int ReviewId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public int Rating { get; set; } // 0-5
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsApproved { get; set; } = true;
        public User? Student { get; set; }
        public Course? Course { get; set; }
    }


    public class News
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public int AuthorId { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Category { get; set; } = "general"; // general, courses, contests, system
        public string LanguageCode { get; set; } = "ru";
        public bool ForTeens { get; set; } = false;
        public string? ImageUrl { get; set; }
        public int ViewsCount { get; set; } = 0;
        public User? Author { get; set; }

        // Для отображения в UI
        public string CategoryDisplay
        {
            get
            {
                return Category switch
                {
                    "courses" => "📚 Курсы",
                    "contests" => "🏆 Конкурсы",
                    "system" => "⚙️ Система",
                    _ => "📰 Общее"
                };
            }
        }

        public string CategoryDisplayEn
        {
            get
            {
                return Category switch
                {
                    "courses" => "📚 Courses",
                    "contests" => "🏆 Contests",
                    "system" => "⚙️ System",
                    _ => "📰 General"
                };
            }
        }

        public string AuthorName => Author != null ? $"{Author.FirstName} {Author.LastName}" : "Система";
        public string FormattedDate => PublishedDate.ToString("dd.MM.yyyy HH:mm");
    }

   
    public class LoginGreeting
    {
        public int GreetingId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "ru";
        public bool ForTeens { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}


