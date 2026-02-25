using System.ComponentModel;

namespace EducationalPlatform.Models
{
    public class ShopItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public string ItemType { get; set; } = string.Empty; // "theme", "avatar_frame", "emoji"
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Для UI
        public bool IsPurchased { get; set; }
        public bool IsEquipped { get; set; }
        public string ButtonText { get; set; } = "Купить";
        public Color ButtonColor { get; set; } = Color.FromArgb("#FF9800");
        public Color BorderColor { get; set; } = Color.FromArgb("#DDD");

        // Для предпросмотра рамок
        public Color PreviewColor { get; set; } = Color.FromArgb("#669BBC");

        // Для предпросмотра тем
        public Color PreviewPrimaryColor { get; set; } = Color.FromArgb("#457b9d");
        public Color PreviewSecondaryColor { get; set; } = Color.FromArgb("#1d3557");
        public Color PreviewAccentColor { get; set; } = Color.FromArgb("#e63946");
        public Color PreviewBackgroundColor { get; set; } = Color.FromArgb("#f1faee");
        public Color PreviewTextColor { get; set; } = Color.FromArgb("#1d3557");
    }
}