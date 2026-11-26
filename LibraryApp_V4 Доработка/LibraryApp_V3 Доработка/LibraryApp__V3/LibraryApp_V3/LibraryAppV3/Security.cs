using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LibraryApp
{
    /// <summary>Хеширование паролей: PBKDF2-HMAC-SHA256, 100000 итераций, соль 16 байт.</summary>
    public static class Security
    {
        public const int Iterations = 100000;

        /// <summary>Коды доступа для самостоятельной регистрации сотрудника и администратора
        /// (ученик регистрируется без кода). Замените на свои значения.</summary>
        public const string StaffCode = "STAFF-2026";
        public const string AdminCode = "ADMIN-2026";

        public static string NewSalt()
        {
            byte[] salt = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return ToHex(salt);
        }

        public static string Hash(string password, string saltHex)
        {
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, FromHex(saltHex), Iterations, HashAlgorithmName.SHA256))
            {
                return ToHex(kdf.GetBytes(32));
            }
        }

        public static bool Verify(string password, string saltHex, string expectedHashHex)
        {
            string actual = Hash(password, saltHex);
            if (actual.Length != expectedHashHex.Length) return false;
            int diff = 0;
            for (int i = 0; i < actual.Length; i++) diff |= actual[i] ^ expectedHashHex[i];
            return diff == 0;
        }

        /// <summary>Политика паролей: не менее 8 символов, строчная и заглавная буквы, цифра.</summary>
        public static string CheckPasswordPolicy(string password)
        {
            if (password == null || password.Length < 8) return "Пароль должен содержать не менее 8 символов.";
            if (!Regex.IsMatch(password, "[a-zа-яё]")) return "Пароль должен содержать строчную букву.";
            if (!Regex.IsMatch(password, "[A-ZА-ЯЁ]")) return "Пароль должен содержать заглавную букву.";
            if (!Regex.IsMatch(password, "[0-9]")) return "Пароль должен содержать цифру.";
            return null;
        }

        static string ToHex(byte[] data)
        {
            char[] c = new char[data.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < data.Length; i++)
            {
                c[i * 2] = hex[data[i] >> 4];
                c[i * 2 + 1] = hex[data[i] & 15];
            }
            return new string(c);
        }

        static byte[] FromHex(string s)
        {
            byte[] r = new byte[s.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return r;
        }
    }
}
