// Models/GroupChatMember.cs
namespace EducationalPlatform.Models
{
    public class GroupChatMember
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        public StudyGroup Group { get; set; }
        public User User { get; set; }
    }
}