using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class AdminDifficultyEditPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly CourseDifficulty? _editingDifficulty;

        private Entry? _difficultyNameEntry;
        private Editor? _descriptionEditor;
        private Switch? _hasTheorySwitch;
        private Switch? _hasPracticeSwitch;
        private Switch? _isActiveSwitch;
        private Label? _titleLabel;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminDifficultyEditPage(User user, DatabaseService dbService, SettingsService settingsService, CourseDifficulty? difficulty = null)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminDifficultyEditPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _editingDifficulty = difficulty;

            InitializeControls();

            if (difficulty != null)
            {
                if (_titleLabel != null) _titleLabel.Text = "Редактирование уровня";
                LoadDifficultyData();
            }
            else
            {
                if (_titleLabel != null) _titleLabel.Text = "Создание уровня";
            }
        }

        private void InitializeControls()
        {
            _titleLabel = this.FindByName<Label>("TitleLabel");
            _difficultyNameEntry = this.FindByName<Entry>("DifficultyNameEntry");
            _descriptionEditor = this.FindByName<Editor>("DescriptionEditor");
            _hasTheorySwitch = this.FindByName<Switch>("HasTheorySwitch");
            _hasPracticeSwitch = this.FindByName<Switch>("HasPracticeSwitch");
            _isActiveSwitch = this.FindByName<Switch>("IsActiveSwitch");
        }

        private void LoadDifficultyData()
        {
            if (_editingDifficulty == null) return;

            if (_difficultyNameEntry != null)
                _difficultyNameEntry.Text = _editingDifficulty.DifficultyName;

            if (_descriptionEditor != null)
                _descriptionEditor.Text = _editingDifficulty.Description ?? "";

            if (_hasTheorySwitch != null)
                _hasTheorySwitch.IsToggled = _editingDifficulty.HasTheory;

            if (_hasPracticeSwitch != null)
                _hasPracticeSwitch.IsToggled = _editingDifficulty.HasPractice;

            if (_isActiveSwitch != null)
                _isActiveSwitch.IsToggled = _editingDifficulty.IsActive;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_difficultyNameEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название уровня", "OK");
                    return;
                }

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                if (_editingDifficulty == null)
                {
                    // Создание нового уровня
                    await connection.ExecuteAsync(@"
                        INSERT INTO Difficulties (DifficultyName, Description, HasTheory, HasPractice, IsActive, CreatedDate)
                        VALUES (@Name, @Description, @HasTheory, @HasPractice, @IsActive, GETDATE())
                    ", new
                    {
                        Name = _difficultyNameEntry.Text.Trim(),
                        Description = _descriptionEditor?.Text?.Trim(),
                        HasTheory = _hasTheorySwitch?.IsToggled ?? true,
                        HasPractice = _hasPracticeSwitch?.IsToggled ?? true,
                        IsActive = _isActiveSwitch?.IsToggled ?? true
                    });

                    await DisplayAlert("Успех", "Уровень сложности успешно создан", "OK");
                }
                else
                {
                    // Обновление существующего уровня
                    await connection.ExecuteAsync(@"
                        UPDATE Difficulties 
                        SET DifficultyName = @Name,
                            Description = @Description,
                            HasTheory = @HasTheory,
                            HasPractice = @HasPractice,
                            IsActive = @IsActive
                        WHERE DifficultyId = @DifficultyId
                    ", new
                    {
                        DifficultyId = _editingDifficulty.DifficultyId,
                        Name = _difficultyNameEntry.Text.Trim(),
                        Description = _descriptionEditor?.Text?.Trim(),
                        HasTheory = _hasTheorySwitch?.IsToggled ?? _editingDifficulty.HasTheory,
                        HasPractice = _hasPracticeSwitch?.IsToggled ?? _editingDifficulty.HasPractice,
                        IsActive = _isActiveSwitch?.IsToggled ?? _editingDifficulty.IsActive
                    });

                    await DisplayAlert("Успех", "Уровень сложности успешно обновлен", "OK");
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