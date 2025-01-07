namespace Common.Summer.Security
{
    public class EncryptionManager
    {
        private AesEncryption aesEncryption;
        private RsaEncryption remoteRsaEncryption;
        private RsaEncryption localRsaEncryption;

        public void Init()
        {
            localRsaEncryption = new RsaEncryption();
            remoteRsaEncryption = new RsaEncryption();

        }
        public void UnInit()
        {
            aesEncryption = null;
            localRsaEncryption = null;
            remoteRsaEncryption = null;
        }
        public string AesEncrypt(string plainText)
        {
            return aesEncryption.Encrypt(plainText);
        }
        public string AesDecrypt(string cipherText)
        {
            return aesEncryption.Decrypt(cipherText);
        }
        public string RsaEncrypt(string plainText)
        {
            return remoteRsaEncryption.Encrypt(plainText);
        }
        public string RsaDecrypt(string cipherText)
        {
            return localRsaEncryption.Decrypt(cipherText);
        }
        public (string key, string iv) GenerateAesKeyAndIv()
        {
            return AesEncryption.GenerateAesKeyAndIv();
        }
        public bool SetAesKeyAndIv(string key, string iv)
        {
            aesEncryption = new AesEncryption(key, iv);
            return true;
        }
        public bool SetRemoteRsaPublicKey(string key)
        {
            remoteRsaEncryption.ImportPublicKey(key);
            return true;
        }

        public string GetLocalRsaPublicKey()
        {
            return localRsaEncryption.GetPublicKey();
        }


        //完整性验证

        //数字签名
    }
}
