using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class GroupStudentsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _groupId;
        private readonly string _groupName;

        private Entry? _searchEntry;
        private CollectionView? _studentsCollectionView;
        private Label? _groupTitleLabel;

        private ObservableCollection<StudentModel> _allStudents = new();
        private ObservableCollection<StudentModel> _filteredStudents = new();

        public ObservableCollection<StudentModel> Students
        {
            get => _filteredStudents;
            set
            {
                _filteredStudents = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GroupStudentsPage(User user, DatabaseService dbService, SettingsService settingsService, int groupId, string groupName)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации GroupStudentsPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _groupId = groupId;
            _groupName = groupName;

            InitializeControls();
            BindingContext = this;

            Task.Run(async () => await LoadStudentsAsync());
        }

        private void InitializeControls()
        {
            _searchEntry = this.FindByName<Entry>("SearchEntry");
            _studentsCollectionView = this.FindByName<CollectionView>("StudentsCollectionView");
            _groupTitleLabel = this.FindByName<Label>("GroupTitleLabel");

            if (_groupTitleLabel != null)
                _groupTitleLabel.Text = $"Студенты: {_groupName}";

            if (_studentsCollectionView != null)
                _studentsCollectionView.ItemsSource = Students;
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var students = await connection.QueryAsync<StudentModel>(@"
                    SELECT 
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.FirstName + ' ' + u.LastName as FullName,
                        u.AvatarUrl,
                        ge.EnrollmentDate
                    FROM GroupEnrollments ge
                    INNER JOIN Users u ON ge.StudentId = u.UserId
                    WHERE ge.GroupId = @GroupId AND ge.Status = 'active'
                    ORDER BY ge.EnrollmentDate DESC
                ", new { GroupId = _groupId });

                var studentList = students.ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _allStudents.Clear();
                    foreach (var student in studentList)
                    {
                        _allStudents.Add(student);
                    }

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки студентов: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", $"Не удалось загрузить студентов: {ex.Message}", "OK");
                });
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allStudents.AsEnumerable();

            if (_searchEntry != null && !string.IsNullOrWhiteSpace(_searchEntry.Text))
            {
                var searchText = _searchEntry.Text.ToLower();
                filtered = filtered.Where(s =>
                    (s.FullName?.ToLower().Contains(searchText) ?? false) ||
                    (s.Email?.ToLower().Contains(searchText) ?? false) ||
                    (s.Username?.ToLower().Contains(searchText) ?? false));
            }

            Students = new ObservableCollection<StudentModel>(filtered);

            if (_studentsCollectionView != null)
            {
                _studentsCollectionView.ItemsSource = null;
                _studentsCollectionView.ItemsSource = Students;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            string username = await DisplayPromptAsync("Добавить студента",
                "Введите логин студента:",
                keyboard: Keyboard.Text);

            if (!string.IsNullOrWhiteSpace(username))
            {
                try
                {
                    using var connection = new SqlConnection(_dbService.ConnectionString);
                    await connection.OpenAsync();

                    // Ищем студента по логину
                    var student = await connection.QueryFirstOrDefaultAsync<StudentModel>(@"
                        SELECT UserId, Username, Email, FirstName, LastName,
                               FirstName + ' ' + LastName as FullName, AvatarUrl
                        FROM Users 
                        WHERE Username = @Username AND RoleId = 1
                    ", new { Username = username.Trim() });

                    if (student == null)
                    {
                        await DisplayAlert("Ошибка", "Студент не найден", "OK");
                        return;
                    }

                    // Проверяем, не добавлен ли уже
                    var exists = await connection.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM GroupEnrollments 
                        WHERE GroupId = @GroupId AND StudentId = @StudentId AND Status = 'active'
                    ", new { GroupId = _groupId, StudentId = student.UserId });

                    if (exists > 0)
                    {
                        await DisplayAlert("Ошибка", "Студент уже в группе", "OK");
                        return;
                    }

                    // Добавляем студента
                    await connection.ExecuteAsync(@"
                        INSERT INTO GroupEnrollments (GroupId, StudentId, EnrollmentDate, Status)
                        VALUES (@GroupId, @StudentId, GETDATE(), 'active')
                    ", new { GroupId = _groupId, StudentId = student.UserId });

                    student.EnrollmentDate = DateTime.Now;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _allStudents.Add(student);
                        ApplyFilter();
                    });

                    await DisplayAlert("Успех", $"Студент {student.FullName} добавлен", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            }
        }

        private async void OnRemoveStudentClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is StudentModel student)
            {
                bool confirm = await DisplayAlert("Подтверждение",
                    $"Удалить студента {student.FullName} из группы?",
                    "Да", "Нет");

                if (confirm)
                {
                    try
                    {
                        using var connection = new SqlConnection(_dbService.ConnectionString);
                        await connection.OpenAsync();

                        await connection.ExecuteAsync(@"
                            UPDATE GroupEnrollments 
                            SET Status = 'dropped' 
                            WHERE GroupId = @GroupId AND StudentId = @StudentId
                        ", new { GroupId = _groupId, StudentId = student.UserId });

                        _allStudents.Remove(student);
                        ApplyFilter();

                        await DisplayAlert("Успех", "Студент удален из группы", "OK");
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

    public class StudentModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}