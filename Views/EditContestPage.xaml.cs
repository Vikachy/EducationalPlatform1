using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class EditContestPage : ContentPage
    {
        private readonly Contest _contest;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private List<ProgrammingLanguage> _languages = new();

        public EditContestPage(Contest contest, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditContestPage: {ex.Message}");
            }

            _contest = contest;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            BindingContext = this;

            LoadLanguages();
            LoadContestData();
            LoadStatistics();
            UpdateTexts();
            CheckPermissions();
        }

        private void UpdateTexts()
        {
            try
            {
                Title = _localizationService.GetText("EditContest");

                if (HeaderLabel != null)
                    HeaderLabel.Text = _localizationService.GetText("EditContest");

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

                if (IsActiveLabel != null)
                    IsActiveLabel.Text = _localizationService.GetText("ContestActive");

                if (CreatedByLabel != null)
                    CreatedByLabel.Text = _localizationService.GetText("CreatedBy");

                if (StatsTitleLabel != null)
                    StatsTitleLabel.Text = _localizationService.GetText("Statistics");

                if (SaveButton != null)
                    SaveButton.Text = "💾 " + _localizationService.GetText("Save");

                if (CancelButton != null)
                    CancelButton.Text = "❌ " + _localizationService.GetText("Cancel");

                if (DeleteButton != null)
                    DeleteButton.Text = "🗑️ " + _localizationService.GetText("DeleteContest");

                if (LoadingLabel != null)
                    LoadingLabel.Text = _localizationService.GetText("Loading");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления текстов: {ex.Message}");
            }
        }

        private void CheckPermissions()
        {
            // Проверяем, может ли пользователь редактировать этот конкурс
            bool canEdit = _currentUser.RoleId == 3 || // Admin
                          (_currentUser.RoleId == 2 && _contest.CreatedByUserId == _currentUser.UserId); // Teacher who created

            if (!canEdit)
            {
                // Блокируем поля для редактирования
                if (ContestNameEntry != null) ContestNameEntry.IsEnabled = false;
                if (DescriptionEditor != null) DescriptionEditor.IsEnabled = false;
                if (LanguagePicker != null) LanguagePicker.IsEnabled = false;
                if (StartDatePicker != null) StartDatePicker.IsEnabled = false;
                if (EndDatePicker != null) EndDatePicker.IsEnabled = false;
                if (PrizeEntry != null) PrizeEntry.IsEnabled = false;
                if (MaxParticipantsEntry != null) MaxParticipantsEntry.IsEnabled = false;
                if (OnlyForGroupsCheckBox != null) OnlyForGroupsCheckBox.IsEnabled = false;
                if (IsActiveCheckBox != null) IsActiveCheckBox.IsEnabled = false;
                if (SaveButton != null) SaveButton.IsEnabled = false;

                // Показываем сообщение
                DisplayAlert(_localizationService.GetText("AccessDenied"),
                    _localizationService.GetText("NoEditRights"),
                    _localizationService.GetText("OK"));
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
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(_localizationService.GetText("Error"),
                    $"Ошибка загрузки языков: {ex.Message}",
                    _localizationService.GetText("OK"));
            }
        }

        private void LoadContestData()
        {
            try
            {
                if (_contest == null) return;

                Console.WriteLine($"📝 Загрузка данных конкурса ID: {_contest.ContestId}");

                // Заполняем поля данными конкурса
                if (ContestNameEntry != null)
                    ContestNameEntry.Text = _contest.ContestName;

                if (DescriptionEditor != null)
                    DescriptionEditor.Text = _contest.Description;

                // Устанавливаем язык
                if (LanguagePicker != null && _languages.Any())
                {
                    if (_contest.LanguageId > 0)
                    {
                        var language = _languages.FirstOrDefault(l => l.LanguageId == _contest.LanguageId);
                        if (language != null)
                        {
                            var index = _languages.IndexOf(language) + 1; // +1 потому что первый элемент "Любой язык"
                            LanguagePicker.SelectedIndex = index;
                        }
                    }
                    else
                    {
                        LanguagePicker.SelectedIndex = 0; // "Любой язык"
                    }
                }

                // Устанавливаем даты
                if (StartDatePicker != null)
                    StartDatePicker.Date = _contest.StartDate;

                if (EndDatePicker != null)
                    EndDatePicker.Date = _contest.EndDate;

                // Призовые монеты
                if (PrizeEntry != null)
                    PrizeEntry.Text = _contest.PrizeCurrency.ToString();

                // Максимум участников
                if (MaxParticipantsEntry != null)
                    MaxParticipantsEntry.Text = _contest.MaxParticipants?.ToString();

                // Только для групп
                if (OnlyForGroupsCheckBox != null)
                    OnlyForGroupsCheckBox.IsChecked = _contest.OnlyForGroups;

                // Активен
                if (IsActiveCheckBox != null)
                    IsActiveCheckBox.IsChecked = _contest.IsActive;

                // Информация о создателе
                if (CreatedByValueLabel != null)
                    CreatedByValueLabel.Text = _contest.CreatedByName;

                if (CreatedDateLabel != null)
                    CreatedDateLabel.Text = string.Format(_localizationService.GetText("CreatedDateFormat"),
                        _contest.CreatedDate.ToString("dd.MM.yyyy HH:mm"));

                Console.WriteLine("✅ Данные конкурса загружены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки данных конкурса: {ex.Message}");
            }
        }

        private async void LoadStatistics()
        {
            try
            {
                // Загружаем заявки для этого конкурса
                var submissions = await _dbService.GetContestSubmissionsForContestAsync(_contest.ContestId);

                if (submissions.Any() && StatsBorder != null)
                {
                    StatsBorder.IsVisible = true;

                    var totalSubmissions = submissions.Count;
                    var gradedSubmissions = submissions.Count(s => s.TeacherScore.HasValue);
                    var pendingSubmissions = totalSubmissions - gradedSubmissions;

                    if (SubmissionsCountLabel != null)
                        SubmissionsCountLabel.Text = string.Format(_localizationService.GetText("TotalSubmissionsFormat"), totalSubmissions);

                    if (GradedCountLabel != null)
                        GradedCountLabel.Text = string.Format(_localizationService.GetText("GradedSubmissionsFormat"), gradedSubmissions, pendingSubmissions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                if (ContestNameEntry == null || string.IsNullOrWhiteSpace(ContestNameEntry.Text))
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("EnterContestName"),
                        _localizationService.GetText("OK"));
                    return;
                }

                if (StartDatePicker == null || EndDatePicker == null || EndDatePicker.Date <= StartDatePicker.Date)
                {
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("EndDateMustBeAfterStart"),
                        _localizationService.GetText("OK"));
                    return;
                }

                // Показываем индикатор загрузки
                if (LoadingOverlay != null)
                    LoadingOverlay.IsVisible = true;

                // Собираем обновленные данные
                int languageId = 0;
                if (LanguagePicker != null && LanguagePicker.SelectedIndex > 0 && _languages.Count > 0)
                {
                    var selectedIndex = LanguagePicker.SelectedIndex - 1;
                    if (selectedIndex >= 0 && selectedIndex < _languages.Count)
                    {
                        languageId = _languages[selectedIndex].LanguageId;
                    }
                }

                int prize = 0;
                if (PrizeEntry != null && !string.IsNullOrWhiteSpace(PrizeEntry.Text))
                {
                    int.TryParse(PrizeEntry.Text, out prize);
                }

                int? maxParticipants = null;
                if (MaxParticipantsEntry != null && !string.IsNullOrWhiteSpace(MaxParticipantsEntry.Text))
                {
                    if (int.TryParse(MaxParticipantsEntry.Text, out int max))
                    {
                        maxParticipants = max;
                    }
                }

                // Обновляем объект конкурса
                _contest.ContestName = ContestNameEntry.Text.Trim();
                _contest.Description = string.IsNullOrWhiteSpace(DescriptionEditor?.Text) ? null : DescriptionEditor.Text.Trim();
                _contest.LanguageId = languageId;
                _contest.StartDate = StartDatePicker.Date;
                _contest.EndDate = EndDatePicker.Date.AddDays(1).AddSeconds(-1);
                _contest.PrizeCurrency = prize;
                _contest.MaxParticipants = maxParticipants;
                _contest.OnlyForGroups = OnlyForGroupsCheckBox != null && OnlyForGroupsCheckBox.IsChecked;
                _contest.IsActive = IsActiveCheckBox != null && IsActiveCheckBox.IsChecked;

                Console.WriteLine($"📝 Сохранение изменений конкурса ID: {_contest.ContestId}");

                // Сохраняем изменения
                bool success = await _dbService.UpdateContestAsync(_contest);

                if (success)
                {
                    Console.WriteLine("✅ Конкурс успешно обновлен");

                    // Отправляем сообщение об обновлении
                    MessagingCenter.Send(this, "ContestUpdated", _contest.ContestId);

                    await DisplayAlert(_localizationService.GetText("Success"),
                        _localizationService.GetText("ContestUpdated"),
                        _localizationService.GetText("OK"));

                    await Navigation.PopAsync();
                }
                else
                {
                    Console.WriteLine("❌ Не удалось обновить конкурс");
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("FailedToUpdateContest"),
                        _localizationService.GetText("OK"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при сохранении: {ex.Message}");
                await DisplayAlert(_localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
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
                // Подтверждение удаления
                bool confirm = await DisplayAlert(_localizationService.GetText("Confirm"),
                    string.Format(_localizationService.GetText("ConfirmDeleteContest"), _contest.ContestName),
                    _localizationService.GetText("Yes"),
                    _localizationService.GetText("No"));

                if (!confirm) return;

                // Показываем индикатор загрузки
                if (LoadingOverlay != null)
                {
                    LoadingLabel.Text = _localizationService.GetText("Deleting");
                    LoadingOverlay.IsVisible = true;
                }

                Console.WriteLine($"🗑️ Удаление конкурса ID: {_contest.ContestId}");

                // Удаляем конкурс
                bool success = await _dbService.DeleteContestAsync(_contest.ContestId, _currentUser.UserId);

                if (success)
                {
                    Console.WriteLine("✅ Конкурс успешно удален");

                    // Отправляем сообщение об удалении
                    MessagingCenter.Send(this, "ContestDeleted", _contest.ContestId);

                    await DisplayAlert(_localizationService.GetText("Success"),
                        _localizationService.GetText("ContestDeleted"),
                        _localizationService.GetText("OK"));

                    await Navigation.PopAsync();
                }
                else
                {
                    Console.WriteLine("❌ Не удалось удалить конкурс");
                    await DisplayAlert(_localizationService.GetText("Error"),
                        _localizationService.GetText("FailedToDeleteContest"),
                        _localizationService.GetText("OK"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при удалении: {ex.Message}");
                await DisplayAlert(_localizationService.GetText("Error"),
                    ex.Message,
                    _localizationService.GetText("OK"));
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