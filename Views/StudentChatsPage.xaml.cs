using System.Collections.ObjectModel;
using EducationalPlatform.Models;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class StudentChatsPage : ContentPage
    {
        private readonly User _currentUser;
        private readonly DatabaseService _dbService;
        private readonly SettingsService _settingsService;

        public ObservableCollection<StudentGroupChatItem> Groups { get; set; } // Изменили на StudentGroupChatItem

        public StudentChatsPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            Groups = new ObservableCollection<StudentGroupChatItem>(); // Изменили на StudentGroupChatItem
            BindingContext = this;

            LoadStudentGroups();
        }

        private async void LoadStudentGroups()
        {
            try
            {
                var groups = await _dbService.GetStudentGroupChatsAsync(_currentUser.UserId);
                Groups.Clear();
                foreach (var group in groups)
                {
                    // Создаем новый объект с правильным типом
                    Groups.Add(new StudentGroupChatItem
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        CourseName = group.CourseName,
                        StudentCount = group.StudentCount
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить группы: {ex.Message}", "OK");
            }
        }

        private async void OnGroupSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is StudentGroupChatItem group)
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
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    public class StudentGroupChatItem
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}







