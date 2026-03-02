using EducationalPlatform.Models;
using EducationalPlatform.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Dapper;

namespace EducationalPlatform.Views
{
    public partial class AdminRolesPage : ContentPage, INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<RoleModel> Roles { get; set; } = new();

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminRolesPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации AdminRolesPage: {ex.Message}");
            }

            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            BindingContext = this;
            Task.Run(async () => await LoadRolesAsync());
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_dbService.ConnectionString);
                await connection.OpenAsync();

                var roles = await connection.QueryAsync<RoleModel>(@"
                    SELECT r.*, 
                           (SELECT COUNT(*) FROM Users WHERE RoleId = r.RoleId) as UsersCount
                    FROM Roles r
                    ORDER BY r.RoleId
                ");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Roles.Clear();
                    foreach (var role in roles)
                    {
                        Roles.Add(role);
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                });
            }
        }

        private async void OnEditRoleClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RoleModel role)
            {
                await Navigation.PushAsync(new AdminRoleEditPage(_currentUser, _dbService, _settingsService, role));
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class RoleModel : Role
    {
        public int UsersCount { get; set; }
    }
}