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
        private const int MAX_ATTEMPTS_BEFORE_CAPTCHA = 3;
        private readonly Random _random = new Random();
        private readonly string _characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public bool IsCaptchaRequired => _failedAttempts >= MAX_ATTEMPTS_BEFORE_CAPTCHA;

        public string GenerateCaptchaText()
        {
            var captcha = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                captcha.Append(_characters[_random.Next(_characters.Length)]);
            }
            return captcha.ToString();
        }

        public bool ValidateCaptcha(string userInput, string correctCaptcha)
        {
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(correctCaptcha))
                return false;

            return string.Equals(userInput.Trim().ToUpper(), correctCaptcha.Trim().ToUpper(), StringComparison.OrdinalIgnoreCase);
        }

        public void RecordFailedAttempt()
        {
            _failedAttempts++;
        }

        public void ResetAttempts()
        {
            _failedAttempts = 0;
        }

        public void ShowCaptcha()
        {
            _failedAttempts = MAX_ATTEMPTS_BEFORE_CAPTCHA;
        }
    }
}