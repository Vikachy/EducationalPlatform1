using EducationalPlatform.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using EducationalPlatform.Services;

namespace EducationalPlatform.Views
{
    public partial class StudentSelectionPage : ContentPage
    {
        public ObservableCollection<StudentSelectionItem> Students { get; set; }
        public event EventHandler<List<User>> StudentsSelected;

        private int _groupId;
        private int _courseId;
        private DatabaseService _dbService;

        public StudentSelectionPage(List<StudentSelectionItem> students, string groupName, int groupId, int courseId, DatabaseService dbService)
        {
            InitializeComponent();
            Title = $"Выбор студентов: {groupName}";

            _groupId = groupId;
            _courseId = courseId;
            _dbService = dbService;

            Students = new ObservableCollection<StudentSelectionItem>(students);
            StudentsCollectionView.ItemsSource = Students;
            BindingContext = this;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var selectedStudents = Students
                    .Where(s => s.IsSelected)
                    .Select(s => s.Student)
                    .ToList();

                if (!selectedStudents.Any())
                {
                    await DisplayAlert("Внимание", "Выберите хотя бы одного студента", "OK");
                    return;
                }

                // Показываем индикатор загрузки
                IsBusy = true;

                // Сохраняем студентов в группу
                bool success = await _dbService.AddStudentsToGroupAsync(_groupId, selectedStudents);

                if (success)
                {
                    // Добавляем студентов в групповой чат
                    bool chatSuccess = await _dbService.AddStudentsToGroupChatAsync(_groupId, selectedStudents);

                    // Отправляем системное сообщение в чат группы
                    await _dbService.AddSystemMessageToGroupAsync(_groupId,
                        $"В группу добавлено {selectedStudents.Count} новых студентов");

                    // Вызываем событие для обновления UI в родительской странице
                    StudentsSelected?.Invoke(this, selectedStudents);

                    string message = chatSuccess ?
                        $"Добавлено {selectedStudents.Count} студентов в группу и чат" :
                        $"Добавлено {selectedStudents.Count} студентов в группу (ошибка добавления в чат)";

                    await DisplayAlert("Успех", message, "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось добавить студентов в группу", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка при сохранении: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        // Метод для быстрого выбора всех студентов
        private void OnSelectAllClicked(object sender, EventArgs e)
        {
            foreach (var student in Students)
            {
                student.IsSelected = true;
            }
            // Обновляем привязку
            StudentsCollectionView.ItemsSource = null;
            StudentsCollectionView.ItemsSource = Students;
        }

        // Метод для сброса выбора
        private void OnDeselectAllClicked(object sender, EventArgs e)
        {
            foreach (var student in Students)
            {
                student.IsSelected = false;
            }
            // Обновляем привязку
            StudentsCollectionView.ItemsSource = null;
            StudentsCollectionView.ItemsSource = Students;
        }
    }

    public class StudentSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public User Student { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}