using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class CreateCoursePage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        private Entry? _courseNameEntry;
        private Editor? _descriptionEditor;
        private Picker? _languagePicker;
        private Picker? _difficultyPicker;
        private Entry? _priceEntry;
        private Entry? _hoursEntry;
        private Entry? _tagsEntry;
        private Switch? _isGroupCourseSwitch;
        private Switch? _publishSwitch;

        private ObservableCollection<ProgrammingLanguage> _languages = new();
        private ObservableCollection<CourseDifficulty> _difficulties = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CreateCoursePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId = 0)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации CreateCoursePage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            InitializeControls();
            LoadData();

            if (courseId > 0)
            {
                LoadCourseData();
            }
        }

        private void InitializeControls()
        {
            _courseNameEntry = this.FindByName<Entry>("CourseNameEntry");
            _descriptionEditor = this.FindByName<Editor>("DescriptionEditor");
            _languagePicker = this.FindByName<Picker>("LanguagePicker");
            _difficultyPicker = this.FindByName<Picker>("DifficultyPicker");
            _priceEntry = this.FindByName<Entry>("PriceEntry");
            _hoursEntry = this.FindByName<Entry>("HoursEntry");
            _tagsEntry = this.FindByName<Entry>("TagsEntry");
            _isGroupCourseSwitch = this.FindByName<Switch>("IsGroupCourseSwitch");
            _publishSwitch = this.FindByName<Switch>("PublishSwitch");
        }

        private async void LoadData()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Загружаем языки
                var languages = await connection.QueryAsync<ProgrammingLanguage>(@"
                    SELECT LanguageId, LanguageName, Icon 
                    FROM ProgrammingLanguages 
                    WHERE IsActive = 1 
                    ORDER BY LanguageName
                ");

                // Загружаем уровни сложности
                var difficulties = await connection.QueryAsync<CourseDifficulty>(@"
                    SELECT DifficultyId, DifficultyName, Description 
                    FROM Difficulties 
                    WHERE IsActive = 1 
                    ORDER BY DifficultyId
                ");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _languages.Clear();
                    foreach (var lang in languages)
                    {
                        _languages.Add(lang);
                    }

                    _difficulties.Clear();
                    foreach (var diff in difficulties)
                    {
                        _difficulties.Add(diff);
                    }

                    if (_languagePicker != null)
                    {
                        _languagePicker.ItemsSource = _languages;
                        _languagePicker.ItemDisplayBinding = new Binding("LanguageName");
                        if (_languages.Any())
                            _languagePicker.SelectedIndex = 0;
                    }

                    if (_difficultyPicker != null)
                    {
                        _difficultyPicker.ItemsSource = _difficulties;
                        _difficultyPicker.ItemDisplayBinding = new Binding("DifficultyName");
                        if (_difficulties.Any())
                            _difficultyPicker.SelectedIndex = 0;
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }

        private async void LoadCourseData()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var course = await connection.QueryFirstOrDefaultAsync<Course>(@"
            SELECT * FROM Courses WHERE CourseId = @CourseId
        ", new { CourseId = _courseId });

                if (course != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (_courseNameEntry != null)
                            _courseNameEntry.Text = course.CourseName;

                        if (_descriptionEditor != null)
                            _descriptionEditor.Text = course.Description;

                        if (_priceEntry != null)
                            _priceEntry.Text = course.Price.ToString();

                        // ИСПРАВЛЕНО: EstimatedHours - int, не nullable
                        if (_hoursEntry != null)
                        {
                            if (course.EstimatedHours > 0)
                                _hoursEntry.Text = course.EstimatedHours.ToString();
                            else
                                _hoursEntry.Text = "40"; // значение по умолчанию
                        }

                        if (_tagsEntry != null)
                            _tagsEntry.Text = course.Tags;

                        if (_isGroupCourseSwitch != null)
                            _isGroupCourseSwitch.IsToggled = course.IsGroupCourse;

                        if (_publishSwitch != null)
                            _publishSwitch.IsToggled = course.IsPublished;

                        // Выбираем язык
                        if (_languagePicker != null && course.LanguageId > 0)
                        {
                            var language = _languages.FirstOrDefault(l => l.LanguageId == course.LanguageId);
                            if (language != null)
                            {
                                var langIndex = _languages.IndexOf(language);
                                if (langIndex >= 0)
                                    _languagePicker.SelectedIndex = langIndex;
                            }
                        }

                        // Выбираем уровень
                        if (_difficultyPicker != null && course.DifficultyId > 0)
                        {
                            var difficulty = _difficulties.FirstOrDefault(d => d.DifficultyId == course.DifficultyId);
                            if (difficulty != null)
                            {
                                var diffIndex = _difficulties.IndexOf(difficulty);
                                if (diffIndex >= 0)
                                    _difficultyPicker.SelectedIndex = diffIndex;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // Можно добавить логику при смене языка
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(_courseNameEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название курса", "OK");
                    return;
                }

                if (_languagePicker?.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите язык программирования", "OK");
                    return;
                }

                if (_difficultyPicker?.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите уровень сложности", "OK");
                    return;
                }

                var language = (ProgrammingLanguage)_languagePicker.SelectedItem;
                var difficulty = (CourseDifficulty)_difficultyPicker.SelectedItem;

                decimal price = 0;
                if (!decimal.TryParse(_priceEntry?.Text, out price))
                    price = 0;

                int hours = 40;
                if (!int.TryParse(_hoursEntry?.Text, out hours))
                    hours = 40;

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                if (_courseId > 0)
                {
                    // Обновление существующего курса
                    await connection.ExecuteAsync(@"
                        UPDATE Courses 
                        SET CourseName = @Name,
                            Description = @Description,
                            LanguageId = @LanguageId,
                            DifficultyId = @DifficultyId,
                            Price = @Price,
                            EstimatedHours = @Hours,
                            Tags = @Tags,
                            IsGroupCourse = @IsGroupCourse,
                            IsPublished = @IsPublished,
                            UpdatedDate = GETDATE()
                        WHERE CourseId = @CourseId
                    ", new
                    {
                        CourseId = _courseId,
                        Name = _courseNameEntry.Text.Trim(),
                        Description = _descriptionEditor?.Text?.Trim(),
                        LanguageId = language.LanguageId,
                        DifficultyId = difficulty.DifficultyId,
                        Price = price,
                        Hours = hours,
                        Tags = _tagsEntry?.Text?.Trim(),
                        IsGroupCourse = _isGroupCourseSwitch?.IsToggled ?? false,
                        IsPublished = _publishSwitch?.IsToggled ?? false
                    });

                    await DisplayAlert("Успех", "Курс успешно обновлен", "OK");
                }
                else
                {
                    // Создание нового курса
                    await connection.ExecuteAsync(@"
                        INSERT INTO Courses (
                            CourseName, Description, LanguageId, DifficultyId, 
                            Price, EstimatedHours, Tags, IsGroupCourse, IsPublished,
                            CreatedByUserId, CreatedDate
                        ) VALUES (
                            @Name, @Description, @LanguageId, @DifficultyId,
                            @Price, @Hours, @Tags, @IsGroupCourse, @IsPublished,
                            @UserId, GETDATE()
                        )
                    ", new
                    {
                        Name = _courseNameEntry.Text.Trim(),
                        Description = _descriptionEditor?.Text?.Trim(),
                        LanguageId = language.LanguageId,
                        DifficultyId = difficulty.DifficultyId,
                        Price = price,
                        Hours = hours,
                        Tags = _tagsEntry?.Text?.Trim(),
                        IsGroupCourse = _isGroupCourseSwitch?.IsToggled ?? false,
                        IsPublished = _publishSwitch?.IsToggled ?? false,
                        UserId = _currentUser.UserId
                    });

                    await DisplayAlert("Успех", "Курс успешно создан", "OK");
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}