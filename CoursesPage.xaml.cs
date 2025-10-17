using EducationalPlatform.Services;
using EducationalPlatform.Models;

namespace EducationalPlatform;

public partial class CoursesPage : ContentPage
{
    private DatabaseService _dbService;
    private User _currentUser;

    public CoursesPage(User user, DatabaseService dbService)
    {
        InitializeComponent();
        _currentUser = user;
        _dbService = dbService;
        LoadCourses();
    }

    private async void LoadCourses()
    {
        var courses = await _dbService.GetCoursesAsync();
        CoursesCollectionView.ItemsSource = courses;
        WelcomeLabel.Text = $"Добро пожаловать, {_currentUser.FirstName}! Серия: {_currentUser.StreakDays} дней";
    }

    private async void OnStartCourseClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var course = (Course)button.BindingContext;

        var success = await _dbService.UpdateProgressAsync(_currentUser.UserId, course.CourseId, "in_progress");

        if (success)
        {
            await DisplayAlert("Успех", $"Курс '{course.CourseName}' начат!", "OK");
        }
    }
}
