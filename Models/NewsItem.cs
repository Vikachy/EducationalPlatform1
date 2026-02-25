using System;

namespace EducationalPlatform.Models
{
    public class NewsItem
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPublished { get; set; } = true;
        public string? Language { get; set; }
    }
}