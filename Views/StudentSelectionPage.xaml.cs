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

            Console.WriteLine($"🎯 Создаем страницу выбора студентов для группы {groupId}");
            Console.WriteLine($"📊 Получено студентов для выбора: {students?.Count ?? 0}");

            if (students != null)
            {
                foreach (var student in students)
                {
                    Console.WriteLine($"   - {student.Student?.Username} (ID: {student.Student?.UserId})");
                }
            }

            Students = new ObservableCollection<StudentSelectionItem>(students ?? new List<StudentSelectionItem>());
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

                Console.WriteLine($"🎯 Выбрано студентов: {selectedStudents.Count}");
                foreach (var student in selectedStudents)
                {
                    Console.WriteLine($"   - {student.Username} (ID: {student.UserId})");
                }

                IsBusy = true;


                // 1. Сохраняем студентов в группу
                Console.WriteLine($"🔄 Сохраняем студентов в группу {_groupId}...");
                bool success = await _dbService.AddStudentsToGroupAsync(_groupId, selectedStudents);

                Console.WriteLine($"📝 Результат сохранения в группу: {success}");

                if (success)
                {
                    // 2. Добавляем студентов в таблицу участников чата
                    Console.WriteLine($"🔄 Добавляем студентов в участники чата группы {_groupId}...");
                    bool chatSuccess = await _dbService.AddStudentsToGroupChatAsync(_groupId, selectedStudents);
                    Console.WriteLine($"📝 Результат добавления в чат: {chatSuccess}");

                    // 3. Отправляем системное сообщение
                    await _dbService.AddSystemMessageToGroupAsync(_groupId,
                        $"В группу добавлено {selectedStudents.Count} новых студентов");

                    // 4. Обновляем UI через MainThread
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        StudentsSelected?.Invoke(this, selectedStudents);

                        string message = chatSuccess ?
                            $"✅ Успешно добавлено {selectedStudents.Count} студентов в группу и чат!" :
                            $"⚠️ Добавлено {selectedStudents.Count} студентов в группу (возможны проблемы с чатом)";

                        await DisplayAlert("Успех", message, "OK");

                        // Закрываем страницу
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Ошибка", "Не удалось добавить студентов в группу", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Ошибка", $"Ошибка при сохранении: {ex.Message}", "OK");
                });
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