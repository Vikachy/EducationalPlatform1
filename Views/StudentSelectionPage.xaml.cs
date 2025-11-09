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

                // ДИАГНОСТИКА: Проверим состояние до добавления
                Console.WriteLine($"🔍 ДИАГНОСТИКА ПЕРЕД ДОБАВЛЕНИЕМ:");
                await _dbService.DebugGroupState(_groupId);

                // ПРОСТОЙ МЕТОД - добавляем каждого студента отдельно
                int successCount = 0;
                foreach (var student in selectedStudents)
                {
                    Console.WriteLine($"\n🔄 Добавляем студента {student.Username} (ID: {student.UserId})...");

                    // Проверяем, состоит ли студент уже в группе
                    bool isInGroup = await _dbService.IsStudentInGroupAsync(student.UserId, _groupId);
                    Console.WriteLine($"📊 Студент уже в группе: {isInGroup}");

                    if (!isInGroup)
                    {
                        // Добавляем в группу
                        bool enrollResult = await _dbService.EnrollStudentToGroupAsync(_groupId, student.UserId);
                        Console.WriteLine($"📝 Результат добавления в группу: {enrollResult}");

                        if (enrollResult)
                        {
                            // Добавляем в чат
                            bool chatResult = await _dbService.SimpleAddToGroupChat(_groupId, student.UserId);
                            Console.WriteLine($"💬 Результат добавления в чат: {chatResult}");

                            if (chatResult)
                            {
                                successCount++;
                                Console.WriteLine($"✅ Студент {student.Username} успешно добавлен");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ℹ️ Студент {student.Username} уже в группе, пропускаем");
                        successCount++;
                    }
                }

                // Отправляем системное сообщение если кто-то добавлен
                if (successCount > 0)
                {
                    await _dbService.AddSystemMessageToGroupAsync(_groupId,
                        $"🎉 В группу добавлено {successCount} новых студентов!");
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (successCount > 0)
                    {
                        StudentsSelected?.Invoke(this, selectedStudents);
                        await DisplayAlert("Успех",
                            $"✅ Добавлено {successCount} студентов в группу и чат!", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Ошибка",
                            "Не удалось добавить студентов. Проверьте консоль для деталей.", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ОШИБКА В UI: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}