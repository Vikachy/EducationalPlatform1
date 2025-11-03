using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace EducationalPlatform.Views
{
    public partial class ShopPage : ContentPage, INotifyPropertyChanged
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        private int _userGameCurrency;
        public int UserGameCurrency
        {
            get => _userGameCurrency;
            set
            {
                _userGameCurrency = value;
                OnPropertyChanged(nameof(UserGameCurrency));
            }
        }

        public ObservableCollection<ShopItem> AvatarFrames { get; set; }
        public ObservableCollection<ShopItem> ProfileEmojis { get; set; }
        public ObservableCollection<ShopItem> Themes { get; set; }

        public ShopPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            AvatarFrames = new ObservableCollection<ShopItem>();
            ProfileEmojis = new ObservableCollection<ShopItem>();
            Themes = new ObservableCollection<ShopItem>();

            BindingContext = this;
            LoadUserData();
        }

        private void LoadUserData()
        {
            UserGameCurrency = _currentUser.GameCurrency;
        }

        private async Task LoadShopItems()
        {
            try
            {
                // Загружаем товары из базы данных
                var shopItems = await _dbService.GetShopItemsAsync();

                AvatarFrames.Clear();
                ProfileEmojis.Clear();
                Themes.Clear();

                foreach (var item in shopItems)
                {
                    var shopItem = new ShopItem
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        ItemType = item.ItemType,
                        Icon = item.Icon,
                        IsPurchased = await _dbService.CheckItemOwnershipAsync(_currentUser.UserId, item.ItemId),
                        IsEquipped = await _dbService.CheckItemEquippedAsync(_currentUser.UserId, item.ItemId)
                    };

                    // Устанавливаем визуальные свойства в зависимости от типа товара
                    SetItemVisualProperties(shopItem);

                    switch (item.ItemType.ToLower())
                    {
                        case "avatar_frame":
                            AvatarFrames.Add(shopItem);
                            break;
                        case "emoji":
                            ProfileEmojis.Add(shopItem);
                            break;
                        case "theme":
                            Themes.Add(shopItem);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить товары: {ex.Message}", "OK");
            }
        }

        private void SetItemVisualProperties(ShopItem item)
        {
            if (item.IsPurchased)
            {
                if (item.IsEquipped)
                {
                    item.ButtonText = "✅ Надето";
                    item.ButtonColor = Color.FromArgb("#4CAF50");
                    item.BorderColor = Color.FromArgb("#4CAF50");
                }
                else
                {
                    item.ButtonText = "Надеть";
                    item.ButtonColor = Color.FromArgb("#2196F3");
                    item.BorderColor = Color.FromArgb("#2196F3");
                }
            }
            else
            {
                item.ButtonText = "Купить";
                item.ButtonColor = Color.FromArgb("#FF9800");
                item.BorderColor = Color.FromArgb("#DDD");
            }

            // Устанавливаем цвета предпросмотра для разных типов товаров
            switch (item.ItemType.ToLower())
            {
                case "avatar_frame":
                    item.PreviewColor = GetFramePreviewColor(item.ItemId);
                    break;
                case "emoji":
                    // Для эмодзи предпросмотр не нужен
                    break;
                case "theme":
                    SetThemePreviewColors(item);
                    break;
            }
        }

        private Color GetFramePreviewColor(int itemId)
        {
            return itemId switch
            {
                1 => Color.FromArgb("#FFD700"), // Золотая рамка
                2 => Color.FromArgb("#C0C0C0"), // Серебряная рамка
                3 => Color.FromArgb("#CD7F32"), // Бронзовая рамка
                4 => Color.FromArgb("#FF6B6B"), // Красная рамка
                5 => Color.FromArgb("#4ECDC4"), // Бирюзовая рамка
                _ => Color.FromArgb("#669BBC")  // Стандартная рамка
            };
        }

        private void SetThemePreviewColors(ShopItem item)
        {
            item.PreviewPrimaryColor = item.ItemId switch
            {
                11 => Color.FromArgb("#2E86AB"), // Океан
                12 => Color.FromArgb("#A23B72"), // Пурпур
                13 => Color.FromArgb("#2A9D8F"), // Джунгли
                14 => Color.FromArgb("#E76F51"), // Закат
                _ => Color.FromArgb("#669BBC")   // Стандарт
            };

            item.PreviewSecondaryColor = Color.FromArgb("#003049");
            item.PreviewAccentColor = Color.FromArgb("#C1121F");
            item.PreviewBackgroundColor = Color.FromArgb("#FDF0D5");
            item.PreviewTextColor = Color.FromArgb("#003049");
        }

        // ОБРАБОТЧИКИ ПОКУПОК
        private async void OnFramePurchaseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ShopItem item)
            {
                await ProcessItemPurchase(item);
            }
        }

        private async void OnEmojiPurchaseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ShopItem item)
            {
                await ProcessItemPurchase(item);
            }
        }

        private async void OnThemePurchaseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is ShopItem item)
            {
                await ProcessItemPurchase(item);
            }
        }

        private async Task ProcessItemPurchase(ShopItem item)
        {
            try
            {
                ShopLoadingOverlay.IsVisible = true;
                if (!item.IsPurchased)
                {
                    // ПОКУПКА товара
                    if (UserGameCurrency >= item.Price)
                    {
                        bool success = await _dbService.PurchaseShopItemAsync(_currentUser.UserId, item.ItemId, item.Price);
                        if (success)
                        {
                            UserGameCurrency -= item.Price;
                            _currentUser.GameCurrency = UserGameCurrency;
                            item.IsPurchased = true;

                            await DisplayAlert("Успех", $"Товар \"{item.Name}\" приобретен!", "OK");
                            LoadShopItems();
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось совершить покупку", "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Недостаточно средств", $"Вам нужно еще {item.Price - UserGameCurrency} монет 🪙", "OK");
                    }
                }
                else
                {
                    // НАДЕВАНИЕ/СНИМАНИЕ товара
                    if (!item.IsEquipped)
                    {
                        // Надеваем товар
                        bool success = await _dbService.EquipShopItemAsync(_currentUser.UserId, item.ItemId, item.ItemType);
                        if (success)
                        {
                            item.IsEquipped = true;
                            await DisplayAlert("Успех", $"\"{item.Name}\" теперь активно!", "OK");

                            // Если это тема, применяем её
                            if (item.ItemType == "theme")
                            {
                                ApplyPurchasedTheme(item);
                            }

                            LoadShopItems();
                        }
                    }
                    else
                    {
                        // Снимаем товар
                        bool success = await _dbService.UnequipShopItemAsync(_currentUser.UserId, item.ItemType);
                        if (success)
                        {
                            item.IsEquipped = false;
                            await DisplayAlert("Успех", $"\"{item.Name}\" снято", "OK");
                            LoadShopItems();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка операции: {ex.Message}", "OK");
            }
            finally
            {
                ShopLoadingOverlay.IsVisible = false;
            }
        }

        private void ApplyPurchasedTheme(ShopItem theme)
        {
            // Применяем купленную тему
            string themeName = theme.Name.ToLower();
            if (themeName.Contains("океан"))
            {
                _settingsService.ApplyTheme("ocean");
            }
            else if (themeName.Contains("пурпур"))
            {
                _settingsService.ApplyTheme("purple");
            }
            // Добавьте другие темы по необходимости
        }

        private async void OnInventoryClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new InventoryPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть инвентарь: {ex.Message}", "OK");
            }
        }

        private async void OnEarnCoinsClicked(object sender, EventArgs e)
        {
            try
            {
                bool ok = await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50, _settingsService?.CurrentLanguage == "ru" ? "Ежедневный бонус" : "Daily bonus");
                if (ok)
                {
                    UserGameCurrency = await _dbService.GetUserGameCurrencyAsync(_currentUser.UserId);
                    _currentUser.GameCurrency = UserGameCurrency;
                    await DisplayAlert("Успех", "+50 монет начислено!", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось начислить монеты", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка начисления: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadUserData();
            InitialLoadingOverlay.IsVisible = true;
            ShopContent.IsVisible = false;
            await LoadShopItems();
            InitialLoadingOverlay.IsVisible = false;
            ShopContent.IsVisible = true;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}