using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using Microsoft.Data.SqlClient; // Добавьте эту директиву

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

        // Создание группы из списка аккаунтов (логины через запятую)
        private async void OnCreateGroupWithUsersClicked(object sender, EventArgs e)
        {
            try
            {
                var name = await DisplayPromptAsync("Новая группа", "Название группы");
                if (string.IsNullOrWhiteSpace(name)) return;
                var courseIdStr = await DisplayPromptAsync("Курс", "ID курса для группы");
                if (!int.TryParse(courseIdStr, out int courseId)) return;
                var ok = await _dbService.CreateStudyGroupAsync(name, courseId, DateTime.Today, DateTime.Today.AddMonths(1), _currentUser.UserId);
                if (!ok)
                {
                    await DisplayAlert("Ошибка", "Не удалось создать группу", "OK");
                    return;
                }
                var usersCsv = await DisplayPromptAsync("Добавить студентов", "Укажите логины через запятую");
                if (!string.IsNullOrWhiteSpace(usersCsv))
                {
                    var usernames = usersCsv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                    var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                    var group = groups.OrderByDescending(g => g.GroupId).FirstOrDefault(g => g.CourseId == courseId && g.GroupName == name);
                    if (group != null)
                    {
                        foreach (var uname in usernames)
                        {
                            var u = await _dbService.GetUserByUsernameAsync(uname);
                            if (u != null && u.RoleId == 1)
                                await _dbService.EnrollStudentToGroupAsync(group.GroupId, u.UserId);
                        }
                        await DisplayAlert("Готово", "Группа создана и студенты добавлены", "OK");
                        LoadTeacherGroups();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void LoadTeacherGroups()
        {
            try
            {
                var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
                Groups.Clear();
                foreach (var group in groups)
                {
                    // Получаем название курса для группы
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
                // Добавляем даты начала и окончания
                var startDate = DateTime.Now;
                var endDate = startDate.AddMonths(3); // Группа на 3 месяца

                var success = await _dbService.CreateStudyGroupAsync(
                    NewGroupNameEntry.Text,
                    course.CourseId,
                    startDate,
                    endDate,
                    _currentUser.UserId);

                if (success)
                {
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

        private async void OnViewStudentsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherGroupInfo group)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService, null));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть студентов: {ex.Message}", "OK");
                }
            }
        }

        private async void OnChatClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherGroupInfo group)
            {
                try
                {
                    var studyGroup = new StudyGroup
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName
                        // StudentCount может отсутствовать в StudyGroup
                    };
                    await Navigation.PushAsync(new ChatPage(studyGroup, _currentUser, _dbService, _settingsService));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть чат: {ex.Message}", "OK");
                }
            }
        }

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

