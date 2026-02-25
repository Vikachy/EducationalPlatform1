// Services/LocalizationService.cs
using Microsoft.Maui.Storage;
using System.Globalization;

namespace EducationalPlatform.Services
{
    public class LocalizationService
    {
        private const string LANGUAGE_KEY = "AppLanguage";
        private const string DEFAULT_LANGUAGE = "ru";

        public event EventHandler<string>? LanguageChanged;

        private Dictionary<string, Dictionary<string, string>> _strings;
        private string _currentLanguage;
        private SettingsService? _settingsService;

        public LocalizationService()
        {
            InitializeStrings();
            _currentLanguage = Preferences.Get(LANGUAGE_KEY, DEFAULT_LANGUAGE);
            Console.WriteLine($"LocalizationService создан, язык: {_currentLanguage}");
        }

        public void Initialize(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private void InitializeStrings()
        {
            _strings = new Dictionary<string, Dictionary<string, string>>
            {
                ["ru"] = new Dictionary<string, string>(),
                ["en"] = new Dictionary<string, string>()
            };

            // Русские строки
            _strings["ru"]["Settings"] = "Настройки";
            _strings["ru"]["Language"] = "Язык";
            _strings["ru"]["Theme"] = "Тема";
            _strings["ru"]["Standard"] = "Стандартная";
            _strings["ru"]["Teen"] = "Для подростков";
            _strings["ru"]["Current"] = "Текущий";
            _strings["ru"]["Russian"] = "Русский";
            _strings["ru"]["English"] = "Английский";
            _strings["ru"]["Save"] = "Сохранить";
            _strings["ru"]["Back"] = "Назад";
            _strings["ru"]["Success"] = "Успех";
            _strings["ru"]["OK"] = "OK";
            _strings["ru"]["Error"] = "Ошибка";
            _strings["ru"]["MyActiveCourses"] = "Мои активные курсы";
            _strings["ru"]["TodayTasks"] = "Сегодняшние задачи";
            _strings["ru"]["LatestNews"] = "Последние новости";
            _strings["ru"]["QuickAccess"] = "Быстрый доступ";
            _strings["ru"]["AllCourses"] = "Все курсы";
            _strings["ru"]["MyCourses"] = "Мои курсы";
            _strings["ru"]["Progress"] = "Прогресс";
            _strings["ru"]["Achievements"] = "Достижения";
            _strings["ru"]["Shop"] = "Магазин";
            _strings["ru"]["Streak"] = "Серия";
            _strings["ru"]["Currency"] = "Монеты";
            _strings["ru"]["User"] = "Пользователь";
            _strings["ru"]["Appearance"] = "Внешний вид";
            _strings["ru"]["EmailNotifications"] = "Email уведомления";
            _strings["ru"]["PushNotifications"] = "Push уведомления";
            _strings["ru"]["Notifications"] = "Уведомления";
            _strings["ru"]["ThemeChanged"] = "Тема успешно изменена!";
            _strings["ru"]["SettingsSaved"] = "Настройки сохранены!";
            _strings["ru"]["SaveFailed"] = "Не удалось сохранить настройки";
            _strings["ru"]["AvatarFrames"] = "Рамки профиля";
            _strings["ru"]["ProfileEmojis"] = "Эмодзи для профиля";
            _strings["ru"]["Themes"] = "Темы оформления";
            _strings["ru"]["Badges"] = "Значки";
            _strings["ru"]["MyInventory"] = "Мой инвентарь";
            _strings["ru"]["EarnCoins"] = "Получить монеты";
            _strings["ru"]["Loading"] = "Загрузка...";
            _strings["ru"]["Buy"] = "Купить";
            _strings["ru"]["Equip"] = "Надеть";
            _strings["ru"]["Equipped"] = "Надето";
            _strings["ru"]["NotEnoughCoins"] = "Недостаточно средств";
            _strings["ru"]["NeedMoreCoins"] = "Вам нужно еще {0} монет 🪙";
            _strings["ru"]["ItemPurchased"] = "Товар \"{0}\" приобретен!";
            _strings["ru"]["ItemEquipped"] = "\"{0}\" теперь активно!";
            _strings["ru"]["ItemUnequipped"] = "\"{0}\" снято";
            _strings["ru"]["ThemeApplied"] = "Тема \"{0}\" применена!";
            _strings["ru"]["FailedToLoadItems"] = "Не удалось загрузить товары";
            _strings["ru"]["OperationError"] = "Ошибка операции";
            _strings["ru"]["FailedToOpenInventory"] = "Не удалось открыть инвентарь";
            _strings["ru"]["CoinsAdded"] = "+50 монет начислено!";
            _strings["ru"]["FailedToAddCoins"] = "Не удалось начислить монеты";
            _strings["ru"]["PurchaseFailed"] = "Не удалось совершить покупку";
            _strings["ru"]["Confirmation"] = "Подтверждение";
            _strings["ru"]["Yes"] = "Да";
            _strings["ru"]["No"] = "Нет";
            _strings["ru"]["Login"] = "Вход";
            _strings["ru"]["Register"] = "Регистрация";
            _strings["ru"]["ForgotPassword"] = "Забыли пароль?";
            _strings["ru"]["Username"] = "Имя пользователя";
            _strings["ru"]["Password"] = "Пароль";
            _strings["ru"]["CaptchaCode"] = "Код с картинки";
            _strings["ru"]["AppName"] = "Образовательная платформа";
            _strings["ru"]["CaptchaInstruction"] = "Введите код с картинки";
            _strings["ru"]["PasswordRecovery"] = "Восстановление пароля";
            _strings["ru"]["EnterLoginOrEmail"] = "Введите логин или email";
            _strings["ru"]["Continue"] = "Продолжить";
            _strings["ru"]["Cancel"] = "Отмена";
            _strings["ru"]["SendingRequest"] = "Отправка...";
            _strings["ru"]["UserNotFound"] = "Пользователь не найден";
            _strings["ru"]["CodeCreationFailed"] = "Не удалось создать код восстановления";
            _strings["ru"]["CodeSentToEmail"] = "Код восстановления отправлен на {0}";
            _strings["ru"]["Confirmation"] = "Подтверждение";
            _strings["ru"]["EnterSixDigitCode"] = "Введите 6-значный код из письма";
            _strings["ru"]["Confirm"] = "Подтвердить";
            _strings["ru"]["InvalidOrExpiredCode"] = "Неверный или устаревший код";
            _strings["ru"]["FailedToSendEmail"] = "Не удалось отправить email";
            _strings["ru"]["EnterLoginAndPassword"] = "Введите логин и пароль";
            _strings["ru"]["InvalidCaptcha"] = "Неверный код с картинки";
            _strings["ru"]["InvalidCredentials"] = "Неверный логин или пароль";
            _strings["ru"]["InvalidCredentialsWithCaptcha"] = "Неверный логин или пароль. Попробуйте еще раз.";
            _strings["ru"]["ConfirmExit"] = "Вы действительно хотите выйти из приложения?";
            _strings["ru"]["LearningProgress"] = "Прогресс обучения";
            _strings["ru"]["OverallProgress"] = "Общий прогресс";
            _strings["ru"]["CompletedCourses"] = "Завершенные курсы";
            _strings["ru"]["AverageScore"] = "Средний балл";
            _strings["ru"]["CompletionRate"] = "Процент выполнения";
            _strings["ru"]["CurrentStreak"] = "Текущая серия";
            _strings["ru"]["TotalDays"] = "Всего дней";
            _strings["ru"]["RecentAchievements"] = "Последние достижения";
            _strings["ru"]["CourseProgress"] = "Прогресс по курсам";
            _strings["ru"]["NoAchievements"] = "Нет достижений";
            _strings["ru"]["NoCourses"] = "Нет курсов";
            _strings["ru"]["BackToMain"] = "На главную";
            _strings["ru"]["Course"] = "Курс";
            _strings["ru"]["ContinueCourse"] = "Продолжить";
            _strings["ru"]["LanguageChanged"] = "Язык успешно изменен!";
            _strings["ru"]["LanguageChanged"] = "Язык успешно изменен!";
            _strings["ru"]["ThemeChanged"] = "Тема успешно изменена!";
            _strings["ru"]["SettingsSaved"] = "Настройки сохранены!";
            _strings["ru"]["Contests"] = "Конкурсы";
            _strings["ru"]["ProgrammingContests"] = "Конкурсы программирования";
            _strings["ru"]["ActiveContests"] = "Активные конкурсы";
            _strings["ru"]["CompletedContests"] = "Завершенные конкурсы";
            _strings["ru"]["MySubmissions"] = "Мои заявки";
            _strings["ru"]["CreateContest"] = "Создать конкурс";
            _strings["ru"]["SubmitProject"] = "Подать проект";
            _strings["ru"]["ContestName"] = "Название конкурса";
            _strings["ru"]["ContestDescription"] = "Описание конкурса";
            _strings["ru"]["SelectLanguage"] = "Выберите язык";
            _strings["ru"]["AnyLanguage"] = "Любой язык";
            _strings["ru"]["StartDate"] = "Дата начала";
            _strings["ru"]["EndDate"] = "Дата окончания";
            _strings["ru"]["PrizeCoins"] = "Призовые монеты";
            _strings["ru"]["MaxParticipants"] = "Макс. участников";
            _strings["ru"]["OnlyForGroups"] = "Только для учебных групп";
            _strings["ru"]["Create"] = "Создать";
            _strings["ru"]["EnterContestName"] = "Введите название конкурса";
            _strings["ru"]["EndDateMustBeAfterStart"] = "Дата окончания должна быть позже даты начала";
            _strings["ru"]["ContestCreated"] = "Конкурс успешно создан!";
            _strings["ru"]["FailedToCreateContest"] = "Не удалось создать конкурс";
            _strings["ru"]["ContestResults"] = "Результаты конкурса";
            _strings["ru"]["ContestSubmissions"] = "Работы участников";
            _strings["ru"]["NoSubmissions"] = "Нет отправленных работ";
            _strings["ru"]["FailedToLoadSubmissions"] = "Не удалось загрузить работы";
            _strings["ru"]["FileNotFound"] = "Файл не найден";
            _strings["ru"]["FileSaved"] = "Файл сохранен";
            _strings["ru"]["FileDownloaded"] = "Файл скачан";
            _strings["ru"]["FailedToDownloadFile"] = "Не удалось скачать файл";
            _strings["ru"]["InvalidFilePath"] = "Неверный путь к файлу";
            _strings["ru"]["OnlyTeachersCanGrade"] = "Оценивать могут только преподаватели";
            _strings["ru"]["GradingUnavailable"] = "Оценка недоступна";
            _strings["ru"]["GradingAvailableAfter"] = "Оценка будет доступна после {0}";
            _strings["ru"]["Grade"] = "Оценка";
            _strings["ru"]["EnterScore"] = "Введите балл (0-100)";
            _strings["ru"]["Comment"] = "Комментарий";
            _strings["ru"]["TeacherComments"] = "Комментарий преподавателя";
            _strings["ru"]["GradeSaved"] = "Оценка сохранена";
            _strings["ru"]["FailedToSaveGrade"] = "Не удалось сохранить оценку";
            _strings["ru"]["ScoreMustBeNumber"] = "Оценка должна быть числом от 0 до 100";
            _strings["ru"]["ProjectName"] = "Название проекта";
            _strings["ru"]["ProjectDescription"] = "Описание проекта";
            _strings["ru"]["SelectZipArchive"] = "Выберите ZIP архив";
            _strings["ru"]["NoFileSelected"] = "Файл не выбран";
            _strings["ru"]["FailedToSelectFile"] = "Не удалось выбрать файл";
            _strings["ru"]["EnterProjectName"] = "Введите название проекта";
            _strings["ru"]["SelectZipFile"] = "Выберите ZIP файл";
            _strings["ru"]["ProjectSubmitted"] = "Проект отправлен!";
            _strings["ru"]["FailedToSubmitProject"] = "Не удалось отправить проект";
            _strings["ru"]["SubmissionError"] = "Ошибка отправки";
            _strings["ru"]["AccessDenied"] = "Доступ запрещен";
            _strings["ru"]["OnlyStudentsCanParticipate"] = "Участвовать могут только студенты";
            _strings["ru"]["AccessRestricted"] = "Доступ ограничен";
            _strings["ru"]["ContestOnlyForGroups"] = "Этот конкурс доступен только участникам учебных групп";
            _strings["ru"]["FailedToLoadContests"] = "Не удалось загрузить конкурсы";
            _strings["ru"]["NoActiveContests"] = "Нет активных конкурсов для участия";
            _strings["ru"]["SelectContest"] = "Выберите конкурс";
            _strings["ru"]["Cancel"] = "Отмена";
            _strings["ru"]["OnlyTeachersCanEditContests"] = "Только преподаватели могут редактировать конкурсы";
            _strings["ru"]["OnlyTeachersCanDeleteContests"] = "Только преподаватели могут удалять конкурсы";
            _strings["ru"]["ConfirmDeleteContest"] = "Вы уверены, что хотите удалить этот конкурс? Все связанные заявки также будут удалены.";
            _strings["ru"]["ContestDeleted"] = "Конкурс успешно удален";
            _strings["ru"]["EditContest"] = "Редактирование конкурса";
            _strings["ru"]["TotalSubmissions"] = "Всего заявок";
            _strings["ru"]["EditContest"] = "Редактирование конкурса";
            _strings["ru"]["ContestActive"] = "Конкурс активен";
            _strings["ru"]["CreatedBy"] = "Создатель:";
            _strings["ru"]["CreatedDateFormat"] = "Создан: {0}";
            _strings["ru"]["Statistics"] = "Статистика";
            _strings["ru"]["DeleteContest"] = "Удалить конкурс";
            _strings["ru"]["TotalSubmissionsFormat"] = "Всего заявок: {0}";
            _strings["ru"]["GradedSubmissionsFormat"] = "Оценено: {0} | Ожидает: {1}";
            _strings["ru"]["ContestUpdated"] = "Конкурс успешно обновлен";
            _strings["ru"]["FailedToUpdateContest"] = "Не удалось обновить конкурс";
            _strings["ru"]["ContestNotFound"] = "Конкурс не найден";
            _strings["ru"]["NoEditRights"] = "У вас нет прав на редактирование этого конкурса";
            _strings["ru"]["OnlyCreatorCanEdit"] = "Только создатель конкурса может его редактировать";
            _strings["ru"]["News"] = "Новости";
            _strings["ru"]["PlatformNews"] = "Новости платформы";
            _strings["ru"]["SearchNews"] = "Поиск новостей...";
            _strings["ru"]["All"] = "Все";
            _strings["ru"]["Courses"] = " Курсы";
            _strings["ru"]["Contests"] = " Конкурсы";
            _strings["ru"]["System"] = " Система";
            _strings["ru"]["General"] = " Общее";
            _strings["ru"]["CreateNews"] = "Создать новость";
            _strings["ru"]["EditNews"] = "Редактировать новость";
            _strings["ru"]["DeleteNews"] = "Удалить новость";
            _strings["ru"]["NewsTitle"] = "Заголовок новости";
            _strings["ru"]["NewsSummary"] = "Краткое описание";
            _strings["ru"]["NewsContent"] = "Полный текст новости";
            _strings["ru"]["ForTeens"] = "Для подростков";
            _strings["ru"]["ImageUrl"] = "URL изображения";
            _strings["ru"]["Creating"] = "Создание...";
            _strings["ru"]["EnterTitle"] = "Введите заголовок новости";
            _strings["ru"]["EnterContent"] = "Введите текст новости";
            _strings["ru"]["NewsCreated"] = "Новость успешно создана";
            _strings["ru"]["FailedToCreateNews"] = "Не удалось создать новость";
            _strings["ru"]["ConfirmDeleteNews"] = "Вы уверены, что хотите удалить эту новость?";
            _strings["ru"]["NewsDeleted"] = "Новость удалена";
            _strings["ru"]["FailedToDeleteNews"] = "Не удалось удалить новость";
            _strings["ru"]["FailedToLoadNews"] = "Ошибка загрузки новостей";
            _strings["ru"]["Deleting"] = "Удаление...";
            _strings["ru"]["EditNews"] = "Редактирование новости";
            _strings["ru"]["NewsActive"] = "Новость активна";
            _strings["ru"]["NewsUpdated"] = "Новость успешно обновлена";
            _strings["ru"]["FailedToUpdateNews"] = "Не удалось обновить новость";
            _strings["ru"]["Saving"] = "Сохранение...";
            _strings["ru"]["AttachFile"] = "Прикрепить файл";
            _strings["ru"]["SelectPhoto"] = "Выберите фото";
            _strings["ru"]["SelectVideo"] = "Выберите видео";
            _strings["ru"]["SelectDocument"] = "Выберите документ";
            _strings["ru"]["SelectArchive"] = "Выберите архив";
            _strings["ru"]["ChooseEmoji"] = "Выберите эмодзи";
            _strings["ru"]["Download"] = "Скачать";
            _strings["ru"]["Open"] = "Открыть";
            _strings["ru"]["Info"] = "Информация";
            _strings["ru"]["FileName"] = "Имя";
            _strings["ru"]["FileType"] = "Тип";
            _strings["ru"]["FileSize"] = "Размер";
            _strings["ru"]["Path"] = "Путь";
            _strings["ru"]["Created"] = "Создан";
            _strings["ru"]["Modified"] = "Изменен";
            _strings["ru"]["SearchInChat"] = "Поиск по сообщениям";
            _strings["ru"]["Online"] = "онлайн";
            _strings["ru"]["LastSeen"] = "был(а)";
            _strings["ru"]["Members"] = "Участники";
            _strings["ru"]["Student"] = "Студент";
            _strings["ru"]["Teacher"] = "Преподаватель";
            _strings["ru"]["Admin"] = "Администратор";
            _strings["ru"]["ContentManager"] = "Контент-менеджер";
            _strings["ru"]["MyChats"] = " Мои чаты";
            _strings["ru"]["TeacherChats"] = " Чаты групп";
            _strings["ru"]["LoadingChats"] = "Загрузка чатов...";
            _strings["ru"]["FailedToLoadChats"] = "Не удалось загрузить чаты";
            _strings["ru"]["FailedToLoadGroups"] = "Не удалось загрузить группы";
            _strings["ru"]["ManageGroups"] = " Управление";
            _strings["ru"]["ViewProfile"] = " Профиль";
            _strings["ru"]["ClearHistory"] = "Очистить историю";
            _strings["ru"]["Block"] = " Заблокировать";
            _strings["ru"]["Typing"] = "печатает...";
            _strings["ru"]["Photo"] = "Фото";
            _strings["ru"]["Document"] = "Документ";
            _strings["ru"]["Archive"] = "Архив";
            _strings["ru"]["Support"] = "Поддержка";
            _strings["ru"]["SearchResults"] = "Результаты поиска";
            _strings["ru"]["NoResults"] = "Ничего не найдено";
            _strings["ru"]["AddMember"] = "➕ Добавить участника";
            _strings["ru"]["EnterUsername"] = "Введите логин пользователя";
            _strings["ru"]["MemberAdded"] = "Участник добавлен";
            _strings["ru"]["SelectGroupAvatar"] = "Выберите аватар для группы";
            _strings["ru"]["AvatarUpdated"] = "Аватар группы обновлен";
            _strings["ru"]["FileTooLarge"] = "Файл слишком большой (максимум 50 МБ)";
            _strings["ru"]["FileSent"] = "Файл отправлен";
            _strings["ru"]["SelectGroupAvatar"] = "Выберите аватар для группы";
            _strings["ru"]["AvatarUpdated"] = "Аватар группы обновлен";
            _strings["ru"]["SearchResults"] = "Результаты поиска";

            // Английские строки
            _strings["en"]["Settings"] = "Settings";
            _strings["en"]["Language"] = "Language";
            _strings["en"]["Theme"] = "Theme";
            _strings["en"]["Standard"] = "Standard";
            _strings["en"]["Teen"] = "For Teens";
            _strings["en"]["Current"] = "Current";
            _strings["en"]["Russian"] = "Russian";
            _strings["en"]["English"] = "English";
            _strings["en"]["Save"] = "Save";
            _strings["en"]["Back"] = "Back";
            _strings["en"]["Success"] = "Success";
            _strings["en"]["OK"] = "OK";
            _strings["en"]["Error"] = "Error";
            _strings["en"]["MyActiveCourses"] = "My Active Courses";
            _strings["en"]["TodayTasks"] = "Today's Tasks";
            _strings["en"]["LatestNews"] = "Latest News";
            _strings["en"]["QuickAccess"] = "Quick Access";
            _strings["en"]["AllCourses"] = "All Courses";
            _strings["en"]["MyCourses"] = "My Courses";
            _strings["en"]["Progress"] = "Progress";
            _strings["en"]["Achievements"] = "Achievements";
            _strings["en"]["Shop"] = "Shop";
            _strings["en"]["Streak"] = "Streak";
            _strings["en"]["Currency"] = "Coins";
            _strings["en"]["User"] = "User";
            _strings["en"]["Appearance"] = "Appearance";
            _strings["en"]["EmailNotifications"] = "Email Notifications";
            _strings["en"]["PushNotifications"] = "Push Notifications";
            _strings["en"]["Notifications"] = "Notifications";
            _strings["en"]["ThemeChanged"] = "Theme changed successfully!";
            _strings["en"]["SettingsSaved"] = "Settings saved!";
            _strings["en"]["SaveFailed"] = "Failed to save settings";
            _strings["en"]["AvatarFrames"] = "Avatar Frames";
            _strings["en"]["ProfileEmojis"] = "Profile Emojis";
            _strings["en"]["Themes"] = "Themes";
            _strings["en"]["Badges"] = "Badges";
            _strings["en"]["MyInventory"] = "My Inventory";
            _strings["en"]["EarnCoins"] = "Earn Coins";
            _strings["en"]["Loading"] = "Loading...";
            _strings["en"]["Buy"] = "Buy";
            _strings["en"]["Equip"] = "Equip";
            _strings["en"]["Equipped"] = " Equipped";
            _strings["en"]["NotEnoughCoins"] = "Not Enough Coins";
            _strings["en"]["NeedMoreCoins"] = "You need {0} more coins 🪙";
            _strings["en"]["ItemPurchased"] = "Item \"{0}\" purchased!";
            _strings["en"]["ItemEquipped"] = "\"{0}\" is now active!";
            _strings["en"]["ItemUnequipped"] = "\"{0}\" unequipped";
            _strings["en"]["ThemeApplied"] = "Theme \"{0}\" applied!";
            _strings["en"]["FailedToLoadItems"] = "Failed to load shop items";
            _strings["en"]["OperationError"] = "Operation error";
            _strings["en"]["FailedToOpenInventory"] = "Failed to open inventory";
            _strings["en"]["CoinsAdded"] = "+50 coins added!";
            _strings["en"]["FailedToAddCoins"] = "Failed to add coins";
            _strings["en"]["PurchaseFailed"] = "Purchase failed";
            _strings["en"]["Confirmation"] = "Confirmation";
            _strings["en"]["Yes"] = "Yes";
            _strings["en"]["No"] = "No";
            _strings["en"]["Login"] = "Login";
            _strings["en"]["Register"] = "Register";
            _strings["en"]["ForgotPassword"] = "Forgot Password?";
            _strings["en"]["Username"] = "Username";
            _strings["en"]["Password"] = "Password";
            _strings["en"]["CaptchaCode"] = "Captcha Code";
            _strings["en"]["AppName"] = "Educational Platform";
            _strings["en"]["CaptchaInstruction"] = "Enter the code from the image";
            _strings["en"]["PasswordRecovery"] = "Password Recovery";
            _strings["en"]["EnterLoginOrEmail"] = "Enter login or email";
            _strings["en"]["Continue"] = "Continue";
            _strings["en"]["Cancel"] = "Cancel";
            _strings["en"]["SendingRequest"] = "Sending...";
            _strings["en"]["UserNotFound"] = "User not found";
            _strings["en"]["CodeCreationFailed"] = "Failed to create reset code";
            _strings["en"]["CodeSentToEmail"] = "Reset code sent to {0}";
            _strings["en"]["Confirmation"] = "Confirmation";
            _strings["en"]["EnterSixDigitCode"] = "Enter 6-digit code from email";
            _strings["en"]["Confirm"] = "Confirm";
            _strings["en"]["InvalidOrExpiredCode"] = "Invalid or expired code";
            _strings["en"]["FailedToSendEmail"] = "Failed to send email";
            _strings["en"]["EnterLoginAndPassword"] = "Enter login and password";
            _strings["en"]["InvalidCaptcha"] = "Invalid captcha code";
            _strings["en"]["InvalidCredentials"] = "Invalid username or password";
            _strings["en"]["InvalidCredentialsWithCaptcha"] = "Invalid username or password. Try again.";
            _strings["en"]["ConfirmExit"] = "Do you really want to exit the application?";
            _strings["en"]["LearningProgress"] = "Learning Progress";
            _strings["en"]["OverallProgress"] = "Overall Progress";
            _strings["en"]["CompletedCourses"] = "Completed Courses";
            _strings["en"]["AverageScore"] = "Average Score";
            _strings["en"]["CompletionRate"] = "Completion Rate";
            _strings["en"]["CurrentStreak"] = "Current Streak";
            _strings["en"]["TotalDays"] = "Total Days";
            _strings["en"]["RecentAchievements"] = "Recent Achievements";
            _strings["en"]["CourseProgress"] = "Course Progress";
            _strings["en"]["NoAchievements"] = "No achievements";
            _strings["en"]["NoCourses"] = "No courses";
            _strings["en"]["BackToMain"] = "Back to Main";
            _strings["en"]["Course"] = "Course";
            _strings["en"]["ContinueCourse"] = "Continue";
            _strings["en"]["LanguageChanged"] = "Language changed successfully!";
            _strings["en"]["LanguageChanged"] = "Language changed successfully!";
            _strings["en"]["ThemeChanged"] = "Theme changed successfully!";
            _strings["en"]["SettingsSaved"] = "Settings saved!";
            _strings["en"]["Contests"] = "Contests";
            _strings["en"]["ProgrammingContests"] = "Programming Contests";
            _strings["en"]["ActiveContests"] = "Active Contests";
            _strings["en"]["CompletedContests"] = "Completed Contests";
            _strings["en"]["MySubmissions"] = "My Submissions";
            _strings["en"]["CreateContest"] = "Create Contest";
            _strings["en"]["SubmitProject"] = "Submit Project";
            _strings["en"]["ContestName"] = "Contest Name";
            _strings["en"]["ContestDescription"] = "Contest Description";
            _strings["en"]["SelectLanguage"] = "Select Language";
            _strings["en"]["AnyLanguage"] = "Any Language";
            _strings["en"]["StartDate"] = "Start Date";
            _strings["en"]["EndDate"] = "End Date";
            _strings["en"]["PrizeCoins"] = "Prize Coins";
            _strings["en"]["MaxParticipants"] = "Max Participants";
            _strings["en"]["OnlyForGroups"] = "Only for Study Groups";
            _strings["en"]["Create"] = "Create";
            _strings["en"]["EnterContestName"] = "Enter contest name";
            _strings["en"]["EndDateMustBeAfterStart"] = "End date must be after start date";
            _strings["en"]["ContestCreated"] = "Contest created successfully!";
            _strings["en"]["FailedToCreateContest"] = "Failed to create contest";
            _strings["en"]["ContestResults"] = "Contest Results";
            _strings["en"]["ContestSubmissions"] = "Contest Submissions";
            _strings["en"]["NoSubmissions"] = "No submissions";
            _strings["en"]["FailedToLoadSubmissions"] = "Failed to load submissions";
            _strings["en"]["FileNotFound"] = "File not found";
            _strings["en"]["FileSaved"] = "File saved";
            _strings["en"]["FileDownloaded"] = "File downloaded";
            _strings["en"]["FailedToDownloadFile"] = "Failed to download file";
            _strings["en"]["InvalidFilePath"] = "Invalid file path";
            _strings["en"]["OnlyTeachersCanGrade"] = "Only teachers can grade";
            _strings["en"]["GradingUnavailable"] = "Grading unavailable";
            _strings["en"]["GradingAvailableAfter"] = "Grading will be available after {0}";
            _strings["en"]["Grade"] = "Grade";
            _strings["en"]["EnterScore"] = "Enter score (0-100)";
            _strings["en"]["Comment"] = "Comment";
            _strings["en"]["TeacherComments"] = "Teacher's comment";
            _strings["en"]["GradeSaved"] = "Grade saved";
            _strings["en"]["FailedToSaveGrade"] = "Failed to save grade";
            _strings["en"]["ScoreMustBeNumber"] = "Score must be a number from 0 to 100";
            _strings["en"]["ProjectName"] = "Project Name";
            _strings["en"]["ProjectDescription"] = "Project Description";
            _strings["en"]["SelectZipArchive"] = "Select ZIP Archive";
            _strings["en"]["NoFileSelected"] = "No file selected";
            _strings["en"]["FailedToSelectFile"] = "Failed to select file";
            _strings["en"]["EnterProjectName"] = "Enter project name";
            _strings["en"]["SelectZipFile"] = "Select ZIP file";
            _strings["en"]["ProjectSubmitted"] = "Project submitted!";
            _strings["en"]["FailedToSubmitProject"] = "Failed to submit project";
            _strings["en"]["SubmissionError"] = "Submission error";
            _strings["en"]["AccessDenied"] = "Access Denied";
            _strings["en"]["OnlyStudentsCanParticipate"] = "Only students can participate";
            _strings["en"]["AccessRestricted"] = "Access Restricted";
            _strings["en"]["ContestOnlyForGroups"] = "This contest is only available to study group members";
            _strings["en"]["FailedToLoadContests"] = "Failed to load contests";
            _strings["en"]["NoActiveContests"] = "No active contests to participate";
            _strings["en"]["SelectContest"] = "Select Contest";
            _strings["en"]["Cancel"] = "Cancel";
            _strings["en"]["OnlyTeachersCanEditContests"] = "Only teachers can edit contests";
            _strings["en"]["OnlyTeachersCanDeleteContests"] = "Only teachers can delete contests";
            _strings["en"]["ConfirmDeleteContest"] = "Are you sure you want to delete this contest? All related submissions will also be deleted.";
            _strings["en"]["ContestDeleted"] = "Contest deleted successfully";
            _strings["en"]["EditContest"] = "Edit Contest";
            _strings["en"]["TotalSubmissions"] = "Total submissions";
            _strings["en"]["EditContest"] = "Edit Contest";
            _strings["en"]["ContestActive"] = "Contest Active";
            _strings["en"]["CreatedBy"] = "Created by:";
            _strings["en"]["CreatedDateFormat"] = "Created: {0}";
            _strings["en"]["Statistics"] = "Statistics";
            _strings["en"]["DeleteContest"] = "Delete Contest";
            _strings["en"]["TotalSubmissionsFormat"] = "Total submissions: {0}";
            _strings["en"]["GradedSubmissionsFormat"] = "Graded: {0} | Pending: {1}";
            _strings["en"]["ContestUpdated"] = "Contest updated successfully";
            _strings["en"]["FailedToUpdateContest"] = "Failed to update contest";
            _strings["en"]["ContestNotFound"] = "Contest not found";
            _strings["en"]["NoEditRights"] = "You don't have permission to edit this contest";
            _strings["en"]["OnlyCreatorCanEdit"] = "Only the creator can edit this contest";
            _strings["en"]["Deleting"] = "Deleting...";
            _strings["en"]["News"] = "News";
            _strings["en"]["PlatformNews"] = "Platform News";
            _strings["en"]["SearchNews"] = "Search news...";
            _strings["en"]["All"] = "📰 All";
            _strings["en"]["Courses"] = "📚 Courses";
            _strings["en"]["Contests"] = "🏆 Contests";
            _strings["en"]["System"] = "⚙️ System";
            _strings["en"]["General"] = "📰 General";
            _strings["en"]["CreateNews"] = "Create News";
            _strings["en"]["EditNews"] = "Edit News";
            _strings["en"]["DeleteNews"] = "Delete News";
            _strings["en"]["NewsTitle"] = "News Title";
            _strings["en"]["NewsSummary"] = "Summary";
            _strings["en"]["NewsContent"] = "Content";
            _strings["en"]["ForTeens"] = "For Teens";
            _strings["en"]["ImageUrl"] = "Image URL";
            _strings["en"]["Creating"] = "Creating...";
            _strings["en"]["EnterTitle"] = "Enter news title";
            _strings["en"]["EnterContent"] = "Enter news content";
            _strings["en"]["NewsCreated"] = "News created successfully";
            _strings["en"]["FailedToCreateNews"] = "Failed to create news";
            _strings["en"]["ConfirmDeleteNews"] = "Are you sure you want to delete this news?";
            _strings["en"]["NewsDeleted"] = "News deleted";
            _strings["en"]["FailedToDeleteNews"] = "Failed to delete news";
            _strings["en"]["FailedToLoadNews"] = "Failed to load news";
            _strings["en"]["EditNews"] = "Edit News";
            _strings["en"]["NewsActive"] = "News Active";
            _strings["en"]["NewsUpdated"] = "News updated successfully";
            _strings["en"]["FailedToUpdateNews"] = "Failed to update news";
            _strings["en"]["Saving"] = "Saving...";
            _strings["en"]["AttachFile"] = "Attach file";
            _strings["en"]["SelectPhoto"] = "Select photo";
            _strings["en"]["SelectVideo"] = "Select video";
            _strings["en"]["SelectDocument"] = "Select document";
            _strings["en"]["SelectArchive"] = "Select archive";
            _strings["en"]["ChooseEmoji"] = "Choose emoji";
            _strings["en"]["Download"] = "Download";
            _strings["en"]["Open"] = "Open";
            _strings["en"]["Info"] = "Info";
            _strings["en"]["FileName"] = "Name";
            _strings["en"]["FileType"] = "Type";
            _strings["en"]["FileSize"] = "Size";
            _strings["en"]["Path"] = "Path";
            _strings["en"]["Created"] = "Created";
            _strings["en"]["Modified"] = "Modified";
            _strings["en"]["SearchInChat"] = "Search in chat";
            _strings["en"]["Online"] = "online";
            _strings["en"]["LastSeen"] = "last seen";
            _strings["en"]["Members"] = "Members";
            _strings["en"]["Student"] = "Student";
            _strings["en"]["Teacher"] = "Teacher";
            _strings["en"]["Admin"] = "Admin";
            _strings["en"]["ContentManager"] = "Content Manager";
            _strings["en"]["MyChats"] = " My Chats";
            _strings["en"]["TeacherChats"] = " Group Chats";
            _strings["en"]["LoadingChats"] = "Loading chats...";
            _strings["en"]["FailedToLoadChats"] = "Failed to load chats";
            _strings["en"]["FailedToLoadGroups"] = "Failed to load groups";
            _strings["en"]["ManageGroups"] = " Manage";
            _strings["en"]["ViewProfile"] = "Profile";
            _strings["en"]["ClearHistory"] = " Clear history";
            _strings["en"]["Block"] = "Block";
            _strings["en"]["Typing"] = "typing...";
            _strings["en"]["Photo"] = "Photo";
            _strings["en"]["Document"] = "Document";
            _strings["en"]["Archive"] = "Archive";
            _strings["en"]["Support"] = "Support";
            _strings["en"]["SearchResults"] = "Search results";
            _strings["en"]["NoResults"] = "No results found";
            _strings["en"]["AddMember"] = "➕ Add member";
            _strings["en"]["EnterUsername"] = "Enter username";
            _strings["en"]["MemberAdded"] = "Member added";
            _strings["en"]["SelectGroupAvatar"] = "Select group avatar";
            _strings["en"]["AvatarUpdated"] = "Group avatar updated";
            _strings["en"]["FileTooLarge"] = "File too large (max 50 MB)";
            _strings["en"]["FileSent"] = "File sent";
            _strings["en"]["SelectGroupAvatar"] = "Select group avatar";
            _strings["en"]["AvatarUpdated"] = "Group avatar updated";
            _strings["en"]["SearchResults"] = "Search results";
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    Console.WriteLine($" LocalizationService: смена языка с {_currentLanguage} на {value}");

                    _currentLanguage = value;
                    Preferences.Set(LANGUAGE_KEY, value);

                    // Устанавливаем культуру
                    try
                    {
                        var culture = new CultureInfo(value);
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка установки культуры: {ex.Message}");
                    }

                    // ВЫЗЫВАЕМ СОБЫТИЕ
                    LanguageChanged?.Invoke(this, value);
                    Console.WriteLine($"LocalizationService: событие LanguageChanged вызвано");
                }
            }
        }

