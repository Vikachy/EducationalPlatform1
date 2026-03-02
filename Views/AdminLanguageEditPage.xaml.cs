using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class AdminLanguageEditPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly ProgrammingLanguage? _editingLanguage;

        private Entry? _languageNameEntry;
        private Entry? _iconEntry;
        private Label? _iconPreviewLabel;
        private Switch? _isActiveSwitch;
        private Label? _titleLabel;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminLanguageEditPage(User user, DatabaseService dbService, SettingsService settingsService, ProgrammingLanguage? language = null)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminLanguageEditPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _editingLanguage = language;

            InitializeControls();

            if (language != null)
            {
                if (_titleLabel != null) _titleLabel.Text = "Редактирование языка";
                LoadLanguageData();
            }
            else
            {
                if (_titleLabel != null) _titleLabel.Text = "Создание языка";
            }
        }

        private void InitializeControls()
        {
            _titleLabel = this.FindByName<Label>("TitleLabel");
            _languageNameEntry = this.FindByName<Entry>("LanguageNameEntry");
            _iconEntry = this.FindByName<Entry>("IconEntry");
            _iconPreviewLabel = this.FindByName<Label>("IconPreviewLabel");
            _isActiveSwitch = this.FindByName<Switch>("IsActiveSwitch");

            if (_iconEntry != null)
                _iconEntry.TextChanged += OnIconTextChanged;
        }

        private void LoadLanguageData()
        {
            if (_editingLanguage == null) return;

            if (_languageNameEntry != null)
                _languageNameEntry.Text = _editingLanguage.LanguageName;

            if (_iconEntry != null)
                _iconEntry.Text = _editingLanguage.Icon ?? "🔷";

            if (_isActiveSwitch != null)
                _isActiveSwitch.IsToggled = _editingLanguage.IsActive;
        }

        private void OnIconTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_iconPreviewLabel != null && !string.IsNullOrEmpty(e.NewTextValue))
            {
                _iconPreviewLabel.Text = e.NewTextValue;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_languageNameEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название языка", "OK");
                    return;
                }

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                if (_editingLanguage == null)
                {
                    // Создание нового языка
                    await connection.ExecuteAsync(@"
                        INSERT INTO ProgrammingLanguages (LanguageName, Icon, IsActive, CreatedDate)
                        VALUES (@Name, @Icon, @IsActive, GETDATE())
                    ", new
                    {
                        Name = _languageNameEntry.Text.Trim(),
                        Icon = _iconEntry?.Text?.Trim() ?? "💻",
                        IsActive = _isActiveSwitch?.IsToggled ?? true
                    });

                    await DisplayAlert("Успех", "Язык успешно создан", "OK");
                }
                else
                {
                    // Обновление существующего языка
                    await connection.ExecuteAsync(@"
                        UPDATE ProgrammingLanguages 
                        SET LanguageName = @Name,
                            Icon = @Icon,
                            IsActive = @IsActive
                        WHERE LanguageId = @LanguageId
                    ", new
                    {
                        LanguageId = _editingLanguage.LanguageId,
                        Name = _languageNameEntry.Text.Trim(),
                        Icon = _iconEntry?.Text?.Trim() ?? _editingLanguage.Icon,
                        IsActive = _isActiveSwitch?.IsToggled ?? _editingLanguage.IsActive
                    });

                    await DisplayAlert("Успех", "Язык успешно обновлен", "OK");
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