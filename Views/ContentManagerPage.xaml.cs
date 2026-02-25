using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class ContentManagerPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly LocalizationService _localizationService;
        private readonly IEmailService _emailService;
        private List<News> _allNews = new();
        private List<User> _allUsers = new();
        private List<StudyGroup> _allGroups = new();
        private List<SelectableGroup> _selectableGroups = new();
        private string _selectedImagePath;
        private string _currentRecipientType = "all";

        // Расширенные категории
        private readonly List<CategoryItem> _categories = new()
        {
            new CategoryItem { DisplayName = "📰 Общие новости", Key = "general" },
            new CategoryItem { DisplayName = "📚 Новости курсов", Key = "courses" },
            new CategoryItem { DisplayName = "🏆 Новости конкурсов", Key = "contests" },
            new CategoryItem { DisplayName = "⚙️ Технические обновления", Key = "system" },
            new CategoryItem { DisplayName = "🎓 Студенческая жизнь", Key = "student_life" },
            new CategoryItem { DisplayName = "💼 Вакансии и стажировки", Key = "jobs" },
            new CategoryItem { DisplayName = "🌐 Мероприятия и вебинары", Key = "events" },
            new CategoryItem { DisplayName = "🎁 Акции и бонусы", Key = "promotions" },
            new CategoryItem { DisplayName = "📅 Важные даты", Key = "important_dates" },
            new CategoryItem { DisplayName = "❓ Часто задаваемые вопросы", Key = "faq" }
        };

        public class CategoryItem
        {
            public string DisplayName { get; set; }
            public string Key { get; set; }
        }

        public ObservableCollection<News> RecentNews { get; set; } = new();
        public ObservableCollection<SelectableGroup> Groups { get; set; } = new();

        public class SelectableGroup : StudyGroup
        {
            public bool IsSelected { get; set; }
        }

        public ContentManagerPage(User currentUser, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации ContentManagerPage: {ex.Message}");
            }

            _currentUser = currentUser;
            _dbService = dbService;
            _settingsService = settingsService;
            _localizationService = App.AppLocalization;
            _emailService = new EmailService();

            BindingContext = this;

            // Загружаем данные
            Task.Run(async () => await LoadDataAsync());

            // Инициализируем категории
            InitializeCategories();
        }

        private void InitializeCategories()
        {
            CategoryPicker.ItemsSource = _categories.Select(c => c.DisplayName).ToList();
            CategoryPicker.SelectedIndex = 0; // По умолчанию "Общие новости"
        }

        private string GetCategoryKey(string displayName)
        {
            var category = _categories.FirstOrDefault(c => c.DisplayName == displayName);
            return category?.Key ?? "general";
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = true;
                });

                // Загружаем новости
                _allNews = await _dbService.GetAllNewsAsync(_currentUser.LanguagePref ?? "ru", _currentUser.InterfaceStyle == "teen");

                // Загружаем всех пользователей
                _allUsers = await _dbService.GetAllUsersAsync();

                // Загружаем группы
                _allGroups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                _selectableGroups = _allGroups.Select(g => new SelectableGroup
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    CourseName = g.CourseName,
                    StudentCount = g.StudentCount,
                    IsSelected = false
                }).ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Обновляем статистику новостей
                    NewsCountLabel.Text = _allNews.Count.ToString();
                    ActiveNewsLabel.Text = _allNews.Count(n => n.IsActive).ToString();

                    // Показываем последние 5 новостей
                    RecentNews.Clear();
                    foreach (var news in _allNews.OrderByDescending(n => n.PublishedDate).Take(5))
                    {
                        RecentNews.Add(news);
                    }
                    RecentNewsCollection.ItemsSource = RecentNews;

                    // Загружаем группы в CollectionView
                    Groups.Clear();
                    foreach (var group in _selectableGroups)
                    {
                        Groups.Add(group);
                    }
                    GroupsCollection.ItemsSource = Groups;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                        LoadingOverlay.IsVisible = false;
                });
            }
        }

        // Выбор изображения с ПК
        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите изображение для новости",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" } },
                        { DevicePlatform.Android, new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" } },
                        { DevicePlatform.iOS, new[] { "public.image" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.image" } },
                    })
                });

                if (result != null)
                {
                    _selectedImagePath = result.FullPath;
                    ImagePathEntry.Text = result.FileName;

                    // Показываем превью
                    ImagePreview.Source = ImageSource.FromFile(_selectedImagePath);
                    ImagePreviewBorder.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
            }
        }

        private void OnRemoveImageClicked(object sender, EventArgs e)
        {
            _selectedImagePath = null;
            ImagePathEntry.Text = "";
            ImagePreviewBorder.IsVisible = false;
            ImagePreview.Source = null;
        }

        // Создание новости
        private async void OnCreateNewsClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewsTitleEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите заголовок новости", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewsContentEditor?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текст новости", "OK");
                    return;
                }

                if (CategoryPicker.SelectedIndex == -1)
                {
                    await DisplayAlert("Ошибка", "Выберите категорию новости", "OK");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingLabel.Text = "Создание новости...";
                        LoadingOverlay.IsVisible = true;
                    }
                });

                // Копируем изображение в папку приложения
                string imageUrl = null;
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    string fileName = $"news_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(_selectedImagePath)}";
                    string destPath = Path.Combine(FileSystem.AppDataDirectory, "NewsImages", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.Copy(_selectedImagePath, destPath, true);
                    imageUrl = destPath;
                }

                var selectedCategory = CategoryPicker.SelectedItem?.ToString() ?? "";

                var news = new News
                {
                    Title = NewsTitleEntry.Text.Trim(),
                    Content = NewsContentEditor.Text.Trim(),
                    Summary = string.IsNullOrWhiteSpace(NewsSummaryEditor?.Text) ? null : NewsSummaryEditor.Text.Trim(),
                    Category = GetCategoryKey(selectedCategory),
                    LanguageCode = _currentUser.LanguagePref ?? "ru",
                    ForTeens = false,
                    ImageUrl = imageUrl,
                    IsActive = true
                };

                Console.WriteLine($"📝 Создание новости:");
                Console.WriteLine($"   Title: {news.Title}");
                Console.WriteLine($"   Category: {news.Category}");
                Console.WriteLine($"   Summary: {news.Summary}");
                Console.WriteLine($"   Content length: {news.Content?.Length}");

                bool success = await _dbService.CreateNewsAsync(news, _currentUser.UserId);

                if (success)
                {
                    await DisplayAlert("Успех", "Новость успешно создана", "OK");

                    // Очищаем форму
                    NewsTitleEntry.Text = "";
                    NewsSummaryEditor.Text = "";
                    NewsContentEditor.Text = "";
                    CategoryPicker.SelectedIndex = 0;
                    OnRemoveImageClicked(null, null);

                    // Перезагружаем данные
                    await LoadDataAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать новость. Проверьте подключение к базе данных.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания новости: {ex.Message}");
                await DisplayAlert("Ошибка", $"Не удалось создать новость: {ex.Message}", "OK");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingLabel.Text = "Загрузка...";
                        LoadingOverlay.IsVisible = false;
                    }
                });
            }
        }

        // Выбор получателей рассылки
        private void OnSelectAllUsers(object sender, EventArgs e)
        {
            _currentRecipientType = "all";
            UpdateRecipientButtons(AllUsersButton);
            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = false;
        }

        private void OnSelectStudents(object sender, EventArgs e)
        {
            _currentRecipientType = "students";
            UpdateRecipientButtons(StudentsButton);
            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = false;
        }

        private void OnSelectTeachers(object sender, EventArgs e)
        {
            _currentRecipientType = "teachers";
            UpdateRecipientButtons(TeachersButton);
            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = false;
        }

        private void OnSelectContentManagers(object sender, EventArgs e)
        {
            _currentRecipientType = "content_managers";
            UpdateRecipientButtons(ContentManagersButton);
            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = false;
        }

        private void OnSelectGroups(object sender, EventArgs e)
        {
            _currentRecipientType = "groups";
            UpdateRecipientButtons(GroupsButton);
            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = true;
        }

        private void UpdateRecipientButtons(Button selectedButton)
        {
            // Сбрасываем все кнопки
            ResetButtonStyle(AllUsersButton);
            ResetButtonStyle(StudentsButton);
            ResetButtonStyle(TeachersButton);
            ResetButtonStyle(ContentManagersButton);
            ResetButtonStyle(GroupsButton); // Теперь GroupsButton существует

            // Выделяем выбранную
            selectedButton.BackgroundColor = Color.FromArgb("#4CAF50");
            selectedButton.TextColor = Colors.White;
        }

        private void ResetButtonStyle(Button button)
        {
            if (button == AllUsersButton)
                button.BackgroundColor = Color.FromArgb("#4CAF50");
            else if (button == StudentsButton)
                button.BackgroundColor = (Color)Application.Current.Resources["SecondaryColor"];
            else if (button == TeachersButton)
                button.BackgroundColor = (Color)Application.Current.Resources["AccentColor"];
            else if (button == ContentManagersButton)
                button.BackgroundColor = Color.FromArgb("#9C27B0");
            else if (button == GroupsButton)
                button.BackgroundColor = Color.FromArgb("#FF9800");

            button.TextColor = Colors.White;
        }
        private void UpdateSelectedRecipientsLabel()
        {
            int count = GetSelectedUsersCount();
            SelectedRecipientsLabel.Text = $"Выбрано: {count} пользователей";
        }

        private int GetSelectedUsersCount()
        {
            return _currentRecipientType switch
            {
                "all" => _allUsers.Count,
                "students" => _allUsers.Count(u => u.RoleId == 1),
                "teachers" => _allUsers.Count(u => u.RoleId == 2),
                "content_managers" => _allUsers.Count(u => u.RoleId == 4),
                "groups" => _selectableGroups.Where(g => g.IsSelected).Sum(g => g.StudentCount),
                _ => 0
            };
        }

        // Методы для работы с email рассылкой
        private async Task<List<string>> GetGroupMembersEmailsAsync()
        {
            var emails = new List<string>();
            var selectedGroups = _selectableGroups.Where(g => g.IsSelected).ToList();

            foreach (var group in selectedGroups)
            {
                var groupEmails = await _dbService.GetGroupMembersEmailsAsync(group.GroupId);
                emails.AddRange(groupEmails);
            }

            return emails.Distinct().ToList();
        }

        private List<string> GetSelectedUserEmails()
        {
            return _currentRecipientType switch
            {
                "all" => _allUsers.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList(),
                "students" => _allUsers.Where(u => u.RoleId == 1 && !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList(),
                "teachers" => _allUsers.Where(u => u.RoleId == 2 && !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList(),
                "content_managers" => _allUsers.Where(u => u.RoleId == 4 && !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList(),
                _ => new List<string>()
            };
        }

        private void OnGroupsSelected(object sender, SelectionChangedEventArgs e)
        {
            _currentRecipientType = "groups";

            // Обновляем выбранные группы
            if (e.CurrentSelection != null)
            {
                foreach (var item in e.CurrentSelection)
                {
                    if (item is SelectableGroup selectedGroup)
                    {
                        selectedGroup.IsSelected = true;
                    }
                }
            }

            if (e.PreviousSelection != null)
            {
                foreach (var item in e.PreviousSelection)
                {
                    if (item is SelectableGroup deselectedGroup)
                    {
                        deselectedGroup.IsSelected = false;
                    }
                }
            }

            UpdateSelectedRecipientsLabel();
            GroupsSection.IsVisible = true;
        }

        private async void OnSendEmailClicked(object sender, EventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NewsletterSubjectEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите тему рассылки", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewsletterContentEditor?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите текст рассылки", "OK");
                    return;
                }

                // Получаем количество получателей
                int recipientCount = GetSelectedUsersCount();
                if (recipientCount == 0)
                {
                    await DisplayAlert("Ошибка", "Нет выбранных получателей", "OK");
                    return;
                }

                bool confirm = await DisplayAlert("Подтверждение",
                    $"Отправить рассылку {recipientCount} пользователям?",
                    "Да", "Нет");

                if (!confirm) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingLabel.Text = "Отправка рассылки...";
                        LoadingOverlay.IsVisible = true;
                    }
                });

                // Получаем список email адресов
                List<string> emails = new List<string>();

                if (_currentRecipientType == "groups")
                {
                    emails = await GetGroupMembersEmailsAsync();
                }
                else
                {
                    emails = GetSelectedUserEmails();
                }

                // Убираем дубликаты и пустые значения
                emails = emails.Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();

                if (emails.Count == 0)
                {
                    await DisplayAlert("Ошибка", "Не найдено email адресов для выбранных получателей", "OK");
                    return;
                }

                // Добавляем подпись к тексту рассылки
                string signature = _currentUser.LanguagePref == "ru"
                    ? $"\n\n---\nС уважением,\nКоманда Educational Platform\nЭто письмо отправлено автоматически, пожалуйста, не отвечайте на него."
                    : $"\n\n---\nBest regards,\nEducational Platform Team\nThis is an automated message, please do not reply.";

                string fullMessage = NewsletterContentEditor.Text + signature;

                // Отправляем рассылку
                int successCount = 0;
                int failCount = 0;
                int total = emails.Count;

                for (int i = 0; i < emails.Count; i++)
                {
                    try
                    {
                        // Обновляем прогресс
                        if (LoadingLabel != null)
                        {
                            LoadingLabel.Text = $"Отправка {i + 1} из {total}...";
                        }

                        bool sent = await _emailService.SendNewsletterAsync(
                            emails[i],
                            NewsletterSubjectEntry.Text,
                            fullMessage);

                        if (sent)
                            successCount++;
                        else
                            failCount++;

                        // Небольшая задержка, чтобы не перегружать почтовый сервер
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка отправки на {emails[i]}: {ex.Message}");
                        failCount++;
                    }
                }

                await DisplayAlert("Результат",
                    $"Рассылка завершена\n\n✅ Успешно: {successCount}\n❌ Ошибок: {failCount}",
                    "OK");

                // Очищаем форму если были успешные отправки
                if (successCount > 0)
                {
                    NewsletterSubjectEntry.Text = "";
                    NewsletterContentEditor.Text = "";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingLabel.Text = "Загрузка...";
                        LoadingOverlay.IsVisible = false;
                    }
                });
            }
        }

        private void OnResetSelectionClicked(object sender, EventArgs e)
        {
            _currentRecipientType = "all";
            UpdateRecipientButtons(AllUsersButton);

            foreach (var group in _selectableGroups)
            {
                group.IsSelected = false;
            }

            GroupsSection.IsVisible = false;
            UpdateSelectedRecipientsLabel();
        }

        // Навигация
        private async void OnAllNewsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NewsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnManageNewsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new NewsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnStatsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new StatisticsPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}