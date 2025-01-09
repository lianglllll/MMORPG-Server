using Common.Summer.Tools;
using System;
using System.Security.Cryptography;

namespace Common.Summer.Security
{
    public  class PasswordHasher :Singleton<PasswordHasher>
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 10000; // Recommended is at least 10,000

        public  string HashPassword(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256);

            var salt = algorithm.Salt;
            var hash = algorithm.GetBytes(KeySize);

            // Combine salt and hash into one byte array
            var hashBytes = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);

            // Convert the byte array to a base64 string
            return Convert.ToBase64String(hashBytes);
        }

        public  bool VerifyPassword(string password, string hashedPassword)
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract salt from the stored hash
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            using var algorithm = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            var hash = algorithm.GetBytes(KeySize);

            // Compare hash with the original hash from storage (skip the salt bytes)
            for (int i = 0; i < KeySize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                    return false;
            }

            return true;
        }
    }
}
