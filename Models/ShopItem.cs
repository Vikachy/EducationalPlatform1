using System.ComponentModel;

namespace EducationalPlatform.Models
{
    public class ShopItem : INotifyPropertyChanged
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public string ItemType { get; set; } = string.Empty; // avatar_frame, emoji, theme, badge
        public string? Icon { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }

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

        // Визуальные свойства с уведомлениями
        private string _buttonText = "Buy";
        public string ButtonText 
        { 
            get => _buttonText;
            set
            {
                _buttonText = value;
                OnPropertyChanged(nameof(ButtonText));
            }
        }

        private Color _buttonColor = Color.FromArgb("#FF9800");
        public Color ButtonColor 
        { 
            get => _buttonColor;
            set
            {
                _buttonColor = value;
                OnPropertyChanged(nameof(ButtonColor));
            }
        }

        private Color _borderColor = Color.FromArgb("#DDD");
        public Color BorderColor 
        { 
            get => _borderColor;
            set
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor));
            }
        }
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

    public class UserInventory
    {
        public int InventoryId { get; set; }
        public int UserId { get; set; }
        public int ItemId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public bool IsEquipped { get; set; } = false;
        public ShopItem? Item { get; set; }
    }

    public class CurrencyTransaction
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty; // income, expense
        public string? Reason { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}