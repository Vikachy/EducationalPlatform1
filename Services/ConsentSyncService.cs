using EducationalPlatform.Models;
using System;
using System.Threading.Tasks;

namespace EducationalPlatform.Services
{
    public class ConsentSyncService
    {
        private DatabaseService _dbService;

        public ConsentSyncService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> SyncPrivacyConsent(int userId, bool currentConsent)
        {
            try
            {
                var user = await _dbService.GetUserByIdAsync(userId);

                if (user == null)
                    return false;

                if (user.PrivacyConsentAccepted && !currentConsent)
                {
                    Console.WriteLine($"Синхронизация: согласие уже было дано ранее для пользователя {userId}");
                    return true;
                }

                if (!user.PrivacyConsentAccepted && currentConsent)
                {
                    await _dbService.UpdatePrivacyConsentAsync(userId, true);
                    Console.WriteLine($"Согласие сохранено и будет синхронизировано");
                }

                return user.PrivacyConsentAccepted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка синхронизации согласия: {ex.Message}");
                return currentConsent;
            }
        }

        public bool IsConsentValid(User user)
        {
            if (!user.PrivacyConsentAccepted || !user.PrivacyConsentDate.HasValue)
                return false;

            var timeSinceConsent = DateTime.UtcNow - user.PrivacyConsentDate.Value;
            return timeSinceConsent.TotalDays <= 365;
        }
    }
}