        public string GetText(string key)
        {
            if (_strings.ContainsKey(CurrentLanguage) && _strings[CurrentLanguage].ContainsKey(key))
                return _strings[CurrentLanguage][key];

            // Fallback на русский
            if (_strings["ru"].ContainsKey(key))
                return _strings["ru"][key];

            return key;
        }

        public string GetRandomGreeting(string userName)
        {
            var greetings = CurrentLanguage == "ru"
                ? new[] { $"Привет, {userName}!", $"С возвращением, {userName}!", $"Рады видеть вас, {userName}!" }
                : new[] { $"Hello, {userName}!", $"Welcome back, {userName}!", $"Great to see you, {userName}!" };

            var random = new Random();
            return greetings[random.Next(greetings.Length)];
        }

        public string GetStreakMessage(int streakDays)
        {
            if (CurrentLanguage == "ru")
            {
                if (streakDays == 0) return "Начните свою серию сегодня!";
                if (streakDays == 1) return "Отличное начало! 1 день";
                if (streakDays < 7) return $" Серия: {streakDays} дней!";
                if (streakDays < 30) return $" Круто! {streakDays} дней подряд!";
                return $"🌟 Невероятно! {streakDays} дней!";
            }
            else
            {
                if (streakDays == 0) return "Start your streak today!";
                if (streakDays == 1) return "Great start! 1 day";
                if (streakDays < 7) return $"Streak: {streakDays} days!";
                if (streakDays < 30) return $" Awesome! {streakDays} days in a row!";
                return $"🌟 Incredible! {streakDays} days!";
            }
        }

