using System;
using System.Security.Cryptography;
using System.Text;

namespace Common.Summer.Security
{
    public class RsaEncryption
    {
        private RSA rsa;

        public RsaEncryption()
        {
            rsa = RSA.Create();
        }

        public string GetPublicKey()
        {
            return Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        }

        public void ImportPublicKey(string publicKey)
        {
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        }

        public string GetPrivateKey()
        {
            return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        }

        public void ImportPrivateKey(string privateKey)
        {
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
        }

        public string Encrypt(string plainText)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedData = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedData);
        }

        public string Decrypt(string cipherText)
        {
            byte[] dataToDecrypt = Convert.FromBase64String(cipherText);
            byte[] decryptedData = rsa.Decrypt(dataToDecrypt, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(decryptedData);
        }

        public static (string PublicKey, string PrivateKey) GenerateRsaKeyPair(int keySize = 2048)
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = keySize;

                // Export the keys in XML format
                string publicKey = rsa.ToXmlString(false); // Only public key
                string privateKey = rsa.ToXmlString(true); // Both public and private key

                return (publicKey, privateKey);
            }
        }
    }
}
