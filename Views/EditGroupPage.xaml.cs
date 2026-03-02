using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using Dapper;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EducationalPlatform.Views
{
    public partial class EditGroupPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly int _groupId;

        private Entry? _groupNameEntry;
        private Picker? _coursePicker;
        private Picker? _teacherPicker;
        private DatePicker? _startDatePicker;
        private DatePicker? _endDatePicker;
        private Switch? _isActiveSwitch;
        private Image? _avatarImage;
        private Label? _titleLabel;

        private ObservableCollection<CourseItem> _courses = new();
        private ObservableCollection<TeacherItem> _teachers = new();

        private byte[]? _selectedAvatarBytes;
        private string? _selectedAvatarFileName;

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EditGroupPage(User user, DatabaseService dbService, SettingsService settingsService, int groupId)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации EditGroupPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _groupId = groupId;

            InitializeControls();
            LoadData();
        }

        private void InitializeControls()
        {
            _titleLabel = this.FindByName<Label>("TitleLabel");
            _groupNameEntry = this.FindByName<Entry>("GroupNameEntry");
            _coursePicker = this.FindByName<Picker>("CoursePicker");
            _teacherPicker = this.FindByName<Picker>("TeacherPicker");
            _startDatePicker = this.FindByName<DatePicker>("StartDatePicker");
            _endDatePicker = this.FindByName<DatePicker>("EndDatePicker");
            _isActiveSwitch = this.FindByName<Switch>("IsActiveSwitch");
            _avatarImage = this.FindByName<Image>("AvatarImage");

            if (_coursePicker != null)
            {
                _coursePicker.ItemsSource = _courses;
                _coursePicker.ItemDisplayBinding = new Binding("CourseName");
            }

            if (_teacherPicker != null)
            {
                _teacherPicker.ItemsSource = _teachers;
                _teacherPicker.ItemDisplayBinding = new Binding("FullName");
            }
        }

        private async void LoadData()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                // Загружаем курсы
                var courses = await connection.QueryAsync<CourseItem>(@"
                    SELECT CourseId, CourseName FROM Courses ORDER BY CourseName
                ");

                foreach (var course in courses)
                {
                    _courses.Add(course);
                }

                // Загружаем преподавателей
                var teachers = await connection.QueryAsync<TeacherItem>(@"
                    SELECT UserId, FirstName, LastName, 
                           FirstName + ' ' + LastName as FullName 
                    FROM Users WHERE RoleId = 2 ORDER BY FirstName
                ");

                foreach (var teacher in teachers)
                {
                    _teachers.Add(teacher);
                }

                // Если редактируем существующую группу
                if (_groupId > 0)
                {
                    var group = await connection.QueryFirstOrDefaultAsync<GroupData>(@"
                        SELECT * FROM StudyGroups WHERE GroupId = @GroupId
                    ", new { GroupId = _groupId });

                    if (group != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (_groupNameEntry != null)
                                _groupNameEntry.Text = group.GroupName;

                            if (_coursePicker != null && _courses.Any())
                            {
                                var courseIndex = _courses.IndexOf(_courses.FirstOrDefault(c => c.CourseId == group.CourseId));
                                if (courseIndex >= 0)
                                    _coursePicker.SelectedIndex = courseIndex;
                            }

                            if (_teacherPicker != null && _teachers.Any())
                            {
                                var teacherIndex = _teachers.IndexOf(_teachers.FirstOrDefault(t => t.UserId == group.TeacherId));
                                if (teacherIndex >= 0)
                                    _teacherPicker.SelectedIndex = teacherIndex;
                            }

                            if (_startDatePicker != null)
                                _startDatePicker.Date = group.StartDate;

                            if (_endDatePicker != null)
                                _endDatePicker.Date = group.EndDate;

                            if (_isActiveSwitch != null)
                                _isActiveSwitch.IsToggled = group.IsActive;

                            if (!string.IsNullOrEmpty(group.AvatarUrl) && _avatarImage != null)
                            {
                                _avatarImage.Source = group.AvatarUrl;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnSelectAvatarClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите аватар для группы",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                        { DevicePlatform.Android, new[] { "image/jpeg", "image/png", "image/gif", "image/bmp" } },
                        { DevicePlatform.iOS, new[] { "public.image" } },
                    })
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    _selectedAvatarBytes = memoryStream.ToArray();
                    _selectedAvatarFileName = result.FileName;

                    if (_avatarImage != null)
                    {
                        _avatarImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedAvatarBytes));
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось выбрать аватар: {ex.Message}", "OK");
            }
        }

        private void OnRemoveAvatarClicked(object sender, EventArgs e)
        {
            _selectedAvatarBytes = null;
            _selectedAvatarFileName = null;
            if (_avatarImage != null)
            {
                _avatarImage.Source = "default_group.png";
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_groupNameEntry?.Text))
                {
                    await DisplayAlert("Ошибка", "Введите название группы", "OK");
                    return;
                }

                if (_coursePicker?.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите курс", "OK");
                    return;
                }

                if (_teacherPicker?.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Выберите преподавателя", "OK");
                    return;
                }

                var course = (CourseItem)_coursePicker.SelectedItem;
                var teacher = (TeacherItem)_teacherPicker.SelectedItem;

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                if (_groupId > 0)
                {
                    // Обновление группы
                    await connection.ExecuteAsync(@"
                        UPDATE StudyGroups SET 
                            GroupName = @GroupName,
                            CourseId = @CourseId,
                            TeacherId = @TeacherId,
                            StartDate = @StartDate,
                            EndDate = @EndDate,
                            IsActive = @IsActive
                        WHERE GroupId = @GroupId
                    ", new
                    {
                        GroupId = _groupId,
                        GroupName = _groupNameEntry.Text.Trim(),
                        CourseId = course.CourseId,
                        TeacherId = teacher.UserId,
                        StartDate = _startDatePicker?.Date ?? DateTime.Now,
                        EndDate = _endDatePicker?.Date ?? DateTime.Now.AddMonths(3),
                        IsActive = _isActiveSwitch?.IsToggled ?? true
                    });

                    // Если есть новый аватар
                    if (_selectedAvatarBytes != null)
                    {
                        await SaveGroupAvatarAsync(_groupId, _selectedAvatarBytes, _selectedAvatarFileName ?? "group.jpg");
                    }

                    await DisplayAlert("Успех", "Группа обновлена", "OK");
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task SaveGroupAvatarAsync(int groupId, byte[] imageBytes, string fileName)
        {
            try
            {
                string avatarsFolder = Path.Combine(FileSystem.AppDataDirectory, "GroupAvatars");
                if (!Directory.Exists(avatarsFolder))
                    Directory.CreateDirectory(avatarsFolder);

                string fileExtension = Path.GetExtension(fileName);
                string newFileName = $"group_{groupId}{fileExtension}";
                string filePath = Path.Combine(avatarsFolder, newFileName);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE StudyGroups SET AvatarUrl = @AvatarUrl WHERE GroupId = @GroupId",
                    new { GroupId = groupId, AvatarUrl = filePath });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения аватара: {ex.Message}");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class CourseItem
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
    }

    public class TeacherItem
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class GroupData
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
    }
}