        /// <summary>
        /// Применяет стиль для подростков или стандартный стиль
        /// </summary>
        /// <param name="isTeen">true - применить подростковый стиль, false - стандартный</param>
        public void SetTeenStyle(bool isTeen)
        {
            try
            {
                Console.WriteLine($"Применяем стиль: {(isTeen ? "Для подростков" : "Стандартный")}");

                if (Application.Current == null) return;

                Application.Current.Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        if (isTeen)
                        {
                            // Загружаем стиль для подростков из файла
                            var teenStyles = new EducationalPlatform.Resources.Styles.TeenStyles();

                            // Очищаем и добавляем новый словарь стилей
                            Application.Current.Resources.MergedDictionaries.Clear();
                            Application.Current.Resources.MergedDictionaries.Add(teenStyles);

                            Console.WriteLine("Применен стиль для подростков из TeenStyles.xaml");
                        }
                        else
                        {
                            // Применяем стандартную тему через SettingsService
                            if (_settingsService != null)
                            {
                                _settingsService.ApplyTheme(_settingsService.CurrentTheme);
                            }
                            else
                            {
                                // Если SettingsService недоступен, загружаем стандартные цвета
                                var defaultColors = ThemeColorService.GetThemeColors("standard");
                                Application.Current.Resources["PrimaryColor"] = defaultColors.Primary;
                                Application.Current.Resources["SecondaryColor"] = defaultColors.Secondary;
                                Application.Current.Resources["BackgroundColor"] = defaultColors.Background;
                                Application.Current.Resources["AccentColor"] = defaultColors.Accent;
                                Application.Current.Resources["DangerColor"] = defaultColors.Danger;
                                Application.Current.Resources["TextColor"] = defaultColors.Text;
                                Application.Current.Resources["LightTextColor"] = defaultColors.LightText;
                            }

                            Console.WriteLine("Применен стандартный стиль");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Ошибка при применении стиля: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка в SetTeenStyle: {ex.Message}");
            }
        }

        public void SetLanguage(string language) => CurrentLanguage = language;

        public string this[string key] => GetText(key);
    }
}