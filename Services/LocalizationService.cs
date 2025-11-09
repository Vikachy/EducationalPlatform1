using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Services
{
    public class LocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations;
        private string _currentLanguage = "ru";
        private bool _isTeenStyle = false;

        public LocalizationService()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();
            InitializeTranslations();
        }

        private void InitializeTranslations()
        {
            // Русский язык - стандартный
            _translations["ru"] = new Dictionary<string, string>
            {
                // Общие
                {"welcome", "Добро пожаловать"},
                {"login", "Вход"},
                {"register", "Регистрация"},
                {"logout", "Выход"},
                {"save", "Сохранить"},
                {"cancel", "Отмена"},
                {"delete", "Удалить"},
                {"edit", "Редактировать"},
                {"add", "Добавить"},
                {"search", "Поиск"},
                {"filter", "Фильтр"},
                {"sort", "Сортировка"},
                {"loading", "Загрузка..."},
                {"error", "Ошибка"},
                {"success", "Успешно"},
                {"warning", "Предупреждение"},
                {"info", "Информация"},
                {"confirm", "Подтвердить"},
                {"yes", "Да"},
                {"no", "Нет"},
                {"ok", "OK"},
                {"close", "Закрыть"},
                {"back", "Назад"},
                {"next", "Далее"},
                {"previous", "Предыдущий"},
                {"finish", "Завершить"},
                {"start", "Начать"},
                {"continue", "Продолжить"},
                {"pause", "Пауза"},
                {"resume", "Возобновить"},
                {"retry", "Повторить"},
                {"refresh", "Обновить"},
                {"settings", "Настройки"},
                {"profile", "Профиль"},
                {"dashboard", "Панель управления"},
                {"courses", "Курсы"},
                {"progress", "Прогресс"},
                {"achievements", "Достижения"},
                {"shop", "Магазин"},
                {"support", "Поддержка"},
                {"news", "Новости"},
                {"contests", "Конкурсы"},
                {"groups", "Группы"},
                {"teachers", "Преподаватели"},
                {"students", "Студенты"},
                {"admin", "Администратор"},
                {"content_manager", "Контент-менеджер"},
                
                // Аутентификация
                {"username", "Логин"},
                {"password", "Пароль"},
                {"email", "Email"},
                {"first_name", "Имя"},
                {"last_name", "Фамилия"},
                {"confirm_password", "Подтвердите пароль"},
                {"forgot_password", "Забыли пароль?"},
                {"remember_me", "Запомнить меня"},
                {"login_success", "Вход выполнен успешно"},
                {"login_failed", "Неверный логин или пароль"},
                {"registration_success", "Регистрация выполнена успешно"},
                {"registration_failed", "Ошибка регистрации"},
                {"password_mismatch", "Пароли не совпадают"},
                {"username_exists", "Пользователь с таким логином уже существует"},
                {"email_exists", "Пользователь с таким email уже существует"},
                {"invalid_email", "Неверный формат email"},
                {"password_too_short", "Пароль должен содержать минимум 6 символов"},
                {"required_field", "Обязательное поле"},
                
                // Курсы
                {"course_name", "Название курса"},
                {"course_description", "Описание курса"},
                {"course_language", "Язык программирования"},
                {"course_difficulty", "Сложность"},
                {"course_duration", "Продолжительность"},
                {"course_price", "Цена"},
                {"course_rating", "Рейтинг"},
                {"course_reviews", "Отзывы"},
                {"enroll_course", "Записаться на курс"},
                {"start_course", "Начать курс"},
                {"continue_course", "Продолжить курс"},
                {"complete_course", "Завершить курс"},
                {"course_progress", "Прогресс курса"},
                {"course_completed", "Курс завершен"},
                {"course_certificate", "Сертификат"},
                {"course_materials", "Материалы курса"},
                {"course_videos", "Видео"},
                {"course_exercises", "Упражнения"},
                {"course_tests", "Тесты"},
                {"course_practice", "Практика"},
                {"course_theory", "Теория"},
                
                // Прогресс
                {"progress_overview", "Обзор прогресса"},
                {"completed_courses", "Завершенные курсы"},
                {"in_progress_courses", "Курсы в процессе"},
                {"total_time", "Общее время"},
                {"streak_days", "Дней подряд"},
                {"total_points", "Общие очки"},
                {"level", "Уровень"},
                {"experience", "Опыт"},
                {"rank", "Ранг"},
                {"badges", "Значки"},
                {"certificates", "Сертификаты"},
                
                // Магазин
                {"shop_items", "Товары"},
                {"my_inventory", "Мой инвентарь"},
                {"currency", "Валюта"},
                {"price", "Цена"},
                {"buy", "Купить"},
                {"purchase", "Покупка"},
                {"equip", "Надеть"},
                {"unequip", "Снять"},
                {"owned", "В собственности"},
                {"not_owned", "Не куплено"},
                {"insufficient_funds", "Недостаточно средств"},
                {"purchase_success", "Покупка выполнена успешно"},
                {"purchase_failed", "Ошибка покупки"},
                
                // Достижения
                {"achievement_unlocked", "Достижение разблокировано"},
                {"achievement_progress", "Прогресс достижения"},
                {"achievement_requirements", "Требования"},
                {"achievement_reward", "Награда"},
                {"achievement_date", "Дата получения"},
                {"achievement_rarity", "Редкость"},
                {"common", "Обычное"},
                {"rare", "Редкое"},
                {"epic", "Эпическое"},
                {"legendary", "Легендарное"},
                
                // Поддержка
                {"support_ticket", "Тикет поддержки"},
                {"ticket_subject", "Тема"},
                {"ticket_description", "Описание"},
                {"ticket_type", "Тип"},
                {"ticket_priority", "Приоритет"},
                {"ticket_status", "Статус"},
                {"ticket_created", "Создан"},
                {"ticket_updated", "Обновлен"},
                {"ticket_resolved", "Решен"},
                {"ticket_open", "Открыт"},
                {"ticket_closed", "Закрыт"},
                {"ticket_high", "Высокий"},
                {"ticket_medium", "Средний"},
                {"ticket_low", "Низкий"},
                {"ticket_bug", "Ошибка"},
                {"ticket_feature", "Функция"},
                {"ticket_question", "Вопрос"},
                {"submit_ticket", "Отправить тикет"},
                {"ticket_success", "Тикет отправлен успешно"},
                {"ticket_failed", "Ошибка отправки тикета"},
                
                // Настройки
                {"language", "Язык"},
                {"theme", "Тема"},
                {"notifications", "Уведомления"},
                {"privacy", "Конфиденциальность"},
                {"security", "Безопасность"},
                {"account", "Аккаунт"},
                {"preferences", "Предпочтения"},
                {"interface_style", "Стиль интерфейса"},
                {"standard", "Стандартный"},
                {"teen", "Подростковый"},
                {"dark_mode", "Темный режим"},
                {"light_mode", "Светлый режим"},
                {"auto_mode", "Автоматический"},
                
                // Ошибки
                {"network_error", "Ошибка сети"},
                {"server_error", "Ошибка сервера"},
                {"database_error", "Ошибка базы данных"},
                {"file_error", "Ошибка файла"},
                {"permission_error", "Ошибка прав доступа"},
                {"validation_error", "Ошибка валидации"},
                {"unknown_error", "Неизвестная ошибка"},
                {"try_again", "Попробуйте еще раз"},
                {"contact_support", "Обратитесь в поддержку"},
                
                // Успех
                {"operation_success", "Операция выполнена успешно"},
                {"data_saved", "Данные сохранены"},
                {"data_updated", "Данные обновлены"},
                {"data_deleted", "Данные удалены"},
                {"file_uploaded", "Файл загружен"},
                {"file_downloaded", "Файл скачан"},
                {"email_sent", "Email отправлен"},
                {"notification_sent", "Уведомление отправлено"},
                {"earned_date", "Дата получения"},
                {"go_to_course", "Перейти к курсу"},
                {"equipped", "Надето"},
                {"message_send_failed", "Не удалось отправить сообщение"}
            };

            // Русский язык - подростковый
            _translations["ru_teen"] = new Dictionary<string, string>
            {
                // Общие
                {"welcome", "Привет! 👋"},
                {"login", "Войти"},
                {"register", "Регистрация"},
                {"logout", "Выйти"},
                {"save", "Сохранить"},
                {"cancel", "Отмена"},
                {"delete", "Удалить"},
                {"edit", "Изменить"},
                {"add", "Добавить"},
                {"search", "Найти"},
                {"filter", "Фильтр"},
                {"sort", "Сортировка"},
                {"loading", "Загружаем... ⏳"},
                {"error", "Упс! 😅"},
                {"success", "Круто! 🎉"},
                {"warning", "Внимание! ⚠️"},
                {"info", "Инфо ℹ️"},
                {"confirm", "Точно?"},
                {"yes", "Да"},
                {"no", "Нет"},
                {"ok", "ОК"},
                {"close", "Закрыть"},
                {"back", "Назад"},
                {"next", "Далее"},
                {"previous", "Назад"},
                {"finish", "Готово!"},
                {"start", "Поехали! 🚀"},
                {"continue", "Продолжить"},
                {"pause", "Пауза"},
                {"resume", "Продолжить"},
                {"retry", "Еще раз"},
                {"refresh", "Обновить"},
                {"settings", "Настройки ⚙️"},
                {"profile", "Профиль 👤"},
                {"dashboard", "Главная 🏠"},
                {"courses", "Курсы 📚"},
                {"progress", "Прогресс 📊"},
                {"achievements", "Достижения 🏆"},
                {"shop", "Магазин 🛒"},
                {"support", "Помощь 💬"},
                {"news", "Новости 📰"},
                {"contests", "Конкурсы 🏅"},
                {"groups", "Группы 👥"},
                {"teachers", "Учителя 👨‍🏫"},
                {"students", "Студенты 👨‍🎓"},
                {"admin", "Админ 👑"},
                {"content_manager", "Контент-менеджер 📝"},
                
                // Аутентификация
                {"username", "Логин"},
                {"password", "Пароль"},
                {"email", "Email"},
                {"first_name", "Имя"},
                {"last_name", "Фамилия"},
                {"confirm_password", "Повтори пароль"},
                {"forgot_password", "Забыл пароль? 🤔"},
                {"remember_me", "Запомнить меня"},
                {"login_success", "Вошли! 🎉"},
                {"login_failed", "Неверный логин или пароль 😅"},
                {"registration_success", "Регистрация прошла успешно! 🎊"},
                {"registration_failed", "Ошибка регистрации 😔"},
                {"password_mismatch", "Пароли не совпадают 🤷‍♂️"},
                {"username_exists", "Такой логин уже есть 😅"},
                {"email_exists", "Такой email уже есть 😅"},
                {"invalid_email", "Неверный email 🤷‍♂️"},
                {"password_too_short", "Пароль слишком короткий 📏"},
                {"required_field", "Обязательное поле ⭐"},
                
                // Курсы
                {"course_name", "Название курса"},
                {"course_description", "Описание курса"},
                {"course_language", "Язык программирования"},
                {"course_difficulty", "Сложность"},
                {"course_duration", "Продолжительность"},
                {"course_price", "Цена"},
                {"course_rating", "Рейтинг ⭐"},
                {"course_reviews", "Отзывы 💬"},
                {"enroll_course", "Записаться! 📝"},
                {"start_course", "Начать курс! 🚀"},
                {"continue_course", "Продолжить курс"},
                {"complete_course", "Завершить курс"},
                {"course_progress", "Прогресс курса 📊"},
                {"course_completed", "Курс завершен! 🎉"},
                {"course_certificate", "Сертификат 🏆"},
                {"course_materials", "Материалы 📚"},
                {"course_videos", "Видео 🎥"},
                {"course_exercises", "Упражнения 💪"},
                {"course_tests", "Тесты 📝"},
                {"course_practice", "Практика 💻"},
                {"course_theory", "Теория 📖"},
                
                // Прогресс
                {"progress_overview", "Обзор прогресса 📊"},
                {"completed_courses", "Завершенные курсы ✅"},
                {"in_progress_courses", "Курсы в процессе 🔄"},
                {"total_time", "Общее время ⏰"},
                {"streak_days", "Дней подряд 🔥"},
                {"total_points", "Общие очки 🎯"},
                {"level", "Уровень 📈"},
                {"experience", "Опыт 💪"},
                {"rank", "Ранг 🏅"},
                {"badges", "Значки 🏆"},
                {"certificates", "Сертификаты 📜"},
                
                // Магазин
                {"shop_items", "Товары 🛒"},
                {"my_inventory", "Мой инвентарь 🎒"},
                {"currency", "Валюта 💰"},
                {"price", "Цена"},
                {"buy", "Купить"},
                {"purchase", "Покупка"},
                {"equip", "Надеть"},
                {"unequip", "Снять"},
                {"owned", "Есть ✅"},
                {"not_owned", "Нет ❌"},
                {"insufficient_funds", "Недостаточно денег 💸"},
                {"purchase_success", "Куплено! 🎉"},
                {"purchase_failed", "Ошибка покупки 😅"},
                
                // Достижения
                {"achievement_unlocked", "Достижение разблокировано! 🏆"},
                {"achievement_progress", "Прогресс достижения 📊"},
                {"achievement_requirements", "Требования 📋"},
                {"achievement_reward", "Награда 🎁"},
                {"achievement_date", "Дата получения 📅"},
                {"achievement_rarity", "Редкость 💎"},
                {"common", "Обычное ⚪"},
                {"rare", "Редкое 🔵"},
                {"epic", "Эпическое 🟣"},
                {"legendary", "Легендарное 🟡"},
                
                // Поддержка
                {"support_ticket", "Тикет поддержки 🎫"},
                {"ticket_subject", "Тема"},
                {"ticket_description", "Описание"},
                {"ticket_type", "Тип"},
                {"ticket_priority", "Приоритет"},
                {"ticket_status", "Статус"},
                {"ticket_created", "Создан"},
                {"ticket_updated", "Обновлен"},
                {"ticket_resolved", "Решен"},
                {"ticket_open", "Открыт"},
                {"ticket_closed", "Закрыт"},
                {"ticket_high", "Высокий 🔴"},
                {"ticket_medium", "Средний 🟡"},
                {"ticket_low", "Низкий 🟢"},
                {"ticket_bug", "Ошибка 🐛"},
                {"ticket_feature", "Функция 💡"},
                {"ticket_question", "Вопрос ❓"},
                {"submit_ticket", "Отправить тикет 📤"},
                {"ticket_success", "Тикет отправлен! 🎉"},
                {"ticket_failed", "Ошибка отправки 😅"},
                
                // Настройки
                {"language", "Язык 🌍"},
                {"theme", "Тема 🎨"},
                {"notifications", "Уведомления 🔔"},
                {"privacy", "Конфиденциальность 🔒"},
                {"security", "Безопасность 🛡️"},
                {"account", "Аккаунт 👤"},
                {"preferences", "Предпочтения ⚙️"},
                {"interface_style", "Стиль интерфейса 🎨"},
                {"standard", "Стандартный"},
                {"teen", "Подростковый 😎"},
                {"dark_mode", "Темный режим 🌙"},
                {"light_mode", "Светлый режим ☀️"},
                {"auto_mode", "Автоматический 🤖"},
                
                // Ошибки
                {"network_error", "Проблемы с сетью 🌐"},
                {"server_error", "Проблемы с сервером 🖥️"},
                {"database_error", "Проблемы с базой данных 🗄️"},
                {"file_error", "Проблемы с файлом 📁"},
                {"permission_error", "Нет прав доступа 🚫"},
                {"validation_error", "Ошибка проверки ✅"},
                {"unknown_error", "Неизвестная ошибка 🤷‍♂️"},
                {"try_again", "Попробуй еще раз 🔄"},
                {"contact_support", "Обратись в поддержку 💬"},
                
                // Успех
                {"operation_success", "Все получилось! 🎉"},
                {"data_saved", "Сохранено! 💾"},
                {"data_updated", "Обновлено! 🔄"},
                {"data_deleted", "Удалено! 🗑️"},
                {"file_uploaded", "Файл загружен! 📤"},
                {"file_downloaded", "Файл скачан! 📥"},
                {"email_sent", "Email отправлен! 📧"},
                {"notification_sent", "Уведомление отправлено! 🔔"},
                {"earned_date", "Дата получения"},
                {"go_to_course", "Перейти к курсу"},
                {"equipped", "Надето"},
                {"message_send_failed", "Не удалось отправить сообщение"}
            };

            // Английский язык - стандартный
            _translations["en"] = new Dictionary<string, string>
            {
                // Общие
                {"welcome", "Welcome"},
                {"login", "Login"},
                {"register", "Register"},
                {"logout", "Logout"},
                {"save", "Save"},
                {"cancel", "Cancel"},
                {"delete", "Delete"},
                {"edit", "Edit"},
                {"add", "Add"},
                {"search", "Search"},
                {"filter", "Filter"},
                {"sort", "Sort"},
                {"loading", "Loading..."},
                {"error", "Error"},
                {"success", "Success"},
                {"warning", "Warning"},
                {"info", "Info"},
                {"confirm", "Confirm"},
                {"yes", "Yes"},
                {"no", "No"},
                {"ok", "OK"},
                {"close", "Close"},
                {"back", "Back"},
                {"next", "Next"},
                {"previous", "Previous"},
                {"finish", "Finish"},
                {"start", "Start"},
                {"continue", "Continue"},
                {"pause", "Pause"},
                {"resume", "Resume"},
                {"retry", "Retry"},
                {"refresh", "Refresh"},
                {"settings", "Settings"},
                {"profile", "Profile"},
                {"dashboard", "Dashboard"},
                {"courses", "Courses"},
                {"progress", "Progress"},
                {"achievements", "Achievements"},
                {"shop", "Shop"},
                {"support", "Support"},
                {"news", "News"},
                {"contests", "Contests"},
                {"groups", "Groups"},
                {"teachers", "Teachers"},
                {"students", "Students"},
                {"admin", "Admin"},
                {"content_manager", "Content Manager"},
                
                // Аутентификация
                {"username", "Username"},
                {"password", "Password"},
                {"email", "Email"},
                {"first_name", "First Name"},
                {"last_name", "Last Name"},
                {"confirm_password", "Confirm Password"},
                {"forgot_password", "Forgot Password?"},
                {"remember_me", "Remember Me"},
                {"login_success", "Login successful"},
                {"login_failed", "Invalid username or password"},
                {"registration_success", "Registration successful"},
                {"registration_failed", "Registration failed"},
                {"password_mismatch", "Passwords do not match"},
                {"username_exists", "Username already exists"},
                {"email_exists", "Email already exists"},
                {"invalid_email", "Invalid email format"},
                {"password_too_short", "Password must be at least 6 characters"},
                {"required_field", "Required field"},
                
                // Курсы
                {"course_name", "Course Name"},
                {"course_description", "Course Description"},
                {"course_language", "Programming Language"},
                {"course_difficulty", "Difficulty"},
                {"course_duration", "Duration"},
                {"course_price", "Price"},
                {"course_rating", "Rating"},
                {"course_reviews", "Reviews"},
                {"enroll_course", "Enroll in Course"},
                {"start_course", "Start Course"},
                {"continue_course", "Continue Course"},
                {"complete_course", "Complete Course"},
                {"course_progress", "Course Progress"},
                {"course_completed", "Course Completed"},
                {"course_certificate", "Certificate"},
                {"course_materials", "Course Materials"},
                {"course_videos", "Videos"},
                {"course_exercises", "Exercises"},
                {"course_tests", "Tests"},
                {"course_practice", "Practice"},
                {"course_theory", "Theory"},
                
                // Прогресс
                {"progress_overview", "Progress Overview"},
                {"completed_courses", "Completed Courses"},
                {"in_progress_courses", "In Progress Courses"},
                {"total_time", "Total Time"},
                {"streak_days", "Streak Days"},
                {"total_points", "Total Points"},
                {"level", "Level"},
                {"experience", "Experience"},
                {"rank", "Rank"},
                {"badges", "Badges"},
                {"certificates", "Certificates"},
                
                // Магазин
                {"shop_items", "Shop Items"},
                {"my_inventory", "My Inventory"},
                {"currency", "Currency"},
                {"price", "Price"},
                {"buy", "Buy"},
                {"purchase", "Purchase"},
                {"equip", "Equip"},
                {"unequip", "Unequip"},
                {"owned", "Owned"},
                {"not_owned", "Not Owned"},
                {"insufficient_funds", "Insufficient funds"},
                {"purchase_success", "Purchase successful"},
                {"purchase_failed", "Purchase failed"},
                
                // Достижения
                {"achievement_unlocked", "Achievement Unlocked"},
                {"achievement_progress", "Achievement Progress"},
                {"achievement_requirements", "Requirements"},
                {"achievement_reward", "Reward"},
                {"achievement_date", "Date Earned"},
                {"achievement_rarity", "Rarity"},
                {"common", "Common"},
                {"rare", "Rare"},
                {"epic", "Epic"},
                {"legendary", "Legendary"},
                
                // Поддержка
                {"support_ticket", "Support Ticket"},
                {"ticket_subject", "Subject"},
                {"ticket_description", "Description"},
                {"ticket_type", "Type"},
                {"ticket_priority", "Priority"},
                {"ticket_status", "Status"},
                {"ticket_created", "Created"},
                {"ticket_updated", "Updated"},
                {"ticket_resolved", "Resolved"},
                {"ticket_open", "Open"},
                {"ticket_closed", "Closed"},
                {"ticket_high", "High"},
                {"ticket_medium", "Medium"},
                {"ticket_low", "Low"},
                {"ticket_bug", "Bug"},
                {"ticket_feature", "Feature"},
                {"ticket_question", "Question"},
                {"submit_ticket", "Submit Ticket"},
                {"ticket_success", "Ticket submitted successfully"},
                {"ticket_failed", "Failed to submit ticket"},
                
                // Настройки
                {"language", "Language"},
                {"theme", "Theme"},
                {"notifications", "Notifications"},
                {"privacy", "Privacy"},
                {"security", "Security"},
                {"account", "Account"},
                {"preferences", "Preferences"},
                {"interface_style", "Interface Style"},
                {"standard", "Standard"},
                {"teen", "Teen"},
                {"dark_mode", "Dark Mode"},
                {"light_mode", "Light Mode"},
                {"auto_mode", "Auto Mode"},
                
                // Ошибки
                {"network_error", "Network error"},
                {"server_error", "Server error"},
                {"database_error", "Database error"},
                {"file_error", "File error"},
                {"permission_error", "Permission error"},
                {"validation_error", "Validation error"},
                {"unknown_error", "Unknown error"},
                {"try_again", "Try again"},
                {"contact_support", "Contact support"},
                
                // Успех
                {"operation_success", "Operation successful"},
                {"data_saved", "Data saved"},
                {"data_updated", "Data updated"},
                {"data_deleted", "Data deleted"},
                {"file_uploaded", "File uploaded"},
                {"file_downloaded", "File downloaded"},
                {"email_sent", "Email sent"},
                {"notification_sent", "Notification sent"},
                {"earned_date", "Earned Date"},
                {"go_to_course", "Go to Course"},
                {"equipped", "Equipped"},
                {"message_send_failed", "Failed to send message"}
            };

            // Английский язык - подростковый
            _translations["en_teen"] = new Dictionary<string, string>
            {
                // Общие
                {"welcome", "Hey! 👋"},
                {"login", "Login"},
                {"register", "Join"},
                {"logout", "Logout"},
                {"save", "Save"},
                {"cancel", "Cancel"},
                {"delete", "Delete"},
                {"edit", "Edit"},
                {"add", "Add"},
                {"search", "Search"},
                {"filter", "Filter"},
                {"sort", "Sort"},
                {"loading", "Loading... ⏳"},
                {"error", "Oops! 😅"},
                {"success", "Awesome! 🎉"},
                {"warning", "Warning! ⚠️"},
                {"info", "Info ℹ️"},
                {"confirm", "Sure?"},
                {"yes", "Yes"},
                {"no", "No"},
                {"ok", "OK"},
                {"close", "Close"},
                {"back", "Back"},
                {"next", "Next"},
                {"previous", "Previous"},
                {"finish", "Done!"},
                {"start", "Let's go! 🚀"},
                {"continue", "Continue"},
                {"pause", "Pause"},
                {"resume", "Resume"},
                {"retry", "Try again"},
                {"refresh", "Refresh"},
                {"settings", "Settings ⚙️"},
                {"profile", "Profile 👤"},
                {"dashboard", "Home 🏠"},
                {"courses", "Courses 📚"},
                {"progress", "Progress 📊"},
                {"achievements", "Achievements 🏆"},
                {"shop", "Shop 🛒"},
                {"support", "Help 💬"},
                {"news", "News 📰"},
                {"contests", "Contests 🏅"},
                {"groups", "Groups 👥"},
                {"teachers", "Teachers 👨‍🏫"},
                {"students", "Students 👨‍🎓"},
                {"admin", "Admin 👑"},
                {"content_manager", "Content Manager 📝"},
                
                // Аутентификация
                {"username", "Username"},
                {"password", "Password"},
                {"email", "Email"},
                {"first_name", "First Name"},
                {"last_name", "Last Name"},
                {"confirm_password", "Confirm Password"},
                {"forgot_password", "Forgot Password? 🤔"},
                {"remember_me", "Remember Me"},
                {"login_success", "Logged in! 🎉"},
                {"login_failed", "Wrong username or password 😅"},
                {"registration_success", "Registration successful! 🎊"},
                {"registration_failed", "Registration failed 😔"},
                {"password_mismatch", "Passwords don't match 🤷‍♂️"},
                {"username_exists", "Username already exists 😅"},
                {"email_exists", "Email already exists 😅"},
                {"invalid_email", "Invalid email 🤷‍♂️"},
                {"password_too_short", "Password too short 📏"},
                {"required_field", "Required field ⭐"},
                
                // Курсы
                {"course_name", "Course Name"},
                {"course_description", "Course Description"},
                {"course_language", "Programming Language"},
                {"course_difficulty", "Difficulty"},
                {"course_duration", "Duration"},
                {"course_price", "Price"},
                {"course_rating", "Rating ⭐"},
                {"course_reviews", "Reviews 💬"},
                {"enroll_course", "Enroll! 📝"},
                {"start_course", "Start Course! 🚀"},
                {"continue_course", "Continue Course"},
                {"complete_course", "Complete Course"},
                {"course_progress", "Course Progress 📊"},
                {"course_completed", "Course Completed! 🎉"},
                {"course_certificate", "Certificate 🏆"},
                {"course_materials", "Materials 📚"},
                {"course_videos", "Videos 🎥"},
                {"course_exercises", "Exercises 💪"},
                {"course_tests", "Tests 📝"},
                {"course_practice", "Practice 💻"},
                {"course_theory", "Theory 📖"},
                
                // Прогресс
                {"progress_overview", "Progress Overview 📊"},
                {"completed_courses", "Completed Courses ✅"},
                {"in_progress_courses", "In Progress Courses 🔄"},
                {"total_time", "Total Time ⏰"},
                {"streak_days", "Streak Days 🔥"},
                {"total_points", "Total Points 🎯"},
                {"level", "Level 📈"},
                {"experience", "Experience 💪"},
                {"rank", "Rank 🏅"},
                {"badges", "Badges 🏆"},
                {"certificates", "Certificates 📜"},
                
                // Магазин
                {"shop_items", "Shop Items 🛒"},
                {"my_inventory", "My Inventory 🎒"},
                {"currency", "Currency 💰"},
                {"price", "Price"},
                {"buy", "Buy"},
                {"purchase", "Purchase"},
                {"equip", "Equip"},
                {"unequip", "Unequip"},
                {"owned", "Owned ✅"},
                {"not_owned", "Not Owned ❌"},
                {"insufficient_funds", "Not enough money 💸"},
                {"purchase_success", "Purchased! 🎉"},
                {"purchase_failed", "Purchase failed 😅"},
                
                // Достижения
                {"achievement_unlocked", "Achievement Unlocked! 🏆"},
                {"achievement_progress", "Achievement Progress 📊"},
                {"achievement_requirements", "Requirements 📋"},
                {"achievement_reward", "Reward 🎁"},
                {"achievement_date", "Date Earned 📅"},
                {"achievement_rarity", "Rarity 💎"},
                {"common", "Common ⚪"},
                {"rare", "Rare 🔵"},
                {"epic", "Epic 🟣"},
                {"legendary", "Legendary 🟡"},
                
                // Поддержка
                {"support_ticket", "Support Ticket 🎫"},
                {"ticket_subject", "Subject"},
                {"ticket_description", "Description"},
                {"ticket_type", "Type"},
                {"ticket_priority", "Priority"},
                {"ticket_status", "Status"},
                {"ticket_created", "Created"},
                {"ticket_updated", "Updated"},
                {"ticket_resolved", "Resolved"},
                {"ticket_open", "Open"},
                {"ticket_closed", "Closed"},
                {"ticket_high", "High 🔴"},
                {"ticket_medium", "Medium 🟡"},
                {"ticket_low", "Low 🟢"},
                {"ticket_bug", "Bug 🐛"},
                {"ticket_feature", "Feature 💡"},
                {"ticket_question", "Question ❓"},
                {"submit_ticket", "Submit Ticket 📤"},
                {"ticket_success", "Ticket submitted! 🎉"},
                {"ticket_failed", "Failed to submit 😅"},
                
                // Настройки
                {"language", "Language 🌍"},
                {"theme", "Theme 🎨"},
                {"notifications", "Notifications 🔔"},
                {"privacy", "Privacy 🔒"},
                {"security", "Security 🛡️"},
                {"account", "Account 👤"},
                {"preferences", "Preferences ⚙️"},
                {"interface_style", "Interface Style 🎨"},
                {"standard", "Standard"},
                {"teen", "Teen 😎"},
                {"dark_mode", "Dark Mode 🌙"},
                {"light_mode", "Light Mode ☀️"},
                {"auto_mode", "Auto Mode 🤖"},
                
                // Ошибки
                {"network_error", "Network issues 🌐"},
                {"server_error", "Server issues 🖥️"},
                {"database_error", "Database issues 🗄️"},
                {"file_error", "File issues 📁"},
                {"permission_error", "No permission 🚫"},
                {"validation_error", "Validation error ✅"},
                {"unknown_error", "Unknown error 🤷‍♂️"},
                {"try_again", "Try again 🔄"},
                {"contact_support", "Contact support 💬"},
                
                // Успех
                {"operation_success", "All good! 🎉"},
                {"data_saved", "Saved! 💾"},
                {"data_updated", "Updated! 🔄"},
                {"data_deleted", "Deleted! 🗑️"},
                {"file_uploaded", "File uploaded! 📤"},
                {"file_downloaded", "File downloaded! 📥"},
                {"email_sent", "Email sent! 📧"},
                {"notification_sent", "Notification sent! 🔔"},
                {"earned_date", "Earned Date"},
                {"go_to_course", "Go to Course"},
                {"equipped", "Equipped"},
                {"message_send_failed", "Failed to send message"}
            };
        }

        public void SetLanguage(string language)
        {
            _currentLanguage = language;
        }

        public void SetTeenStyle(bool isTeenStyle)
        {
            _isTeenStyle = isTeenStyle;
        }

        public string GetText(string key)
        {
            var languageKey = _isTeenStyle ? $"{_currentLanguage}_teen" : _currentLanguage;
            
            if (_translations.ContainsKey(languageKey) && _translations[languageKey].ContainsKey(key))
            {
                return _translations[languageKey][key];
            }
            
            // Fallback to standard language
            if (_translations.ContainsKey(_currentLanguage) && _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            
            // Fallback to key itself
            return key;
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        public bool IsTeenStyle()
        {
            return _isTeenStyle;
        }

        public List<string> GetAvailableLanguages()
        {
            return new List<string> { "ru", "en" };
        }

        public List<string> GetAvailableStyles()
        {
            return new List<string> { "standard", "teen" };
        }
    }
}