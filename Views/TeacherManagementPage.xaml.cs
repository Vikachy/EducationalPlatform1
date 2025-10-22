using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Converters;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EducationalPlatform.Views
{
    public partial class TeacherManagementPage : ContentPage
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        public ObservableCollection<TeacherCourse> MyCourses { get; set; }
        public ObservableCollection<PendingReview> PendingReviews { get; set; }

        public TeacherManagementPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            MyCourses = new ObservableCollection<TeacherCourse>();
            PendingReviews = new ObservableCollection<PendingReview>();

            // ������������ ����������
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());
            Resources.Add("PublishButtonConverter", new PublishButtonTextConverter());
            Resources.Add("PublishButtonColorConverter", new PublishButtonColorConverter());

            BindingContext = this;

            LoadTeacherData();
        }

        private async void LoadTeacherData()
        {
            try
            {
                var courses = await _dbService.GetTeacherCoursesAsync(_currentUser.UserId);
                MyCourses.Clear();
                foreach (var course in courses)
                {
                    MyCourses.Add(course);
                }

                MyCoursesCollectionView.ItemsSource = MyCourses;

                // ��������� ������ �� ��������
                LoadPendingReviews();

                // ��������� ������
                LoadPickersData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"�� ������� ��������� ������: {ex.Message}", "OK");
            }
        }

        private void LoadPendingReviews()
        {
            try
            {
                PendingReviews.Clear();

                // �������� ��������� �������� ������
                PendingReviews.Add(new PendingReview
                {
                    AttemptId = 1,
                    StudentName = "���� ������",
                    CourseName = "C# ��� ����������",
                    TestTitle = "������ ���",
                    Score = 75
                });

                PendingReviewsCollectionView.ItemsSource = PendingReviews;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ �������� ����� �� ��������: {ex.Message}");
            }
        }

        private void LoadPickersData()
        {
            try
            {
                // �������� ��������� �������� ������
                var languages = new List<ProgrammingLanguage>
                {
                    new ProgrammingLanguage { LanguageId = 1, LanguageName = "C#" },
                    new ProgrammingLanguage { LanguageId = 2, LanguageName = "Python" },
                    new ProgrammingLanguage { LanguageId = 3, LanguageName = "Java" }
                };
                LanguagePicker.ItemsSource = languages;

                var difficulties = new List<CourseDifficulty>
                {
                    new CourseDifficulty { DifficultyId = 1, DifficultyName = "������" },
                    new CourseDifficulty { DifficultyId = 2, DifficultyName = "�������" },
                    new CourseDifficulty { DifficultyId = 3, DifficultyName = "�������" }
                };
                DifficultyPicker.ItemsSource = difficulties;

                CourseForTestPicker.ItemsSource = MyCourses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ �������� ������ ��� �������: {ex.Message}");
            }
        }

        // �������� �����
        private async void OnCreateCourseClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCourseNameEntry.Text))
            {
                await DisplayAlert("������", "������� �������� �����", "OK");
                return;
            }

            if (LanguagePicker.SelectedItem == null || DifficultyPicker.SelectedItem == null)
            {
                await DisplayAlert("������", "�������� ���� ���������������� � ���������", "OK");
                return;
            }

            try
            {
                var selectedLanguage = (ProgrammingLanguage)LanguagePicker.SelectedItem;
                var selectedDifficulty = (CourseDifficulty)DifficultyPicker.SelectedItem;

                bool success = await _dbService.CreateCourseAsync(
                    NewCourseNameEntry.Text,
                    NewCourseDescriptionEditor.Text,
                    selectedLanguage.LanguageId,
                    selectedDifficulty.DifficultyId,
                    _currentUser.UserId,
                    IsGroupCourseCheckBox.IsChecked);

                if (success)
                {
                    await DisplayAlert("�����", "���� ������� ������!", "OK");
                    // ������� ����
                    NewCourseNameEntry.Text = "";
                    NewCourseDescriptionEditor.Text = "";
                    LanguagePicker.SelectedItem = null;
                    DifficultyPicker.SelectedItem = null;
                    IsGroupCourseCheckBox.IsChecked = false;

                    // ��������� ������ ������
                    LoadTeacherData();
                }
                else
                {
                    await DisplayAlert("������", "�� ������� ������� ����", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"������ �������� �����: {ex.Message}", "OK");
            }
        }

        // �������� �����
        private async void OnCreateTestClicked(object sender, EventArgs e)
        {
            if (CourseForTestPicker.SelectedItem == null)
            {
                await DisplayAlert("������", "�������� ���� ��� �����", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewTestTitleEntry.Text))
            {
                await DisplayAlert("������", "������� �������� �����", "OK");
                return;
            }

            try
            {
                var selectedCourse = (TeacherCourse)CourseForTestPicker.SelectedItem;

                bool success = await _dbService.CreateTestAsync(
                    selectedCourse.CourseId,
                    NewTestTitleEntry.Text,
                    NewTestDescriptionEditor.Text,
                    int.TryParse(TimeLimitEntry.Text, out int timeLimit) ? timeLimit : 30,
                    int.TryParse(PassingScoreEntry.Text, out int passingScore) ? passingScore : 60);

                if (success)
                {
                    await DisplayAlert("�����", "���� ������� ������!", "OK");
                    // ������� ����
                    NewTestTitleEntry.Text = "";
                    NewTestDescriptionEditor.Text = "";
                    TimeLimitEntry.Text = "";
                    PassingScoreEntry.Text = "";
                    CourseForTestPicker.SelectedItem = null;
                }
                else
                {
                    await DisplayAlert("������", "�� ������� ������� ����", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", $"������ �������� �����: {ex.Message}", "OK");
            }
        }

        // ���������� �������
        private async void OnPublishCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    bool success = await _dbService.PublishCourseAsync(course.CourseId, _currentUser.UserId);
                    if (success)
                    {
                        await DisplayAlert("�����",
                            course.IsPublished ? "���� ���� � ����������!" : "���� �����������!",
                            "OK");
                        LoadTeacherData();
                    }
                    else
                    {
                        await DisplayAlert("������", "�� ������� �������� ������ �����", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("������", $"������: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("������", $"���������� �������� �����: {course.CourseName}", "OK");
            }
        }

        private async void OnCourseStatsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("����������", $"���������� �� �����: {course.CourseName}", "OK");
            }
        }

        // �������� �����
        private async void OnReviewWorkClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is PendingReview review)
            {
                await DisplayAlert("��������", $"�������� ������: {review.StudentName}", "OK");
            }
        }

        // ������� ������
        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("������", "��������� ������� � Word/Excel", "OK");
        }

        private async void OnGroupsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("������", "���������� �������� ��������", "OK");
        }

        private async void OnTestsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("�����", "���������� ������� � ���������", "OK");
        }

        private async void OnChatsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("����", "������� � ���������", "OK");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    // �������������� ������
    public class PendingReview
    {
        public int AttemptId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string TestTitle { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    // ���������� ��� ������
    public class PublishButtonTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool published && published ? "�����" : "������������";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PublishButtonColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool published && published ? Color.FromArgb("#FF9800") : Color.FromArgb("#4CAF50");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}