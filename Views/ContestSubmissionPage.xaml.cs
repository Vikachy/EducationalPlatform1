using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class ContestSubmissionPage : ContentPage
    {
        private readonly int _contestId;
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private string? _pickedZipPath;

        public ContestSubmissionPage(int contestId, User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _contestId = contestId;
            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;

            UpdateTexts();
        }

        private void UpdateTexts()
        {
            try
            {
                Title = _localizationService?.GetText("SubmitProject") ?? "Отправка проекта";

                var headerLabel = this.FindByName<Label>("HeaderLabel");
                if (headerLabel != null)
                    headerLabel.Text = _localizationService?.GetText("SubmitProject") ?? "Отправка проекта";

                var nameEntry = this.FindByName<Entry>("ProjectNameEntry");
                if (nameEntry != null)
                    nameEntry.Placeholder = _localizationService?.GetText("ProjectName") ?? "Название проекта";

                var descEditor = this.FindByName<Editor>("DescriptionEditor");
                if (descEditor != null)
                    descEditor.Placeholder = _localizationService?.GetText("ProjectDescription") ?? "Описание проекта";

                var pickButton = this.FindByName<Button>("PickZipButton");
                if (pickButton != null)
                    pickButton.Text = _localizationService?.GetText("SelectZipArchive") ?? "Выбрать ZIP архив";

                var fileLabel = this.FindByName<Label>("PickedFileLabel");
                if (fileLabel != null)
                    fileLabel.Text = _localizationService?.GetText("NoFileSelected") ?? "Файл не выбран";

                var submitButton = this.FindByName<Button>("SubmitButton");
                if (submitButton != null)
                    submitButton.Text = _localizationService?.GetText("SubmitProject") ?? "Отправить проект";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления текстов: {ex.Message}");
            }
        }

        private async void OnPickZipClicked(object sender, EventArgs e)
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = _localizationService?.GetText("SelectZipArchive") ?? "Выберите ZIP архив",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".zip" } },
                        { DevicePlatform.Android, new[] { "application/zip", "application/x-zip-compressed", "*/*" } },
                        { DevicePlatform.iOS, new[] { "com.pkware.zip-archive", "public.zip-archive" } },
                        { DevicePlatform.MacCatalyst, new[] { "com.pkware.zip-archive", "public.zip-archive" } },
                    })
                };

                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    _pickedZipPath = result.FullPath;

                    var fileLabel = this.FindByName<Label>("PickedFileLabel");
                    if (fileLabel != null)
                        fileLabel.Text = System.IO.Path.GetFileName(_pickedZipPath);

                    Console.WriteLine($"✅ Выбран файл: {_pickedZipPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка выбора файла: {ex.Message}");
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("FailedToSelectFile") ?? "Не удалось выбрать файл"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                var nameEntry = this.FindByName<Entry>("ProjectNameEntry");
                if (nameEntry == null || string.IsNullOrWhiteSpace(nameEntry.Text))
                {
                    await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                        _localizationService?.GetText("EnterProjectName") ?? "Введите название проекта",
                        _localizationService?.GetText("OK") ?? "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_pickedZipPath))
                {
                    await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                        _localizationService?.GetText("SelectZipFile") ?? "Выберите ZIP файл",
                        _localizationService?.GetText("OK") ?? "OK");
                    return;
                }

                // Показываем индикатор загрузки
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                {
                    loadingOverlay.IsVisible = true;
                    var loadingLabel = this.FindByName<Label>("LoadingLabel");
                    if (loadingLabel != null)
                        loadingLabel.Text = _localizationService?.GetText("Sending") ?? "Отправка...";
                }

                Console.WriteLine($"📤 Начинаем отправку проекта для конкурса ID: {_contestId}");

                // Создаем папку для файлов конкурсов
                string contestFilesDir = Path.Combine(FileSystem.AppDataDirectory, "ContestFiles");
                if (!Directory.Exists(contestFilesDir))
                {
                    Directory.CreateDirectory(contestFilesDir);
                    Console.WriteLine($"📁 Создана папка: {contestFilesDir}");
                }

                // Генерируем уникальное имя файла
                string fileExtension = Path.GetExtension(_pickedZipPath);
                string fileName = $"contest_{_contestId}_user_{_currentUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string destPath = Path.Combine(contestFilesDir, fileName);

                // Копируем файл
                File.Copy(_pickedZipPath, destPath, true);
                Console.WriteLine($"✅ Файл сохранен: {destPath} (размер: {new FileInfo(destPath).Length} байт)");

                // Получаем описание
                var descEditor = this.FindByName<Editor>("DescriptionEditor");
                string description = descEditor?.Text?.Trim();

                // Отправляем заявку
                bool success = await _dbService.CreateContestSubmissionAsync(
                    _contestId,
                    _currentUser.UserId,
                    nameEntry.Text.Trim(),
                    destPath,
                    description);

                if (success)
                {
                    Console.WriteLine($"✅ Заявка успешно отправлена");

                    await DisplayAlert(_localizationService?.GetText("Success") ?? "Успех",
                        _localizationService?.GetText("ProjectSubmitted") ?? "Проект отправлен!",
                        _localizationService?.GetText("OK") ?? "OK");

                    // Отправляем сообщение об успешной отправке
                    MessagingCenter.Send(this, "SubmissionCreated");

                    await Navigation.PopAsync();
                }
                else
                {
                    Console.WriteLine($"❌ Не удалось отправить заявку");

                    // Проверяем, может уже есть заявка
                    bool hasExisting = await CheckExistingSubmission();
                    if (hasExisting)
                    {
                        await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                            _localizationService?.GetText("SubmissionAlreadyExists") ?? "Вы уже отправляли заявку на этот конкурс",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                    else
                    {
                        await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                            _localizationService?.GetText("FailedToSubmitProject") ?? "Не удалось отправить проект",
                            _localizationService?.GetText("OK") ?? "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка отправки: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                await DisplayAlert(_localizationService?.GetText("Error") ?? "Ошибка",
                    $"{_localizationService?.GetText("SubmissionError") ?? "Ошибка отправки"}: {ex.Message}",
                    _localizationService?.GetText("OK") ?? "OK");
            }
            finally
            {
                var loadingOverlay = this.FindByName<Grid>("LoadingOverlay");
                if (loadingOverlay != null)
                    loadingOverlay.IsVisible = false;
            }
        }

        private async Task<bool> CheckExistingSubmission()
        {
            try
            {
                var submissions = await _dbService.GetUserContestSubmissionsAsync(_currentUser.UserId);
                return submissions.Any(s => s.ContestId == _contestId);
            }
            catch
            {
                return false;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}