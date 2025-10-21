using EducationalPlatform.Views;
using EducationalPlatform.Models;

namespace EducationalPlatform
{
    public partial class AppShell : Shell
    {
        private User _currentUser;

    public AppShell()
    {
        InitializeComponent();
        
        // Регистрация маршрутов
        Routing.RegisterRoute("MainDashboard", typeof(MainDashboardPage));
        Routing.RegisterRoute("Courses", typeof(CoursesPage));
        Routing.RegisterRoute("Progress", typeof(ProgressPage));
        Routing.RegisterRoute("Profile", typeof(ProfilePage));
        Routing.RegisterRoute("TeacherDashboard", typeof(TeacherDashboardPage));
        Routing.RegisterRoute("Settings", typeof(SettingsPage));
        
        // Скрываем меню по умолчанию
        TeacherFlyoutItem.IsVisible = false;
        AdminFlyoutItem.IsVisible = false;
    }

    // Метод для настройки меню в зависимости от роли пользователя
    public void ConfigureMenuForUser(User user)
    {
        _currentUser = user;
        
        // Показываем/скрываем меню в зависимости от роли
        if (user.RoleId == 2 || user.RoleId == 1) // Учитель или Админ
        {
            TeacherFlyoutItem.IsVisible = true;
        }
        
        if (user.RoleId == 1) // Только Админ
        {
            AdminFlyoutItem.IsVisible = true;
        }
    }
    }
}
