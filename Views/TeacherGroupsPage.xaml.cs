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
            var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
            CoursePicker.ItemsSource = courses;
            if (_courseContext != null)
            {
                CoursePicker.SelectedItem = courses.FirstOrDefault(c => c.CourseId == _courseContext.CourseId);
            }
        }

        private async Task LoadGroups()
        {
            Groups.Clear();
            var groups = await _dbService.GetTeacherStudyGroupsAsync(_currentUser.UserId);
            foreach (var g in groups) Groups.Add(g);
            GroupsCollection.ItemsSource = Groups;
        }

        private async void OnCreateGroupClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameEntry.Text) || CoursePicker.SelectedItem is not TeacherCourse course)
            {
                await DisplayAlert("Ошибка", "Введите название и выберите курс", "OK");
                return;
            }

            bool ok = await _dbService.CreateStudyGroupAsync(GroupNameEntry.Text!, course.CourseId, DateTime.Today, DateTime.Today.AddMonths(1), _currentUser.UserId);
            if (ok)
            {
                await DisplayAlert("Успех", "Группа создана", "OK");
                GroupNameEntry.Text = string.Empty;
                await LoadGroups();
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось создать группу", "OK");
            }
        }

        private async void OnOpenChatClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is StudyGroup group)
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

        private void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StudyGroup group)
            {
                LoadGroupStudents(group.GroupId);
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void LoadGroupStudents(int groupId)
        {
            try
            {
                var students = await _dbService.GetGroupStudentsAsync(groupId);
                GroupStudents.Clear();
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

        private async void OnDeactivateGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is StudyGroup group)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы действительно хотите деактивировать группу {group.GroupName}?", "Да", "Нет");

                if (confirm)
                {
                    bool ok = await _dbService.DeactivateGroupAsync(group.GroupId);
                    if (ok)
                    {
                        await DisplayAlert("Успех", "Группа деактивирована", "OK");
                        await LoadGroups();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось деактивировать группу", "OK");
                    }
                }
            }
        }

        private async void OnRemoveStudentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is User student &&
                GroupsCollection.SelectedItem is StudyGroup group)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Удалить студента {student.Username} из группы?", "Да", "Нет");

                if (confirm)
                {
                    bool ok = await _dbService.RemoveStudentFromGroupAsync(group.GroupId, student.UserId);
                    if (ok)
                    {
                        await DisplayAlert("Успех", "Студент удален из группы", "OK");
                        LoadGroupStudents(group.GroupId);
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Не удалось удалить студента", "OK");
                    }
                }
            }
        }

        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            if (GroupsCollection.SelectedItem is not StudyGroup group)
            {
                await DisplayAlert("Ошибка", "Выберите группу", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(StudentUsernameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите имя пользователя", "OK");
                return;
            }

            var user = await _dbService.GetUserByUsernameAsync(StudentUsernameEntry.Text);
            if (user == null || user.RoleId != 1)
            {
                await DisplayAlert("Ошибка", "Студент не найден", "OK");
                return;
            }

            bool ok = await _dbService.EnrollStudentToGroupAsync(group.GroupId, user.UserId);
            if (ok)
            {
                await DisplayAlert("Успех", "Студент добавлен в группу", "OK");
                StudentUsernameEntry.Text = string.Empty;
                LoadGroupStudents(group.GroupId);
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось добавить студента", "OK");
            }
        }
    }
}