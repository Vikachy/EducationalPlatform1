using System.ComponentModel;

namespace EducationalPlatform.Models
{
    public class ShopItem : INotifyPropertyChanged
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        private bool _isPurchased;
        public bool IsPurchased
        {
            get => _isPurchased;
            set
            {
                _isPurchased = value;
                OnPropertyChanged(nameof(IsPurchased));
            }
        }

        private bool _isEquipped;
        public bool IsEquipped
        {
            get => _isEquipped;
            set
            {
                _isEquipped = value;
                OnPropertyChanged(nameof(IsEquipped));
            }
        }

        // Визуальные свойства
        public string ButtonText { get; set; } = "Купить";
        public Color ButtonColor { get; set; } = Color.FromArgb("#FF9800");
        public Color BorderColor { get; set; } = Color.FromArgb("#DDD");
        public Color PreviewColor { get; set; } = Color.FromArgb("#669BBC");
        public Color PreviewPrimaryColor { get; set; } = Color.FromArgb("#669BBC");
        public Color PreviewSecondaryColor { get; set; } = Color.FromArgb("#003049");
        public Color PreviewAccentColor { get; set; } = Color.FromArgb("#C1121F");
        public Color PreviewBackgroundColor { get; set; } = Color.FromArgb("#FDF0D5");
        public Color PreviewTextColor { get; set; } = Color.FromArgb("#003049");

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}