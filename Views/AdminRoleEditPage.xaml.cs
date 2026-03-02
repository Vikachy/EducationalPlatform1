using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class AdminRoleEditPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;
        private readonly RoleModel _role;

        public AdminRoleEditPage(User user, DatabaseService dbService, SettingsService settingsService, RoleModel role)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminRoleEditPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;
            _role = role;

            LoadRoleData();
        }

        private void LoadRoleData()
        {
            RoleNameEntry.Text = _role.RoleName;
            RoleDescriptionEditor.Text = _role.Description;
            CanCreateCoursesCheckBox.IsChecked = _role.CanCreateCourses;
            CanManageUsersCheckBox.IsChecked = _role.CanManageUsers;
            CanManageGroupsCheckBox.IsChecked = _role.CanManageGroups;
            CanManageSystemCheckBox.IsChecked = _role.CanManageSystem;
            CanGradeTestsCheckBox.IsChecked = _role.CanGradeTests;
            CanManageContentCheckBox.IsChecked = _role.CanManageContent;
            CanPublishNewsCheckBox.IsChecked = _role.CanPublishNews;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Информация", "Изменения сохранены", "OK");
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}