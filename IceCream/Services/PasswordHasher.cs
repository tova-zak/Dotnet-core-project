using System;
using System.Security.Cryptography;

namespace IceCream.Services
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; 
        private const int KeySize = 32; 
        private const int Iterations = 10000; 
        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string hashed)
        {
            if (string.IsNullOrEmpty(hashed)) return false;
            var parts = hashed.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out var iterations)) return false;
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var keyToCheck = pbkdf2.GetBytes(key.Length);
            return CryptographicOperations.FixedTimeEquals(keyToCheck, key);
        }
    }
}
