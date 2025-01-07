using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Common.Summer.Security
{
    public class RsaEncryption
    {
        private RSA rsa;

        public RsaEncryption()
        {
            rsa = RSA.Create(2048);
        }

        public string GetPublicKey()
        {
            var parameters = rsa.ExportParameters(false);
            var publicKey = new RSAParameters
            {
                Modulus = parameters.Modulus,
                Exponent = parameters.Exponent
            };
            return Convert.ToBase64String(publicKey.Modulus) + "|" + Convert.ToBase64String(publicKey.Exponent);
        }

        public string GetPrivateKey()
        {
            var parameters = rsa.ExportParameters(true);
            var privateKey = new RSAParameters
            {
                Modulus = parameters.Modulus,
                Exponent = parameters.Exponent,
                D = parameters.D,
                P = parameters.P,
                Q = parameters.Q,
                DP = parameters.DP,
                DQ = parameters.DQ,
                InverseQ = parameters.InverseQ
            };
            return Convert.ToBase64String(privateKey.Modulus) + "|" + Convert.ToBase64String(privateKey.Exponent) + "|" +
                   Convert.ToBase64String(privateKey.D) + "|" + Convert.ToBase64String(privateKey.P) + "|" +
                   Convert.ToBase64String(privateKey.Q) + "|" + Convert.ToBase64String(privateKey.DP) + "|" +
                   Convert.ToBase64String(privateKey.DQ) + "|" + Convert.ToBase64String(privateKey.InverseQ);
        }

        public void ImportPublicKey(string publicKey)
        {
            var parts = publicKey.Split('|');
            var parameters = new RSAParameters
            {
                Modulus = Convert.FromBase64String(parts[0]),
                Exponent = Convert.FromBase64String(parts[1])
            };
            rsa.ImportParameters(parameters);
        }

        public void ImportPrivateKey(string privateKey)
        {
            var parts = privateKey.Split('|');
            var parameters = new RSAParameters
            {
                Modulus = Convert.FromBase64String(parts[0]),
                Exponent = Convert.FromBase64String(parts[1]),
                D = Convert.FromBase64String(parts[2]),
                P = Convert.FromBase64String(parts[3]),
                Q = Convert.FromBase64String(parts[4]),
                DP = Convert.FromBase64String(parts[5]),
                DQ = Convert.FromBase64String(parts[6]),
                InverseQ = Convert.FromBase64String(parts[7])
            };
            rsa.ImportParameters(parameters);
        }

        public string Encrypt(string plainText)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
            int maxBlockSize = 245; // 2048 bits / 8 - 11 (padding) = 245 bytes
            var encryptedData = new List<byte>();

            for (int i = 0; i < dataToEncrypt.Length; i += maxBlockSize)
            {
                byte[] block = dataToEncrypt.Skip(i).Take(maxBlockSize).ToArray();
                byte[] encryptedBlock = rsa.Encrypt(block, RSAEncryptionPadding.Pkcs1);
                encryptedData.AddRange(encryptedBlock);
            }

            return Convert.ToBase64String(encryptedData.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            byte[] dataToDecrypt = Convert.FromBase64String(cipherText);
            int blockSize = 256; // 2048 bits / 8 = 256 bytes
            var decryptedData = new List<byte>();

            for (int i = 0; i < dataToDecrypt.Length; i += blockSize)
            {
                byte[] block = dataToDecrypt.Skip(i).Take(blockSize).ToArray();
                byte[] decryptedBlock = rsa.Decrypt(block, RSAEncryptionPadding.Pkcs1);
                decryptedData.AddRange(decryptedBlock);
            }

            return Encoding.UTF8.GetString(decryptedData.ToArray());
        }
    }
}
