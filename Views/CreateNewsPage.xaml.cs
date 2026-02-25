using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CreateNewsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;

        public CreateNewsPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            LoadPickers();
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("CreateNews") ?? "Создание новости";

            if (HeaderLabel != null)
                HeaderLabel.Text = _localizationService.GetText("CreateNews") ?? "Создание новости";

            if (TitleEntry != null)
                TitleEntry.Placeholder = _localizationService.GetText("NewsTitle") ?? "Заголовок новости";

            if (SummaryEditor != null)
                SummaryEditor.Placeholder = _localizationService.GetText("NewsSummary") ?? "Краткое описание";

            if (ContentEditor != null)
                ContentEditor.Placeholder = _localizationService.GetText("NewsContent") ?? "Полный текст новости";

            if (ForTeensLabel != null)
                ForTeensLabel.Text = _localizationService.GetText("ForTeens") ?? "Для подростков";

            if (ImageUrlEntry != null)
                ImageUrlEntry.Placeholder = _localizationService.GetText("ImageUrl") ?? "URL изображения";

            if (CreateButton != null)
                CreateButton.Text = _localizationService.GetText("Create") ?? "📰 Создать новость";

            if (LoadingLabel != null)
                LoadingLabel.Text = _localizationService.GetText("Creating") ?? "Создание...";
        }

        private void LoadPickers()
        {
            // Категории
            var categories = new List<string>
            {
                _localizationService.GetText("General") ?? "📰 Общее",
                _localizationService.GetText("Courses") ?? "📚 Курсы",
                _localizationService.GetText("Contests") ?? "🏆 Конкурсы",
                _localizationService.GetText("System") ?? "⚙️ Система"
            };
            CategoryPicker.ItemsSource = categories;
            CategoryPicker.SelectedIndex = 0;

            // Языки
            var languages = new List<string>
            {
                _localizationService.GetText("Russian") ?? "Русский",
                _localizationService.GetText("English") ?? "English"
            };
            LanguagePicker.ItemsSource = languages;
            LanguagePicker.SelectedIndex = _currentUser.LanguagePref == "en" ? 1 : 0;
        }

        private string GetCategoryKey(string displayName)
        {
            if (displayName.Contains("Курсы") || displayName.Contains("Courses"))
                return "courses";
            if (displayName.Contains("Конкурсы") || displayName.Contains("Contests"))
                return "contests";
            if (displayName.Contains("Система") || displayName.Contains("System"))
                return "system";
            return "general";
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(TitleEntry?.Text))
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("EnterTitle") ?? "Введите заголовок новости",
                        _localizationService.GetText("OK") ?? "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContentEditor?.Text))
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("EnterContent") ?? "Введите текст новости",
                        _localizationService.GetText("OK") ?? "OK");
                    return;
                }

                // Показываем индикатор загрузки
                if (LoadingOverlay != null)
                    LoadingOverlay.IsVisible = true;

                var news = new News
                {
                    Title = TitleEntry.Text.Trim(),
                    Content = ContentEditor.Text.Trim(),
                    Summary = string.IsNullOrWhiteSpace(SummaryEditor?.Text) ? null : SummaryEditor.Text.Trim(),
                    Category = GetCategoryKey(CategoryPicker.SelectedItem?.ToString() ?? ""),
                    LanguageCode = LanguagePicker.SelectedIndex == 0 ? "ru" : "en",
                    ForTeens = ForTeensCheckBox.IsChecked,
                    ImageUrl = string.IsNullOrWhiteSpace(ImageUrlEntry?.Text) ? null : ImageUrlEntry.Text.Trim()
                };

                bool success = await _dbService.CreateNewsAsync(news, _currentUser.UserId);

                if (success)
                {
                    await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                        _localizationService.GetText("NewsCreated") ?? "Новость успешно создана",
                        _localizationService.GetText("OK") ?? "OK");

                    MessagingCenter.Send(this, "NewsCreated");

                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("FailedToCreateNews") ?? "Не удалось создать новость",
                        _localizationService.GetText("OK") ?? "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                    ex.Message,
                    _localizationService.GetText("OK") ?? "OK");
            }
            finally
            {
                if (LoadingOverlay != null)
                    LoadingOverlay.IsVisible = false;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}