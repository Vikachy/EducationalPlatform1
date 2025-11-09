using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ContestSubmissionPage : ContentPage
    {
        private readonly int _contestId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private string? _pickedZipPath;

        public ContestSubmissionPage(int contestId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _contestId = contestId;
            _currentUser = currentUser;
            _dbService = dbService;
        }

        private async void OnPickZipClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите ZIP архив",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[]{ ".zip" } },
                        { DevicePlatform.Android, new[]{ "application/zip" } },
                        { DevicePlatform.iOS, new[]{ "com.pkware.zip-archive" } },
                        { DevicePlatform.MacCatalyst, new[]{ "com.pkware.zip-archive" } },
                    })
                });

                if (result != null)
                {
                    _pickedZipPath = result.FullPath;
                    PickedFileLabel.Text = System.IO.Path.GetFileName(_pickedZipPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать файл: {ex.Message}", "OK");
            }
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectNameEntry.Text) || string.IsNullOrEmpty(_pickedZipPath))
            {
                await DisplayAlert("Ошибка", "Укажите название проекта и выберите ZIP файл", "OK");
                return;
            }

            try
            {
                // Загружаем файл в облако или сохраняем локально
                string fileUrl = _pickedZipPath!;
                
                // В будущем здесь можно добавить загрузку в облачное хранилище
                // var fileService = new FileService();
                // fileUrl = await fileService.UploadFileAsync(_pickedZipPath);
                
                bool ok = await _dbService.CreateContestSubmissionAsync(
                    _contestId,
                    _currentUser.UserId,
                    ProjectNameEntry.Text!.Trim(),
                    fileUrl,
                    DescriptionEditor.Text?.Trim());

                if (ok)
                {
                    await DisplayAlert("Успех", "Проект отправлен!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось отправить проект", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Сбой отправки: {ex.Message}", "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}