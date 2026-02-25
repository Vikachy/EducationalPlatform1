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
        private LocalizationService _localizationService;

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
        public ObservableCollection<ShopItem> Badges { get; set; }

        public ShopPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            AvatarFrames = new ObservableCollection<ShopItem>();
            ProfileEmojis = new ObservableCollection<ShopItem>();
            Themes = new ObservableCollection<ShopItem>();
            Badges = new ObservableCollection<ShopItem>();

            BindingContext = this;

            _localizationService.LanguageChanged += OnLanguageChanged;

            LoadUserData();
        }

        private void OnLanguageChanged(object? sender, string language)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                UpdateTexts();
                UpdateAllButtonTexts();
            });
        }

        private void UpdateTexts()
        {
            Title = _localizationService?.GetText("Shop") ?? "Магазин";

            var titleLabel = this.FindByName<Label>("TitleLabel");
            if (titleLabel != null)
                titleLabel.Text = _localizationService?.GetText("Shop") ?? "Магазин";

            var framesTitle = this.FindByName<Label>("FramesTitle");
            if (framesTitle != null)
                framesTitle.Text = _localizationService?.GetText("AvatarFrames") ?? "Рамки профиля";

            var emojisTitle = this.FindByName<Label>("EmojisTitle");
            if (emojisTitle != null)
                emojisTitle.Text = _localizationService?.GetText("ProfileEmojis") ?? "Эмодзи для профиля";

            var themesTitle = this.FindByName<Label>("ThemesTitle");
            if (themesTitle != null)
                themesTitle.Text = _localizationService?.GetText("Themes") ?? "Темы оформления";

            var badgesTitle = this.FindByName<Label>("BadgesTitle");
            if (badgesTitle != null)
                badgesTitle.Text = _localizationService?.GetText("Badges") ?? "Значки";

            var inventoryBtn = this.FindByName<Button>("InventoryButton");
            if (inventoryBtn != null)
                inventoryBtn.Text = _localizationService?.GetText("MyInventory") ?? "Мой инвентарь";

            var earnCoinsBtn = this.FindByName<Button>("EarnCoinsButton");
            if (earnCoinsBtn != null)
                earnCoinsBtn.Text = _localizationService?.GetText("EarnCoins") ?? "Получить монеты";

            var loadingText = this.FindByName<Label>("LoadingText");
            if (loadingText != null)
                loadingText.Text = _localizationService?.GetText("Loading") ?? "Загрузка...";
        }

        private void LoadUserData()
        {
            UserGameCurrency = _currentUser.GameCurrency;
        }

        private string GetThemeKeyFromShopItem(ShopItem item)
        {
            return ThemeColorService.GetThemeKeyFromName(item.Name);
        }

        private void SetItemVisualProperties(ShopItem item)
        {
            if (item.IsPurchased)
            {
                if (item.IsEquipped)
                {
                    item.ButtonText = _localizationService?.GetText("Equipped") ?? "✅ Надето";
                    item.ButtonColor = Color.FromArgb("#4CAF50");
                    item.BorderColor = Color.FromArgb("#4CAF50");
                }
                else
                {
                    item.ButtonText = _localizationService?.GetText("Equip") ?? "Надеть";
                    item.ButtonColor = Color.FromArgb("#2196F3");
                    item.BorderColor = Color.FromArgb("#2196F3");
                }
            }
            else
            {
                item.ButtonText = _localizationService?.GetText("Buy") ?? "Купить";
                item.ButtonColor = Color.FromArgb("#FF9800");
                item.BorderColor = Color.FromArgb("#DDD");
            }

            switch (item.ItemType.ToLower())
            {
                case "avatar_frame":
                    SetFramePreviewColor(item);
                    break;
                case "theme":
                    SetThemePreviewColors(item);
                    break;
            }
        }

        private void UpdateAllButtonTexts()
        {
            foreach (var item in AvatarFrames) SetItemVisualProperties(item);
            foreach (var item in ProfileEmojis) SetItemVisualProperties(item);
            foreach (var item in Themes) SetItemVisualProperties(item);
            foreach (var item in Badges) SetItemVisualProperties(item);
        }

        private void SetFramePreviewColor(ShopItem item)
        {
            string nameLower = item.Name.ToLower();

            // Существующие рамки
            if (nameLower.Contains("золот"))
                item.PreviewColor = Color.FromArgb("#FFD700");
            else if (nameLower.Contains("серебр"))
                item.PreviewColor = Color.FromArgb("#C0C0C0");
            else if (nameLower.Contains("бронз"))
                item.PreviewColor = Color.FromArgb("#CD7F32");
            else if (nameLower.Contains("красн"))
                item.PreviewColor = Color.FromArgb("#FF6B6B");
            else if (nameLower.Contains("бирюз"))
                item.PreviewColor = Color.FromArgb("#4ECDC4");
            else if (nameLower.Contains("фиолет"))
                item.PreviewColor = Color.FromArgb("#9C27B0");
            else if (nameLower.Contains("оранж"))
                item.PreviewColor = Color.FromArgb("#FF9800");
            else if (nameLower.Contains("неон"))
                item.PreviewColor = Color.FromArgb("#00FFFF");
            else if (nameLower.Contains("лазер"))
                item.PreviewColor = Color.FromArgb("#FF00FF");
            else if (nameLower.Contains("кибер"))
                item.PreviewColor = Color.FromArgb("#00FF00");
            else if (nameLower.Contains("полос"))
                item.PreviewColor = Color.FromArgb("#FFA500");
            else if (nameLower.Contains("карт"))
                item.PreviewColor = Color.FromArgb("#8B4513");
            else if (nameLower.Contains("радуж"))
                item.PreviewColor = Color.FromArgb("#FF1493");
            else if (nameLower.Contains("космич"))
                item.PreviewColor = Color.FromArgb("#4B0082");
            else if (nameLower.Contains("огнен"))
                item.PreviewColor = Color.FromArgb("#FF4500");
            else if (nameLower.Contains("ледян"))
                item.PreviewColor = Color.FromArgb("#ADD8E6");
            else if (nameLower.Contains("цветоч"))
                item.PreviewColor = Color.FromArgb("#FFB6C1");
            else if (nameLower.Contains("ретро"))
                item.PreviewColor = Color.FromArgb("#CD7F32");

            // НОВЫЕ РАМКИ (25+ штук)
            else if (nameLower.Contains("пастель") || nameLower.Contains("pastel"))
                item.PreviewColor = Color.FromArgb("#FFD1DC");
            else if (nameLower.Contains("металл") || nameLower.Contains("metal"))
                item.PreviewColor = Color.FromArgb("#C0C0C0");
            else if (nameLower.Contains("драгоц") || nameLower.Contains("gem"))
                item.PreviewColor = Color.FromArgb("#E0115F");
            else if (nameLower.Contains("кристалл") || nameLower.Contains("crystal"))
                item.PreviewColor = Color.FromArgb("#B0E0E6");
            else if (nameLower.Contains("пиксель") || nameLower.Contains("pixel"))
                item.PreviewColor = Color.FromArgb("#32CD32");
            else if (nameLower.Contains("волн") || nameLower.Contains("wave"))
                item.PreviewColor = Color.FromArgb("#1E90FF");
            else if (nameLower.Contains("племен") || nameLower.Contains("tribal"))
                item.PreviewColor = Color.FromArgb("#8B4513");
            else if (nameLower.Contains("винтаж") || nameLower.Contains("vintage"))
                item.PreviewColor = Color.FromArgb("#DAA520");
            else if (nameLower.Contains("стимпанк") || nameLower.Contains("steampunk"))
                item.PreviewColor = Color.FromArgb("#8B4513");
            else if (nameLower.Contains("киберпанк") || nameLower.Contains("cyberpunk"))
                item.PreviewColor = Color.FromArgb("#FF00FF");
            else if (nameLower.Contains("фэнтези") || nameLower.Contains("fantasy"))
                item.PreviewColor = Color.FromArgb("#9932CC");
            else if (nameLower.Contains("мистич") || nameLower.Contains("mystic"))
                item.PreviewColor = Color.FromArgb("#4B0082");
            else if (nameLower.Contains("магич") || nameLower.Contains("magic"))
                item.PreviewColor = Color.FromArgb("#9400D3");
            else if (nameLower.Contains("эльф") || nameLower.Contains("elf"))
                item.PreviewColor = Color.FromArgb("#228B22");
            else if (nameLower.Contains("дракон") || nameLower.Contains("dragon"))
                item.PreviewColor = Color.FromArgb("#DC143C");
            else if (nameLower.Contains("фея") || nameLower.Contains("fairy"))
                item.PreviewColor = Color.FromArgb("#FFB6C1");
            else if (nameLower.Contains("единорог") || nameLower.Contains("unicorn"))
                item.PreviewColor = Color.FromArgb("#FF69B4");
            else if (nameLower.Contains("русал") || nameLower.Contains("mermaid"))
                item.PreviewColor = Color.FromArgb("#20B2AA");
            else if (nameLower.Contains("пират") || nameLower.Contains("pirate"))
                item.PreviewColor = Color.FromArgb("#8B4513");
            else if (nameLower.Contains("ковбой") || nameLower.Contains("cowboy"))
                item.PreviewColor = Color.FromArgb("#CD853F");
            else if (nameLower.Contains("самурай") || nameLower.Contains("samurai"))
                item.PreviewColor = Color.FromArgb("#B22222");
            else if (nameLower.Contains("ниндзя") || nameLower.Contains("ninja"))
                item.PreviewColor = Color.FromArgb("#2F4F4F");
            else if (nameLower.Contains("супергерой") || nameLower.Contains("superhero"))
                item.PreviewColor = Color.FromArgb("#4169E1");
            else if (nameLower.Contains("зомби") || nameLower.Contains("zombie"))
                item.PreviewColor = Color.FromArgb("#228B22");
            else if (nameLower.Contains("вампир") || nameLower.Contains("vampire"))
                item.PreviewColor = Color.FromArgb("#8B0000");
            else if (nameLower.Contains("оборотень") || nameLower.Contains("werewolf"))
                item.PreviewColor = Color.FromArgb("#696969");
            else if (nameLower.Contains("призрак") || nameLower.Contains("ghost"))
                item.PreviewColor = Color.FromArgb("#F8F8FF");
            else if (nameLower.Contains("скелет") || nameLower.Contains("skeleton"))
                item.PreviewColor = Color.FromArgb("#F5F5F5");
            else if (nameLower.Contains("клоун") || nameLower.Contains("clown"))
                item.PreviewColor = Color.FromArgb("#FF69B4");
            else if (nameLower.Contains("робот") || nameLower.Contains("robot"))
                item.PreviewColor = Color.FromArgb("#00CED1");
            else
                item.PreviewColor = Color.FromArgb("#669BBC");
        }

        private void SetThemePreviewColors(ShopItem item)
        {
            string themeKey = GetThemeKeyFromShopItem(item);
            var colors = ThemeColorService.GetThemeColors(themeKey);

            item.PreviewPrimaryColor = colors.Primary;
            item.PreviewSecondaryColor = colors.Secondary;
            item.PreviewAccentColor = colors.Accent;
            item.PreviewBackgroundColor = colors.Background;
            item.PreviewTextColor = colors.Text;
        }

        private async Task LoadShopItems()
        {
            try
            {
                var shopItems = await _dbService.GetShopItemsAsync();

                AvatarFrames.Clear();
                ProfileEmojis.Clear();
                Themes.Clear();
                Badges.Clear();

                foreach (var item in shopItems)
                {
                    item.IsPurchased = await _dbService.CheckItemOwnershipAsync(_currentUser.UserId, item.ItemId);
                    item.IsEquipped = await _dbService.CheckItemEquippedAsync(_currentUser.UserId, item.ItemId);

                    SetItemVisualProperties(item);

                    switch (item.ItemType.ToLower())
                    {
                        case "avatar_frame":
                            AvatarFrames.Add(item);
                            break;
                        case "emoji":
                            ProfileEmojis.Add(item);
                            break;
                        case "theme":
                            Themes.Add(item);
                            break;
                        case "badge":
                            Badges.Add(item);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("FailedToLoadItems") ?? "Не удалось загрузить товары"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

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

        private async void OnBadgePurchaseClicked(object sender, EventArgs e)
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
                    // ПОКУПКА
                    if (UserGameCurrency >= item.Price)
                    {
                        bool success = await _dbService.PurchaseShopItemAsync(_currentUser.UserId, item.ItemId, item.Price);
                        if (success)
                        {
                            UserGameCurrency -= item.Price;
                            _currentUser.GameCurrency = UserGameCurrency;
                            item.IsPurchased = true;

                            await DisplayAlert(
                                _localizationService?.GetText("Success") ?? "Успех",
                                string.Format(_localizationService?.GetText("ItemPurchased") ?? "Товар \"{0}\" приобретен!", item.Name),
                                _localizationService?.GetText("OK") ?? "OK");

                            // После покупки автоматически надеваем предмет
                            await EquipItem(item);

                            // Обновляем UI
                            SetItemVisualProperties(item);

                            // Отправляем событие об обновлении инвентаря
                            MessagingCenter.Send(this, "InventoryUpdated");
                        }
                        else
                        {
                            await DisplayAlert(
                                _localizationService?.GetText("Error") ?? "Ошибка",
                                _localizationService?.GetText("PurchaseFailed") ?? "Не удалось совершить покупку",
                                _localizationService?.GetText("OK") ?? "OK");
                        }
                    }
                    else
                    {
                        await DisplayAlert(
                            _localizationService?.GetText("NotEnoughCoins") ?? "Недостаточно средств",
                            string.Format(_localizationService?.GetText("NeedMoreCoins") ?? "Вам нужно еще {0} монет 🪙", item.Price - UserGameCurrency),
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
                else
                {
                    // НАДЕВАНИЕ/СНИМАНИЕ
                    if (!item.IsEquipped)
                    {
                        await EquipItem(item);
                    }
                    else
                    {
                        await UnequipItem(item);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("OperationError") ?? "Ошибка операции"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
            finally
            {
                ShopLoadingOverlay.IsVisible = false;
            }
        }

        private async Task EquipItem(ShopItem item)
        {
            // Сначала снимаем все предметы того же типа
            await _dbService.UnequipAllItemsByTypeAsync(_currentUser.UserId, item.ItemType);

            // Надеваем новый
            bool success = await _dbService.EquipShopItemAsync(_currentUser.UserId, item.ItemId, item.ItemType);
            if (success)
            {
                // Обновляем статусы всех предметов этого типа
                await UpdateEquippedStatus(item.ItemType, item.ItemId);

                await DisplayAlert(
                    _localizationService?.GetText("Success") ?? "Успех",
                    string.Format(_localizationService?.GetText("ItemEquipped") ?? "\"{0}\" теперь активно!", item.Name),
                    _localizationService?.GetText("OK") ?? "OK");

                // Если это тема, применяем её
                if (item.ItemType == "theme")
                {
                    ApplyPurchasedTheme(item);
                }

                // Отправляем события для обновления UI на других страницах
                if (item.ItemType == "avatar_frame")
                {
                    UserSessionService.OnAvatarChanged(_currentUser.UserId, _currentUser.AvatarUrl);
                    MessagingCenter.Send(this, "FrameChanged", item.ItemId);
                }

                if (item.ItemType == "emoji")
                {
                    UserSessionService.OnAvatarChanged(_currentUser.UserId, _currentUser.AvatarUrl);
                    MessagingCenter.Send(this, "EmojiChanged", item.Icon);
                }

                // Отправляем общее событие об обновлении
                MessagingCenter.Send(this, "InventoryUpdated");
            }
        }

        private async Task UnequipItem(ShopItem item)
        {
            bool success = await _dbService.UnequipShopItemAsync(_currentUser.UserId, item.ItemId, item.ItemType);
            if (success)
            {
                item.IsEquipped = false;

                if (item.ItemType == "theme")
                {
                    _settingsService.CurrentTheme = "standard";
                    await _dbService.SaveUserThemeAsync(_currentUser.UserId, "standard");
                }

                if (item.ItemType == "avatar_frame")
                {
                    UserSessionService.OnAvatarChanged(_currentUser.UserId, _currentUser.AvatarUrl);
                    MessagingCenter.Send(this, "FrameChanged", (int?)null);
                }

                if (item.ItemType == "emoji")
                {
                    UserSessionService.OnAvatarChanged(_currentUser.UserId, _currentUser.AvatarUrl);
                    MessagingCenter.Send(this, "EmojiChanged", (string?)null);
                }

                await DisplayAlert(
                    _localizationService?.GetText("Success") ?? "Успех",
                    string.Format(_localizationService?.GetText("ItemUnequipped") ?? "\"{0}\" снято", item.Name),
                    _localizationService?.GetText("OK") ?? "OK");

                SetItemVisualProperties(item);

                // Отправляем общее событие об обновлении
                MessagingCenter.Send(this, "InventoryUpdated");
            }
        }

        private async Task UpdateEquippedStatus(string itemType, int equippedItemId)
        {
            var collections = itemType switch
            {
                "avatar_frame" => AvatarFrames,
                "emoji" => ProfileEmojis,
                "theme" => Themes,
                "badge" => Badges,
                _ => null
            };

            if (collections != null)
            {
                foreach (var item in collections)
                {
                    item.IsEquipped = item.ItemId == equippedItemId;
                    SetItemVisualProperties(item);
                }
            }
        }

        private async void ApplyPurchasedTheme(ShopItem theme)
        {
            try
            {
                string themeKey = GetThemeKeyFromShopItem(theme);

                if (_settingsService.CurrentTheme != themeKey)
                {
                    _settingsService.CurrentTheme = themeKey;
                    await _dbService.SaveUserThemeAsync(_currentUser.UserId, themeKey);
                }

                await UpdateEquippedStatus("theme", theme.ItemId);

                Console.WriteLine($"✅ Тема '{themeKey}' применена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка применения темы: {ex.Message}");
            }
        }

        private async void OnInventoryClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new InventoryPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("FailedToOpenInventory") ?? "Не удалось открыть инвентарь"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnEarnCoinsClicked(object sender, EventArgs e)
        {
            try
            {
                bool ok = await _dbService.AddGameCurrencyAsync(_currentUser.UserId, 50,
                    _localizationService?.CurrentLanguage == "ru" ? "Ежедневный бонус" : "Daily bonus");

                if (ok)
                {
                    UserGameCurrency = await _dbService.GetUserGameCurrencyAsync(_currentUser.UserId);
                    _currentUser.GameCurrency = UserGameCurrency;

                    await _dbService.AddCurrencyTransactionAsync(_currentUser.UserId, 50, "income",
                        _localizationService?.CurrentLanguage == "ru" ? "Ежедневный бонус" : "Daily bonus");

                    await DisplayAlert(
                        _localizationService?.GetText("Success") ?? "Успех",
                        _localizationService?.GetText("CoinsAdded") ?? "+50 монет начислено!",
                        _localizationService?.GetText("OK") ?? "OK");
                }
                else
                {
                    await DisplayAlert(
                        _localizationService?.GetText("Error") ?? "Ошибка",
                        _localizationService?.GetText("FailedToAddCoins") ?? "Не удалось начислить монеты",
                        _localizationService?.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    _localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("Error") ?? "Ошибка"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Обновляем баланс при каждом появлении страницы
            UserGameCurrency = await _dbService.GetUserGameCurrencyAsync(_currentUser.UserId);
            _currentUser.GameCurrency = UserGameCurrency;

            UpdateTexts();

            InitialLoadingOverlay.IsVisible = true;
            ShopContent.IsVisible = false;

            await LoadShopItems();

            InitialLoadingOverlay.IsVisible = false;
            ShopContent.IsVisible = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _localizationService.LanguageChanged -= OnLanguageChanged;

            MessagingCenter.Unsubscribe<ShopPage, int?>(this, "FrameChanged");
            MessagingCenter.Unsubscribe<ShopPage, string?>(this, "EmojiChanged");
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}