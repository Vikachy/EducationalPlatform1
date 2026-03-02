using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class CreatePracticePage : ContentPage
    {
        private readonly User _user;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;
        private readonly int _courseId;

        public ObservableCollection<EnhancedFileAttachment> Attachments { get; set; } = new();

        public CreatePracticePage(User user, DatabaseService dbService, SettingsService settingsService, int courseId)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }

            _user = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _fileService = ServiceHelper.GetService<FileService>();
            _courseId = courseId;

            // Настройка BindingContext и коллекций
            BindingContext = this;

            var attachmentsCollection = this.FindByName<CollectionView>("AttachmentsCollection");
            if (attachmentsCollection != null)
                attachmentsCollection.ItemsSource = Attachments;

            // Устанавливаем начальные значения
            var typePicker = this.FindByName<Picker>("AnswerTypePicker");
            if (typePicker != null)
            {
                typePicker.SelectedIndex = 0;
                typePicker.SelectedIndexChanged += OnAnswerTypeChanged;
            }

            // Устанавливаем начальные значения для чекбоксов
            var zipBox = this.FindByName<CheckBox>("ZipCheckBox");
            if (zipBox != null) zipBox.IsChecked = true;

            var imgBox = this.FindByName<CheckBox>("ImageCheckBox");
            if (imgBox != null) imgBox.IsChecked = true;

            var pdfBox = this.FindByName<CheckBox>("PdfCheckBox");
            if (pdfBox != null) pdfBox.IsChecked = true;

            OnAnswerTypeChanged(null, null);
        }

        private void OnAnswerTypeChanged(object sender, EventArgs e)
        {
            var typePicker = this.FindByName<Picker>("AnswerTypePicker");
            var selectedType = typePicker?.SelectedItem as string;

            var codeSection = this.FindByName<StackLayout>("CodeSection");
            if (codeSection != null)
                codeSection.IsVisible = selectedType == "code";

            var fileSection = this.FindByName<StackLayout>("FileSettingsSection");
            if (fileSection != null)
                fileSection.IsVisible = selectedType == "file";
        }

        private async void OnSelectFilesClicked(object sender, EventArgs e)
        {
            try
            {
                var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Выберите файлы",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".txt", ".zip" } },
                        { DevicePlatform.Android, new[] { "*/*" } },
                        { DevicePlatform.iOS, new[] { "public.data" } }
                    })
                });

                if (results != null && results.Any())
                {
                    foreach (var file in results)
                    {
                        using var stream = await file.OpenReadAsync();
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);

                        Attachments.Add(new EnhancedFileAttachment
                        {
                            FileName = file.FileName,
                            FileSize = ms.Length,
                            FileBytes = ms.ToArray(),
                            FileIcon = GetFileIcon(Path.GetExtension(file.FileName))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void OnRemoveAttachmentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is EnhancedFileAttachment attachment)
            {
                Attachments.Remove(attachment);
            }
        }

        private async void OnClearAllFilesClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Подтверждение", "Удалить все файлы?", "Да", "Нет");
            if (confirm)
                Attachments.Clear();
        }

        private string GetFileIcon(string extension)
        {
            return extension?.ToLower() switch
            {
                ".pdf" => "📄",
                ".doc" or ".docx" => "📝",
                ".ppt" or ".pptx" => "📽️",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
                ".zip" or ".rar" => "🗜️",
                ".txt" => "📃",
                _ => "📎"
            };
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            try
            {
                var titleEntry = this.FindByName<Entry>("TitleEntry");
                if (titleEntry == null || string.IsNullOrWhiteSpace(titleEntry.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название", "OK");
                    return;
                }

                var typePicker = this.FindByName<Picker>("AnswerTypePicker");
                var answerType = typePicker?.SelectedItem as string ?? "text";

                var descEditor = this.FindByName<Editor>("DescriptionEditor");
                var starterEditor = this.FindByName<Editor>("StarterCodeEditor");
                var expectedEditor = this.FindByName<Editor>("ExpectedAnswerEditor");
                var hintEditor = this.FindByName<Editor>("HintEditor");
                var maxSizeEntry = this.FindByName<Entry>("MaxFileSizeEntry");

                int maxFileSize = 10;
                var allowedTypes = new List<string>();

                if (answerType == "file")
                {
                    if (maxSizeEntry != null)
                        int.TryParse(maxSizeEntry.Text, out maxFileSize);

                    var zipBox = this.FindByName<CheckBox>("ZipCheckBox");
                    var imgBox = this.FindByName<CheckBox>("ImageCheckBox");
                    var pdfBox = this.FindByName<CheckBox>("PdfCheckBox");
                    var docBox = this.FindByName<CheckBox>("DocCheckBox");
                    var txtBox = this.FindByName<CheckBox>("TxtCheckBox");
                    var pptBox = this.FindByName<CheckBox>("PowerPointCheckBox");

                    if (zipBox?.IsChecked == true) allowedTypes.Add(".zip");
                    if (imgBox?.IsChecked == true)
                    {
                        allowedTypes.Add(".jpg"); allowedTypes.Add(".jpeg");
                        allowedTypes.Add(".png"); allowedTypes.Add(".gif");
                    }
                    if (pdfBox?.IsChecked == true) allowedTypes.Add(".pdf");
                    if (docBox?.IsChecked == true) { allowedTypes.Add(".doc"); allowedTypes.Add(".docx"); }
                    if (txtBox?.IsChecked == true) allowedTypes.Add(".txt");
                    if (pptBox?.IsChecked == true) { allowedTypes.Add(".ppt"); allowedTypes.Add(".pptx"); }

                    if (!allowedTypes.Any())
                    {
                        await DisplayAlert("Ошибка", "Выберите тип файла", "OK");
                        return;
                    }
                }

                var lessonId = await _dbService.AddPracticeWithAnswerTypeAsync(
                    courseId: _courseId,
                    title: titleEntry.Text.Trim(),
                    description: descEditor?.Text?.Trim() ?? "",
                    answerType: answerType,
                    starterCode: answerType == "code" ? starterEditor?.Text?.Trim() : null,
                    expectedAnswer: expectedEditor?.Text?.Trim(),
                    hint: hintEditor?.Text?.Trim(),
                    maxFileSize: maxFileSize,
                    allowedFileTypes: string.Join(";", allowedTypes)
                );

                if (lessonId.HasValue)
                {
                    if (Attachments.Any())
                    {
                        foreach (var att in Attachments.Where(a => a.FileBytes != null))
                        {
                            await _dbService.AddLessonAttachmentAsync(
                                lessonId.Value,
                                att.FileName,
                                Path.GetExtension(att.FileName),
                                FormatFileSize(att.FileSize),
                                att.FileBytes);
                        }
                    }
                    await DisplayAlert("Успех", "Практика создана!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} Б";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.0} КБ";
            return $"{bytes / (1024.0 * 1024.0):0.0} МБ";
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}