using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Converters;

namespace EducationalPlatform.Views
{
    public partial class TeacherDashboardPage : ContentPage, INotifyPropertyChanged
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private SettingsService _settingsService;

        private int _totalCourses;
        private int _totalStudents;
        private double _averageRating;

        public int TotalCourses
        {
            get => _totalCourses;
            set
            {
                _totalCourses = value;
                OnPropertyChanged();
            }
        }

        public int TotalStudents
        {
            get => _totalStudents;
            set
            {
                _totalStudents = value;
                OnPropertyChanged();
            }
        }

        public double AverageRating
        {
            get => _averageRating;
            set
            {
                _averageRating = value;
                OnPropertyChanged();
            }
        }

        public TeacherDashboardPage(User user, DatabaseService dbService, SettingsService settingsService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            _settingsService = settingsService;

            // Конвертеры состояний
            Resources.Add("PublishedConverter", new PublishedToTextConverter());
            Resources.Add("StatusColorConverter", new StatusColorConverter());

            BindingContext = this;
            LoadTeacherData();
        }

        private void LoadTeacherData()
        {
            try
            {
                // Демо-данные курса (если БД недоступна)
                var courses = new List<TeacherCourse>
                {
                    new TeacherCourse
                    {
                        CourseId = 1,
                        CourseName = "Python для начинающих",
                        Description = "Базовый курс программирования на Python",
                        StudentCount = 25,
                        AverageRating = 4.7,
                        Groups = new List<StudyGroup> { new StudyGroup(), new StudyGroup() },
                        IsPublished = true
                    },
                    new TeacherCourse
                    {
                        CourseId = 2,
                        CourseName = "JavaScript основы",
                        Description = "Быстрый старт JavaScript",
                        StudentCount = 18,
                        AverageRating = 4.5,
                        Groups = new List<StudyGroup> { new StudyGroup() },
                        IsPublished = true
                    },
                    new TeacherCourse
                    {
                        CourseId = 3,
                        CourseName = "Java OOP",
                        Description = "Объектно-ориентированное программирование на Java",
                        StudentCount = 12,
                        AverageRating = 4.8,
                        Groups = new List<StudyGroup>(),
                        IsPublished = false
                    },
                    new TeacherCourse
                    {
                        CourseId = 4,
                        CourseName = "C++ для продвинутых",
                        Description = "Продвинутый курс C++",
                        StudentCount = 8,
                        AverageRating = 4.9,
                        Groups = new List<StudyGroup> { new StudyGroup(), new StudyGroup(), new StudyGroup() },
                        IsPublished = true
                    }
                };

                TotalCourses = courses.Count;
                TotalStudents = courses.Sum(c => c.StudentCount);
                AverageRating = courses.Average(c => c.AverageRating);

                CoursesCollectionView.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }

        private async void OnCreateCourseClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new TeacherManagementPage(_currentUser, _dbService, _settingsService));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть создание курса: {ex.Message}", "OK");
            }
        }

        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Отчеты", "Экспорт и аналитика", "OK");
        }

        private async void OnManageContentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    // Переход на страницу управления контентом курса
                    await Navigation.PushAsync(new TeacherContentManagementPage(
                        _currentUser, _dbService, _settingsService, course.CourseId, course.CourseName));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть управление контентом: {ex.Message}", "OK");
                }
            }
        }

        private async void OnManageCourseClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                await DisplayAlert("Управление курсом", $"Курс: {course.CourseName}", "OK");
            }
        }

        private async void OnManageGroupsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TeacherCourse course)
            {
                try
                {
                    await Navigation.PushAsync(new TeacherGroupsPage(_currentUser, _dbService, _settingsService, course));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть группы: {ex.Message}", "OK");
                }
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}