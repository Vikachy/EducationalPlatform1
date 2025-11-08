using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;

namespace EducationalPlatform.Views
{
    public partial class TeacherGroupsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly TeacherCourse _courseContext;

        public ObservableCollection<StudyGroup> Groups { get; set; } = new();
        public ObservableCollection<User> GroupStudents { get; set; } = new();

        // Конструктор с 3 аргументами (для вызова из MainDashboardPage)
        public TeacherGroupsPage(User user, DatabaseService dbService, SettingsService settingsService)
            : this(user, dbService, settingsService, null)
        {
        }

        // Основной конструктор с 4 аргументами
        public TeacherGroupsPage(User user, DatabaseService dbService, SettingsService settingsService, TeacherCourse course)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _courseContext = course;

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGroups();
            await LoadCourses();
            if (_courseContext != null)
            {
                CoursePicker.SelectedItem = _courseContext;
            }
        }

        private async Task LoadCourses()
        {
            try
            {
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                CoursePicker.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить курсы: {ex.Message}", "OK");
            }
        }

        // ЗАГРУЗКА ГРУПП ПРЕПОДАВАТЕЛЯ
        private async Task LoadGroups()
        {
            try
            {
                Groups.Clear();
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }
                GroupsCollection.ItemsSource = Groups;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить группы: {ex.Message}", "OK");
            }
        }

        // СОЗДАНИЕ НОВОЙ ГРУППЫ
        private async void OnCreateGroupClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите название группы", "OK");
                return;
            }

            if (CoursePicker.SelectedItem == null)
            {
                await DisplayAlert("Ошибка", "Выберите курс", "OK");
                return;
            }

            try
            {
                var course = (TeacherCourse)CoursePicker.SelectedItem;
                var startDate = DateTime.Now;
                var endDate = startDate.AddMonths(9); // Группа на 3 месяца

                var success = await _dbService.CreateStudyGroupAsync(
                    GroupNameEntry.Text.Trim(),
                    course.CourseId,
                    startDate,
                    endDate,
                    _currentUser.UserId);

                if (success)
                {
                    await DisplayAlert("Успех", $"Группа '{GroupNameEntry.Text}' создана!", "OK");
                    GroupNameEntry.Text = string.Empty;
                    CoursePicker.SelectedItem = null;
                    await LoadGroups();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать группу", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при создании группы: {ex.Message}", "OK");
            }
        }

        // ВЫБОР ГРУППЫ И ЗАГРУЗКА СТУДЕНТОВ
        private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StudyGroup selectedGroup)
            {
                await LoadGroupStudents(selectedGroup.GroupId);
                StudentsSection.IsVisible = true;
            }
            else
            {
                StudentsSection.IsVisible = false;
                GroupStudents.Clear();
            }
        }

        // ЗАГРУЗКА СТУДЕНТОВ ГРУППЫ
        private async Task LoadGroupStudents(int groupId)
        {
            try
            {
                GroupStudents.Clear();
                var students = await _dbService.GetGroupStudentsAsync(groupId);
                foreach (var student in students)
                {
                    GroupStudents.Add(student);
                }
                GroupStudentsCollection.ItemsSource = GroupStudents;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить студентов: {ex.Message}", "OK");
            }
        }

        // ОТКРЫТИЕ ЧАТА ГРУППЫ
        private async void OnOpenChatClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is StudyGroup group)
            {
                try
                {
                    await Navigation.PushAsync(new GroupChatPage(group, _currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }
        }

        // ДЕАКТИВАЦИЯ ГРУППЫ
        private async void OnDeactivateGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is StudyGroup group)
            {
                bool confirm = await DisplayAlert(
                    "Подтверждение",
                    $"Вы действительно хотите деактивировать группу '{group.GroupName}'?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        bool success = await _dbService.DeactivateGroupAsync(group.GroupId);
                        if (success)
                        {
                            await DisplayAlert("Успех", "Группа деактивирована", "OK");
                            await LoadGroups();
                            StudentsSection.IsVisible = false;
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось деактивировать группу", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", $"Ошибка при деактивации: {ex.Message}", "OK");
                    }
                }
            }
        }

        // ДОБАВЛЕНИЕ СТУДЕНТА В ГРУППУ
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            if (GroupsCollection.SelectedItem is not StudyGroup selectedGroup)
            {
                await DisplayAlert("Ошибка", "Сначала выберите группу", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(StudentUsernameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите имя пользователя студента", "OK");
                return;
            }

            try
            {
                // Поиск студента по имени пользователя
                var student = await _dbService.GetUserByUsernameAsync(StudentUsernameEntry.Text.Trim());

                if (student == null)
                {
                    await DisplayAlert("Ошибка", "Студент не найден", "OK");
                    return;
                }

                if (student.RoleId != 1) // Проверяем, что это студент (RoleId = 1)
                {
                    await DisplayAlert("Ошибка", "Пользователь не является студентом", "OK");
                    return;
                }

                // Добавление студента в группу
                bool success = await _dbService.EnrollStudentToGroupAsync(selectedGroup.GroupId, student.UserId);

                if (success)
                {
                    await DisplayAlert("Успех", $"Студент {student.Username} добавлен в группу", "OK");
                    StudentUsernameEntry.Text = string.Empty;
                    await LoadGroupStudents(selectedGroup.GroupId);
                    await LoadGroups(); // Обновляем счетчик студентов
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось добавить студента в группу", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при добавлении студента: {ex.Message}", "OK");
            }
        }

        // УДАЛЕНИЕ СТУДЕНТА ИЗ ГРУППЫ
        private async void OnRemoveStudentClicked(object sender, EventArgs e)
        {
            if (sender is Button button &&
                button.CommandParameter is User student &&
                GroupsCollection.SelectedItem is StudyGroup selectedGroup)
            {
                bool confirm = await DisplayAlert(
                    "Подтверждение",
                    $"Удалить студента {student.Username} из группы?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        bool success = await _dbService.RemoveStudentFromGroupAsync(selectedGroup.GroupId, student.UserId);

                        if (success)
                        {
                            await DisplayAlert("Успех", "Студент удален из группы", "OK");
                            await LoadGroupStudents(selectedGroup.GroupId);
                            await LoadGroups(); // Обновляем счетчик студентов
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось удалить студента", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", $"Ошибка при удалении студента: {ex.Message}", "OK");
                    }
                }
            }
        }

        // МАССОВОЕ ДОБАВЛЕНИЕ СТУДЕНТОВ - ИСПРАВЛЕННАЯ ВЕРСИЯ
        private async void OnAddMultipleStudentsClicked(object sender, EventArgs e)
        {
            if (GroupsCollection.SelectedItem is not StudyGroup selectedGroup)
            {
                await DisplayAlert("Ошибка", "Сначала выберите группу", "OK");
                return;
            }

            try
            {
                Console.WriteLine($"🎯 Начинаем добавление студентов в группу {selectedGroup.GroupName} (ID: {selectedGroup.GroupId})");

                // Загружаем всех студентов
                var allStudents = await _dbService.GetAllStudentsAsync();
                Console.WriteLine($"📊 Загружено всех студентов: {allStudents?.Count ?? 0}");

                // Создаем список для выбора
                var selectionItems = allStudents.Select(s => new StudentSelectionItem
                {
                    Student = s,
                    IsSelected = false
                }).ToList();

                Console.WriteLine($"🎯 Создано элементов для выбора: {selectionItems.Count}");

                // Открываем страницу выбора
                var selectionPage = new StudentSelectionPage(
                    selectionItems,
                    selectedGroup.GroupName,
                    selectedGroup.GroupId,
                    selectedGroup.CourseId,
                    _dbService);

                selectionPage.StudentsSelected += async (s, selectedStudents) =>
                {
                    if (selectedStudents.Any())
                    {
                        Console.WriteLine($"🔄 Обрабатываем выбранных студентов: {selectedStudents.Count}");

                        // Сохраняем студентов в группу и чат
                        bool success = await _dbService.AddStudentsToGroupAsync(selectedGroup.GroupId, selectedStudents);

                        if (success)
                        {
                            // Добавляем студентов в групповой чат
                            bool chatSuccess = await _dbService.AddStudentsToGroupChatAsync(selectedGroup.GroupId, selectedStudents);

                            // Отправляем системное сообщение в чат группы
                            await _dbService.AddSystemMessageToGroupAsync(selectedGroup.GroupId,
                                $"В группу добавлено {selectedStudents.Count} новых студентов");

                            string message = chatSuccess ?
                                $"Добавлено {selectedStudents.Count} студентов в группу и чат" :
                                $"Добавлено {selectedStudents.Count} студентов в группу (ошибка добавления в чат)";

                            await DisplayAlert("Успех", message, "OK");
                            await LoadGroupStudents(selectedGroup.GroupId);
                            await LoadGroups(); // Обновляем счетчик
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось добавить студентов", "OK");
                        }
                    }
                };

                await Navigation.PushAsync(selectionPage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ОШИБКА при выборе студентов: {ex.Message}");
                await DisplayAlert("Ошибка", $"Ошибка при выборе студентов: {ex.Message}", "OK");
            }
        }

        // ЗАПИСЬ ВСЕЙ ГРУППЫ НА КУРС
        private async void OnEnrollGroupToCourseClicked(object sender, EventArgs e)
        {
            if (GroupsCollection.SelectedItem is not StudyGroup selectedGroup)
            {
                await DisplayAlert("Ошибка", "Сначала выберите группу", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Подтверждение",
                $"Записать всех студентов группы '{selectedGroup.GroupName}' на курс?",
                "Да", "Нет");

            if (confirm)
            {
                try
                {
                    var success = await _dbService.EnrollGroupToCourseAsync(selectedGroup.GroupId);

                    if (success)
                    {
                        await DisplayAlert("Успех", "Все студенты группы записаны на курс", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось записать студентов на курс", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Ошибка при записи на курс: {ex.Message}", "OK");
                }
            }
        }

        // НАЗАД
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}