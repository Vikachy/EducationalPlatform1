using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Models
{
    public class StudentGroupChatItem
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime LastMessageDate { get; set; }
        public int UnreadMessages { get; set; } // ДОБАВЛЕНО ЭТО СВОЙСТВО
    }
}