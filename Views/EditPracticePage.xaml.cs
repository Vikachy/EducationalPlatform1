using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class EditPracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _lessonId;
        private PracticeDto _practice;

        public EditPracticePage(User user, DatabaseService dbService, SettingsService settingsService, int lessonId)
        {
            InitializeComponent();
            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _lessonId = lessonId;

            AnswerTypePicker.SelectedIndexChanged += OnAnswerTypeChanged;
            LoadPracticeData();
        }

        private async void LoadPracticeData()
        {
            try
            {
                // Получаем данные практического задания с названием урока
                _practice = await _dbService.GetPracticeExerciseWithLessonDataAsync(_lessonId);

                if (_practice != null)
                {
                    // Загружаем данные урока
                    TitleEntry.Text = _practice.Title;
                    DescriptionEditor.Text = _practice.Description;

                    // Устанавливаем тип ответа
                    if (!string.IsNullOrEmpty(_practice.AnswerType))
                    {
                        var index = AnswerTypePicker.Items.IndexOf(_practice.AnswerType);
                        if (index >= 0)
                            AnswerTypePicker.SelectedIndex = index;
                    }

                    StarterCodeEditor.Text = _practice.StarterCode;
                    ExpectedAnswerEditor.Text = _practice.ExpectedOutput;
                    HintEditor.Text = _practice.Hint;
                }
                else
                {
                    await DisplayAlert("Информация", "Практическое задание не найдено", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные практики: {ex.Message}", "OK");
            }
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var selectedType = AnswerTypePicker.SelectedItem as string;
            CodeSection.IsVisible = selectedType == "code";
            FileSettingsSection.IsVisible = selectedType == "file";
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название задания", "OK");
                    return;
                }

                if (_practice == null)
                {
                    await DisplayAlert("Ошибка", "Данные практики не загружены", "OK");
                    return;
                }

                // Обновляем данные
                _practice.Title = TitleEntry.Text;
                _practice.Description = DescriptionEditor.Text;
                _practice.AnswerType = AnswerTypePicker.SelectedItem?.ToString() ?? "text";
                _practice.StarterCode = StarterCodeEditor.Text;
                _practice.ExpectedOutput = ExpectedAnswerEditor.Text;
                _practice.Hint = HintEditor.Text;

                // Сохраняем в базу
                var success = await _dbService.UpdatePracticeExerciseAsync(_practice);

                if (success)
                {
                    await DisplayAlert("Успех", "Практическое задание успешно обновлено", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось сохранить изменения", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка сохранения: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}