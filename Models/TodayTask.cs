using System;

namespace EducationalPlatform.Models
{
    public class TodayTask
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public Color CardColor { get; set; } = Colors.White;
        public Color BorderColor { get; set; } = Colors.Gray;
        public Color ButtonColor { get; set; } = Colors.Blue;

        // ДОБАВЛЕННЫЕ СВОЙСТВА
        public int TaskId { get; set; }
        public bool IsCompleted { get; set; }
        public string TaskType { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}