using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminDifficultiesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Entry? _searchEntry;
        private CollectionView? _difficultiesCollectionView;

        private ObservableCollection<CourseDifficulty> _allDifficulties = new();
        private ObservableCollection<CourseDifficulty> _filteredDifficulties = new();

        public ObservableCollection<CourseDifficulty> Difficulties
        {
            get => _filteredDifficulties;
            set
            {
                _filteredDifficulties = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminDifficultiesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminDifficultiesPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadDifficultiesAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _difficultiesCollectionView = this.FindByName<CollectionView>("DifficultiesCollectionView");

            if (_difficultiesCollectionView != null)
                _difficultiesCollectionView.ItemsSource = Difficulties;
        }

        private async Task LoadDifficultiesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Загружаем все уровни сложности
                var difficulties = await connection.QueryAsync<CourseDifficulty>(@"
            SELECT 
                DifficultyId,
                DifficultyName,
                Description,
                HasTheory,
                HasPractice,
                IsActive,
                CreatedDate,
                (SELECT COUNT(*) FROM Courses WHERE DifficultyId = Difficulties.DifficultyId) as CoursesCount
            FROM Difficulties
            ORDER BY DifficultyId
        ");

                var diffList = difficulties.ToList();
                Console.WriteLine($"📊 Загружено уровней сложности: {diffList.Count}");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allDifficulties.Clear();
                    foreach (var diff in diffList)
                    {
                        _allDifficulties.Add(diff);
                    }

                    ApplyFilter();

                    if (_difficultiesCollectionView != null)
                    {
                        _difficultiesCollectionView.ItemsSource = null;
                        _difficultiesCollectionView.ItemsSource = Difficulties;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки уровней сложности: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить уровни сложности: {ex.Message}", "OK");
                });
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allDifficulties.AsEnumerable();

            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(d =>
                    d.DifficultyName.ToLower().Contains(searchText) ||
                    (d.Description?.ToLower().Contains(searchText) == true));
            }

            Difficulties = new ObservableCollection<CourseDifficulty>(filtered);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnAddDifficultyClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdminDifficultyEditPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnEditDifficultyClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseDifficulty difficulty)
            {
                await Navigation.PushAsync(new AdminDifficultyEditPage(_currentUser, _dbService, _settingsService, difficulty));
            }
        }

        private async void OnDeleteDifficultyClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CourseDifficulty difficulty)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите удалить уровень '{difficulty.DifficultyName}'?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        // Проверяем, есть ли курсы с этим уровнем
                        var coursesCount = await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(*) FROM Courses WHERE DifficultyId = @DifficultyId",
                            new { DifficultyId = difficulty.DifficultyId });

                        if (coursesCount > 0)
                        {
                            // Мягкое удаление - деактивируем
                            await connection.ExecuteAsync(
                                "UPDATE Difficulties SET IsActive = 0 WHERE DifficultyId = @DifficultyId",
                                new { DifficultyId = difficulty.DifficultyId });
                            await DisplayAlert("Информация",
                                $"Уровень используется в {coursesCount} курсах. Он деактивирован.", "OK");
                        }
                        else
                        {
                            // Полное удаление
                            await connection.ExecuteAsync(
                                "DELETE FROM Difficulties WHERE DifficultyId = @DifficultyId",
                                new { DifficultyId = difficulty.DifficultyId });
                        }

                        await LoadDifficultiesAsync();
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