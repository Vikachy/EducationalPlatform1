using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Maui;

namespace EducationalPlatform.Views
{
    public partial class CreatePracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _courseId;

        public CreatePracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseId = courseId;

            AnswerTypePicker.SelectedIndex = 0;
            AnswerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;

            // Инициализация видимости секций
            OnAnswerTypeChanged(null, EventArgs.Empty);
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var selectedType = AnswerTypePicker.SelectedItem as string;
            CodeSection.IsVisible = selectedType == "code";
            FileSettingsSection.IsVisible = selectedType == "file";
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название задания", "OK");
                    return;
                }

                var answerType = AnswerTypePicker.SelectedItem as string ?? "text";

                // Получаем настройки для файлов
                var maxFileSize = 10;
                var allowedTypes = new List<string>();

                if (answerType == "file")
                {
                    if (!int.TryParse(MaxFileSizeEntry.Text, out maxFileSize) || maxFileSize <= 0)
                    {
                        await DisplayAlert("Ошибка", "Введите корректный максимальный размер файла", "OK");
                        return;
                    }

                    if (ZipCheckBox.IsChecked) allowedTypes.Add(".zip");
                    if (ImageCheckBox.IsChecked)
                    {
                        allowedTypes.Add(".jpg");
                        allowedTypes.Add(".jpeg");
                        allowedTypes.Add(".png");
                        allowedTypes.Add(".gif");
                    }
                    if (PdfCheckBox.IsChecked) allowedTypes.Add(".pdf");

                    if (!allowedTypes.Any())
                    {
                        await DisplayAlert("Ошибка", "Выберите хотя бы один разрешенный тип файлов", "OK");
                        return;
                    }
                }

                // Получаем стартовый код (только для типа "code")
                var starterCode = answerType == "code" ? StarterCodeEditor.Text?.Trim() : null;

                var lessonId = await _dbService.AddPracticeWithAnswerTypeAsync(
                    _courseId,
                    TitleEntry.Text.Trim(),
                    DescriptionEditor.Text?.Trim() ?? "",
                    answerType,
                    starterCode,
                    ExpectedAnswerEditor.Text?.Trim(),
                    HintEditor.Text?.Trim(),
                    maxFileSize,
                    string.Join(";", allowedTypes)
                );

                if (lessonId.HasValue && lessonId.Value > 0)
                {
                    await DisplayAlert("Успех", "Практическое задание создано!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать задание", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка создания: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}