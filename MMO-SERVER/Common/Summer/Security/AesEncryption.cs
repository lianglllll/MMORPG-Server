using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common.Summer.Security
{
    public class AesEncryption
    {
        private readonly byte[] key;
        private readonly byte[] iv;

        public AesEncryption(string Key,string IV)
        {
            // Ensure the key and IV have a length of 16 bytes (128 bits), 24 bytes (192 bits), or 32 bytes (256 bits).
            key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));
            iv = Encoding.UTF8.GetBytes(IV.PadRight(16).Substring(0, 16));
        }

        public string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        public string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        public static (string Key, string IV) GenerateAesKeyAndIv()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                // Convert the byte arrays to Base64 strings for easy storage or transmission
                string key = Convert.ToBase64String(aes.Key);
                string iv = Convert.ToBase64String(aes.IV);

                return (key, iv);
            }
        }
    }
}
