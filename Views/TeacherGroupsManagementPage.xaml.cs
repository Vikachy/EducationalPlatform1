using EducationalPlatform.Models;
using System.Collections.ObjectModel;
using System.Data;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient;

namespace EducationalPlatform.Views
{
    public partial class TeacherGroupsManagementPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<TeacherGroupInfo> Groups { get; set; }

        public TeacherGroupsManagementPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            Groups = new ObservableCollection<TeacherGroupInfo>();
            BindingContext = this;

            LoadTeacherGroups();
            LoadCourses();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTeacherGroups();
        }

        private async void LoadTeacherGroups()
        {
            try
            {
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                Groups.Clear();
                foreach (var group in groups)
                {
                    var courseName = await GetCourseNameForGroup(group.GroupId);

                    Groups.Add(new TeacherGroupInfo
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = courseName ?? "Без курса",
                        StudentCount = group.StudentCount,
                        IsActive = group.IsActive
                    });
                }

                NoGroupsLabel.IsVisible = !Groups.Any();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить группы: {ex.Message}", "OK");
            }
        }

        private async Task<string?> GetCourseNameForGroup(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT c.CourseName 
                    FROM StudyGroups sg
                    JOIN Courses c ON sg.CourseId = c.CourseId
                    WHERE sg.GroupId = @GroupId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async void LoadCourses()
        {
            try
            {
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                CoursePicker.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки курсов: {ex.Message}");
            }
        }

        // СОЗДАНИЕ ГРУППЫ
        private async void OnCreateGroupClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewGroupNameEntry.Text))
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

                if (!await CheckCourseExists(course.CourseId))
                {
                    await DisplayAlert("Ошибка", "Выбранный курс не существует или не опубликован", "OK");
                    return;
                }

                var startDate = DateTime.Now;
                var endDate = startDate.AddMonths(3);

                var success = await _dbService.CreateStudyGroupAsync(
                    NewGroupNameEntry.Text,
                    course.CourseId,
                    startDate,
                    endDate,
                    _currentUser.UserId);

                if (success)
                {
                    // Получаем ID созданной группы
                    var groupId = await GetLastCreatedGroupId();

                    if (groupId > 0)
                    {
                        // Отправляем приветственное системное сообщение
                        var welcomeMessage = $"👋 Группа '{NewGroupNameEntry.Text}' создана! Добро пожаловать в учебный чат.";
                        await _dbService.AddSystemMessageToGroupAsync(groupId, welcomeMessage);
                    }

                    await DisplayAlert("Успех", "Группа создана!", "OK");
                    NewGroupNameEntry.Text = string.Empty;
                    CoursePicker.SelectedItem = null;
                    LoadTeacherGroups();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось создать группу", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
            }
        }

        // ДОБАВЬТЕ ЭТОТ МЕТОД В КЛАСС TeacherGroupsManagementPage
        private async Task<int> GetLastCreatedGroupId()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT TOP 1 GroupId FROM StudyGroups ORDER BY GroupId DESC";
                using var command = new SqlCommand(query, connection);

                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        // Проверка существования курса
        private async Task<bool> CheckCourseExists(int courseId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM Courses WHERE CourseId = @CourseId AND IsPublished = 1";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CourseId", courseId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        // РЕДАКТИРОВАНИЕ ГРУППЫ
        private async void OnEditGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TeacherGroupInfo group)
            {
                try
                {
                    var newName = await DisplayPromptAsync("Редактирование группы",
                        "Новое название группы:",
                        initialValue: group.GroupName);

                    if (!string.IsNullOrWhiteSpace(newName) && newName != group.GroupName)
                    {
                        var success = await UpdateGroupName(group.GroupId, newName);
                        if (success)
                        {
                            await DisplayAlert("Успех", "Название группы обновлено", "OK");
                            LoadTeacherGroups();
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось обновить название", "OK");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось редактировать группу: {ex.Message}", "OK");
                }
            }
        }

        private async Task<bool> UpdateGroupName(int groupId, string newName)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "UPDATE StudyGroups SET GroupName = @GroupName WHERE GroupId = @GroupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupName", newName);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // УДАЛЕНИЕ ГРУППЫ
        private async void OnDeleteGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TeacherGroupInfo group)
            {
                try
                {
                    bool confirm = await DisplayAlert("Подтверждение",
                        $"Вы уверены, что хотите удалить группу '{group.GroupName}'?",
                        "Да", "Нет");

                    if (confirm)
                    {
                        var success = await DeactivateGroup(group.GroupId);
                        if (success)
                        {
                            await DisplayAlert("Успех", "Группа удалена", "OK");
                            LoadTeacherGroups();
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Не удалось удалить группу", "OK");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить группу: {ex.Message}", "OK");
                }
            }
        }

        private async Task<bool> DeactivateGroup(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = "UPDATE StudyGroups SET IsActive = 0 WHERE GroupId = @GroupId";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // УПРАВЛЕНИЕ СТУДЕНТАМИ
        private async void OnManageStudentsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TeacherGroupInfo group)
            {
                try
                {
                    var action = await DisplayActionSheet(
                        $"Управление студентами: {group.GroupName}",
                        "Отмена",
                        null,
                        "Просмотреть список",
                        "Добавить студента",
                        "Удалить студента");

                    switch (action)
                    {
                        case "Просмотреть список":
                            await ViewGroupStudents(group);
                            break;
                        case "Добавить студента":
                            await AddStudentToGroup(group);
                            break;
                        case "Удалить студента":
                            await RemoveStudentFromGroup(group);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось управлять студентами: {ex.Message}", "OK");
                }
            }
        }

        private async Task ViewGroupStudents(TeacherGroupInfo group)
        {
            try
            {
                var students = await GetGroupStudents(group.GroupId);
                if (students.Any())
                {
                    var studentList = string.Join("\n", students.Select(s => $"- {s.Username} ({s.FirstName} {s.LastName})"));
                    await DisplayAlert($"Студенты группы {group.GroupName}", studentList, "OK");
                }
                else
                {
                    await DisplayAlert("Информация", "В группе нет студентов", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить список студентов: {ex.Message}", "OK");
            }
        }

        private async Task AddStudentToGroup(TeacherGroupInfo group)
        {
            var username = await DisplayPromptAsync("Добавить студента",
                "Введите логин студента:");

            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = await _dbService.GetUserByUsernameAsync(username.Trim());
                if (user != null && user.RoleId == 1) // Проверяем что это студент
                {
                    // Добавляем студента в группу
                    var success = await _dbService.EnrollStudentToGroupAsync(group.GroupId, user.UserId);

                    if (success)
                    {
                        // Отправляем системное сообщение в чат
                        var systemMessage = $"🎓 Студент {user.FirstName} {user.LastName} (@{user.Username}) присоединился к группе";
                        await _dbService.AddSystemMessageToGroupAsync(group.GroupId, systemMessage);

                        await DisplayAlert("Успех", $"Студент {username} добавлен в группу", "OK");
                        LoadTeacherGroups();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось добавить студента в группу", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Ошибка", "Студент не найден", "OK");
                }
            }
        }

        private async Task RemoveStudentFromGroup(TeacherGroupInfo group)
        {
            try
            {
                var students = await GetGroupStudents(group.GroupId);
                if (!students.Any())
                {
                    await DisplayAlert("Информация", "В группе нет студентов", "OK");
                    return;
                }

                var studentNames = students.Select(s => $"{s.Username} - {s.FirstName} {s.LastName}").ToArray();
                var selectedStudent = await DisplayActionSheet("Выберите студента для удаления:",
                    "Отмена", null, studentNames);

                if (selectedStudent != null && selectedStudent != "Отмена")
                {
                    var selectedUsername = selectedStudent.Split(" - ").First();
                    var student = students.FirstOrDefault(s => s.Username == selectedUsername);

                    if (student != null)
                    {
                        bool confirm = await DisplayAlert("Подтверждение",
                            $"Удалить студента {student.Username} из группы?", "Да", "Нет");

                        if (confirm)
                        {
                            await _dbService.RemoveStudentFromGroupAsync(group.GroupId, student.UserId);

                            // Отправляем системное сообщение об уходе студента
                            var systemMessage = $"👋 Студент {student.FirstName} {student.LastName} (@{student.Username}) покинул группу";
                            await _dbService.AddSystemMessageToGroupAsync(group.GroupId, systemMessage);

                            await DisplayAlert("Успех", "Студент удален из группы", "OK");
                            LoadTeacherGroups();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось удалить студента: {ex.Message}", "OK");
            }
        }

        private async Task<List<User>> GetGroupStudents(int groupId)
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT u.UserId, u.Username, u.FirstName, u.LastName, u.Email
                    FROM GroupEnrollments ge
                    JOIN Users u ON ge.StudentId = u.UserId
                    WHERE ge.GroupId = @GroupId AND ge.Status = 'active'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GroupId", groupId);

                var students = new List<User>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    students.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        FirstName = reader.IsDBNull("FirstName") ? null : reader.GetString("FirstName"),
                        LastName = reader.IsDBNull("LastName") ? null : reader.GetString("LastName"),
                        Email = reader.GetString("Email")
                    });
                }
                return students;
            }
            catch
            {
                return new List<User>();
            }
        }

        // ЧАТ ГРУППЫ
        private async void OnChatClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is TeacherGroupInfo group)
            {
                try
                {
                    var studyGroup = new StudyGroup
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName
                    };
                    await Navigation.PushAsync(new ChatPage(studyGroup, _currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }
        }

        // НАЗАД
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class TeacherGroupInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
    }
}