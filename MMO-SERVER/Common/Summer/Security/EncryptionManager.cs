using System.Collections.Generic;

namespace Common.Summer.Security
{
    enum EncryptType
    {
        AES,RSA
    }

    public class EncryptionManager
    {
        private AesEncryption aesEncryption;
        public string aesKey;
        public string aesIV;

        private RsaEncryption serverRsaEncryption;
        public string serverRsaPublicKey;
        public string serverRsaPrivateKey;
        private string clientRsaPublicKey;

        public void Init()
        {
            //todo
            var pair1 = AesEncryption.GenerateAesKeyAndIv();
            aesKey = pair1.Key;
            aesIV = pair1.IV;
            aesEncryption = new AesEncryption(aesKey, aesIV);

            var pair2 = RsaEncryption.GenerateRsaKeyPair();
            serverRsaPublicKey = pair2.PublicKey;
            serverRsaPrivateKey = pair2.PrivateKey;
            serverRsaEncryption = new RsaEncryption();
        }

        public void UnInit()
        {

        }

        public string Encrypt(string plainText,List<string> args)
        {
            return null;
        }
        public string Decrypt(string cipherText, List<string> args) 
        { 
            return null ;
        }

        //对称密钥获取
        public (string key1,string key2) GetComunicationKey(string publicKey)
        {
            serverRsaEncryption.ImportPublicKey(publicKey);
            string key1 = serverRsaEncryption.Encrypt(aesKey);
            string key2 = serverRsaEncryption.Encrypt(aesIV);
            return (key1, key2);
        }

        //通过传入加密模式(组合形式)来进行加解密操作

        //完整性验证

        //数字签名
    }
}
