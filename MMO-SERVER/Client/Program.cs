using System;
using System.Threading;
using Serilog;
using System.IO;
using System.Collections.Generic;
using lLua.Binchunk;
using Common.Summer.Security;
using Serilog.Sinks.SystemConsole.Themes;
using Common.Summer.Net;
using HS.Protobuf.Login;
using Google.Protobuf;
using Common.Summer.Core;
using HS.Protobuf.LoginGate;
using HS.Protobuf.Common;
using Common.Summer.MyLog;
using Common.Summer.Server;
using System.Collections;

namespace ClientTest
{

    public class MyHeap<T> where T : IComparable<T>
    {
        private T[] m_elements;
        private int m_size;
        private bool m_isMaxHeap;

        public MyHeap(bool isMaxHeap = true, int capacity = 4)
        {
            m_elements = new T[capacity];
            m_size = 0;
            m_isMaxHeap = isMaxHeap;
        }

        public int Count => m_size;

        public T Peek()
        {
            if (m_size == 0) throw new InvalidOperationException("Heap is empty");
            return m_elements[0];
        }

        public void Insert(T item)
        {
            if (m_size == m_elements.Length)
            {
                Array.Resize(ref m_elements, m_elements.Length * 2);
            }

            m_elements[m_size] = item;
            HeapifyUp(m_size);
            m_size++;
        }

        public T ExtractTop()
        {
            if (m_size == 0) throw new InvalidOperationException("Heap is empty");

            T top = m_elements[0];
            m_size--;
            m_elements[0] = m_elements[m_size];
            HeapifyDown(0);
            return top;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                // 检查父节点是否满足堆性质，若满足则停止
                if (Compare(m_elements[parentIndex], m_elements[index]))
                    break;
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int targetChild = index;

                // 选择更符合堆性质的子节点（大根堆选更大，小根堆选更小）
                if (leftChild < m_size &&
                    !Compare(m_elements[targetChild], m_elements[leftChild]))
                {
                    targetChild = leftChild;
                }
                if (rightChild < m_size &&
                    !Compare(m_elements[targetChild], m_elements[rightChild]))
                {
                    targetChild = rightChild;
                }

                if (targetChild == index) break;

                Swap(index, targetChild);
                index = targetChild;
            }
        }

        private bool Compare(T parent, T child)
        {
            int result = parent.CompareTo(child);
            return m_isMaxHeap ? result > 0 : result < 0;
        }

        private void Swap(int i, int j)
        {
            T temp = m_elements[i];
            m_elements[i] = m_elements[j];
            m_elements[j] = temp;
        }

