public class ProgrammingLanguage
{
    public int LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
}
public class CourseDifficulty
{
    public int DifficultyId { get; set; }
    public string DifficultyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool HasTheory { get; set; }
    public bool HasPractice { get; set; }
}