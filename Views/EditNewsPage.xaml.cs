using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class EditNewsPage : ContentPage
    {
        private readonly News _news;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private List<string> _categories = new();

        public EditNewsPage(News news, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditNewsPage: {ex.Message}");
            }

            _news = news;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            LoadPickers();
            LoadNewsData();
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            Title = _localizationService.GetText("EditNews") ?? "Редактирование новости";

            if (HeaderLabel != null)
                HeaderLabel.Text = _localizationService.GetText("EditNews") ?? "Редактирование новости";

            if (TitleEntry != null)
                TitleEntry.Placeholder = _localizationService.GetText("NewsTitle") ?? "Заголовок новости";

            if (SummaryEditor != null)
                SummaryEditor.Placeholder = _localizationService.GetText("NewsSummary") ?? "Краткое описание";

            if (ContentEditor != null)
                ContentEditor.Placeholder = _localizationService.GetText("NewsContent") ?? "Полный текст новости";

            if (ForTeensLabel != null)
                ForTeensLabel.Text = _localizationService.GetText("ForTeens") ?? "Для подростков";

            if (IsActiveLabel != null)
                IsActiveLabel.Text = _localizationService.GetText("NewsActive") ?? "Новость активна";

            if (ImageUrlEntry != null)
                ImageUrlEntry.Placeholder = _localizationService.GetText("ImageUrl") ?? "URL изображения";

            if (SaveButton != null)
                SaveButton.Text = _localizationService.GetText("Save") ?? "💾 Сохранить";

            if (CancelButton != null)
                CancelButton.Text = _localizationService.GetText("Cancel") ?? "❌ Отмена";

            if (LoadingLabel != null)
                LoadingLabel.Text = _localizationService.GetText("Saving") ?? "Сохранение...";
        }

        private void LoadPickers()
        {
            // Категории
            _categories = new List<string>
            {
                _localizationService.GetText("General") ?? "📰 Общее",
                _localizationService.GetText("Courses") ?? "📚 Курсы",
                _localizationService.GetText("Contests") ?? "🏆 Конкурсы",
                _localizationService.GetText("System") ?? "⚙️ Система"
            };
            CategoryPicker.ItemsSource = _categories;

            // Языки
            var languages = new List<string>
            {
                _localizationService.GetText("Russian") ?? "Русский",
                _localizationService.GetText("English") ?? "English"
            };
            LanguagePicker.ItemsSource = languages;
        }

        private void LoadNewsData()
        {
            if (_news == null) return;

            // Заполняем поля
            TitleEntry.Text = _news.Title;
            SummaryEditor.Text = _news.Summary;
            ContentEditor.Text = _news.Content;

            // Устанавливаем категорию
            var categoryIndex = _news.Category switch
            {
                "courses" => 1,
                "contests" => 2,
                "system" => 3,
                _ => 0
            };
            CategoryPicker.SelectedIndex = categoryIndex;

            // Устанавливаем язык
            LanguagePicker.SelectedIndex = _news.LanguageCode == "en" ? 1 : 0;

            ForTeensCheckBox.IsChecked = _news.ForTeens;
            IsActiveCheckBox.IsChecked = _news.IsActive;
            ImageUrlEntry.Text = _news.ImageUrl;

            // Статистика
            ViewsCountLabel.Text = _news.ViewsCount.ToString();
            CreatedDateLabel.Text = _news.PublishedDate.ToString("dd.MM.yyyy HH:mm");
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

        private async void OnSaveClicked(object sender, EventArgs e)
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

                // Обновляем данные
                _news.Title = TitleEntry.Text.Trim();
                _news.Content = ContentEditor.Text.Trim();
                _news.Summary = string.IsNullOrWhiteSpace(SummaryEditor?.Text) ? null : SummaryEditor.Text.Trim();
                _news.Category = GetCategoryKey(CategoryPicker.SelectedItem?.ToString() ?? "");
                _news.LanguageCode = LanguagePicker.SelectedIndex == 0 ? "ru" : "en";
                _news.ForTeens = ForTeensCheckBox.IsChecked;
                _news.IsActive = IsActiveCheckBox.IsChecked;
                _news.ImageUrl = string.IsNullOrWhiteSpace(ImageUrlEntry?.Text) ? null : ImageUrlEntry.Text.Trim();

                bool success = await _dbService.UpdateNewsAsync(_news);

                if (success)
                {
                    await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                        _localizationService.GetText("NewsUpdated") ?? "Новость успешно обновлена",
                        _localizationService.GetText("OK") ?? "OK");

                    // Отправляем сообщение об обновлении
                    MessagingCenter.Send(this, "NewsUpdated", _news.NewsId);

                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                        _localizationService.GetText("FailedToUpdateNews") ?? "Не удалось обновить новость",
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

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            try
            {
                bool confirm = await DisplayAlert(
                    _localizationService.GetText("Confirm") ?? "Подтверждение",
                    _localizationService.GetText("ConfirmDeleteNews") ?? "Вы уверены, что хотите удалить эту новость?",
                    _localizationService.GetText("Yes") ?? "Да",
                    _localizationService.GetText("No") ?? "Нет");

                if (confirm)
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = true;

                    bool success = await _dbService.DeleteNewsAsync(_news.NewsId);

                    if (success)
                    {
                        await DisplayAlert(_localizationService.GetText("Success") ?? "Успех",
                            _localizationService.GetText("NewsDeleted") ?? "Новость удалена",
                            _localizationService.GetText("OK") ?? "OK");

                        // Отправляем сообщение об удалении
                        MessagingCenter.Send(this, "NewsDeleted", _news.NewsId);

                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert(_localizationService.GetText("Error") ?? "Ошибка",
                            _localizationService.GetText("FailedToDeleteNews") ?? "Не удалось удалить новость",
                            _localizationService.GetText("OK") ?? "OK");
                    }
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

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}