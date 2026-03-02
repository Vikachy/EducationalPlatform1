using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminGroupsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        private Entry? _searchEntry;
        private CollectionView? _groupsCollectionView;
        private Label? _totalGroupsLabel;
        private Label? _activeGroupsLabel;
        private Label? _totalStudentsLabel;

        private ObservableCollection<AdminGroupModel> _allGroups = new();
        private ObservableCollection<AdminGroupModel> _filteredGroups = new();

        public ObservableCollection<AdminGroupModel> Groups
        {
            get => _filteredGroups;
            set
            {
                _filteredGroups = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminGroupsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminGroupsPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadGroupsAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _groupsCollectionView = this.FindByName<CollectionView>("GroupsCollectionView");
            _totalGroupsLabel = this.FindByName<Label>("TotalGroupsLabel");
            _activeGroupsLabel = this.FindByName<Label>("ActiveGroupsLabel");
            _totalStudentsLabel = this.FindByName<Label>("TotalStudentsLabel");

            if (_groupsCollectionView != null)
                _groupsCollectionView.ItemsSource = Groups;
        }

        private async Task LoadGroupsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var groups = await connection.QueryAsync<AdminGroupModel>(@"
                    SELECT 
                        sg.GroupId,
                        sg.GroupName,
                        sg.CourseId,
                        ISNULL(c.CourseName, 'Без курса') as CourseName,
                        sg.TeacherId,
                        ISNULL(u.FirstName + ' ' + u.LastName, 'Не назначен') as TeacherName,
                        sg.StartDate,
                        sg.EndDate,
                        ISNULL(sg.IsActive, 1) as IsActive,
                        sg.AvatarUrl,
                        ISNULL(sg.CreatedDate, GETDATE()) as CreatedDate,
                        (SELECT COUNT(*) FROM GroupEnrollments WHERE GroupId = sg.GroupId AND Status = 'active') as StudentCount
                    FROM StudyGroups sg
                    LEFT JOIN Courses c ON sg.CourseId = c.CourseId
                    LEFT JOIN Users u ON sg.TeacherId = u.UserId
                    ORDER BY sg.CreatedDate DESC
                ");

                var groupList = groups.ToList();

                // Подсчет статистики
                int totalGroups = groupList.Count;
                int activeGroups = groupList.Count(g => g.IsActive);
                int totalStudents = groupList.Sum(g => g.StudentCount);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_totalGroupsLabel != null)
                        _totalGroupsLabel.Text = totalGroups.ToString();

                    if (_activeGroupsLabel != null)
                        _activeGroupsLabel.Text = activeGroups.ToString();

                    if (_totalStudentsLabel != null)
                        _totalStudentsLabel.Text = totalStudents.ToString();

                    _allGroups.Clear();
                    foreach (var group in groupList)
                    {
                        _allGroups.Add(group);
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки групп: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить группы: {ex.Message}", "OK");
                });
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allGroups.AsEnumerable();

            // Поиск по названию группы, курсу или преподавателю
            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(g =>
                    (g.GroupName?.ToLower().Contains(searchText) ?? false) ||
                    (g.CourseName?.ToLower().Contains(searchText) ?? false) ||
                    (g.TeacherName?.ToLower().Contains(searchText) ?? false));
            }

            Groups = new ObservableCollection<AdminGroupModel>(filtered);

            if (_groupsCollectionView != null)
            {
                _groupsCollectionView.ItemsSource = null;
                _groupsCollectionView.ItemsSource = Groups;
            }

            Console.WriteLine($"📊 Отфильтровано групп: {Groups.Count}");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnAddGroupClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TeacherGroupsManagementPage(_currentUser, _dbService, _settingsService));
        }

        private async void OnEditGroupClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminGroupModel group)
            {
                // Переход на страницу редактирования группы
                await Navigation.PushAsync(new EditGroupPage(_currentUser, _dbService, _settingsService, group.GroupId));
            }
        }

        private async void OnManageStudentsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminGroupModel group)
            {
                // Переход на страницу управления студентами группы
                await Navigation.PushAsync(new GroupStudentsPage(_currentUser, _dbService, _settingsService, group.GroupId, group.GroupName));
            }
        }

        private async void OnGroupInfoClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminGroupModel group)
            {
                string info = $"ИНФОРМАЦИЯ О ГРУППЕ\n\n" +
                             $"Название: {group.GroupName}\n" +
                             $"Курс: {group.CourseName}\n" +
                             $" Преподаватель: {group.TeacherName}\n" +
                             $"Студентов: {group.StudentCount}\n" +
                             $"Создана: {group.CreatedDate:dd.MM.yyyy}\n" +
                             $"Период: {group.StartDate:dd.MM.yyyy} - {group.EndDate:dd.MM.yyyy}\n" +
                             $" Статус: {(group.IsActive ? "Активна" : "Неактивна")}";

                await DisplayAlert("Информация о группе", info, "OK");
            }
        }

        private async void OnToggleStatusClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is AdminGroupModel group)
            {
                string action = group.IsActive ? "деактивировать" : "активировать";
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Вы уверены, что хотите {action} группу '{group.GroupName}'?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        await connection.ExecuteAsync(
                            "UPDATE StudyGroups SET IsActive = @IsActive WHERE GroupId = @GroupId",
                            new { GroupId = group.GroupId, IsActive = !group.IsActive });

                        group.IsActive = !group.IsActive;

                        // Обновляем статистику
                        int activeGroups = _allGroups.Count(g => g.IsActive);
                        if (_activeGroupsLabel != null)
                            _activeGroupsLabel.Text = activeGroups.ToString();

                        ApplyFilter();

                        await DisplayAlert("Успех", $"Группа {action}на", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Ошибка", ex.Message, "OK");
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class AdminGroupModel
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public int StudentCount { get; set; }
    }
}