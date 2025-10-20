using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationalPlatform.Services
{
    public class CaptchaService
    {
        private int _failedAttempts = 0;
        private const int MAX_ATTEMPTS = 3;

        public bool IsCaptchaRequired => _failedAttempts >= MAX_ATTEMPTS;

        public void RecordFailedAttempt()
        {
            _failedAttempts++;
        }

        public void ResetAttempts()
        {
            _failedAttempts = 0;
        }

        public string GenerateCaptchaText()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var captcha = new StringBuilder(6);

            for (int i = 0; i < 6; i++)
            {
                captcha.Append(chars[random.Next(chars.Length)]);
            }

            return captcha.ToString();
        }

        public bool ValidateCaptcha(string userInput, string correctCaptcha)
        {
            return string.Equals(userInput, correctCaptcha, StringComparison.OrdinalIgnoreCase);
        }
    }
}
