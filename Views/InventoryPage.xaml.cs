using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class InventoryPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<InventoryItemVm> Items { get; set; } = new();

        public InventoryPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadInventory();
        }

        private async void LoadInventory()
        {
            try
            {
                Items.Clear();
                var inv = await _dbService.GetUserInventoryAsync(_currentUser.UserId);
                foreach (var it in inv)
                {
                    Items.Add(new InventoryItemVm
                    {
                        ItemId = it.ItemId,
                        Name = it.Name,
                        Description = it.Description,
                        ItemType = it.ItemType,
                        IsEquipped = it.IsEquipped,
                        ActionText = it.IsEquipped ? "Снять" : "Надеть",
                        ButtonColor = it.IsEquipped ? Color.FromArgb("#9E9E9E") : Color.FromArgb("#2196F3")
                    });
                }

                InventoryCollection.ItemsSource = Items;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить инвентарь: {ex.Message}", "OK");
            }
        }

        private async void OnToggleEquipClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is InventoryItemVm vm)
            {
                try
                {
                    if (!vm.IsEquipped)
                    {
                        bool ok = await _dbService.EquipShopItemAsync(_currentUser.UserId, vm.ItemId, vm.ItemType);
                        if (ok)
                        {
                            await DisplayAlert("Успех", $"'{vm.Name}' активно!", "OK");
                            LoadInventory();
                        }
                    }
                    else
                    {
                        bool ok = await _dbService.UnequipShopItemAsync(_currentUser.UserId, vm.ItemType);
                        if (ok)
                        {
                            await DisplayAlert("Успех", $"'{vm.Name}' снято", "OK");
                            LoadInventory();
                        }
                    }

                    // Применение темы сразу
                    if (vm.ItemType == "theme" && vm.IsEquipped)
                    {
                        _settingsService.ApplyTheme(vm.Name.ToLower().Contains("океан") ? "ocean" : "standard");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка изменения состояния: {ex.Message}", "OK");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class InventoryItemVm
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public bool IsEquipped { get; set; }
        public string ActionText { get; set; } = string.Empty;
        public Color ButtonColor { get; set; } = Colors.Blue;
    }
}











