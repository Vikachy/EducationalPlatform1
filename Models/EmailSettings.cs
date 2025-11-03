namespace EducationalPlatform.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "mituxina85@gmail.com";
        public string SenderName { get; set; } = "Educational Platform";
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool EnableSsl { get; set; } = true;
    }
}