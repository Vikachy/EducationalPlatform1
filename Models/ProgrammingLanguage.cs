namespace EducationalPlatform.Models
{
    public class ProgrammingLanguage
    {
        public int LanguageId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int CoursesCount { get; set; }

        // Для обратной совместимости (если используется в старом коде)
        public string? IconUrl
        {
            get => Icon;
            set => Icon = value;
        }
    }
}