        public void Clear()
        {
            Array.Clear(m_elements, 0, m_size);
            m_size = 0;
        }
    }



    class Program
    {
        static void TestAES()
        {
            var pair = AesEncryption.GenerateAesKeyAndIv();
            AesEncryption aes = new AesEncryption(pair.Key, pair.IV);

            string original = "Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111" +
                "Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111" +
                "Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111Hello, World!111111111111111111111111111111111111111111111111111111111111111111111111111111111";
            Console.WriteLine("Original:   " + original);

            string encrypted = aes.Encrypt(original);
            Console.WriteLine("Encrypted:  " + encrypted);

            string decrypted = aes.Decrypt(encrypted);
            Console.WriteLine("Decrypted:  " + decrypted);
        }

        static void TestRSA()
        {
            RsaEncryption rsaEncryption = new RsaEncryption();

            // 获取公钥和私钥
            string publicKey = rsaEncryption.GetPublicKey();
            string privateKey = rsaEncryption.GetPrivateKey();

            Console.WriteLine("Public Key: " + publicKey);
            Console.WriteLine("Private Key: " + privateKey);

            var pair = AesEncryption.GenerateAesKeyAndIv();

            // 加密数据
            string originalText = pair.Key;
            Console.WriteLine("\nOriginal: " + pair.Key);

            string encryptedText = rsaEncryption.Encrypt(originalText);
            Console.WriteLine("Encrypted: " + encryptedText);

            // 解密数据
            string decryptedText = rsaEncryption.Decrypt(encryptedText);
            Console.WriteLine("Decrypted: " + decryptedText);
        }

        static void TestRsa2()
        {
            EncryptionManager encryptionManager1 = new();
            encryptionManager1.Init();
            EncryptionManager encryptionManager2 = new();
            encryptionManager2.Init();

            string originalText = "hello world111111111111111111111111111111111111111111111111111world111111111111111111111111111111111111111111111111111world111111111111111111111111111111111111111111111111111" +
                "world111111111111111111111111111111111111111111111111111world111111111111111111111111111111111111111111111111111world1111111" +
                "1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111";
            Console.WriteLine("Original: " + originalText);

            encryptionManager1.SetRemoteRsaPublicKey(encryptionManager2.GetLocalRsaPublicKey());
            string encryptedText = encryptionManager1.RsaEncrypt(originalText);
            Console.WriteLine("Encrypted: " + encryptedText);

            string decryptedText = encryptionManager2.RsaDecrypt(encryptedText);
            Console.WriteLine("Decrypted: " + decryptedText);

        }

        static void TestHash()
        {
            // 示例密码
            string password = "my_secure_password";

            // 哈希密码
            string hashedPassword = PasswordHasher.Instance.HashPassword(password);
            Console.WriteLine($"Hashed Password: {hashedPassword}");

            // 验证密码
            bool isPasswordCorrect = PasswordHasher.Instance.VerifyPassword("my_secure_password", hashedPassword);
            Console.WriteLine($"Is the password correct? {isPasswordCorrect}");

            // 尝试使用错误的密码进行验证
            bool isWrongPasswordCorrect = PasswordHasher.Instance.VerifyPassword("wrong_password", hashedPassword);
            Console.WriteLine($"Is the wrong password correct? {isWrongPasswordCorrect}");
        }


        static void Lua()
        {
            string fileName = "luac.out";

            // 获取当前执行的可执行文件所在的目录
            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;

            // 构建完整的文件路径
            string filePath = Path.Combine(directoryPath, fileName);

            try
            {
                // 读取文件内容
                byte[] fileBytes = File.ReadAllBytes(filePath);
                var p = Binary_Chunk.Undump(fileBytes);
                Binary_Chunk.Print(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            SerilogManager.Instance.Init();
            // Thread.Sleep(2000);

            //ProtoHelper.Register<UserLoginRequest>((int)LoginProtocl.UserLoginRequest);
            //ProtoHelper.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResponse);
            //ProtoHelper.Register<GetLoginGateTokenRequest>((int)LoginGateProtocl.GetLogingateTokenReq);
            //ProtoHelper.Register<GetLoginGateTokenResponse>((int)LoginGateProtocl.GetLogingateTokenResp);

            //MessageRouter.Instance.Start(1);
            //MessageRouter.Instance.Subscribe<UserLoginResponse>(_HandleUserLoginResponse);
            //MessageRouter.Instance.Subscribe<GetLoginGateTokenResponse>(_HandleGetLoginGateTokenResponse);


            //m_netClient = new NetClient();
            //m_netClient.Init("127.0.0.1", 10700, 10,
            //    (netClient) => { 
            //        Log.Debug("Connected to LoginGate Server.");
            //    },
            //    (tcpClient, isEnd) => { },
            //    (tcpClient) => { });

            // TestHash();


            Console.ReadLine();
        }

        private static void TestServerSelector()
        {
            var server1 = new ServerInfoNode
            {
                ServerId = 1,
            };
            var server2 = new ServerInfoNode
            {
                ServerId = 2,
            };
            var server3 = new ServerInfoNode
            {
                ServerId = 3,
            };
            List<ServerInfoNode> list = new List<ServerInfoNode>();
            list.Add(server1);
            list.Add(server2);
            list.Add(server3);

            var selector = new ServerSelector(list, ServerSelector.SelectionStrategy.Random);
            var serverInfoNode =  selector.SelectServer();
            Log.Information(serverInfoNode.ToString());
            serverInfoNode = selector.SelectServer();
            Log.Information(serverInfoNode.ToString());
            serverInfoNode = selector.SelectServer();
            Log.Information(serverInfoNode.ToString());
            serverInfoNode = selector.SelectServer();
            Log.Information(serverInfoNode.ToString());
        }

        private static NetClient m_netClient;
        private static string loginGateToken;

        private static void _HandleGetLoginGateTokenResponse(Connection sender, GetLoginGateTokenResponse message)
        {
            loginGateToken = message.LoginGateToken;
            IMessage userLoginRequest = new UserLoginRequest { Username = "令狐冲", Password = "123" , LoginGateToken = loginGateToken };
            m_netClient.Send(userLoginRequest);
        }

        private static void _HandleUserLoginResponse(Connection sender, UserLoginResponse message)
        {
            Log.Information(message.ToString());  
        }

    }
}
