using System;
using EducationalPlatform.Models;

namespace EducationalPlatform.Services
{
    /// <summary>
    /// Глобальный сервис сессии пользователя.
    /// Хранит текущего пользователя и рассылает события об изменении профиля/аватара
    /// во все части приложения (дашборд, профиль, чаты и т.д.).
    /// </summary>
    public static class UserSessionService
    {
        private static User? _currentUser;

        /// <summary>
        /// Событие вызывается при смене текущего пользователя (логин/логаут).
        /// </summary>
        public static event EventHandler<User?>? CurrentUserChanged;

        /// <summary>
        /// Событие вызывается при обновлении аватара любого пользователя.
        /// </summary>
        public static event EventHandler<AvatarChangedEventArgs>? AvatarChanged;

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (!ReferenceEquals(_currentUser, value))
                {
                    _currentUser = value;
                    CurrentUserChanged?.Invoke(null, _currentUser);
                }
            }
        }

        /// <summary>
        /// Явно вызвать событие изменения аватара.
        /// Вызывается после успешного сохранения аватара в БД.
        /// </summary>
        public static void RaiseAvatarChanged(int userId, string? avatarData)
        {
            AvatarChanged?.Invoke(null, new AvatarChangedEventArgs
            {
                UserId = userId,
                AvatarData = avatarData
            });
        }
    }

    /// <summary>
    /// Аргументы события изменения аватара.
    /// </summary>
    public class AvatarChangedEventArgs : EventArgs
    {
        public int UserId { get; init; }
        public string? AvatarData { get; init; }
    }
}


