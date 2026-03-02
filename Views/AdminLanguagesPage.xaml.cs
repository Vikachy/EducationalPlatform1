using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminLanguagesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Entry? _searchEntry;
        private CollectionView? _languagesCollectionView;

        // ДОБАВЛЯЕМ НЕДОСТАЮЩИЕ ПОЛЯ
        private Label? _totalLanguagesLabel;
        private Label? _activeLanguagesLabel;
        private Label? _usedLanguagesLabel;

        private ObservableCollection<ProgrammingLanguage> _allLanguages = new();
        private ObservableCollection<ProgrammingLanguage> _filteredLanguages = new();

        public ObservableCollection<ProgrammingLanguage> Languages
        {
            get => _filteredLanguages;
            set
            {
                _filteredLanguages = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminLanguagesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminLanguagesPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadLanguagesAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _languagesCollectionView = this.FindByName<CollectionView>("LanguagesCollectionView");

            // ИНИЦИАЛИЗИРУЕМ НЕДОСТАЮЩИЕ ПОЛЯ
            _totalLanguagesLabel = this.FindByName<Label>("TotalLanguagesLabel");
            _activeLanguagesLabel = this.FindByName<Label>("ActiveLanguagesLabel");
            _usedLanguagesLabel = this.FindByName<Label>("UsedLanguagesLabel");

            if (_languagesCollectionView != null)
                _languagesCollectionView.ItemsSource = Languages;
        }

        private async Task LoadLanguagesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var languages = await connection.QueryAsync<ProgrammingLanguage>(@"
                    SELECT 
                        l.LanguageId,
                        l.LanguageName,
                        l.Icon,
                        ISNULL(l.IsActive, 1) as IsActive,
                        ISNULL(l.CreatedDate, GETDATE()) as CreatedDate,
                        COUNT(c.CourseId) as CoursesCount
                    FROM ProgrammingLanguages l
                    LEFT JOIN Courses c ON l.LanguageId = c.LanguageId
                    GROUP BY l.LanguageId, l.LanguageName, l.Icon, l.IsActive, l.CreatedDate
                    ORDER BY l.LanguageName
                ");

                var langList = languages.ToList();

                // Подсчет статистики
                int totalLanguages = langList.Count;
                int activeLanguages = langList.Count(l => l.IsActive);
                int usedLanguages = langList.Count(l => l.CoursesCount > 0);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // ОБНОВЛЯЕМ ЗНАЧЕНИЯ
                    if (_totalLanguagesLabel != null)
                        _totalLanguagesLabel.Text = totalLanguages.ToString();

                    if (_activeLanguagesLabel != null)
                        _activeLanguagesLabel.Text = activeLanguages.ToString();

                    if (_usedLanguagesLabel != null)
                        _usedLanguagesLabel.Text = usedLanguages.ToString();

                    _allLanguages.Clear();
                    foreach (var lang in langList)
                    {
                        _allLanguages.Add(lang);
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки языков: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить языки: {ex.Message}", "OK");
                });
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allLanguages.AsEnumerable();

            // Поиск по названию
            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(l =>
                    l.LanguageName.ToLower().Contains(searchText));
            }

            Languages = new ObservableCollection<ProgrammingLanguage>(filtered);

            if (_languagesCollectionView != null)
            {
                _languagesCollectionView.ItemsSource = null;
                _languagesCollectionView.ItemsSource = Languages;
            }

            Console.WriteLine($"📊 Отфильтровано языков: {Languages.Count}");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnAddLanguageClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminLanguageEditPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnEditLanguageClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ProgrammingLanguage language)
            {
                await Navigation.PushAsync(new AdminLanguageEditPage(_currentUser, _dbService, _settingsService, language));
            }
        }

        private async void OnDeleteLanguageClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ProgrammingLanguage language)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите удалить язык '{language.LanguageName}'?\n" +
                    $"Этот язык используется в {language.CoursesCount} курсах.",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        if (language.CoursesCount > 0)
                        {
                            // Мягкое удаление - просто деактивируем
                            await connection.ExecuteAsync(
                                "UPDATE ProgrammingLanguages SET IsActive = 0 WHERE LanguageId = @LanguageId",
                                new { LanguageId = language.LanguageId });

                            await LoadLanguagesAsync();
                            await DisplayAlert("Успех", "Язык деактивирован", "OK");
                        }
                        else
                        {
                            // Полное удаление
                            await connection.ExecuteAsync(
                                "DELETE FROM ProgrammingLanguages WHERE LanguageId = @LanguageId",
                                new { LanguageId = language.LanguageId });

                            await LoadLanguagesAsync();
                            await DisplayAlert("Успех", "Язык удален", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}