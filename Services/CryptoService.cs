
using System.Security.Cryptography;
using System.Text;

namespace EducationalPlatform.Services
{
    public interface ICryptoService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string GenerateMessageHash(string message);
        bool VerifyMessageHash(string message, string hash);
    }

    public class CryptoService : ICryptoService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public CryptoService()
        {
            // В реальном приложении ключи должны храниться в безопасном месте
            // и быть уникальными для каждого пользователя/чата
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes("EducationalPlatformSecretKey2024"));

            // IV может быть фиксированным или передаваться вместе с сообщением
            _iv = new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        }

        public string Encrypt(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                    return plainText;

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                using var sw = new StreamWriter(cs);

                sw.Write(plainText);
                sw.Flush();
                cs.FlushFinalBlock();

                var encrypted = Convert.ToBase64String(ms.ToArray());
                return $"[ENC]{encrypted}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка шифрования: {ex.Message}");
                return plainText;
            }
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                if (string.IsNullOrEmpty(cipherText) || !cipherText.StartsWith("[ENC]"))
                    return cipherText;

                var encryptedData = cipherText.Substring(5); // Убираем [ENC]
                var cipherBytes = Convert.FromBase64String(encryptedData);

                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка дешифрования: {ex.Message}");
                return cipherText; // Возвращаем как есть в случае ошибки
            }
        }

        public string GenerateMessageHash(string message)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyMessageHash(string message, string hash)
        {
            var computedHash = GenerateMessageHash(message);
            return computedHash == hash;
        }
    }
}