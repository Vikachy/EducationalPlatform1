using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class CreateContestPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private List<ProgrammingLanguage> _languages = new();

        public CreateContestPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
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

            // Устанавливаем минимальные даты
            if (StartDatePicker != null && EndDatePicker != null)
            {
                StartDatePicker.MinimumDate = DateTime.Today;
                EndDatePicker.MinimumDate = DateTime.Today.AddDays(1);
                StartDatePicker.Date = DateTime.Today;
                EndDatePicker.Date = DateTime.Today.AddDays(7);
            }

            LoadLanguages();
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            try
            {
                Title = _localizationService.GetText("CreateContest");

                if (HeaderLabel != null)
                    HeaderLabel.Text = _localizationService.GetText("CreateContest");

                if (ContestNameEntry != null)
                    ContestNameEntry.Placeholder = _localizationService.GetText("ContestName");

                if (DescriptionEditor != null)
                    DescriptionEditor.Placeholder = _localizationService.GetText("ContestDescription");

                if (LanguagePicker != null)
                    LanguagePicker.Title = _localizationService.GetText("SelectLanguage");

                if (StartDateLabel != null)
                    StartDateLabel.Text = _localizationService.GetText("StartDate");

                if (EndDateLabel != null)
                    EndDateLabel.Text = _localizationService.GetText("EndDate");

                if (PrizeLabel != null)
                    PrizeLabel.Text = _localizationService.GetText("PrizeCoins");

                if (MaxParticipantsLabel != null)
                    MaxParticipantsLabel.Text = _localizationService.GetText("MaxParticipants");

                if (OnlyForGroupsLabel != null)
                    OnlyForGroupsLabel.Text = _localizationService.GetText("OnlyForGroups");

                if (CreateButton != null)
                    CreateButton.Text = _localizationService.GetText("Create");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления текстов: {ex.Message}");
            }
        }

        private async void LoadLanguages()
        {
            try
            {
                _languages = await _dbService.GetProgrammingLanguagesAsync();

                var items = new List<string> { _localizationService.GetText("AnyLanguage") };
                items.AddRange(_languages.Select(l => l.LanguageName));

                if (LanguagePicker != null)
                {
                    LanguagePicker.ItemsSource = items;
                    LanguagePicker.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error"),
                    $"Ошибка загрузки языков: {ex.Message}",
                    _localizationService.GetText("OK"));
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("🔍 Нажата кнопка создания конкурса");

                // Валидация
                if (ContestNameEntry == null)
                {
                    Console.WriteLine("❌ ContestNameEntry is null");
                    await DisplayAlert("Ошибка", "Ошибка инициализации страницы", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContestNameEntry.Text))
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("EnterContestName"),
                        _localizationService.GetText("OK"));
                    return;
                }

                if (StartDatePicker == null || EndDatePicker == null)
                {
                    Console.WriteLine("❌ DatePicker is null");
                    await DisplayAlert("Ошибка", "Ошибка инициализации дат", "OK");
                    return;
                }

                if (EndDatePicker.Date <= StartDatePicker.Date)
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("EndDateMustBeAfterStart"),
                        _localizationService.GetText("OK"));
                    return;
                }

                // Показываем индикатор загрузки
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = true;
                    Console.WriteLine("🔄 Показан индикатор загрузки");
                }

                // Собираем данные
                int languageId = 0;
                if (LanguagePicker != null && LanguagePicker.SelectedIndex > 0 && _languages.Count > 0)
                {
                    var selectedIndex = LanguagePicker.SelectedIndex - 1;
                    if (selectedIndex >= 0 && selectedIndex < _languages.Count)
                    {
                        languageId = _languages[selectedIndex].LanguageId;
                        Console.WriteLine($"   Выбран язык ID: {languageId}");
                    }
                }

                int prize = 0;
                if (PrizeEntry != null && !string.IsNullOrWhiteSpace(PrizeEntry.Text))
                {
                    if (int.TryParse(PrizeEntry.Text, out int parsedPrize))
                    {
                        prize = parsedPrize;
                        Console.WriteLine($"   Призовые монеты: {prize}");
                    }
                }

                int? maxParticipants = null;
                if (MaxParticipantsEntry != null && !string.IsNullOrWhiteSpace(MaxParticipantsEntry.Text))
                {
                    if (int.TryParse(MaxParticipantsEntry.Text, out int max))
                    {
                        maxParticipants = max;
                        Console.WriteLine($"   Макс. участников: {max}");
                    }
                }

                bool onlyForGroups = OnlyForGroupsCheckBox != null && OnlyForGroupsCheckBox.IsChecked;
                Console.WriteLine($"   Только для групп: {onlyForGroups}");

                var contest = new Contest
                {
                    ContestName = ContestNameEntry.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(DescriptionEditor?.Text) ? null : DescriptionEditor.Text.Trim(),
                    LanguageId = languageId,
                    StartDate = StartDatePicker.Date,
                    EndDate = EndDatePicker.Date.AddDays(1).AddSeconds(-1),
                    PrizeCurrency = prize,
                    MaxParticipants = maxParticipants,
                    OnlyForGroups = onlyForGroups
                };

                Console.WriteLine($"📦 Данные конкурса собраны:");
                Console.WriteLine($"   Название: {contest.ContestName}");
                Console.WriteLine($"   Описание: {contest.Description ?? "null"}");
                Console.WriteLine($"   LanguageId: {contest.LanguageId}");
                Console.WriteLine($"   StartDate: {contest.StartDate:yyyy-MM-dd}");
                Console.WriteLine($"   EndDate: {contest.EndDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Prize: {contest.PrizeCurrency}");
                Console.WriteLine($"   MaxParticipants: {contest.MaxParticipants?.ToString() ?? "null"}");
                Console.WriteLine($"   OnlyForGroups: {contest.OnlyForGroups}");
                Console.WriteLine($"   CreatedByUserId: {_currentUser.UserId}");


                // Создаем конкурс
                Console.WriteLine("📤 Отправка запроса на создание конкурса...");
                bool success = await _dbService.CreateContestAsync(contest, _currentUser.UserId);

                if (success)
                {
                    Console.WriteLine("✅ Конкурс успешно создан!");
                    await DisplayAlert(_localizationService.GetText("Success"),
                        _localizationService.GetText("ContestCreated"),
                        _localizationService.GetText("OK"));

                    // Отправляем сообщение об успешном создании
                    MessagingCenter.Send(this, "ContestCreated");

                    await Navigation.PopAsync();
                }
                else
                {
                    Console.WriteLine("❌ Не удалось создать конкурс");
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("FailedToCreateContest"),
                        _localizationService.GetText("OK"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 КРИТИЧЕСКАЯ ОШИБКА:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                await DisplayAlert(_localizationService.GetText("Error"),
                    $"Ошибка: {ex.Message}",
                    _localizationService.GetText("OK"));
            }
            finally
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = false;
                    Console.WriteLine("🔄 Индикатор загрузки скрыт");
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}